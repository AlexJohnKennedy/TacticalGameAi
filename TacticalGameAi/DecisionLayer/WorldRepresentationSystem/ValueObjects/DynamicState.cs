using System;
using System.Collections;
using System.Collections.Generic;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    public class DynamicState {

        // The original representation of data
        private Dictionary<FactType, Fact>[] areaFacts;
        private Dictionary<EffectType, EffectSum>[] areaEffectSums;
        private List<Effect>[,] areaEffects;

        // Internal interface. Special priviledged objects can use these to read the internal data of a DynamicState (e.g. WorldUpdator Logic)
        internal Dictionary<FactType, Fact> GetNodeFact(int id) { return areaFacts[id]; }
        internal Dictionary<EffectType, EffectSum> GetNodeEffectSum(int id) { return areaEffectSums[id]; }

        // Redundent representation of info built upon construction to allow clients faster lookups to info we have already calculated.
        private AreaNode[]  areaNodes;
        private AreaEdge[,] areaEdges;

        // Public Interface - Directly get all Vertex and Edge data
        public AreaNode GetNodeData(int nodeId) {
            if (areaNodes[nodeId] == null) areaNodes[nodeId] = new AreaNode(nodeId, KnownFriendlyPresenceReader(), KnownEnemyPresenceReader(), KnownDangerLevelReader(), HasDangerFromUnknownSourceReader(), HasNoKnownPresenceReader(), IsFriendlyAreaReader(), IsEnemyAreaReader(), IsContestedAreaReader(), IsClearReader(), IsControlledByTeamReader(), IsControlledByEnemiesReader(), VisibleToEnemiesReader(), PotentialEnemiesReader());
            return areaNodes[nodeId];
        }
        public AreaEdge GetEdge(int fromNode, int toNode) {
            if (areaEdges[fromNode, toNode] == null) areaEdges[fromNode, toNode] = new AreaEdge(fromNode, toNode, CausingClearEffectReader(), CausingControlledByTeamEffectReader(), CausingControlledByEnemiesEffectReader(), CausingVisibleToEnemiesEffectReader(), CausingPotentialEnemiesEffectReader());
            return areaEdges[fromNode, toNode];
        }

        /* TODO: 7 - Implement a constructor which allows the WorldUpdater object (or whoever is building this) to pass in a pre-made Effect edge matrix and a pre-made EffectSum array.
         * This would probably be more efficient because the world updater can just build these collections as it searches to apply effects, rather than re-doing to work in the DS constructor
         * as is currently the case (in the triple nested for-each loop inside the current one. */
        public DynamicState(Dictionary<FactType, Fact>[] facts) {
            areaFacts = facts ?? throw new ArgumentNullException("facts", "Dynamic State received null fact array.");
            areaEffectSums = new Dictionary<EffectType, EffectSum>[facts.Length];
            for (int i=0; i<facts.Length; i++) { areaEffectSums[i] = new Dictionary<EffectType, EffectSum>(); }
            areaEffects = new List<Effect>[facts.Length, facts.Length];
            areaNodes = new AreaNode[facts.Length];
            areaEdges = new AreaEdge[facts.Length, facts.Length];

            // Populate EffectSum dictionaries for each node, to identify all the effects they have on each node.
            foreach (Dictionary<FactType,Fact> dict in facts) {
                foreach (Fact f in dict.Values) {
                    foreach (Effect e in f.EffectsCaused) {
                        AddEffectToEffectSum(e.NodeId, e);
                        if (areaEffects[e.CauseNodeId, e.NodeId] == null) { areaEffects[e.CauseNodeId, e.NodeId] = new List<Effect>(); }
                        areaEffects[e.CauseNodeId, e.NodeId].Add(e);
                    }
                }
            }
        }
        private void AddEffectToEffectSum(int node, Effect e) {
            if (areaEffectSums[node].ContainsKey(e.EffectType)) {
                areaEffectSums[node][e.EffectType].IncorporateNewEffect(e);
            }
            else {
                areaEffectSums[node].Add(e.EffectType, new EffectSum(e));
            }
        }
        // Constructor which facilitates updating an old dynamic state with new changes more easily.
        public DynamicState(DynamicState oldState, Dictionary<int, Dictionary<FactType, Fact>> changes) {
            if (oldState == null) throw new ArgumentNullException("oldState", "Dynamic State received null old state in constructor");
            // Copy all the old facts into new facts, unless it appears in the changes dict, in which case use the new one!
            Dictionary<FactType, Fact>[] oldfacts = oldState.areaFacts;
            areaFacts = new Dictionary<FactType, Fact>[oldfacts.Length];
            for (int i=0; i < oldfacts.Length; i++) {
                // Check if the i'th node is a node with 'changes'.
                if (changes.ContainsKey(i)) {
                    areaFacts[i] = changes[i];
                }
                else {
                    areaFacts[i] = new Dictionary<FactType, Fact>(oldfacts[i]);
                }

                // Calculate the EffectSums
                foreach (Fact f in areaFacts[i].Values) {
                    foreach (Effect e in f.EffectsCaused) {
                        AddEffectToEffectSum(e.NodeId, e);
                        if (areaEffects[e.CauseNodeId, e.NodeId] == null) { areaEffects[e.CauseNodeId, e.NodeId] = new List<Effect>(); }
                        areaEffects[e.CauseNodeId, e.NodeId].Add(e);
                    }
                }
            }
        }

        // Public Interface - Clients can request reader functions to read a subset of the graph. This describes the Fact reader functions.
        private Func<int, int> FactValueReaderFunction(FactType type) {
            return n => areaFacts[n].ContainsKey(type) ? areaFacts[n][type].Value : 0;
        }
        private Func<int, bool> FactTruthReaderFunction(FactType type) {
            return n => areaFacts[n].ContainsKey(type);
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
                if (areaEffectSums[n].ContainsKey(type) || FactAndEffectRules.FactsWhichInclude(type).Overlaps(areaFacts[n].Keys)) {
                    // If there are ALSO any effects or facts which preclude it, then we still must return false since preclusion supersedes inclusion in the current model.
                    if (FactAndEffectRules.EffectsWhichPreclude(type).Overlaps(areaEffectSums[n].Keys) || FactAndEffectRules.FactsWhichPreclude(type).Overlaps(areaFacts[n].Keys)) {
                        return false;
                    }
                    else {
                        return true;
                    }
                }
                else {
                    return false;
                }
            };
        }
        public Func<int, bool> IsClearReader() {
            return EffectTruthReaderFunction(EffectType.Clear);
        }
        public Func<int, bool> IsControlledByTeamReader() {
            return EffectTruthReaderFunction(EffectType.Controlled);
        }
        public Func<int, bool> VisibleToEnemiesReader() {
            return EffectTruthReaderFunction(EffectType.VisibleToEnemies);
        }
        public Func<int, bool> PotentialEnemiesReader() {
            return EffectTruthReaderFunction(EffectType.PotentialEnemies);
        }
        public Func<int, bool> IsControlledByEnemiesReader() {
            return EffectTruthReaderFunction(EffectType.ControlledByEnemy);
        }

        private Func<int, int, bool> EdgeDataReaderFunction(EffectType type) {
            return (from, to) => {
                if (areaEffects[from, to] == null) return false;
                foreach(Effect e in areaEffects[from,to]) {
                    if (e.EffectType == type) return true;
                }
                return false;
            };
        }
        public Func<int, int, bool> CausingClearEffectReader() {
            return EdgeDataReaderFunction(EffectType.Clear);
        }
        public Func<int, int, bool> CausingControlledByTeamEffectReader() {
            return EdgeDataReaderFunction(EffectType.Controlled);
        }
        public Func<int, int, bool> CausingControlledByEnemiesEffectReader() {
            return EdgeDataReaderFunction(EffectType.ControlledByEnemy);
        }
        public Func<int, int, bool> CausingVisibleToEnemiesEffectReader() {
            return EdgeDataReaderFunction(EffectType.VisibleToEnemies);
        }
        public Func<int, int, bool> CausingPotentialEnemiesEffectReader() {
            return EdgeDataReaderFunction(EffectType.PotentialEnemies);
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

            // Cached values for this node. Nullable values since upon creation of this object they have not been looked up yet.
            private int? friendlyPresence;
            private int? enemyPresence;
            private int? dangerLevel;
            private bool? hasDangerFromUnknownSource;
            private bool? unknownPresence;
            private bool? isFriendlyArea;
            private bool? isEnemyArea;
            private bool? isContestedArea;
            private bool? isClear;
            private bool? isControlledByTeam;
            private bool? isControlledByEnemies;
            private bool? visibleToEnemies;
            private bool? potentialEnemies;

            // Lookup functions, used to lookup data values if they have not been cached yet.
            private Func<int, int> friendlyPresenceReader;
            private Func<int, int> enemyPresenceReader;
            private Func<int, int> dangerLevelReader;
            private Func<int, bool> hasDangerFromUnknownSourceReader;
            private Func<int, bool> unknownPresenceReader;
            private Func<int, bool> isFriendlyAreaReader;
            private Func<int, bool> isEnemyAreaReader;
            private Func<int, bool> isContestedAreaReader;
            private Func<int, bool> isClearReader;
            private Func<int, bool> isControlledByTeamReader;
            private Func<int, bool> isControlledByEnemiesReader;
            private Func<int, bool> visibleToEnemiesReader;
            private Func<int, bool> potentialEnemiesReader;

            // Public Properties which clients will use to query data about this node!
            public int FriendlyPresence {
                get {
                    if (friendlyPresence == null) { friendlyPresence = friendlyPresenceReader(NodeId); }
                    return friendlyPresence.Value;
                }
            }
            public int EnemyPresence {
                get {
                    if (enemyPresence == null) { enemyPresence = enemyPresenceReader(NodeId); }
                    return enemyPresence.Value;
                }
            }
            public int DangerLevel {
                get {
                    if (dangerLevel == null) { dangerLevel = dangerLevelReader(NodeId); }
                    return dangerLevel.Value;
                }
            }
            public bool HasDangerFromUnknownSource {
                get {
                    if (hasDangerFromUnknownSource == null) { hasDangerFromUnknownSource = hasDangerFromUnknownSourceReader(NodeId); }
                    return hasDangerFromUnknownSource.Value;
                }
            }
            public bool UnknownPresence {
                get {
                    if (unknownPresence == null) { unknownPresence = unknownPresenceReader(NodeId); }
                    return unknownPresence.Value;
                }
            }
            public bool IsFriendlyArea {
                get {
                    if (isFriendlyArea == null) isFriendlyArea = isFriendlyAreaReader(NodeId);
                    return isFriendlyArea.Value;
                }
            }
            public bool IsEnemyArea {
                get {
                    if (isEnemyArea == null) isEnemyArea = isEnemyAreaReader(NodeId);
                    return isEnemyArea.Value;
                }
            }
            public bool IsContestedArea {
                get {
                    if (isContestedArea == null) isContestedArea = isContestedAreaReader(NodeId);
                    return isContestedArea.Value;
                }
            }
            public bool IsClear {
                get {
                    if (isClear == null) isClear = isClearReader(NodeId);
                    return isClear.Value;
                }
            }
            public bool IsControlledByTeam {
                get {
                    if (isControlledByTeam == null) isControlledByTeam = isControlledByTeamReader(NodeId);
                    return isControlledByTeam.Value;
                }
            }
            public bool IsControlledByEnemies {
                get {
                    if (isControlledByEnemies == null) { isControlledByEnemies = isControlledByEnemiesReader(NodeId); }
                    return isControlledByEnemies.Value;
                }
            }
            public bool VisibleToEnemies {
                get {
                    if (visibleToEnemies == null) visibleToEnemies = visibleToEnemiesReader(NodeId);
                    return visibleToEnemies.Value;
                }
            }
            public bool PotentialEnemies {
                get {
                    if (potentialEnemies == null) potentialEnemies = potentialEnemiesReader(NodeId);
                    return potentialEnemies.Value;
                }
            }

            // Constructor with horrificly long paramter list, but it's fine coz it's only used in one location.
            internal AreaNode(int nodeId, Func<int, int> friendlyPresenceReader, Func<int, int> enemyPresenceReader, Func<int, int> dangerLevelReader, Func<int, bool> hasDangerFromUnknownSourceReader, Func<int, bool> unknownPresenceReader, Func<int, bool> isFriendlyAreaReader, Func<int, bool> isEnemyAreaReader, Func<int, bool> isContestedAreaReader, Func<int, bool> isClearReader, Func<int, bool> isControlledByTeamReader, Func<int, bool> isControlledByEnemiesReader, Func<int, bool> visibleToEnemiesReader, Func<int, bool> potentialEnemiesReader) {
                NodeId = nodeId;
                this.friendlyPresenceReader = friendlyPresenceReader;
                this.enemyPresenceReader = enemyPresenceReader;
                this.dangerLevelReader = dangerLevelReader;
                this.hasDangerFromUnknownSourceReader = hasDangerFromUnknownSourceReader;
                this.unknownPresenceReader = unknownPresenceReader;
                this.isFriendlyAreaReader = isFriendlyAreaReader;
                this.isEnemyAreaReader = isEnemyAreaReader;
                this.isContestedAreaReader = isContestedAreaReader;
                this.isClearReader = isClearReader;
                this.isControlledByTeamReader = isControlledByTeamReader;
                this.isControlledByEnemiesReader = isControlledByEnemiesReader;
                this.visibleToEnemiesReader = visibleToEnemiesReader;
                this.potentialEnemiesReader = potentialEnemiesReader;

                // All cached values should be automatically initialised to null, since they are fields.
            }
        }

        /* Class which is used by clients to read the edge data of a node pair. Data is not stored in this manner
         * (See comment above the AreaNode)
         * */
        public class AreaEdge {
            public int FromNodeId { get; }          // 'This' node (A)
            public int ToNodeId { get; }            // 'That' node (B)

            private bool? isCausingClearEffect;
            private bool? isCausingControlledByTeamEffect;
            private bool? isCausingControlledByEnemiesEffect;
            private bool? isCausingVisibleToEnemiesEffect;
            private bool? isCausingPotentialEnemiesEffect;

            private Func<int, int, bool> causingClearEffectReader;
            private Func<int, int, bool> causingControlledByTeamEffectReader;
            private Func<int, int, bool> causingControlledByEnemiesEffectReader;
            private Func<int, int, bool> causingVisibleToEnemiesEffectReader;
            private Func<int, int, bool> causingPotentialEnemiesEffectReader;

            public bool IsCausingClearEffect {
                get {
                    if (isCausingClearEffect == null) isCausingClearEffect = causingClearEffectReader(FromNodeId, ToNodeId);
                    return isCausingClearEffect.Value;
                }
            }
            public bool IsCausingControlledByTeamEffect {
                get {
                    if (isCausingControlledByTeamEffect == null) isCausingControlledByTeamEffect = causingControlledByTeamEffectReader(FromNodeId, ToNodeId);
                    return isCausingControlledByTeamEffect.Value;
                }
            }
            public bool IsCausingControlledByEnemiesEffect {
                get {
                    if (isCausingControlledByEnemiesEffect == null) isCausingControlledByEnemiesEffect = causingControlledByEnemiesEffectReader(FromNodeId, ToNodeId);
                    return isCausingControlledByEnemiesEffect.Value;
                }
            }
            public bool IsCausingVisibleToEnemiesEffect {
                get {
                    if (isCausingVisibleToEnemiesEffect == null) isCausingVisibleToEnemiesEffect = causingVisibleToEnemiesEffectReader(FromNodeId, ToNodeId);
                    return isCausingVisibleToEnemiesEffect.Value;
                }
            }
            public bool IsCausingPotentialEnemiesEffect {
                get {
                    if (isCausingPotentialEnemiesEffect == null) isCausingPotentialEnemiesEffect = causingPotentialEnemiesEffectReader(FromNodeId, ToNodeId);
                    return isCausingPotentialEnemiesEffect.Value;
                }
            }

            internal AreaEdge(int fromNodeId, int toNodeId, Func<int, int, bool> causingClearEffectReader, Func<int, int, bool> causingControlledByTeamEffectReader, Func<int, int, bool> causingControlledByEnemiesEffectReader, Func<int, int, bool> causingVisibleToEnemiesEffectReader, Func<int, int, bool> causingPotentialEnemiesEffectReader) {
                FromNodeId = fromNodeId;
                ToNodeId = toNodeId;
                this.causingClearEffectReader = causingClearEffectReader;
                this.causingControlledByTeamEffectReader = causingControlledByTeamEffectReader;
                this.causingControlledByEnemiesEffectReader = causingControlledByEnemiesEffectReader;
                this.causingVisibleToEnemiesEffectReader = causingVisibleToEnemiesEffectReader;
                this.causingPotentialEnemiesEffectReader = causingPotentialEnemiesEffectReader;
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

    public enum FactType {
        FriendlyPresence,
        EnemyPresence,
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
            effectsIncludedByFactTable[(int)FactType.Danger] = new HashSet<EffectType>(new EffectType[] { });
            effectsIncludedByFactTable[(int)FactType.DangerFromUnknownSource] = new HashSet<EffectType>(new EffectType[] { EffectType.PotentialEnemies });

            effectsPrecludedByFactTable = new HashSet<EffectType>[Enum.GetNames(typeof(FactType)).Length];  // Number of FactTypes
            effectsPrecludedByFactTable[(int)FactType.FriendlyPresence] = new HashSet<EffectType>(new EffectType[] { EffectType.ControlledByEnemy, EffectType.PotentialEnemies });
            effectsPrecludedByFactTable[(int)FactType.EnemyPresence] = new HashSet<EffectType>(new EffectType[] { EffectType.Clear, EffectType.Controlled });
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

    // Specialised reader class allowing a client to read the actual 'FACT' objects which make up the internal representation of the dynamic state. Should
    // only be used by objects who are priviledged enough to reference the other hidden types. A wrapper to access internal functions, such that you have to
    // import a separate namespace (hiddentype namespace) to access them!
    public static class DynamicStateInternalReader {
        public static Dictionary<FactType, Fact> GetNodeFact(int id, DynamicState state) {
            return state.GetNodeFact(id);
        }
        public static Dictionary<EffectType, EffectSum> GetNodeEffectSum(int id, DynamicState state) {
            return state.GetNodeEffectSum(id);
        }
    }
}
