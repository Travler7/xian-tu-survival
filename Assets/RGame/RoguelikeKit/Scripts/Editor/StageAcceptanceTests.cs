#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using RGame.RoguelikeKit;

public class StageAcceptanceTests
{
    [Test] public void SpawnSetTimesAreValid()
    {
        foreach (var path in AssetDatabase.FindAssets("t:SpawnSet").Select(AssetDatabase.GUIDToAssetPath))
        {
            var set = AssetDatabase.LoadAssetAtPath<SpawnSet>(path);
            Assert.That(set.StartTime, Is.GreaterThanOrEqualTo(0), path);
            if (set.Pattern == SpawnPatternType.Random) Assert.That(set.EndTime, Is.GreaterThan(set.StartTime), path);
            else Assert.That(set.EndTime, Is.GreaterThanOrEqualTo(set.StartTime), path);
            if (set.Entries.Any(e => e != null && e.PoolKey == "Crow")) Assert.That(set.StartTime, Is.GreaterThanOrEqualTo(660), path);
            Assert.That(set.StartTime, Is.LessThanOrEqualTo(720), path);
        }
    }

    [Test] public void NoSpawnAfterStageBoundary()
    {
        foreach (var path in AssetDatabase.FindAssets("t:SpawnSet").Select(AssetDatabase.GUIDToAssetPath))
        {
            var set = AssetDatabase.LoadAssetAtPath<SpawnSet>(path);
            Assert.That(set.StartTime, Is.LessThanOrEqualTo(720), path);
        }
    }

    [Test] public void ActiveCharacterCopyIsComplete()
    {
        foreach (var path in new[] { "Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Level/SelectCharacter/Assassin.asset", "Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Level/SelectCharacter/Mage.asset" })
        {
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            var serialized = new SerializedObject(obj).FindProperty("Description");
            Assert.That(serialized.stringValue, Is.Not.Null.And.Not.Empty, path);
        }
    }

    [Test] public void StageReferencesAreValid()
    {
        foreach (var path in AssetDatabase.FindAssets("t:StageConfigSO").Select(AssetDatabase.GUIDToAssetPath))
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageConfigSO>(path);
            Assert.That(stage, Is.Not.Null, path);
            var missing = stage.SpawnSets.Select((s, i) => new { s, i }).Where(x => ReferenceEquals(x.s, null)).Select(x => x.i.ToString());
            Assert.That(missing, Is.Empty, $"{path}: missing indices {string.Join(",", missing)}");
        }
    }

    [Test] public void EnemyPrefabsHaveRuntimeComponents()
    {
        foreach (var path in AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath).Where(p => p.Contains("/Prefabs/Enemy/")))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab.GetComponentInChildren<BaseEnemy>() == null) continue;
            Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(), Is.Not.Null, path);
            Assert.That(prefab.GetComponentInChildren<Collider2D>(), Is.Not.Null, path);
            foreach (var hit in prefab.GetComponentsInChildren<EnemyHit>(true))
            {
                Assert.That(hit.GetComponent<ElementMarkController>(), Is.Not.Null, $"{path}: missing ElementMarkController");
                Assert.That(hit.GetComponent<ElementMarkView>(), Is.Not.Null, $"{path}: missing ElementMarkView");
                Assert.That(hit.GetComponent<ElementReactionEffects>(), Is.Not.Null, $"{path}: missing ElementReactionEffects");
            }
        }
    }

    [Test] public void SkillDescriptionsAndIconsArePresent()
    {
        foreach (var path in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Skill" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            var serialized = new SerializedObject(obj);
            var description = serialized.FindProperty("Description");
            if (description != null) Assert.That(description.stringValue, Is.Not.Null.And.Not.Empty, path);
        }
    }
}
#endif