using System;
using System.Collections.Generic;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using Priority_Queue;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldInterpreterSystem {

    public interface IWorldInterpreter {
        Interpretation Interpret(WorldRepresentation world);
    }

    public class GraphTraversalBasedWorldInterpreter : IWorldInterpreter {

        private const float NEUTRAL_DISTANCE_THRESHOLD = 100;   // This interpreter will assume any node over 100 meters away from an enemy source point is neutral if it would otherwise have potential enemies.

        private enum ThreatLevel {
            Unchecked,
            Secure,
            Clear,
            Neutral,
            PotentialThreat,
            KnownThreat,
            ActiveThreat
        }

        private class TraversalNode {
            public int areaNodeId;
            public ThreatLevel currThreatLevel;
            public float currCost;
            public int predArea;

            public TraversalNode(int areaNodeId, ThreatLevel currThreatLevel, float currCost, int predArea) {
                this.areaNodeId = areaNodeId;
                this.currThreatLevel = currThreatLevel;
                this.currCost = currCost;
                this.predArea = predArea;
            }
        }

        public Interpretation Interpret(WorldRepresentation world) {

            // Lists of nodes to populate via traversal, as a scale of threat severity.
            // ************************************************************************

            ThreatLevel[] exploredSet = new ThreatLevel[world.NumberOfNodes];
            for (int i=0; i<world.NumberOfNodes; i++) exploredSet[i] = ThreatLevel.Unchecked;

            // *** SEVERE THREAT ***
            HashSet<int> knownThreats = new HashSet<int>();

            HashSet<int> potentialThreats = new HashSet<int>();
            Dictionary<int, HashSet<int>> potentialThreatSourceEdges = new Dictionary<int, HashSet<int>>();   // Edge data to specify which known threats or enemy origin points are the enemies which 'could reach here'
            Dictionary<int, HashSet<int>> potentialThreatReachabilityPredecessors = new Dictionary<int, HashSet<int>>();   // Edge data to specify which areas a potential threat could come from directly. (traversable nodes)

            HashSet<int> neutrals = new HashSet<int>();

            HashSet<int> clear = new HashSet<int>();
            Dictionary<int, HashSet<int>> clearSourceEdges = new Dictionary<int, HashSet<int>>();  // Edge data to specify which 'actively cleared' nodes are responsible for clearing subsequent cleared nodes.

            HashSet<int> secure = new HashSet<int>();
            Dictionary<int, HashSet<int>> secureSourceEdges = new Dictionary<int, HashSet<int>>();  // Edge data to specify which 'actively controlled' nodes are responsible for securing subsequent secure nodes.
            // *** COMPLETELY SAFE ***


            // Additional types of information which is calculated after the traversal assignments.
            HashSet<int> activeThreats = new HashSet<int>();
            Dictionary<int, HashSet<int>> targetsOfActiveThreatEdges = new Dictionary<int, HashSet<int>>();   // Edge data to specify which nodes are being attacked by an active threat.

            HashSet<int> suppresedThreats = new HashSet<int>();
            Dictionary<int, HashSet<int>> suppressorNodes = new Dictionary<int, HashSet<int>>();  // Edge data to specify which nodes are currently suppressing each suppressed threat.

            HashSet<(int, int)> engagements = new HashSet<(int, int)>();  // Pairs of nodes which have friendlies engaged in combat with hostiles. ( friendlyNodes, enemyNode )

            // ************************************************************************

            StaticState s = world.StaticState;
            DynamicState d = world.DynamicState;
            var threatSearchStartPoints = new HashSet<int>();

            // Populate known threats
            foreach (int n in d.NodeSetQueryObject.GetEnemyPresenceNodes()) {
                knownThreats.Add(n);
                threatSearchStartPoints.Add(n);
            }
            // Add enemy origin points to the search start points
            // TODO: 7 - Add some flag technique in the unit's AI which signals to the interpreter whether or not the unit is expecting any new enemies to potentially 'arrive'. If not, they won't search from enemy origin points.
            foreach (int n in s.GetEnemyOriginPointAreas()) {
                threatSearchStartPoints.Add(n);
            }

            // Complete a search from each 'starting point'. We will Djikstra search based on traversal distances from each point.
            foreach (int startArea in threatSearchStartPoints) {
                SimplePriorityQueue<TraversalNode, float> searchQueue = new SimplePriorityQueue<TraversalNode, float>();
                TraversalNode startPoint = new TraversalNode(startArea, ThreatLevel.PotentialThreat, 0, -1);
                searchQueue.Enqueue(startPoint, 0);
                exploredSet[startArea] = ThreatLevel.PotentialThreat;
                while (searchQueue.Count > 0) {
                    TraversalNode curr = searchQueue.Dequeue();

                    // Check to see if the current traversal node is out of date
                    if (curr.currThreatLevel < exploredSet[curr.areaNodeId]) continue;

                    // Examine all traversable nodes.
                    foreach (int candidate in s.GetTraversableNodes(curr.areaNodeId)) {
                        // If the known threat level of the candidate node is higher or equal to the current node, we cannot go further.
                        // If the known threat level of the candidate node is lower than the current node, we have the potential to promote it's threat level higher.
                        // Note that completely unexplored nodes have an effective threat level of zero, thus will always be expanded.
                        if (exploredSet[candidate] < exploredSet[curr.areaNodeId]) {
                            DetermineCandidateThreatLevel(candidate, curr, world, out exploredSet[candidate]);
                            searchQueue.Enqueue(new TraversalNode(candidate, exploredSet[candidate], curr.currCost + s.GetEdge(curr.areaNodeId, candidate).Distance, curr.areaNodeId), curr.currCost + s.GetEdge(curr.areaNodeId, candidate).Distance);
                        }
                    }
                }
            }
            foreach (int n in d.NodeSetQueryObject.GetEnemyPresenceNodes()) {
                exploredSet[n] = ThreatLevel.KnownThreat;
            }



            return null;
        }

        private void DetermineCandidateThreatLevel(int candidate, TraversalNode curr, WorldRepresentation world, out ThreatLevel candidateThreatLevel) {
            // Do a series of checks to determine what the threat level should be, based on the world representation.
            Func<int, bool> controlledByTeam = world.DynamicState.IsControlledByTeamReader();
            Func<int, bool> influencedByTeam = world.DynamicState.IsInfluencedByTeamReader();
            Func<int, bool> clear   = world.DynamicState.IsClearReader();
            Func<int, bool> visible = world.DynamicState.VisibleToSquadReader();
            int nodeWeCameFrom = curr.areaNodeId;
            // This node we are exploring is deemed:
            // 'Secure' if it is controlled, or if the node through which the enemies reach it is at least influenced.
            if (influencedByTeam(nodeWeCameFrom) || controlledByTeam(candidate)) {
                candidateThreatLevel = ThreatLevel.Secure;
            }
            // 'Clear' if it currently fully visible (clear effect) or if the node through which the enemies reach it is at least travel-visible (i.e. enemies cannot reach without being spotted first).
            else if (clear(candidate) || visible(nodeWeCameFrom)) {
                candidateThreatLevel = ThreatLevel.Clear;
            }
            // 'Neutral' if it is over the threshold distance away, and not secure or clear
            else if (curr.currCost + world.StaticState.DistanceReader()(nodeWeCameFrom, candidate) > NEUTRAL_DISTANCE_THRESHOLD && curr.currThreatLevel >= ThreatLevel.Neutral) {
                candidateThreatLevel = ThreatLevel.Neutral;
            }
            // If we reach here, then there are no 'update conditions'. Thus, the candidateThreatLevel is the same as the node we arrived from; it propogates!
            else {
                candidateThreatLevel = curr.currThreatLevel;
            }
        }
    }
}
