#region

using System;
using System.Linq;
using RGame.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

#endregion

namespace RGame.RoguelikeKit
{
    public class SettingPanel : MonoBehaviour
    {
        /// <summary>
        /// Width of the prefab's authored row layout. Only used before the container has been
        /// laid out once; after that rows follow the live canvas width.
        /// </summary>
        private const float ReferenceRowWidth = 1248f;

        [SerializeField] private SettingsSO _currentSettings;
        [SerializeField] private VoidEventChannelSO SaveSettingsEvent;

        [Header("Setting Items")] [SerializeField]
        private SettingItem masterVolumeItem;

        [SerializeField] private SettingItem musicVolumeItem;
        [SerializeField] private SettingItem sfxVolumeItem;
        [SerializeField] private SettingItem resolutionItem;
        [SerializeField] private SettingItem fullscreenItem;
        [SerializeField] private SettingItem languageItem;
        [SerializeField] private SettingItem showDamageItem;

        private Locale[] availableLocales;

        public UnityAction Closed;
        private string[] localeDisplayNames;
        private string[] resolutionDisplayTexts;

        private RectTransform _rowContainer;
        private Vector2 _lastRowContainerSize;

        private void Awake()
        {
            ConfigurePanelArtwork();
            CreateSectionHeaders();
            CreateSettingsTitle();
            ConfigureCloseButton();

            var items = GetComponentsInChildren<SettingItem>(true);
            for (var i = 0; i < items.Length; i++)
                items[i].ApplyVisualStyle(i % 2 == 1);
        }

        private void ConfigurePanelArtwork()
        {
            // The prefab artwork already contains the complete ornate panel.
            // Keep its proportions and let the row sprites provide the content
            // surface instead of covering it with a second opaque rectangle.
            var background = transform.Find("BG")?.GetComponent<Image>();
            if (background != null)
            {
                background.color = Color.white;
                background.raycastTarget = false;
                background.transform.SetAsFirstSibling();
            }

            var group = transform.Find("SettingGroup") as RectTransform;
            if (group != null)
            {
                group.anchorMin = new Vector2(0.17818828f, 0.078f);
                group.anchorMax = new Vector2(0.82519037f, 0.8093298f);
                group.offsetMin = Vector2.zero;
                group.offsetMax = Vector2.zero;

                // The prefab carried a small non-zero size delta on top of its relative
                // anchors, which made the content box drift away from the artwork frame.
                group.sizeDelta = Vector2.zero;

                // SettingGroup also ships a VerticalLayoutGroup. It re-drives the anchors of the
                // row container whenever the canvas refreshes, which collapsed the rows back to
                // their prefab size and defeated the responsive layout. Rows are positioned by
                // ApplyFixedRowLayout, so the group must not manage them.
                var groupLayout = group.GetComponent<VerticalLayoutGroup>();
                if (groupLayout != null)
                {
                    groupLayout.enabled = false;
                    if (Application.isPlaying) Destroy(groupLayout);
                    else DestroyImmediate(groupLayout);
                }

                var contentBackground = group.Find("SettingsContentBackground") as RectTransform;
                if (contentBackground == null)
                {
                    // This object was created by the previous runtime styling
                    // pass. Do not create it again; the row artwork is the
                    // intended content background.
                }
                else
                    contentBackground.gameObject.SetActive(false);
            }

            var rowContainer = transform.Find("SettingGroup/Setting Items") as RectTransform;
            if (rowContainer != null)
            {
                // Stretch horizontally inside SettingGroup and keep the 631px content
                // height. Rows then inherit the live width instead of the old fixed 1248,
                // so the layout survives non 16:9 aspect ratios (see ApplyFixedRowLayout).
                rowContainer.anchorMin = new Vector2(0f, 1f);
                rowContainer.anchorMax = new Vector2(1f, 1f);
                rowContainer.pivot = new Vector2(0.5f, 1f);
                rowContainer.offsetMin = new Vector2(0f, 0f);
                rowContainer.offsetMax = new Vector2(0f, 0f);
                rowContainer.sizeDelta = new Vector2(0f, 631f);
                rowContainer.anchoredPosition = new Vector2(0f, 0f);

                // The prefab ships a VerticalLayoutGroup on the row container. Disabling the
                // component is not enough: Canvas.ForceUpdateCanvases() re-enables it, and the
                // group then re-drives the children's anchors and collapses the container.
                // Rows are positioned manually, so the autolayout has to be removed outright.
                var layout = rowContainer.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.enabled = false;
                    if (Application.isPlaying) Destroy(layout);
                    else DestroyImmediate(layout);
                }

                _rowContainer = rowContainer;
            }
        }

