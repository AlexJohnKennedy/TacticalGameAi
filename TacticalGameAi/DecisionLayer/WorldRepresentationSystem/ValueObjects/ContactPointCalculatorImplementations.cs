using System;
using System.Collections.Generic;
using System.Text;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ContactPointCalculationSystem {
    public interface IContactPointCalculator {
        HashSet<HashSet<int>> CalculateContactPointGroups(StaticState s, int node);
    }

    public class DefaultContactPointCalculator : IContactPointCalculator {
        public HashSet<HashSet<int>> CalculateContactPointGroups(StaticState s, int node) {
            HashSet<HashSet<int>> toRet = new HashSet<HashSet<int>>();

            // According to this implementation, a 'Contact node' is any visible node which is adjacent to a non-visible node.
            // We define it this way because an enemy could 'appear' in that node, by moving from the adjacent invisible node, into that node.

            // Contact nodes which are adjacent to each other are classed as the same 'Contact Point', by being placed together into a group.

            // Start with all nodes visible to the source node in the candidate set.
            HashSet<int> visible = s.GetVisibleNodes(node);

            // Find all Contact Nodes by just brute force dumb-assing it coz it's late
            HashSet<int> contactNodes = new HashSet<int>();
            foreach(int n in visible) {
                foreach(int connectedNode in s.GetConnectedNodes(n)) {
                    if (!visible.Contains(connectedNode)) {
                        contactNodes.Add(connectedNode);
                    }
                }
            }

            // Now find all the disconnected sub-graphs, taking traversability as the edges for the graph.
            HashSet<int> visited = new HashSet<int>();
            foreach(int sourcePoint in contactNodes) {
                if (!visited.Contains(sourcePoint)) {
                    HashSet<int> group = new HashSet<int>();
                    group.Add(sourcePoint);
                    visited.Add(sourcePoint);

                    // Breadth First traversal from this point, where neighbors are connected and also are contact points.
                    Queue<int> frontier = new Queue<int>();
                    frontier.Enqueue(sourcePoint);
                    while (frontier.Count > 0) {
                        int n = frontier.Dequeue();
                        foreach (int i in s.GetConnectedNodes(n)) {
                            if (contactNodes.Contains(i) && !visited.Contains(i)) {
                                // 'i' is a node that belongs in the same group as 'n' which we haven't already visited!
                                frontier.Enqueue(i);
                                group.Add(i);
                                visited.Add(i);
                            }
                        }
                    }
                    toRet.Add(group);
                }
                else {
                    // We have already reached this node with one of the other groups. Skip!
                }
            }

            return toRet;
        }
    }
}
