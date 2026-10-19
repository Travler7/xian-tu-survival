using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Fixed HUD readout for active-skill breakthrough progress.
    /// Pinned to the top-right corner of the game HUD so it can never be mistaken for a
    /// skill level, cooldown or upgrade card in the middle of the screen.
    /// </summary>
    public sealed class BreakthroughProgressView : MonoBehaviour
    {
        private const string LabelName = "BreakthroughProgress";
        private const float RightMargin = 24f;
        private const float TopMargin = 95f;
        private const float PanelWidth = 360f;
        private const float PanelHeight = 120f;

        private TextMeshProUGUI _text;

        private void Awake()
        {
            // The label lives directly on the HUD canvas rather than under this component: the
            // component object itself is a zero-sized child of the canvas, so anchoring to *its*
            // rect would pin the text to that tiny box instead of the screen corner.
            _text = EnsureLabel();
            ApplyLabelLayout(_text.rectTransform);
            _text.gameObject.SetActive(false);
        }

        /// <summary>Refreshes the readout, hiding it while no skill has any progress.</summary>
        public void Refresh(IReadOnlyList<SkillDataSO> skills)
        {
            if (_text == null) _text = EnsureLabel();
            if (_text == null) return;
            ApplyLabelLayout(_text.rectTransform);
            string text = BuildText(skills);
            bool hasProgress = !string.IsNullOrEmpty(text);
            if (_text.gameObject.activeSelf != hasProgress) _text.gameObject.SetActive(hasProgress);
            if (hasProgress && _text.text != text) _text.text = text;
            if (!hasProgress && !string.IsNullOrEmpty(_text.text)) _text.text = string.Empty;
        }

        /// <summary>Hides the readout without destroying it (run end, scene teardown).</summary>
        public void Hide()
        {
            if (_text == null) return;
            _text.text = string.Empty;
            if (_text.gameObject.activeSelf) _text.gameObject.SetActive(false);
        }

        private TextMeshProUGUI EnsureLabel()
        {
            Transform host = transform;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.transform != transform) host = canvas.transform;

            var existing = host.Find(LabelName);
            if (existing != null)
            {
                var existingText = existing.GetComponent<TextMeshProUGUI>();
                if (existingText != null) return existingText;
            }

            var go = new GameObject(LabelName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(host, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 16f;
            text.color = new Color(.8f, .72f, 1f);
            text.alignment = TextAlignmentOptions.TopRight;
            text.textWrappingMode = TextWrappingModes.Normal;
            // The battlefield behind the corner is pale terrain, so the readout needs its own
            // contrast to stay legible without a background plate over the action.
            text.fontStyle = FontStyles.Bold;
            text.outlineWidth = .18f;
            text.outlineColor = new Color32(20, 12, 36, 255);
            // Pure HUD label: it must never swallow clicks meant for panels underneath.
            text.raycastTarget = false;
            return text;
        }

        private static void ApplyLabelLayout(RectTransform rect)
        {
            if (rect == null) return;
            // Top-right anchor, right aligned, fixed margins from the screen edge: the readout
            // never follows the player, enemies or the upgrade cards.
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-RightMargin, -TopMargin);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        }

        private static string BuildText(IReadOnlyList<SkillDataSO> skills)
        {
            if (skills == null) return string.Empty;
            var values = skills
                .Where(s => s != null && s.SkillType == SkillType.AttackSkill &&
                            ShentongRuntime.ProgressFor(s.Key) > 0 && !ShentongRuntime.IsClaimed(s.Key))
                .OrderByDescending(s => ShentongRuntime.ProgressFor(s.Key))
                .Take(3)
                .Select(s => $"{s.DisplayName}：{ShentongRuntime.ProgressFor(s.Key)}/{ShentongRuntime.RequiredReactions}");
            return string.Join("\n", values);
        }
    }
}
