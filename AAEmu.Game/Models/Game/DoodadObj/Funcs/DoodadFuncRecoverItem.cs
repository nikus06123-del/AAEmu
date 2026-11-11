using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncRecoverItem : DoodadFuncTemplate
{
    // doodad_funcs
    public override void Use(BaseUnit caster, Doodad owner, uint skillId, int nextPhase = 0)
    {
        Logger.Debug($"DoodadFuncRecoverItem({Id}) - Caster:{caster.Name} - DoodadOwner Template:{owner?.TemplateId} - SkillId:{skillId} - Nextphase:{nextPhase}");

        var character = (Character)caster;
        var addedItem = false;
        var item = ItemManager.Instance.GetItemByItemId(owner?.ItemId ?? 0);
        if (owner?.ItemId > 0)
        {
            if (item != null)
            {
                // Recoverable doodads, should be referencing an item in a System container, if this is not the case,
                // that means that it was already picked up by somebody else
                if (item._holdingContainer?.ContainerType != SlotType.System)
                {
                    owner.ToNextPhase = false;
                    character.SendErrorMessage(ErrorMessageType.InteractionRecoverParent); // TODO: Not sure what error I need to put here
                    // Fix #1091: the reference is stale - the item already lives in some other
                    // container (bag, equipment, ...) and will never come back to this slot.
                    // Clear it, otherwise the cargo slot keeps blocking recovering AND
                    // un-summoning the vehicle forever ("unknown pack stuck in cart").
                    Logger.Warn("DoodadFuncRecoverItem: Doodad {0} referenced item {1} that is no longer in a System container ({2}), clearing the stale cargo slot",
                        owner.ObjId, owner.ItemId, item._holdingContainer?.ContainerType.ToString() ?? "none");
                    owner.ItemId = 0;
                    owner.ItemTemplateId = 0;
                    owner.Save();
                    return;
                }

                // If it's on house property, check if the player has access to it
                if (owner.OwnerDbId > 0)
                {
                    GameObject ownerGameObject;
                    switch (owner.OwnerType)
                    {
                        case DoodadOwnerType.Slave:
                            // Fix #1091: OwnerDbId holds the slave's DATABASE Id, but this used to
                            // look it up as a temporary ObjId. The lookup always failed, so the
                            // permission check was silently skipped and anyone could take packs
                            // off somebody else's cart.
                            ownerGameObject = character.ParentWorld.SlaveManager.GetSlaveByDbId(owner.OwnerDbId);
                            break;
                        case DoodadOwnerType.Housing:
                            ownerGameObject = HousingManager.Instance.GetHouseById(owner.OwnerDbId);
                            break;
                        case DoodadOwnerType.Character:
                        case DoodadOwnerType.System:
                        default:
                            ownerGameObject = null;
                            break;
                    }

                    if (ownerGameObject != null && !ownerGameObject.AllowedToInteract(character))
                    {
                        character.SendErrorMessage(ErrorMessageType.InteractionPermissionDeny);
                        return;
                    }
                }

                if (ItemManager.Instance.IsAutoEquipTradePack(item.TemplateId))
                {
                    if (character.Inventory.TakeoffBackpack(ItemTaskType.RecoverDoodadItem, true))
                    {
                        if (character.Inventory.Equipment.AddOrMoveExistingItem(ItemTaskType.RecoverDoodadItem,
                            item,
                            (int)EquipmentItemSlot.Backpack))
                            addedItem = true;
                    }
                }
                else
                {
                    if (character.Inventory.Bag.AddOrMoveExistingItem(ItemTaskType.RecoverDoodadItem, item))
                        addedItem = true;
                    else
                        character.SendErrorMessage(ErrorMessageType.BagFull);
                }
            }
            else
            {
                // Fix #1091: the doodad still references an item Id, but the item itself no longer
                // exists (lost to a crash or container desync). Without this branch nothing would
                // ever clear the slot: recovering failed silently, and un-summoning the vehicle was
                // refused because the slot still "held an item", restarting the whole loop.
                Logger.Warn("DoodadFuncRecoverItem: Doodad {0} references missing item {1}, clearing the stale cargo slot",
                    owner.ObjId, owner.ItemId);
                owner.ItemId = 0;
                owner.ItemTemplateId = 0;
                owner.Save();
                addedItem = true; // fake success so the doodad can leave its "holding" phase
            }
        }
        else if (owner?.ItemTemplateId > 0)
        {
            addedItem = character.Inventory.Bag.AcquireDefaultItem(ItemTaskType.RecoverDoodadItem, owner.ItemTemplateId, 1);
        }
        else
        {
            // No itemId was provided with the doodad, need to check what needs to be done with this
            Logger.Warn($"DoodadFuncRecoverItem: Doodad {owner?.ObjId} has no item information attached to it");
            addedItem = true; // fake it to get rid of the error state
        }

        if (addedItem && item != null && item._holdingContainer.ContainerType == SlotType.Equipment)
            character.BroadcastPacket(new SCUnitEquipmentsChangedPacket(character.ObjId, (byte)item.Slot, item), false);

        if (addedItem && owner != null)
        {
            // remove the old reference
            owner.ItemId = 0;
            owner.ItemTemplateId = 0;
        }

        if (owner != null)
            owner.ToNextPhase = addedItem;
    }
}
