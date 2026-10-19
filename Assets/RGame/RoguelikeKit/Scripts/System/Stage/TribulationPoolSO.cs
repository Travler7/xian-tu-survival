using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    [CreateAssetMenu(menuName = "RGame/RoguelikeKit/Tribulation/Pool", fileName = "TribulationPool")]
    public sealed class TribulationPoolSO : ScriptableObject
    {
        public List<TribulationDefinition> Tribulations = new();
    }
}
