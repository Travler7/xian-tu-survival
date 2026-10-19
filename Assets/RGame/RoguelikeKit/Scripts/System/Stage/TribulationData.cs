using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public enum TribulationPressure { Spawn, Ranged, Control, Elite, Element }
    public enum TribulationGoal { Survive, KillRanged, KillMarked, KillAny, KillElites, TriggerReactions }
    public enum TribulationRewardKind { SkillMutation, InnateRootAwakening }

    public static class TribulationRules
    {
        public static IEnumerable<TribulationDefinition> GetAvailableChoices(
            IEnumerable<TribulationDefinition> pool, IEnumerable<string> chosenIds, ISet<TribulationPressure> chosenPressures)
        {
            var ids = new HashSet<string>(chosenIds ?? Enumerable.Empty<string>());
            return (pool ?? Enumerable.Empty<TribulationDefinition>()).Where(t => t != null &&
                !string.IsNullOrWhiteSpace(t.Id) && !ids.Contains(t.Id) && !chosenPressures.Contains(t.Pressure));
        }

        public static IEnumerable<TribulationRewardDefinition> GetEligibleRewards(
            IEnumerable<TribulationRewardDefinition> pool, IEnumerable<string> chosenIds,
            ISet<string> activeSkillKeys, bool rootAwakeningClaimed)
        {
            var ids = new HashSet<string>(chosenIds ?? Enumerable.Empty<string>());
            return (pool ?? Enumerable.Empty<TribulationRewardDefinition>()).Where(r => r != null &&
                !string.IsNullOrWhiteSpace(r.Id) && !ids.Contains(r.Id) &&
                (r.Kind != TribulationRewardKind.InnateRootAwakening || !rootAwakeningClaimed) &&
                (r.Kind == TribulationRewardKind.InnateRootAwakening || activeSkillKeys.Contains(r.RequiredSkillKey)));
        }
    }

    [Serializable]
    public sealed class TribulationDefinition
    {
        public string Id;
        public string DisplayName;
        [TextArea(2, 4)] public string Description;
        public TribulationPressure Pressure;
        [Min(1)] public float Duration = 25f;
        public TribulationGoal Goal;
        [Min(1)] public int TargetCount = 1;
        public SpawnSet EventSpawnSet;
        [Min(1)] public int SpawnMultiplier = 1;
        [TextArea(1, 2)] public string DifficultyEffect;
        [TextArea(1, 2)] public string RewardDirection;
    }

    [Serializable]
    public sealed class TribulationRewardDefinition
    {
        public string Id;
        public string DisplayName;
        [TextArea(2, 4)] public string Description;
        public TribulationRewardKind Kind;
        public string RequiredSkillKey;
        public int AmountBonus;
        public int AreaBonus;
        public InnateRootType Root;
    }

    [Serializable]
    public sealed class TribulationRunState
    {
        public readonly List<string> ChosenIds = new();
        public readonly List<TribulationPressure> ChosenPressures = new();
        public readonly List<string> RewardIds = new();
        public bool EventActive;
        public bool EventSucceeded;
        public bool FinalOmenShown;
        public bool RootAwakeningClaimed;

        public void Reset()
        {
            ChosenIds.Clear();
            ChosenPressures.Clear();
            RewardIds.Clear();
            EventActive = false;
            EventSucceeded = false;
            FinalOmenShown = false;
            RootAwakeningClaimed = false;
        }
    }

    public static class TribulationRewardRuntime
    {
        private static readonly HashSet<string> Active = new();

        public static void Apply(string id)
        {
            if (!string.IsNullOrWhiteSpace(id)) Active.Add(id);
        }

        public static void Clear() => Active.Clear();

        // Static state survives a domain reload; clear it when Play starts so rewards never carry
        // over from a previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearOnPlayStart() => Clear();

        public static bool Has(string id) => Active.Contains(id);

        public static float DirectDamageMultiplier(ReactionResult reaction)
        {
            return Has("burning") && reaction.First == ElementType.Wood && reaction.Second == ElementType.Fire ? 1.15f : 1f;
        }

        public static float AreaRadiusMultiplier(ReactionResult reaction)
        {
            return Has("burning") && reaction.First == ElementType.Wood && reaction.Second == ElementType.Fire ? 1.3f : 1f;
        }

        public static int DotTicks(ReactionResult reaction)
        {
            return Has("burning") && reaction.Burn ? reaction.DotTicks + 1 : reaction.DotTicks;
        }

        public static float FreezeDuration(ReactionResult reaction)
        {
            return Has("echo") && reaction.Freeze ? reaction.FreezeDuration + 0.15f : reaction.FreezeDuration;
        }
    }
}