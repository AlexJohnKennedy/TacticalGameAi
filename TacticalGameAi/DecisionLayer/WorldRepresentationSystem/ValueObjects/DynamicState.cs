﻿using System;
using System.Collections;
using System.Collections.Generic;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    public class DynamicState {

        // The original representation of data
        private Dictionary<FactType, Fact>[] areaFacts;
        private Dictionary<EffectType, EffectSum>[] areaEffects;

        // Redundent representation of info built upon construction to allow clients faster lookups to info we have already calculated.
        private AreaNode[]  areaNodes;
        private AreaEdge[,] areaEdges;

        // Public Interface - Directly get all Vertex and Edge data
        public AreaNode GetNodeData(int nodeId) {
            if (areaNodes[nodeId] == null) areaNodes[nodeId] = new AreaNode();
            return areaNodes[nodeId];
        }
        public AreaEdge GetEdge(int fromNode, int toNode) {
            if (areaEdges[fromNode, toNode] == null) areaEdges[fromNode, toNode] = new AreaEdge();
            return areaEdges[fromNode, toNode];
        }

        public DynamicState(Dictionary<FactType, Fact>[] facts) {
            areaFacts = facts ?? throw new ArgumentNullException("facts", "Dynamic State received null fact array.");
            areaEffects = new Dictionary<EffectType, EffectSum>[facts.Length];
            for (int i=0; i<facts.Length; i++) { areaEffects[i] = new Dictionary<EffectType, EffectSum>(); }
            areaNodes = new AreaNode[facts.Length];
            areaEdges = new AreaEdge[facts.Length, facts.Length];

            // Populate EffectSum dictionaries for each node, to identify all the effects they have on each node.
            foreach (Dictionary<FactType,Fact> dict in facts) {
                foreach (Fact f in dict.Values) {
                    foreach (Effect e in f.EffectsCaused) {
                        AddEffectToEffectSum(e.NodeId, e);
                    }
                }
            }
        }
        private void AddEffectToEffectSum(int node, Effect e) {
            EffectSum sumObj = areaEffects[node][e.EffectType];
            if (sumObj != null) {
                sumObj.IncorporateNewEffect(e);
            }
            else {
                areaEffects[node].Add(e.EffectType, new EffectSum(e));
            }
        }

        // Public Interface - Clients can request reader functions to read a subset of the graph. This describes the Fact reader functions.
        private Func<int, int> FactValueReaderFunction(FactType type) {
            return n => areaFacts[n][type] == null ? 0 : areaFacts[n][type].Value;
        }
        private Func<int, bool> FactTruthReaderFunction(FactType type) {
            return n => areaFacts[n][type] != null;
        }
        public Func<int, int> KnownEnemyPresenceReader() {
            return FactValueReaderFunction(FactType.EnemyPresence);
        }
        public Func<int, int> KnownFriendlyPresenceReader() {
            return FactValueReaderFunction(FactType.FriendlyPresence);
        }
        public Func<int, int> KnownDangerLevelReader() {
            // Danger levels are reported as the sum of all Facts which signal 'danger'.
            return n => FactValueReaderFunction(FactType.Danger)(n) + FactValueReaderFunction(FactType.DangerFromUnknownSource)(n);
        }
        public Func<int, bool> HasDangerFromUnknownSourceReader() {
            return FactTruthReaderFunction(FactType.DangerFromUnknownSource);
        }
        public Func<int, bool> HasNoKnownPresenceReader() {
            return n => !FactTruthReaderFunction(FactType.FriendlyPresence)(n) && !FactTruthReaderFunction(FactType.EnemyPresence)(n);
        }
        public Func<int, bool> IsFriendlyAreaReader() {
            return n => FactTruthReaderFunction(FactType.FriendlyPresence)(n) && !FactTruthReaderFunction(FactType.EnemyPresence)(n);
        }
        public Func<int, bool> IsEnemyAreaReader() {
            return n => !FactTruthReaderFunction(FactType.FriendlyPresence)(n) && FactTruthReaderFunction(FactType.EnemyPresence)(n);
        }
        public Func<int, bool> IsContestedAreaReader() {
            return n => FactTruthReaderFunction(FactType.FriendlyPresence)(n) && FactTruthReaderFunction(FactType.EnemyPresence)(n);
        }

        // Public Interface - Clients can request reader function to read a subset of the graph. This describes the Effect reader function, which require preclusion table lookups.
        private Func<int, bool> EffectTruthReaderFunction(EffectType type) {
            return n => {
                // For this to be true, the effect type must directly be present in the node's EffectSum, or it has a fact which includes it.
                if (areaEffects[n].ContainsKey(type) || areaFacts[n].
            }
        }

        /* Class which is used by clients to read the current state of a Node. Data is not stored in this manner
         * since DynamicState has internal relationships required to update itself correctly and facilitate
         * reversible changes. This class will be constructed as a wrapper for the underlying data so that client
         * objects can read the state of a node without being exposed to the internal data structures. The states
         * here actually have relationships with each other (i.e. some are mutally exclusive, some imply each other,
         * etc, however this wrapper object is just presented without those semantics attached so that clients can
         * query our data without becoming dependent on those relationships. This allows us to change how we decide
         * a 'fact' becomes true or not without affecting our clients. I.e., we want to RESTRICT the client's ability
         * to make decisions based on our state rules, since those are unstable. */
        public class AreaNode {
            public int NodeId { get; }

            // The known number of friendly units in this area.
            public int FriendlyPresence { get; }
            public int EnemyPresence { get; }      // The known number of enemy units in this area.
            public int DangerLevel { get; }         // The amount of 'Danger' the unit has observed in the area. (E.g. witnessing a death in that spot)
            public bool IsDangerSourceKnown { get; }// True if there is danger but no known source of that danger.
            public bool UnknownPresence { get; }
            public bool IsFriendlyArea {            // True if there are no known enemies in the area, but there are known friendlies
                get { return FriendlyPresence > 0 && EnemyPresence <= 0; }
            }
            public bool IsEnemyArea {               // True if there are known enemies in the area, but no friendlies
                get { return FriendlyPresence <= 0 && EnemyPresence > 0; }
            }
            public bool IsContestedArea {           // True if there are currently both friendlies and enemies in the same area.
                get { return FriendlyPresence > 0 && EnemyPresence > 0; }
            }
            
            // DYNAMIC EFFECTS
            public bool IsClear { get; }            // Is visible to an area with friendlies in it and is not engaging in combat.
            public bool IsControlledByTeam { get; } // Has a 'controlOver' relationship from a friendly controlled area.
            public bool VisibleToEnemies { get; }   // Visible from an area with known enemies.
            public bool PotentialEnemies { get; }
            public bool IsControlledByEnemies { get; }
        }

        /* Class which is used by clients to read the edge data of a node pair. Data is not stored in this manner
         * (See comment above the AreaNode)
         * */
        public class AreaEdge {
            public int FromNodeId { get; }          // 'This' node (A)
            public int ToNodeId { get; }            // 'That' node (B)
            
            public bool IsCausingClearedState { get; }        // Is A causing B to be clear?
            public bool IsCausingControlledState { get; }
            public bool IsCausingControlledByEnemiesState { get; }
            public bool IsCausingVisibleToEnemiesState { get; }
            public bool IsCausingPotentialEnemiesState { get; }
        }
    }
}


