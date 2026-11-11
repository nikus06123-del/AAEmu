using AAEmu.Game.Models.Game.InstantGame;
using AAEmu.Game.Models.Game.InstantGame.Static;

using MySql.Data.MySqlClient;

namespace AAEmu.Game.Core.Managers;

public interface IInstantGameStatsManager : ILoadable
{
    InstantGameCharacterStats GetOrCreate(uint characterId, uint battlefieldId);
    int GetRating(uint characterId, uint battlefieldId);
    void RecordMatchResult(uint battlefieldId, VictoryState corps1State, IReadOnlyCollection<InstantGameTeamMember> corps1Members, VictoryState corps2State, IReadOnlyCollection<InstantGameTeamMember> corps2Members);
    (int updatedCount, int deletedCount) Save(MySqlConnection connection, MySqlTransaction transaction);
}
