using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Merchant;
using AAEmu.Game.Models.StaticValues;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSBuyItemsPacket() : GamePacket(CSOffsets.CSBuyItemsPacket, 1)
{
    public override void Read(PacketStream stream)
    {
        var npcObjId = stream.ReadBc();
        var npc = Connection.ActiveChar.ParentWorld.GetNpc(npcObjId);

        var doodadObjId = stream.ReadBc();
        var doodad =Connection.ActiveChar.ParentWorld.GetDoodad(doodadObjId);

        var unkId = stream.ReadUInt32(); // type(id)?

        var nBuy = stream.ReadByte();
        var nBuyBack = stream.ReadByte();

        Logger.Debug($"NPCObjId:{npcObjId} DoodadObjId:{doodadObjId} unkId:{unkId} nBuy:{nBuy} nBuyBack{nBuyBack}");

        // If a NPC was provided, check if it's valid
        if (npcObjId != 0 && (npc == null || !npc.Template.Merchant || npc.Template.MerchantPackId == 0))
            return;
        MerchantGoods pack = null;
        if (npc != null)
        {
            var dist = MathUtil.CalculateDistance(Connection.ActiveChar.Transform.World.Position, npc.Transform.World.Position);
            if (dist > 3f) // 3m should be enough for NPC shops
            {
                Connection.ActiveChar.SendErrorMessage(ErrorMessageType.TooFarAway);
                return;
            }
            pack = NpcManager.Instance.GetGoods(npc.Template.MerchantPackId);
        }

        // If a Doodad was provided, check if we're near it
        if (doodadObjId != 0)
        {
            if (doodad == null)
                return;
            var dist = MathUtil.CalculateDistance(Connection.ActiveChar.Transform.World.Position, doodad.Transform.World.Position);
            if (dist > 3f) // 3m should be enough for these
            {
                Connection.ActiveChar.SendErrorMessage(ErrorMessageType.TooFarAway);
                return;
            }
        }

        var money = 0;
        var honorPoints = 0;
        var vocationBadges = 0;
        // Fix: track buyback costs separately from the shop purchases so we can only charge
        // for buyback items that were actually restored to the bag
        var buyBackMoney = 0;

        // Get list of items to buy from the shop
        var itemsBuy = new List<(uint itemId, byte itemGrade, int itemCount)>();
        for (var i = 0; i < nBuy; i++)
        {
            var itemId = stream.ReadUInt32();
            var grade = stream.ReadByte();
            var count = stream.ReadInt32();
            var currency = (ShopCurrencyType)stream.ReadByte();

            // If using a NPC shop, check if the NPC is selling the specified item
            if (npcObjId != 0 && (pack == null || !pack.SellsItem(itemId)))
                continue;

            if (doodadObjId != 0)
            {
                // TODO: validate doodad "shop" (mirage furniture for example)
                // unkId value looks related to the "shop type" for buying, but unsure how it's all linked
            }

            itemsBuy.Add((itemId, grade, count));
            var template = ItemManager.Instance.GetTemplate(itemId);

            if (currency == ShopCurrencyType.Money)
                money += template.Price * count;
            else if (currency == ShopCurrencyType.Honor)
                honorPoints += template.HonorPrice * count;
            else if (currency == ShopCurrencyType.VocationBadges)
                vocationBadges += template.LivingPointPrice * count;
            else
            {
                Logger.Error("Unknown currency type");
            }
        }

        // Get a list of items to buy from the buyback window
        var itemsBuyBack = new Dictionary<Item, int>();
        for (var i = 0; i < nBuyBack; i++)
        {
            var index = stream.ReadInt32();
            var item = Connection.ActiveChar.BuyBackItems.GetItemBySlot(index);
            /*
            if (index >= Connection.ActiveChar.BuyBack.Length)
                continue;

            var item = Connection.ActiveChar.BuyBack[index];
            */
            if (item == null)
                continue;
            itemsBuyBack.Add(item, index);
            buyBackMoney += (int)(item.Template.Refund * ItemManager.Instance.GetGradeTemplate(item.Grade).RefundMultiplier / 100f) *
                            item.Count;
        }

        var useAAPoint = stream.ReadBoolean();

        // Fix: this check used && instead of ||, meaning the purchase was only rejected when
        // EVERY currency was short at the same time. A player with enough honor (or vocation
        // badges) could buy any number of gold-priced items for free and drive the gold counter
        // negative, since ChangeMoney() only refuses withdrawals above the current balance.
        if (money + buyBackMoney > Connection.ActiveChar.Money ||
            honorPoints > Connection.ActiveChar.HonorPoint ||
            vocationBadges > Connection.ActiveChar.VocationPoint)
            return;

        var tasks = new List<ItemTask>();
        foreach (var (itemId, grade, count) in itemsBuy)
        {
            // Omit grade when creating to prevent "cheating" when creating the grade
            Connection.ActiveChar.Inventory.Bag.AcquireDefaultItem(ItemTaskType.StoreBuy, itemId, count, -1);
            // Connection.ActiveChar.Inventory.Bag.AcquireDefaultItem(ItemTaskType.StoreBuy, itemId, count, grade);
        }

        // Fix: only charge for buyback items that actually made it back into the bag (it can be full)
        var boughtBackMoney = 0;
        foreach (var (item, index) in itemsBuyBack)
        {
            if (!Connection.ActiveChar.Inventory.Bag.AddOrMoveExistingItem(ItemTaskType.StoreBuy, item))
            {
                Logger.Warn("Buyback of item {0} ({1}) failed, bag might be full, skipping", item.Id, item.TemplateId);
                continue;
            }

            boughtBackMoney += (int)(item.Template.Refund *
                ItemManager.Instance.GetGradeTemplate(item.Grade).RefundMultiplier / 100f) * item.Count;
            tasks.Add(new ItemBuyback(item));
        }

        if (honorPoints > 0)
        {
            Connection.ActiveChar.ChangeGamePoints(GamePointKind.Honor, -honorPoints);
        }

        if (vocationBadges > 0)
        {
            Connection.ActiveChar.ChangeGamePoints(GamePointKind.Vocation, -vocationBadges);
        }

        // Fix: charge the shop price plus only the successful buyback refunds
        var totalMoney = money + boughtBackMoney;
        if (totalMoney > 0)
        {
            Connection.ActiveChar.ChangeMoney(SlotType.Inventory, -totalMoney);
        }

        Connection.SendPacket(new SCItemTaskSuccessPacket(ItemTaskType.StoreBuy, tasks, []));
    }
}
