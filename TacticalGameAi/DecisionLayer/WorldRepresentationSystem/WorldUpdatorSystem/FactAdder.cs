﻿using System;
using System.Collections.Generic;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem {
    
    public interface IFactAdder {
        void AddFact(WorldRepresentation world, int node, int value, int timeLearned, Dictionary<FactType, Fact.MutableFact> nodeFacts, IEnumerable<int> relatedNodes);
        void RemoveFact(WorldRepresentation world, int node, Dictionary<FactType, Fact.MutableFact> nodeFactsToModify);
    }
    
    /* This class is a generic 'fact adder'. Upon construction, it is passed in a fact type which is will apply and remove to specified nodes.
     * The fact adder contains a collection of 'effectadder' logic pieces which it delegates the task of applying subsesquent EFFECTS to, which occur
     * as a result of the Fact that was added. */
    public class FactAdder : IFactAdder {
        private FactType factType;
        private IEnumerable<IEffectAdder> effectAdders;

        public FactAdder(FactType factType, IEnumerable<IEffectAdder> effectAdders) {
            this.factType = factType;
            this.effectAdders = effectAdders;
        }

        // Public Interface.
        public void AddFact(WorldRepresentation world, int node, int value, int timeLearned, Dictionary<FactType, Fact.MutableFact> nodeFacts, IEnumerable<int> relatedNodes) {
            if (nodeFacts.ContainsKey(factType)) {
                // Modify existing fact object's value, and that's it! The relevant effects should already exist, because the fact already exists.
                nodeFacts[factType].SetValue(value);
                nodeFacts[factType].SetTime(timeLearned);
            }
            else {
                // New fact!
                Fact.MutableFact f = new Fact.MutableFact(factType, value, timeLearned, new List<Effect>());
                // Delegate Effect adding logic to all our IEffectAdder objects. We don't care!
                foreach (IEffectAdder adder in effectAdders) {
                    adder.AddEffects(world, node, f, relatedNodes);
                }
                nodeFacts.Add(factType, f);
            }
        }
        public void RemoveFact(WorldRepresentation world, int node, Dictionary<FactType, Fact.MutableFact> nodeFactsToModify) {
            if (nodeFactsToModify.ContainsKey(factType)) {
                nodeFactsToModify.Remove(factType);
            }
        }
    }

    public interface IEffectAdder {
        void AddEffects(WorldRepresentation world, int factNode, Fact.MutableFact factObject, IEnumerable<int> relatedNodes);
    }
}
