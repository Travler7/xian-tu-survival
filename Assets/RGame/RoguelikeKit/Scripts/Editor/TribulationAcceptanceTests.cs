#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit.EditorTests
{
    public sealed class TribulationAcceptanceTests
    {
        [Test]
        public void TimelineMatchesStageBoundaries()
        {
            var timeline = ScriptableObject.CreateInstance<TribulationTimelineSO>();
            Assert.That(timeline.FirstChoiceTime, Is.EqualTo(240f));
            Assert.That(timeline.SecondChoiceTime, Is.EqualTo(480f));
            Assert.That(timeline.FinalOmenTime, Is.EqualTo(630f));
            Assert.That(timeline.FinalBossTime, Is.EqualTo(660f));
            Assert.That(timeline.StageEndTime, Is.EqualTo(720f));
            Object.DestroyImmediate(timeline);
        }

        [Test]
        public void RunStateAllowsOnlyOneRootAwakening()
        {
            var state = new TribulationRunState();
            Assert.That(state.RootAwakeningClaimed, Is.False);
            state.RootAwakeningClaimed = true;
            Assert.That(state.RootAwakeningClaimed, Is.True);
            state.Reset();
            Assert.That(state.RootAwakeningClaimed, Is.False);
        }

        [Test]
        public void PressureTagsCanBeComparedWithoutPoolKeyChanges()
        {
            var selected = new HashSet<TribulationPressure> { TribulationPressure.Spawn };
            Assert.That(selected.Contains(TribulationPressure.Spawn), Is.True);
            Assert.That(selected.Contains(TribulationPressure.Element), Is.False);
        }

        [Test]
        public void ResourcePoolsContainConfiguredTribulationsAndRewards()
        {
            var timeline = Resources.Load<TribulationTimelineSO>("TribulationTimeline");
            var pool = Resources.Load<TribulationPoolSO>("TribulationPool");
            var rewards = Resources.Load<TribulationRewardPoolSO>("TribulationRewardPool");
            Assert.That(timeline, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);
            Assert.That(pool.Tribulations, Has.Count.EqualTo(6));
            Assert.That(rewards, Is.Not.Null);
            Assert.That(rewards.Rewards, Has.Count.EqualTo(7));
            Assert.That(pool.Tribulations.TrueForAll(t => t.EventSpawnSet != null), Is.True);
            Assert.That(pool.Tribulations.TrueForAll(t => !string.IsNullOrWhiteSpace(t.DifficultyEffect)), Is.True);
        }

        [Test]
        public void ChoicePoolExcludesPreviouslyChosenIdAndPressure()
        {
            var pool = new List<TribulationDefinition>
            {
                new() { Id = "a", Pressure = TribulationPressure.Spawn },
                new() { Id = "b", Pressure = TribulationPressure.Ranged },
                new() { Id = "c", Pressure = TribulationPressure.Control }
            };
            var choices = TribulationRules.GetAvailableChoices(pool, new[] { "b" }, new HashSet<TribulationPressure> { TribulationPressure.Spawn });
            var result = new List<TribulationDefinition>(choices);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo("c"));
        }

        [Test]
        public void RewardPoolKeepsSingleEligibleRewardAndEnforcesRunLimits()
        {
            var pool = new List<TribulationRewardDefinition>
            {
                new() { Id = "skill", Kind = TribulationRewardKind.SkillMutation, RequiredSkillKey = "KnownSkill" },
                new() { Id = "other", Kind = TribulationRewardKind.SkillMutation, RequiredSkillKey = "MissingSkill" },
                new() { Id = "root", Kind = TribulationRewardKind.InnateRootAwakening }
            };
            var eligible = TribulationRules.GetEligibleRewards(pool, new[] { "root" }, new HashSet<string> { "KnownSkill" }, true);
            var result = new List<TribulationRewardDefinition>(eligible);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo("skill"));
        }
    }
}
#endif
