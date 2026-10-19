#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RGame.RoguelikeKit;

public sealed class Stage5ResonanceAcceptanceTests
{
    [SetUp]
    public void SetUp() => ElementResonanceRuntime.Reset();

    [TearDown]
    public void TearDown() => ElementResonanceRuntime.Reset();

    [Test]
    public void FiveElementsHaveIndependentConfiguredResonance()
    {
        Assert.That(ElementResonanceRuntime.Definitions, Has.Count.EqualTo(5));
        var elements = ElementResonanceRuntime.Definitions.Select(x => x.Element).ToList();
        Assert.That(elements.Distinct().Count(), Is.EqualTo(5));
        foreach (var definition in ElementResonanceRuntime.Definitions)
        {
            Assert.That(definition.Id, Is.Not.Empty);
            Assert.That(definition.DisplayName, Is.Not.Empty);
            Assert.That(definition.Description, Is.Not.Empty);
            Assert.That(definition.Threshold, Is.GreaterThan(0));
            Assert.That(definition.DirectDamageMultiplier, Is.GreaterThan(0));
        }
        Assert.That(Resources.Load<ElementResonanceSO>("ElementResonance"), Is.Not.Null);
    }

    [Test]
    public void ResonanceTriggersAtThresholdAndResetsItsCounter()
    {
        var definition = ElementResonanceRuntime.DefinitionFor(ElementType.Metal);
        ElementResonanceResult result = default;
        for (int i = 0; i < definition.Threshold - 1; i++)
            Assert.That(ElementResonanceRuntime.RecordDirectHit(ElementType.Metal, "skill_" + i, 7, out result), Is.False);
        Assert.That(ElementResonanceRuntime.CountFor(ElementType.Metal), Is.EqualTo(definition.Threshold - 1));
        Assert.That(ElementResonanceRuntime.RecordDirectHit(ElementType.Metal, "threshold", 7, out result), Is.True);
        Assert.That(result.Name, Is.EqualTo(definition.DisplayName));
        Assert.That(ElementResonanceRuntime.CountFor(ElementType.Metal), Is.Zero);
    }

    [Test]
    public void SourceCooldownBlocksOnlyRepeatedSameSourceAndDoesNotConsumeMarksContract()
    {
        var definition = ElementResonanceRuntime.DefinitionFor(ElementType.Water);
        Assert.That(ElementResonanceRuntime.RecordDirectHit(ElementType.Water, "same", 3, out _), Is.False);
        Assert.That(ElementResonanceRuntime.RecordDirectHit(ElementType.Water, "same", 3, out _), Is.False);
        Assert.That(ElementResonanceRuntime.CountFor(ElementType.Water), Is.EqualTo(1));
        Assert.That(ElementResonanceRuntime.RecordDirectHit(ElementType.Water, "same", 4, out _), Is.False);
        Assert.That(ElementResonanceRuntime.CountFor(ElementType.Water), Is.EqualTo(2));
        Assert.That(definition.SourceCooldown, Is.GreaterThan(0));
    }

    [Test]
    public void ResonanceCooldownPreventsImmediateSecondBurst()
    {
        var definition = ElementResonanceRuntime.DefinitionFor(ElementType.Fire);
        for (int i = 0; i < definition.Threshold; i++)
            ElementResonanceRuntime.RecordDirectHit(ElementType.Fire, "fire_" + i, 10, out _);
        Assert.That(ElementResonanceRuntime.CountFor(ElementType.Fire), Is.Zero);
        for (int i = 0; i < definition.Threshold; i++)
            Assert.That(ElementResonanceRuntime.RecordDirectHit(ElementType.Fire, "blocked_" + i, 10, out _), Is.False);
        Assert.That(ElementResonanceRuntime.CountFor(ElementType.Fire), Is.EqualTo(definition.Threshold));
    }

    [Test]
    public void DirectResonanceSourceIsSeparateFromReactionAndMarkOnlyPaths()
    {
        Assert.That(ReactionResolver.TryResolve(ElementType.Metal, ElementType.Water, out var reaction), Is.True);
        var events = new List<string>();
        void OnTriggered(ElementResonanceResult value, string source, int target) => events.Add(value.Id);
        ElementResonanceRuntime.Triggered += OnTriggered;
        ElementResonanceRuntime.RecordDirectHit(ElementType.Metal, "direct", 11, out _);
        ElementResonanceRuntime.RecordDirectHit(ElementType.Water, "direct", 11, out _);
        Assert.That(events, Is.Empty);
        ElementResonanceRuntime.Triggered -= OnTriggered;
        Assert.That(reaction.Name, Is.EqualTo("寒刃"));
    }

