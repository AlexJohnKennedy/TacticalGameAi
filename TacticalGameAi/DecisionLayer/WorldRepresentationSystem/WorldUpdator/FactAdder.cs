using System;
using System.Collections.Generic;
using System.Text;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdator {
    /* This class is a generic 'fact adder'. Upon construction, it is passed in a fact type which is will apply and remove to specified nodes.
     * The fact adder contains a collection of 'effectadder' logic pieces which it delegates the task of applying subsesquent EFFECTS to, which occur
     * as a result of the Fact that was added. */
    class FactAdder {
        private FactType factType;  // This is immutable. This type will be defined upon construction and is the type that is added or removed by the object.
        private IEnumerable<IEffectAdder> effectAdders;    // This is immutable. The effect adding logic will be defined upon construction by the factory.

        // Public Interface.
        public void AddFact(WorldRepresentation world, int node, out Dictionary<FactType, Fact> nodeFacts) {

        }
        public void RemoveFact(WorldRepresentation world, int node, out Dictionary<FactType, Fact> nodeFacts) {

        }
    }
}
