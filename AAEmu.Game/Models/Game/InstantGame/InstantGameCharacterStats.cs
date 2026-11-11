namespace AAEmu.Game.Models.Game.InstantGame;

/// <summary>
/// Persistent arena (instant game) rating and match statistics for one character
/// on one battlefield. Ratings use a standard Elo system starting at 1000.
/// </summary>
public class InstantGameCharacterStats
{
    public const int DefaultRating = 1000;

    public uint CharacterId { get; set; }
    public uint BattlefieldId { get; set; }
    public int Rating { get; set; } = DefaultRating;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Matches { get; set; }
    public DateTime LastMatchAt { get; set; }

    /// <summary>Runtime dirty flag for the periodic DB save</summary>
    public bool IsDirty { get; set; }
}

/// <summary>
/// Standard Elo rating math, kept pure and dependency-free so it is unit-testable.
/// </summary>
public static class EloRating
{
    public const int DefaultKFactor = 32;

    /// <summary>
    /// Expected score for a player with <paramref name="ownRating"/> against an
    /// opponent with <paramref name="opposingRating"/> (1 = certain win, 0.5 = even).
    /// </summary>
    public static double ExpectedScore(int ownRating, int opposingRating)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (opposingRating - ownRating) / 400.0));
    }

    /// <summary>
    /// Elo rating delta for one participant of a finished match.
    /// </summary>
    /// <param name="ownRating">This participant's (team average) rating</param>
    /// <param name="opposingRating">The opposing team's average rating</param>
    /// <param name="actualScore">1.0 for a win, 0.5 for a draw, 0.0 for a loss</param>
    /// <param name="kFactor">Elo K-factor</param>
    /// <returns>The (rounded) rating delta, may be negative</returns>
    public static int ComputeDelta(int ownRating, int opposingRating, double actualScore, int kFactor = DefaultKFactor)
    {
        var expected = ExpectedScore(ownRating, opposingRating);
        return (int)Math.Round(kFactor * (actualScore - expected));
    }
}
