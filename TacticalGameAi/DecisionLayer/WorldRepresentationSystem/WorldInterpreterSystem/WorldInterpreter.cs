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
                bool[] selfDetermined = new bool[world.NumberOfNodes];
                int[] traversalPredecessor = new int[world.NumberOfNodes];
                int[] threatLevelSource = new int[world.NumberOfNodes];
                for (int i=0; i<world.NumberOfNodes; i++) {
                    selfDetermined[i] = false;
                    traversalPredecessor[i] = -1;
                    threatLevelSource[i] = -1;
                }
                SearchFrom(world, startArea, exploredThreat, selfDetermined, traversalPredecessor, threatLevelSource);

                // Populate global Data structures.
            }
            foreach (int n in d.NodeSetQueryObject.GetEnemyPresenceNodes()) {
                exploredThreat[n] = ThreatLevel.KnownThreat;
            }



            return null;
        }
        
        private void SearchFrom(WorldRepresentation world, int startPoint, ThreatLevel[] exploredThreat, bool[] selfDetermined, int[] traversalPredecessor, int[] threatLevelSource) {
            Queue<TraversalNode> frontier = new Queue<TraversalNode>();

            //Always expand the first node: Set it up properly
            selfDetermined[startPoint] = true;
            threatLevelSource[startPoint] = startPoint;
            exploredThreat[startPoint] = ThreatLevel.PotentialThreat;   // Searches start at this priority level.
            traversalPredecessor[startPoint] = -1;
            foreach (int reachable in world.StaticState.GetTraversableNodes(startPoint)) {
                // TODO: 9 - add distance cost coefficients/modifiers depending on whether the traversability is walkable, vaultable, crawlable, climbable (latter ones are 'slower' so higher cost)
                frontier.Enqueue(new TraversalNode(reachable, startPoint, GetTraversalCost(startPoint, reachable, world)));
            }

            while (frontier.Count > 0) {
                TraversalNode curr = frontier.Dequeue();

                // Expand this node if and only if we have the potential to 'promote' this current node's threat level based on propogation from the previous one!
                if (exploredThreat[curr.prev] > exploredThreat[curr.id]) {
                    Expand(world, curr, frontier, exploredThreat, selfDetermined, traversalPredecessor, threatLevelSource);
                }
            }
        }

        private void Expand(WorldRepresentation world, TraversalNode curr, Queue<TraversalNode> frontier, ThreatLevel[] exploredThreat, bool[] selfDetermined, int[] traversalPredecessor, int[] threatLevelSource) {
            // Check if there is no need to propogate further; this is true if this current node has special World States which means it determines it's own threat level, i.e. cleared by teammate, and we have already searched from here before!
            if (selfDetermined[curr.id] && exploredThreat[curr.id] <= exploredThreat[curr.prev]) {
                return;
            }

            // Check to see if the current areaNode demotes the current threat level based on State qualities, and determine if it is a 'specialDetermined' case.
            ThreatLevel? stateDeterminedThreatLevel = null;
            
            CheckState(world, curr, out stateDeterminedThreatLevel, out selfDetermined[curr.id]);

            // If some state condition determined a threat level demotion, apply it if and only if it is LOWER than the propogated threat level!
            if (stateDeterminedThreatLevel.HasValue && stateDeterminedThreatLevel.Value < exploredThreat[curr.prev]) {
                exploredThreat[curr.id] = stateDeterminedThreatLevel.Value;

                // If it is self determined, then the traversal predecessor needs to be reset to this point! This is because no predecessor is determining the state via propogation.
                if (selfDetermined[curr.id]) {
                    traversalPredecessor[curr.id] = -1;     // This is it's own source of threat level status, based on special conditions!
                    threatLevelSource[curr.id] = curr.id;
                }
                else {
                    traversalPredecessor[curr.id] = curr.prev;  // This node's threat level is contingent upon the predecessor in the traversal!
                    threatLevelSource[curr.id] = curr.prev;
                }
            }
            // Propogate the threat level instead.
            else {
                exploredThreat[curr.id] = (exploredThreat[curr.prev] > ThreatLevel.Neutral && curr.cost > NEUTRAL_DISTANCE_THRESHOLD) ? ThreatLevel.Neutral : exploredThreat[curr.prev];
                traversalPredecessor[curr.id] = curr.prev;
                threatLevelSource[curr.id] = threatLevelSource[curr.prev];
            }

            // Finally, add all the traversable nodes to the frontier for future expansion in the search
            foreach (int reachable in world.StaticState.GetTraversableNodes(curr.id)) {
                frontier.Enqueue(new TraversalNode(reachable, curr.id, curr.cost + GetTraversalCost(curr.id, reachable, world)));
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
            // 'Secure' if the node we came from is at least influenced, but not selfDetermined;
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
