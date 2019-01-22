using System;
using System.Collections.Generic;
using System.Text;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem.EffectAdderImplementations {
    public class SimpleRelatedNodesEffectAdder : IEffectAdder {
        private EffectType effectType;

        public SimpleRelatedNodesEffectAdder(EffectType effectType) {
            this.effectType = effectType;
        }

        public void AddEffects(WorldRepresentation world, int factNode, Fact.MutableFact factObject, IEnumerable<int> relatedNodes) {
            // All this simple kind of effect adder does, is add the effect to all the specified related nodes, if they are passed to us!
            if (relatedNodes != null) {
                foreach (int n in relatedNodes) {
                    factObject.AccessEffectsCausedList().Add(new Effect(effectType, 1, n, factNode));
                }
            }
        }
    }
}
