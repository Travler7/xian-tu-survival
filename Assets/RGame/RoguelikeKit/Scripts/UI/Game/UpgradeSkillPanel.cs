using System;
using System.Collections.Generic;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class UpgradeSkillPanel : MonoBehaviour
    {
        [SerializeField] private UpgradeSkillUIChannel _upgradeSkillUIChannel;
        [SerializeField] private StringEventChannelSO _upgradeSkillChannel;
        [SerializeField] private UpgradeSkillItem[] _upgradeSkillPrefab;
        [SerializeField] private CanvasGroup _canvasGroup;
        
        private void OnEnable()
        {
            _upgradeSkillUIChannel.RegisterListener(SkillUI);
        }

        private void OnDisable()
        {
            _upgradeSkillUIChannel.UnregisterListener(SkillUI);
        }

        private void SkillUI(List<SkillDataSO> skills)
        {
            Show();

            // Several level-ups can land in the same frame (one big XP gain), so this
            // listener may fire repeatedly. Subscribe defensively, otherwise a single
            // click would apply the upgrade once per stacked subscription.
            var count = Mathf.Min(skills.Count, _upgradeSkillPrefab.Length);

            for (var i = 0; i < _upgradeSkillPrefab.Length; i++)
            {
                _upgradeSkillPrefab[i].UpgradeSkillAction -= UpgradeSkill;
            }

            for (int i = 0; i < count; i++)
            {
                _upgradeSkillPrefab[i].UpgradeSkillAction += UpgradeSkill;

                // Set UI for skill or money bag
                _upgradeSkillPrefab[i].SetUI(skills[i]);
            }
        }
        
        public void Show()
        {
            Time.timeScale = 0;
            
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true; 
            _canvasGroup.blocksRaycasts = true; 
        }

        public void UpgradeSkill(SkillDataSO skill)
        {
            // Tear the UI down BEFORE applying the upgrade. If anything inside the
            // upgrade chain throws, the selection screen must not be left on screen
            // with the game already resumed.
            Time.timeScale = 1;

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            for (int i = 0; i < _upgradeSkillPrefab.Length; i++)
            {
                _upgradeSkillPrefab[i].UpgradeSkillAction -= UpgradeSkill;
            }

            if (skill == null) return;

            // Handle money bag selection or regular skill upgrade
            if (skill.IsMoneyBag())
            {
                _upgradeSkillChannel.RaiseEvent("MoneyBag");
            }
            else
            {
                _upgradeSkillChannel.RaiseEvent(skill.Key);
            }
        }
    }
}