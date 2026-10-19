using TMPro;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Shows the gold collected this run. The label only refreshes when the value actually
    /// changes: rebuilding a TMP string every FixedUpdate made TextMeshPro re-layout the text
    /// fifty times a second for a number that changes a few times a minute.
    /// </summary>
    public class CoinCountInGame : MonoBehaviour
    {
        [SerializeField] private GlobalConfigSO _globalConfig;
        [SerializeField] private TextMeshProUGUI _coinCountText;

        private int _lastGold = int.MinValue;
        private string _cache = string.Empty;

        private void Update()
        {
            if (_globalConfig == null || _coinCountText == null) return;

            int gold = _globalConfig.CurrentGetGold;
            if (gold == _lastGold) return;

            _lastGold = gold;
            _cache = gold.ToString();
            _coinCountText.SetText(_cache);
        }
    }
}
