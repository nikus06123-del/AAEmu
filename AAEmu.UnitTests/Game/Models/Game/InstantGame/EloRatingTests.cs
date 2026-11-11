using AAEmu.Game.Models.Game.InstantGame;

namespace AAEmu.UnitTests.Game.Models.Game.InstantGame;

/// <summary>
/// Tests for the pure Elo rating math used by the persistent arena rating system (#1323).
/// </summary>
public class EloRatingTests
{
    [Test]
    public async Task ExpectedScore_EqualRatings_IsHalf()
    {
        var expected = EloRating.ExpectedScore(1000, 1000);
        await Assert.That(expected).IsEqualTo(0.5);
    }

    [Test]
    public async Task ExpectedScore_HigherRating_IsAboveHalf()
    {
        var expected = EloRating.ExpectedScore(1200, 1000);
        await Assert.That(expected).IsGreaterThan(0.5);
        await Assert.That(expected).IsLessThan(1.0);
    }

    [Test]
    public async Task ComputeDelta_BeatEqualOpponent_GainsHalfK()
    {
        // Winning against an even opponent: K * (1 - 0.5) = 16
        var delta = EloRating.ComputeDelta(1000, 1000, 1.0);
        await Assert.That(delta).IsEqualTo(16);
    }

    [Test]
    public async Task ComputeDelta_LoseToEqualOpponent_LosesHalfK()
    {
        var delta = EloRating.ComputeDelta(1000, 1000, 0.0);
        await Assert.That(delta).IsEqualTo(-16);
    }

    [Test]
    public async Task ComputeDelta_DrawAgainstEqualOpponent_NoChange()
    {
        var delta = EloRating.ComputeDelta(1000, 1000, 0.5);
        await Assert.That(delta).IsEqualTo(0);
    }

    [Test]
    public async Task ComputeDelta_BeatStrongerOpponent_GainsMore()
    {
        var beatingFavorite = EloRating.ComputeDelta(1000, 1300, 1.0);
        var beatingUnderdog = EloRating.ComputeDelta(1300, 1000, 1.0);
        await Assert.That(beatingFavorite).IsGreaterThan(beatingUnderdog);
        await Assert.That(beatingFavorite).IsGreaterThan(0);
        await Assert.That(beatingUnderdog).IsGreaterThan(0);
    }

    [Test]
    public async Task ComputeDelta_SymmetricZeroSum()
    {
        // For two players with equal ratings, win + loss deltas must cancel out
        var winDelta = EloRating.ComputeDelta(1000, 1000, 1.0);
        var lossDelta = EloRating.ComputeDelta(1000, 1000, 0.0);
        await Assert.That(winDelta + lossDelta).IsEqualTo(0);
    }
}
