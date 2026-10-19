using System;
using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace RGame.RoguelikeKit
{
    public class EnemyHit : MonoBehaviour
    {
        [SerializeField] private HitAnimationSO _hitAnimationSO;
        [SerializeField] private DissolveAnimationSO _dissolveAnimationSO;
        [SerializeField] private SpriteRenderer _spriteRenderers;
        [SerializeField] private PoolRuntimeSO poolRuntimeSo;
        private CommonStatRuntimeSO _selfStat;
        private ElementMarkController _elementMarks;
        private ElementMarkView _elementMarkView;
        private ElementReactionEffects _reactionEffects;
        private CorruptionWard _corruptionWard;
        private SpawnSetRole _pendingWardRole;
        private bool _wardConfigurationPending;
        private bool _isDead;
        private static readonly Collider2D[] AreaBuffer = new Collider2D[32];
        private static readonly EnemyHit[] AreaHits = new EnemyHit[8];

        public UnityAction OnDeath { get; set; }
        public UnityAction OnDeathEnd { get; set; }

        private void Awake()
        {
            _elementMarks = GetComponent<ElementMarkController>();
            _elementMarkView = GetComponent<ElementMarkView>();
            _reactionEffects = GetComponent<ElementReactionEffects>() ?? GetComponentInParent<ElementReactionEffects>();
            _corruptionWard = GetComponent<CorruptionWard>();
        }

        private void Start()
        {
            // Start runs after the enemy pool has finished initializing, so
            // prewarming cannot recursively initialize PoolRuntimeSO.
            ReactionFeedback.Preload(poolRuntimeSo);
            ReactionVfx.Preload(poolRuntimeSo);
        }

        private void OnEnable()
        {
            _elementMarks = _elementMarks != null ? _elementMarks : GetComponent<ElementMarkController>();
            _elementMarkView = _elementMarkView != null ? _elementMarkView : GetComponent<ElementMarkView>();
            _reactionEffects = _reactionEffects != null ? _reactionEffects : GetComponent<ElementReactionEffects>() ?? GetComponentInParent<ElementReactionEffects>();
            _corruptionWard = _corruptionWard != null ? _corruptionWard : GetComponent<CorruptionWard>();
            _dissolveAnimationSO?.ResetMaterial(_spriteRenderers);
            if (_spriteRenderers != null) _hitAnimationSO?.ResetMaterial(_spriteRenderers.material);
        }

        public void SetStat(CommonStatRuntimeSO stat)
        {
            _selfStat = stat;
            if (_wardConfigurationPending) ApplyWardConfiguration();
            OnDeathEnd -= DieEnd;
            OnDeathEnd += DieEnd;
        }

        private void OnDestroy()
        {
            OnDeathEnd -= DieEnd;
        }

        public void Hit(int damage) => HitInternal(damage, Color.white);
        public void Hit(int damage, Color damageColor) => HitInternal(damage, damageColor);

        /// <summary>
        /// Atomic elemental hit: register marks and resolve the pair before
        /// applying ordinary and reaction damage in one transaction.
        /// </summary>
        public void ReceiveElementalHit(int damage, ElementType element, string sourceKey)
        {
            if (_isDead || damage <= 0) return;
            ReactionResult reaction = default;
            ElementResonanceResult resonance = default;
            bool reacted = _elementMarks != null && _elementMarks.Register(element, sourceKey, out reaction, out resonance);
            if (reacted)
            {
                int reactionDamage = Mathf.RoundToInt(damage * reaction.DirectDamageMultiplier * InnateRootRuntime.ReactionDamageMultiplier(reaction.First, reaction.Second) * TribulationRewardRuntime.DirectDamageMultiplier(reaction) * ShentongRuntime.ReactionDamageMultiplier(reaction));
                HitInternalCombined(damage, Mathf.Max(0, reactionDamage), reaction.PopupColor);
                // Feedback/VFX are still emitted when the combined hit is
                // lethal; ResolveReaction itself skips persistent effects on
                // a dead target.
                ResolveReaction(reaction, damage, sourceKey);
                if (!string.IsNullOrEmpty(resonance.Id)) ResolveResonance(resonance, damage);
                return;
            }
            HitInternal(damage, Color.white);
            if (!string.IsNullOrEmpty(resonance.Id)) ResolveResonance(resonance, damage);
        }

        /// <summary>Compatibility overload for existing skill/projectile callers.</summary>
        public void Hit(int damage, ElementType element, string sourceKey) => ReceiveElementalHit(damage, element, sourceKey);

        /// <summary>Adds only an element mark. It never creates a zero damage popup.</summary>
        public void ApplyMarkOnly(ElementType element, string sourceKey) => _elementMarks?.ApplyMarkOnly(element, sourceKey);

        public void ClearElementMarks() => _elementMarks?.Clear();
        public void ConfigureCorruptionWard(SpawnSetRole role)
        {
            _pendingWardRole = role;
            _wardConfigurationPending = true;
            if (_selfStat == null) return;
            ApplyWardConfiguration();
        }

        private void ApplyWardConfiguration()
        {
            _wardConfigurationPending = false;
            var role = _pendingWardRole;
            if (role == SpawnSetRole.Normal)
            {
                _corruptionWard?.ResetState();
                return;
            }
            if (_corruptionWard == null) _corruptionWard = gameObject.AddComponent<CorruptionWard>();
            if (GetComponent<CorruptionWardView>() == null) gameObject.AddComponent<CorruptionWardView>();
            _corruptionWard.Configure(role, _selfStat != null ? _selfStat.GetValue("HP") : 0);
            NotifyWardState();
        }
        public CorruptionWard Corruption => _corruptionWard;
        public PoolRuntimeSO PoolRuntime => poolRuntimeSo;
        public ElementMarkController ElementMarks => _elementMarks;

        internal void ApplyReactionDamageOnly(int damage, Color color)
        {
            if (_isDead || damage <= 0) return;
            HitInternal(damage, color, true);
        }

        internal void ApplyResonanceDamageOnly(int damage, Color color)
        {
            if (_isDead || damage <= 0) return;
            HitInternal(damage, color);
        }

        private void HitInternal(int damage, Color damageColor, bool reactionDamage = false)
        {
            HitInternalCombined(reactionDamage ? 0 : damage, reactionDamage ? damage : 0, damageColor);
        }

        private void HitInternalCombined(int ordinaryDamage, int reactionDamage, Color damageColor)
        {
            if (_isDead || _selfStat == null || ordinaryDamage <= 0 && reactionDamage <= 0) return;
            bool broke = false;
            int ordinaryHealth = _corruptionWard != null ? _corruptionWard.Absorb(ordinaryDamage, false, out broke) : ordinaryDamage;
            bool reactionBroke = false;
            int reactionHealth = _corruptionWard != null ? _corruptionWard.Absorb(reactionDamage, true, out reactionBroke) : reactionDamage;
            broke |= reactionBroke;
            if (broke)
            {
                string wardText = _corruptionWard != null && _corruptionWard.Role == SpawnSetRole.FinalBoss ? "护体核心暴露" : "破染";
                ReactionFeedback.Show(poolRuntimeSo, transform.position, wardText, new Color(0.72f, 0.18f, 0.95f), _elementMarkView != null && _elementMarkView.LargeDisplay);
                ReactionVfx.Show(poolRuntimeSo, transform.position, "ReactionVfx", new Color(0.28f, 0.03f, 0.38f), 1.25f);
            }
            int damage = Mathf.CeilToInt((ordinaryHealth + reactionHealth) * (_corruptionWard != null ? _corruptionWard.DamageMultiplier : 1f));
            if (damage <= 0) return;
            _selfStat.ModifyValue("HP", damage * -1);

            if (poolRuntimeSo != null)
            {
                var popup = poolRuntimeSo.Request("DamagePopup");
                if (popup != null) popup.GetComponent<DamagePopup>()?.Show(transform.position, damage, damageColor);
            }

            NotifyWardState();

            if (_selfStat.GetValue("HP") <= 0)
            {
                _isDead = true;
                OnDeath?.Invoke();
                _dissolveAnimationSO?.Play(_spriteRenderers, OnDeathEnd);
            }
            else
            {
                _hitAnimationSO?.Play(_spriteRenderers);
            }
        }

        private void NotifyWardState()
        {
            if (_corruptionWard == null) return;
            var view = GetComponent<CorruptionWardView>();
            if (view != null) view.Refresh(_corruptionWard);
        }

        private void ResolveReaction(ReactionResult reaction, int baseDamage, string sourceKey)
        {
            _elementMarks?.NotifyReaction(reaction, sourceKey);
            Vector3 position = transform.position;
            ReactionFeedback.Show(poolRuntimeSo, position, reaction.Name, reaction.PopupColor, _elementMarkView != null && _elementMarkView.LargeDisplay);
            float areaRadius = reaction.AreaRadius * TribulationRewardRuntime.AreaRadiusMultiplier(reaction) * InnateRootRuntime.AreaRadiusMultiplier(reaction) * ShentongRuntime.AreaRadiusMultiplier(reaction);
            ReactionVfx.Show(poolRuntimeSo, position, reaction.VfxPoolKey, reaction.PopupColor, areaRadius);

            if (_isDead) return;
            float controlScale = InnateRootRuntime.ControlDurationMultiplier(reaction.First, reaction.Second);
            if (_reactionEffects != null)
            {
                if (reaction.Freeze) _reactionEffects.Freeze(TribulationRewardRuntime.FreezeDuration(reaction) * controlScale, ShentongRuntime.FreezeFollowupDuration(reaction));
                else if (reaction.Slow) _reactionEffects.Slow(reaction.SlowPercent,
                    reaction.SlowDuration * controlScale + ShentongRuntime.SlowDurationBonus(reaction));
                if (reaction.Burn)
                {
                    int dotDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * reaction.DotDamageMultiplier * InnateRootRuntime.BurnDamageMultiplier(reaction)));
                    _reactionEffects.Burn(dotDamage, TribulationRewardRuntime.DotTicks(reaction) + ShentongRuntime.DotTickBonus(reaction));
                }
            }

            if (areaRadius > 0f && reaction.AreaDamageMultiplier > 0f)
            {
                ApplyAreaReaction(reaction, baseDamage, position, areaRadius);
                if (ShentongRuntime.Has("shentong_stone") && reaction.First == ElementType.Metal && reaction.Second == ElementType.Earth ||
                    ShentongRuntime.Has("shentong_stone") && reaction.First == ElementType.Earth && reaction.Second == ElementType.Metal)
                    ApplyAreaReaction(reaction, Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.35f)), position, areaRadius * 0.65f);
            }
            if (reaction.SpreadMarkCount > 0)
                SpreadMarks(reaction.SpreadMarkCount, reaction.First, sourceKey, position);
        }

        private void ResolveResonance(ElementResonanceResult resonance, int baseDamage)
        {
            ReactionFeedback.Show(poolRuntimeSo, transform.position, resonance.Name, resonance.PopupColor, _elementMarkView != null && _elementMarkView.LargeDisplay);
            ReactionVfx.Show(poolRuntimeSo, transform.position, resonance.VfxPoolKey, resonance.PopupColor, resonance.AreaRadius);
            if (_isDead) return;

            int directDamage = Mathf.RoundToInt(baseDamage * resonance.DirectDamageMultiplier);
            if (directDamage > 0) ApplyResonanceDamageOnly(directDamage, resonance.PopupColor);
            if (_isDead) return;

            if (_reactionEffects != null)
            {
                if (resonance.HasFreeze) _reactionEffects.Freeze(resonance.FreezeDuration);
                else if (resonance.HasSlow) _reactionEffects.Slow(resonance.SlowPercent, resonance.SlowDuration);
                if (resonance.HasBurn)
                {
                    int dotDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * resonance.DotDamageMultiplier));
                    _reactionEffects.Burn(dotDamage, resonance.DotTicks);
                }
            }

            if (resonance.HasAreaDamage)
            {
                int count = Physics2D.OverlapCircleNonAlloc(transform.position, resonance.AreaRadius, AreaBuffer);
                int max = Mathf.Min(resonance.MaxAreaTargets, AreaHits.Length);
                int seenCount = 0;
                int areaDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * resonance.AreaDamageMultiplier));
                for (int i = 0; i < count && seenCount < max; i++)
                {
                    var other = AreaBuffer[i]?.GetComponentInParent<EnemyHit>();
                    if (other == null || other == this) continue;
                    bool duplicate = false;
                    for (int j = 0; j < seenCount; j++) if (AreaHits[j] == other) { duplicate = true; break; }
                    if (duplicate) continue;
                    AreaHits[seenCount++] = other;
                    other.ApplyResonanceDamageOnly(areaDamage, resonance.PopupColor);
                }
                for (int i = 0; i < seenCount; i++) AreaHits[i] = null;
            }
        }

        private void ApplyAreaReaction(ReactionResult reaction, int baseDamage, Vector3 position, float areaRadius)
        {
            int count = Physics2D.OverlapCircleNonAlloc(position, areaRadius, AreaBuffer);
            int max = Mathf.Min(reaction.MaxAreaTargets, AreaHits.Length);
            int seenCount = 0;
            int areaDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * reaction.AreaDamageMultiplier * InnateRootRuntime.AreaDamageMultiplier(reaction) * ShentongRuntime.AreaDamageMultiplier(reaction)));
            for (int i = 0; i < count && seenCount < max; i++)
            {
                var other = AreaBuffer[i]?.GetComponentInParent<EnemyHit>();
                if (other == null || other == this) continue;
                bool duplicate = false;
                for (int j = 0; j < seenCount; j++) if (AreaHits[j] == other) { duplicate = true; break; }
                if (duplicate) continue;
                AreaHits[seenCount++] = other;
                other.ApplyReactionDamageOnly(areaDamage, reaction.PopupColor);
                if (reaction.AreaSlow && other._reactionEffects != null)
                    other._reactionEffects.Slow(reaction.SlowPercent, reaction.SlowDuration + ShentongRuntime.AreaSlowDurationBonus(reaction));
            }
            for (int i = 0; i < seenCount; i++) AreaHits[i] = null;
        }

        private void SpreadMarks(int count, ElementType element, string sourceKey, Vector3 position)
        {
            int colliderCount = Physics2D.OverlapCircleNonAlloc(position, 1.75f, AreaBuffer);
            int applied = 0;
            for (int i = 0; i < colliderCount && applied < count; i++)
            {
                var other = AreaBuffer[i]?.GetComponentInParent<EnemyHit>();
                if (other == null || other == this) continue;
                other.ApplyMarkOnly(element, sourceKey + ":spread");
                applied++;
            }
        }

        private void DieEnd()
        {
            _isDead = false;
        }
    }
}