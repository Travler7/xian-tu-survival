#region

using UnityEngine;
using UnityEngine.Events;
using TMPro;

#endregion

namespace RGame.RoguelikeKit
{
    public class UIMainMenu : MonoBehaviour
    {
        public UnityAction ExitButtonAction;
        public UnityAction NewGameButtonAction;
        public UnityAction PowerUpButtonAction;
        public UnityAction SettingsButtonAction;

        private void Awake()
        {
            CreateGameTitle();
        }

        private void CreateGameTitle()
        {
            var title = GetOrCreateTitle("GameTitle");
            var shadow = GetOrCreateTitle("GameTitleShadow");

            // The title belongs in the clear center of the frame, above the
            // primary action. Anchoring to the panel center keeps it stable at
            // different aspect ratios and prevents overlap with the top crest.
            ConfigureTitleRect(title.rectTransform, new Vector2(0f, 100f));
            ConfigureTitleRect(shadow.rectTransform, new Vector2(0f, 92f));

            var chineseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansSC SDF");
            ConfigureTitle(shadow, chineseFont, new Color32(18, 55, 53, 220), 94f, 0.12f, new Color32(9, 34, 34, 235));
            ConfigureTitle(title, chineseFont, new Color32(248, 213, 137, 255), 94f, 0.13f, new Color32(72, 48, 24, 245));

            shadow.transform.SetAsLastSibling();
            title.transform.SetAsLastSibling();
        }

        private TextMeshProUGUI GetOrCreateTitle(string objectName)
        {
            var existing = transform.Find(objectName);
            var text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (text != null) return text;

            var titleObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(transform, false);
            return titleObject.GetComponent<TextMeshProUGUI>();
        }

        private static void ConfigureTitleRect(RectTransform rect, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(840f, 150f);
        }

        private static void ConfigureTitle(TextMeshProUGUI title, TMP_FontAsset chineseFont, Color color,
            float fontSize, float outlineWidth, Color outlineColor)
        {
            title.text = "仙途生存";
            title.font = chineseFont != null ? chineseFont : TMP_Settings.defaultFontAsset;
            title.fontSize = fontSize;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.characterSpacing = 8f;
            title.lineSpacing = 0f;
            title.color = color;
            title.outlineWidth = outlineWidth;
            title.outlineColor = outlineColor;
            title.raycastTarget = false;
        }

        public void NewGameButton()
        {
            NewGameButtonAction.Invoke();
        }

        public void SettingsButton()
        {
            SettingsButtonAction.Invoke();
        }

        public void PowerUpButton()
        {
            PowerUpButtonAction.Invoke();
        }

        public void ExitButton()
        {
            ExitButtonAction.Invoke();
        }
    }
}
