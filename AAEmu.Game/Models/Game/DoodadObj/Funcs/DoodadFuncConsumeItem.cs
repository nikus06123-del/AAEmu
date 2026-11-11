using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncConsumeItem : DoodadPhaseFuncTemplate
{
    public uint ItemId { get; set; }
    public int Count { get; set; }

    public override bool Use(BaseUnit caster, Doodad owner)
    {
        Logger.Trace("DoodadFuncConsumeItem: ItemId {0}, Count {1}", ItemId, Count);

        if (caster is not Character character || character.Inventory?.Bag == null)
            return true; // abort the phase chain, no player to consume from

        // Consume the required item from the interacting player's bag. Mirrors
        // DoodadFuncRemoveItem, but as a phase func (upstream issue #1255: the
        // Explosives Pit never consumed Lloyd's Explosive).
        var consumed = character.Inventory.Bag.ConsumeItem(ItemTaskType.DoodadItemChanger, ItemId, Count, null);

        // Return false to let the phase chain continue when the item was consumed,
        // true to abort it when there was nothing to consume (same semantics as
        // DoodadFuncRequireItem).
        return consumed <= 0;
    }
}