        private void OnEnable()
        {
            if (_rowContainer == null) return;

            // Resolve the width from the container's anchors instead of forcing a layout
            // rebuild: rebuilding here zeroes the rect when the object is not part of a live
            // canvas yet, which used to leave every row at the prefab's fixed width.
            _lastRowContainerSize = new Vector2(ResolveRowWidth(_rowContainer), 0f);
            ApplyFixedRowLayout(_rowContainer);
        }

        private void OnRectTransformDimensionsChange()
        {
            RelayoutRowsIfNeeded();
        }

        /// <summary>
        /// Rows are laid out from the container's live width, so a window that changes size
        /// (or a resolution applied on Start) has to re-run the pass. This is cheap: it only
        /// runs when the container width actually changed.
        /// </summary>
        private void RelayoutRowsIfNeeded()
        {
            if (_rowContainer == null) return;

            // Use the anchoring-derived width, not rect: during a resize Unity calls this
            // before the container has been laid out, and rect would still read zero.
            float width = ResolveRowWidth(_rowContainer);
            if (width <= 1f) return;
            if (Mathf.Abs(width - _lastRowContainerSize.x) < 0.01f) return;

            _lastRowContainerSize = new Vector2(width, _lastRowContainerSize.y);
            ApplyFixedRowLayout(_rowContainer);
        }

        /// <summary>
        /// Width the rows should use. The container stretches to its parent, so the parent's
        /// rect is available as soon as the parent has been laid out, which is earlier and more
        /// reliable than the container's own rect during a resize.
        /// </summary>
        private static float ResolveRowWidth(RectTransform rowContainer)
        {
            var parent = rowContainer.parent as RectTransform;
            if (parent != null && parent.rect.width > 1f) return parent.rect.width;

            float own = rowContainer.rect.width;
            return own > 1f ? own : ReferenceRowWidth;
        }

        private void Start()
        {
            InitializeLocales();
            InitializeResolutions();
            InitializeSettingItems();
            // SettingItem.Awake runs after this panel's Awake. Apply once more
            // after every row has initialized so the final theme is deterministic.
            var items = GetComponentsInChildren<SettingItem>(true);
            for (var i = 0; i < items.Length; i++)
                items[i].ApplyVisualStyle(i % 2 == 1);
        }

        private void CreateSectionHeaders()
        {
            var rowContainer = transform.Find("SettingGroup/Setting Items");
            if (rowContainer == null) return;

            var audioHeader = CreateSectionHeader(rowContainer, "AudioHeader", "音频");
            var displayHeader = CreateSectionHeader(rowContainer, "DisplayHeader", "显示");
            var languageHeader = CreateSectionHeader(rowContainer, "LanguageHeader", "语言");

            var index = 0;
            audioHeader.SetSiblingIndex(index++);
            masterVolumeItem.transform.SetSiblingIndex(index++);
            musicVolumeItem.transform.SetSiblingIndex(index++);
            sfxVolumeItem.transform.SetSiblingIndex(index++);

            displayHeader.SetSiblingIndex(index++);
            resolutionItem.transform.SetSiblingIndex(index++);
            fullscreenItem.transform.SetSiblingIndex(index++);
            showDamageItem.transform.SetSiblingIndex(index++);

            languageHeader.SetSiblingIndex(index++);
            languageItem.transform.SetSiblingIndex(index);

            ApplyFixedRowLayout(rowContainer);
        }

