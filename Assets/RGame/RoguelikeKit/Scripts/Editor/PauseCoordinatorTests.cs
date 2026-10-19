#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit.EditorTests
{
    /// <summary>
    /// Covers the regression where two screens paused the game and the one closed first
    /// resumed it, leaving the other panel on screen over a running world.
    ///
    /// Owners are real GameObjects on purpose: a bare <c>new Object()</c> is a fake-null
    /// Unity object, which every Unity API (including the coordinator) treats as destroyed.
    /// </summary>
    public sealed class PauseCoordinatorTests
    {
        private GameObject _a;
        private GameObject _b;
        private GameObject _c;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            PauseCoordinator.ResetForTests();
            _a = new GameObject("PauseOwnerA");
            _b = new GameObject("PauseOwnerB");
            _c = new GameObject("PauseOwnerC");
        }

        [TearDown]
        public void TearDown()
        {
            PauseCoordinator.ResetForTests();
            Time.timeScale = 1f;
            Object.DestroyImmediate(_a);
            Object.DestroyImmediate(_b);
            Object.DestroyImmediate(_c);
        }

        [Test]
        public void SingleOwnerFreezesAndResumes()
        {
            Assert.That(PauseCoordinator.IsPaused, Is.False);

            PauseCoordinator.Acquire(_a);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(PauseCoordinator.IsPaused, Is.True);

            PauseCoordinator.Release(_a);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(PauseCoordinator.IsPaused, Is.False);
        }

        [Test]
        public void ReleasingOneOwnerKeepsTheGameFrozenForTheOther()
        {
            // This is the exact bug: a level-up card is picked while the tribulation
            // panel is still open. The world must stay frozen.
            PauseCoordinator.Acquire(_a);
            PauseCoordinator.Acquire(_b);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            PauseCoordinator.Release(_a);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "releasing one owner must not resume the game");
            Assert.That(PauseCoordinator.OwnerCount, Is.EqualTo(1));

            PauseCoordinator.Release(_b);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void AcquiringTwiceFromTheSameOwnerCountsOnce()
        {
            PauseCoordinator.Acquire(_a);
            PauseCoordinator.Acquire(_a);
            Assert.That(PauseCoordinator.OwnerCount, Is.EqualTo(1));

            PauseCoordinator.Release(_a);
            Assert.That(Time.timeScale, Is.EqualTo(1f), "a duplicated Acquire must not leave a stray lock");
            Assert.That(PauseCoordinator.IsPaused, Is.False);
        }

        [Test]
        public void ReleaseForANonOwnerDoesNotResume()
        {
            PauseCoordinator.Acquire(_a);
            PauseCoordinator.Release(_b);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(PauseCoordinator.IsPaused, Is.True);
        }

        [Test]
        public void ReleaseAllDropsEveryOwner()
        {
            PauseCoordinator.Acquire(_a);
            PauseCoordinator.Acquire(_b);
            PauseCoordinator.Acquire(_c);
            Assert.That(PauseCoordinator.OwnerCount, Is.EqualTo(3));

            PauseCoordinator.ReleaseAll();
            Assert.That(PauseCoordinator.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void ExternalTimeScaleIsNotOverwrittenOnRelease()
        {
            // If something else already stopped the clock, the coordinator must not claim
            // ownership of it, and must not resume it on release either.
            Time.timeScale = 0f;
            PauseCoordinator.Acquire(_a);
            PauseCoordinator.Release(_a);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "an externally owned pause must survive release");
        }

        [Test]
        public void DestroyedOwnerDoesNotWedgeThePause()
        {
            var temp = new GameObject("PauseCoordinatorTestOwner");
            PauseCoordinator.Acquire(temp);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            Object.DestroyImmediate(temp);

            // Pruning happens on the next query; the dead owner must not hold the lock.
            Assert.That(PauseCoordinator.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void QuickResumeRestoresTheOriginalTimeScale()
        {
            Time.timeScale = 0.5f;
            PauseCoordinator.Acquire(_a);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            PauseCoordinator.Release(_a);
            Assert.That(Time.timeScale, Is.EqualTo(0.5f), "a slowed game must come back slowed, not at 1x");
        }

        [Test]
        public void NullOwnerIsIgnored()
        {
            PauseCoordinator.Acquire(null);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(PauseCoordinator.IsPaused, Is.False);
        }
    }
}
#endif
