using System;
using System.Collections.Generic;
using RGame.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace RGame.RoguelikeKit
{
    public class SkillIconPanel : MonoBehaviour
    {
        [SerializeField] private AddSkillChannel _addIconEventChannel;
        [SerializeField] private GameObject[] _skillIcons;

        [Header("Icon layout")]
        [Tooltip("Fraction of the slot that is kept as a border so the icon stays inside the frame artwork.")]
        [SerializeField, Range(0f, 0.45f)] private float _iconInset = 0.16f;

        private const string ICON_CHILD_NAME = "Icon";
        private int _attackSkillIconIndex = 0;
        private int _attributeSkillIndex;
        private const int ATTRIBUTE_INDEX_START = 6;
        private readonly HashSet<string> _displayedSkillKeys = new HashSet<string>();
        private bool _syncPending;

        private void Awake()
        {
            _attributeSkillIndex = ATTRIBUTE_INDEX_START;
        }

        private void OnEnable()
        {
            _addIconEventChannel.RegisterListener(AddIcon);
            _syncPending = true;
        }

        private void OnDisable()
        {
            _addIconEventChannel.UnregisterListener(AddIcon);
            _syncPending = false;
        }

        private void LateUpdate()
        {
            // SkillManager and Player publish their initial skill during Start.
            // Syncing once in LateUpdate avoids depending on scene object order
            // and still catches the event-driven path for later upgrades.
            if (!_syncPending) return;

            SkillManager skillManager = FindAnyObjectByType<SkillManager>();
            if (skillManager == null) return;

            SyncActiveSkills(skillManager);
            _syncPending = false;
        }

        private void AddIcon(SkillDataSO skillData)
        {
            if (skillData == null || !_displayedSkillKeys.Add(skillData.Key)) return;

            switch (skillData.SkillType)
            {
                case SkillType.AttackSkill:
                    SetSkillIcon(ref _attackSkillIconIndex, skillData);
                    break;
                case SkillType.AttributeSkill:
                    SetSkillIcon(ref _attributeSkillIndex, skillData);
                    break;
            }
        }

        private void SyncActiveSkills(SkillManager skillManager)
        {
            _attackSkillIconIndex = 0;
            _attributeSkillIndex = ATTRIBUTE_INDEX_START;
            _displayedSkillKeys.Clear();

            foreach (var skillData in skillManager.GetActiveSkillSnapshot())
            {
                AddIcon(skillData);
            }
        }

        private void SetSkillIcon(ref int index, SkillDataSO skillData)
        {
            if (index >= _skillIcons.Length)
            {
                Debug.LogWarning("SkillIconPanel: Not enough skill icon slots!");
                return;
            }

            Image image = ResolveIconImage(_skillIcons[index]);
            if (image != null)
            {
                image.sprite = skillData.SkillIcon;
                image.enabled = true;
                image.color = Color.white;
                _skillIcons[index].SetActive(true);
                if (skillData.SkillIcon == null)
                    Debug.LogWarning($"SkillIconPanel: Skill '{skillData.Key}' has no SkillIcon sprite.");
                index++;
            }
            else
            {
                Debug.LogError($"SkillIconPanel: Missing Image component on skill icon at index {index}");
            }
        }

        /// <summary>
        /// Returns the Image that should draw the skill icon. The authored slot
        /// keeps its ornamental frame on a child called "BG", and uGUI draws
        /// children on top of their parent, so a sprite assigned to the slot
        /// root is hidden behind the opaque frame. Draw the icon on a dedicated
        /// child that is kept after the frame instead.
        /// </summary>
        private Image ResolveIconImage(GameObject slot)
        {
            if (slot == null) return null;

            Transform iconTransform = slot.transform.Find(ICON_CHILD_NAME);
            if (iconTransform == null)
            {
                GameObject iconObject = new GameObject(
                    ICON_CHILD_NAME,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                iconTransform = iconObject.transform;
                iconTransform.SetParent(slot.transform, false);

                RectTransform iconRect = (RectTransform)iconTransform;
                iconRect.anchorMin = new Vector2(_iconInset, _iconInset);
                iconRect.anchorMax = new Vector2(1f - _iconInset, 1f - _iconInset);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
            }

            // Keep the icon as the last sibling so it stays above the frame.
            iconTransform.SetAsLastSibling();

            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage == null)
                iconImage = iconTransform.gameObject.AddComponent<Image>();
            iconImage.raycastTarget = false;
            return iconImage;
        }
    }
}
