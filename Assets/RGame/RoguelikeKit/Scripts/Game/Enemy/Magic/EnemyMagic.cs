using System;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class EnemyMagic : MonoBehaviour, IPoolR
    {
        [SerializeField] private GlobalConfigSO globalConfig;
        [SerializeField] private PoolRuntimeSO poolRuntime;
        [SerializeField] private float velocity;

        [Tooltip("Point the visual forward (+X) at the launch direction. The rotation is applied once in Init and never changes afterwards.")]
        [SerializeField] private bool alignVisualToLaunchDirection = true;

        private int _damage;
        private Vector3 _moveDir;
        private float _timer;
        private readonly float _delayReturn = 5f;
        
        public void Init(int damage,Vector3 moveDir)
        {
            _damage = damage;
            _moveDir = moveDir.sqrMagnitude > 0.0001f ? moveDir.normalized : Vector3.zero;
            _timer = 0f;

            // Face the target once on launch. The visual (and the polygon hitbox)
            // are authored so the needle tip points along local +X, so rotating the
            // root to the launch angle is enough. Direction stays fixed after launch.
            if (alignVisualToLaunchDirection && _moveDir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(_moveDir.y, _moveDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
        
        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;

            if (_timer < 0.08f)
            {
                _moveDir = (globalConfig.GlobalPlayer.transform.position - transform.position).normalized;
            }
            
            transform.position += velocity * Time.fixedDeltaTime * _moveDir;

            if (_timer >= _delayReturn)
            {
                poolRuntime.Return(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("PlayerHit"))
            {
                other.GetComponent<PlayerHit>().OnHit(_damage);
                poolRuntime.Return(gameObject);
            }
        }

        public string Key { get; set; }
        public void Request()
        {
        }

        public void Return()
        {
            _timer = 0;
        }
    }
}
