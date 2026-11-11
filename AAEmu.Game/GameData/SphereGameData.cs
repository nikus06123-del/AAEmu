using System.Numerics;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.GameData.Framework;
using AAEmu.Game.Models.Game.Chat;
using AAEmu.Game.Models.Game.Quests.Acts;
using AAEmu.Game.Models.Game.Quests.Static;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.Spheres;
using AAEmu.Game.Utils.DB;

using NLog;

using Microsoft.Data.Sqlite;
using Jitter2.LinearMath;

namespace AAEmu.Game.GameData;

[GameData]
public class SphereGameData : Singleton<SphereGameData>, IGameDataLoader
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private Dictionary<uint, Spheres> _spheres;
    private Dictionary<uint, SphereQuests> _sphereQuests;
    private Dictionary<uint, SphereSkills> _sphereSkills;
    private Dictionary<uint, SphereSounds> _sphereSounds;
    private Dictionary<uint, SphereDoodadInteracts> _sphereDoodadInteracts;
    private Dictionary<uint, SphereChatBubbles> _sphereChatBubbles;
    private Dictionary<uint, SphereBuffs> _sphereBuffs;
    private Dictionary<uint, SphereBubbles> _sphereBubbles;
    private Dictionary<uint, SphereAcceptQuests> _sphereAcceptQuests;
    private Dictionary<uint, SphereAcceptQuestQuests> _sphereAcceptQuestQuests;

    public void Load(SqliteConnection connection)
    {
        _spheres = [];
        _sphereQuests = [];
        _sphereSkills = [];
        _sphereSounds = [];
        _sphereDoodadInteracts = [];
        _sphereChatBubbles = [];
        _sphereBuffs = [];
        _sphereBubbles = [];
        _sphereAcceptQuests = [];
        _sphereAcceptQuestQuests = [];

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM spheres";
            command.Prepare();
            using (var sqliteReader = command.ExecuteReader())
            using (var reader = new SQLiteWrapperReader(sqliteReader))
            {
                while (reader.Read())
                {
                    var template = new Spheres
                    {
                        Id = reader.GetUInt32("id"),
                        Name = reader.GetString("name"),
                        EnterOrLeave = reader.GetBoolean("enter_or_leave"),
                        SphereDetailId = reader.GetUInt32("sphere_detail_id"),
                        SphereDetailType = reader.GetString("sphere_detail_type"),
                        TriggerConditionId = (AreaSphereTriggerCondition)reader.GetUInt32("trigger_condition_id"),
                        TriggerConditionTime = reader.GetUInt32("trigger_condition_time", 0),
                        TeamMsg = reader.GetString("team_msg"),
                        CategoryId = reader.GetUInt32("category_id"),
                        OrUnitReqs = reader.GetBoolean("or_unit_reqs"),
                        IsPersonalMsg = reader.GetBoolean("is_personal_msg"),
                        MilestoneId = reader.GetUInt32("milestone_id"),
                        NameTr = reader.GetBoolean("name_tr"),
                        TeamMsgTr = reader.GetBoolean("team_msg_tr")
                    };

                    _spheres.Add(template.Id, template);
                }
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM sphere_quests";
            command.Prepare();
            using (var sqliteReader = command.ExecuteReader())
            using (var reader = new SQLiteWrapperReader(sqliteReader))
            {
                while (reader.Read())
                {
                    var template = new SphereQuests
                    {
                        Id = reader.GetUInt32("id"),
                        QuestId = reader.GetUInt32("quest_id"),
                        QuestTriggerId = (QuestTrigger)reader.GetUInt32("quest_trigger_id")
                    };

                    _sphereQuests.Add(template.Id, template);
                }
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM sphere_skills";
            command.Prepare();
            using (var sqliteReader = command.ExecuteReader())
            using (var reader = new SQLiteWrapperReader(sqliteReader))
            {
                while (reader.Read())
                {
                    var template = new SphereSkills
                    {
                        Id = reader.GetUInt32("id"),
                        SkillId = reader.GetUInt32("skill_id"),
                        MaxRate = reader.GetUInt32("max_rate"),
                        MinRate = reader.GetUInt32("min_rate")
                    };

                    _sphereSkills.Add(template.Id, template);
                }
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM sphere_sounds";
            command.Prepare();
            using (var sqliteReader = command.ExecuteReader())
            using (var reader = new SQLiteWrapperReader(sqliteReader))
            {
                while (reader.Read())
                {
                    var template = new SphereSounds
                    {
                        Id = reader.GetUInt32("id"),
                        SoundId = reader.GetUInt32("sound_id"),
                        Broadcast = reader.GetBoolean("broadcast")
                    };

                    _sphereSounds.Add(template.Id, template);
                }
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM sphere_doodad_interacts";
            command.Prepare();
            using (var sqliteReader = command.ExecuteReader())
            using (var reader = new SQLiteWrapperReader(sqliteReader))
            {
                while (reader.Read())
                {
                    var template = new SphereDoodadInteracts
                    {
                        Id = reader.GetUInt32("id"),
                        SkillId = reader.GetUInt32("skill_id"),
                        DoodadFamilyId = reader.GetUInt32("doodad_family_id")
                    };

                    _sphereDoodadInteracts.Add(template.Id, template);
                }
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM sphere_chat_bubbles";
            command.Prepare();
            using (var sqliteReader = command.ExecuteReader())
            using (var reader = new SQLiteWrapperReader(sqliteReader))
            {
                while (reader.Read())
                {
                    var template = new SphereChatBubbles
                    {
                        Id = reader.GetUInt32("id"),
                        SphereBubbleId = reader.GetUInt32("sphere_bubble_id"),
                        IsStart = reader.GetBoolean("is_start"),
                        Speech = reader.GetString("speech"),
                        NpcId = reader.GetUInt32("npc_id", 0),
                        NpcSpawnerId = reader.GetUInt32("npc_spawner_id", 0),
                        NextBubble = reader.GetUInt32("next_bubble", 0),
                        SoundId = reader.GetUInt32("sound_id", 0),
                        Angle = reader.GetUInt32("angle", 0),
                        ChatBubbleKindId = (ChatBubbleKind)reader.GetUInt32("chat_bubble_kind_id"),
                        Facial = reader.GetString("facial", ""),
                        CameraId = reader.GetUInt32("camera_id", 0),
                        ChangeSpeakerName = reader.GetString("change_speaker_name", "")
                    };

                    _sphereChatBubbles.Add(template.Id, template);
                }
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM sphere_buffs";
            command.Prepare();
            using (var sqliteReader = command.ExecuteReader())
            using (var reader = new SQLiteWrapperReader(sqliteReader))
            {
                while (reader.Read())
                {
                    var template = new SphereBuffs
                    {
                        Id = reader.GetUInt32("id"),
                        BuffId = reader.GetUInt32("buff_id")

                    };

                    _sphereBuffs.Add(template.Id, template);
                }
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM sphere_bubbles";
            command.Prepare();
            using (var sqliteReader = command.ExecuteReader())
            using (var reader = new SQLiteWrapperReader(sqliteReader))
            {
                while (reader.Read())
                {
                    var template = new SphereBubbles
                    {
                        Id = reader.GetUInt32("id")
                    };

                    _sphereBubbles.Add(template.Id, template);
                }
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM sphere_accept_quests";
            command.Prepare();
            using (var sqliteReader = command.ExecuteReader())
            using (var reader = new SQLiteWrapperReader(sqliteReader))
            {
                while (reader.Read())
                {
                    var template = new SphereAcceptQuests
                    {
                        Id = reader.GetUInt32("id")
                    };

                    _sphereAcceptQuests.Add(template.Id, template);
                }
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM sphere_accept_quest_quests";
            command.Prepare();
            using (var sqliteReader = command.ExecuteReader())
            using (var reader = new SQLiteWrapperReader(sqliteReader))
            {
                while (reader.Read())
                {
                    var template = new SphereAcceptQuestQuests
                    {
                        Id = reader.GetUInt32("id"),
                        SphereAcceptQuestId = reader.GetUInt32("sphere_accept_quest_id"),
                        QuestId = reader.GetUInt32("quest_id")
                    };

                    _sphereAcceptQuestQuests.Add(template.Id, template);
                }
            }
        }
    }

    public static List<string> GetQuestSphere(uint questId)
    {
        var sphereIds = new List<string>();

        var questTemplate = QuestManager.Instance.GetTemplate(questId);
        if (questTemplate == null)
            return null;
        foreach (var step in Enum.GetValues<QuestComponentKind>())
        {
            var components = questTemplate.GetComponents(step);
            if (components.Length == 0)
                continue;

            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                var component = components[componentIndex];
                var acts = QuestManager.Instance.GetActsInComponent(component.Id);

                if (acts.Count > 0)
                {
                    for (var actIndex = 0; actIndex < components.Length; actIndex++)
                    {
                        var act = acts[actIndex];
                        if (act.DetailType == "QuestActObjSphere")
                        {
                            if (acts[actIndex] is QuestActObjSphere actTemplate)
                                sphereIds.Add(actTemplate.SphereId.ToString());
                        }
                    }
                }
            }
        }

        return sphereIds;
    }

    public void PostLoad()
    {
        // Upstream issue #1329: surface broken sphere -> sphere_quest -> quest links at
        // startup instead of failing silently inside skill-use checks later. A known
        // example is sphere 1448 (quest 3889 "Closing the Gate"), whose sphere_quests
        // row pointed at unrelated quest 4280; data fixes for these live in
        // SQL/patches/compact.
        var brokenLinks = 0;
        foreach (var sphere in _spheres.Values)
        {
            if (sphere.SphereDetailType != "SphereQuest")
                continue;

            if (!_sphereQuests.TryGetValue(sphere.SphereDetailId, out var sphereQuest))
            {
                brokenLinks++;
                Logger.Warn("Sphere {0} references missing sphere_quest {1}", sphere.Id, sphere.SphereDetailId);
                continue;
            }

            if (QuestManager.Instance.GetTemplate(sphereQuest.QuestId) == null)
            {
                brokenLinks++;
                Logger.Warn("Sphere {0} -> sphere_quest {1} points to non-existent quest {2}, area checks for it will always fail", sphere.Id, sphere.SphereDetailId, sphereQuest.QuestId);
            }
        }

        if (brokenLinks > 0)
            Logger.Warn("Found {0} broken sphere/sphere_quest/quest links, see warnings above", brokenLinks);
    }

    public uint GetSphereIdFromQuest(uint questId)
    {
        var validQuestSpheres = _sphereQuests.Values.Where(x => x.QuestId == questId);

        foreach (var validQuestSphere in validQuestSpheres)
        {
            var res = _spheres.Values.FirstOrDefault(x => x.SphereDetailId == validQuestSphere.Id && x.SphereDetailType == "SphereQuest");
            if (res != null)
                return res.Id;
        }
        return 0;
    }

    public Spheres GetSphere(uint sphereId)
    {
        return _spheres.GetValueOrDefault(sphereId);
    }

    /// <summary>
    /// Checks if a position is inside the given SphereId
    /// </summary>
    /// <param name="sphereId">Sphere Id as defined in a Quest Act</param>
    /// <param name="value2">Unknown, always one except for skill 13305 (plant unidentified tree)</param>
    /// <param name="worldPosition"></param>
    /// <param name="requiredComponentId"></param>
    /// <returns>SphereQuest that was hit, null if none found</returns>
    public SphereQuest IsInsideAreaSphere(uint sphereId, uint value2, JVector worldPosition, uint requiredComponentId = 0)
    {
        if (!_spheres.TryGetValue(sphereId, out var dbSphere))
            return null;

        if (dbSphere.SphereDetailType != "SphereQuest")
            return null;

        if (!_sphereQuests.TryGetValue(dbSphere.SphereDetailId, out var dbSphereQuest))
            return null;

        var pakDataSpheres = SphereQuestManager.GetSpheresForQuest(dbSphereQuest.QuestId);

        // Upstream issue #1329: when the quest component is known, prefer the physical
        // spheres keyed by component id. The sphere_quests.quest_id link in the client
        // data is sometimes wrong (e.g. sphere 1448 of quest 3889 pointing at unrelated
        // quest 4280), which made the skill-use check fail even while standing inside
        // the correct sphere. Component ids are unique per quest component, so this
        // fallback cannot leak spheres belonging to other quests.
        if (requiredComponentId > 0)
        {
            var spheresByComponent = SphereQuestManager.GetQuestSpheresByComponent(requiredComponentId);
            if (spheresByComponent is { Count: > 0 })
                pakDataSpheres = spheresByComponent;
        }

        var requiredComponent = requiredComponentId > 0
            ? QuestManager.Instance.GetComponent(requiredComponentId)
            : null;

        foreach (var pakDataSphere in pakDataSpheres)
        {
            var componentMatches = requiredComponentId == 0 || pakDataSphere.ComponentId == requiredComponentId;
            if (!componentMatches &&
                dbSphere.TriggerConditionId == AreaSphereTriggerCondition.TriggerEveryNTimeAfter &&
                requiredComponent?.ParentQuestTemplate?.Id == dbSphereQuest.QuestId)
                componentMatches = true;

            if (pakDataSphere.Contains(worldPosition) && componentMatches)
                return pakDataSphere;
        }

        return null;
    }

}
