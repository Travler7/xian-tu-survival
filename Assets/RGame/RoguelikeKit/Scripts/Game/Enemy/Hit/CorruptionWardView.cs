using UnityEngine;

namespace RGame.RoguelikeKit
{
    public sealed class CorruptionWardView : MonoBehaviour
    {
        private SpriteRenderer _bar;
        private static Sprite _sprite;

        private void Awake()
        {
            if (_sprite == null) _sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f));
            var go = new GameObject("WardBar");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(-0.5f, 1.25f, 0f);
            go.transform.localScale = new Vector3(1f, 0.08f, 1f);
            _bar = go.AddComponent<SpriteRenderer>();
            _bar.sprite = _sprite;
            _bar.color = new Color(0.5f, 0.05f, 0.65f, 0.9f);
            _bar.sortingOrder = 60;
            _bar.enabled = false;
        }

        public void Refresh(CorruptionWard ward)
        {
            if (_bar == null || ward == null) return;
            _bar.enabled = ward.Enabled && ward.Current > 0f;
            if (_bar.enabled) _bar.transform.localScale = new Vector3(Mathf.Clamp01(ward.Current / Mathf.Max(1f, ward.Maximum)), 0.08f, 1f);
        }
    }
}
