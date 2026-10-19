using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    [CreateAssetMenu(menuName = "RGame/RoguelikeKit/Tribulation/Reward Pool", fileName = "TribulationRewardPool")]
    public sealed class TribulationRewardPoolSO : ScriptableObject
    {
        public List<TribulationRewardDefinition> Rewards = new();
    }
}
