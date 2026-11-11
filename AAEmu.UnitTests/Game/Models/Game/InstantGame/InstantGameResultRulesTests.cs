using AAEmu.Game.Models.Game.InstantGame;
using AAEmu.Game.Models.Game.InstantGame.Static;

namespace AAEmu.UnitTests.Game.Models.Game.InstantGame;

/// <summary>
/// Tests for the time-up match outcome derivation (issue #1323).
/// Before the fix both corps were always reported as "Lose" with a generic
/// AchievementScore reason when an arena match ended by time-up.
/// </summary>
public class InstantGameResultRulesTests
{
    private static InstantGameTeamResult CreateCorps(uint factionId, params int[] memberScores)
    {
        var result = new InstantGameTeamResult(VictoryState.Lose, factionId);
        foreach (var score in memberScores)
        {
            result.Members.Add(new InstantGameTeamMember { Score = score });
        }
        return result;
    }

    [Test]
    public async Task DeriveTimeOver_HigherScoreWins()
    {
        // Arrange
        var corps1 = CreateCorps(1, 30, 30); // score 60
        var corps2 = CreateCorps(2, 30);     // score 30

        // Act
        var reason = InstantGameResultRules.DeriveTimeOver(corps1, corps2);

        // Assert
        await Assert.That(reason).IsEqualTo(BattlefieldEndingReason.TimeoverCompareScore);
        await Assert.That(corps1.State).IsEqualTo(VictoryState.Win);
        await Assert.That(corps2.State).IsEqualTo(VictoryState.Lose);
    }

    [Test]
    public async Task DeriveTimeOver_ScoreTieFallsBackToKills()
    {
        // Arrange - scores are equal via assists, but corps2 has more kills
        var corps1 = CreateCorps(1, 30, 15); // score 45, 1 kill
        var corps2 = CreateCorps(2, 30, 15); // score 45, 2 kills
        corps1.Members[0].Kills = 1;
        corps2.Members[0].Kills = 1;
        corps2.Members[1].Kills = 1;

        // Act
        var reason = InstantGameResultRules.DeriveTimeOver(corps1, corps2);

        // Assert
        await Assert.That(reason).IsEqualTo(BattlefieldEndingReason.TimeoverCompareKillCount);
        await Assert.That(corps1.State).IsEqualTo(VictoryState.Lose);
        await Assert.That(corps2.State).IsEqualTo(VictoryState.Win);
    }

    [Test]
    public async Task DeriveTimeOver_FullTieIsDraw()
    {
        // Arrange
        var corps1 = CreateCorps(1, 30);
        var corps2 = CreateCorps(2, 30);
        corps1.Members[0].Kills = 1;
        corps2.Members[0].Kills = 1;

        // Act
        var reason = InstantGameResultRules.DeriveTimeOver(corps1, corps2);

        // Assert
        await Assert.That(reason).IsEqualTo(BattlefieldEndingReason.TimeoverDraw);
        await Assert.That(corps1.State).IsEqualTo(VictoryState.Draw);
        await Assert.That(corps2.State).IsEqualTo(VictoryState.Draw);
    }

    [Test]
    public async Task DeriveTimeOver_ZeroZeroIsDraw()
    {
        // Arrange - nobody scored or killed anyone during the whole match
        var corps1 = CreateCorps(1);
        var corps2 = CreateCorps(2);

        // Act
        var reason = InstantGameResultRules.DeriveTimeOver(corps1, corps2);

        // Assert
        await Assert.That(reason).IsEqualTo(BattlefieldEndingReason.TimeoverDraw);
        await Assert.That(corps1.State).IsEqualTo(VictoryState.Draw);
        await Assert.That(corps2.State).IsEqualTo(VictoryState.Draw);
    }

    [Test]
    public async Task DeriveTimeOver_Corps2HigherScoreWins()
    {
        // Arrange
        var corps1 = CreateCorps(1, 15);
        var corps2 = CreateCorps(2, 30, 30);

        // Act
        var reason = InstantGameResultRules.DeriveTimeOver(corps1, corps2);

        // Assert
        await Assert.That(reason).IsEqualTo(BattlefieldEndingReason.TimeoverCompareScore);
        await Assert.That(corps1.State).IsEqualTo(VictoryState.Lose);
        await Assert.That(corps2.State).IsEqualTo(VictoryState.Win);
    }
}
