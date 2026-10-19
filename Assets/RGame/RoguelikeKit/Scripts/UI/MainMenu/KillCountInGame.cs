using TMPro;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Shows the kill count for this run. Refreshing only on change keeps TextMeshPro from
    /// rebuilding and re-laying out the label every FixedUpdate.
    /// </summary>
    public class KillCountInGame : MonoBehaviour
    {
        [SerializeField] private GlobalConfigSO _globalConfig;
        [SerializeField] private TextMeshProUGUI _killCountText;

        private int _lastKills = int.MinValue;
        private string _cache = string.Empty;

        private void Update()
        {
            if (_globalConfig == null || _killCountText == null) return;

            int kills = _globalConfig.CurrentGetKill;
            if (kills == _lastKills) return;

            _lastKills = kills;
            _cache = kills.ToString();
            _killCountText.SetText(_cache);
        }
    }
}
