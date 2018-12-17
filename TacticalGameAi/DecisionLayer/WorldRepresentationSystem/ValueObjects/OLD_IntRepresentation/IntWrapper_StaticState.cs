using System;
using System.Collections.Generic;
using System.Text;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects.UnderlyingNodeData;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    public class IntWrapper_StaticState {
        private WorldData data;      // This class acts as an immutable mapper for this underlying data. No one else should EVER access this data!

        // Clients can gain access to limited versions of the graph by asking for a delegate which reads one specific data type for each node.
        private Func<int, int> IntStateGetter(InformationTable type) {
            return nodeid => data.Nodes[nodeid].stateData[(int)type];
        }
        private Func<int, bool> BoolStateGetter(InformationTable type) {
            return nodeid => data.Nodes[nodeid].stateData[(int)type] > 0;
        }
        public Func<int, int> GeneralAreaIds { get { return IntStateGetter(InformationTable.GeneralAreaId); } }
        public Func<int, int> CoverLevels { get { return IntStateGetter(InformationTable.CoverLevel); } }
        public Func<int, int> ConcealmentLevels { get { return IntStateGetter(InformationTable.ConcealmentLevel); } }
        public Func<int, int> TacticalValues { get { return IntStateGetter(InformationTable.TacticalValue); } }
        public Func<int, int> ExposureLevels { get { return IntStateGetter(InformationTable.ExposureLevel); } }
        public Func<int, bool> IsChokepoint { get { return BoolStateGetter(InformationTable.Chokepoint); } }
        public Func<int, bool> IsDeadEnd { get { return BoolStateGetter(InformationTable.DeadEnd); } }
        public Func<int, bool> IsJunction { get { return BoolStateGetter(InformationTable.Junction); } }
        public Func<int, bool> IsOverwatchLocation { get { return BoolStateGetter(InformationTable.OverwatchLocation); } }

        // Alternatively, clients can ask for all the data associated with a specific node through a mapper object
        public NodeInfo GetNodeInfo(int nodeId) { return new NodeInfo(nodeId, data.Nodes[nodeId].stateData); }
        public class NodeInfo {
            private int id;
            private int[] data;
            public NodeInfo(int id, int[] data) {
                this.id = id;
                this.data = data;
            }
            public int GeneralAreaId { get { return data[(int)InformationTable.GeneralAreaId]; } }
            public int CoverLevel { get { return data[(int)InformationTable.CoverLevel]; } }
            public int ConcealmentLevel { get { return data[(int)InformationTable.ConcealmentLevel]; } }
            public int TacticalValue { get { return data[(int)InformationTable.TacticalValue]; } }
            public int ExposureLevel { get { return data[(int)InformationTable.ExposureLevel]; } }
            public bool IsChokepoint { get { return data[(int)InformationTable.Chokepoint] > 0; } }
            public bool IsDeadEnd { get { return data[(int)InformationTable.DeadEnd] > 0; } }
            public bool IsJunction { get { return data[(int)InformationTable.Junction] > 0; } }
            public bool IsOverwatchLocation { get { return data[(int)InformationTable.OverwatchLocation] > 0; } }
        }

        // Define constants for all of the possible 'state' attributes associated with AreaNodes. I.e., what information is available about AreaNodes
        private enum InformationTable {
            GeneralAreaId       = 0,    // Identifies which larger-abstraction of 'area' this sub-area belongs to. This allows us to easily 'group' areas into a larger semantic block.
            CoverLevel          = 1,    // Describes how much 'cover' a unit is considered to generally have if they are in this area from a combat perspective.
            ConcealmentLevel    = 2,    // Describes how well a unit can conceal themselves in general while in this area.
            Chokepoint          = 3,    // BOOL - Is this area a chokepoint?
            TacticalValue       = 4,    // Describes some general notion of how tactically valuable this area is.
            ExposureLevel       = 5,    // Descrbies how 'out in the open' this area is.
            DeadEnd             = 6,    // BOOL - Is this area a deadend? Will be true if there is only one 'exit' from the general area.
            Junction            = 7,    // BOOL - Is this area a junction point? Will be true if multiple paths converge at this point.
            OverwatchLocation   = 8,    // BOOL - Is this area somewhere which observes over a large number of different general areas?
        }
        private enum RelationshipInfoTable {

        }
    }
}
