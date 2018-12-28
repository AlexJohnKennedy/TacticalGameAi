using System;
using System.Collections.Generic;
using System.Text;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem {
    class WorldUpdator {
        private Dictionary<FactType, FactAdder> adderLogic;     // This class doesn't care about the semanitcs of each FactType. It uses generic Adder components to apply them.

        // Public interface
        public WorldRepresentation ApplyDynamicStateChange(WorldRepresentation world, IDynamicStateChange eventData) {
            // Initialise data container for the 'changes' object which we will pass into the DynamicState constructor.
            Dictionary<int, Dictionary<FactType, Fact>> changeData = new Dictionary<int, Dictionary<FactType, Fact>>();
            Remove(eventData.GetFactsBefore(), changeData);
            Add(eventData.GetFactsAfter(), changeData);

            return new WorldRepresentation(world.StaticState, new DynamicState(world.DynamicState, changeData));
        }

        public WorldRepresentation RevertDynamicStateChange(WorldRepresentation world, IDynamicStateChange eventData) {
            // Initialise data container for the 'changes' object which we will pass into the DynamicState constructor.
            Dictionary<int, Dictionary<FactType, Fact>> changeData = new Dictionary<int, Dictionary<FactType, Fact>>();
            Remove(eventData.GetFactsAfter(), changeData);
            Add(eventData.GetFactsBefore(), changeData);

            return new WorldRepresentation(world.StaticState, new DynamicState(world.DynamicState, changeData));
        }
    }

    // Interface for some object which encapsulates a Dynamic State Data change as a result of some event (Perception)
    public interface IDynamicStateChange {
        // (nodeid , (factTypes and corresponding value))
        Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> GetFactsAfter();
        Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> GetFactsBefore();
    }

}
