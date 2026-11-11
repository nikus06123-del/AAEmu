using AAEmu.Game.Models.Game.Crime;

namespace AAEmu.Game.Models.Game.Char;

public partial class Character
{
    /// <summary>
    /// Mirrors the characters.deleted database flag. Fix: previously the character save
    /// used REPLACE INTO without this column, silently resetting any pending deletion
    /// mark made by the character deletion timer while the row was being rewritten.
    /// </summary>
    public bool Deleted { get; set; }
    public uint HostileFactionKills { get; set; }
    public uint HonorGainedInCombat { get; set; }
    public int ConsumedLaborPower { get; set; }
    public short DeadCount { get; set; }

    // Justice
    public int ArrestCount { get; set; }
    public int AcceptGuiltyCount { get; set; }
    public int AcceptTrialCount { get; set; }
    public int NotGuiltyCount { get; set; }
    public int GuiltyCount { get; set; }
    public int EvidenceReportedCount { get; set; }
    public int BotReportedCount { get; set; }
    public int ReportedAsBotCount { get; set; }
    /// <summary>When this is set at login, it means a trial was skipped and should be considered guilty and thrown into jail</summary>
    public int OfflineGuiltyTime { get; set; }
    public CourtRoomRegion OfflineGuiltyRegion { get; set; }
}
