using System;
using System.Collections.Generic;
using System.Text;

/**
 * This file defines the underlying data structures which will generically contain all node and edge information.
 * Data will all be contained using integer arrays which are only semantically interpretable through the use of a
 * 'Mapper' interface. StaticState, DynamicState, and Interpretation classes will provide the official mapper logic
 * to convert the underlying int values and arrays into meaningful world information. This design is intended to make
 * the data representation generic enough to:
 *      a) Be re-usable for Static, Dynamic, and Interpretation info through the use of different mappers.
 *      b) Be easily 'adaptable' so that specific implemtation logic elsewhere in the system can easily write anti-corruption
 *         adapter logic modules, which simply work by providing an additional mapper object to interpret the underlying int values
 *         differently.
 *         
 * NOTE: Other systems should NEVER access these types - only the "official" data mapper logic types should have access to this namespace, namely:
 *      a) StaticState class
 *      b) DynamicState class
 *      c) Interpretation class
 */
namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects.UnderlyingNodeData {
    readonly struct WorldData {
        // World data is generically represented as a graph; Nodes and Edges.
        NodeStateData[] Nodes { get; }
        NodeRelationshipData[,] Edges { get; }

        public WorldData(NodeStateData[] nodes, NodeRelationshipData[,] edges) {
            Nodes = nodes ?? throw new ArgumentNullException("nodes", "ERROR: Tried to create new WorldData struct but nodes array was null");
            Edges = edges ?? throw new ArgumentNullException("edges", "ERROR: Tried to create new WorldData struct but edges array was null");
            if (nodes.Length != edges.GetLength(0)) {
                throw new ArgumentException("ERROR: Tried to create a new WorldData struct but the node array and edge matrix were difference size");
            }
            else if (edges.GetLength(0) != edges.GetLength(1)) {
                throw new ArgumentException("ERROR: Tried to create a new WorldData struct but the edge matrix was not a sqaure matrix");
            }
        }
    }

    readonly struct NodeStateData {
        readonly int nodeId;
        readonly int nodeType;
        readonly int[] stateData;

        public NodeStateData(int nodeId, int nodeType, int[] stateData) {
            if (nodeId < 0) { throw new ArgumentOutOfRangeException("nodeId", "ERROR: Attempted to build a NodeStateData struct but the nodeId was less than zero"); }
            this.nodeId = nodeId;
            this.nodeType = nodeType;
            this.stateData = stateData ?? throw new ArgumentNullException("stateData", "ERROR: Attempted to build a NodeStateData struct but the statedata array was null");
        }
    }

    readonly struct NodeRelationshipData {
        readonly int fromNodeId;
        readonly int toNodeId;
        readonly int fromNodeType;
        readonly int toNodeType;
        readonly int[] edgeData;

        public NodeRelationshipData(int fromNodeId, int toNodeId, int fromNodeType, int toNodeType, int[] edgeData) {
            if (fromNodeId == toNodeId) throw new ArgumentException("ERROR: Attempted to build a NodeRelationshipData struct but FromNodeID and ToNodeID were the same. This is illegal because a Node should not have an edge to itself.");
            if (fromNodeId < 0) throw new ArgumentOutOfRangeException("fromNodeId", "ERROR: Tried to instantiate a NodeRelationshipData struct with a fromNodeId less than zero");
            if (toNodeId < 0) throw new ArgumentOutOfRangeException("toNodeId", "ERROR: Tried to instantiate a NodeRelationshipData struct with a toNodeId less than zero");
            this.fromNodeId = fromNodeId;
            this.toNodeId = toNodeId;
            this.fromNodeType = fromNodeType;
            this.toNodeType = toNodeType;
            this.edgeData = edgeData ?? throw new ArgumentNullException("edgeData","ERROR: Attempted to build a NodeRelationshipData struct but the edgedata array was null");
        }
    }
}
