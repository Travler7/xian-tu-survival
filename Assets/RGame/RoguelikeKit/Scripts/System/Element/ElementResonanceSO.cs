using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    [CreateAssetMenu(menuName = "RGame/RoguelikeKit/Element/Single Element Resonance", fileName = "ElementResonance")]
    public sealed class ElementResonanceSO : ScriptableObject
    {
        public List<ElementResonanceDefinition> Definitions = new();
    }
}
