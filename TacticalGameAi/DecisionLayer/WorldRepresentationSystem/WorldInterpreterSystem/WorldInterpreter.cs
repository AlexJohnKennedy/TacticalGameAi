using System;
using System.Collections.Generic;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using System.Linq;
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
            public int id;
            public int prev;
            public float cost;

            public TraversalNode(int id, int prev, float cost) {
                this.id = id;
                this.prev = prev;
                this.cost = cost;
            }
        }

        public Interpretation Interpret(WorldRepresentation world) {

            // Lists of nodes to populate via traversal, as a scale of threat severity.
            // ************************************************************************

            ThreatLevel[] exploredThreat = new ThreatLevel[world.NumberOfNodes];
            for (int i=0; i<world.NumberOfNodes; i++) exploredThreat[i] = ThreatLevel.Unchecked;

            // *** SEVERE THREAT ***
            HashSet<int> knownThreats = new HashSet<int>();

            HashSet<int> potentialThreats = new HashSet<int>();
            Dictionary<int, HashSet<int>> potentialThreatSourceEdges = new Dictionary<int, HashSet<int>>();   // Edge data to specify which known threats or enemy origin points are the enemies which 'could reach here'
            Dictionary<int, int> closestThreatReachabilityPredecessor = new Dictionary<int, int>();           // Edge data to specify which traversable node the closest enemy will arive from, if they went the fastest way.

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
            var enemyPresenceNodes = d.NodeSetQueryObject.GetEnemyPresenceNodes();

            // Populate known threats
            foreach (int n in enemyPresenceNodes) {
                knownThreats.Add(n);
                threatSearchStartPoints.Add(n);
            }
            // Add enemy origin points to the search start points
            // TODO: 7 - Add some flag technique in the unit's AI which signals to the interpreter whether or not the unit is expecting any new enemies to potentially 'arrive'. If not, they won't search from enemy origin points.
            foreach (int n in s.GetEnemyOriginPointAreas()) {
                threatSearchStartPoints.Add(n);
            }

            // Complete a search from each 'starting point'. We will Djikstra search based on traversal distances from each point.
            bool[] selfDetermined = new bool[world.NumberOfNodes];
            int[] traversalPredecessor = new int[world.NumberOfNodes];
            List<int>[] threatLevelSource = new List<int>[world.NumberOfNodes];
            for (int i=0; i<world.NumberOfNodes; i++) {
                selfDetermined[i] = false;
                traversalPredecessor[i] = -1;
                threatLevelSource[i] = new List<int>(10);
            }
            SimultaneousSearch(world, threatSearchStartPoints, exploredThreat, selfDetermined, traversalPredecessor, threatLevelSource);

            foreach (int n in enemyPresenceNodes) {
                exploredThreat[n] = ThreatLevel.KnownThreat;
            }

            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // TODO: 10 - Calculate the post-traversal qualities based on Dynamic and Static state conditions, such as Active Thrests, engagements, suppressed threats, etc.
            // TODO: 10 - Calculate the post-traversal qualities based on Dynamic and Static state conditions, such as Active Thrests, engagements, suppressed threats, etc.
            // TODO: 10 - Calculate the post-traversal qualities based on Dynamic and Static state conditions, such as Active Thrests, engagements, suppressed threats, etc.
            // TODO: 10 - Calculate the post-traversal qualities based on Dynamic and Static state conditions, such as Active Thrests, engagements, suppressed threats, etc.
            // TODO: 10 - Calculate the post-traversal qualities based on Dynamic and Static state conditions, such as Active Thrests, engagements, suppressed threats, etc.
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            // Populate global Data structures.
            for (int i=0; i < world.NumberOfNodes; i++) {
                // Case by case; depending on the determined threat level for that node.
                if (exploredThreat[i] == ThreatLevel.ActiveThreat) {
                    activeThreats.Add(i);
                }
                else if (exploredThreat[i] == ThreatLevel.KnownThreat) {
                    knownThreats.Add(i);
                }
                else if (exploredThreat[i] == ThreatLevel.PotentialThreat) {
                    potentialThreats.Add(i);
                    closestThreatReachabilityPredecessor.Add(i, (traversalPredecessor[i] == -1) ? i : traversalPredecessor[i]); // If i'th node has a threat predecessor, that's the closest one. Else, itself.
                    potentialThreatSourceEdges.Add(i, new HashSet<int>(threatLevelSource[i]));      // Enumerates all the 'source nodes' which can cause this node to be a potential threat
                }
                else if (exploredThreat[i] == ThreatLevel.Neutral) {
                    neutrals.Add(i);
                }
                else if (exploredThreat[i] == ThreatLevel.Clear) {
                    clear.Add(i);
                    clearSourceEdges.Add(i, new HashSet<int>(threatLevelSource[i]));
                }
                else if (exploredThreat[i] == ThreatLevel.Secure) {
                    secure.Add(i);
                    secureSourceEdges.Add(i, new HashSet<int>(threatLevelSource[i]));
                }
            }



            return null;
        }

        private void SimultaneousSearch(WorldRepresentation world, IEnumerable<int> threatSearchStartPoints, ThreatLevel[] exploredThreat, bool[] selfDetermined, int[] traversalPredecessor, List<int>[] threatLevelSource) {
            SimplePriorityQueue<TraversalNode> frontier = new SimplePriorityQueue<TraversalNode>();     // Dijkstra Search
            // Queue<TraversalNode> frontier = new Queue<TraversalNode>();      // BFS

            // Always expand the first node(s): Set it up properly.
            // Add all the neighbours of all the start points into the frontier set simultaneously! :OO
            foreach (int startPoint in threatSearchStartPoints) {
                StartPointSetup(startPoint, world, frontier, exploredThreat, selfDetermined, traversalPredecessor, threatLevelSource);
            }

            while (frontier.Count > 0) {
                TraversalNode curr = frontier.Dequeue();

                // Expand this node if and only if we have the potential to 'promote' this current node's threat level based on propogation from the previous one!
                if (exploredThreat[curr.prev] > exploredThreat[curr.id]) {
                    Expand(world, curr, frontier, exploredThreat, selfDetermined, traversalPredecessor, threatLevelSource);
                }
                // If the threat levels are the same, then we DO NOT continue searching. However, the previous node should be added as a 'threatLevelSource' 
                // point, because the source of the prev node has also managed to reach this point with the same threat level. I.e. the curr node has already been
                // searched from here with this threat level, but had the 'prev' node gotten here before, it would have kept going with the same result!
                else if (exploredThreat[curr.prev] == exploredThreat[curr.id] && !threatLevelSource[curr.id].Contains(curr.prev)) {
                    threatLevelSource[curr.id].Add(curr.prev);
                }
            }
        }

        private void StartPointSetup(int startPoint, WorldRepresentation world, SimplePriorityQueue<TraversalNode> frontier, ThreatLevel[] exploredThreat, bool[] selfDetermined, int[] traversalPredecessor, List<int>[] threatLevelSource) {
            selfDetermined[startPoint] = true;
            threatLevelSource[startPoint].Add(startPoint);
            exploredThreat[startPoint] = ThreatLevel.PotentialThreat;   // Searches start at this priority level.
            traversalPredecessor[startPoint] = -1;
            foreach (int reachable in world.StaticState.GetTraversableNodes(startPoint)) {
                // TODO: 9 - add distance cost coefficients/modifiers depending on whether the traversability is walkable, vaultable, crawlable, climbable (latter ones are 'slower' so higher cost)
                float cost = GetTraversalCost(startPoint, reachable, world);
                frontier.Enqueue(new TraversalNode(reachable, startPoint, cost), cost);
            }
        }

        private void Expand(WorldRepresentation world, TraversalNode curr, SimplePriorityQueue<TraversalNode> frontier, ThreatLevel[] exploredThreat, bool[] selfDetermined, int[] traversalPredecessor, List<int>[] threatLevelSource) {
            // Check if there is no need to propogate further; this is true if this current node has special World States which means it determines it's own threat level, i.e. cleared by teammate, and we have already searched from here before!
            if (selfDetermined[curr.id] && exploredThreat[curr.id] <= exploredThreat[curr.prev]) {
                return;
            }

            // Check to see if the current areaNode demotes the current threat level based on State qualities, and determine if it is a 'specialDetermined' case.
            ThreatLevel? stateDeterminedThreatLevel = null;
            
            CheckState(world, curr, out stateDeterminedThreatLevel, out selfDetermined[curr.id]);

            // If some world-state condition determined a threat level demotion, apply it if and only if it is LOWER than the propogated threat level!
            if (stateDeterminedThreatLevel.HasValue && stateDeterminedThreatLevel.Value < exploredThreat[curr.prev]) {
                exploredThreat[curr.id] = stateDeterminedThreatLevel.Value;

                // If it is self determined, then the traversal predecessor needs to be reset to this point! This is because no predecessor is determining this node's state via propogation.
                if (selfDetermined[curr.id]) {
                    traversalPredecessor[curr.id] = -1;     // This is it's own source of threat level status, based on special conditions!
                    threatLevelSource[curr.id].Clear();
                    threatLevelSource[curr.id].Add(curr.id);
                }
                else {
                    traversalPredecessor[curr.id] = curr.prev;  // This node's threat level is contingent upon the predecessor in the traversal!
                    threatLevelSource[curr.id].Clear();
                    threatLevelSource[curr.id].Add(curr.prev);     // This node's threat level SOURCE is the previous node, because it has world-state determined threat level, but is NOT self determined.
                }
            }
            // Propogate the threat level instead.
            else {
                exploredThreat[curr.id] = (exploredThreat[curr.prev] > ThreatLevel.Neutral && curr.cost > NEUTRAL_DISTANCE_THRESHOLD) ? ThreatLevel.Neutral : exploredThreat[curr.prev];
                traversalPredecessor[curr.id] = curr.prev;
                // Threat level source becomes is completely propogated; the threat level source is the same as the predecessor.
                foreach (int n in threatLevelSource[curr.prev]) {
                    if (!threatLevelSource[curr.id].Contains(n)) {
                        threatLevelSource[curr.id].Add(n);
                    }
                }
            }

            // Finally, add all the traversable nodes to the frontier for future expansion in the search
            foreach (int reachable in world.StaticState.GetTraversableNodes(curr.id)) {
                float cost = curr.cost + GetTraversalCost(curr.id, reachable, world);
                frontier.Enqueue(new TraversalNode(reachable, curr.id, cost), cost);
            }
        }

        private void CheckState(WorldRepresentation world, TraversalNode curr, out ThreatLevel? stateDeterminedThreatLevel, out bool selfDetermined) {
            // Do a series of checks to determine what the threat level should be, based on the world representation.
            Func<int, bool> controlledByTeam = world.DynamicState.IsControlledByTeamReader();
            Func<int, bool> influencedByTeam = world.DynamicState.IsInfluencedByTeamReader();
            Func<int, bool> clear   = world.DynamicState.IsClearReader();
            Func<int, bool> visible = world.DynamicState.VisibleToSquadReader();
            
            // This node we are exploring is deemed:
            // 'Secure' if it is controlled, in which case it is also 'self determined'.
            if (controlledByTeam(curr.id)) {
                stateDeterminedThreatLevel = ThreatLevel.Secure;
                selfDetermined = true;
            }
            // 'Secure' if the node we came from is at least influenced. In this case, the node we are exploring is not selfDetermined;
            else if (influencedByTeam(curr.prev)) {
                stateDeterminedThreatLevel = ThreatLevel.Secure;
                selfDetermined = false;
            }
            // 'Clear' if it currently fully visible (clear effect), and this means it's self determined
            else if (clear(curr.id)) {
                stateDeterminedThreatLevel = ThreatLevel.Clear;
                selfDetermined = true;
            }
            // 'Clear if the node we came from is at least travel visible, however this means it is not self determined
            else if (visible(curr.prev)) {
                stateDeterminedThreatLevel = ThreatLevel.Clear;
                selfDetermined = false;
            }
            // If none of the previous conditions are met, then the status is propogated from the previous node!
            else {
                stateDeterminedThreatLevel = null;
                selfDetermined = false;
            }
        }

        private float GetTraversalCost(int from, int to, WorldRepresentation world) {
            // TODO: 9 - add distance cost coefficients/modifiers depending on whether the traversability is walkable, vaultable, crawlable, climbable (latter ones are 'slower' so higher cost)
            return world.StaticState.DistanceReader()(from, to);
        }
    }
}
