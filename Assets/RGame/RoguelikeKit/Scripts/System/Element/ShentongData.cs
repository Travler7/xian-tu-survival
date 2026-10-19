using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    [Serializable]
    public sealed class ShentongBreakthroughDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public ElementType First;
        public ElementType Second;
        public float AreaRadiusMultiplier = 1f;
        public float AreaDamageMultiplier = 1f;
        public int DotTickBonus;
        public float FreezeFollowupDuration;
        public float SlowAreaDurationBonus;
        public float SlowDurationBonus;
        public float ReactionDamageMultiplier = 1f;
    }

    public static class ShentongResolver
    {
        private static readonly IReadOnlyList<ShentongBreakthroughDefinition> Defaults = new[]
        {
            new ShentongBreakthroughDefinition { Id = "shentong_burning", DisplayName = "燎原·焚野", Description = "燎原范围扩大25%，灼烧额外持续一次。", First = ElementType.Wood, Second = ElementType.Fire, AreaRadiusMultiplier = 1.25f, DotTickBonus = 1 },
            new ShentongBreakthroughDefinition { Id = "shentong_frost", DisplayName = "寒刃·回响", Description = "寒刃冻结结束后，对附近敌人施加短暂减速。", First = ElementType.Metal, Second = ElementType.Water, FreezeFollowupDuration = 0.5f },
            new ShentongBreakthroughDefinition { Id = "shentong_stone", DisplayName = "金石·余震", Description = "金石崩触发后产生一次小范围余震。", First = ElementType.Metal, Second = ElementType.Earth, AreaRadiusMultiplier = 1.2f },
            new ShentongBreakthroughDefinition { Id = "shentong_mud", DisplayName = "泥沼·沉陷", Description = "泥沼减速区域延长1秒。", First = ElementType.Water, Second = ElementType.Earth, SlowAreaDurationBonus = 1f },
            new ShentongBreakthroughDefinition { Id = "shentong_steam", DisplayName = "蒸爆·回潮", Description = "蒸爆范围扩大，冷热冲击的爆发伤害提高。", First = ElementType.Fire, Second = ElementType.Water, AreaRadiusMultiplier = 1.2f, AreaDamageMultiplier = 1.15f, ReactionDamageMultiplier = 1.1f },
            new ShentongBreakthroughDefinition { Id = "shentong_lava", DisplayName = "熔岩·地火", Description = "熔岩灼烧额外持续一次，减速时间延长。", First = ElementType.Fire, Second = ElementType.Earth, DotTickBonus = 1, SlowDurationBonus = 0.5f },
            new ShentongBreakthroughDefinition { Id = "shentong_breaklife", DisplayName = "断生·枯荣", Description = "断生的减速持续更久，并强化反应伤害。", First = ElementType.Metal, Second = ElementType.Wood, SlowDurationBonus = 0.5f, ReactionDamageMultiplier = 1.1f },
            new ShentongBreakthroughDefinition { Id = "shentong_moisture", DisplayName = "润木·回生", Description = "润木灼烧额外持续一次，持续伤害提高。", First = ElementType.Wood, Second = ElementType.Water, DotTickBonus = 1, ReactionDamageMultiplier = 1.1f },
            new ShentongBreakthroughDefinition { Id = "shentong_moltenmetal", DisplayName = "熔金·淬火", Description = "熔金灼烧额外持续一次，并强化反应伤害。", First = ElementType.Metal, Second = ElementType.Fire, DotTickBonus = 1, ReactionDamageMultiplier = 1.1f },
            new ShentongBreakthroughDefinition { Id = "shentong_rootbreak", DisplayName = "根裂·破土", Description = "根裂的强力减速持续更久，并强化反应伤害。", First = ElementType.Wood, Second = ElementType.Earth, SlowDurationBonus = 0.5f, ReactionDamageMultiplier = 1.1f },
            new ShentongBreakthroughDefinition { Id = "shentong_wuxing", DisplayName = "五行·贯通", Description = "五行反应伤害提高15%。", First = ElementType.None, Second = ElementType.None, ReactionDamageMultiplier = 1.15f }
        };

        private static ShentongBreakthroughSO _pool;
        private static IReadOnlyList<ShentongBreakthroughDefinition> ActiveDefinitions
        {
            get
            {
                if (_pool == null) _pool = Resources.Load<ShentongBreakthroughSO>("ShentongBreakthroughs");
                return _pool != null && _pool.Definitions.Count > 0 ? _pool.Definitions : Defaults;
            }
        }

        public static IReadOnlyList<ShentongBreakthroughDefinition> Definitions => ActiveDefinitions;

        public static IEnumerable<ShentongBreakthroughDefinition> ForReaction(ReactionResult reaction, IEnumerable<string> unlocked)
        {
            var ids = new HashSet<string>(unlocked ?? Enumerable.Empty<string>());
            bool hasSpecial = ActiveDefinitions.Any(d => d.First != ElementType.None && !ids.Contains(d.Id) && Pair(reaction.First, reaction.Second, d.First, d.Second));
            return ActiveDefinitions.Where(d => !ids.Contains(d.Id) && (d.First == ElementType.None ? !hasSpecial : Pair(reaction.First, reaction.Second, d.First, d.Second)));
        }

        public static ShentongBreakthroughDefinition Find(string id) => ActiveDefinitions.FirstOrDefault(d => d.Id == id);

        private static bool Pair(ElementType a, ElementType b, ElementType x, ElementType y) => (a == x && b == y) || (a == y && b == x);
    }

    public static class ShentongRuntime
    {
        private static readonly Dictionary<string, int> Progress = new();
        private static readonly HashSet<string> Unlocked = new();
        private static readonly HashSet<string> ReadySkills = new();
        private static readonly HashSet<string> ClaimedSkills = new();
        private static readonly Dictionary<string, ReactionResult> LastReactions = new();
        public const int RequiredReactions = 8;
        public const int MaxBreakthroughs = 2;
        public static event Action<string> BreakthroughReady;
        public static IReadOnlyCollection<string> UnlockedIds => Unlocked;

        public static void Reset() { Progress.Clear(); Unlocked.Clear(); ReadySkills.Clear(); ClaimedSkills.Clear(); LastReactions.Clear(); }

        // Static state survives a domain reload; clear it when Play starts so a run can never
        // inherit breakthroughs from a previous session (or from the editor's last play).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayStart() => Reset();

        public static int ProgressFor(string skillKey) => string.IsNullOrEmpty(skillKey) ? 0 : (Progress.TryGetValue(skillKey, out var count) ? count : 0);

        /// <summary>True once the skill already spent its reactions on a breakthrough, so its
        /// counter is no longer pending and must not be shown as active progress.</summary>
        public static bool IsClaimed(string skillKey) => !string.IsNullOrEmpty(skillKey) && ClaimedSkills.Contains(skillKey);

        public static void RecordReaction(string sourceKey, ReactionResult reaction)
        {
            if (string.IsNullOrWhiteSpace(sourceKey) || Unlocked.Count >= MaxBreakthroughs) return;
            string skill = sourceKey.Split(':')[0];
            if (ClaimedSkills.Contains(skill)) return;
            LastReactions[skill] = reaction;
            if (!Progress.TryGetValue(skill, out var count)) count = 0;
            count = Math.Min(RequiredReactions, count + 1);
            Progress[skill] = count;
            if (count == RequiredReactions && ReadySkills.Add(skill)) BreakthroughReady?.Invoke(skill);
        }

        public static bool TryGetReadyReaction(string skill, out ReactionResult reaction) => LastReactions.TryGetValue(skill, out reaction);
        public static void Unlock(string id, string skill)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(skill) || Unlocked.Count >= MaxBreakthroughs) return;
            Unlocked.Add(id);
            ClaimedSkills.Add(skill);
        }
        public static bool Has(string id) => Unlocked.Contains(id);
        public static float AreaRadiusMultiplier(ReactionResult reaction)
        {
            var d = ShentongResolver.Definitions.FirstOrDefault(x => Unlocked.Contains(x.Id) && Pair(reaction, x));
            return d != null ? d.AreaRadiusMultiplier : 1f;
        }
        public static float AreaDamageMultiplier(ReactionResult reaction)
        {
            var d = ShentongResolver.Definitions.FirstOrDefault(x => Unlocked.Contains(x.Id) && x.AreaDamageMultiplier > 1f && Pair(reaction, x));
            return d != null ? d.AreaDamageMultiplier : 1f;
        }
        public static int DotTickBonus(ReactionResult reaction)
        {
            var d = ShentongResolver.Definitions.FirstOrDefault(x => Unlocked.Contains(x.Id) && Pair(reaction, x));
            return d != null ? d.DotTickBonus : 0;
        }
        public static float FreezeFollowupDuration(ReactionResult reaction)
        {
            var d = ShentongResolver.Definitions.FirstOrDefault(x => Unlocked.Contains(x.Id) && Pair(reaction, x));
            return d != null ? d.FreezeFollowupDuration : 0f;
        }
        public static float AreaSlowDurationBonus(ReactionResult reaction)
        {
            var d = ShentongResolver.Definitions.FirstOrDefault(x => Unlocked.Contains(x.Id) && Pair(reaction, x));
            return d != null ? d.SlowAreaDurationBonus : 0f;
        }
        public static float SlowDurationBonus(ReactionResult reaction)
        {
            var d = ShentongResolver.Definitions.FirstOrDefault(x => Unlocked.Contains(x.Id) && Pair(reaction, x));
            return d != null ? d.SlowDurationBonus : 0f;
        }
        public static float ReactionDamageMultiplier(ReactionResult reaction)
        {
            var d = ShentongResolver.Definitions.FirstOrDefault(x => Unlocked.Contains(x.Id) && x.ReactionDamageMultiplier > 1f && (x.First == ElementType.None || Pair(reaction, x)));
            return d != null ? d.ReactionDamageMultiplier : 1f;
        }
        private static bool Pair(ReactionResult r, ShentongBreakthroughDefinition d) => Pair(r.First, r.Second, d.First, d.Second);
        private static bool Pair(ReactionResult r, ElementType a, ElementType b) => Pair(r.First, r.Second, a, b);
        private static bool Pair(ElementType a, ElementType b, ElementType x, ElementType y) => (a == x && b == y) || (a == y && b == x);
    }
}
