using System;
using System.Collections.Generic;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ContactPointCalculationSystem;
using System.Text;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    public class StaticState {

        // Logic object to define how contact point sets are calculated.
        private static IContactPointCalculator contactPointCalculator;
        static StaticState() {
            contactPointCalculator = new DefaultContactPointCalculator();
        }

        // Data storage types for the static world state graph.
        public class AreaNode {
            public int NodeId { get; }
            public int GeneralAreaId { get; }       // Identifies which larger-abstraction of 'area' this sub-area belongs to. This allows us to easily 'group' areas into a larger semantic block.
            public int CoverLevel { get; }          // Describes how much 'cover' a unit is considered to generally have if they are in this area from a combat perspective. Used as a 'weigh up' metric, not to determine if you will be 'in cover' since that is relative to other areas.
            public int ConcealmentLevel { get; }    // Describes how well a unit can conceal themselves in general while in this area.
            public bool Chokepoint { get; }         // BOOL - Is this area a chokepoint?
            public int TacticalValue { get; }       // Describes some general notion of how tactically valuable this area is.
            public int ExposureLevel { get; }       // Descrbies how 'out in the open' this area is.
            public bool DeadEnd { get; }            // BOOL - Is this area a deadend? Will be true if there is only one 'exit' from the general area.
            public bool Junction { get; }           // BOOL - Is this area a junction point? Will be true if multiple paths converge at this point.
            public bool OverwatchLocation { get; }  // BOOL - Is this area somewhere which observes over a large number of different general areas?
            public bool AttackObjective { get; }
            public bool DefendObjective { get; }
            public bool EnemyOriginPoint { get; }
            public bool Indoors { get; }

            // A set of sets, where each inner set is a group of adjacent contact point nodes. Each group represents one 'angle' (in the Counter-Strike sense) that this area is exposed to.
            internal HashSet<HashSet<int>> contactPointGroups;  // Internal so that the staticState constructor can populate upon construction!
            public IReadOnlyCollection<IReadOnlyCollection<int>> ContactPointGroups {
                get { return contactPointGroups; }
            }

            public AreaNode(int nodeId, int generalAreaId, int coverLevel, int concealmentLevel, bool chokepoint, int tacticalValue, int exposureLevel, bool deadEnd, bool junction, bool overwatchLocation, bool attackObjective, bool defendObjective, bool enemyOriginPoint, bool indoors) {
                NodeId = nodeId;
                GeneralAreaId = generalAreaId;
                CoverLevel = coverLevel;
                ConcealmentLevel = concealmentLevel;
                Chokepoint = chokepoint;
                TacticalValue = tacticalValue;
                ExposureLevel = exposureLevel;
                DeadEnd = deadEnd;
                Junction = junction;
                OverwatchLocation = overwatchLocation;
                AttackObjective = attackObjective;
                DefendObjective = defendObjective;
                EnemyOriginPoint = enemyOriginPoint;
                Indoors = indoors;
                contactPointGroups = new HashSet<HashSet<int>>();
            }
        }
        public class AreaEdge {
            public int FromNodeId { get; }          // 'This' node (A)
            public int ToNodeId { get; }            // 'That' node (B)
            public float Distance { get; }          // Represents the physical distance between the two areas. (Used to determine how long it will take to get there, etc.)
            public float MinimumHearableVolume { get; }     // How loud a sound has to be in B for a unit in A to hear it.
            public int CombatAdvantage { get; }     // How much 'advantage' a unit in A has while engaging a unit in B. Negative values represent disadvantage.
            public int RelativeCoverLevel { get; }  // BOOL - True if a unit in A can take cover relative to area B.
            public bool HasControlOver { get; }     // BOOL - True if control over area A gives you control over area B.

            // Traversability qualities.
            public bool WalkTraversability { get; }
            public bool CrawlTraversability { get; }
            public bool ClimbTraversability { get; }
            public bool VaultTraversability { get; }
            public bool IsTraversable {
                get { return WalkTraversability || CrawlTraversability || ClimbTraversability || VaultTraversability; }
            }

            // Visibility qualities.
            public bool FullVisibility { get; }     // This means that anyone in the 'to' node is always able to be seen by someone in the from node; they cannot hide.
            public bool PartialVisibility { get; }  // This means that someone in the 'to' node might be visible, or might be hidden depending on what they are doing. E.g. they could be crouching down behind something.
            public bool TravelVisibility { get; }   // This means that someone in the 'to' node is not able to leave the 'to' node without being seen by someone in the from node. It does not imply full visibility.

            internal AreaEdge oppositeEdge;  // Reference to the reverse edge, so we can more easily derive information.
            
            public bool IsControlledBy {
                get { return oppositeEdge.HasControlOver; }
            }
            public int EnemyCoverLevel {
                get { return oppositeEdge.RelativeCoverLevel; }
            }
            public bool IsFullyVisibleFrom {
                get { return oppositeEdge.FullVisibility; }
            }
            public bool IsPartiallyVisibleFrom {
                get { return oppositeEdge.PartialVisibility; }
            }
            public bool IsTravelVisibleFrom {
                get { return oppositeEdge.TravelVisibility; }
            }

            public AreaEdge(int fromNodeId, int toNodeId, float distance, float minimumHearableVolume, int combatAdvantage, int relativeCoverLevel, bool hasControlOver, bool walkTraversability, bool crawlTraversability, bool climbTraversability, bool vaultTraversability, bool fullVisibility, bool partialVisibility, bool travelVisibility) {
                FromNodeId = fromNodeId;
                ToNodeId = toNodeId;
                Distance = distance;
                MinimumHearableVolume = minimumHearableVolume;
                CombatAdvantage = combatAdvantage;
                RelativeCoverLevel = relativeCoverLevel;
                HasControlOver = hasControlOver;

                WalkTraversability = walkTraversability;
                CrawlTraversability = crawlTraversability;
                ClimbTraversability = climbTraversability;
                VaultTraversability = vaultTraversability;

                FullVisibility = fullVisibility;
                PartialVisibility = partialVisibility;
                TravelVisibility = travelVisibility;
            }
        }

        // Graph storage - Arrays since the graphs are likely to become dense, thus adjacency matrix is more effective.
        private AreaNode[] areaNodes;
        private AreaEdge[,] areaEdges;
        private int numNodes;

        // Set based references for rarely true data fields, that are more useful to be looked up as sets since most of the time they won't apply to a given area node.
        private HashSet<int> attackObjectiveNodes;
        private HashSet<int> defendObjectiveNodes;
        private HashSet<int> enemyOriginPointNodes;

        // TODO: 7 - Implement the ability to read a structured data file (JSON, XML) containing a WorldRepresentation configuration in order to build the StaticState/DynamicState. This will be essential for testing the system!
        public StaticState(AreaNode[] nodes, AreaEdge[,] edges) {
            areaNodes = nodes ?? throw new ArgumentNullException("nodes", "ERROR: Tried to create new StaticState but nodes array was null");
            areaEdges = edges ?? throw new ArgumentNullException("edges", "ERROR: Tried to create new StaticState but edges array was null");
            if (nodes.Length != edges.GetLength(0)) {
                throw new ArgumentException("ERROR: Tried to create a new StaticState but the node array and edge matrix were difference size");
            }
            else if (edges.GetLength(0) != edges.GetLength(1)) {
                throw new ArgumentException("ERROR: Tried to create a new StaticState but the edge matrix was not a sqaure matrix");
            }
            numNodes = areaNodes.Length;

            // Setup 'opposite edge' relationships
            foreach (AreaEdge e in areaEdges) {
                e.oppositeEdge = areaEdges[e.ToNodeId, e.FromNodeId];
            }

            // Setup the objective sets and the contact points (expensive, but StaticState should never be fully rebuilt).
            attackObjectiveNodes = new HashSet<int>();
            defendObjectiveNodes = new HashSet<int>();
            enemyOriginPointNodes = new HashSet<int>();
            for (int i = 0; i < nodes.Length; i++) {
                if (nodes[i].AttackObjective) attackObjectiveNodes.Add(i);
                if (nodes[i].DefendObjective) defendObjectiveNodes.Add(i);
                if (nodes[i].EnemyOriginPoint) enemyOriginPointNodes.Add(i);
                areaNodes[i].contactPointGroups = contactPointCalculator.CalculateContactPointGroups(this, i);
            }
        }

        // Public Interface - Read how large the node set is for this static state.
        public int NumberOfNodes {
            get {
                return numNodes;
            }
        }

        // Public Interface - Directly get all Vertex and Edge data
        public AreaNode GetNodeData(int nodeId) {
            // For performance, this value is not checked.
            return areaNodes[nodeId];
        }
        public AreaEdge GetEdge(int fromNode, int toNode) {
            // For performance, this value is not checked.
            return areaEdges[fromNode, toNode];
        }
        public IReadOnlyCollection<int> GetAttackObjectiveAreas() { return attackObjectiveNodes; }
        public IReadOnlyCollection<int> GetDefendObjectiveAreas() { return defendObjectiveNodes; }
        public IReadOnlyCollection<int> GetEnemyOriginPointAreas() { return enemyOriginPointNodes; }

        // Public Interface - Get a function which gains access to a subset of the vertex data in the graph
        public Func<int, int> GeneralAreaReader() {
            return nodeid => areaNodes[nodeid].GeneralAreaId;
        }
        public Func<int, int> CoverLevelReader() {
            return node => areaNodes[node].CoverLevel;
        }
        public Func<int, int> ConcealmentLevelReader() {
            return node => areaNodes[node].ConcealmentLevel;
        }
        public Func<int, int> TacticalValueReader() {
            return node => areaNodes[node].TacticalValue;
        }
        public Func<int, int> ExposureLevelReader() {
            return node => areaNodes[node].ExposureLevel;
        }
        public Func<int, bool> IsChokepointReader() {
            return node => areaNodes[node].Chokepoint;
        }
        public Func<int, bool> IsDeadEndReader() {
            return node => areaNodes[node].DeadEnd;
        }
        public Func<int, bool> IsJunctionReader() {
            return node => areaNodes[node].Junction;
        }
        public Func<int, bool> IsOverwatchLocationReader() {
            return node => areaNodes[node].OverwatchLocation;
        }
        public Func<int, bool> IsAttackObjectiveReader() {
            return node => areaNodes[node].AttackObjective;
        }
        public Func<int, bool> IsDefendObjectiveReader() {
            return node => areaNodes[node].DefendObjective;
        }
        public Func<int, bool> IsEnemyOriginPointReader() {
            return node => areaNodes[node].EnemyOriginPoint;
        }
        public Func<int, IReadOnlyCollection<IReadOnlyCollection<int>>> ContactPointGroupsReader() {
            return node => areaNodes[node].ContactPointGroups;
        }
        public Func<int, bool> IndoorsReader() {
            return node => areaNodes[node].Indoors;
        }

        // Public Interface - Get a function which gains access to a subset of the edge data in the graph
        public Func<int, int, bool> FullVisibilityReader() {
            return (from, to) => areaEdges[from, to].FullVisibility;
        }
        public Func<int, int, bool> PartialVisibilityReader() {
            return (from, to) => areaEdges[from, to].PartialVisibility;
        }
        public Func<int, int, bool> TravelVisibilityReader() {
            return (from, to) => areaEdges[from, to].TravelVisibility;
        }
        public Func<int, int, bool> IsTraversableReader() {
            return (from, to) => areaEdges[from, to].IsTraversable;
        }
        public Func<int, int, bool> WalkTraversabilityReader() {
            return (from, to) => areaEdges[from, to].WalkTraversability;
        }
        public Func<int, int, bool> CrawlTraversabilityReader() {
            return (from, to) => areaEdges[from, to].CrawlTraversability;
        }
        public Func<int, int, bool> ClimbTraversabilityReader() {
            return (from, to) => areaEdges[from, to].ClimbTraversability;
        }
        public Func<int, int, bool> VaultTraversabilityReader() {
            return (from, to) => areaEdges[from, to].VaultTraversability;
        }
        public Func<int, int, bool> HasControlOverReader() {
            return (from, to) => areaEdges[from, to].HasControlOver;
        }
        public Func<int, int, int> RelativeCoverLevelReader() {
            return (from, to) => areaEdges[from, to].RelativeCoverLevel
;
        }
        public Func<int, int, int> CombatAdvantageReader() {
            return (from, to) => areaEdges[from, to].CombatAdvantage;
        }
        public Func<int, int, float> DistanceReader() {
            return (from, to) => areaEdges[from, to].Distance;
        }
        public Func<int, int, float> MinimumHearableVolumeReader() {
            return (from, to) => areaEdges[from, to].MinimumHearableVolume;
        }

        // Public Interface - Query for sets based on an edge condition. This interface can later be refactored to use adjacency lists to avoid O(n) querying.
        private HashSet<int> NodeQueryBasedOnEdgeCondition(int n, Func<int, bool> edgeCondition) {
            HashSet<int> toRet = new HashSet<int>();
            for (int i=0; i < areaNodes.Length; i++) {
                if (edgeCondition(i) && i != n) {
                    toRet.Add(i);
                }
            }
            return toRet;
        }
        public HashSet<int> GetFullyVisibleNodes(int node) {
            return NodeQueryBasedOnEdgeCondition(node, n => areaEdges[node, n].FullVisibility);
        }
        public HashSet<int> GetTravelVisibleNodes(int node) {
            return NodeQueryBasedOnEdgeCondition(node, n => areaEdges[node, n].TravelVisibility);
        }
        public HashSet<int> GetTraversableNodes(int node) {
            return NodeQueryBasedOnEdgeCondition(node, n => areaEdges[node, n].IsTraversable);
        }
    }
}
