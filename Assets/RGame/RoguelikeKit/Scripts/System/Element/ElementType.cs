using System;
using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public enum ElementType { None, Metal, Wood, Water, Fire, Earth }

    [Serializable]
    public sealed class ReactionDefinition
    {
        public ElementType First;
        public ElementType Second;
        public string ReactionName;
        [TextArea(2, 4)] public string Description;
        public string RelationType;
        public float DirectDamageMultiplier;
        public float AreaDamageMultiplier;
        public float AreaRadius;
        public int MaxAreaTargets;
        public float DotDamageMultiplier;
        public int DotTicks;
        public float SlowPercent;
        public float SlowDuration;
        public float FreezeDuration;
        public int SpreadMarkCount;
        public string VfxPoolKey;
        public Color PopupColor;
        public bool AreaSlow;

        public ReactionDefinition(ElementType first, ElementType second, string name, string description,
            string relation, float directDamage, float areaDamage = 0f, float areaRadius = 0f,
            int maxAreaTargets = 0, float dotDamage = 0f, int dotTicks = 0, float slowPercent = 0f,
            float slowDuration = 0f, float freezeDuration = 0f, int spreadMarkCount = 0,
            string vfxPoolKey = "ReactionVfx", Color? popupColor = null, bool areaSlow = false)
        {
            First = first; Second = second; ReactionName = name; Description = description; RelationType = relation;
            DirectDamageMultiplier = directDamage; AreaDamageMultiplier = areaDamage; AreaRadius = areaRadius;
            MaxAreaTargets = maxAreaTargets; DotDamageMultiplier = dotDamage; DotTicks = dotTicks;
            SlowPercent = slowPercent; SlowDuration = slowDuration; FreezeDuration = freezeDuration;
            SpreadMarkCount = spreadMarkCount; VfxPoolKey = vfxPoolKey; PopupColor = popupColor ?? Color.white;
            AreaSlow = areaSlow;
        }
    }

    public readonly struct ReactionResult
    {
        public readonly ElementType First;
        public readonly ElementType Second;
        public readonly string Name;
        public readonly string Description;
        public readonly string RelationType;
        public readonly float DirectDamageMultiplier;
        public readonly float AreaDamageMultiplier;
        public readonly float AreaRadius;
        public readonly int MaxAreaTargets;
        public readonly float DotDamageMultiplier;
        public readonly int DotTicks;
        public readonly float SlowPercent;
        public readonly float SlowDuration;
        public readonly float FreezeDuration;
        public readonly int SpreadMarkCount;
        public readonly string VfxPoolKey;
        public readonly Color PopupColor;
        public readonly bool AreaSlow;

        public float DamageMultiplier => DirectDamageMultiplier;
        public bool Slow => SlowPercent > 0f && SlowDuration > 0f;
        public bool Freeze => FreezeDuration > 0f;
        public bool Burn => DotTicks > 0 && DotDamageMultiplier > 0f;
        public bool Spread => SpreadMarkCount > 0;
        public int BurnTicks => DotTicks;

        public ReactionResult(ReactionDefinition definition)
        {
            First = definition.First; Second = definition.Second; Name = definition.ReactionName;
            Description = definition.Description; RelationType = definition.RelationType;
            DirectDamageMultiplier = definition.DirectDamageMultiplier; AreaDamageMultiplier = definition.AreaDamageMultiplier;
            AreaRadius = definition.AreaRadius; MaxAreaTargets = definition.MaxAreaTargets;
            DotDamageMultiplier = definition.DotDamageMultiplier; DotTicks = definition.DotTicks;
            SlowPercent = definition.SlowPercent; SlowDuration = definition.SlowDuration;
            FreezeDuration = definition.FreezeDuration; SpreadMarkCount = definition.SpreadMarkCount;
            VfxPoolKey = definition.VfxPoolKey; PopupColor = definition.PopupColor; AreaSlow = definition.AreaSlow;
        }
    }

    public static class ReactionResolver
    {
        private static readonly IReadOnlyList<ReactionDefinition> Matrix = new[]
        {
            new ReactionDefinition(ElementType.Metal, ElementType.Wood, "断生", "金克木，断绝生机。对目标造成额外伤害并减速。", "金克木", 0.6f, slowPercent: 35f, slowDuration: 1f, popupColor: new Color(1f, 0.78f, 0.18f)),
            new ReactionDefinition(ElementType.Metal, ElementType.Fire, "熔金", "火克金，烈焰熔化护体金气。对目标造成额外伤害并灼烧。", "火克金", 0.6f, dotDamage: 0.25f, dotTicks: 3, popupColor: new Color(1f, 0.35f, 0.08f)),
            new ReactionDefinition(ElementType.Metal, ElementType.Water, "寒刃", "金生水，寒意凝成锋刃。对目标造成额外伤害并冻结。", "金生水", 0.8f, freezeDuration: 0.35f, popupColor: new Color(0.35f, 0.82f, 1f)),
            new ReactionDefinition(ElementType.Metal, ElementType.Earth, "金石崩", "土生金，金石受灵力冲击而崩裂。对目标和附近敌人造成伤害。", "土生金", 0.7f, areaDamage: 0.35f, areaRadius: 1.5f, maxAreaTargets: 3, popupColor: new Color(1f, 0.72f, 0.22f)),
            new ReactionDefinition(ElementType.Wood, ElementType.Fire, "燎原", "木生火，余焰借势扩散。对目标造成爆发伤害并灼烧，同时灼伤附近敌群。", "木生火", 0.8f, areaDamage: 0.45f, areaRadius: 1.75f, maxAreaTargets: 3, dotDamage: 0.25f, dotTicks: 3, spreadMarkCount: 1, popupColor: new Color(1f, 0.2f, 0.05f)),
            new ReactionDefinition(ElementType.Wood, ElementType.Water, "润木", "水生木，灵液滋养藤木。对目标造成持续伤害并减速。", "水生木", 0.6f, dotDamage: 0.2f, dotTicks: 3, slowPercent: 45f, slowDuration: 1.5f, popupColor: new Color(0.28f, 0.95f, 0.55f)),
            new ReactionDefinition(ElementType.Wood, ElementType.Earth, "根裂", "木克土，根脉破土而出。对目标造成高额伤害并强力减速。", "木克土", 0.8f, slowPercent: 70f, slowDuration: 1.25f, popupColor: new Color(0.5f, 1f, 0.25f)),
            new ReactionDefinition(ElementType.Fire, ElementType.Water, "蒸爆", "水克火，冷热相冲骤然爆裂。对目标和附近敌人造成范围爆发并减速。", "水克火", 0.8f, areaDamage: 0.45f, areaRadius: 1.75f, maxAreaTargets: 3, slowPercent: 35f, slowDuration: 0.75f, popupColor: new Color(1f, 0.55f, 0.25f)),
            new ReactionDefinition(ElementType.Fire, ElementType.Earth, "熔岩", "火生土，熔流附着大地。对目标造成持续灼烧并减速。", "火生土", 0.7f, dotDamage: 0.25f, dotTicks: 3, slowPercent: 30f, slowDuration: 1.25f, popupColor: new Color(1f, 0.3f, 0.08f)),
            new ReactionDefinition(ElementType.Water, ElementType.Earth, "泥沼", "土克水，湿土化为沉重泥沼。对目标和附近敌人造成伤害并减速。", "土克水", 0.7f, areaDamage: 0.45f, areaRadius: 1.75f, maxAreaTargets: 3, slowPercent: 45f, slowDuration: 2f, areaSlow: true, popupColor: new Color(0.72f, 0.55f, 0.25f))
        };

        private static ReactionMatrixSO _resourceMatrix;
        private static bool _resourceChecked;

        public static IReadOnlyList<ReactionDefinition> Definitions
        {
            get
            {
                if (!_resourceChecked)
                {
                    _resourceChecked = true;
                    _resourceMatrix = Resources.Load<ReactionMatrixSO>("ReactionMatrix");
                }
                return _resourceMatrix != null && _resourceMatrix.Definitions.Count == 10 ? _resourceMatrix.Definitions : Matrix;
            }
        }

        public static bool TryResolve(ElementType a, ElementType b, out ReactionResult result)
        {
            result = default;
            if (a == b || a == ElementType.None || b == ElementType.None) return false;
            var definitions = Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (!Pair(a, b, definition.First, definition.Second)) continue;
                result = new ReactionResult(definition);
                return true;
            }
            return false;
        }

        private static bool Pair(ElementType a, ElementType b, ElementType x, ElementType y) => (a == x && b == y) || (a == y && b == x);
    }

    public enum InnateRootType { None, Metal, Wood, Water, Fire, Earth }

    public static class InnateRootRuntime
    {
        public static InnateRootType Current { get; private set; }
        public static InnateRootType Awakened { get; private set; }
        public static void Set(InnateRootType root) => Current = root;
        public static void Awaken(InnateRootType root) => Awakened = root;
        public static void ClearAwakening() => Awakened = InnateRootType.None;
        public static void Clear() { Current = InnateRootType.None; Awakened = InnateRootType.None; }

        // Static state survives a domain reload; clear it when Play starts so a run can never
        // inherit an awakened root from a previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearOnPlayStart() => Clear();
        public static float MarkDuration => Current == InnateRootType.Metal ? (Awakened == InnateRootType.Metal ? 4f : 3.5f) : Awakened == InnateRootType.Metal ? 3.5f : 3f;
        public static float ReactionDamageMultiplier(ElementType first, ElementType second) => (first == ElementType.Wood || second == ElementType.Wood) ? (Current == InnateRootType.Wood ? (Awakened == InnateRootType.Wood ? 1.2f : 1.1f) : Awakened == InnateRootType.Wood ? 1.15f : 1f) : 1f;
        public static float ControlDurationMultiplier(ElementType first, ElementType second) => (first == ElementType.Water || second == ElementType.Water) ? (Current == InnateRootType.Water ? (Awakened == InnateRootType.Water ? 1.2f : 1.1f) : Awakened == InnateRootType.Water ? 1.15f : 1f) : 1f;
        public static float BurnDamageMultiplier(ReactionResult reaction) => Awakened == InnateRootType.Fire && (reaction.First == ElementType.Fire || reaction.Second == ElementType.Fire) ? 1.15f : 1f;
        public static float AreaDamageMultiplier(ReactionResult reaction) => Awakened == InnateRootType.Earth && reaction.AreaDamageMultiplier > 0f ? 1.15f : 1f;
        public static float AreaRadiusMultiplier(ReactionResult reaction) => Awakened == InnateRootType.Earth && reaction.AreaRadius > 0f ? 1.15f : 1f;
    }

    public static class ElementCatalog
    {
        public static ElementType ForSkill(string key)
        {
            switch (key)
            {
                case "ToNearestSkill": case "CircularAttackSkill": return ElementType.Metal;
                case "BothSideSkill": return ElementType.Metal;
                case "BouncingSkill": return ElementType.Water;
                case "ChainSkill": return ElementType.Wood;
                case "CircularMotionSkill": case "FourCornerSkill": return ElementType.Earth;
                case "InsideScopeSkill": case "RandomSkill": return ElementType.Fire;
                default: return ElementType.None;
            }
        }
    }
}
