using System;
using System.Collections.Generic;
using System.Linq;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem {
    class WorldUpdator {
        private Dictionary<FactType, FactAdder> adderLogic;     // This class doesn't care about the semanitcs of each FactType. It uses generic Adder components to apply them.

        // Public interface
        public WorldRepresentation ApplyDynamicStateChange(WorldRepresentation world, IDynamicStateChange eventData) {
            // Initialise data container for the 'changes' object which we will pass into the DynamicState constructor.
            // For all the nodes which are going to be affected, we need to ensure
            Dictionary<int, Dictionary<FactType, Fact.MutableFact>> changeData = new Dictionary<int, Dictionary<FactType, Fact.MutableFact>>();
            foreach (int n in eventData.AffectedNodes) {

            }
            Remove(eventData.GetFactsBefore(), changeData);
            Add(eventData.GetFactsAfter(), changeData);

            Dictionary<int, Dictionary<FactType, Fact>> immutableCast = changeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDictionary(kvp2 => kvp2.Key, kvp2 => (Fact)kvp2.Value));
            return new WorldRepresentation(world.StaticState, new DynamicState(world.DynamicState, immutableCast));
        }

        public WorldRepresentation RevertDynamicStateChange(WorldRepresentation world, IDynamicStateChange eventData) {
            // Initialise data container for the 'changes' object which we will pass into the DynamicState constructor.
            Dictionary<int, Dictionary<FactType, Fact.MutableFact>> changeData = new Dictionary<int, Dictionary<FactType, Fact.MutableFact>>();
            Remove(eventData.GetFactsAfter(), changeData);
            Add(eventData.GetFactsBefore(), changeData);

            Dictionary<int, Dictionary<FactType, Fact>> immutableCast = changeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDictionary(kvp2 => kvp2.Key, kvp2 => (Fact)kvp2.Value));
            return new WorldRepresentation(world.StaticState, new DynamicState(world.DynamicState, immutableCast));
        }
    }

    // Interface for some object which encapsulates a Dynamic State Data change as a result of some event (Perception)
    /* IMPLEMENTATION REQUIREMENT: AffectedNodes == GetFactsAfter.Keys == GetGactsBefore.Keys | (They must all refer to the same set of nodes) */
    public interface IDynamicStateChange {
        // (nodeid , (factTypes and corresponding value))
        Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> GetFactsAfter();
        Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> GetFactsBefore();
        IEnumerable<int> AffectedNodes { get; }
    }

}
