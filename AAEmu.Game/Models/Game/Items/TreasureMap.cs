using System.Numerics;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Items.Templates;
using Jitter2.LinearMath;

namespace AAEmu.Game.Models.Game.Items;

public class TreasureMap : Item
{
    // Logger is inherited from PacketMarshaler

    private long _mapPositionX;
    private long _mapPositionY;

    /// <summary>
    /// True when the map points at an actual treasure chest location
    /// </summary>
    public bool HasValidTreasureTarget => MapPositionX != 0 || MapPositionY != 0;

    public double MapPositionX
    {
        get => _mapPositionX / 4096.0;
        set => _mapPositionX = (long)Math.Round(value * 4096.0);
    }

    public double MapPositionY
    {
        get => _mapPositionY / 4096.0;
        set => _mapPositionY = (long)Math.Round(value * 4096.0);
    }

    public override ItemDetailType DetailType => ItemDetailType.Treasure;
    public override uint DetailBytesLength => 24;

    /// <summary>
    /// Generates ItemDetails data based on possible spawn locations
    /// </summary>
    private void GenerateTreasureMapData()
    {
        // Pick a random target
        // TODO: Update this to use the spawners rather than the Doodads
        var possibleChests = WorldManager.Instance.GetWorld(WorldManager.DefaultInstanceId).SpawnManager.GetTreasureChestDoodadSpawners();
        if (possibleChests.Count <= 0)
        {
            // No valid location found (upstream issue #1152): leave the coordinates at
            // (0,0) and keep them re-rollable so EnsureTreasureMapData() can retry once
            // the chest spawners are available, instead of permanently stamping the map
            // with an unreachable (0,0) target and a Mythic grade.
            Logger.Warn("GenerateTreasureMapData: no treasure chest spawners available, target will be re-rolled on first use");
            return;
        }
        var rngPos = Random.Shared.Next(possibleChests.Count);
        var chest = possibleChests[rngPos];
        // Setup coordinates
        MapPositionX = chest.Position.X;
        MapPositionY = chest.Position.Y;
        
        var chestTemplate = DoodadManager.Instance.GetTemplate(chest.UnitId) ?? DoodadManager.Instance.GetTemplate(chest.RespawnDoodadTemplateId);
        var chestGradeId = chestTemplate?.GroupId ?? 0; 
        // Give it a corresponding grade
        switch (chestGradeId)
        {
            case 55: Grade = 0;
                break;
            case 56: Grade = (byte)ItemGrade.Grand;
                break;
            case 57: Grade = (byte)ItemGrade.Rare;
                break;
            case 58: Grade = (byte)ItemGrade.Arcane;
                break;
            case 59: Grade = (byte)ItemGrade.Heroic;
                break;
            default:
                // Invalid category
                Grade = (byte)ItemGrade.Legendary;
                Logger.Error($"Generated treasure map data for chest {chestTemplate?.Id} did not have a valid category for chests ({chestTemplate?.GroupId})");
                break;
        }
    }

    /// <summary>
    /// Re-rolls the treasure target when the map was created before the treasure chest
    /// spawner data was available (upstream issue #1152: maps generated at (0,0) could
    /// never pass the proximity check and became permanently unusable)
    /// </summary>
    public void EnsureTreasureMapData()
    {
        if (HasValidTreasureTarget)
            return;
        GenerateTreasureMapData();
    }

    public TreasureMap()
    {
        // GenerateTreasureMapData();
    }

    public TreasureMap(ulong id, ItemTemplate template, int count) : base(id, template, count)
    {
        try
        {
            GenerateTreasureMapData();
        }
        catch (Exception e)
        {
            // Never let a generation failure bubble up into ItemManager.Create's
            // fallback, which would degrade the map into a plain Item that can no
            // longer carry coordinates at all (#1152). The target is re-rolled
            // lazily by EnsureTreasureMapData() on first use instead.
            Logger.Warn(e, "TreasureMap: failed to generate target data, will retry on first use");
        }
    }

    public override void ReadDetails(PacketStream stream)
    {
        if (stream.LeftBytes < DetailBytesLength)
            return;
        _ = stream.ReadInt64(); // unknown1
        _mapPositionX = stream.ReadInt64();
        _mapPositionY = stream.ReadInt64();
    }

    public override void WriteDetails(PacketStream stream)
    {
        // MapPositionX = 21504 * 4096; // E 0
        // MapPositionY = 28672 * 4096; // N 0
        stream.Write((long)0);
        stream.Write(_mapPositionX);
        stream.Write(_mapPositionY);
    }

    public JVector GetMapPosition(float z)
    {
        return new JVector((float)MapPositionX, (float)MapPositionY, z);
    }
}
