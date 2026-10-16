#region

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#endregion

namespace RGame.RoguelikeKit
{
    public class SettingItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        private int currentValue;
        private string[] displayTexts;
        private bool isBoolean;
        private int maxValue;
        private int minValue;
        private UnityAction<int> onValueChanged;
        private TextMeshProUGUI leftButtonLabel;
        private TextMeshProUGUI rightButtonLabel;
        private bool displayAsPercentage;

        private static TMP_FontAsset ChineseFont =>
            Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansSC SDF") ?? TMP_Settings.defaultFontAsset;

        private void Awake()
        {
            // Keep the existing Xianxia button artwork and add a readable
            // direction mark on top of it.
            leftButtonLabel = EnsureButtonLabel(leftButton, "←");
            rightButtonLabel = EnsureButtonLabel(rightButton, "→");
            ConfigureText(titleText, 29f, FontStyles.Bold, new Color32(250, 247, 235, 255));
            ConfigureText(valueText, 27f, FontStyles.Bold, new Color32(255, 221, 145, 255));
        }

        public void ApplyVisualStyle(bool alternate)
        {
            var rowBackground = GetComponent<Image>();
            if (rowBackground != null)
            {
                // The row sprite is the main visual language of this panel.
                // Preserve it and only use a very light alternating tint.
                rowBackground.color = alternate
                    ? new Color32(245, 249, 242, 255)
                    : Color.white;
                rowBackground.raycastTarget = false;
            }

            var rowRect = transform as RectTransform;
            if (rowRect != null)
                rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, 64f);

            ConfigureText(titleText, 29f, FontStyles.Bold, new Color32(250, 247, 235, 255));
            ConfigureText(valueText, 27f, FontStyles.Bold, new Color32(255, 221, 145, 255));

            if (titleText != null)
            {
                titleText.alignment = TextAlignmentOptions.Left;
                titleText.enableAutoSizing = false;
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                titleText.overflowMode = TextOverflowModes.Overflow;
                titleText.margin = new Vector4(30f, 0f, 0f, 0f);
            }

            if (valueText != null)
            {
                valueText.alignment = TextAlignmentOptions.Center;
                valueText.enableAutoSizing = true;
                valueText.fontSizeMin = 20f;
                valueText.fontSizeMax = 27f;
                valueText.textWrappingMode = TextWrappingModes.NoWrap;
                valueText.overflowMode = TextOverflowModes.Ellipsis;
                valueText.margin = new Vector4(0f, 0f, 0f, 0f);
            }

            StyleButton(leftButton, leftButtonLabel);
            StyleButton(rightButton, rightButtonLabel);

            var oldAccent = transform.Find("AccentLine");
            if (oldAccent != null)
                oldAccent.gameObject.SetActive(false);
        }

        public void Initialize(int defaultValue, int min, int max, bool isBool, UnityAction<int> callback,
            bool asPercentage = false)
        {
            leftButtonLabel ??= EnsureButtonLabel(leftButton, "←");
            rightButtonLabel ??= EnsureButtonLabel(rightButton, "→");
            minValue = min;
            maxValue = max;
            isBoolean = isBool;
            displayAsPercentage = asPercentage;
            onValueChanged = callback;

            SetValue(defaultValue);

            leftButton.onClick.AddListener(OnLeftButtonClick);
            rightButton.onClick.AddListener(OnRightButtonClick);
        }

        public void Initialize(int defaultValue, string[] texts, UnityAction<int> callback)
        {
            leftButtonLabel ??= EnsureButtonLabel(leftButton, "←");
            rightButtonLabel ??= EnsureButtonLabel(rightButton, "→");
            displayTexts = texts;
            minValue = 0;
            maxValue = texts.Length - 1;
            isBoolean = false;
            onValueChanged = callback;

            SetValue(defaultValue);

            leftButton.onClick.AddListener(OnLeftButtonClick);
            rightButton.onClick.AddListener(OnRightButtonClick);
        }

        private void OnLeftButtonClick()
        {
            if (isBoolean)
            {
                SetValue(0);
            }
            else
            {
                var newValue = Mathf.Max(minValue, currentValue - 1);
                SetValue(newValue);
            }
        }

        private void OnRightButtonClick()
        {
            if (isBoolean)
            {
                SetValue(1);
            }
            else
            {
                var newValue = Mathf.Min(maxValue, currentValue + 1);
                SetValue(newValue);
            }
        }

        private void SetValue(int value)
        {
            currentValue = Mathf.Clamp(value, minValue, maxValue);

            if (displayTexts != null && displayTexts.Length > 0)
                valueText.text = displayTexts[Mathf.Clamp(currentValue, 0, displayTexts.Length - 1)];
            else if (isBoolean)
                valueText.text = currentValue == 1 ? "开" : "关";
            else if (displayAsPercentage)
                valueText.text = $"{currentValue * 10}%";
            else
                valueText.text = currentValue.ToString();

            onValueChanged?.Invoke(currentValue);
        }

        private TextMeshProUGUI EnsureButtonLabel(Button button, string text)
        {
            if (button == null) return null;

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                var labelObject = new GameObject("Direction", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(button.transform, false);
                label = labelObject.GetComponent<TextMeshProUGUI>();
            }

            label.text = text;
            label.font = ChineseFont;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 27f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color32(255, 221, 145, 255);
            label.outlineWidth = 0.05f;
            label.outlineColor = new Color32(12, 33, 34, 240);
            label.raycastTarget = false;

            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return label;
        }

        private static void ConfigureText(TextMeshProUGUI text, float size, FontStyles style, Color color)
        {
            if (text == null) return;

            text.font = ChineseFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.outlineWidth = 0.05f;
            text.outlineColor = new Color32(9, 29, 30, 235);
        }

        private static void StyleButton(Button button, TextMeshProUGUI label)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                // Keep the original tertiary button sprite and its sliced/preserve
                // aspect settings; this is what restores the jade-and-gold style.
                image.color = Color.white;
                image.raycastTarget = true;
            }

            if (label == null) return;

            label.fontSize = 29f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color32(255, 221, 145, 255);
            label.outlineWidth = 0.05f;
            label.outlineColor = new Color32(12, 33, 34, 240);
        }

        public void UpdateValue(int value)
        {
            SetValue(value);
        }
    }
}
