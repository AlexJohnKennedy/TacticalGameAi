using System;
using System.Collections.Generic;
using System.Linq;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem {
    public class WorldUpdator {
        private Dictionary<FactType, IFactAdder> adderLogic;     // This class doesn't care about the semanitcs of each FactType. It uses generic Adder components to apply them.

        public WorldUpdator(Dictionary<FactType, IFactAdder> adderLogic) {
            this.adderLogic = adderLogic;

            // Do not allow a World Updator object to be created with an incomplete FactAdder set. This would mean we could encounter DynamicStateChanges which
            // the WorldUpdator does not know how to handle!
            foreach(FactType f in Enum.GetValues(typeof(FactType))) {
                if (!adderLogic.ContainsKey(f)) throw new ArgumentException("adderLogic", "A World Updator was created with an incomplete FactAdder set. " +
                    "This would allow the scenario where this World Updator does not know how to handle certain DynamicStateChange events!");
            }
        }

        // Public interface
        public WorldRepresentation ApplyDynamicStateChangesSequentially(WorldRepresentation world, params IDynamicStateChange[] events) {
            foreach (IDynamicStateChange e in events) {
                world = ApplyDynamicStateChange(world, e);
            }
            return world;
        }
        public WorldRepresentation RevertDynamicStateChangesSequentially(WorldRepresentation world, params IDynamicStateChange[] events) {
            foreach (IDynamicStateChange e in events) {
                world = RevertDynamicStateChange(world, e);
            }
            return world;
        }
        public WorldRepresentation ApplyDynamicStateChange(WorldRepresentation world, IDynamicStateChange eventData) {
            // Initialise data container for the 'changes' object which we will pass into the DynamicState constructor.
            // For all the nodes which are going to be affected, we need to ensure we collect all the 'current' fact data for those nodes. That way we can remove and add the changes accordingly.
            Dictionary<int, Dictionary<FactType, Fact.MutableFact>> changeData = new Dictionary<int, Dictionary<FactType, Fact.MutableFact>>();

            // Collect the 'current' fact sets for the nodes which are to be affected by this State change being applied.
            foreach (int n in eventData.AffectedNodes) {
                Dictionary<FactType, Fact> nFacts = DynamicStateInternalReader.GetNodeFact(n, world.DynamicState);
                Dictionary<FactType, Fact.MutableFact> nFactsMutableCopies = nFacts.ToDictionary(kvp => kvp.Key, kvp => new Fact.MutableFact(kvp.Value));
                changeData.Add(n, nFactsMutableCopies);
            }
            Remove(eventData.GetFactsBefore(), changeData, world);
            Add(eventData.GetFactsAfter(), changeData, world);

            // TODO 5 - Make this LINQ dictionary 'cast' less retarded.
            Dictionary<int, Dictionary<FactType, Fact>> immutableCast = changeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDictionary(kvp2 => kvp2.Key, kvp2 => (Fact)kvp2.Value));
            return new WorldRepresentation(world.StaticState, new DynamicState(world.DynamicState, immutableCast));
        }

        public WorldRepresentation RevertDynamicStateChange(WorldRepresentation world, IDynamicStateChange eventData) {
            // Initialise data container for the 'changes' object which we will pass into the DynamicState constructor.
            Dictionary<int, Dictionary<FactType, Fact.MutableFact>> changeData = new Dictionary<int, Dictionary<FactType, Fact.MutableFact>>();

            // Collect the 'current' fact sets for the nodes which are to be affected by this State change being applied.
            foreach (int n in eventData.AffectedNodes) {
                Dictionary<FactType, Fact> nFacts = DynamicStateInternalReader.GetNodeFact(n, world.DynamicState);
                Dictionary<FactType, Fact.MutableFact> nFactsMutableCopies = nFacts.ToDictionary(kvp => kvp.Key, kvp => new Fact.MutableFact(kvp.Value));
                changeData.Add(n, nFactsMutableCopies);
            }

            Remove(eventData.GetFactsAfter(), changeData, world);
            Add(eventData.GetFactsBefore(), changeData, world);

            // TODO 5 - Make this LINQ dictionary 'cast' less retarded.
            Dictionary<int, Dictionary<FactType, Fact>> immutableCast = changeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDictionary(kvp2 => kvp2.Key, kvp2 => (Fact)kvp2.Value));
            return new WorldRepresentation(world.StaticState, new DynamicState(world.DynamicState, immutableCast));
        }

        private void Remove(Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> factsToRemove, Dictionary<int, Dictionary<FactType, Fact.MutableFact>> nodeFactData, WorldRepresentation world) {
            // Cycle each node to have something removed
            foreach(var n in factsToRemove) {
                // n.Key   == NodeId
                // n.Value == Enumerable list of fact types and their associated values, to be removed.
                foreach(var f in n.Value) {
                    FactType typeToBeRemoved = f.Key;
                    // Use our mapped adder logic to remove this fact!
                    adderLogic[typeToBeRemoved].RemoveFact(world, n.Key, nodeFactData[n.Key]);
                }
            }
        }
        private void Add(Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> factsToAdd, Dictionary<int, Dictionary<FactType, Fact.MutableFact>> nodeFactData, WorldRepresentation world) {
            // Cycle each node to have something removed
            foreach (var n in factsToAdd) {
                // n.Key   == NodeId
                // n.Value == Enumerable list of fact types and their associated values, to be added.
                foreach (var f in n.Value) {
                    // Use our mapped adder logic to add this fact!
                    FactType typeToBeAdded = f.Key;
                    adderLogic[typeToBeAdded].AddFact(world, n.Key, f.Value, nodeFactData[n.Key]);
                }
            }
        }
    }

    // Interface for some object which encapsulates a Dynamic State Data change as a result of some event (Perception)
    /* IMPLEMENTATION REQUIREMENT: AffectedNodes == GetFactsAfter.Keys == GetGactsBefore.Keys | (They must all refer to the same set of nodes) */
    public interface IDynamicStateChange {
        // (nodeid , (factTypes and corresponding value)). Note the the set of facts here DO NOT necessarilly contain ALL the facts of a given node after this
        // change. It only contains the facts which should be 'added'. Conversely, the 'before' facts only contain the previously existing facts which are
        // removed or changed by this StateChange event.
        Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> GetFactsAfter();      // Set of NEW facts and values to be added to each respective node.
        Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> GetFactsBefore();     // Set of OLD facts and values which are changed/removed by this change, for each respective ndoe.
        IEnumerable<int> AffectedNodes { get; }
        int TimeLearned { get; }                                                        // A Timestamp in gametime which tells us when the a unit was made aware of these fact changes.
    }

}
