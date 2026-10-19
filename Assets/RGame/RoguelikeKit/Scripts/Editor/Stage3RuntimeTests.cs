#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RGame.RoguelikeKit;

public sealed class Stage3RuntimeTests
{
    private GameObject _root;
    [SetUp] public void SetUp() { Time.timeScale = 1f; ShentongRuntime.Reset(); TribulationRewardRuntime.Clear(); InnateRootRuntime.Clear(); _root = new GameObject("Stage3RuntimeTestRoot"); }
    [TearDown] public void TearDown() { Time.timeScale = 1f; ShentongRuntime.Reset(); TribulationRewardRuntime.Clear(); InnateRootRuntime.Clear(); if (_root != null) Object.DestroyImmediate(_root); }

    [Test]
    public void WardUsesDifferentOrdinaryAndReactionAbsorption()
    {
        var ward = _root.AddComponent<CorruptionWard>(); bool broke;
        ward.Configure(SpawnSetRole.Elite, 100);
        Assert.That(ward.Absorb(10, false, out broke), Is.EqualTo(0));
        Assert.That(ward.Absorb(8, true, out broke), Is.EqualTo(1));
        Assert.That(broke && ward.Broken, Is.True);
        Assert.That(ward.DamageMultiplier, Is.EqualTo(1.25f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator ElementMarksResolveAndClearInPlayMode()
    {
        var marks = _root.AddComponent<ElementMarkController>();
        Assert.That(marks.Register(ElementType.Metal, "metal", out _), Is.False);
        Assert.That(marks.Register(ElementType.Water, "water", out var reaction), Is.True);
        Assert.That(reaction.Name, Is.EqualTo("寒刃")); marks.Clear();
        Assert.That(marks.MarkCount, Is.EqualTo(0)); yield return null;
    }

    [Test] public void BreakthroughProgressCapsAtEightAndUnlocksOnlyTwice()
    { Assert.That(ReactionResolver.TryResolve(ElementType.Metal, ElementType.Water, out var reaction), Is.True); for (var i = 0; i < ShentongRuntime.RequiredReactions; i++) ShentongRuntime.RecordReaction("SkillA", reaction); Assert.That(ShentongRuntime.ProgressFor("SkillA"), Is.EqualTo(8)); ShentongRuntime.Unlock("shentong_frost", "SkillA"); ShentongRuntime.Unlock("shentong_stone", "SkillB"); ShentongRuntime.Unlock("shentong_mud", "SkillC"); Assert.That(ShentongRuntime.UnlockedIds, Has.Count.EqualTo(2)); }

    [Test] public void RunStateAndRewardRuntimeResetBetweenRuns()
    { var state = new TribulationRunState { EventActive = true, EventSucceeded = true, FinalOmenShown = true, RootAwakeningClaimed = true }; state.ChosenIds.Add("soul"); TribulationRewardRuntime.Apply("burning"); state.Reset(); TribulationRewardRuntime.Clear(); Assert.That(state.ChosenIds, Is.Empty); Assert.That(state.EventActive, Is.False); Assert.That(TribulationRewardRuntime.Has("burning"), Is.False); }

    [Test] public void TimelineAndRewardFilteringMatchStage3Flow()
    { var timeline = Resources.Load<TribulationTimelineSO>("TribulationTimeline"); Assert.That(timeline, Is.Not.Null); Assert.That(timeline.FirstChoiceTime, Is.EqualTo(240f)); Assert.That(timeline.SecondChoiceTime, Is.EqualTo(480f)); Assert.That(timeline.FinalOmenTime, Is.EqualTo(630f)); Assert.That(timeline.FinalBossTime, Is.EqualTo(660f)); Assert.That(timeline.StageEndTime, Is.EqualTo(720f)); var skill = new TribulationRewardDefinition { Id = "skill", Kind = TribulationRewardKind.SkillMutation, RequiredSkillKey = "SkillA" }; var root = new TribulationRewardDefinition { Id = "root", Kind = TribulationRewardKind.InnateRootAwakening }; Assert.That(TribulationRules.GetEligibleRewards(new[] { skill, root }, null, new System.Collections.Generic.HashSet<string> { "SkillA" }, false).Count(), Is.EqualTo(2)); }
}
#endif