using System;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>Runtime corruption shield for elite and final-boss targets.</summary>
    public sealed class CorruptionWard : MonoBehaviour
    {
        public const float EliteRatio = 0.25f;
        public const float BossRatio = 0.35f;
        public const float ReactionMultiplier = 2f;
        public const float BreakDuration = 2.5f;
        public const float Vulnerability = 1.25f;
        private static CorruptionWardConfigSO _config;

        public float Current { get; private set; }
        public float Maximum { get; private set; }
        public bool Enabled { get; private set; }
        public bool Broken { get; private set; }
        public float BrokenUntil { get; private set; }
        public SpawnSetRole Role { get; private set; }
        public event Action BrokenEvent;
        private SpriteRenderer _visual;
        private SpriteRenderer _sourceRenderer;

        private void Awake()
        {
            _sourceRenderer = GetComponentInParent<BaseEnemy>()?.MySpriteRenderer;
            var visual = new GameObject("CorruptionWardVisual");
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 1.08f;
            _visual = visual.AddComponent<SpriteRenderer>();
            _visual.sprite = _sourceRenderer != null ? _sourceRenderer.sprite : null;
            _visual.color = new Color(0.25f, 0.02f, 0.35f, 0.22f);
            _visual.sortingOrder = -1;
            _visual.enabled = false;
        }

        public void Configure(SpawnSetRole role, int maxHp)
        {
            if (_config == null) _config = Resources.Load<CorruptionWardConfigSO>("CorruptionWardConfig");
            float ratio = role == SpawnSetRole.FinalBoss ? (_config != null ? _config.BossWardRatio : BossRatio) : role == SpawnSetRole.Elite ? (_config != null ? _config.EliteWardRatio : EliteRatio) : 0f;
            Enabled = ratio > 0f && maxHp > 0;
            Role = role;
            if (_sourceRenderer == null) _sourceRenderer = GetComponentInParent<BaseEnemy>()?.MySpriteRenderer;
            if (_visual != null && _sourceRenderer != null)
            {
                _visual.sprite = _sourceRenderer.sprite;
                _visual.sortingLayerID = _sourceRenderer.sortingLayerID;
                _visual.sortingOrder = _sourceRenderer.sortingOrder - 1;
            }
            Maximum = Enabled ? Mathf.Max(1f, maxHp * ratio) : 0f;
            Current = Maximum;
            Broken = false;
            BrokenUntil = 0f;
            if (_visual != null) _visual.enabled = false;
        }

        public void ResetState()
        {
            Enabled = false;
            Current = 0f;
            Maximum = 0f;
            Broken = false;
            BrokenUntil = 0f;
            Role = SpawnSetRole.Normal;
            if (_visual != null) _visual.enabled = false;
        }

        public int Absorb(int damage, bool reaction, out bool broke)
        {
            broke = false;
            if (damage <= 0 || !Enabled) return damage;
            if (Broken && Time.time >= BrokenUntil) Broken = false;
            if (Broken) return damage;

            float reactionMultiplier = _config != null ? _config.ReactionDamageMultiplier : ReactionMultiplier;
            float shieldDamage = damage * (reaction ? reactionMultiplier : 1f);
            float consumed = Mathf.Min(Current, shieldDamage);
            Current -= consumed;
            float remainingShieldDamage = shieldDamage - consumed;
            int healthDamage = reaction
                ? Mathf.CeilToInt(remainingShieldDamage / reactionMultiplier)
                : Mathf.CeilToInt(remainingShieldDamage);
            if (Current <= 0f)
            {
                Current = 0f;
                Broken = true;
                BrokenUntil = Time.time + (_config != null ? _config.BrokenDuration : BreakDuration);
                broke = true;
                BrokenEvent?.Invoke();
                if (_visual != null) _visual.color = new Color(0.7f, 0.05f, 0.9f, 0.36f);
            }
            return healthDamage;
        }

        public float DamageMultiplier
        {
            get
            {
                if (!Broken) return 1f;
                if (Time.time >= BrokenUntil)
                {
                    Broken = false;
                    if (_visual != null) _visual.color = new Color(0.25f, 0.02f, 0.35f, 0.22f);
                    return 1f;
                }
                return _config != null ? _config.VulnerabilityMultiplier : Vulnerability;
            }
        }

        private void LateUpdate()
        {
            if (_visual == null) return;
            _visual.enabled = (Enabled && Current > 0f) || (Broken && Time.time < BrokenUntil);
            if (Enabled && Current > 0f)
            {
                Color color = _visual.color;
                color.a = Mathf.Lerp(0.08f, 0.24f, Current / Mathf.Max(1f, Maximum));
                _visual.color = color;
            }
        }
    }
}