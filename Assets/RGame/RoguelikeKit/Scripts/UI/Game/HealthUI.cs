using RGame.CommonStat;
using RGame.Framework;
using RoguelikeKit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RGame.RoguelikeKit
{
    public class HealthUI : MonoBehaviour
    {
        [SerializeField] private CommonStatRuntimeSO mStat;
        [SerializeField] private Image mHPBar;
        [SerializeField] private Image mHpTransitionBar;

        [Header("Feedback")]
        [SerializeField] private Color mDamageColor = new Color(0.99f, 0.55f, 0.20f, 1f);
        [SerializeField] private Color mHealColor = new Color(0.42f, 0.92f, 0.55f, 1f);
        [Range(0f, 0.6f)] [SerializeField] private float mDelayTime = 0.35f;
        [Range(0.05f, 1f)] [SerializeField] private float mTweenTime = 0.28f;
        [Range(0.05f, 1f)] [SerializeField] private float mFadeTime = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float mLowHealthThreshold = 0.25f;
        [SerializeField] private Color mLowHealthColor = new Color(1f, 0.30f, 0.26f, 1f);

        [Header("Bar art mapping")]
        [Tooltip("The ornate art's inner channel does not span the whole texture, so 0..1 is remapped onto it.")]
        [SerializeField] private float mFillMin = 0.1941f;
        [SerializeField] private float mFillMax = 0.8624f;

        private BarAnimator mHpBar;
        private UnityAction<int, int> mHPChange;

        private void Start()
        {
            mHpBar = new BarAnimator(
                mHPBar,
                mHpTransitionBar,
                mDelayTime,
                mDamageColor,
                mHealColor,
                mTweenTime,
                mFadeTime,
                mFillMin,
                mFillMax,
                mLowHealthThreshold,
                mLowHealthColor);

            mHPChange += mHpBar.SetValue;
            mStat.AddAction("HP", mHPChange);

            // AddAction only fires on change, so open the bar on the real values
            // instead of whatever fillAmount the art happened to be saved with.
            var hp = mStat.GetValue("HP");
            var maxHp = mStat.GetMaxValue("HP");
            if (maxHp <= 0) maxHp = Mathf.Max(1, hp);
            mHpBar.SetValueImmediate(hp, maxHp);
        }

        private void OnDestroy()
        {
            mHpBar?.Kill();
            mStat.RemoveAction("HP", mHPChange);
            mHPChange -= mHpBar.SetValue;
        }
    }
}
