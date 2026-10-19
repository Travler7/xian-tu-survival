using System.Collections;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>Pre-attached pooled effect runner for reaction controls and damage-over-time.</summary>
    public sealed class ElementReactionEffects : MonoBehaviour
    {
        private BaseEnemy _owner;
        private Coroutine _controlRoutine;
        private Coroutine _burnRoutine;
        private int _speedDelta;
        private static readonly Collider2D[] EchoBuffer = new Collider2D[32];

        private void Awake() => _owner = GetComponentInParent<BaseEnemy>();

        private void OnDisable()
        {
            StopAllCoroutines();
            _controlRoutine = null;
            _burnRoutine = null;
            RestoreSpeed();
        }

        public void Slow(float percent, float duration)
        {
            StartControl(percent, duration, 0f);
        }

        private void StartControl(float percent, float duration, float followupSlowDuration)
        {
            if (_controlRoutine != null) StopCoroutine(_controlRoutine);
            RestoreSpeed();
            _controlRoutine = StartCoroutine(SlowRoutine(percent, duration, followupSlowDuration));
        }

        public void Freeze(float duration, float followupSlowDuration = 0f) => StartControl(100f, duration, followupSlowDuration);

        public void Burn(int damage, int ticks)
        {
            if (_burnRoutine != null) StopCoroutine(_burnRoutine);
            _burnRoutine = StartCoroutine(BurnRoutine(Mathf.Max(1, damage), Mathf.Max(1, ticks)));
        }

        private IEnumerator SlowRoutine(float percent, float duration, float followupSlowDuration)
        {
            if (_owner == null || _owner.StatRuntime == null) yield break;
            int speed = _owner.StatRuntime.GetValue("Speed");
            _speedDelta = Mathf.Max(0, Mathf.RoundToInt(speed * percent * 0.01f));
            _owner.StatRuntime.ModifyValue("Speed", -_speedDelta);
            yield return new WaitForSeconds(Mathf.Max(0.01f, duration));
            RestoreSpeed();
            if (followupSlowDuration > 0f)
            {
                ApplyFreezeEcho(followupSlowDuration);
            }
            _controlRoutine = null;
        }

        private void ApplyFreezeEcho(float duration)
        {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, 1.5f, EchoBuffer);
            for (int i = 0; i < count; i++)
            {
                var enemy = EchoBuffer[i] != null ? EchoBuffer[i].GetComponentInParent<BaseEnemy>() : null;
                if (enemy == null || enemy == _owner) continue;
                var effects = enemy.GetComponentInChildren<ElementReactionEffects>();
                effects?.Slow(35f, duration);
            }
        }

        private IEnumerator BurnRoutine(int damage, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                yield return new WaitForSeconds(1f);
                if (_owner == null || _owner.MyHit == null) break;
                _owner.MyHit.Hit(damage, new Color(1f, 0.3f, 0.08f));
            }
            _burnRoutine = null;
        }

        private void RestoreSpeed()
        {
            if (_owner != null && _owner.StatRuntime != null && _speedDelta != 0)
                _owner.StatRuntime.ModifyValue("Speed", _speedDelta);
            _speedDelta = 0;
        }
    }
}
