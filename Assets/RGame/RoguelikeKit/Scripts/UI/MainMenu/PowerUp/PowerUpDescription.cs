using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace RGame.RoguelikeKit
{
    public class PowerUpDescription : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI mTitle;
        [SerializeField] private TextMeshProUGUI mDescription;
        [SerializeField] private TextMeshProUGUI mGoldCount;
        
        private UnityAction mUpgradeAction;

        private void Awake()
        {
            ConfigureText(mTitle, 42f, FontStyles.Bold, new Color32(249, 241, 221, 255));
            ConfigureText(mDescription, 30f, FontStyles.Normal, new Color32(249, 241, 221, 255));
            ConfigureText(mGoldCount, 34f, FontStyles.Bold, new Color32(255, 226, 144, 255));
        }
        
        public void SelectPowerUp(string _title, string _description, int _requireGold, UnityAction _upgrade)
        {
            mUpgradeAction = null;
            mUpgradeAction += _upgrade;

            mTitle.text = _title;
            mDescription.text = _description;
            mGoldCount.text = _requireGold.ToString();
        }

        public void OnBuyButtonOnclick()
        {
            mUpgradeAction?.Invoke();
        }

        private static void ConfigureText(TextMeshProUGUI text, float size, FontStyles style, Color color)
        {
            if (text == null) return;

            text.font = text.font != null ? text.font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.outlineWidth = 0.12f;
            text.outlineColor = new Color32(28, 24, 20, 255);
        }
    }
}
