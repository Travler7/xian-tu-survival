using RGame.CommonStat;
using RGame.Framework;
using RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RGame.RoguelikeKit
{
    public class LevelUI : MonoBehaviour
    {
        [SerializeField] private Image mLevelBar;
        [SerializeField] private Image mLevelDelta;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private VoidEventChannelSO _updateLevelUI;
        [SerializeField] private CommonStatRuntimeSO _stat;
        [SerializeField] private ExpConfig expConfig;

        [Header("Feedback")]
        [SerializeField] private Color mGainColor = new Color(0.96f, 0.86f, 0.28f, 1f);
        [SerializeField] private Color mResetColor = new Color(0.35f, 0.85f, 0.95f, 1f);
        [Range(0.05f, 1f)] [SerializeField] private float mTweenTime = 0.35f;
        [Range(0.05f, 1f)] [SerializeField] private float mFadeTime = 0.30f;

        [Header("Bar art mapping")]
        [SerializeField] private float mFillMin = 0.1941f;
        [SerializeField] private float mFillMax = 0.8624f;

        private BarAnimator mExpBar;

        private void Start()
        {
            mExpBar = new BarAnimator(
                mLevelBar,
                mLevelDelta,
                0f,
                mResetColor,
                mGainColor,
                mTweenTime,
                mFadeTime,
                mFillMin,
                mFillMax);

            _updateLevelUI.RegisterListener(UpdateUI);

            // XP starts empty: snap once so the bar does not open on the art's saved fill.
            mExpBar.SetValueImmediate(GetExpLine());
            _levelText.text = "境界 " + _stat.GetValue("Level");
        }

        private void OnDestroy()
        {
            mExpBar?.Kill();
            _updateLevelUI.UnregisterListener(UpdateUI);
        }

        private void UpdateUI()
        {
            int currentLevel = _stat.GetValue("Level");

            mExpBar.SetValue(GetExpLine());

            _levelText.text = "境界 " + currentLevel;
        }

        private float GetExpLine()
        {
            float currentExp = _stat.GetValue("Exp");
            float requiredExp = expConfig.GetExperienceForLevel(_stat.GetValue("Level"));
            return requiredExp > 0f ? currentExp / requiredExp : 0f;
        }
    }
}
