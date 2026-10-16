using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Lightweight presentation motion for the generated character sprite.
    /// Gameplay movement and collision remain on the original Fighter hierarchy.
    /// </summary>
    public sealed class CultivationCharacterAnimator : MonoBehaviour
    {
        [SerializeField] private float _idleBobAmplitude = 0.012f;
        [SerializeField] private float _moveBobAmplitude = 0.024f;
        [SerializeField] private float _bobSpeed = 5f;
        [SerializeField] private float _moveLeanAngle = 2.5f;

        private Rigidbody2D _body;
        private Vector3 _baseLocalPosition;
        private float _phase;

        private void Awake()
        {
            _body = GetComponentInParent<Rigidbody2D>();
            _baseLocalPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            _phase = Random.Range(0f, Mathf.PI * 2f);
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            float speed01 = _body == null ? 0f : Mathf.Clamp01(_body.linearVelocity.magnitude / 2.5f);
            float bob = Mathf.Sin(Time.time * _bobSpeed + _phase) * Mathf.Lerp(_idleBobAmplitude, _moveBobAmplitude, speed01);
            transform.localPosition = _baseLocalPosition + Vector3.up * bob;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * _bobSpeed + _phase) * _moveLeanAngle * speed01);
        }
    }
}