        private void ApplyFixedRowLayout(Transform rowContainer)
        {
            const float rowHeight = 64f;
            const float headerHeight = 26f;
            const float gap = 8f;

            // Rows follow the container instead of a hard-coded 1248px. The canvas scaler
            // matches height only, so a 4:3 window widens the canvas to 1440 while the
            // panel keeps its relative anchors: a fixed row width then overflowed the frame
            // and clipped the value labels off the right edge.
            float rowWidth = rowContainer is RectTransform rect ? ResolveRowWidth(rect) : ReferenceRowWidth;

            var y = 0f;
            for (var i = 0; i < rowContainer.childCount; i++)
            {
                var child = rowContainer.GetChild(i) as RectTransform;
                if (child == null) continue;

                var isHeader = child.name.EndsWith("Header", StringComparison.Ordinal);
                var height = isHeader ? headerHeight : rowHeight;
                // Stretch horizontally: the anchoredPosition of a stretched rect is relative
                // to its anchors' centre, which keeps the row centred without a magic offset.
                child.anchorMin = new Vector2(0f, 1f);
                child.anchorMax = new Vector2(1f, 1f);
                child.pivot = new Vector2(0.5f, 0.5f);
                child.offsetMin = new Vector2(0f, child.offsetMin.y);
                child.offsetMax = new Vector2(0f, child.offsetMax.y);
                child.sizeDelta = new Vector2(0f, height);
                child.anchoredPosition = new Vector2(0f, -(y + height * 0.5f));
                y += height + gap;
            }
        }

        private static RectTransform CreateSectionHeader(Transform parent, string objectName, string label)
        {
            var existing = parent.Find(objectName) as RectTransform;
            var header = existing != null
                ? existing
                : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(LayoutElement)).GetComponent<RectTransform>();
            if (existing == null)
                header.SetParent(parent, false);

            header.sizeDelta = new Vector2(1248f, 26f);
            var layoutElement = header.GetComponent<LayoutElement>();
            layoutElement.minHeight = 28f;
            layoutElement.preferredHeight = 28f;
            layoutElement.flexibleHeight = 0f;

