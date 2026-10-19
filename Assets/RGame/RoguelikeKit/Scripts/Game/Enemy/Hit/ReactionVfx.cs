using System.Collections;
using System.Collections.Generic;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>Short pooled reaction burst. It intentionally uses a single lightweight sprite.</summary>
    public sealed class ReactionVfx : MonoBehaviour, IPoolR
    {
        private const string DefaultPoolKey = "ReactionVfx";
        private static readonly HashSet<PoolRuntimeSO> RegisteredPools = new();
        private static Sprite _sprite;
        private SpriteRenderer _renderer;
        private PoolRuntimeSO _pool;
        private float _elapsed;
        private const float Duration = 0.35f;

        public string Key { get; set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
            if (_sprite == null) _sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            _renderer.sprite = _sprite;
            _renderer.sortingOrder = short.MaxValue - 2;
        }

        public void Request() => _elapsed = 0f;
        public void Return() { _elapsed = 0f; StopAllCoroutines(); if (_renderer != null) _renderer.enabled = false; }

        public static void Preload(PoolRuntimeSO pool, string poolKey = DefaultPoolKey, int preloadAmount = 8)
        {
            if (pool != null) EnsurePool(pool, string.IsNullOrEmpty(poolKey) ? DefaultPoolKey : poolKey, Mathf.Max(1, preloadAmount));
        }

        public static void Show(PoolRuntimeSO pool, Vector3 position, string poolKey, Color color, float radius)
        {
            if (pool == null) return;
            string key = string.IsNullOrEmpty(poolKey) ? DefaultPoolKey : poolKey;
            EnsurePool(pool, key, 8);
            var go = pool.Request(key);
            if (go == null) return;
            var vfx = go.GetComponent<ReactionVfx>();
            if (vfx == null) { pool.Return(go); return; }
            vfx._pool = pool;
            vfx.transform.position = position;
            vfx.transform.localScale = Vector3.one * Mathf.Clamp(radius <= 0f ? 0.35f : radius * 0.35f, 0.22f, 0.7f);
            vfx._renderer.color = color;
            vfx._renderer.enabled = true;
            vfx.StartCoroutine(vfx.Play(key));
        }

        private static void EnsurePool(PoolRuntimeSO pool, string key, int preloadAmount)
        {
            // PoolRuntime test instances are reset between isolated runs, but
            // this static set lives through the scene reload. Rebuild when the
            // actual pool entry is gone instead of trusting stale membership.
            if (pool.HasPool(key)) { RegisteredPools.Add(pool); return; }
            RegisteredPools.Remove(pool);
            var template = new GameObject(key + "Template");
            template.SetActive(false);
            template.AddComponent<ReactionVfx>();
            if (pool.Register(new PoolDefinition(key, new SolePool(template, Mathf.Max(1, preloadAmount))))) RegisteredPools.Add(pool);
            else Object.Destroy(template);
        }

        private IEnumerator Play(string key)
        {
            _elapsed = 0f;
            while (_elapsed < Duration)
            {
                _elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsed / Duration);
                transform.localScale *= 1f + Time.deltaTime * 1.8f;
                var color = _renderer.color;
                color.a = 1f - t;
                _renderer.color = color;
                yield return null;
            }
            _pool?.Return(gameObject);
        }
    }
}