    [Test]
    public void ResetClearsCountsCooldownsAndAllowsNewRun()
    {
        var definition = ElementResonanceRuntime.DefinitionFor(ElementType.Earth);
        for (int i = 0; i < definition.Threshold; i++)
            ElementResonanceRuntime.RecordDirectHit(ElementType.Earth, "earth_" + i, 5, out _);
        ElementResonanceRuntime.Reset();
        Assert.That(ElementResonanceRuntime.CountFor(ElementType.Earth), Is.Zero);
        Assert.That(ElementResonanceRuntime.RecordDirectHit(ElementType.Earth, "fresh", 5, out _), Is.False);
        Assert.That(ElementResonanceRuntime.CountFor(ElementType.Earth), Is.EqualTo(1));
    }

    [Test]
    public void RemainingFeaturedBreakthroughsHaveConfiguredRuntimeEffects()
    {
        var definitions = ShentongResolver.Definitions.ToDictionary(x => x.Id);
        foreach (var id in new[]
        {
            "shentong_steam", "shentong_lava", "shentong_breaklife", "shentong_moisture",
            "shentong_moltenmetal", "shentong_rootbreak"
        })
        {
            Assert.That(definitions.ContainsKey(id), Is.True, $"missing {id}");
            Assert.That(definitions[id].DisplayName, Is.Not.Empty);
            Assert.That(definitions[id].Description, Is.Not.Empty);
        }

        Assert.That(ReactionResolver.TryResolve(ElementType.Fire, ElementType.Water, out var steam), Is.True);
        ShentongRuntime.Reset();
        ShentongRuntime.Unlock("shentong_steam", "SteamSkill");
        Assert.That(ShentongRuntime.AreaRadiusMultiplier(steam), Is.GreaterThan(1f));
        Assert.That(ShentongRuntime.AreaDamageMultiplier(steam), Is.GreaterThan(1f));
        Assert.That(ShentongRuntime.ReactionDamageMultiplier(steam), Is.GreaterThan(1f));

        Assert.That(ReactionResolver.TryResolve(ElementType.Fire, ElementType.Earth, out var lava), Is.True);
        ShentongRuntime.Reset();
        ShentongRuntime.Unlock("shentong_lava", "LavaSkill");
        Assert.That(ShentongRuntime.DotTickBonus(lava), Is.GreaterThan(0));
        Assert.That(ShentongRuntime.SlowDurationBonus(lava), Is.GreaterThan(0f));

        Assert.That(ReactionResolver.TryResolve(ElementType.Metal, ElementType.Wood, out var breaklife), Is.True);
        ShentongRuntime.Reset();
        ShentongRuntime.Unlock("shentong_breaklife", "BreaklifeSkill");
        Assert.That(ShentongRuntime.SlowDurationBonus(breaklife), Is.GreaterThan(0f));
        Assert.That(ShentongRuntime.ReactionDamageMultiplier(breaklife), Is.GreaterThan(1f));

        Assert.That(ReactionResolver.TryResolve(ElementType.Wood, ElementType.Water, out var moisture), Is.True);
        ShentongRuntime.Reset();
        ShentongRuntime.Unlock("shentong_moisture", "MoistureSkill");
        Assert.That(ShentongRuntime.DotTickBonus(moisture), Is.GreaterThan(0));
        Assert.That(ShentongRuntime.ReactionDamageMultiplier(moisture), Is.GreaterThan(1f));

        Assert.That(ReactionResolver.TryResolve(ElementType.Metal, ElementType.Fire, out var moltenMetal), Is.True);
        ShentongRuntime.Reset();
        ShentongRuntime.Unlock("shentong_moltenmetal", "MoltenMetalSkill");
        Assert.That(ShentongRuntime.DotTickBonus(moltenMetal), Is.GreaterThan(0));
        Assert.That(ShentongRuntime.ReactionDamageMultiplier(moltenMetal), Is.GreaterThan(1f));

        Assert.That(ReactionResolver.TryResolve(ElementType.Wood, ElementType.Earth, out var rootbreak), Is.True);
        ShentongRuntime.Reset();
        ShentongRuntime.Unlock("shentong_rootbreak", "RootbreakSkill");
        Assert.That(ShentongRuntime.SlowDurationBonus(rootbreak), Is.GreaterThan(0f));
        Assert.That(ShentongRuntime.ReactionDamageMultiplier(rootbreak), Is.GreaterThan(1f));
        ShentongRuntime.Reset();
    }

    [Test]
    public void AllTenReactionPairsHaveDedicatedBreakthroughDefinitions()
    {
        var elements = new[] { ElementType.Metal, ElementType.Wood, ElementType.Water, ElementType.Fire, ElementType.Earth };
        var definitions = ShentongResolver.Definitions.Where(x => x.First != ElementType.None && x.Second != ElementType.None).ToList();
        Assert.That(definitions.Count, Is.EqualTo(10));
        for (int i = 0; i < elements.Length; i++)
        for (int j = i + 1; j < elements.Length; j++)
        {
            var first = elements[i];
            var second = elements[j];
            Assert.That(definitions.Any(x => (x.First == first && x.Second == second) || (x.First == second && x.Second == first)), Is.True,
                $"missing dedicated breakthrough for {first}+{second}");
        }
    }
}
#endif
