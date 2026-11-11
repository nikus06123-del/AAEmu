using AAEmu.Game.Models.Game.InstantGame.Static;

namespace AAEmu.Game.Models.Game.InstantGame;

/// <summary>
/// Pure rules for deriving the outcome of a finished arena (instant game) match.
/// Kept free of manager dependencies so it is directly unit-testable.
/// </summary>
public static class InstantGameResultRules
{
    /// <summary>
    /// Derives the final VictoryState for both corps and the matching end reason for a
    /// match that ended by time-up instead of reaching the victory score (fix #1323).
    /// The corps with the higher score wins; on a score tie the higher total kill count
    /// wins; a full tie is reported as a draw.
    /// </summary>
    /// <returns>The ending reason that matches the derivation path taken.</returns>
    public static BattlefieldEndingReason DeriveTimeOver(InstantGameTeamResult corps1, InstantGameTeamResult corps2)
    {
        if (corps1.Score != corps2.Score)
        {
            corps1.State = corps1.Score > corps2.Score ? VictoryState.Win : VictoryState.Lose;
            corps2.State = corps1.State == VictoryState.Win ? VictoryState.Lose : VictoryState.Win;
            return BattlefieldEndingReason.TimeoverCompareScore;
        }

        if (corps1.TotalKills != corps2.TotalKills)
        {
            corps1.State = corps1.TotalKills > corps2.TotalKills ? VictoryState.Win : VictoryState.Lose;
            corps2.State = corps1.State == VictoryState.Win ? VictoryState.Lose : VictoryState.Win;
            return BattlefieldEndingReason.TimeoverCompareKillCount;
        }

        corps1.State = VictoryState.Draw;
        corps2.State = VictoryState.Draw;
        return BattlefieldEndingReason.TimeoverDraw;
    }
}
