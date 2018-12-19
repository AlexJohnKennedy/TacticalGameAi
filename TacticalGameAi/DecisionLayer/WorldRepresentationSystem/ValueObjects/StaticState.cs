using System;
using System.Collections.Generic;
using System.Text;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    public class StaticState {
        
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

            // Private constructor, so that only the surrounding class can access it
            public AreaNode(int nodeId, int generalAreaId, int concealmentLevel, bool chokepoint, int tacticalValue, int exposureLevel, bool deadEnd, bool junction, bool overwatchLocation) {
                NodeId = nodeId;
                GeneralAreaId = generalAreaId;
                ConcealmentLevel = concealmentLevel;
                Chokepoint = chokepoint;
                TacticalValue = tacticalValue;
                ExposureLevel = exposureLevel;
                DeadEnd = deadEnd;
                Junction = junction;
                OverwatchLocation = overwatchLocation;
            }
        }
        public class AreaEdge {
            public int FromNodeId { get; }          // 'This' node (A)
            public int ToNodeId { get; }            // 'That' node (B)
            public bool CanSee { get; }             // BOOL - Is a unit in this node able to see a unit in that node?
            public bool IsConnected { get; }        // BOOL - Is a unit in this node able to move to that node.
            public float Distance { get; }          // Represents the physical distance between the two areas. (Used to determine how long it will take to get there, etc.)
            public float MinimumHearableVolume { get; }     // How loud a sound has to be in B for a unit in A to hear it.
            public int CombatAdvantage { get; }     // How much 'advantage' a unit in A has while engaging a unit in B. Negative values represent disadvantage.
            public int RelativeCoverLevel { get; }  // BOOL - True if a unit in A can take cover relative to area B.
            public bool HasControlOver { get; }     // BOOL - True if control over area A gives you control over area B.

            private protected AreaEdge oppositeEdge;  // Reference to the reverse edge, so we can more easily derive information.
            public bool CanBeSeenFrom {
                get { return oppositeEdge.CanSee; }
            }
            public bool IsControlledBy {
                get { return oppositeEdge.HasControlOver; }
            }
            public int EnemyCoverLevel {
                get { return oppositeEdge.RelativeCoverLevel; }
            }

            public AreaEdge(int fromNodeId, int toNodeId, bool canSee, bool isConnected, float distance, float minimumHearableVolume, int combatAdvantage, int relativeCoverLevel, bool hasControlOver) {
                FromNodeId = fromNodeId;
                ToNodeId = toNodeId;
                CanSee = canSee;
                IsConnected = isConnected;
                Distance = distance;
                MinimumHearableVolume = minimumHearableVolume;
                CombatAdvantage = combatAdvantage;
                RelativeCoverLevel = relativeCoverLevel;
                HasControlOver = hasControlOver;
            }
        }

        // Graph storage - Arrays since the graphs are likely to become dense, thus adjacency matrix is more effective.
        private AreaNode[] areaNodes;
        private AreaEdge[,] areaEdges;

        // TODO: 6 - Add Obstacle graph creation and storage. Obstacle graph is applied on top of base area data to modify return results, essentially acting as an additional 'filter'.
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
        public Func<int, bool> IsOverwatchLocation() {
            return node => areaNodes[node].OverwatchLocation;
        }

        // Public Interface - Get a function which gains access to a subset of the edge data in the graph
        public Func<int, int, bool> CanSeeReader() {
            return (from, to) => areaEdges[from, to].CanSee;
        }
        public Func<int, int, bool> IsConnectedReader() {
            return (from, to) => areaEdges[from, to].IsConnected;
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
    }
}
