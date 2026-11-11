using AAEmu.Game.Core.Managers;

namespace AAEmu.UnitTests.Game.Core.Managers;

/// <summary>
/// Tests for the npc_buff_paths.json parsing used by AiBuffPathManager (issue #1256).
/// The mapping drives NPC reactions to buffs, e.g. quest 3706 where kicking a Lazy
/// Guard awake (buff 2802) sends it walking back to Royster's Camp.
/// </summary>
public class AiBuffPathManagerTests
{
    [Test]
    public async Task ParseBuffPaths_ValidEntry_IsMappedByNpcAndBuff()
    {
        // Arrange
        const string json = """
            [
                {
                    "Comment": "Quest 3706 - Lazy Guard walks back to Royster's Camp",
                    "NpcTemplateId": 707,
                    "BuffId": 2802,
                    "PathFile": "q3706_roysters_camp"
                }
            ]
            """;

        // Act
        var result = AiBuffPathManager.ParseBuffPaths(json);

        // Assert
        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result.TryGetValue((707u, 2802u), out var pathFile)).IsTrue();
        await Assert.That(pathFile).IsEqualTo("q3706_roysters_camp");
    }

    [Test]
    public async Task ParseBuffPaths_InvalidEntries_AreSkippedAndReported()
    {
        // Arrange - zero ids and missing path file must be rejected
        const string json = """
            [
                { "NpcTemplateId": 0, "BuffId": 2802, "PathFile": "a" },
                { "NpcTemplateId": 707, "BuffId": 0, "PathFile": "b" },
                { "NpcTemplateId": 707, "BuffId": 2802, "PathFile": "" },
                { "NpcTemplateId": 707, "BuffId": 2802, "PathFile": "q3706_roysters_camp" }
            ]
            """;
        var errors = new List<string>();

        // Act
        var result = AiBuffPathManager.ParseBuffPaths(json, errors.Add);

        // Assert
        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(errors).Count().IsEqualTo(3);
    }

    [Test]
    public async Task ParseBuffPaths_DuplicateKeys_KeepFirstEntry()
    {
        // Arrange
        const string json = """
            [
                { "NpcTemplateId": 707, "BuffId": 2802, "PathFile": "first_path" },
                { "NpcTemplateId": 707, "BuffId": 2802, "PathFile": "second_path" }
            ]
            """;
        var errors = new List<string>();

        // Act
        var result = AiBuffPathManager.ParseBuffPaths(json, errors.Add);

        // Assert
        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[(707u, 2802u)]).IsEqualTo("first_path");
        await Assert.That(errors).Count().IsEqualTo(1);
    }

    [Test]
    public async Task ParseBuffPaths_EmptyOrBrokenContent_ReturnsEmpty()
    {
        // Act
        var empty = AiBuffPathManager.ParseBuffPaths("   ");
        var broken = AiBuffPathManager.ParseBuffPaths("{ not a valid json ]", _ => { });

        // Assert
        await Assert.That(empty).IsEmpty();
        await Assert.That(broken).IsEmpty();
    }

    [Test]
    public async Task ParseBuffPaths_UnknownNpcAndBuffCombination_NotMapped()
    {
        // Arrange
        const string json = """[ { "NpcTemplateId": 707, "BuffId": 2802, "PathFile": "q3706_roysters_camp" } ]""";
        var result = AiBuffPathManager.ParseBuffPaths(json);

        // Act
        var found = result.ContainsKey((1234u, 9999u));

        // Assert
        await Assert.That(found).IsFalse();
    }
}