/// <summary>
/// Objects which should not be imported and used by WorldRepresentation clients. These are intended to only be used by
/// The world representation data objects themselves, AND by the WorldUpdator logic classes (which are part of this module,
/// presenting a generic interface to the client objects).
/// </summary>
namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes {

    public enum FactType {
        FriendlyPresence,
        EnemyPresence,
        UnknownPresence,
        Danger,
        DangerFromUnknownSource
    }
    public enum EffectType {
        Clear,
        Controlled,
        VisibleToEnemies,
        PotentialEnemies,
        ControlledByEnemy
    }

    // Serves as a hardcoded lookup table which defines which types of effects preclude other types of effects from being true,
    // and which effects are automatically true given particular facts, and so on. Defined here so that this logic is centrally controlled and
    // does not concern the WorldUpdater Modules; it is modelled here since this logic is purely semantic (how the dynamic state model is defined).
    // TODO: 5 - Fact and Effect hardcoded relationships should be set up by reading a config file. Probably a structured data file (JSON, XML, etc.)
    // TODO: 5 - Implement more expressive Fact and Effect relationships. Atm, it's only what is automatically 'true' and 'false' depending on 1 to many relationship. Should eventually be able to make an effect true or false based on a conjunction of other facts and effects!
    public static class FactAndEffectRules {
        private static HashSet<EffectType>[] effectsPrecludedByEffectTable;     // Which effects are precluded by each given effect.
        private static HashSet<EffectType>[] effectsIncludedByFactTable;        // Which effects are included by each given fact.
        private static HashSet<EffectType>[] effectsPrecludedByFactTable;       // Which effects are precluded by each given fact. Preclusion supersedes Inclusion.

        private static HashSet<EffectType>[] effectsWhichPrecludeTable;         // Which effects preclude a given effect.
        private static HashSet<FactType>[] factsWhichPrecludeTable;             // Which facts preclude a given effect. Preclusion supersedes Inclusion.
        private static HashSet<FactType>[] factsWhichIncludeTable;              // Which facts include a given effect.
        static FactAndEffectRules() {
            effectsPrecludedByEffectTable = new HashSet<EffectType>[Enum.GetNames(typeof(EffectType)).Length];     // Number of EffectTypes
            effectsPrecludedByEffectTable[(int)EffectType.Clear] = new HashSet<EffectType>(new EffectType[] { EffectType.PotentialEnemies } );
            effectsPrecludedByEffectTable[(int)EffectType.Controlled] = new HashSet<EffectType>(new EffectType[] { EffectType.PotentialEnemies } );
            effectsPrecludedByEffectTable[(int)EffectType.ControlledByEnemy] = new HashSet<EffectType>(new EffectType[] { } );
            effectsPrecludedByEffectTable[(int)EffectType.VisibleToEnemies] = new HashSet<EffectType>(new EffectType[] { } );
            effectsPrecludedByEffectTable[(int)EffectType.PotentialEnemies] = new HashSet<EffectType>(new EffectType[] { } );

            effectsIncludedByFactTable = new HashSet<EffectType>[Enum.GetNames(typeof(FactType)).Length];    // Number of FactTypes
            effectsIncludedByFactTable[(int)FactType.FriendlyPresence] = new HashSet<EffectType>(new EffectType[] { EffectType.Clear, EffectType.Controlled });
            effectsIncludedByFactTable[(int)FactType.EnemyPresence] = new HashSet<EffectType>(new EffectType[] { EffectType.ControlledByEnemy, EffectType.VisibleToEnemies });
            effectsIncludedByFactTable[(int)FactType.UnknownPresence] = new HashSet<EffectType>(new EffectType[] { });
            effectsIncludedByFactTable[(int)FactType.Danger] = new HashSet<EffectType>(new EffectType[] { });
            effectsIncludedByFactTable[(int)FactType.DangerFromUnknownSource] = new HashSet<EffectType>(new EffectType[] { EffectType.PotentialEnemies });

            effectsPrecludedByFactTable = new HashSet<EffectType>[Enum.GetNames(typeof(FactType)).Length];  // Number of FactTypes
            effectsPrecludedByFactTable[(int)FactType.FriendlyPresence] = new HashSet<EffectType>(new EffectType[] { EffectType.ControlledByEnemy, EffectType.PotentialEnemies });
            effectsPrecludedByFactTable[(int)FactType.EnemyPresence] = new HashSet<EffectType>(new EffectType[] { EffectType.Clear, EffectType.Controlled });
            effectsPrecludedByFactTable[(int)FactType.UnknownPresence] = new HashSet<EffectType>(new EffectType[] { });
            effectsPrecludedByFactTable[(int)FactType.Danger] = new HashSet<EffectType>(new EffectType[] { });
            effectsPrecludedByFactTable[(int)FactType.DangerFromUnknownSource] = new HashSet<EffectType>(new EffectType[] { });

            effectsWhichPrecludeTable = new HashSet<EffectType>[Enum.GetNames(typeof(EffectType)).Length];
            effectsWhichPrecludeTable[(int)EffectType.PotentialEnemies] = new HashSet<EffectType>(new EffectType[] { EffectType.Clear, EffectType.Controlled });
            effectsWhichPrecludeTable[(int)EffectType.Clear] = new HashSet<EffectType>(new EffectType[] { });
            effectsWhichPrecludeTable[(int)EffectType.Controlled] = new HashSet<EffectType>(new EffectType[] { });
            effectsWhichPrecludeTable[(int)EffectType.VisibleToEnemies] = new HashSet<EffectType>(new EffectType[] { });
            effectsWhichPrecludeTable[(int)EffectType.ControlledByEnemy] = new HashSet<EffectType>(new EffectType[] { });

            factsWhichIncludeTable = new HashSet<FactType>[Enum.GetNames(typeof(EffectType)).Length];
            factsWhichIncludeTable[(int)EffectType.PotentialEnemies] = new HashSet<FactType>(new FactType[] { FactType.DangerFromUnknownSource });
            factsWhichIncludeTable[(int)EffectType.Clear] = new HashSet<FactType>(new FactType[] { FactType.FriendlyPresence });
            factsWhichIncludeTable[(int)EffectType.Controlled] = new HashSet<FactType>(new FactType[] { FactType.FriendlyPresence });
            factsWhichIncludeTable[(int)EffectType.VisibleToEnemies] = new HashSet<FactType>(new FactType[] { FactType.EnemyPresence });
            factsWhichIncludeTable[(int)EffectType.ControlledByEnemy] = new HashSet<FactType>(new FactType[] { FactType.EnemyPresence });

            factsWhichPrecludeTable = new HashSet<FactType>[Enum.GetNames(typeof(EffectType)).Length];
            factsWhichPrecludeTable[(int)EffectType.PotentialEnemies] = new HashSet<FactType>(new FactType[] { FactType.FriendlyPresence });
            factsWhichPrecludeTable[(int)EffectType.Clear] = new HashSet<FactType>(new FactType[] { FactType.EnemyPresence });
            factsWhichPrecludeTable[(int)EffectType.Controlled] = new HashSet<FactType>(new FactType[] { FactType.EnemyPresence });
            factsWhichPrecludeTable[(int)EffectType.VisibleToEnemies] = new HashSet<FactType>(new FactType[] {  });
            factsWhichPrecludeTable[(int)EffectType.ControlledByEnemy] = new HashSet<FactType>(new FactType[] { FactType.FriendlyPresence });
        }

        public static HashSet<EffectType> GetEffectsPrecludedByEffect(EffectType e) {
            return effectsPrecludedByEffectTable[(int)e];
        }
        public static HashSet<EffectType> GetEffectsPrecludedByFact(FactType f) {
            return effectsPrecludedByFactTable[(int)f];
        }
        public static HashSet<EffectType> GetEffectsIncludedByFact(FactType f) {
            return effectsIncludedByFactTable[(int)f];
        }

        public static HashSet<EffectType> EffectsWhichPreclude(EffectType e) {
            return effectsWhichPrecludeTable[(int)e];
        }
        public static HashSet<FactType> FactsWhichPreclude(EffectType e) {
            return factsWhichPrecludeTable[(int)e];
        }
        public static HashSet<FactType> FactsWhichInclude(EffectType e) {
            return factsWhichIncludeTable[(int)e];
        }
    }

    public class Fact {
        public FactType FactType { get; private set; }   // Identifies what kind of fact this is! The enum is not to be visible outside of the WorldRepresentation Module.
        public int Value { get; private set; }           // The 'magnitude' of the fact type, if applicable.
        private List<Effect> effectsCaused;                  // A List of all the nodes which this fact is causing 'Effects' upon.
        public IReadOnlyCollection<Effect> EffectsCaused {
            get { return effectsCaused.AsReadOnly(); }
        }
        public Fact(FactType factType, int value, List<Effect> effectsCaused) {
            FactType = factType;
            Value = value;
            this.effectsCaused = effectsCaused;
        }

        public sealed class MutableFact : Fact {
            public MutableFact(FactType factType, int value, List<Effect> effects) : base(factType, value, effects) { }
            public MutableFact(Fact toClone) : base(toClone.FactType, toClone.Value, CloneList(toClone.EffectsCaused)) { }
            // Mutators. Only to be used by builder classes, such as the world updator
            public void SetValue(int value) { Value = value; }
            public void SetFactType(FactType f) { FactType = f; }
            public List<Effect> AccessEffectsCausedList() { return effectsCaused; }

            private static List<Effect> CloneList(IEnumerable<Effect> c) {
                List<Effect> toRet = new List<Effect>();
                foreach (Effect e in c) toRet.Add(e);
                return toRet;
            }
        }
    }

    // Represents the aggregation of multiple same-type effects, when the same 'effect type' is applied to a node from more than one origin.
    // MUTABLE - MUST NOT BE EXPOSED TO CLIENTS
    public class EffectSum {
        public EffectType? EffectType { get; set; }
        public int ValueSum { get; set; }
        public int MaxValue { get; set; }
        public List<int> CauseNodes { get; }
        public void IncorporateNewEffect(Effect e) {
            CauseNodes.Add(e.CauseNodeId);
            if (EffectType == null) {
                // This is the first incorporated effect!
                EffectType = e.EffectType;
                ValueSum = MaxValue = e.Value;
            }
            else if (EffectType == e.EffectType) {
                ValueSum += e.Value;
                MaxValue = MaxValue > e.Value ? MaxValue : e.Value;
            }
            else { throw new ArgumentException("EffectSum cannot incorporate Effect objects with different EffectTypes"); }
        }
        public EffectSum() {
            CauseNodes = new List<int>();
            EffectType = null;
        }
        public EffectSum(Effect e) {
            CauseNodes = new List<int>();
            IncorporateNewEffect(e);
        }
        public EffectSum(IEnumerable<Effect> es) {
            CauseNodes = new List<int>();
            foreach (Effect e in es) IncorporateNewEffect(e);
        }
    }

    public class Effect {
        public EffectType EffectType { get; private set; }
        public int Value { get; private set; }
        public int NodeId { get; private set; }
        public int CauseNodeId { get; private set; }

        public Effect(EffectType effectType, int value, int affectedNodeId, int causeNodeId) {
            EffectType = effectType;
            Value = value;
            NodeId = affectedNodeId;
            CauseNodeId = causeNodeId;
        }

        public override bool Equals(object obj) {
            if (obj is Effect) {
                Effect o = (Effect)obj;
                return (this.EffectType == o.EffectType && this.Value == o.Value && this.NodeId == o.NodeId);
            }
            else {
                return false;
            }
        }
        public override int GetHashCode() {
            return EffectType.GetHashCode() * 17 + Value.GetHashCode() * 17 + NodeId.GetHashCode();
        }

        public sealed class MutableEffect : Effect {
            public MutableEffect(EffectType effectType, int value, int nodeId, int causeId) : base(effectType, value, nodeId, causeId) { }
            public MutableEffect(Effect toClone) : base(toClone.EffectType, toClone.Value, toClone.NodeId, toClone.CauseNodeId) { }
            public void SetEffectType(EffectType e) { EffectType = e; }
            public void SetValue(int v) { Value = v; }
            public void SetNodeId(int n) { NodeId = n; }
            public void SetCauseId(int n) { CauseNodeId = n; }
        }
    }
}
