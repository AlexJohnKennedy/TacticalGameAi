﻿using NUnit.Framework;
using Moq;
using System;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using System.Collections.Generic;
using System.Xml;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    public static class HardCodedStateCreator {
        private static int numNodes = 10;   // Let's test this with 10 areas
        private static int defaultTime = 0;

        public static StaticState CreateTestStaticStateFromHardCode() {
            return new StaticState(PrepareNodeDataFromHardCode(), PrepareEdgeDataFromHardCode());
        }

        public static void WriteTestStaticStateToXML(string outputPath) {
            XmlWriter writer = XmlWriter.Create(outputPath);
            writer.WriteStartDocument();

            // Root element
            writer.WriteStartElement("StaticState", "http://tempuri.org/UnderivedStaticStateData.xsd");
            writer.WriteAttributeString("unit-type", "default");
            writer.WriteAttributeString("variation-id", "default");
            
            StaticState.AreaNode[] nodes = PrepareNodeDataFromHardCode();
            StaticState.AreaEdge[,] edges = PrepareEdgeDataFromHardCode();

            writer.WriteStartElement("NumberOfNodes");
            writer.WriteString(nodes.Length.ToString());
            writer.WriteEndElement();

            for (int i=0; i<nodes.Length; i++) {
                writer.WriteStartElement("AreaNode");
                writer.WriteAttributeString("id", i.ToString());

                    writer.WriteStartElement("NodeData");
                        writer.WriteStartElement("GeneralAreaId");
                            writer.WriteString(nodes[i].GeneralAreaId.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("CoverLevel");
                            writer.WriteString(nodes[i].CoverLevel.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("ConcealmentLevel");
                            writer.WriteString(nodes[i].ConcealmentLevel.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("TacticalValue");
                            writer.WriteString(nodes[i].TacticalValue.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("ExposureLevel");
                            writer.WriteString(nodes[i].ExposureLevel.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("Indoors");
                            writer.WriteString(nodes[i].Indoors.ToString().ToLowerInvariant());
                        writer.WriteEndElement();
                        writer.WriteStartElement("AveragePosition");
                            writer.WriteStartElement("xVal");
                                writer.WriteString("0");    // TODO
                            writer.WriteEndElement();
                            writer.WriteStartElement("yVal");
                                writer.WriteString("0");    // TODO
                            writer.WriteEndElement();
                        writer.WriteEndElement();
                    writer.WriteEndElement();

                writer.WriteStartElement("EdgeData");
                WriteEdgeData(writer, "MinimumHearableVolumeList", "MinimumHearableVolume", j => false, j => edges[i, j].MinimumHearableVolume.ToString(), nodes.Length);
                WriteEdgeData(writer, "CombatAdvantageList", "CombatAdvantage", j => edges[i, j].CombatAdvantage == 0, j => edges[i, j].CombatAdvantage.ToString(), nodes.Length);
                WriteEdgeData(writer, "RelativeCoverLevelList", "RelativeCoverLevel", j => edges[i, j].RelativeCoverLevel == Int32.MaxValue, j => edges[i, j].RelativeCoverLevel.ToString(), nodes.Length);
                WriteEdgeData(writer, "HasControlOverList", "HasControlOver", j => edges[i, j].HasControlOver == false, j => edges[i, j].HasControlOver.ToString(), nodes.Length);
                WriteEdgeData(writer, "WalkTraversabilityList", "WalkTraversability", j => edges[i, j].WalkTraversability == false, j => edges[i, j].WalkTraversability.ToString(), nodes.Length);
                WriteEdgeData(writer, "CrawlTraversabilityList", "CrawlTraversability", j => edges[i, j].CrawlTraversability == false, j => edges[i, j].CrawlTraversability.ToString(), nodes.Length);
                WriteEdgeData(writer, "VaultTraversabilityList", "VaultTraversability", j => edges[i, j].VaultTraversability == false, j => edges[i, j].VaultTraversability.ToString(), nodes.Length);
                WriteEdgeData(writer, "ClimbTraversabilityList", "ClimbTraversability", j => edges[i, j].ClimbTraversability == false, j => edges[i, j].ClimbTraversability.ToString(), nodes.Length);
                WriteEdgeData(writer, "FullVisibilityList", "FullVisibility", j => edges[i, j].FullVisibility == false, j => edges[i, j].FullVisibility.ToString(), nodes.Length);
                WriteEdgeData(writer, "PartialVisibilityList", "PartialVisibility", j => edges[i, j].PartialVisibility == false, j => edges[i, j].PartialVisibility.ToString(), nodes.Length);
                WriteEdgeData(writer, "TravelVisibilityList", "TravelVisibility", j => edges[i, j].TravelVisibility == false, j => edges[i, j].TravelVisibility.ToString(), nodes.Length);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            writer.WriteStartElement("QueryableNodeSets");
            WriteQueryableNodeSet(writer, "Chokepoints", i => nodes[i].Chokepoint, nodes.Length);
            WriteQueryableNodeSet(writer, "DeadEnds", i => nodes[i].DeadEnd, nodes.Length);
            WriteQueryableNodeSet(writer, "Junctions", i => nodes[i].Junction, nodes.Length);
            WriteQueryableNodeSet(writer, "OverwatchLocations", i => nodes[i].OverwatchLocation, nodes.Length);
            WriteQueryableNodeSet(writer, "AttackObjectives", i => nodes[i].AttackObjective, nodes.Length);
            WriteQueryableNodeSet(writer, "DefendObjectives", i => nodes[i].DefendObjective, nodes.Length);
            WriteQueryableNodeSet(writer, "EnemyOriginPoints", i => nodes[i].EnemyOriginPoint, nodes.Length);
            writer.WriteEndElement();

            writer.WriteEndDocument();
            writer.Close();
        }

        private static void WriteQueryableNodeSet(XmlWriter writer, string listname, Func<int, bool> truthCondition, int n) {
            writer.WriteStartElement(listname);
            for (int i=0; i < n; i++) {
                if (truthCondition(i)) {
                    writer.WriteStartElement("NodeId");
                    writer.WriteString(i.ToString());
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        private static void WriteEdgeData(XmlWriter writer, string listname, string valueContainerName, Func<int, bool> skipCondition, Func<int, string> valueFunc, int n) {
            writer.WriteStartElement(listname);
            for (int j = 0; j < n; j++) {
                if (skipCondition(j)) continue;
                writer.WriteStartElement(valueContainerName);
                writer.WriteStartElement("toNodeId");
                writer.WriteString(j.ToString());
                writer.WriteEndElement();
                writer.WriteStartElement("Value");
                writer.WriteString(valueFunc(j).ToLowerInvariant());
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        private static StaticState.AreaNode[] PrepareNodeDataFromHardCode() {
            StaticState.AreaNode[] toRet = new StaticState.AreaNode[numNodes];
            toRet[0] = new StaticState.AreaNode(0, 0, 8, 3, false, 5, 5, false, false, false, false, false, false, true);
            toRet[1] = new StaticState.AreaNode(1, 0, 0, 0, true, 1, 8, false, false, false, false, false, false, true);
            toRet[2] = new StaticState.AreaNode(2, 1, 6, 1, false, 3, 3, false, false, true, false, false, false, true);
            toRet[3] = new StaticState.AreaNode(3, 0, 0, 0, false, 1, 5, true, false, false, false, false, false, true);
            toRet[4] = new StaticState.AreaNode(4, 0, 0, 5, false, 2, 1, true, false, false, false, true, false, true);
            toRet[5] = new StaticState.AreaNode(5, 1, 3, 1, false, 6, 2, false, false, true, false, false, false, true);
            toRet[6] = new StaticState.AreaNode(6, 1, 0, 0, true, 3, 5, false, true, false, false, false, true, true);
            toRet[7] = new StaticState.AreaNode(7, 2, 0, 0, false, 1, 8, true, false, false, false, false, true, true);
            toRet[8] = new StaticState.AreaNode(8, 2, 0, 0, false, 1, 8, true, false, false, true, false, false, true);
            toRet[9] = new StaticState.AreaNode(9, 2, 0, 0, false, 1, 5, true, false, false, false, false, false, true);

            return toRet;
        }
        private static StaticState.AreaEdge[,] PrepareEdgeDataFromHardCode() {
            StaticState.AreaEdge[,] toRet = new StaticState.AreaEdge[numNodes, numNodes];
            for (int i = 0; i < numNodes; i++) {
                for (int j = 0; j < numNodes; j++) {
                    toRet[i, j] = new StaticState.AreaEdge(i, j, 0, 0, 0, Int32.MaxValue, false, false, false, false, false, false, false, false);  // Defaults
                }
            }
            // Replace all the ones with actual data! Note 'distance' and stuff is not going to be tested since it is too tedious. Auto generation of SS should fill this in though.
            // int fromNodeId, int toNodeId, float distance, float minimumHearableVolume, int combatAdvantage, int relativeCoverLevel, bool hasControlOver, bool walkTraversability, bool crawlTraversability, bool climbTraversability, bool vaultTraversability, bool fullVisibility, bool partialVisibility, bool travelVisibility
            toRet[0, 1] = new StaticState.AreaEdge(0, 1, 5, 5, 3, 5, false, true, false, false, false, true, false, false);
            toRet[0, 3] = new StaticState.AreaEdge(0, 3, 5, 5, 6, 5, true, true, false, false, false, true, false, false);
            toRet[1, 0] = new StaticState.AreaEdge(1, 0, 5, 5, -3, 0, false, true, false, false, false, true, false, false);
            toRet[3, 0] = new StaticState.AreaEdge(3, 0, 5, 5, -6, 0, false, true, false, false, false, true, false, false);
            toRet[3, 4] = new StaticState.AreaEdge(3, 4, 5, 5, 0, 1, false, true, false, false, false, true, false, false);
            toRet[4, 3] = new StaticState.AreaEdge(4, 3, 5, 5, 0, 1, false, true, false, false, false, true, false, false);
            toRet[2, 1] = new StaticState.AreaEdge(2, 1, 5, 5, 4, 4, false, true, false, false, false, true, false, false);
            toRet[1, 2] = new StaticState.AreaEdge(1, 2, 5, 5, -4, 0, false, true, false, false, false, true, false, false);
            toRet[5, 1] = new StaticState.AreaEdge(5, 1, 7, 4, 5, 8, false, false, false, false, false, true, false, false);
            toRet[1, 5] = new StaticState.AreaEdge(1, 5, 7, 3, -5, 2, false, false, false, false, false, true, false, false);
            toRet[2, 5] = new StaticState.AreaEdge(2, 5, 5, 5, 0, 0, false, true, false, false, false, true, false, false);
            toRet[5, 2] = new StaticState.AreaEdge(5, 2, 5, 5, 0, 0, false, true, false, false, false, true, false, false);
            toRet[5, 6] = new StaticState.AreaEdge(5, 6, 5, 5, 2, 1, false, true, false, false, false, true, false, false);
            toRet[6, 5] = new StaticState.AreaEdge(6, 5, 5, 5, -2, 0, false, true, false, false, false, true, false, false);
            toRet[6, 7] = new StaticState.AreaEdge(6, 7, 5, 5, 6, 3, true, true, false, false, false, true, false, false);
            toRet[7, 6] = new StaticState.AreaEdge(7, 6, 5, 5, -6, 0, false, true, false, false, false, true, false, false);
            toRet[6, 8] = new StaticState.AreaEdge(6, 8, 5, 5, 0, 0, false, true, false, false, false, true, false, false);
            toRet[8, 6] = new StaticState.AreaEdge(8, 6, 5, 5, 0, 0, false, true, false, false, false, true, false, false);
            toRet[7, 8] = new StaticState.AreaEdge(7, 8, 5, 5, 1, 0, false, true, false, false, false, true, false, false);
            toRet[8, 7] = new StaticState.AreaEdge(8, 7, 5, 5, -1, 0, false, true, false, false, false, true, false, false);
            toRet[8, 9] = new StaticState.AreaEdge(8, 9, 5, 5, 0, 0, false, false, false, false, false, true, false, false);
            toRet[9, 8] = new StaticState.AreaEdge(9, 8, 5, 5, 0, 0, false, false, false, false, false, true, false, false);

            return toRet;
        }

        public static void CheckTestStaticState(StaticState s) {
            // Assert node states
            Func<int, bool> ow = s.IsOverwatchLocationReader();
            Func<int, bool> attack = s.IsAttackObjectiveReader();
            Func<int, bool> defend = s.IsDefendObjectiveReader();
            Func<int, bool> origin = s.IsEnemyOriginPointReader();
            Func<int, bool> junction = s.IsJunctionReader();
            Func<int, bool> deadend = s.IsDeadEndReader();
            Func<int, bool> chokepoint = s.IsChokepointReader();
            for (int i = 0; i < numNodes; i++) {
                // Overwatch locations
                if (i == 2 || i == 5) {
                    Assert.IsTrue(ow(i));
                    Assert.IsTrue(s.GetNodeData(i).OverwatchLocation);
                }
                else {
                    Assert.IsFalse(ow(i));
                    Assert.IsFalse(s.GetNodeData(i).OverwatchLocation);
                }

                // Attack objectives
                if (i == 8) {
                    Assert.IsTrue(attack(i));
                    Assert.IsTrue(s.GetNodeData(i).AttackObjective);
                }
                else {
                    Assert.IsFalse(attack(i));
                    Assert.IsFalse(s.GetNodeData(i).AttackObjective);
                }

                // Defend objectives
                if (i == 4) {
                    Assert.IsTrue(defend(i));
                    Assert.IsTrue(s.GetNodeData(i).DefendObjective);
                }
                else {
                    Assert.IsFalse(defend(i));
                    Assert.IsFalse(s.GetNodeData(i).DefendObjective);
                }

                // Enemy Origin points
                if (i == 6 || i == 7) {
                    Assert.IsTrue(origin(i));
                    Assert.IsTrue(s.GetNodeData(i).EnemyOriginPoint);
                }
                else {
                    Assert.IsFalse(origin(i));
                    Assert.IsFalse(s.GetNodeData(i).EnemyOriginPoint);
                }

                // Junction
                if (i == 6) {
                    Assert.IsTrue(junction(i));
                    Assert.IsTrue(s.GetNodeData(i).Junction);
                }
                else {
                    Assert.IsFalse(junction(i));
                    Assert.IsFalse(s.GetNodeData(i).Junction);
                }
            }

            // Assert visibility relationships
            Func<int, int, bool> vis = s.FullVisibilityReader();
            Func<int, int, bool> con = s.HasControlOverReader();
            Func<int, int, bool> trav = s.IsTraversableReader();
            for (int i = 0; i < numNodes; i++) {
                for (int j = 0; j < numNodes; j++) {
                    // Should be visible
                    if (   (i == 0 && j == 1 || i == 1 && j == 0)
                        || (i == 0 && j == 3 || i == 3 && j == 0)
                        || (i == 3 && j == 4 || i == 4 && j == 3)
                        || (i == 1 && j == 2 || i == 2 && j == 1)
                        || (i == 1 && j == 5 || i == 5 && j == 1)
                        || (i == 2 && j == 5 || i == 5 && j == 2)
                        || (i == 5 && j == 6 || i == 6 && j == 5)
                        || (i == 6 && j == 7 || i == 7 && j == 6)
                        || (i == 6 && j == 8 || i == 8 && j == 6)
                        || (i == 7 && j == 8 || i == 8 && j == 7)
                        || (i == 8 && j == 9 || i == 9 && j == 8)
                       ) {
                        Assert.IsTrue(vis(i, j));
                        Assert.IsTrue(s.GetEdge(i, j).FullVisibility);
                        Assert.IsTrue(s.GetEdge(i, j).IsFullyVisibleFrom);
                    }
                    // Should NOT be visible
                    else {
                        Assert.IsFalse(vis(i, j));
                        Assert.IsFalse(s.GetEdge(i, j).FullVisibility);
                        Assert.IsFalse(s.GetEdge(i, j).IsFullyVisibleFrom);
                    }

                    // Should 'control'
                    if (i == 0 && j == 3 || i == 6 && j == 7) {
                        Assert.IsTrue(con(i, j));
                        Assert.IsTrue(s.GetEdge(i, j).HasControlOver);
                        Assert.IsTrue(s.GetEdge(j, i).IsControlledBy);
                    }
                    // Should NOT control
                    else {
                        Assert.IsFalse(con(i, j));
                        Assert.IsFalse(s.GetEdge(i, j).HasControlOver);
                    }

                    // Should be traverseable
                    if (   (i == 0 && j == 1 || i == 1 && j == 0)
                        || (i == 0 && j == 3 || i == 3 && j == 0)
                        || (i == 3 && j == 4 || i == 4 && j == 3)
                        || (i == 1 && j == 2 || i == 2 && j == 1)
                        || (i == 2 && j == 5 || i == 5 && j == 2)
                        || (i == 5 && j == 6 || i == 6 && j == 5)
                        || (i == 6 && j == 7 || i == 7 && j == 6)
                        || (i == 6 && j == 8 || i == 8 && j == 6)
                        || (i == 7 && j == 8 || i == 8 && j == 7)
                       ) {
                        Assert.IsTrue(trav(i, j));
                        Assert.IsTrue(s.GetEdge(i, j).IsTraversable);
                    }
                    // Should NOT be traversable
                    else {
                        Assert.IsFalse(trav(i, j));
                        Assert.IsFalse(s.GetEdge(i, j).IsTraversable);
                    }
                }
            }
        }

        public static DynamicState CreateTestDynamicState() {
            var facts = new Dictionary<FactType, Fact>[numNodes];

            // Set up the effects that each fact is causing.
            // Node 0 causes 'clear' / 'visibleToSquad' on node 1 and 3, and 'control' on 3
            List<Effect> e = new List<Effect> {
                new Effect(EffectType.Clear, 0 ,1, 0),
                new Effect(EffectType.Clear, 0, 3, 0),
                new Effect(EffectType.VisibleToSquad, 0 ,1, 0),
                new Effect(EffectType.VisibleToSquad, 0, 3, 0),
                new Effect(EffectType.Controlled, 0, 3, 0)
            };
            facts[0] = new Dictionary<FactType, Fact> { { FactType.SquadMemberPresence, new Fact(FactType.SquadMemberPresence, 2, defaultTime, e) } };

            // Node 1 has no facts
            facts[1] = new Dictionary<FactType, Fact> { };

            // Node 2 causes 'visible to enemies' on 1 and 5
            e = new List<Effect> {
                new Effect(EffectType.VisibleToEnemies, 0, 1, 2),
                new Effect(EffectType.VisibleToEnemies, 0, 5, 2)
            };
            facts[2] = new Dictionary<FactType, Fact> { { FactType.EnemyPresence, new Fact(FactType.EnemyPresence, 3, defaultTime, e) } };

            // Node 3 has no facts
            facts[3] = new Dictionary<FactType, Fact> { };

            // Node 4 causes 'clear' on node 3, and visibleToFriendlies on node 3
            e = new List<Effect> {
                new Effect(EffectType.Clear, 0, 3, 4),
                new Effect(EffectType.VisibleToSquad, 0, 3, 4),
            };
            facts[4] = new Dictionary<FactType, Fact> { { FactType.SquadMemberPresence, new Fact(FactType.SquadMemberPresence, 1, defaultTime, e) } };

            // Node 5 causes 'potential enemies' on node 1 and 6
            e = new List<Effect> {
                new Effect(EffectType.PotentialEnemies, 0, 1, 5),
                new Effect(EffectType.PotentialEnemies, 0, 6, 5)
            };
            facts[5] = new Dictionary<FactType, Fact> { { FactType.TakingFireFromUnknownSource, new Fact(FactType.TakingFireFromUnknownSource, 5, defaultTime, e) } };

            // Node 6 has LastKnownFriendlyPosition with value 2, and causes no Effects
            e = new List<Effect> { };
            facts[6] = new Dictionary<FactType, Fact> { { FactType.LastKnownFriendlyPosition, new Fact(FactType.LastKnownFriendlyPosition, 2, defaultTime, e) } };

            // Node 7 has no facts.
            facts[7] = new Dictionary<FactType, Fact> { };

            // Node 8 has LastKnownEnemyPosition with value 1, and causes potential enemies on all nodes traversable from node 8 (node 6 and 7)
            e = new List<Effect> {
                new Effect(EffectType.PotentialEnemies, 0, 6, 8),
                new Effect(EffectType.PotentialEnemies, 0, 7, 8)
            };
            facts[8] = new Dictionary<FactType, Fact> { { FactType.LastKnownEnemyPosition, new Fact(FactType.LastKnownEnemyPosition, 1, defaultTime, e) } };

            // Node 9 is taking fire from node 8
            e = new List<Effect> {
                new Effect(EffectType.SourceOfEnemyFire, 1, 8, 9)
            };
            facts[9] = new Dictionary<FactType, Fact> { { FactType.TakingFire, new Fact(FactType.TakingFire, 1, defaultTime, e) } };

            return new DynamicState(facts);
        }
        
        public static void CheckTestDynamicStateNodeSets(DynamicState toTest) {
            DynamicState.NodeSetQuery set = toTest.NodeSetQueryObject;

            CollectionAssert.AreEquivalent(new int[] { 0, 4 }, set.GetSquadMemberPresenceNodes());
            CollectionAssert.AreEquivalent(new int[] { 0, 4 }, set.GetFriendlyAreaNodes());
            CollectionAssert.AreEquivalent(new int[] { 2 }, set.GetEnemyAreaNodes());
            CollectionAssert.AreEquivalent(new int[] { }, set.GetContestedAreaNodes());
            CollectionAssert.AreEquivalent(new int[] { 5 }, set.GetNodesTakingFireFromUnknownSource());
            CollectionAssert.AreEquivalent(new int[] { 0, 1, 3, 4 }, set.GetClearNodes());
            CollectionAssert.AreEquivalent(new int[] { 2, 1, 5 }, set.GetVisibleToEnemiesNodes());
            CollectionAssert.AreEquivalent(new int[] { 5, 6, 7, 8 }, set.GetPotentialEnemiesNodes());
            CollectionAssert.AreEquivalent(new int[] { 3, 0, 4 }, set.GetControlledByTeamNodes());
            CollectionAssert.AreEquivalent(new int[] { 2 }, set.GetControlledByEnemiesNodes());
            CollectionAssert.AreEquivalent(new int[] { 1, 3, 5, 6, 7, 8, 9 }, set.GetNoKnownPresenceNodes());
            CollectionAssert.AreEquivalent(new int[] { 2 }, set.GetEnemyPresenceNodes());
            CollectionAssert.AreEquivalent(new int[] { 9 }, set.GetNodesTakingFireFromKnownSource());
        }

        public static void CheckTestDynamicState(DynamicState toTest) {
            // Test node data reads
            Assert.IsTrue(toTest.GetNodeData(0).SquadMemberPresence == 2);
            Assert.IsTrue(toTest.GetNodeData(0).EnemyPresence == 0);
            Assert.IsTrue(toTest.GetNodeData(0).IsFriendlyArea);
            Assert.IsTrue(toTest.GetNodeData(0).IsClear);
            Assert.IsTrue(toTest.GetNodeData(0).IsControlledByTeam);
            Assert.IsTrue(toTest.GetNodeData(0).LastKnownEnemyPosition == 0);
            Assert.IsTrue(!toTest.GetNodeData(1).IsControlledByTeam);
            Assert.IsTrue(toTest.GetNodeData(1).IsClear);
            Assert.IsTrue(toTest.GetNodeData(1).VisibleToEnemies);
            Assert.IsTrue(!toTest.GetNodeData(1).PotentialEnemies);
            Assert.IsTrue(toTest.GetNodeData(2).EnemyPresence == 3);
            Assert.IsTrue(toTest.GetNodeData(2).IsEnemyArea);
            Assert.IsTrue(!toTest.GetNodeData(2).IsContestedArea);
            Assert.IsTrue(!toTest.GetNodeData(2).IsClear);
            Assert.IsTrue(toTest.GetNodeData(2).IsControlledByEnemies);
            Assert.IsTrue(toTest.GetNodeData(2).VisibleToEnemies);
            Assert.IsTrue(toTest.GetNodeData(3).IsClear);
            Assert.IsTrue(toTest.GetNodeData(3).IsControlledByTeam);
            Assert.IsTrue(!toTest.GetNodeData(3).IsControlledByEnemies);
            Assert.IsTrue(toTest.GetNodeData(3).LastKnownEnemyPosition == 0);
            Assert.IsTrue(toTest.GetNodeData(3).LastKnownFriendlyPosition == 0);
            Assert.IsTrue(!toTest.GetNodeData(4).IsControlledByEnemies);
            Assert.IsTrue(toTest.GetNodeData(4).SquadMemberPresence == 1);
            Assert.IsTrue(toTest.GetNodeData(4).IsFriendlyArea);
            Assert.IsTrue(toTest.GetNodeData(5).TakingFireMagnitudeLevel == 5);
            Assert.IsTrue(toTest.GetNodeData(5).VisibleToEnemies);
            Assert.IsTrue(toTest.GetNodeData(5).PotentialEnemies);
            Assert.IsTrue(toTest.GetNodeData(5).IsClear == false);
            Assert.IsTrue(toTest.GetNodeData(6).PotentialEnemies);
            Assert.IsTrue(toTest.GetNodeData(6).IsEnemyArea == false);
            Assert.IsTrue(toTest.GetNodeData(6).IsControlledByTeam == false);
            Assert.IsTrue(toTest.GetNodeData(6).LastKnownFriendlyPosition == 2);
            Assert.IsTrue(toTest.GetNodeData(6).LastKnownEnemyPosition == 0);
            Assert.IsTrue(!toTest.GetNodeData(7).IsControlledByTeam);
            Assert.IsTrue(toTest.GetNodeData(7).EnemyPresence == 0);
            Assert.IsTrue(toTest.GetNodeData(7).SquadMemberPresence == 0);
            Assert.IsTrue(toTest.GetNodeData(1).NoKnownPresence);
            Assert.IsTrue(toTest.GetNodeData(3).NoKnownPresence);
            Assert.IsTrue(toTest.GetNodeData(7).NoKnownPresence);
            Assert.IsTrue(toTest.GetNodeData(8).NoKnownPresence);
            Assert.IsTrue(toTest.GetNodeData(9).NoKnownPresence);
            Assert.IsTrue(toTest.GetNodeData(7).PotentialEnemies);
            Assert.IsTrue(!toTest.GetNodeData(0).NoKnownPresence);
            Assert.IsTrue(toTest.GetNodeData(0).VisibleToSquad);
            Assert.IsTrue(toTest.GetNodeData(1).VisibleToSquad);
            Assert.IsTrue(toTest.GetNodeData(3).VisibleToSquad);
            Assert.IsTrue(toTest.GetNodeData(4).VisibleToSquad);
            Assert.IsTrue(!toTest.GetNodeData(2).VisibleToSquad);
            Assert.IsTrue(toTest.GetNodeData(8).LastKnownEnemyPosition == 1);
            Assert.IsTrue(!toTest.GetNodeData(8).VisibleToEnemies);
            Assert.IsTrue(toTest.GetNodeData(8).PotentialEnemies);
            Assert.IsTrue(!toTest.GetNodeData(9).PotentialEnemies);
            Assert.IsTrue(toTest.GetNodeData(8).EnemyPresence == 0);
            Assert.IsTrue(!toTest.GetNodeData(8).IsEnemyArea);
            Assert.IsTrue(!toTest.GetNodeData(8).IsContestedArea);
            Assert.IsTrue(toTest.GetNodeData(9).TakingFireMagnitudeLevel == 1);
            Assert.IsTrue(toTest.GetNodeData(8).SourceOfEnemyFire);

            // Do the same tests but using the graph-subset reader functions
            Func<int, bool> visf = toTest.VisibleToSquadReader();
            Assert.IsTrue(visf(0) && visf(1) && visf(3) && visf(4) && !visf(5) && !visf(6) && !visf(7) && !visf(2)); 
            Func<int, int> friends = toTest.KnownSquadMemberPresenceReader();
            Assert.IsTrue(friends(0) == 2);
            Assert.IsTrue(friends(1) == 0);
            Assert.IsTrue(friends(2) == 0);
            Assert.IsTrue(friends(3) == 0);
            Assert.IsTrue(friends(4) == 1);
            Func<int, bool> clear = toTest.IsClearReader();
            Assert.IsTrue(clear(0) && clear(1) && !clear(2) && clear(3) && !clear(7));
            Func<int, bool> potentials = toTest.PotentialEnemiesReader();
            Assert.IsTrue(potentials(6) && !potentials(1) && potentials(7) && potentials(8) && potentials(5) && !potentials(4) && !potentials(9));
            Func<int, bool> vis = toTest.VisibleToEnemiesReader();
            Assert.IsTrue(vis(1) && vis(2) && !vis(3) && !vis(0) && !vis(7));
            Func<int, bool> noPres = toTest.HasNoKnownPresenceReader();
            Assert.IsTrue(noPres(7) && noPres(5) && !noPres(2));
            Func<int, int> lastE = toTest.LastKnownEnemyPositionReader();
            Assert.IsTrue(lastE(8) == 1 && lastE(5) == 0 && lastE(3) == 0);
            Func<int, int> lastF = toTest.LastKnownFriendlyPositionReader();
            Assert.IsTrue(lastF(6) == 2 && lastF(8) == 0 && lastF(5) == 0 && lastF(3) == 0);
            Func<int, int> takingFire = toTest.TakingFireMagnitudeLevelReader();
            Assert.IsTrue(takingFire(9) == 1 && takingFire(4) == 0 && takingFire(0) == 0);

            // Test edges
            Func<int, int, bool> clearing = toTest.CausingClearEffectReader();
            Assert.IsTrue(clearing(0, 1) && clearing(0, 3) && !clearing(0, 2) && !clearing(2, 1) && clearing(4, 3));
            Func<int, int, bool> causingPotentials = toTest.CausingPotentialEnemiesEffectReader();
            Assert.IsTrue(causingPotentials(5, 1) && causingPotentials(5, 6) && !causingPotentials(2, 1) && causingPotentials(8, 6) && causingPotentials(8, 7));
            Func<int, int, bool> causingVisible = toTest.CausingVisibleToEnemiesEffectReader();
            Assert.IsTrue(causingVisible(2, 1) && !causingVisible(2, 7) && !causingVisible(1, 5));

            Assert.IsTrue(toTest.GetEdge(0, 1).IsCausingClearEffect && !toTest.GetEdge(0, 1).IsCausingControlledByTeamEffect);
            Assert.IsTrue(toTest.GetEdge(0, 3).IsCausingClearEffect && toTest.GetEdge(0, 3).IsCausingControlledByTeamEffect);
            Assert.IsTrue(toTest.GetEdge(2, 1).IsCausingVisibleToEnemiesEffect && toTest.GetEdge(2, 5).IsCausingVisibleToEnemiesEffect);
            Assert.IsTrue(toTest.GetEdge(5, 1).IsCausingPotentialEnemiesEffect && toTest.GetEdge(8, 6).IsCausingPotentialEnemiesEffect && toTest.GetEdge(8, 7).IsCausingPotentialEnemiesEffect);
            Assert.IsTrue(toTest.GetEdge(9, 8).IsCausingSourceOfEnemyFireEffect && toTest.GetEdge(8, 9).IsCausingTakingFireEffect);
            // TODO: 8 - Implement specfic logic to define what happens when you call a node edge read from a node, to itself. I.e. dynamicStateInstance.GetEdge(x, x).friendlies;. Possibly not allowed?
        }
    }
}
