using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.InstantGame.Static;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World.Transform;

namespace AAEmu.Game.Models.Game.InstantGame;

public partial class InstantGame
{
    /// <summary>
    /// Adds score to the given corps, broadcasts the update, and ends the game when the victory score is reached.
    /// Returns true when this call ended the match.
    /// </summary>
    private bool AddScore(InstantCorps corps, InstantGameTeamMember scorer, int score)
    {
        var result = corps == InstantCorps.Corps1 ? _corps1Result : _corps2Result;

        scorer.Score += score;

        // Fix #1323: broadcast the actual corps that scored instead of always Corps1
        BroadcastPacket(new SCInstantGameAddPointPacket(_zoneInstanceId, corps, score, result.Score, 0, scorer.Character.Name));

        if (result.Score >= _battlefield.RuleSet.VictoryScore)
        {
            result.State = VictoryState.Win;
            _endGameTokenSource.Cancel();
            // Fix #1323: EndGame() is an async method; calling .Start() on the hot-path task
            // throws InvalidOperationException ("Start may not be called on a promise-style task"),
            // silently killing the end-of-match sequence. Fire-and-forget instead.
            _ = EndGame();
            return true; // Match is over
        }

        return false; // Match continues
    }

    public void OnKill(object sender, OnKillArgs args)
    {
        if (args.Killer is not Character killer || args.Victim is not Character victim)
            return;

        // _log.Debug("{0} killed {1}", args.Killer.Name, args.Victim.Name, character.Name);

        var memberKiller = _members[killer];
        memberKiller.Kills++;
        memberKiller.Killstreak++;

        var memberVictim = _members[victim];
        memberVictim.Deaths++;
        memberVictim.Killstreak = 0;

        BroadcastPacket(new SCInstantGameKillPacket(_zoneInstanceId, killer, victim, _characterCorps[killer], _characterCorps[victim], (sbyte)memberKiller.Killstreak, memberKiller.Corps.TotalKills, memberVictim.Corps.TotalKills));
        killer.SendPacket(new SCInstantGameKillstreakPacket(_zoneInstanceId, (sbyte)memberKiller.Killstreak, 0, true));
        // Kill score reward is configurable via World.json (GladiatorKillScore)
        var isMatchOver = AddScore(_characterCorps[killer], memberKiller, AppConfiguration.Instance.World.GladiatorKillScore);

        // Fix #1323: when the final kill ended the match, stop here and reset both players onto
        // their platforms so they stand alive for the scoreboard. Without this, the 6 second
        // respawn timer below would still run after the match ended, leaving a dead character
        // floating around when the results screen is already locked on screen.
        if (isMatchOver)
        {
            var endCorps = _characterCorps[victim];
            var endVictimSpawn = endCorps == InstantCorps.Corps1 ? _battlefield.Spawns.Corps1Spawn : _battlefield.Spawns.Corps2Spawn;
            var endKillerSpawn = endCorps == InstantCorps.Corps1 ? _battlefield.Spawns.Corps2Spawn : _battlefield.Spawns.Corps1Spawn;

            Task.Run(async () =>
            {
                await Task.Delay(3000);

                // Reset victim to their platform and resurrect
                victim.Transform.ApplyWorldSpawnPosition(
                    new WorldSpawnPosition
                    {
                        ZoneId = _battlefield.ZoneKey,
                        X = endVictimSpawn.X,
                        Y = endVictimSpawn.Y,
                        Z = endVictimSpawn.Z
                    },
                    _world.Id
                );
                victim.BroadcastPacket(new SCCharacterResurrectedPacket(victim.ObjId, endVictimSpawn.X, endVictimSpawn.Y, endVictimSpawn.Z, endVictimSpawn.RotationZ), true);
                victim.Hp = victim.MaxHp;
                victim.Mp = victim.MaxMp;
                victim.BroadcastPacket(new SCUnitPointsPacket(victim.ObjId, victim.Hp, victim.Mp), true);
                victim.Buffs.RemoveAllEffects();
                victim.ResetAllSkillCooldowns(false);

                // Reset killer to their platform
                if (_battlefield.Id == (uint)InstantGameType.Gladiator)
                {
                    killer.Transform.ApplyWorldSpawnPosition(
                        new WorldSpawnPosition
                        {
                            ZoneId = _battlefield.ZoneKey,
                            X = endKillerSpawn.X,
                            Y = endKillerSpawn.Y,
                            Z = endKillerSpawn.Z
                        },
                        _world.Id
                    );
                    killer.SendPacket(new SCTeleportUnitPacket(0, 0, endKillerSpawn.X, endKillerSpawn.Y, endKillerSpawn.Z, endKillerSpawn.RotationZ));
                    killer.Hp = killer.MaxHp;
                    killer.Mp = killer.MaxMp;
                    killer.BroadcastPacket(new SCUnitPointsPacket(killer.ObjId, killer.Hp, killer.Mp), true);
                    killer.Buffs.RemoveAllEffects();
                    killer.ResetAllSkillCooldowns(false);
                }
            });
            return;
        }

        var corps = _characterCorps[victim];
        var spawn = corps == InstantCorps.Corps1 ? _battlefield.Spawns.Corps1Spawn : _battlefield.Spawns.Corps2Spawn;

        Task.Run(async () =>
        {
            await Task.Delay(6000);
            // Sync the victim's server position to the spawn point before resurrecting,
            // this prevents the client from applying fall damage on arrival
            victim.Transform.ApplyWorldSpawnPosition(
                new WorldSpawnPosition
                {
                    ZoneId = _battlefield.ZoneKey,
                    X = spawn.X,
                    Y = spawn.Y,
                    Z = spawn.Z
                },
                _world.Id
            );

            // Reset victim for the next round
            victim.BroadcastPacket(new SCCharacterResurrectedPacket(victim.ObjId, spawn.X, spawn.Y, spawn.Z, spawn.RotationZ), true); // Ressurrect
            victim.ResetAllSkillCooldowns(false); // Skill Cooldown Reset
            victim.Buffs.RemoveAllEffects(); // Buff Reset

            victim.Hp = victim.MaxHp; // Full HP
            victim.Mp = victim.MaxMp; // Full MP
            victim.BroadcastPacket(new SCUnitPointsPacket(victim.ObjId, victim.Hp, victim.Mp), true); // Reset HP and MP

            spawn = corps == InstantCorps.Corps1 ? _battlefield.Spawns.Corps2Spawn : _battlefield.Spawns.Corps1Spawn;

            // Reset killer for the next round
            if (_battlefield.Id == (uint)InstantGameType.Gladiator)
            {
                // Sync the killer's server position before teleporting to prevent fall damage
                killer.Transform.ApplyWorldSpawnPosition(
                    new WorldSpawnPosition
                    {
                        ZoneId = _battlefield.ZoneKey,
                        X = spawn.X,
                        Y = spawn.Y,
                        Z = spawn.Z
                    },
                    _world.Id
                );

                if (killer.Hp == 0)
                {
                    // Killer somehow died
                    killer.BroadcastPacket(new SCCharacterResurrectedPacket(killer.ObjId, spawn.X, spawn.Y, spawn.Z, spawn.RotationZ), true);
                }
                else
                    killer.SendPacket(new SCTeleportUnitPacket(0, 0, spawn.X, spawn.Y, spawn.Z, spawn.RotationZ));

                killer.Hp = killer.MaxHp;
                killer.Mp = killer.MaxMp;
                killer.BroadcastPacket(new SCUnitPointsPacket(killer.ObjId, killer.Hp, killer.Mp), true);
                killer.Buffs.RemoveAllEffects();
                killer.ResetAllSkillCooldowns(false);
            }

            // Heal any landing fall damage shortly after teleporting to the platform
            await Task.Delay(1500);
            victim.Hp = victim.MaxHp;
            victim.Mp = victim.MaxMp;
            victim.BroadcastPacket(new SCUnitPointsPacket(victim.ObjId, victim.Hp, victim.Mp), true);

            if (_battlefield.Id == (uint)InstantGameType.Gladiator)
            {
                killer.Hp = killer.MaxHp;
                killer.Mp = killer.MaxMp;
                killer.BroadcastPacket(new SCUnitPointsPacket(killer.ObjId, killer.Hp, killer.Mp), true);
            }
        });
    }
}
