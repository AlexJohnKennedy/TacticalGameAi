﻿using System;
using System.Collections.Generic;
using System.Text;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem.EffectAdderImplementations {

    // Strategy for adding an effect to directly adjacent nodes based on some edge condition. Re-used among all of the simple effect adder logic implementations.
    // Note that these simple implementations only care about examining the StaticState edges, and ignore the 'related nodes' field in their logic!
    internal static class SingleEdgeBasedEffectAdder {
        internal static void AddEffects(int factNode, int value, int numNodes, Fact.MutableFact factObject, Func<int, int, bool> edgeCondition, EffectType effectType) {
            // Add our effect type to all nodes that are visible from the fact node.
            for (int i=0; i < numNodes; i++) {
                if (edgeCondition(factNode, i)) {
                    factObject.AccessEffectsCausedList().Add(new Effect(effectType, value, i, factNode));
                }
            }
        }
    }

    public class TraversabilityBasedEffectAdder : IEffectAdder {
        private EffectType effectType;

        public TraversabilityBasedEffectAdder(EffectType effectType) {
            this.effectType = effectType;
        }

        public void AddEffects(WorldRepresentation world, int factNode, Fact.MutableFact factObject, IEnumerable<int> relatedNodes) {
            SingleEdgeBasedEffectAdder.AddEffects(factNode, 1, world.NumberOfNodes, factObject, world.StaticState.IsTraversableReader(), effectType);
        }
    }

    public class ControlBasedEffectAdder : IEffectAdder {
        private EffectType effectType;

        public ControlBasedEffectAdder(EffectType effectType) {
            this.effectType = effectType;
        }

        public void AddEffects(WorldRepresentation world, int factNode, Fact.MutableFact factObject, IEnumerable<int> relatedNodes) {
            SingleEdgeBasedEffectAdder.AddEffects(factNode, 1, world.NumberOfNodes, factObject, world.StaticState.HasControlOverReader(), effectType);
        }
    }

    public class FullVisibilityBasedEffectAdder : IEffectAdder {
        private EffectType effectType;

        public FullVisibilityBasedEffectAdder(EffectType effectType) {
            this.effectType = effectType;
        }

        public void AddEffects(WorldRepresentation world, int factNode, Fact.MutableFact factObject, IEnumerable<int> relatedNodes) {
            SingleEdgeBasedEffectAdder.AddEffects(factNode, 1, world.NumberOfNodes, factObject, world.StaticState.FullVisibilityReader(), effectType);
        }
    }

    public class AtLeastTravelVisibilityBasedEffectAdder : IEffectAdder {
        private EffectType effectType;

        public AtLeastTravelVisibilityBasedEffectAdder(EffectType effectType) {
            this.effectType = effectType;
        }

        public void AddEffects(WorldRepresentation world, int factNode, Fact.MutableFact factObject, IEnumerable<int> relatedNodes) {
            SingleEdgeBasedEffectAdder.AddEffects(factNode, 1, world.NumberOfNodes, factObject, (from, to) => world.StaticState.TravelVisibilityReader()(from, to) || world.StaticState.FullVisibilityReader()(from, to), effectType);
        }
    }

    public class CanBeSeenByBasedEffectAdder : IEffectAdder {
        private EffectType effectType;

        public CanBeSeenByBasedEffectAdder(EffectType effectType) {
            this.effectType = effectType;
        }

        public void AddEffects(WorldRepresentation world, int factNode, Fact.MutableFact factObject, IEnumerable<int> relatedNodes) {
            SingleEdgeBasedEffectAdder.AddEffects(factNode, 1, world.NumberOfNodes, factObject, (from, to) => world.StaticState.FullVisibilityReader()(to, from), effectType);
        }
    }
}
