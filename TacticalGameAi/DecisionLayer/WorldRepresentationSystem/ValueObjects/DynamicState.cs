using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    public class DynamicState {

        // The original representation of data
        private FactSet[] areaFacts;         // Each Node can have a number of facts.
        private EffectSet[] areaEffects;

        // Redundent representation of info built upon construction to allow clients faster lookups to the current info
        private AreaNode[]  areaNodes;
        private AreaEdge[,] areaEdges;

        // Used by anyone who wants to update the world. Note that the WorldUpdator must be doing this via the World Rep
        // to also have access to the underlying StaticState!
        public IReadOnlyCollection<FactSet> AreaFacts {
            get { return Array.AsReadOnly(areaFacts); }
        }
        public IReadOnlyCollection<EffectSet> AreaEffects {
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

        public DynamicState(FactSet[] facts) {
            areaFacts = facts ?? throw new ArgumentNullException("facts", "Dynamic State received null fact array.");

            // Build redundent wrapper data
            areaNodes = new AreaNode[facts.Length];
            areaEdges = new AreaEdge[facts.Length, facts.Length];

            PopulateRedundentArrays(facts, areaNodes, areaEdges);
        }

        private void PopulateRedundentArrays(FactSet[] facts, AreaNode[] nodes, AreaEdge[,] edges) {
            // We assume that the facts in each node's FactSet are valid; I.e., the world updator has not passed us multiple facts as true when they are mutually exclusive!

            // Store all area data in temp arrays so we can construct the data fully before instantiating the immutable wrapper objects.
            int n = facts.Length;
            // FACTS
            int[] friendlyPresence = new int[n];
            int[] enemyPresence = new int[n];
            int[] dangerLevel = new int[n];
            bool[] isDangerSourceKnown = new bool[n];
            bool[] unknownPresence = new bool[n];
            bool[] isFriendlyArea = new bool[n];
            // EFFECTS
            bool[] isClear = new bool[n];
            bool[] isControlledByTeam = new bool[n];
            bool[] visibleToEnemies = new bool[n];
            bool[] potentialEnemies = new bool[n];
            bool[] isControlledByEnemy = new bool[n];
            // EDGE DATA
            bool[,] causingClear = new bool[n,n];
            bool[,] causingControlled = new bool[n,n];
            bool[,] causingControlledByEnemy = new bool[n,n];
            bool[,] causingVisibleToEnemies = new bool[n,n];
            bool[,] causingPotentialEnemies = new bool[n,n];

            // EFFECT HASHSETS
            HashSet<Effect>[] esets = new HashSet<Effect>[n];

            for (int i = 0; i < n; i++) {
                esets[i] = new HashSet<Effect>();

                // Use all the facts in the set to populate the fact based data.
                foreach (Fact fact in facts[i]) {
                    // Store knowledge of this fact in node i
                    switch (fact.FactType) {
                        case FactType.FriendlyPresence:
                            friendlyPresence[i] = fact.Value;
                            break;
                        case FactType.EnemyPresence:
                            enemyPresence[i] = fact.Value;  
                            break;
                        case FactType.UnknownPresence:
                            unknownPresence[i] = true;
                            break;
                        case FactType.Danger:
                            dangerLevel[i] = fact.Value;
                            isDangerSourceKnown[i] = true;
                            break;
                        case FactType.DangerFromUnknownSource:
                            dangerLevel[i] = fact.Value;
                            isDangerSourceKnown[i] = false;
                            break;
                        default:
                            break;
                    }

                    // Store knowledge of the effects it's causing in edges, and in target nodes.
                    foreach (Effect effect in fact.EffectsCaused) {
                        esets[effect.NodeId].Add(effect);
                        switch (effect.EffectType) {
                            case EffectType.Clear:
                                causingClear[i,effect.NodeId] = true;
                                isClear[effect.NodeId] = true;
                                break;
                            case EffectType.Controlled:
                                causingControlled[i, effect.NodeId] = true;
                                isControlledByTeam[effect.NodeId] = true;
                                break;
                            case EffectType.VisibleToEnemies:
                                causingVisibleToEnemies[i, effect.NodeId] = true;
                                visibleToEnemies[effect.NodeId] = true;
                                break;
                            case EffectType.PotentialEnemies:
                                causingPotentialEnemies[i, effect.NodeId] = true;
                                potentialEnemies[effect.NodeId] = true;
                                break;
                            case EffectType.ControlledByEnemy:
                                causingControlledByEnemy[i, effect.NodeId] = true;
                                isControlledByEnemy[effect.NodeId] = true;
                                break;
                        }
                    }
                }
            }

            // Do pass on aggregated effects to remove precluded effect states.
            for (int i=0; i<n;i++) {
                areaEffects[i] = new EffectSet(esets[i]);
                foreach(Effect e in esets[i]) {
                    foreach(EffectType t in e.PrecludedEffectTypes) {
                        // EffectType 't' is being caused, but is precluded from node 'i' // TODO: MAKE THE FACT THAT AN EFFECT STATE IS CAUSED BUT PRECLUDED VISIBLE TO CLIENTS?
                        switch (t) {
                            case EffectType.Clear:
                                isClear[i] = false;
                                break;
                            case EffectType.Controlled:
                                isControlledByTeam[i] = false;
                                break;
                            case EffectType.VisibleToEnemies:
                                visibleToEnemies[i] = false;
                                break;
                            case EffectType.PotentialEnemies:
                                potentialEnemies[i] = false;
                                break;
                            case EffectType.ControlledByEnemy:
                                isControlledByEnemy[i] = false;
                                break;
                        }
                    }
                }

                // Finally, build the damn data objects!
                nodes[i] = new AreaNode(i, friendlyPresence[i], enemyPresence[i], dangerLevel[i], isDangerSourceKnown[i], unknownPresence[i], isClear[i], isControlledByTeam[i], visibleToEnemies[i], potentialEnemies[i], isControlledByEnemy[i]);
                for (int j=0; j<n; j++) {
                    edges[i, j] = new AreaEdge(i, j, causingClear[i, j], causingControlled[i, j], causingControlledByEnemy[i, j], causingVisibleToEnemies[i, j], causingPotentialEnemies[i, j]);
                }
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


    public class FactSet : IEnumerable<Fact> {
        private HashSet<Fact> facts;

        // Users can only cycle through all of the facts in the set, nothing more.
        public IEnumerator<Fact> GetEnumerator() {
            return facts.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return facts.GetEnumerator();
        }

        public FactSet(HashSet<Fact> facts) {
            this.facts = facts;
        }
    }
    public class EffectSet : IEnumerable<Effect> {
        private HashSet<Effect> effects;
        public EffectSet(HashSet<Effect> effects) {
            this.effects = effects;
        }
        public IEnumerator<Effect> GetEnumerator() {
            return effects.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return effects.GetEnumerator();
        }
    }

    public class Fact {
        public FactType FactType { get; private set; }   // Identifies what kind of fact this is! The enum is not to be visible outside of the WorldRepresentation Module.
        public int Value { get; private set; }           // The 'magnitude' of the fact type, if applicable.
        private Effect[] effectsCaused;                  // A List of all the nodes which this fact is causing 'Effects' upon.
        public IReadOnlyCollection<Effect> EffectsCaused {
            get { return Array.AsReadOnly(effectsCaused); }
        }
        public Fact(FactType factType, int value, Effect[] effectsCaused) {
            FactType = factType;
            Value = value;
            this.effectsCaused = effectsCaused;
        }

        public sealed class MutableFact : Fact {
            public MutableFact(FactType factType, int value, Effect[] effects) : base(factType, value, effects) { }
            // Mutators. Only to be used by builder classes, such as the world updator
            public void SetValue(int value) { Value = value; }
            public void SetFactType(FactType f) { FactType = f; }
            public Effect[] AccessEffectsCausedArray() { return effectsCaused; }
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
        private EffectType[] precludedEffectTypes;
        public IReadOnlyCollection<EffectType> PrecludedEffectTypes {
            get { return Array.AsReadOnly(precludedEffectTypes); }
        }
        public int Value { get; private set; }
        public int NodeId { get; private set; }

        public Effect(EffectType effectType, int value, int nodeId, EffectType[] precludedTypes) {
            EffectType = effectType;
            Value = value;
            NodeId = nodeId;
            precludedEffectTypes = precludedTypes;
        }

        public sealed class MutableEffect : Effect {
            public MutableEffect(EffectType effectType, int value, int causeCount, int nodeId, EffectType[] precludedTypes) : base(effectType, value, nodeId, precludedTypes) { }
            public void SetEffectType(EffectType e) { EffectType = e; }
            public void SetValue(int v) { Value = v; }
            public void SetNodeId(int n) { NodeId = n; }
            public EffectType[] AccessPrecludedEffectTypesArray() { return precludedEffectTypes; }
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
