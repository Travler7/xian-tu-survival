#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using RGame.RoguelikeKit;

public class EliteRoleAcceptanceTests
{
    [Test]
    public void EliteAndFinalBossRolesAreExplicit()
    {
        var sets = AssetDatabase.FindAssets("t:SpawnSet")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<SpawnSet>)
            .ToList();
        var finals = sets.Where(s => s.Role == SpawnSetRole.FinalBoss).ToList();
        Assert.That(finals.Count, Is.EqualTo(2));
        Assert.That(finals.All(s => s.Entries.Any(e => e != null && e.PoolKey == "Crow")), Is.True);
        foreach (var elite in sets.Where(s => s.Role == SpawnSetRole.Elite))
        {
            Assert.That(elite.Pattern, Is.Not.EqualTo(SpawnPatternType.Random), elite.name);
            Assert.That(elite.StartTime, Is.LessThan(660), elite.name);
        }
    }
}
#endif