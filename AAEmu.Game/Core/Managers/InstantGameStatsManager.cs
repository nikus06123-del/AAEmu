using AAEmu.Commons.Utils;
using AAEmu.Commons.Utils.DB;
using AAEmu.Game.Models.Game.InstantGame;
using AAEmu.Game.Models.Game.InstantGame.Static;

using MySql.Data.MySqlClient;

using NLog;

namespace AAEmu.Game.Core.Managers;

/// <summary>
/// Persists per-character arena (instant game) rating and match statistics in the
/// <c>character_instant_game_stats</c> table. Ratings are updated with a standard
/// Elo system where each corps' team rating is the average of its members' ratings
/// (issue #1323 - "ratings should be tied to arenas").
/// </summary>
public class InstantGameStatsManager : Singleton<InstantGameStatsManager>, IInstantGameStatsManager
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private Dictionary<(uint CharacterId, uint BattlefieldId), InstantGameCharacterStats> _stats = [];

    public void Load()
    {
        _stats = [];
        using (var connection = MySQL.CreateConnection())
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM character_instant_game_stats";
            command.Prepare();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var entry = new InstantGameCharacterStats
                {
                    CharacterId = reader.GetUInt32("character_id"),
                    BattlefieldId = reader.GetUInt32("battlefield_id"),
                    Rating = reader.GetInt32("rating"),
                    Wins = reader.GetInt32("wins"),
                    Losses = reader.GetInt32("losses"),
                    Draws = reader.GetInt32("draws"),
                    Kills = reader.GetInt32("kills"),
                    Deaths = reader.GetInt32("deaths"),
                    Assists = reader.GetInt32("assists"),
                    Matches = reader.GetInt32("matches"),
                    LastMatchAt = reader.GetDateTime("last_match_at"),
                    IsDirty = false
                };
                _stats[(entry.CharacterId, entry.BattlefieldId)] = entry;
            }
        }
        Logger.Info("Loaded arena stats for {0} (character, battlefield) combinations", _stats.Count);
    }

    public InstantGameCharacterStats GetOrCreate(uint characterId, uint battlefieldId)
    {
        if (_stats.TryGetValue((characterId, battlefieldId), out var entry))
            return entry;

        entry = new InstantGameCharacterStats
        {
            CharacterId = characterId,
            BattlefieldId = battlefieldId,
            LastMatchAt = DateTime.UtcNow,
            IsDirty = true
        };
        _stats[(characterId, battlefieldId)] = entry;
        return entry;
    }

    public int GetRating(uint characterId, uint battlefieldId)
    {
        return _stats.TryGetValue((characterId, battlefieldId), out var entry) ? entry.Rating : InstantGameCharacterStats.DefaultRating;
    }

    /// <summary>
    /// Records the outcome of one finished arena match: updates each participant's
    /// win/loss/draw counters, kills/deaths/assists and Elo rating (team average based).
    /// </summary>
    public void RecordMatchResult(uint battlefieldId, VictoryState corps1State, IReadOnlyCollection<InstantGameTeamMember> corps1Members, VictoryState corps2State, IReadOnlyCollection<InstantGameTeamMember> corps2Members)
    {
        if (corps1Members.Count <= 0 || corps2Members.Count <= 0)
            return;

        var corps1Entries = corps1Members.Where(m => m.Character != null).Select(m => (member: m, stats: GetOrCreate(m.Character.Id, battlefieldId))).ToList();
        var corps2Entries = corps2Members.Where(m => m.Character != null).Select(m => (member: m, stats: GetOrCreate(m.Character.Id, battlefieldId))).ToList();
        if (corps1Entries.Count <= 0 || corps2Entries.Count <= 0)
            return;

        var corps1Rating = (int)Math.Round(corps1Entries.Average(e => e.stats.Rating));
        var corps2Rating = (int)Math.Round(corps2Entries.Average(e => e.stats.Rating));

        var (corps1Score, corps2Score) = corps1State switch
        {
            VictoryState.Win when corps2State == VictoryState.Lose => (1.0, 0.0),
            VictoryState.Lose when corps2State == VictoryState.Win => (0.0, 1.0),
            _ => (0.5, 0.5)
        };

        ApplyCorpsResult(battlefieldId, corps1Entries, corps1State, corps1Score, corps1Rating, corps2Rating);
        ApplyCorpsResult(battlefieldId, corps2Entries, corps2State, corps2Score, corps2Rating, corps1Rating);
    }

    private static void ApplyCorpsResult(uint battlefieldId, List<(InstantGameTeamMember member, InstantGameCharacterStats stats)> entries, VictoryState state, double actualScore, int ownRating, int opposingRating)
    {
        var delta = EloRating.ComputeDelta(ownRating, opposingRating, actualScore);
        foreach (var (member, stats) in entries)
        {
            stats.Matches++;
            switch (state)
            {
                case VictoryState.Win:
                    stats.Wins++;
                    break;
                case VictoryState.Lose:
                    stats.Losses++;
                    break;
                default:
                    stats.Draws++;
                    break;
            }

            stats.Kills += member.Kills;
            stats.Deaths += member.Deaths;
            stats.Assists += member.Assists;
            stats.Rating = Math.Max(0, stats.Rating + delta);
            stats.LastMatchAt = DateTime.UtcNow;
            stats.IsDirty = true;

            Logger.Debug($"RecordMatchResult: character {stats.CharacterId} on battlefield {battlefieldId} finished a match ({state}), rating {stats.Rating - delta} -> {stats.Rating} ({delta:+0;-0})");
        }
    }

    public (int updatedCount, int deletedCount) Save(MySqlConnection connection, MySqlTransaction transaction)
    {
        var updatedCount = 0;
        foreach (var entry in _stats.Values)
        {
            if (!entry.IsDirty)
                continue;

            using var command = connection.CreateCommand();
            command.Connection = connection;
            command.Transaction = transaction;
            command.CommandText = "REPLACE INTO character_instant_game_stats(" +
                "`character_id`,`battlefield_id`,`rating`,`wins`,`losses`,`draws`," +
                "`kills`,`deaths`,`assists`,`matches`,`last_match_at`" +
                ") VALUES (" +
                "@characterId, @battlefieldId, @rating, @wins, @losses, @draws, " +
                "@kills, @deaths, @assists, @matches, @lastMatchAt)";
            command.Parameters.AddWithValue("@characterId", entry.CharacterId);
            command.Parameters.AddWithValue("@battlefieldId", entry.BattlefieldId);
            command.Parameters.AddWithValue("@rating", entry.Rating);
            command.Parameters.AddWithValue("@wins", entry.Wins);
            command.Parameters.AddWithValue("@losses", entry.Losses);
            command.Parameters.AddWithValue("@draws", entry.Draws);
            command.Parameters.AddWithValue("@kills", entry.Kills);
            command.Parameters.AddWithValue("@deaths", entry.Deaths);
            command.Parameters.AddWithValue("@assists", entry.Assists);
            command.Parameters.AddWithValue("@matches", entry.Matches);
            command.Parameters.AddWithValue("@lastMatchAt", entry.LastMatchAt);
            command.Prepare();
            command.ExecuteNonQuery();
            entry.IsDirty = false;
            updatedCount++;
        }

        return (updatedCount, 0);
    }
}
