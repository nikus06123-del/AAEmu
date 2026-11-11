using AAEmu.Commons.IO;
using AAEmu.Commons.Utils;
using AAEmu.Game.Models.Game.AI.v2.Behaviors.Common;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

using NLog;

namespace AAEmu.Game.Core.Managers;

/// <summary>
/// Data-driven NPC reactions to buffs (<c>AAEmu.Game/Data/npc_buff_paths.json</c>).
/// Lets a specific (Npc template, buff) combination make the buffed NPC walk a
/// configured AI path. Primary use case: quest mechanics like issue #1256, where
/// kicking a Lazy Guard awake (quest 3706, buff 2802 "Awakening") should send the
/// guard walking back to Petty Officer Alberton at Royster's Camp.
/// </summary>
public class AiBuffPathManager : Singleton<AiBuffPathManager>, IAiBuffPathManager
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    private const string ConfigFileName = "npc_buff_paths.json";

    private Dictionary<(uint NpcTemplateId, uint BuffId), string> _buffPaths = [];

    /// <summary>Model for a single entry of <c>Data/npc_buff_paths.json</c></summary>
    public sealed class NpcBuffPathEntry
    {
        public string Comment { get; set; } = string.Empty;
        public uint NpcTemplateId { get; set; }
        public uint BuffId { get; set; }
        public string PathFile { get; set; } = string.Empty;
    }

    public void Load()
    {
        _buffPaths = [];
        var pathFile = Path.Combine(FileManager.AppPath, "Data", ConfigFileName);
        var contents = FileManager.GetFileContents(pathFile);
        if (string.IsNullOrWhiteSpace(contents))
        {
            Logger.Warn($"AiBuffPathManager: {pathFile} does not exist or is empty, no buff-triggered NPC paths loaded");
            return;
        }

        _buffPaths = ParseBuffPaths(contents, message => Logger.Warn(message));
        Logger.Info("Loaded {0} buff-triggered NPC path definitions", _buffPaths.Count);
    }

    /// <summary>
    /// Parses <c>npc_buff_paths.json</c> content into a lookup keyed by
    /// (npc template id, buff id). Split out from Load() so it is unit-testable.
    /// </summary>
    public static Dictionary<(uint NpcTemplateId, uint BuffId), string> ParseBuffPaths(string contents, Action<string> onError = null)
    {
        var result = new Dictionary<(uint NpcTemplateId, uint BuffId), string>();
        if (string.IsNullOrWhiteSpace(contents))
            return result;

        if (!JsonHelper.TryDeserializeObject(contents, out List<NpcBuffPathEntry> entries, out _))
        {
            onError?.Invoke("AiBuffPathManager: failed to parse " + ConfigFileName);
            return result;
        }

        foreach (var entry in entries ?? [])
        {
            if (entry is not { NpcTemplateId: > 0, BuffId: > 0 } || string.IsNullOrWhiteSpace(entry.PathFile))
            {
                onError?.Invoke($"AiBuffPathManager: skipping invalid entry (NpcTemplateId: {entry?.NpcTemplateId}, BuffId: {entry?.BuffId}, PathFile: {entry?.PathFile})");
                continue;
            }

            var key = (entry.NpcTemplateId, entry.BuffId);
            if (!result.TryAdd(key, entry.PathFile))
                onError?.Invoke($"AiBuffPathManager: duplicate entry for Npc {key.NpcTemplateId} buff {key.BuffId} in " + ConfigFileName + ", keeping the first one");
        }

        return result;
    }

    public bool TryGetPathFile(uint npcTemplateId, uint buffId, out string pathFile)
    {
        return _buffPaths.TryGetValue((npcTemplateId, buffId), out pathFile!);
    }

    /// <summary>
    /// Called whenever a new buff starts on a unit. If this (npc template, buff)
    /// combination is mapped in <c>Data/npc_buff_paths.json</c>, the NPC immediately
    /// starts walking the configured AI path and returns to idle once it is done.
    /// </summary>
    public void HandleBuffApplied(BaseUnit owner, Buff buff)
    {
        if (owner is not Npc npc || npc.Ai == null || buff?.Template == null)
            return;

        if (!TryGetPathFile(npc.TemplateId, buff.Template.BuffId, out var pathFile))
            return;

        // Do not restart the walk when the buff is (re)applied mid-walk
        if (npc.Ai.GetCurrentBehavior() is FollowPathBehavior)
            return;

        if (!npc.Ai.LoadAiPathPoints(pathFile, false))
        {
            Logger.Warn($"HandleBuffApplied: path file {pathFile} (Npc {npc.TemplateId}, buff {buff.Template.BuffId}) could not be loaded or is empty");
            return;
        }

        Logger.Trace($"HandleBuffApplied: Npc {npc.TemplateId} received buff {buff.Template.BuffId}, walking AI path {pathFile}");
        npc.Ai.GoToFollowPath();
    }
}
