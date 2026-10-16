using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RGame.RoguelikeKit
{
    public class UpgradeSkillItem : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private TextMeshProUGUI _level;
        private SkillDataSO _skillData;
        
        public UnityAction<SkillDataSO> UpgradeSkillAction;
        
        public void SetUI(SkillDataSO skillData)
        {
            _icon.sprite = skillData.SkillIcon;
            _name.text = DisplaySkillName(skillData);
            
            _level.text = "等级：" + skillData.Level;
            _skillData = skillData;
            
            switch (skillData.SkillType)
            {
                case SkillType.AttackSkill:
                    AttackSkillUI(skillData);
                    break;
                case SkillType.AttributeSkill:
                    AttributeSkillUI(skillData);
                    break;
                case SkillType.MoneyBag:
                    MoneyBagUI(skillData);
                    break;
            }
        }

        private void AttributeSkillUI(SkillDataSO skillData)
        {
            if (skillData.UpgradeAttributeModValue == null ||
                skillData.UpgradeAttributeModValue.Count == 0)
            {
                _description.text = skillData.Description;
                return;
            }

            // Attribute skills use Level as the zero-based index of the next
            // upgrade shown in the selection panel. Clamp the value so a
            // malformed or maxed asset cannot throw while refreshing the UI.
            int index = Mathf.Clamp(
                skillData.Level,
                0,
                skillData.UpgradeAttributeModValue.Count - 1);
            int value = skillData.UpgradeAttributeModValue[index];

            _description.text = FormatAttributeDescription(skillData.AttributeType, value);
        }

        private string FormatAttributeDescription(AttributeType attributeType, int value)
        {
            switch (attributeType)
            {
                case AttributeType.Might:
                    return $"所有技能伤害提高 {value}%";
                case AttributeType.Armor:
                    return $"受到伤害降低 {Mathf.Abs(value)}%";
                case AttributeType.HPMax:
                    return $"最大生命 {FormatSignedValue(value)}";
                case AttributeType.Recovery:
                    return $"每秒恢复 {value} 点生命";
                case AttributeType.Cooldown:
                    return $"技能冷却时间缩短 {Mathf.Abs(value)}%";
                case AttributeType.Area:
                    return $"技能范围扩大 {value}%";
                case AttributeType.SkillSpeed:
                    return $"投射物速度提高 {value}%";
                case AttributeType.Duration:
                    return $"技能持续时间延长 {value}%";
                case AttributeType.Amount:
                    return $"技能数量 {FormatSignedValue(value)}";
                case AttributeType.MoveSpeed:
                    return $"移动速度提高 {value}%";
                case AttributeType.Magnet:
                    return $"灵物拾取范围扩大 {value}%";
                case AttributeType.Growth:
                    return $"经验获取提高 {value}%";
                case AttributeType.Greed:
                    return $"宝箱灵石获取提高 {value}%";
                case AttributeType.Curse:
                    return $"随机刷怪速率提高 {value}%";
                default:
                    return $"{DisplayAttributeName(attributeType)} {FormatSignedValue(value)}";
            }
        }

        private string FormatSignedValue(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private void AttackSkillUI(SkillDataSO skillData)
        {
            if (skillData.Level == 0)
            {
                _description.text = string.IsNullOrWhiteSpace(skillData.Description)
                    ? $"习得{DisplaySkillName(skillData)}"
                    : skillData.Description;
                return;
            }
            
            switch (skillData.UpgradeAttribute[skillData.Level - 1].skillAttributeType)
            {
                case UpgradeSkillAttributeType.DamageAdd:
                    _description.text = $"剑伤提高 {skillData.UpgradeAttribute[skillData.Level - 1].IntValue}%";
                    break;

                case UpgradeSkillAttributeType.CDLessPercent:
                    _description.text = $"调息时间缩短 {skillData.UpgradeAttribute[skillData.Level - 1].IntValue}%";
                    break;

                case UpgradeSkillAttributeType.ProjectilesCountAdd:
                    _description.text = $"飞剑数量增加 {skillData.UpgradeAttribute[skillData.Level - 1].IntValue} 把";
                    break;

                case UpgradeSkillAttributeType.VelocityAddPercent:
                    _description.text = $"御剑速度提高 {skillData.UpgradeAttribute[skillData.Level - 1].IntValue}%";
                    break;

                case UpgradeSkillAttributeType.AreaAddPercent:
                    _description.text = $"剑势范围扩大 {skillData.UpgradeAttribute[skillData.Level - 1].IntValue}%";
                    break;
            }
        }

        private string DisplaySkillName(SkillDataSO skillData)
        {
            if (!string.IsNullOrWhiteSpace(skillData.DisplayName)) return skillData.DisplayName;
            return skillData.Key switch
            {
                "ToNearestSkill" => "御剑诀·追星",
                "BothSideSkill" => "阴阳剑气",
                "BouncingSkill" => "冰魄符",
                "ChainSkill" => "藤缚诀",
                "CircularAttackSkill" => "金光剑轮",
                "CircularMotionSkill" => "山岳护法",
                "FourCornerSkill" => "四象镇灵阵",
                "InsideScopeSkill" => "离火领域",
                "RandomSkill" => "天火流星",
                _ => skillData.Key
            };
        }

        private string DisplayAttributeName(AttributeType attributeType)
        {
            return attributeType switch
            {
                AttributeType.Might => "术威",
                AttributeType.Armor => "护体",
                AttributeType.HPMax => "道基",
                AttributeType.Recovery => "回元",
                AttributeType.Cooldown => "调息",
                AttributeType.Area => "法域",
                AttributeType.SkillSpeed => "施术速度",
                AttributeType.Duration => "持续真元",
                AttributeType.Amount => "分化",
                AttributeType.MoveSpeed => "身法",
                AttributeType.Magnet => "神识",
                AttributeType.Growth => "悟性",
                AttributeType.Greed => "聚财",
                AttributeType.Curse => "业障",
                _ => attributeType.ToString()
            };
        }

        private void MoneyBagUI(SkillDataSO skillData)
        {
            _name.text = "灵石袋";
            _level.text = ""; 
            _description.text = $"获得 {skillData.MoneyAmount} 枚灵石";
        }

        public void UpgradeSkill()
        {
            UpgradeSkillAction?.Invoke(_skillData);
        }
    }
}
