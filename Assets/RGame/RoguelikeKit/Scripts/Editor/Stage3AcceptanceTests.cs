#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RGame.RoguelikeKit;

public sealed class Stage3AcceptanceTests
{
    [Test]
    public void WardRatiosAndReactionMultiplierMatchStage3Contract()
    {
        Assert.That(CorruptionWard.EliteRatio, Is.EqualTo(0.25f));
        Assert.That(CorruptionWard.BossRatio, Is.EqualTo(0.35f));
        Assert.That(CorruptionWard.ReactionMultiplier, Is.EqualTo(2f));
        Assert.That(CorruptionWard.BreakDuration, Is.EqualTo(2.5f));
        Assert.That(CorruptionWard.Vulnerability, Is.EqualTo(1.25f));
    }

    [Test]
    public void WardAbsorbsOrdinaryAndReactionDamageThenBreaks()
    {
        var go = new GameObject("WardTest");
        var ward = go.AddComponent<CorruptionWard>();
        ward.Configure(SpawnSetRole.Elite, 100);
        bool broke;
        Assert.That(ward.Absorb(10, false, out broke), Is.EqualTo(0));
        Assert.That(ward.Current, Is.EqualTo(15f).Within(0.01f));
        Assert.That(ward.Absorb(8, true, out broke), Is.EqualTo(1));
        Assert.That(broke, Is.True);
        Assert.That(ward.Broken, Is.True);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void BreakthroughDefinitionsCoverFeaturedReactions()
    {
        Assert.That(ShentongResolver.Definitions, Has.Count.GreaterThanOrEqualTo(11));
        foreach (var id in new[]
        {
            "shentong_burning", "shentong_frost", "shentong_stone", "shentong_mud",
            "shentong_steam", "shentong_lava", "shentong_breaklife", "shentong_moisture",
            "shentong_moltenmetal", "shentong_rootbreak", "shentong_wuxing"
        })
            Assert.That(ShentongResolver.Definitions.Any(x => x.Id == id), Is.True, $"missing {id}");
        Assert.That(Resources.Load<ShentongBreakthroughSO>("ShentongBreakthroughs"), Is.Not.Null);
        Assert.That(Resources.Load<CorruptionWardConfigSO>("CorruptionWardConfig"), Is.Not.Null);
    }

    [Test]
    public void EachSkillBuildsReactionProgressAndOnlyRaisesReadyAtEight()
    {
        ShentongRuntime.Reset();
        var raised = new List<string>();
        ShentongRuntime.BreakthroughReady += raised.Add;
        Assert.That(ReactionResolver.TryResolve(ElementType.Metal, ElementType.Water, out var reaction), Is.True);
        for (int i = 0; i < 7; i++) ShentongRuntime.RecordReaction("TestSkill", reaction);
        Assert.That(ShentongRuntime.ProgressFor("TestSkill"), Is.EqualTo(7));
        Assert.That(raised, Is.Empty);
        ShentongRuntime.RecordReaction("TestSkill", reaction);
        Assert.That(raised, Is.EqualTo(new[] { "TestSkill" }));
        ShentongRuntime.RecordReaction("OtherSkill", reaction);
        Assert.That(ShentongRuntime.ProgressFor("OtherSkill"), Is.EqualTo(1));
        ShentongRuntime.BreakthroughReady -= raised.Add;
        ShentongRuntime.Reset();
    }

    [Test]
    public void OrdinaryTargetsReceiveNoWardAndBossReceivesThirtyFivePercent()
    {
        var go = new GameObject("WardRoleTest");
        var ward = go.AddComponent<CorruptionWard>();
        ward.Configure(SpawnSetRole.Normal, 100);
        Assert.That(ward.Enabled, Is.False);
        ward.Configure(SpawnSetRole.FinalBoss, 200);
        Assert.That(ward.Maximum, Is.EqualTo(70f).Within(0.01f));
        ward.ResetState();
        Assert.That(ward.Enabled, Is.False);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ShentongRuntimeCapsAtTwoUnlocksAndResets()
    {
        ShentongRuntime.Reset();
        Assert.That(ShentongRuntime.MaxBreakthroughs, Is.EqualTo(2));
        ShentongRuntime.Unlock("shentong_burning", "SkillA");
        ShentongRuntime.Unlock("shentong_frost", "SkillB");
        ShentongRuntime.Unlock("shentong_stone", "SkillC");
        Assert.That(ShentongRuntime.UnlockedIds, Has.Count.EqualTo(2));
        ShentongRuntime.Reset();
        Assert.That(ShentongRuntime.UnlockedIds, Is.Empty);
    }
}
#endif