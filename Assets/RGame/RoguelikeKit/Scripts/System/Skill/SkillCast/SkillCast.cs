using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class SkillCast : MonoBehaviour
    {
        public string Key;
        [SerializeField] protected PoolRuntimeSO _pool;
        [SerializeField] protected CommonStatRuntimeSO _stat;
        [SerializeField] protected GlobalConfigSO _globalConfig;
        [SerializeField] protected EnemySystem _enemySystem;
        [SerializeField] private Vector3 _originLocalScale;
        
        protected SkillDataSO _mySkillData;

        /// <summary>
        /// Returns the presentation renderer for a skill. Cultivation skill prefabs
        /// keep the root renderer as a compatibility shell for the original logic,
        /// while the visible artwork lives under CultivationMain.
        /// </summary>
        protected static SpriteRenderer GetPrimarySpriteRenderer(GameObject skillObject)
        {
            if (skillObject == null) return null;

            var main = skillObject.transform.Find("CultivationMain");
            if (main != null && main.TryGetComponent(out SpriteRenderer mainRenderer))
                return mainRenderer;

            return skillObject.GetComponent<SpriteRenderer>() ?? skillObject.GetComponentInChildren<SpriteRenderer>();
        }
        
        public virtual void Cast(SkillDataSO skillData)
        {
            _originLocalScale = skillData.SkillPrefab.transform.localScale;
            _mySkillData = skillData;
        }

        public virtual void MixSkill()
        {
            _mySkillData.CurrentState = SkillCurrentState.Mixed;
        }
        
        protected void HandleSkillDeath(SkillBase skill)
        {
            if (skill != null)
            {
                skill.transform.localScale = _originLocalScale;
                var renderer = GetPrimarySpriteRenderer(skill.gameObject);
                if (renderer != null)
                    renderer.color = Color.white;
                skill.OnDeath -= HandleSkillDeath;
                _pool.Return(skill.gameObject);
            }
        }
    }
}
