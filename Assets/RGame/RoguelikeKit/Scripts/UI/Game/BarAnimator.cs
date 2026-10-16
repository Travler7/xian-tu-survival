using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikeKit
{
    /// <summary>
    /// Drives a filled Image plus a delta Image that must sit BEHIND it.
    /// Damage  : the main bar drops immediately and the lost slice flashes then fades.
    /// Heal/XP : the main bar grows while the gained slice in front of nothing (behind
    ///           the main bar) is eaten, so the gain reads as an expanding segment.
    /// fillMin/fillMax remap the logical 0..1 value onto the sprite, which lets a bar
    /// art whose channel does not span the whole texture map cleanly.
    /// </summary>
    public class BarAnimator
    {
        private readonly Image mBar;
        private readonly Image mDelta;
        private readonly float mDelayTime;
        private readonly float mTweenTime;
        private readonly float mFadeTime;
        private readonly float mFillMin;
        private readonly float mFillMax;
        private readonly Color mDamageColor;
        private readonly Color mHealColor;
        private readonly float mLowThreshold;
        private readonly Color mLowPulseColor;
        private readonly Color mBaseBarColor;

        private float mCurrent;
        private Tween mAnimationTween;
        private Tween mPulseTween;
        private bool _disposed;

        public BarAnimator(
            Image _bar,
            Image _transitionBar,
            float _delayTime,
            Color damageColor,
            Color healColor,
            float tweenTime = 0.28f,
            float fadeTime = 0.35f,
            float fillMin = 0f,
            float fillMax = 1f,
            float lowThreshold = 0f,
            Color? lowPulseColor = null)
        {
            mBar = _bar;
            mDelta = _transitionBar;
            mDelayTime = _delayTime;
            mTweenTime = tweenTime;
            mFadeTime = fadeTime;
            mFillMin = Mathf.Clamp01(Mathf.Min(fillMin, fillMax));
            mFillMax = Mathf.Clamp01(Mathf.Max(fillMin, fillMax));
            mDamageColor = damageColor;
            mHealColor = healColor;
            mLowThreshold = lowThreshold;
            mLowPulseColor = lowPulseColor ?? new Color(1f, 0.32f, 0.28f, 1f);
            mBaseBarColor = mBar != null ? mBar.color : Color.white;

            mCurrent = mBar != null ? ToLogical(mBar.fillAmount) : 0f;
            if (mDelta != null)
            {
                mDelta.fillAmount = ToSprite(mCurrent);
                SetAlpha(mDelta, 0f);
            }
        }

        public void SetValue(int _newValue, int _maxValue)
        {
            SetValue(_maxValue <= 0 ? 0f : _newValue * 1.0f / _maxValue);
        }

        /// <summary>
        /// Snaps to a value without any transition. Use it once on Start so the bar
        /// opens on the real value (full health, empty XP) instead of whatever the
        /// art happened to be saved with.
        /// </summary>
        public void SetValueImmediate(int _newValue, int _maxValue)
        {
            SetValueImmediate(_maxValue <= 0 ? 0f : _newValue * 1.0f / _maxValue);
        }

        public void SetValueImmediate(float line)
        {
            if (_disposed) return;
            line = Mathf.Clamp01(line);

            mAnimationTween?.Kill();
            mAnimationTween = null;
            ApplyCurrent(line);

            if (mDelta != null)
            {
                mDelta.fillAmount = ToSprite(line);
                SetAlpha(mDelta, 0f);
            }

            UpdateLowPulse(line);
        }

        public void SetValue(float line)
        {
            if (_disposed) return;
            line = Mathf.Clamp01(line);

            var previous = mCurrent;
            var delta = line - previous;

            if (Mathf.Abs(delta) < 0.001f)
            {
                ApplyCurrent(line);
                UpdateLowPulse(line);
                return;
            }

            mAnimationTween?.Kill();

            if (delta < 0f)
            {
                ApplyCurrent(line);
                if (mDelta != null)
                {
                    mDelta.fillAmount = ToSprite(previous);
                    SetColor(mDelta, mDamageColor);
                    SetAlpha(mDelta, 1f);
                    mAnimationTween = DOTween.Sequence()
                        .AppendInterval(mDelayTime)
                        .Append(mDelta.DOFade(0f, mFadeTime))
                        .OnComplete(() =>
                        {
                            if (_disposed) return;
                            mDelta.fillAmount = ToSprite(line);
                            SetAlpha(mDelta, 1f);
                        });
                }
            }
            else if (mDelta != null)
            {
                // Grow into the gained slice: the delta covers the target and the main
                // bar eats into it, so the leftover segment is exactly [previous, line].
                mDelta.fillAmount = ToSprite(line);
                SetColor(mDelta, mHealColor);
                SetAlpha(mDelta, 1f);
                mAnimationTween = DOTween.Sequence()
                    .Join(mBar.DOFillAmount(ToSprite(line), mTweenTime).SetEase(Ease.OutQuad))
                    .Join(mDelta.DOFade(0f, mTweenTime + mFadeTime))
                    .OnComplete(() =>
                    {
                        if (_disposed) return;
                        mDelta.fillAmount = ToSprite(line);
                        SetAlpha(mDelta, 1f);
                    });
                mCurrent = line;
                UpdateLowPulse(line);
                return;
            }
            else
            {
                ApplyCurrent(line);
            }

            mCurrent = line;
            UpdateLowPulse(line);
        }

        private void ApplyCurrent(float line)
        {
            mCurrent = line;
            if (mBar != null) mBar.fillAmount = ToSprite(line);
        }

        private float ToSprite(float line)
        {
            return Mathf.Lerp(mFillMin, mFillMax, Mathf.Clamp01(line));
        }

        private float ToLogical(float spriteFill)
        {
            var span = mFillMax - mFillMin;
            return span <= 0.0001f ? 0f : Mathf.Clamp01((spriteFill - mFillMin) / span);
        }

        private void UpdateLowPulse(float line)
        {
            if (mBar == null || mLowThreshold <= 0f) return;

            if (line > 0f && line <= mLowThreshold)
            {
                if (mPulseTween == null || !mPulseTween.IsActive())
                {
                    mPulseTween = mBar.DOColor(mLowPulseColor, 0.45f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                }
            }
            else if (mPulseTween != null && mPulseTween.IsActive())
            {
                mPulseTween.Kill();
                mPulseTween = null;
                mBar.color = mBaseBarColor;
            }
        }

        private void SetColor(Image image, Color color)
        {
            if (image == null) return;
            image.color = color;
        }

        private void SetAlpha(Image image, float alpha)
        {
            if (_disposed) return;
            if (image == null) return;
            var c = image.color;
            c.a = alpha;
            image.color = c;
        }

        public void Kill()
        {
            if (_disposed) return;
            _disposed = true;
            mAnimationTween?.Kill();
            mAnimationTween = null;
            mPulseTween?.Kill();
            mPulseTween = null;
        }
    }
}
