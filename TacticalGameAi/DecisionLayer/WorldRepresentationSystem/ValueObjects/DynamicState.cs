using System;
using System.Collections;
using System.Collections.Generic;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    public class DynamicState {

        // The original representation of data
        private HashSet<Fact>[] areaFacts;         // Each Node can have a number of facts.
        private HashSet<Effect>[] areaEffects;

        // Redundent representation of info built upon construction to allow clients faster lookups to the current info
        private AreaNode[]  areaNodes;
        private AreaEdge[,] areaEdges;

        // Used by anyone who wants to update the world. Note that the WorldUpdator must be doing this via the World Rep
        // to also have access to the underlying StaticState!
        public IReadOnlyCollection<IEnumerable<Fact>> AreaFacts {
            get { return Array.AsReadOnly(areaFacts); }
        }
        public IReadOnlyCollection<IEnumerable<Effect>> AreaEffects {
            get { return Array.AsReadOnly(areaEffects); }
        }

        // Public Interface - Directly get all Vertex and Edge data
        public AreaNode GetNodeData(int nodeId) {
            // For performance, this value is not checked.
            return areaNodes[nodeId];
        }
        public AreaEdge GetEdge(int fromNode, int toNode) {
            // For performance, these values are not checked.
            return areaEdges[fromNode, toNode];
        }

        public DynamicState(HashSet<Fact>[] facts) {
            areaFacts = facts ?? throw new ArgumentNullException("facts", "Dynamic State received null fact array.");
            areaEffects = new HashSet<Effect>[facts.Length];

            // Build redundent wrapper data
            areaNodes = new AreaNode[facts.Length];
            areaEdges = new AreaEdge[facts.Length, facts.Length];
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
            
            // DYNAMIC FACTS
            public int FriendlyPresence { get; }   // The known number of friendly units in this area.
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

            public AreaNode(int nodeId, int friendlyPresence, int enemyPresence, int dangerLevel, bool isDangerSourceKnown, bool unknownPresence, bool isClear, bool isControlledByTeam, bool visibleToEnemies, bool potentialEnemies, bool isControlledByEnemies) {
                NodeId = nodeId;
                FriendlyPresence = friendlyPresence;
                EnemyPresence = enemyPresence;
                DangerLevel = dangerLevel;
                IsDangerSourceKnown = isDangerSourceKnown;
                UnknownPresence = unknownPresence;
                IsClear = isClear;
                IsControlledByTeam = isControlledByTeam;
                VisibleToEnemies = visibleToEnemies;
                PotentialEnemies = potentialEnemies;
                IsControlledByEnemies = isControlledByEnemies;
            }
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

            public AreaEdge(int fromNodeId, int toNodeId, bool isCausingClearedState, bool isCausingControlledState, bool isCausingControlledByEnemiesState, bool isCausingVisibleToEnemiesState, bool isCausingPotentialEnemiesState) {
                FromNodeId = fromNodeId;
                ToNodeId = toNodeId;
                IsCausingClearedState = isCausingClearedState;
                IsCausingControlledState = isCausingControlledState;
                IsCausingControlledByEnemiesState = isCausingControlledByEnemiesState;
                IsCausingVisibleToEnemiesState = isCausingVisibleToEnemiesState;
                IsCausingPotentialEnemiesState = isCausingPotentialEnemiesState;
            }
        }
    }
}


/// <summary>
/// Objects which should not be imported and used by WorldRepresentation clients. These are intended to only be used by
/// The world representation data objects themselves, AND by the WorldUpdator logic classes (which are part of this module,
/// presenting a generic interface to the client objects).
/// </summary>
namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes {

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

    public enum FactType {
        FriendlyPresence,
        EnemyPresence,
        UnknownPresence,
        Danger,
        DangerFromUnknownSource
    }

    public class Effect {
        public EffectType EffectType { get; private set; }
        public int Value { get; private set; }
        public int NodeId { get; private set; }

        public Effect(EffectType effectType, int value, int nodeId) {
            EffectType = effectType;
            Value = value;
            NodeId = nodeId;
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

        public sealed class MutableEffect : Effect {
            public MutableEffect(EffectType effectType, int value, int nodeId) : base(effectType, value, nodeId) { }
            public MutableEffect(Effect toClone) : base(toClone.EffectType, toClone.Value, toClone.NodeId) { }
            public void SetEffectType(EffectType e) { EffectType = e; }
            public void SetValue(int v) { Value = v; }
            public void SetNodeId(int n) { NodeId = n; }
        }
    }

    public enum EffectType {
        Clear,
        Controlled,
        VisibleToEnemies,
        PotentialEnemies,
        ControlledByEnemy
    }
}
