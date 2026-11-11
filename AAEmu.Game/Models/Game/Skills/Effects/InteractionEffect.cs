using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.Game.Models.Game.Skills.Effects;

public class InteractionEffect : EffectTemplate
{
    public WorldInteractionType WorldInteraction { get; set; }
    public uint DoodadId { get; set; }

    public override bool OnActionTime => false;

    public override void Apply(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
        CastAction castObj, EffectSource source, SkillObject skillObject, DateTime time,
        CompressedGamePackets packetBuilder = null)
    {
        Logger.Debug("InteractionEffect, {0}", WorldInteraction);

        // Fire quest interaction events before checking whether the world interaction
        // class exists (upstream issues #1279 / #1255): quest progress for doodad
        // interactions must not depend on the WI implementation being present, otherwise
        // quests whose skills use unimplemented world interactions can never advance.
        if (caster is Character questCharacter && !questCharacter.SkillCancelled && target is Doodad questDoodad)
        {
            QuestManager.Instance.DoDoodadInteractionEvents(questCharacter, questCharacter, questDoodad.TemplateId);
        }

        var classType = Type.GetType("AAEmu.Game.Models.Game.World.Interactions." + WorldInteraction);
        if (classType == null)
        {
            Logger.Error("InteractionEffect, Unknown world interaction: {0}", WorldInteraction);
            return;
        }

        Logger.Debug("InteractionEffect, Action: {0}", classType); // TODO help to debug...

        caster.Buffs.TriggerRemoveOn(Buffs.BuffRemoveOn.Interaction);

        var action = (IWorldInteraction)Activator.CreateInstance(classType);
        if (source is { Skill: { } } && casterObj != null && target != null && targetObj != null && source.Skill.Template != null)
        {
            action?.Execute(caster, casterObj, target, targetObj, source.Skill.Template.Id, DoodadId);
        }
    }
}
