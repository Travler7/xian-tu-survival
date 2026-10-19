using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    [CreateAssetMenu(menuName = "RGame/RoguelikeKit/Shentong Breakthroughs", fileName = "ShentongBreakthroughs")]
    public sealed class ShentongBreakthroughSO : ScriptableObject
    {
        public List<ShentongBreakthroughDefinition> Definitions = new();
    }
}
