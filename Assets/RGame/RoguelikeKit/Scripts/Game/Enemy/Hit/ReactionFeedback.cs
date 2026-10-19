using System.Collections;
using System.Collections.Generic;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public sealed class ReactionFeedback : MonoBehaviour, IPoolR
    {
        private static readonly string PoolKey = "ReactionFeedback";
        private TextMesh _text;
        private PoolRuntimeSO _pool;
        private float _elapsed;
        private const float Duration = 0.7f;
        private static readonly HashSet<PoolRuntimeSO> RegisteredPools = new();

        public string Key { get; set; }
        private void Awake() => _text = GetComponent<TextMesh>();
        public void Request() { _elapsed = 0f; }
        public void Return() { _elapsed = 0f; StopAllCoroutines(); transform.localScale = Vector3.one; }

        public static void Preload(PoolRuntimeSO pool, int preloadAmount = 8)
        {
            if (pool != null) EnsurePool(pool, Mathf.Max(1, preloadAmount));
        }

        public static void Show(PoolRuntimeSO pool, Vector3 position, string name, Color color, bool emphasized = false)
        {
            if (pool == null) return;
            EnsurePool(pool, 8);
            var go = pool.Request(PoolKey);
            if (go == null) return;
            var feedback = go.GetComponent<ReactionFeedback>();
            feedback._pool = pool;
            feedback._text.text = name;
            feedback._text.color = color;
            feedback.transform.position = position + Vector3.up * 0.8f;
            feedback.transform.localScale = Vector3.one * (emphasized ? 1.25f : 1f);
            feedback.StartCoroutine(feedback.Play());
        }

        private static void EnsurePool(PoolRuntimeSO pool, int preloadAmount)
        {
            // A test pool can be torn down and recreated while the static
            // registration set survives a scene reload. The live pool map is
            // authoritative; stale set membership must not suppress rebuild.
            if (pool.HasPool(PoolKey))
            {
                RegisteredPools.Add(pool);
                return;
            }
            RegisteredPools.Remove(pool);
            var template = new GameObject("ReactionFeedbackTemplate");
            template.SetActive(false);
            template.AddComponent<ReactionFeedback>();
            var text = template.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 32;
            text.characterSize = 0.08f;
            var renderer = template.GetComponent<MeshRenderer>();
            renderer.sortingOrder = short.MaxValue - 1;
            if (pool.Register(new PoolDefinition(PoolKey, new SolePool(template, Mathf.Max(1, preloadAmount)))))
                RegisteredPools.Add(pool);
            else
                Object.Destroy(template);
        }

        private IEnumerator Play()
        {
            _elapsed = 0f;
            var start = transform.position;
            while (_elapsed < Duration)
            {
                _elapsed += Time.deltaTime;
                transform.position = start + Vector3.up * (_elapsed * 0.35f);
                var color = _text.color;
                color.a = 1f - Mathf.Clamp01(_elapsed / Duration);
                _text.color = color;
                yield return null;
            }
            _pool?.Return(gameObject);
        }
    }
}
