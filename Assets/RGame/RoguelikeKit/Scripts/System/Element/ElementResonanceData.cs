using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>Data for one single-element resonance. It never consumes marks.</summary>
    [Serializable]
    public sealed class ElementResonanceDefinition
    {
        public string Id;
        public string DisplayName;
        [TextArea(2, 4)] public string Description;
        public ElementType Element;
        [Min(1)] public int Threshold = 5;
        [Min(0f)] public float SourceCooldown = 0.25f;
        [Min(0f)] public float ResonanceCooldown = 4f;
        [Min(0f)] public float DirectDamageMultiplier;
        [Min(0f)] public float AreaDamageMultiplier;
        [Min(0f)] public float AreaRadius;
        [Min(0)] public int MaxAreaTargets;
        [Min(0f)] public float DotDamageMultiplier;
        [Min(0)] public int DotTicks;
        [Min(0f)] public float SlowPercent;
        [Min(0f)] public float SlowDuration;
        [Min(0f)] public float FreezeDuration;
        public string VfxPoolKey = "ReactionVfx";
        public Color PopupColor = Color.white;

        public ElementResonanceDefinition() { }

        public ElementResonanceDefinition(ElementType element, string id, string name, string description,
            int threshold, float sourceCooldown, float resonanceCooldown, float directDamageMultiplier,
            float areaDamageMultiplier = 0f, float areaRadius = 0f, int maxAreaTargets = 0,
            float dotDamageMultiplier = 0f, int dotTicks = 0, float slowPercent = 0f,
            float slowDuration = 0f, float freezeDuration = 0f, string vfxPoolKey = "ReactionVfx",
            Color? popupColor = null)
        {
            Element = element;
            Id = id;
            DisplayName = name;
            Description = description;
            Threshold = threshold;
            SourceCooldown = sourceCooldown;
            ResonanceCooldown = resonanceCooldown;
            DirectDamageMultiplier = directDamageMultiplier;
            AreaDamageMultiplier = areaDamageMultiplier;
            AreaRadius = areaRadius;
            MaxAreaTargets = maxAreaTargets;
            DotDamageMultiplier = dotDamageMultiplier;
            DotTicks = dotTicks;
            SlowPercent = slowPercent;
            SlowDuration = slowDuration;
            FreezeDuration = freezeDuration;
            VfxPoolKey = vfxPoolKey;
            PopupColor = popupColor ?? Color.white;
        }
    }

    public readonly struct ElementResonanceResult
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly ElementType Element;
        public readonly float DirectDamageMultiplier;
        public readonly float AreaDamageMultiplier;
        public readonly float AreaRadius;
        public readonly int MaxAreaTargets;
        public readonly float DotDamageMultiplier;
        public readonly int DotTicks;
        public readonly float SlowPercent;
        public readonly float SlowDuration;
        public readonly float FreezeDuration;
        public readonly string VfxPoolKey;
        public readonly Color PopupColor;

        public bool HasDirectDamage => DirectDamageMultiplier > 0f;
        public bool HasAreaDamage => AreaDamageMultiplier > 0f && AreaRadius > 0f && MaxAreaTargets > 0;
        public bool HasBurn => DotDamageMultiplier > 0f && DotTicks > 0;
        public bool HasSlow => SlowPercent > 0f && SlowDuration > 0f;
        public bool HasFreeze => FreezeDuration > 0f;

        public ElementResonanceResult(ElementResonanceDefinition definition)
        {
            Id = definition.Id;
            Name = definition.DisplayName;
            Description = definition.Description;
            Element = definition.Element;
            DirectDamageMultiplier = definition.DirectDamageMultiplier;
            AreaDamageMultiplier = definition.AreaDamageMultiplier;
            AreaRadius = definition.AreaRadius;
            MaxAreaTargets = definition.MaxAreaTargets;
            DotDamageMultiplier = definition.DotDamageMultiplier;
            DotTicks = definition.DotTicks;
            SlowPercent = definition.SlowPercent;
            SlowDuration = definition.SlowDuration;
            FreezeDuration = definition.FreezeDuration;
            VfxPoolKey = definition.VfxPoolKey;
            PopupColor = definition.PopupColor;
        }
    }

    /// <summary>
    /// Per-run single-element counters. Only accepted direct Register calls reach
    /// this runtime. Area damage, DoT, ward damage and mark-only propagation do not.
    /// </summary>
    public static class ElementResonanceRuntime
    {
        private static readonly IReadOnlyList<ElementResonanceDefinition> Defaults = new[]
        {
            new ElementResonanceDefinition(ElementType.Metal, "resonance_metal", "金鸣", "金气连击达到阈值，造成额外伤害并短暂破势。", 5, 0.25f, 4f, 0.25f, slowPercent: 20f, slowDuration: 0.5f, popupColor: new Color(1f, 0.86f, 0.3f)),
            new ElementResonanceDefinition(ElementType.Wood, "resonance_wood", "木生", "木灵积势，余韵化为持续灼伤。", 5, 0.25f, 4f, 0.2f, dotDamageMultiplier: 0.15f, dotTicks: 2, popupColor: new Color(0.3f, 1f, 0.45f)),
            new ElementResonanceDefinition(ElementType.Water, "resonance_water", "水澜", "水势回环，寒潮使目标短暂凝滞。", 5, 0.25f, 4f, 0.25f, freezeDuration: 0.15f, popupColor: new Color(0.35f, 0.8f, 1f)),
            new ElementResonanceDefinition(ElementType.Fire, "resonance_fire", "焚脉", "火势贯体，灼烧并波及近处敌群。", 5, 0.25f, 4f, 0.25f, areaDamageMultiplier: 0.3f, areaRadius: 1.25f, maxAreaTargets: 2, dotDamageMultiplier: 0.2f, dotTicks: 3, popupColor: new Color(1f, 0.25f, 0.05f)),
            new ElementResonanceDefinition(ElementType.Earth, "resonance_earth", "厚土", "土势凝实，震荡近处敌人并减缓其行动。", 5, 0.25f, 4f, 0.35f, areaDamageMultiplier: 0.35f, areaRadius: 1.35f, maxAreaTargets: 3, slowPercent: 20f, slowDuration: 0.75f, popupColor: new Color(0.82f, 0.62f, 0.28f))
        };

        private static readonly Dictionary<ElementType, int> Counts = new();
        private static readonly Dictionary<ElementType, float> CooldownUntil = new();
        private static readonly Dictionary<string, float> SourceCooldownUntil = new();
        private static ElementResonanceSO _resource;
        private static bool _resourceChecked;

        public static event Action<ElementResonanceResult, string, int> Triggered;

        public static IReadOnlyList<ElementResonanceDefinition> Definitions
        {
            get
            {
                if (!_resourceChecked)
                {
                    _resourceChecked = true;
                    _resource = Resources.Load<ElementResonanceSO>("ElementResonance");
                }
                return _resource != null && _resource.Definitions != null && _resource.Definitions.Count == 5
                    ? _resource.Definitions : Defaults;
            }
        }

        public static int CountFor(ElementType element) => Counts.TryGetValue(element, out var count) ? count : 0;

        public static ElementResonanceDefinition DefinitionFor(ElementType element) =>
            Definitions.FirstOrDefault(definition => definition != null && definition.Element == element);

        public static bool RecordDirectHit(ElementType element, string sourceKey, int targetInstanceId, out ElementResonanceResult result)
        {
            result = default;
            if (element == ElementType.None) return false;
            var definition = DefinitionFor(element);
            if (definition == null || definition.Threshold <= 0) return false;

            float now = Time.time;
            string source = string.IsNullOrEmpty(sourceKey) ? "<direct>" : sourceKey;
            string cooldownKey = targetInstanceId + "|" + (int)element + "|" + source;
            if (SourceCooldownUntil.TryGetValue(cooldownKey, out var nextSource) && now < nextSource) return false;
            SourceCooldownUntil[cooldownKey] = now + Mathf.Max(0f, definition.SourceCooldown);

            Counts.TryGetValue(element, out var count);
            count = Mathf.Min(definition.Threshold, count + 1);
            Counts[element] = count;
            if (CooldownUntil.TryGetValue(element, out var nextResonance) && now < nextResonance) return false;
            if (count < definition.Threshold) return false;

            Counts[element] = 0;
            CooldownUntil[element] = now + Mathf.Max(0f, definition.ResonanceCooldown);
            result = new ElementResonanceResult(definition);
            Triggered?.Invoke(result, sourceKey ?? string.Empty, targetInstanceId);
            return true;
        }

        public static void Reset()
        {
            Counts.Clear();
            CooldownUntil.Clear();
            SourceCooldownUntil.Clear();
        }

        // Static state survives a domain reload; clear it when Play starts so resonance counters
        // never carry over from a previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayStart() => Reset();
    }
}
