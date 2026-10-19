using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Single source of truth for the global pause. Every screen that freezes the game
    /// (level-up, treasure, tribulation, shentong, run result) registers itself as an owner
    /// instead of writing <see cref="Time.timeScale"/> directly.
    ///
    /// Writing timeScale from several places made the last writer win: when a level-up card
    /// was picked while the tribulation panel was still open, the level-up panel resumed the
    /// game and left the tribulation overlay on screen over a running world. Counting owners
    /// means the world only resumes when the last panel lets go.
    /// </summary>
    public static class PauseCoordinator
    {
        // Unity's == null is true for destroyed objects, so owners are stored as a list and
        // pruned instead of relying on hashing: a destroyed owner must never wedge the pause.
        private static readonly List<Object> Owners = new(8);
        private static float _previousTimeScale = 1f;
        private static bool _ownsTimeScale;

        /// <summary>True while at least one panel holds the pause.</summary>
        public static bool IsPaused
        {
            get
            {
                Prune();
                return Owners.Count > 0;
            }
        }

        /// <summary>Number of live owners; mostly useful for tests and debugging.</summary>
        public static int OwnerCount
        {
            get
            {
                Prune();
                return Owners.Count;
            }
        }

        /// <summary>Registers <paramref name="owner"/> and freezes time if it was the first one.</summary>
        public static void Acquire(Object owner)
        {
            if (owner == null) return;
            Prune();
            if (Contains(owner)) return;

            Owners.Add(owner);
            if (Owners.Count != 1) return;

            // Only capture the clock when we are actually the ones stopping it; if something
            // else already froze the game, leave its value alone and simply hold the lock.
            if (Time.timeScale > 0f)
            {
                _previousTimeScale = Time.timeScale;
                _ownsTimeScale = true;
                Time.timeScale = 0f;
            }
            else
            {
                _ownsTimeScale = false;
            }
        }

        /// <summary>Drops <paramref name="owner"/> and resumes time once no owner remains.</summary>
        public static void Release(Object owner)
        {
            if (owner == null) return;
            if (!Remove(owner)) return;
            if (Owners.Count > 0) return;
            RestoreTimeScale();
        }

        /// <summary>Drops every owner and resumes time. Used when a run starts or ends.</summary>
        public static void ReleaseAll()
        {
            Owners.Clear();
            RestoreTimeScale();
        }

        /// <summary>Clears bookkeeping without touching the clock (tests, editor resets).</summary>
        public static void ResetForTests()
        {
            Owners.Clear();
            _previousTimeScale = 1f;
            _ownsTimeScale = false;
        }

        private static void RestoreTimeScale()
        {
            if (!_ownsTimeScale)
            {
                // Someone else owned the clock; do not overwrite their pause.
                _previousTimeScale = 1f;
                return;
            }

            if (Time.timeScale == 0f) Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
            _previousTimeScale = 1f;
            _ownsTimeScale = false;
        }

        private static void Prune()
        {
            for (int i = Owners.Count - 1; i >= 0; i--)
            {
                if (Owners[i] != null) continue;
                Owners.RemoveAt(i);
                if (Owners.Count == 0) RestoreTimeScale();
            }
        }

        private static bool Contains(Object owner)
        {
            for (int i = 0; i < Owners.Count; i++)
                if (ReferenceEquals(Owners[i], owner)) return true;
            return false;
        }

        private static bool Remove(Object owner)
        {
            for (int i = 0; i < Owners.Count; i++)
            {
                if (!ReferenceEquals(Owners[i], owner)) continue;
                Owners.RemoveAt(i);
                return true;
            }
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayStart() => ResetForTests();
    }
}
