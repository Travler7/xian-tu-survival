#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using RGame.RoguelikeKit;

public class ElementAcceptanceTests
{
    [Test] public void AllTenElementPairsResolve()
    {
        var elements = new[] { ElementType.Metal, ElementType.Wood, ElementType.Water, ElementType.Fire, ElementType.Earth };
        int count = 0;
        for (int i = 0; i < elements.Length; i++) for (int j = i + 1; j < elements.Length; j++)
        {
            Assert.That(ReactionResolver.TryResolve(elements[i], elements[j], out _), Is.True);
            count++;
        }
        Assert.That(count, Is.EqualTo(10));
    }

    [Test] public void AllTenDefinitionsHaveNamesDescriptionsAndDamage()
    {
        var matrixAsset = AssetDatabase.LoadAssetAtPath<ReactionMatrixSO>("Assets/RGame/RoguelikeKit/Resources/ReactionMatrix.asset");
        Assert.That(matrixAsset, Is.Not.Null);
        Assert.That(matrixAsset.Definitions.Count, Is.EqualTo(10));
        Assert.That(ReactionResolver.Definitions.Count, Is.EqualTo(10));
        var names = new System.Collections.Generic.HashSet<string>();
        foreach (var definition in ReactionResolver.Definitions)
        {
            Assert.That(definition.ReactionName, Is.Not.Null.And.Not.Empty);
            Assert.That(definition.Description, Is.Not.Null.And.Not.Empty);
            Assert.That(definition.RelationType, Is.Not.Null.And.Not.Empty);
            Assert.That(definition.DirectDamageMultiplier, Is.GreaterThan(0f));
            Assert.That(definition.VfxPoolKey, Is.Not.Null.And.Not.Empty);
            Assert.That(names.Add(definition.ReactionName), Is.True, "Reaction names must be unique");
        }
    }

    [Test] public void FeaturedDefinitionsContainObservableEffects()
    {
        Assert.That(ReactionResolver.TryResolve(ElementType.Wood, ElementType.Fire, out var wildfire), Is.True);
        Assert.That(wildfire.Burn && wildfire.AreaDamageMultiplier > 0f && wildfire.AreaRadius > 0f, Is.True);
        Assert.That(wildfire.MaxAreaTargets, Is.EqualTo(3));
        Assert.That(ReactionResolver.TryResolve(ElementType.Water, ElementType.Earth, out var mire), Is.True);
        Assert.That(mire.AreaDamageMultiplier > 0f && mire.SlowPercent > 0f && mire.SlowDuration > 0f, Is.True);
        Assert.That(ReactionResolver.TryResolve(ElementType.Metal, ElementType.Water, out var cold), Is.True);
        Assert.That(cold.FreezeDuration, Is.GreaterThan(0f));
    }

    [Test] public void MarkOnlyNeverCreatesDamagePath()
    {
        var go = new GameObject("ElementMarkOnlyTest");
        var marks = go.AddComponent<ElementMarkController>();
        marks.ApplyMarkOnly(ElementType.Fire, "spread");
        Assert.That(marks.MarkCount, Is.EqualTo(1));
        Object.DestroyImmediate(go);
    }

    [Test] public void ReactionOrderIsCommutative()
    {
        Assert.That(ReactionResolver.TryResolve(ElementType.Metal, ElementType.Water, out var a), Is.True);
        Assert.That(ReactionResolver.TryResolve(ElementType.Water, ElementType.Metal, out var b), Is.True);
        Assert.That(a.Name, Is.EqualTo(b.Name));
    }

    [Test] public void SameElementDoesNotReact()
    {
        Assert.That(ReactionResolver.TryResolve(ElementType.Fire, ElementType.Fire, out _), Is.False);
    }

    [Test] public void ActiveSkillsHaveElementDataAndCopiesPreserveIt()
    {
        var paths = AssetDatabase.FindAssets("t:SkillDataSO", new[] { "Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Skill/AttackSkill" })
            .Select(AssetDatabase.GUIDToAssetPath).ToList();
        Assert.That(paths.Count, Is.GreaterThanOrEqualTo(9));
        foreach (var path in paths)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillDataSO>(path);
            Assert.That(skill.PrimaryElement, Is.Not.EqualTo(ElementType.None), path);
            var copy = skill.Copy();
            Assert.That(copy.PrimaryElement, Is.EqualTo(skill.PrimaryElement), path);
            Assert.That(copy.SecondaryElement, Is.EqualTo(skill.SecondaryElement), path);
            Object.DestroyImmediate(copy);
        }
    }

    [Test] public void InnateRootsAndFeaturedReactionsAreConfigured()
    {
        var fighter = AssetDatabase.LoadAssetAtPath<CharacterSelectConfigSO>("Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Level/SelectCharacter/Fighter.asset");
        var assassin = AssetDatabase.LoadAssetAtPath<CharacterSelectConfigSO>("Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Level/SelectCharacter/Assassin.asset");
        var mage = AssetDatabase.LoadAssetAtPath<CharacterSelectConfigSO>("Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Level/SelectCharacter/Mage.asset");
        Assert.That(fighter.InnateRoot, Is.EqualTo(InnateRootType.Metal));
        Assert.That(assassin.InnateRoot, Is.EqualTo(InnateRootType.Wood));
        Assert.That(mage.InnateRoot, Is.EqualTo(InnateRootType.Water));
        Assert.That(ReactionResolver.TryResolve(ElementType.Metal, ElementType.Water, out var cold), Is.True);
        Assert.That(cold.Freeze, Is.True);
        Assert.That(ReactionResolver.TryResolve(ElementType.Wood, ElementType.Fire, out var wildfire), Is.True);
        Assert.That(wildfire.Burn && wildfire.Spread, Is.True);
        Assert.That(ReactionResolver.TryResolve(ElementType.Water, ElementType.Earth, out var mire), Is.True);
        Assert.That(mire.AreaSlow, Is.True);
        Assert.That(mire.SlowPercent, Is.EqualTo(45f));
    }

    [Test] public void ElementMarksRespectCapacityAndSourceCooldown()
    {
        var go = new GameObject("ElementMarkTest");
        var marks = go.AddComponent<ElementMarkController>();
        marks.Apply(ElementType.Metal, 1, "skill");
        Assert.That(marks.MarkCount, Is.EqualTo(1));
        marks.Apply(ElementType.Water, 1, "other");
        Assert.That(marks.MarkCount, Is.EqualTo(0), "A reaction consumes both marks");
        marks.Apply(ElementType.Fire, 1, "skill");
        marks.Apply(ElementType.Fire, 1, "skill");
        Assert.That(marks.MarkCount, Is.EqualTo(1), "A source cannot duplicate its mark during cooldown");
        marks.Apply(ElementType.Wood, 1, "wood");
        marks.Apply(ElementType.Earth, 1, "earth");
        Assert.That(marks.MarkCount, Is.LessThanOrEqualTo(2));
        Object.DestroyImmediate(go);
    }
}
#endif
