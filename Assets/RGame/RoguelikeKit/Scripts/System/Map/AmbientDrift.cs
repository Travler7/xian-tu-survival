using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Cheap drifting ambience (falling leaves / spirit motes / mist). Particles are
    /// plain SpriteRenderers that wrap around the camera, so the pool stays tiny and
    /// nothing is allocated per frame.
    /// </summary>
    public class AmbientDrift : MonoBehaviour
    {
        private Sprite[] _sprites;
        private Transform[] _items;
        private SpriteRenderer[] _renderers;
        private float[] _fallSpeed;
        private float[] _swaySpeed;
        private float[] _swayPhase;
        private float[] _spin;
        private float[] _baseAlpha;
        private Vector2 _area = new Vector2(36f, 22f);
        private Transform _target;
        private bool _ready;

        public void Configure(
            Sprite[] sprites,
            int count,
            Vector2 scaleRange,
            Vector2 speedRange,
            Vector2 swayRange,
            float alpha,
            Vector2 area)
        {
            _sprites = sprites;
            _area = new Vector2(Mathf.Max(6f, area.x), Mathf.Max(6f, area.y));

            var camera = Camera.main;
            _target = camera != null ? camera.transform : transform;

            count = Mathf.Max(0, count);
            _items = new Transform[count];
            _renderers = new SpriteRenderer[count];
            _fallSpeed = new float[count];
            _swaySpeed = new float[count];
            _swayPhase = new float[count];
            _spin = new float[count];
            _baseAlpha = new float[count];

            var half = _area * 0.5f;

            for (var i = 0; i < count; i++)
            {
                var go = new GameObject("Ambient", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);

                var renderer = go.GetComponent<SpriteRenderer>();
                renderer.sprite = sprites != null && sprites.Length > 0
                    ? sprites[Random.Range(0, sprites.Length)]
                    : null;
                renderer.sortingLayerName = "Map";
                renderer.sortingOrder = short.MaxValue;
                renderer.flipX = Random.value < 0.5f;

                var scale = Random.Range(
                    Mathf.Min(scaleRange.x, scaleRange.y),
                    Mathf.Max(scaleRange.x, scaleRange.y));
                go.transform.localScale = new Vector3(scale, scale, 1f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                _baseAlpha[i] = alpha * Random.Range(0.6f, 1f);
                var color = Color.white;
                color.a = _baseAlpha[i];
                renderer.color = color;

                _fallSpeed[i] = Random.Range(
                    Mathf.Min(speedRange.x, speedRange.y),
                    Mathf.Max(speedRange.x, speedRange.y));
                _swaySpeed[i] = Random.Range(
                    Mathf.Min(swayRange.x, swayRange.y),
                    Mathf.Max(swayRange.x, swayRange.y));
                _swayPhase[i] = Random.Range(0f, Mathf.PI * 2f);
                _spin[i] = Random.Range(-30f, 30f);

                var center = _target != null ? _target.position : transform.position;
                go.transform.position = new Vector3(
                    center.x + Random.Range(-half.x, half.x),
                    center.y + Random.Range(-half.y, half.y),
                    0f);

                _items[i] = go.transform;
                _renderers[i] = renderer;
            }

            _ready = true;
        }

        private void Update()
        {
            if (!_ready || _items == null) return;

            var dt = Time.deltaTime;
            if (dt <= 0f) return;

            var center = _target != null ? _target.position : transform.position;
            var half = _area * 0.5f;

            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                if (item == null) continue;

                _swayPhase[i] += dt * 1.35f;

                var position = item.position;
                position.x += (_swaySpeed[i] + Mathf.Sin(_swayPhase[i]) * 0.35f) * dt;
                position.y -= _fallSpeed[i] * dt;
                position.z = 0f;

                var wrapped = false;
                if (position.x - center.x > half.x) { position.x -= _area.x; wrapped = true; }
                else if (position.x - center.x < -half.x) { position.x += _area.x; wrapped = true; }

                if (position.y - center.y > half.y) { position.y -= _area.y; wrapped = true; }
                else if (position.y - center.y < -half.y) { position.y += _area.y; wrapped = true; }

                item.position = position;

                if (_spin[i] != 0f) item.Rotate(0f, 0f, _spin[i] * dt);

                if (wrapped && _renderers[i] != null)
                {
                    var color = _renderers[i].color;
                    color.a = _baseAlpha[i];
                    _renderers[i].color = color;
                }
                else if (_renderers[i] != null)
                {
                    // gentle twinkle so motes read as living light
                    var color = _renderers[i].color;
                    color.a = _baseAlpha[i] * (0.8f + 0.2f * Mathf.Sin(_swayPhase[i] * 1.7f + i));
                    _renderers[i].color = color;
                }
            }
        }
    }
}
