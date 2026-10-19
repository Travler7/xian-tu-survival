using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    [CreateAssetMenu(menuName = "RGame/RoguelikeKit/Element/Reaction Matrix", fileName = "ReactionMatrix")]
    public sealed class ReactionMatrixSO : ScriptableObject
    {
        [SerializeField] private List<ReactionDefinition> _definitions = new();
        public IReadOnlyList<ReactionDefinition> Definitions => _definitions;

        public void ReplaceDefinitions(IEnumerable<ReactionDefinition> source)
        {
            _definitions = new List<ReactionDefinition>();
            if (source == null) return;
            foreach (var definition in source)
            {
                if (definition == null) continue;
                _definitions.Add(new ReactionDefinition(definition.First, definition.Second, definition.ReactionName,
                    definition.Description, definition.RelationType, definition.DirectDamageMultiplier,
                    definition.AreaDamageMultiplier, definition.AreaRadius, definition.MaxAreaTargets,
                    definition.DotDamageMultiplier, definition.DotTicks, definition.SlowPercent,
                    definition.SlowDuration, definition.FreezeDuration, definition.SpreadMarkCount,
                    definition.VfxPoolKey, definition.PopupColor, definition.AreaSlow));
            }
        }
    }
}
