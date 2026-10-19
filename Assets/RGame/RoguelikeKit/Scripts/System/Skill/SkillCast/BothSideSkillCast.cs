using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class BothSideSkillCast : SkillCast
    {
        public override void Cast(SkillDataSO skillData)
        {
            base.Cast(skillData);

            int damage = (int)(_stat.GetValue("Might") * skillData.Damage * 0.01f) + 1;
            float velocity = skillData.Velocity * _stat.GetValue("SkillSpeed") * 0.01f;
            float duration = skillData.Duration * _stat.GetValue("Duration") * 0.01f;

            SpawnSkill(180, velocity, damage, skillData, skillData.PrimaryElement);
            SpawnSkill(360, velocity, damage, skillData, skillData.SecondaryElement == ElementType.None ? skillData.PrimaryElement : skillData.SecondaryElement);
        }

        private void SpawnSkill(float angle, float velocity, int damage, SkillDataSO skillData, ElementType element)
        {
            var go = _pool.Request(Key);
            var skill = ConfigureSkill<BothSideSkill>(go, skillData);
            skill.transform.localScale = skill.transform.localScale * (_stat.GetValue("Area") + skillData.Area) * 0.01f;         
            skill.transform.position = _globalConfig.GlobalPlayer.transform.position;

            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), -Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;
            skill.OnDeath += HandleSkillDeath;
            skill.Init(velocity, damage, direction, element);
        }

        public override void MixSkill()
        {
            base.MixSkill();
            _mySkillData.CD *= 0.3f;
            _mySkillData.Area *= 2;
        }
    }
}
