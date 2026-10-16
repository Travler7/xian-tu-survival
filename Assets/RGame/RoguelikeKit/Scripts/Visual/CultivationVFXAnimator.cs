using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Presentation-only animation for cultivation skill VFX children.
    /// It never touches damage, collision, pooling or skill lifetime.
    /// </summary>
    public sealed class CultivationVFXAnimator : MonoBehaviour
    {
        [SerializeField] private Transform _impact;
        [SerializeField] private Transform _area;
        [SerializeField] private float _pulseSpeed = 4f;
        [SerializeField] private float _rotationSpeed = 45f;

        private Vector3 _impactScale = Vector3.one;
        private Vector3 _areaScale = Vector3.one;
        private float _phase;

        private void Awake()
        {
            if (_impact == null) _impact = transform.Find("Impact");
            if (_area == null) _area = transform.Find("TrailOrArea");
            if (_impact != null) _impactScale = _impact.localScale;
            if (_area != null) _areaScale = _area.localScale;
        }

        private void OnEnable()
        {
            _phase = Random.Range(0f, Mathf.PI * 2f);
            if (_impact != null) _impact.localScale = _impactScale;
            if (_area != null) _area.localScale = _areaScale;
        }

        private void Update()
        {
            float pulse = 0.88f + Mathf.Sin(Time.time * _pulseSpeed + _phase) * 0.12f;
            if (_impact != null) _impact.localScale = _impactScale * pulse;
            if (_area != null)
            {
                _area.localScale = _areaScale * (0.96f + pulse * 0.04f);
                _area.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);
            }
        }
    }
}
