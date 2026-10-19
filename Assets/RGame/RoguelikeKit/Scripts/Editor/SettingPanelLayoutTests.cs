#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RGame.RoguelikeKit.EditorTests
{
    /// <summary>
    /// Guards the settings panel layout against aspect ratios other than 16:9.
    ///
    /// The canvas scaler matches height only, so a 4:3 window turns the 1920-wide canvas
    /// into a 1440-wide one. The rows used to be a hard-coded 1248px wide, which overflowed
    /// the ornate frame and clipped the value labels off the right edge. These tests drive
    /// the real panel at several canvas widths and assert that every row stays inside it.
    /// </summary>
    public sealed class SettingPanelLayoutTests
    {
        // Logical canvas widths produced by CanvasScaler(match=1) at common screen sizes.
        // Width is screenWidth / (screenHeight / 1080).
        private static readonly (int width, int height, float canvasWidth)[] Cases =
        {
            (1280, 720, 1920f),   // 16:9
            (1280, 960, 1440f),   // 4:3
            (1152, 864, 1440f),   // 4:3
            (2560, 1080, 2560f),  // 21:9
            (1920, 1080, 1920f)   // reference
        };

        private GameObject _root;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _panel;
        private SettingPanel _panelComponent;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("SettingPanelLayoutTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _canvas = _root.GetComponent<Canvas>();
            // World space: a screen-space canvas ignores its own rect in EditMode, so its
            // children would lay out against 0x0 and every assertion below would be vacuous.
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvasRect = (RectTransform)_root.transform;
            _canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            _canvasRect.localScale = Vector3.one;
            _canvasRect.localPosition = Vector3.zero;

            // No CanvasScaler on purpose: the tests drive the logical canvas width directly,
            // which is exactly what the scaler produces at each aspect ratio. Adding a scaler
            // here would overwrite the rect we set.
            Object.DestroyImmediate(_root.GetComponent<CanvasScaler>());

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RGame/RoguelikeKit/Prefabs/UI/Settings_Panel_PR.prefab");
            Assert.That(prefab, Is.Not.Null, "settings panel prefab is missing");

            var instance = Object.Instantiate(prefab, _root.transform, false);
            _panel = (RectTransform)instance.transform;

            _panelComponent = instance.GetComponent<SettingPanel>();
            Assert.That(_panelComponent, Is.Not.Null, "settings panel component is missing");

            // EditMode does not run Unity's lifecycle on objects created outside a scene, so
            // Awake/OnEnable never fire here. Drive them explicitly, in order, exactly as Unity
            // would: Awake builds the rows, OnEnable performs the first layout pass.
            instance.SetActive(true);
            Invoke(_panelComponent, "Awake");
            Invoke(_panelComponent, "OnEnable");

            // Surface the post-setup state in the failure message if a later assertion trips.
            var itemsAfterSetup = FindItems();
            SetupDiagnostics = itemsAfterSetup != null
                ? $"items anchors={itemsAfterSetup.anchorMin}/{itemsAfterSetup.anchorMax} "
                  + $"sizeDelta={itemsAfterSetup.sizeDelta} rect={itemsAfterSetup.rect} "
                  + $"parentRect={(itemsAfterSetup.parent as RectTransform)?.rect}"
                : "items missing";
        }

        private static string SetupDiagnostics = string.Empty;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void RowsStayInsideTheContainerAtEveryAspectRatio()
        {
            foreach (var (width, height, canvasWidth) in Cases)
            {
                ApplyCanvasSize(canvasWidth);

                var items = FindItems();
                Assert.That(items, Is.Not.Null, "row container not found");

                float containerWidth = items.rect.width;
                if (containerWidth <= 1f)
                {
                    var g = _panel.Find("SettingGroup") as RectTransform;
                    Assert.Fail($"container collapsed at {width}x{height}: "
                        + $"canvas rect={_canvasRect.rect} sizeDelta={_canvasRect.sizeDelta} "
                        + $"panel rect={_panel.rect} "
                        + $"group={(g != null ? g.rect.ToString() : "<null>")} "
                        + $"items anchors={items.anchorMin}/{items.anchorMax} sizeDelta={items.sizeDelta} "
                        + $"items parent={(items.parent as RectTransform)?.rect} "
                        + $"|| setup: {SetupDiagnostics}");
                }

                for (int i = 0; i < items.childCount; i++)
                {
                    var row = items.GetChild(i) as RectTransform;
                    if (row == null) continue;

                    Assert.That(row.rect.width, Is.EqualTo(containerWidth).Within(1f),
                        $"row '{row.name}' should follow the container at {width}x{height}");

                    // The container is the frame the row must stay inside; every row is
                    // stretched to 0..1 horizontally, so its offsets must both be zero.
                    Assert.That(row.offsetMin.x, Is.EqualTo(0f).Within(0.5f),
                        $"row '{row.name}' is inset on the left at {width}x{height}");
                    Assert.That(row.offsetMax.x, Is.EqualTo(0f).Within(0.5f),
                        $"row '{row.name}' is inset on the right at {width}x{height}");

                    // And it must not extend past the container's own width.
                    Assert.That(row.rect.width, Is.LessThanOrEqualTo(containerWidth + 0.5f),
                        $"row '{row.name}' is wider than the container at {width}x{height}");
                }
            }
        }

        [Test]
        public void RowsNeverFallBackToTheHardCodedWidth()
        {
            foreach (var (width, height, canvasWidth) in Cases)
            {
                ApplyCanvasSize(canvasWidth);

                var items = FindItems();
                float containerWidth = items.rect.width;

                // The old layout pinned every row to 1248 regardless of the container.
                // At 4:3 the container is ~932, so a 1248 row would immediately fail here.
                for (int i = 0; i < items.childCount; i++)
                {
                    var row = items.GetChild(i) as RectTransform;
                    if (row == null) continue;
                    Assert.That(row.sizeDelta.x, Is.EqualTo(0f).Within(0.01f),
                        $"row '{row.name}' uses a fixed width instead of stretching at {width}x{height}");
                    Assert.That(row.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
                    Assert.That(row.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
                    Assert.That(containerWidth, Is.GreaterThan(1f));
                }
            }
        }

        private void ApplyCanvasSize(float canvasWidth)
        {
            _canvasRect.sizeDelta = new Vector2(canvasWidth, 1080f);

            // The panel is anchored to the canvas, so its rect only follows once the canvas
            // has actually laid its children out. Rebuild the canvas hierarchy, not just
            // update canvases, otherwise the row container still reports the old width.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_canvasRect);
            Canvas.ForceUpdateCanvases();

            // Force the panel to recompute from the new container width.
            var panel = _panel.GetComponent<SettingPanel>();
            var field = typeof(SettingPanel).GetField("_lastRowContainerSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(panel, new Vector2(-1f, -1f));
            Invoke(panel, "RelayoutRowsIfNeeded");
            Canvas.ForceUpdateCanvases();
        }

        private RectTransform FindItems()
        {
            var group = _panel.Find("SettingGroup") as RectTransform;
            return group != null ? group.Find("Setting Items") as RectTransform : null;
        }

        private static void Invoke(object target, string method)
        {
            var info = target.GetType().GetMethod(method,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.That(info, Is.Not.Null, $"missing method {method}");
            info.Invoke(target, null);
        }
    }
}
#endif
