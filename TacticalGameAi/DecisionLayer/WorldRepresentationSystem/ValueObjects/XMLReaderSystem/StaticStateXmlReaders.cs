using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects.XMLReaderSystem {
    public class UnderivedStaticStateXmlReader {

        private int numNodes;

        // Data containers for all underived node data.
        private int[] generalAreaId;
        private int[] coverLevel;
        private int[] concealmentLevel;
        private int[] tacticalValue;
        private int[] exposureLevel;
        private bool[] indoors;
        private float[] xPositions;
        private float[] yPositions;
        private HashSet<int> junctions;
        private HashSet<int> chokepoints;
        private HashSet<int> deadends;
        private HashSet<int> overwatchlocations;
        private HashSet<int> attackObjectives;
        private HashSet<int> defendObjectives;
        private HashSet<int> enemyOriginPoints;

        // Data containers for all underived edge data.
        private float[,] minimumHearableVolume;
        private int[,] combatAdvantage;
        private int[,] relativeCoverLevel;
        private bool[,] hasControlOver;
        private bool[,] walkTraversability;
        private bool[,] crawlTraversability;
        private bool[,] climbTraversability;
        private bool[,] vaultTraversability;
        private bool[,] fullvisibility;
        private bool[,] partialVisibility;
        private bool[,] travelvisibility;

        private void PopulateUnderivedData(XmlDocument doc) {
            // We expect the document to be validated.
            numNodes = Int32.Parse(doc.SelectSingleNode("NumberOfNodes").InnerText);
            generalAreaId = new int[numNodes];
            coverLevel = new int[numNodes];
            concealmentLevel = new int[numNodes];
            tacticalValue = new int[numNodes];
            exposureLevel = new int[numNodes];
            indoors = new bool[numNodes];
            xPositions = new float[numNodes];
            yPositions = new float[numNodes];
            junctions = new HashSet<int>();
            chokepoints = new HashSet<int>();
            deadends = new HashSet<int>();
            overwatchlocations = new HashSet<int>();
            attackObjectives = new HashSet<int>();
            defendObjectives = new HashSet<int>();
            enemyOriginPoints = new HashSet<int>();
            minimumHearableVolume = new float[numNodes, numNodes];
            combatAdvantage = new int[numNodes, numNodes];
            relativeCoverLevel = new int[numNodes, numNodes];
            hasControlOver = new bool[numNodes, numNodes];
            walkTraversability = new bool[numNodes, numNodes];
            crawlTraversability = new bool[numNodes, numNodes];
            climbTraversability = new bool[numNodes, numNodes];
            vaultTraversability = new bool[numNodes, numNodes];
            fullvisibility = new bool[numNodes, numNodes];
            partialVisibility = new bool[numNodes, numNodes];
            travelvisibility = new bool[numNodes, numNodes];

            // Get all the 'AreaNode' elements.
            XmlNodeList areaNodes = doc.GetElementsByTagName("AreaNode");
            foreach (XmlNode area in areaNodes) {
                int id = Int32.Parse(area.Attributes["id"].Value);

                XmlNode nodeData = area.SelectSingleNode("NodeData");
                generalAreaId[id] = Int32.Parse(nodeData.SelectSingleNode("GeneralAreaId").InnerText);
                coverLevel[id] = Int32.Parse(nodeData.SelectSingleNode("CoverLevel").InnerText);
                concealmentLevel[id] = Int32.Parse(nodeData.SelectSingleNode("ConcealmentLevel").InnerText);
                tacticalValue[id] = Int32.Parse(nodeData.SelectSingleNode("TacticalValue").InnerText);
                exposureLevel[id] = Int32.Parse(nodeData.SelectSingleNode("ExposureLevel").InnerText);
                indoors[id] = bool.Parse(nodeData.SelectSingleNode("Indoors").InnerText);
                xPositions[id] = float.Parse(nodeData.SelectSingleNode("AveragePosition").Attributes.GetNamedItem("xVal").InnerText);
                yPositions[id] = float.Parse(nodeData.SelectSingleNode("AveragePosition").Attributes.GetNamedItem("yVal").InnerText);

                XmlNode edgeData = area.SelectSingleNode("EdgeData");
                dumpFloat(edgeData, "MinimumHearableVolumeList", "MinimumHearableVolume", id, minimumHearableVolume, 999);
                dumpInt(edgeData, "CombatAdvantageList", "CombatAdvantage", id, combatAdvantage, 0);
                dumpInt(edgeData, "RelativeCoverLevelList", "RelativeCoverLevel", id, relativeCoverLevel, Int32.MaxValue);
                dumpBool(edgeData, "HasControlOverList", "HasControlOver", id, hasControlOver, false);
                dumpBool(edgeData, "WalkTraversabilityList", "WalkTraversability", id, walkTraversability, false);
                dumpBool(edgeData, "CrawlTraversabilityList", "CrawlTraversability", id, crawlTraversability, false);
                dumpBool(edgeData, "ClimbTraversabilityList", "ClimbTraversability", id, climbTraversability, false);
                dumpBool(edgeData, "VaultTraversabilityList", "VaultTraversability", id, vaultTraversability, false);
                dumpBool(edgeData, "FullVisibilityList", "FullVisibility", id, fullvisibility, false);
                dumpBool(edgeData, "PartialVisibilityList", "PartialVisibility", id, partialVisibility, false);
                dumpBool(edgeData, "TravelVisibilityList", "TravelVisibility", id, travelvisibility, false);
            }
            XmlNode querySets = doc.SelectSingleNode("QueryableNodeSets");
            populateSet(querySets, "Chokepoints", chokepoints);
            populateSet(querySets, "DeadEnds", deadends);
            populateSet(querySets, "Junctions", junctions);
            populateSet(querySets, "OverwatchLocations", overwatchlocations);
            populateSet(querySets, "AttackObjectives", attackObjectives);
            populateSet(querySets, "DefendObjectives", defendObjectives);
            populateSet(querySets, "EnemyOriginPoints", enemyOriginPoints);
        }
        private void populateSet(XmlNode querySets, string setName, HashSet<int> toPopulate) {
            foreach(XmlNode n in querySets.SelectSingleNode(setName).SelectNodes("NodeId")) {
                toPopulate.Add(Int32.Parse(n.InnerText));
            }
        }
        private void dumpFloat(XmlNode edgeData, string listname, string containername, int fromNode, float[,] matrix, float defaultVal) {
            for (int j = 0; j < numNodes; j++) {
                matrix[fromNode, j] = defaultVal;
            }
            foreach (XmlNode container in edgeData.SelectSingleNode(listname).SelectNodes(containername)) {
                int to = Int32.Parse(container.SelectSingleNode("toNodeId").InnerText);
                matrix[fromNode, to] = float.Parse(container.SelectSingleNode("Value").InnerText);
            }
        }
        private void dumpInt(XmlNode edgeData, string listname, string containername, int fromNode, int[,] matrix, int defaultVal) {
            for (int j = 0; j < numNodes; j++) {
                matrix[fromNode, j] = defaultVal;
            }
            foreach (XmlNode container in edgeData.SelectSingleNode(listname).SelectNodes(containername)) {
                int to = Int32.Parse(container.SelectSingleNode("toNodeId").InnerText);
                matrix[fromNode, to] = Int32.Parse(container.SelectSingleNode("Value").InnerText);
            }
        }
        private void dumpBool(XmlNode edgeData, string listname, string containername, int fromNode, bool[,] matrix, bool defaultVal) {
            for (int j=0; j < numNodes; j++) {
                matrix[fromNode, j] = defaultVal;
            }
            foreach (XmlNode container in edgeData.SelectSingleNode(listname).SelectNodes(containername)) {
                int to = Int32.Parse(container.SelectSingleNode("toNodeId").InnerText);
                matrix[fromNode, to] = bool.Parse(container.SelectSingleNode("Value").InnerText);
            }
        }

        public StaticState GetNewStaticStateObject() {
            StaticState.AreaEdge[,] edges = new StaticState.AreaEdge[numNodes, numNodes];
            StaticState.AreaNode[] nodes = new StaticState.AreaNode[numNodes];

            for (int i=0; i < numNodes; i++) {
                nodes[i] = new StaticState.AreaNode(i, generalAreaId[i], coverLevel[i], concealmentLevel[i], chokepoints.Contains(i),
                    tacticalValue[i], exposureLevel[i], deadends.Contains(i), junctions.Contains(i), overwatchlocations.Contains(i),
                    attackObjectives.Contains(i), defendObjectives.Contains(i), enemyOriginPoints.Contains(i), indoors[i]);

                for (int j=0; j < numNodes; j++) {
                    edges[i, j] = new StaticState.AreaEdge(i, j, Dist(xPositions[i], yPositions[i], xPositions[j], yPositions[j]),
                        minimumHearableVolume[i, j], combatAdvantage[i, j], relativeCoverLevel[i, j], hasControlOver[i, j], walkTraversability[i, j],
                        crawlTraversability[i, j], climbTraversability[i, j], vaultTraversability[i, j], fullvisibility[i, j], partialVisibility[i, j],
                        travelvisibility[i, j]);
                }
            }

            // Use the constructor which is not passed derived data, and calculates it upon construction
            return new StaticState(nodes, edges);
        }

        private float Dist(float x1, float y1, float x2, float y2) {
            float deltax = x2 - x1;
            float deltay = y2 - y1;
            return (float)Math.Sqrt(deltax * deltax + deltay * deltay);
        }

        public UnderivedStaticStateXmlReader(string dataFilePath, string schemaFilePath, string targetNamespace) {
            XmlDocument doc = ReadUnderivedStaticStateDataFromXML(dataFilePath, schemaFilePath, targetNamespace);
            PopulateUnderivedData(doc);
        }

        private XmlDocument ReadUnderivedStaticStateDataFromXML(string dataFilePath, string schemaFilePath, string schemaNamespace) {
            XmlDocument doc = new XmlDocument();
            doc.Load(dataFilePath);

            // Associate the schema and the target namespace of the schema.
            doc.Schemas.Add(schemaNamespace, schemaFilePath);

            // Validate the document against the schema. We must pass in a function which acts as the validation event callback.
            bool validationError = false;
            doc.Validate((o, e) => {
                validationError = true;
                Console.WriteLine("UnderivedStaticState XML Data Validation error: " + e.Message);
            });
            if (!validationError) Console.WriteLine("UnderivedStaticState XML Data document was loaded, and is valid against the passed schema");

            return doc;
        }
    }
}
