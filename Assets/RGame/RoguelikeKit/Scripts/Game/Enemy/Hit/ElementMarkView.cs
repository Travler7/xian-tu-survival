using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    [RequireComponent(typeof(ElementMarkController))]
    public sealed class ElementMarkView : MonoBehaviour
    {
        [SerializeField] private bool _largeDisplay;
        private static readonly Dictionary<ElementType, Color> Colors = new()
        {
            { ElementType.Metal, new Color(1f, 0.9f, 0.35f) },
            { ElementType.Wood, new Color(0.25f, 0.9f, 0.45f) },
            { ElementType.Water, new Color(0.25f, 0.55f, 1f) },
            { ElementType.Fire, new Color(1f, 0.25f, 0.15f) },
            { ElementType.Earth, new Color(0.8f, 0.55f, 0.2f) }
        };
        private static Sprite _dotSprite;
        private ElementMarkController _marks;
        private SpriteRenderer[] _dots;
        private Coroutine _reactionFlash;

        public bool LargeDisplay => _largeDisplay;

        private void Awake()
        {
            _marks = GetComponent<ElementMarkController>();
            _dots = new SpriteRenderer[ElementMarkController.MaxMarks];
            if (_dotSprite == null) _dotSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            for (int i = 0; i < _dots.Length; i++)
            {
                var dot = new GameObject("ElementMark_" + i);
                dot.transform.SetParent(transform, false);
                dot.transform.localPosition = new Vector3((i - 0.5f) * 0.28f, 0.88f, -0.2f);
                dot.transform.localScale = Vector3.one * (_largeDisplay ? 0.28f : 0.18f);
                _dots[i] = dot.AddComponent<SpriteRenderer>();
                _dots[i].sprite = _dotSprite;
                _dots[i].sortingOrder = 50;
                _dots[i].enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_marks == null) _marks = GetComponent<ElementMarkController>();
            if (_marks == null) return;
            _marks.MarksChanged += Refresh;
            _marks.ReactionTriggered += ShowReactionMarks;
            Refresh(_marks.ActiveElements);
        }

        private void OnDisable()
        {
            if (_marks != null)
            {
                _marks.MarksChanged -= Refresh;
                _marks.ReactionTriggered -= ShowReactionMarks;
            }
            if (_reactionFlash != null) StopCoroutine(_reactionFlash);
            HideDots();
        }

        private void Refresh(IReadOnlyList<ElementType> elements)
        {
            if (_dots == null) return;
            for (int i = 0; i < _dots.Length; i++)
            {
                bool visible = i < elements.Count;
                _dots[i].enabled = visible;
                if (!visible) continue;
                _dots[i].color = Colors.TryGetValue(elements[i], out var color) ? color : Color.white;
                _dots[i].transform.localRotation = Quaternion.Euler(0f, 0f, RotationFor(elements[i]));
            }
        }

        private void ShowReactionMarks(ReactionResult reaction, Vector3 position)
        {
            if (_reactionFlash != null) StopCoroutine(_reactionFlash);
            _reactionFlash = StartCoroutine(ReactionFlashRoutine(reaction));
        }

        private IEnumerator ReactionFlashRoutine(ReactionResult reaction)
        {
            if (_dots == null) yield break;
            var elements = new[] { reaction.First, reaction.Second };
            for (int i = 0; i < _dots.Length; i++)
            {
                _dots[i].enabled = true;
                _dots[i].color = Colors.TryGetValue(elements[i], out var color) ? color : Color.white;
                _dots[i].transform.localRotation = Quaternion.Euler(0f, 0f, RotationFor(elements[i]));
            }
            yield return new WaitForSeconds(0.65f);
            _reactionFlash = null;
            if (_marks != null) Refresh(_marks.ActiveElements);
        }

        private void HideDots()
        {
            if (_dots == null) return;
            for (int i = 0; i < _dots.Length; i++) _dots[i].enabled = false;
        }

        private static float RotationFor(ElementType element)
        {
            switch (element)
            {
                case ElementType.Metal: return 45f;
                case ElementType.Wood: return 0f;
                case ElementType.Water: return 90f;
                case ElementType.Fire: return 22.5f;
                case ElementType.Earth: return 67.5f;
                default: return 0f;
            }
        }
    }
}
