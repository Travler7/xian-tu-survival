using UnityEngine;

namespace RGame.RoguelikeKit
{
    [CreateAssetMenu(menuName = "RGame/RoguelikeKit/Corruption Ward Config", fileName = "CorruptionWardConfig")]
    public sealed class CorruptionWardConfigSO : ScriptableObject
    {
        [Range(0f, 1f)] public float EliteWardRatio = 0.25f;
        [Range(0f, 1f)] public float BossWardRatio = 0.35f;
        [Min(1f)] public float ReactionDamageMultiplier = 2f;
        [Min(0f)] public float BrokenDuration = 2.5f;
        [Min(1f)] public float VulnerabilityMultiplier = 1.25f;
    }
}
