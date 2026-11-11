using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncTreeByProductsCollect : DoodadFuncTemplate
{
    // doodad_funcs
    public override void Use(BaseUnit caster, Doodad owner, uint skillId, int nextPhase = 0)
    {
        if (caster is Character)
            Logger.Debug("DoodadFuncTreeByProductsCollect: TemplateId {0}, nextPhase {1}", owner.TemplateId, nextPhase);
        else
            Logger.Trace("DoodadFuncTreeByProductsCollect: TemplateId {0}, nextPhase {1}", owner.TemplateId, nextPhase);

        // Thinning an old-growth tree collects its by-products and sends the tree back to
        // its last standing growth phase (upstream issue #696). Only change the phase when
        // the data actually defines a target phase (nextPhase > 0); never allow this func
        // to route into the next_phase = -1 despawn path, which would make the tree look
        // like it was chopped down. When no phase is defined the tree simply keeps
        // standing in its current phase.
        owner.ToNextPhase = nextPhase > 0;
    }
}