            var text = header.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null)
            {
                var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(header, false);
                text = textObject.GetComponent<TextMeshProUGUI>();
            }

            text.text = label;
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansSC SDF") ?? TMP_Settings.defaultFontAsset;
            text.fontSize = 22f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Left;
            text.characterSpacing = 2f;
            text.color = new Color32(255, 221, 145, 255);
            text.outlineWidth = 0.04f;
            text.outlineColor = new Color32(8, 28, 30, 255);
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;

            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(0f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = new Vector2(24f, 0f);
            textRect.sizeDelta = new Vector2(180f, 28f);

            var line = header.Find("Divider") as RectTransform;
            if (line == null)
            {
                var lineObject = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                lineObject.transform.SetParent(header, false);
                line = lineObject.GetComponent<RectTransform>();
            }

            line.anchorMin = new Vector2(0f, 0.5f);
            line.anchorMax = new Vector2(1f, 0.5f);
            line.offsetMin = new Vector2(218f, -1f);
            line.offsetMax = new Vector2(0f, 1f);
            line.GetComponent<Image>().color = new Color32(211, 168, 76, 145);
            line.GetComponent<Image>().raycastTarget = false;
            return header;
        }

        private void ConfigureCloseButton()
        {
            var closeButton = GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.gameObject.name == "Button Close");
            if (closeButton == null) return;

            var closeRect = closeButton.transform as RectTransform;
            if (closeRect == null) return;

            closeRect.SetParent(transform, false);
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-72f, -72f);
            closeRect.sizeDelta = new Vector2(68f, 68f);

            var image = closeButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.white;
                image.raycastTarget = true;
            }

            var closeContainer = transform.Find("ButtonClose");
            if (closeContainer != null)
                closeContainer.gameObject.SetActive(false);
        }

        public void CloseButton()
        {
            Closed?.Invoke();
        }

        private void InitializeLocales()
        {
            // 获取所有可用的语言
            availableLocales = LocalizationSettings.AvailableLocales.Locales.ToArray();
            // The only locale currently shipped uses the `en` identifier for
            // historical reasons, while its tables contain the Chinese copy.
            // Keep the identifier for localization, but show the player-facing
            // name that matches the actual content.
            localeDisplayNames = availableLocales.Select(GetLocaleDisplayName).ToArray();
        }

        private static string GetLocaleDisplayName(Locale locale)
        {
            if (locale == null) return "中文";

            var code = locale.Identifier.Code;
            if (string.Equals(code, "zh", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(code, "zh-CN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(code, "en", StringComparison.OrdinalIgnoreCase))
                return "中文";

            return locale.LocaleName;
        }

        private void InitializeResolutions()
        {
            var resolutions = Screen.resolutions;
            resolutionDisplayTexts = resolutions
                .Select(r => $"{r.width}x{r.height} @{Math.Round(r.refreshRateRatio.value)}Hz")
                .ToArray();
        }


        private void InitializeSettingItems()
        {
            masterVolumeItem.Initialize(
                Mathf.RoundToInt(_currentSettings.MasterVolume * 10),
                0, 10, false,
                value => SettingMasterVolume(value), true);

            musicVolumeItem.Initialize(
                Mathf.RoundToInt(_currentSettings.MusicVolume * 10),
                0, 10, false,
                value => SettingMusicVolume(value), true);

            sfxVolumeItem.Initialize(
                Mathf.RoundToInt(_currentSettings.SfxVolume * 10),
                0, 10, false,
                value => SettingSFXVolume(value), true);

            resolutionItem.Initialize(
                _currentSettings.ResolutionsIndex,
                resolutionDisplayTexts,
                value => SettingResolution(value));

            fullscreenItem.Initialize(
                _currentSettings.IsFullscreen ? 1 : 0,
                0, 1, true,
                value => SettingFullscreen(value == 1));

            var currentLocaleIndex = Array.IndexOf(availableLocales, _currentSettings.LanguageLocale);
            if (currentLocaleIndex < 0 && LocalizationSettings.SelectedLocale != null)
                currentLocaleIndex = Array.IndexOf(availableLocales, LocalizationSettings.SelectedLocale);
            if (currentLocaleIndex < 0 && availableLocales.Length > 0)
                currentLocaleIndex = 0;
            languageItem.Initialize(
                currentLocaleIndex,
                localeDisplayNames,
                value => SettingLanguage(value));

            showDamageItem.Initialize(
                _currentSettings.IsShowDamage ? 1 : 0,
                0, 1, true,
                value => SettingShowDamage(value == 1));
        }

        private void SettingMasterVolume(int value)
        {
            var normalizedValue = value / 10f;
            SaveSettings(
                _currentSettings.MusicVolume,
                _currentSettings.SfxVolume,
                normalizedValue,
                _currentSettings.ResolutionsIndex,
                _currentSettings.IsFullscreen,
                _currentSettings.LanguageLocale,
                _currentSettings.IsShowDamage
            );
        }

        private void SettingMusicVolume(int value)
        {
            var normalizedValue = value / 10f;
            SaveSettings(
                normalizedValue,
                _currentSettings.SfxVolume,
                _currentSettings.MasterVolume,
                _currentSettings.ResolutionsIndex,
                _currentSettings.IsFullscreen,
                _currentSettings.LanguageLocale,
                _currentSettings.IsShowDamage
            );
        }

        private void SettingSFXVolume(int value)
        {
            var normalizedValue = value / 10f;
            SaveSettings(
                _currentSettings.MusicVolume,
                normalizedValue,
                _currentSettings.MasterVolume,
                _currentSettings.ResolutionsIndex,
                _currentSettings.IsFullscreen,
                _currentSettings.LanguageLocale,
                _currentSettings.IsShowDamage
            );
        }

        private void SettingResolution(int value)
        {
            SaveSettings(
                _currentSettings.MusicVolume,
                _currentSettings.SfxVolume,
                _currentSettings.MasterVolume,
                value,
                _currentSettings.IsFullscreen,
                _currentSettings.LanguageLocale,
                _currentSettings.IsShowDamage
            );
        }

        private void SettingFullscreen(bool isFullscreen)
        {
            SaveSettings(
                _currentSettings.MusicVolume,
                _currentSettings.SfxVolume,
                _currentSettings.MasterVolume,
                _currentSettings.ResolutionsIndex,
                isFullscreen,
                _currentSettings.LanguageLocale,
                _currentSettings.IsShowDamage
            );
        }

        private void SettingLanguage(int localeIndex)
        {
            if (localeIndex >= 0 && localeIndex < availableLocales.Length)
                SaveSettings(
                    _currentSettings.MusicVolume,
                    _currentSettings.SfxVolume,
                    _currentSettings.MasterVolume,
                    _currentSettings.ResolutionsIndex,
                    _currentSettings.IsFullscreen,
                    availableLocales[localeIndex],
                    _currentSettings.IsShowDamage
                );
        }

        private void SettingShowDamage(bool showDamage)
        {
            SaveSettings(
                _currentSettings.MusicVolume,
                _currentSettings.SfxVolume,
                _currentSettings.MasterVolume,
                _currentSettings.ResolutionsIndex,
                _currentSettings.IsFullscreen,
                _currentSettings.LanguageLocale,
                showDamage
            );
        }

        private void SaveSettings(float _musicVolume, float _sfxVolume, float _masterVolume, int _resolutionsIndex, bool _isFullscreen, Locale _locale, bool _damageShow)
        {
            _currentSettings.SaveSettings(_musicVolume, _sfxVolume, _masterVolume, _resolutionsIndex, _isFullscreen, _locale, _damageShow);
            SaveSettingsEvent.RaiseEvent();
        }

        private void CreateSettingsTitle()
        {
            var title = transform.Find("SettingsTitle")?.GetComponent<TextMeshProUGUI>();
            if (title == null)
            {
                var titleObject = new GameObject("SettingsTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                titleObject.transform.SetParent(transform, false);
                title = titleObject.GetComponent<TextMeshProUGUI>();
            }

            var rect = title.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 415f);
            rect.sizeDelta = new Vector2(520f, 76f);

            title.text = "设置";
            title.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansSC SDF") ?? TMP_Settings.defaultFontAsset;
            title.fontSize = 48f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.characterSpacing = 6f;
            title.color = new Color32(248, 210, 126, 255);
            title.outlineWidth = 0.06f;
            title.outlineColor = new Color32(11, 36, 38, 245);
            title.raycastTarget = false;

            var divider = transform.Find("SettingsTitleDivider") as RectTransform;
            if (divider == null)
            {
                var dividerObject = new GameObject("SettingsTitleDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dividerObject.transform.SetParent(transform, false);
                divider = dividerObject.GetComponent<RectTransform>();
            }

            divider.anchorMin = new Vector2(0.5f, 0.5f);
            divider.anchorMax = new Vector2(0.5f, 0.5f);
            divider.pivot = new Vector2(0.5f, 0.5f);
            divider.anchoredPosition = new Vector2(0f, 354f);
            divider.sizeDelta = new Vector2(300f, 2f);
            divider.GetComponent<Image>().color = new Color32(211, 168, 76, 165);
            divider.GetComponent<Image>().raycastTarget = false;
        }
    }
}
