using AAEmu.Game.Models.Game.AI.v2.Behaviors.Common;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Core.Managers;

public interface IAiBuffPathManager : ILoadable
{
    bool TryGetPathFile(uint npcTemplateId, uint buffId, out string pathFile);
    void HandleBuffApplied(BaseUnit owner, Buff buff);
}
