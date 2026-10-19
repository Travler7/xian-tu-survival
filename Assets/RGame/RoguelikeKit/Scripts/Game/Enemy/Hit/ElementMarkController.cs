using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>Stores at most two temporary element marks and resolves pairs.</summary>
    public sealed class ElementMarkController : MonoBehaviour
    {
        public static event Action<ReactionResult> AnyReactionTriggered;
        public static event Action<ReactionResult, string> AnyReactionTriggeredWithSource;
        private struct Mark
        {
            public ElementType Element;
            public float Expire;
            public string Source;
            public float NextSourceTime;
        }

        public const float SourceCooldown = 0.75f;
        public const float ReactionCooldown = 1.75f;
        public const int MaxMarks = 2;

        private readonly List<Mark> _marks = new(MaxMarks);
        private float _nextReactionTime;
        private Coroutine _cleanupRoutine;

        public event Action<IReadOnlyList<ElementType>> MarksChanged;
        public event Action<ReactionResult, Vector3> ReactionTriggered;

        private void OnEnable() => Clear();

        public IReadOnlyList<ElementType> ActiveElements
        {
            get
            {
                Cleanup();
                return ActiveElementsWithoutCleanup();
            }
        }

        /// <summary>
        /// Registers an incoming element before damage is applied. The caller can
        /// then merge the returned reaction damage into the same hit transaction.
        /// </summary>
        public bool Register(ElementType element, string sourceKey, out ReactionResult reaction)
        {
            return Register(element, sourceKey, out reaction, out _);
        }

        public bool Register(ElementType element, string sourceKey, out ReactionResult reaction, out ElementResonanceResult resonance)
        {
            reaction = default;
            resonance = default;
            if (element == ElementType.None) return false;
            Cleanup();
            float now = Time.time;

            for (int i = 0; i < _marks.Count; i++)
            {
                if (_marks[i].Element != element) continue;
                var mark = _marks[i];
                if (mark.Source == sourceKey && now < mark.NextSourceTime) return false;
                mark.Expire = now + InnateRootRuntime.MarkDuration;
                mark.Source = sourceKey;
                mark.NextSourceTime = now + SourceCooldown;
                _marks[i] = mark;
                ElementResonanceRuntime.RecordDirectHit(element, sourceKey, GetHashCode(), out resonance);
                NotifyChanged();
                return false;
            }

            for (int i = 0; i < _marks.Count; i++)
            {
                if (!ReactionResolver.TryResolve(_marks[i].Element, element, out var candidate)) continue;
                if (now < _nextReactionTime) break;
                _nextReactionTime = now + ReactionCooldown;
                reaction = candidate;
                _marks.Clear();
                ElementResonanceRuntime.RecordDirectHit(element, sourceKey, GetHashCode(), out resonance);
                NotifyChanged();
                return true;
            }

            if (_marks.Count >= MaxMarks)
            {
                int oldest = 0;
                for (int i = 1; i < _marks.Count; i++)
                    if (_marks[i].Expire < _marks[oldest].Expire) oldest = i;
                _marks.RemoveAt(oldest);
            }

            _marks.Add(new Mark
            {
                Element = element,
                Expire = now + InnateRootRuntime.MarkDuration,
                Source = sourceKey,
                NextSourceTime = now + SourceCooldown
            });
            ElementResonanceRuntime.RecordDirectHit(element, sourceKey, GetHashCode(), out resonance);
            NotifyChanged();
            return false;
        }

        /// <summary>Compatibility entry for callers that only manage marks.</summary>
        public void Apply(ElementType element, int baseDamage, string sourceKey)
        {
            if (Register(element, sourceKey, out var reaction))
                NotifyReaction(reaction);
        }

        /// <summary>Adds a mark without dealing damage or evaluating a reaction.</summary>
        public void ApplyMarkOnly(ElementType element, string sourceKey)
        {
            if (element == ElementType.None) return;
            Cleanup();
            float now = Time.time;
            for (int i = 0; i < _marks.Count; i++)
            {
                if (_marks[i].Element != element) continue;
                var mark = _marks[i];
                if (mark.Source == sourceKey && now < mark.NextSourceTime) return;
                mark.Expire = now + InnateRootRuntime.MarkDuration;
                mark.Source = sourceKey;
                mark.NextSourceTime = now + SourceCooldown;
                _marks[i] = mark;
                NotifyChanged();
                return;
            }
            if (_marks.Count >= MaxMarks)
            {
                int oldest = _marks[0].Expire <= _marks[1].Expire ? 0 : 1;
                _marks.RemoveAt(oldest);
            }
            _marks.Add(new Mark { Element = element, Expire = now + InnateRootRuntime.MarkDuration, Source = sourceKey, NextSourceTime = now + SourceCooldown });
            NotifyChanged();
        }

        public void NotifyReaction(ReactionResult reaction)
        {
            AnyReactionTriggered?.Invoke(reaction);
            AnyReactionTriggeredWithSource?.Invoke(reaction, string.Empty);
            ReactionTriggered?.Invoke(reaction, transform.position);
        }

        public void NotifyReaction(ReactionResult reaction, string sourceKey)
        {
            AnyReactionTriggered?.Invoke(reaction);
            AnyReactionTriggeredWithSource?.Invoke(reaction, sourceKey ?? string.Empty);
            ShentongRuntime.RecordReaction(sourceKey, reaction);
            ReactionTriggered?.Invoke(reaction, transform.position);
        }

        public void Clear()
        {
            _marks.Clear();
            _nextReactionTime = 0f;
            if (_cleanupRoutine != null) StopCoroutine(_cleanupRoutine);
            _cleanupRoutine = null;
            NotifyChanged();
        }

        public int MarkCount { get { Cleanup(); return _marks.Count; } }

        public bool HasMark(ElementType element)
        {
            Cleanup();
            for (int i = 0; i < _marks.Count; i++) if (_marks[i].Element == element) return true;
            return false;
        }

        private void Cleanup()
        {
            float now = Time.time;
            int removed = _marks.RemoveAll(m => m.Expire <= now);
            if (removed > 0) NotifyChanged();
        }

        private void NotifyChanged()
        {
            MarksChanged?.Invoke(ActiveElementsWithoutCleanup());
            if (_marks.Count > 0 && _cleanupRoutine == null) _cleanupRoutine = StartCoroutine(CleanupRoutine());
        }

        private IReadOnlyList<ElementType> ActiveElementsWithoutCleanup()
        {
            var values = new List<ElementType>(_marks.Count);
            for (int i = 0; i < _marks.Count; i++) values.Add(_marks[i].Element);
            return values;
        }

        private IEnumerator CleanupRoutine()
        {
            while (_marks.Count > 0)
            {
                float wait = Mathf.Max(0.02f, _marks[0].Expire - Time.time);
                for (int i = 1; i < _marks.Count; i++) wait = Mathf.Min(wait, _marks[i].Expire - Time.time);
                yield return new WaitForSeconds(wait);
                Cleanup();
            }
            _cleanupRoutine = null;
        }
    }
}
