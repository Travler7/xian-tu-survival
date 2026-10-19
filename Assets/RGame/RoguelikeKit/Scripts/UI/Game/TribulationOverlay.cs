using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Full-screen modal for tribulation choices, rewards, event status and shentong
    /// breakthroughs. It owns a dedicated screen-space canvas that always carries a
    /// <see cref="GraphicRaycaster"/>, and every card is clickable as a whole while also
    /// exposing an explicit "选择" button, so a shown panel can never become a dead end.
    /// </summary>
    public sealed class TribulationOverlay : MonoBehaviour
    {
        private const int CardSlots = 3;
        private const float CardHeight = 286f;
        private const float CardGapFactor = 1.08f;
        private const float CardMinWidth = 190f;
        private const float CardMaxWidth = 340f;
        private const float ActionButtonHeight = 46f;
        private const float ActionButtonInset = 14f;
        private const int OverlaySortingOrder = 500;

        private sealed class ChoiceCard
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Background;
            public Button Button;
            public TextMeshProUGUI Label;
            public Button ActionButton;
            public TextMeshProUGUI ActionLabel;
        }

        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _root;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _status;
        private Image _background;
        private readonly List<ChoiceCard> _cards = new(CardSlots);
        private Action<ShentongBreakthroughDefinition> _breakthroughSelected;
        private float _toastUntil;
        private bool _toastOnly;
        private bool _modalVisible;
        private string _lastStatus;
        private string _pendingToast;
        private float _pendingToastDuration;

        private void Awake()
        {
            // The overlay is created on the fly while scenes stream in and out (MainMenu ->
            // GamePlay -> back). Surviving the scene change keeps the manager's reference valid;
            // a destroyed overlay would leave Time.timeScale frozen with no visible panel.
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void Build()
        {
            // A script reload re-runs Awake on the surviving object while its children are still
            // there. Rebuilding on top of them would leave a second, unreferenced canvas behind
            // (visible panel, dead buttons), so start from a clean slate.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var leftover = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(leftover); else DestroyImmediate(leftover);
            }
            _cards.Clear();

            EnsureCanvas();
            EnsureEventSystem();

            var rootObject = new GameObject("TribulationOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            rootObject.transform.SetParent(_canvas.transform, false);
            _root = rootObject.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            _background = rootObject.GetComponent<Image>();
            _background.color = new Color(0.035f, 0.045f, 0.055f, 0.88f);
            _group = rootObject.GetComponent<CanvasGroup>();

            _title = CreateText("Title", _root, new Vector2(.5f, .86f), new Vector2(Mathf.Min(1000f, CanvasWidth * .9f), 72f), 30, TextAlignmentOptions.Center);
            _status = CreateText("Status", _root, new Vector2(.5f, .71f), new Vector2(Mathf.Min(1100f, CanvasWidth * .9f), 100f), 19, TextAlignmentOptions.Center);
            for (int i = 0; i < CardSlots; i++) _cards.Add(CreateCard(i));
            SetVisible(false);
        }

        /// <summary>Creates (once) a dedicated overlay canvas that is guaranteed to receive mouse events.</summary>
        private void EnsureCanvas()
        {
            var canvasObject = new GameObject("TribulationCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = OverlaySortingOrder;
            _canvas.pixelPerfect = false;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // A canvas without a raycaster renders a panel that cannot be clicked. Reuse whatever
            // exists and add the missing piece instead of rebuilding the canvas.
            if (canvasObject.GetComponent<GraphicRaycaster>() == null) canvasObject.AddComponent<GraphicRaycaster>();
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) canvasObject.layer = uiLayer;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                return;
            }
            if (eventSystem.GetComponent<BaseInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private ChoiceCard CreateCard(int index)
        {
            var card = new ChoiceCard
            {
                Root = new GameObject($"Choice_{index}", typeof(RectTransform), typeof(Image), typeof(Button))
            };
            card.Root.transform.SetParent(_root, false);
            card.Rect = card.Root.GetComponent<RectTransform>();
            card.Rect.anchorMin = card.Rect.anchorMax = new Vector2(.5f, .5f);
            card.Rect.sizeDelta = new Vector2(CardWidth(), CardHeight);
            card.Background = card.Root.GetComponent<Image>();
            card.Background.color = new Color(.11f, .15f, .17f, 1f);
            card.Background.raycastTarget = true;
            card.Button = card.Root.GetComponent<Button>();
            card.Button.targetGraphic = card.Background;

            card.Label = CreateText("Label", card.Rect, new Vector2(.5f, .5f), Vector2.zero, 17, TextAlignmentOptions.Top);
            card.Label.rectTransform.anchorMin = Vector2.zero;
            card.Label.rectTransform.anchorMax = Vector2.one;
            card.Label.rectTransform.offsetMin = new Vector2(18f, ActionButtonHeight + ActionButtonInset * 2f);
            card.Label.rectTransform.offsetMax = new Vector2(-18f, -16f);

            // Explicit action button: the card is a Button already, but players need a visible
            // affordance telling them the card is the thing to click.
            var actionObject = new GameObject("Select", typeof(RectTransform), typeof(Image), typeof(Button));
            actionObject.transform.SetParent(card.Rect, false);
            var actionRect = actionObject.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0f, 0f);
            actionRect.anchorMax = new Vector2(1f, 0f);
            actionRect.pivot = new Vector2(.5f, 0f);
            actionRect.offsetMin = new Vector2(18f, ActionButtonInset);
            actionRect.offsetMax = new Vector2(-18f, ActionButtonInset + ActionButtonHeight);
            var actionImage = actionObject.GetComponent<Image>();
            actionImage.color = new Color(.26f, .45f, .55f, 1f);
            card.ActionButton = actionObject.GetComponent<Button>();
            card.ActionButton.targetGraphic = actionImage;

            card.ActionLabel = CreateText("Label", actionRect, new Vector2(.5f, .5f), Vector2.zero, 19, TextAlignmentOptions.Center);
            card.ActionLabel.rectTransform.anchorMin = Vector2.zero;
            card.ActionLabel.rectTransform.anchorMax = Vector2.one;
            card.ActionLabel.rectTransform.offsetMin = Vector2.zero;
            card.ActionLabel.rectTransform.offsetMax = Vector2.zero;
            card.ActionLabel.color = new Color(.96f, .97f, .93f);
            card.ActionLabel.text = "选择";

            card.Root.SetActive(false);
            return card;
        }

        public void ShowChoices(IReadOnlyList<TribulationDefinition> choices, Action<TribulationDefinition> selected)
        {
            if (choices == null || choices.Count == 0) return;
            BeginModal(new Color(0.035f, 0.045f, 0.055f, 0.88f));
            _title.text = "择一迎劫";
            SetStatus("劫数持续期间战场压力提升，成功后可选择本局机缘。");
            int count = PrepareCards(choices.Count, "选择");
            for (int i = 0; i < count; i++)
            {
                var choice = choices[i];
                _cards[i].Label.text = $"{choice.DisplayName}\n\n压力：{PressureName(choice.Pressure)}\n持续：{choice.Duration:0} 秒\n成功：{GoalText(choice)}\n\n劫境变化：{choice.DifficultyEffect}\n\n{choice.Description}\n\n机缘：{choice.RewardDirection}";
                BindCard(i, () => selected(choice));
            }
            SetVisible(true);
        }

        public void ShowRewards(IReadOnlyList<TribulationRewardDefinition> rewards, Action<TribulationRewardDefinition> selected)
        {
            if (rewards == null || rewards.Count == 0) return;
            BeginModal(new Color(0.035f, 0.045f, 0.055f, 0.88f));
            _title.text = "渡劫成功 · 择取机缘";
            SetStatus("机缘仅在本局生效。点击卡片或下方按钮领取。");
            int count = PrepareCards(rewards.Count, "领取");
            for (int i = 0; i < count; i++)
            {
                var reward = rewards[i];
                _cards[i].Label.text = $"{reward.DisplayName}\n\n{reward.Description}";
                BindCard(i, () => selected(reward));
            }
            SetVisible(true);
        }

        public void ShowEvent(string text)
        {
            // A status banner must never take the click target away from an open modal.
            if (_modalVisible) return;
            _background.color = new Color(0f, 0f, 0f, 0f);
            HideCards();
            _toastOnly = true;
            _toastUntil = 0f;
            _title.text = string.Empty;
            SetStatus(text);
            SetVisible(true);
        }

        public void ShowBreakthroughs(IReadOnlyList<ShentongBreakthroughDefinition> breakthroughs, Action<ShentongBreakthroughDefinition> selected, string sourceSkillName = null)
        {
            if (breakthroughs == null || breakthroughs.Count == 0) return;
            BeginModal(new Color(0.035f, 0.045f, 0.055f, 0.88f));
            _title.text = "神通突破";
            SetStatus(string.IsNullOrEmpty(sourceSkillName)
                ? "此突破仅在本局生效，每局最多选择两次。点击卡片或下方「选择」按钮完成突破。"
                : $"{sourceSkillName} 已累计触发 {ShentongRuntime.RequiredReactions} 次反应。此突破仅在本局生效，每局最多选择两次。点击卡片或下方「选择」按钮完成突破。");
            _breakthroughSelected = selected;
            int count = PrepareCards(breakthroughs.Count, "选择");
            for (int i = 0; i < count; i++)
            {
                var choice = breakthroughs[i];
                string name = string.IsNullOrEmpty(sourceSkillName) ? choice.DisplayName : $"{sourceSkillName} · {choice.DisplayName}";
                _cards[i].Label.text = $"{name}\n\n{choice.Description}";
                BindCard(i, () => _breakthroughSelected?.Invoke(choice));
            }
            SetVisible(true);
        }

        public BreakthroughProgressView EnsureBreakthroughProgressView()
        {
            var existing = _canvas.GetComponentInChildren<BreakthroughProgressView>(true);
            if (existing != null) return existing;
            var go = new GameObject("BreakthroughProgressView", typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            // Draw the HUD readout underneath the overlay root, so an open modal covers it
            // instead of showing breakthrough progress on top of the choice cards.
            go.transform.SetAsFirstSibling();
            return go.AddComponent<BreakthroughProgressView>();
        }

        public void ShowToast(string text, float duration)
        {
            if (_modalVisible)
            {
                // Queue it: replacing an open choice panel with a toast leaves the game paused
                // behind an invisible, unclickable overlay.
                _pendingToast = text;
                _pendingToastDuration = duration;
                return;
            }
            ShowToastNow(text, duration);
        }

        private void ShowToastNow(string text, float duration)
        {
            _background.color = new Color(0f, 0f, 0f, 0f);
            HideCards();
            _toastOnly = true;
            _title.text = text;
            SetStatus(string.Empty);
            _toastUntil = Time.unscaledTime + duration;
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
            _background.color = new Color(0.035f, 0.045f, 0.055f, 0.88f);
            _breakthroughSelected = null;
            _modalVisible = false;
        }

        private void Update()
        {
            if (_group == null) return;
            if (_toastOnly && _group.alpha > 0 && _toastUntil > 0 && Time.unscaledTime >= _toastUntil) Hide();
            if (!_modalVisible && _group.alpha <= 0 && !string.IsNullOrEmpty(_pendingToast))
            {
                string text = _pendingToast;
                float duration = _pendingToastDuration;
                _pendingToast = null;
                ShowToastNow(text, duration);
            }
        }

        private void BeginModal(Color background)
        {
            _background.color = background;
            _toastOnly = false;
            _toastUntil = 0f;
            _modalVisible = true;
        }

        /// <summary>Activates the first <paramref name="count"/> cards with a centered layout.</summary>
        private int PrepareCards(int requested, string actionText)
        {
            int count = Mathf.Clamp(requested, 0, _cards.Count);
            float width = CardWidth();
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                bool active = i < count;
                card.Button.onClick.RemoveAllListeners();
                card.ActionButton.onClick.RemoveAllListeners();
                card.Root.SetActive(active);
                if (!active) continue;

                card.Rect.sizeDelta = new Vector2(width, CardHeight);
                // Center the whole group, so a single candidate sits in the middle of the screen
                // instead of off to the left.
                card.Rect.anchoredPosition = new Vector2((i - (count - 1) * .5f) * width * CardGapFactor, -30f);
                card.ActionLabel.text = actionText;
            }
            return count;
        }

        /// <summary>Whole card and its explicit button share one commit action.</summary>
        private void BindCard(int index, UnityEngine.Events.UnityAction commit)
        {
            var card = _cards[index];
            card.Button.onClick.AddListener(commit);
            card.ActionButton.onClick.AddListener(commit);
        }

        private void HideCards()
        {
            foreach (var card in _cards)
            {
                card.Button.onClick.RemoveAllListeners();
                card.ActionButton.onClick.RemoveAllListeners();
                card.Root.SetActive(false);
            }
        }

        private void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible && !_toastOnly;
            _group.blocksRaycasts = visible && !_toastOnly;
        }

        private void SetStatus(string text)
        {
            if (_lastStatus == text) return;
            _lastStatus = text;
            _status.text = text;
        }

        private float CanvasWidth
        {
            get
            {
                float width = _root != null ? _root.rect.width : 0f;
                return width > 0.5f ? width : Mathf.Max(1f, Screen.width);
            }
        }

        private float CardWidth() => Mathf.Clamp((CanvasWidth - 100f) / 3.3f, CardMinWidth, CardMaxWidth);

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchor, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = new Color(.92f, .94f, .9f);
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static string PressureName(TribulationPressure pressure) => pressure switch
        {
            TribulationPressure.Spawn => "刷怪压力",
            TribulationPressure.Ranged => "远程压力",
            TribulationPressure.Control => "控制压力",
            TribulationPressure.Elite => "精英压力",
            TribulationPressure.Element => "元素压力",
            _ => pressure.ToString()
        };

        private static string GoalText(TribulationDefinition item) => item.Goal switch
        {
            TribulationGoal.Survive => $"存活 {item.Duration:0} 秒",
            TribulationGoal.KillRanged => $"击杀 {item.TargetCount} 个事件远程敌人",
            TribulationGoal.KillElites => $"击杀 {item.TargetCount} 个事件精英",
            TribulationGoal.TriggerReactions => $"触发 {item.TargetCount} 次五行反应",
            _ => $"限时击杀 {item.TargetCount} 个事件敌人"
        };
    }
}
