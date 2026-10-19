using System;
using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace RGame.RoguelikeKit
{
    public class SkillBase : MonoBehaviour, IPoolR
    {
        [SerializeField] protected AddBuffEvent _addBuffEvent;
        [SerializeField] protected BaseBuff _baseBuff;
        
        public UnityAction<SkillBase> OnDeath { get; set; }
        
        public string Key { get; set; }
        private ElementType _configuredElement = ElementType.None;

        public void ConfigureFrom(SkillDataSO data)
        {
            if (data == null) return;
            Key = data.Key;
            _configuredElement = data.PrimaryElement;
        }

        public void Request()
        {
            SkillRequest();
        }

        public void Return()
        {
            
        }
        
        public virtual void SkillRequest(){}

        protected void Attack(BaseEnemy meleeEnemy)
        {
            if (_addBuffEvent != null && _baseBuff != null)
            {
                _addBuffEvent.RaiseEvent(meleeEnemy, _baseBuff);
            }
        }

        protected void HitEnemy(EnemyHit enemyHit, int damage)
        {
            var element = _configuredElement != ElementType.None ? _configuredElement : ElementCatalog.ForSkill(Key);
            enemyHit?.Hit(damage, element, Key);
        }

        protected void HitEnemy(EnemyHit enemyHit, int damage, ElementType element)
        {
            enemyHit?.Hit(damage, element, Key);
        }
    }
}
