﻿using NUnit.Framework;
using Moq;
using System;
using System.Reflection;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using System.Collections.Generic;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    public static class HardCodedStateCreator {
        public static StaticState CreateTestStaticState() {
            return new StaticState(PrepareNodeData(), PrepareEdgeData());
        }

        private static StaticState.AreaNode[] PrepareNodeData() {
            StaticState.AreaNode[] toRet = new StaticState.AreaNode[8];
            toRet[0] = new StaticState.AreaNode(0, 0, 8, 3, false, 5, 5, false, false, false);
            toRet[1] = new StaticState.AreaNode(1, 0, 0, 0, true, 1, 8, false, false, false);
            toRet[2] = new StaticState.AreaNode(2, 1, 6, 1, false, 3, 3, false, false, true);
            toRet[3] = new StaticState.AreaNode(3, 0, 0, 0, false, 1, 5, true, false, false);
            toRet[4] = new StaticState.AreaNode(4, 0, 0, 5, false, 2, 1, true, false, false);
            toRet[5] = new StaticState.AreaNode(5, 1, 3, 1, false, 6, 2, false, false, true);
            toRet[6] = new StaticState.AreaNode(6, 1, 0, 0, false, 3, 5, true, false, false);
            toRet[7] = new StaticState.AreaNode(7, 2, 0, 0, false, 1, 8, true, false, false);
            return toRet;
        }
        private static StaticState.AreaEdge[,] PrepareEdgeData() {
            StaticState.AreaEdge[,] toRet = new StaticState.AreaEdge[8, 8];
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    toRet[i, j] = new StaticState.AreaEdge(i, j, false, false, 0, 0, 0, 0, false);  // Defaults
                }
            }
            // Replace all the ones with actual data! Note 'distance' and stuff is not going to be tested since it is too tedious. Auto generation of SS should fill this in though.
            toRet[0, 1] = new StaticState.AreaEdge(0, 1, true, true, 5, 5, 3, 5, false);
            toRet[0, 3] = new StaticState.AreaEdge(0, 3, true, true, 5, 5, 6, 5, true);
            toRet[1, 0] = new StaticState.AreaEdge(1, 0, true, true, 5, 5, -3, 0, false);
            toRet[3, 0] = new StaticState.AreaEdge(3, 0, true, true, 5, 5, -6, 0, false);
            toRet[3, 4] = new StaticState.AreaEdge(3, 4, true, true, 5, 5, 0, 1, false);
            toRet[4, 3] = new StaticState.AreaEdge(4, 3, true, true, 5, 5, 0, 1, false);
            toRet[2, 1] = new StaticState.AreaEdge(2, 1, true, true, 5, 5, 4, 4, false);
            toRet[1, 2] = new StaticState.AreaEdge(1, 2, true, true, 5, 5, -4, 0, false);
            toRet[5, 1] = new StaticState.AreaEdge(5, 1, true, false, 7, 4, 5, 8, false);
            toRet[1, 5] = new StaticState.AreaEdge(1, 5, true, false, 7, 3, -5, 2, false);
            toRet[2, 5] = new StaticState.AreaEdge(2, 5, true, true, 5, 5, 0, 0, false);
            toRet[5, 2] = new StaticState.AreaEdge(5, 2, true, true, 5, 5, 0, 0, false);
            toRet[5, 6] = new StaticState.AreaEdge(5, 6, true, true, 5, 5, 2, 1, false);
            toRet[6, 5] = new StaticState.AreaEdge(6, 5, true, true, 5, 5, -2, 0, false);
            toRet[6, 7] = new StaticState.AreaEdge(6, 7, true, true, 5, 5, 6, 3, true);
            toRet[7, 6] = new StaticState.AreaEdge(7, 6, true, true, 5, 5, -6, 0, false);

            return toRet;
        }

        public static void CheckTestStaticState(StaticState s) {
            // Assert the overwatch locations
            Func<int, bool> reader = s.IsOverwatchLocation();
            for (int i = 0; i < 8; i++) {
                // Should be overwatch locations
                if (i == 2 || i == 5) {
                    Assert.IsTrue(reader(i));
                    Assert.IsTrue(s.GetNodeData(i).OverwatchLocation);
                }
                // Should NOT be overwatch locations
                else {
                    Assert.IsFalse(reader(i));
                    Assert.IsFalse(s.GetNodeData(i).OverwatchLocation);
                }
            }

            // Assert visibility relationships
            Func<int, int, bool> vis = s.CanSeeReader();
            Func<int, int, bool> con = s.HasControlOverReader();
            Func<int, int, bool> trav = s.IsConnectedReader();
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    // Should be visible
                    if (   (i == 0 && j == 1 || i == 1 && j == 0)
                        || (i == 0 && j == 3 || i == 3 && j == 0)
                        || (i == 3 && j == 4 || i == 4 && j == 3)
                        || (i == 1 && j == 2 || i == 2 && j == 1)
                        || (i == 1 && j == 5 || i == 5 && j == 1)
                        || (i == 2 && j == 5 || i == 5 && j == 2)
                        || (i == 5 && j == 6 || i == 6 && j == 5)
                        || (i == 6 && j == 7 || i == 7 && j == 6)
                       ) {
                        Assert.IsTrue(vis(i, j));
                        Assert.IsTrue(s.GetEdge(i, j).CanSee);
                        Assert.IsTrue(s.GetEdge(i, j).CanBeSeenFrom);
                    }
                    // Should NOT be visible
                    else {
                        Assert.IsFalse(vis(i, j));
                        Assert.IsFalse(s.GetEdge(i, j).CanSee);
                        Assert.IsFalse(s.GetEdge(i, j).CanBeSeenFrom);
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
                       ) {
                        Assert.IsTrue(trav(i, j));
                        Assert.IsTrue(s.GetEdge(i, j).IsConnected);
                    }
                    // Should NOT be visible
                    else {
                        Assert.IsFalse(trav(i, j));
                        Assert.IsFalse(s.GetEdge(i, j).IsConnected);
                    }
                }
            }
        }

        public static DynamicState CreateTestDynamicState() {
            int numNodes = 8;   // Let's test this with 8 areas
            var facts = new Dictionary<FactType, Fact>[8];

            // Set up the effects that each fact is causing.
            // Node 0 causes 'clear' on node 1 and 3, and 'control' on 3
            List<Effect> e = new List<Effect> {
                new Effect(EffectType.Clear, 0 ,1, 0),
                new Effect(EffectType.Clear, 0, 3, 0),
                new Effect(EffectType.Controlled, 0, 3, 0)
            };
            facts[0] = new Dictionary<FactType, Fact> { { FactType.FriendlyPresence, new Fact(FactType.FriendlyPresence, 2, e) } };

            // Node 1 has no facts
            facts[1] = new Dictionary<FactType, Fact> { };

            // Node 2 causes 'visible to enemies' on 1 and 5
            e = new List<Effect> {
                new Effect(EffectType.VisibleToEnemies, 0, 1, 2),
                new Effect(EffectType.VisibleToEnemies, 0, 5, 2)
            };
            facts[2] = new Dictionary<FactType, Fact> { { FactType.EnemyPresence, new Fact(FactType.EnemyPresence, 3, e) } };

            // Node 3 has no facts
            facts[3] = new Dictionary<FactType, Fact> { };

            // Node 4 causes 'clear' on node 3
            e = new List<Effect> {
                new Effect(EffectType.Clear, 0, 3, 4),
            };
            facts[4] = new Dictionary<FactType, Fact> { { FactType.FriendlyPresence, new Fact(FactType.FriendlyPresence, 1, e) } };

            // Node 5 causes 'potential enemies' on node 1 and 6
            e = new List<Effect> {
                new Effect(EffectType.PotentialEnemies, 0, 1, 5),
                new Effect(EffectType.PotentialEnemies, 0, 6, 5)
            };
            facts[5] = new Dictionary<FactType, Fact> { { FactType.DangerFromUnknownSource, new Fact(FactType.DangerFromUnknownSource, 5, e) } };

            // Node 6 and 7 has no facts
            facts[6] = new Dictionary<FactType, Fact> { };
            facts[7] = new Dictionary<FactType, Fact> { };

            return new DynamicState(facts);
        }
        
        public static void CheckTestDynamicState(DynamicState toTest) {
            // Test node data reads
            Assert.IsTrue(toTest.GetNodeData(0).FriendlyPresence == 2);
            Assert.IsTrue(toTest.GetNodeData(0).EnemyPresence == 0);
            Assert.IsTrue(toTest.GetNodeData(0).IsFriendlyArea);
            Assert.IsTrue(toTest.GetNodeData(0).IsClear);
            Assert.IsTrue(toTest.GetNodeData(0).IsControlledByTeam);
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
            Assert.IsTrue(!toTest.GetNodeData(4).IsControlledByEnemies);
            Assert.IsTrue(toTest.GetNodeData(4).FriendlyPresence == 1);
            Assert.IsTrue(toTest.GetNodeData(4).IsFriendlyArea);
            Assert.IsTrue(toTest.GetNodeData(5).DangerLevel == 5);
            Assert.IsTrue(toTest.GetNodeData(5).VisibleToEnemies);
            Assert.IsTrue(toTest.GetNodeData(5).PotentialEnemies);
            Assert.IsTrue(toTest.GetNodeData(5).IsClear == false);
            Assert.IsTrue(toTest.GetNodeData(6).PotentialEnemies);
            Assert.IsTrue(toTest.GetNodeData(6).IsEnemyArea == false);
            Assert.IsTrue(toTest.GetNodeData(6).IsControlledByTeam == false);
            Assert.IsTrue(!toTest.GetNodeData(7).IsControlledByTeam);
            Assert.IsTrue(toTest.GetNodeData(7).EnemyPresence == 0);
            Assert.IsTrue(toTest.GetNodeData(7).FriendlyPresence == 0);

            // Do the same tests but using the graph-subset reader functions
            Func<int, int> friends = toTest.KnownFriendlyPresenceReader();
            Assert.IsTrue(friends(0) == 2);
            Assert.IsTrue(friends(1) == 0);
            Assert.IsTrue(friends(2) == 0);
            Assert.IsTrue(friends(3) == 0);
            Assert.IsTrue(friends(4) == 1);
            Func<int, bool> clear = toTest.IsClearReader();
            Assert.IsTrue(clear(0) && clear(1) && !clear(2) && clear(3) && !clear(7));
            Func<int, bool> potentials = toTest.PotentialEnemiesReader();
            Assert.IsTrue(potentials(6) && !potentials(1) && !potentials(7) && !potentials(7) && potentials(5));
            Func<int, bool> vis = toTest.VisibleToEnemiesReader();
            Assert.IsTrue(vis(1) && vis(2) && !vis(3) && !vis(0) && !vis(7));

            // Test edges
            Func<int, int, bool> clearing = toTest.CausingClearEffectReader();
            Assert.IsTrue(clearing(0, 1) && clearing(0, 3) && !clearing(0, 2) && !clearing(2, 1) && clearing(4, 3));
            Func<int, int, bool> causingPotentials = toTest.CausingPotentialEnemiesEffectReader();
            Assert.IsTrue(causingPotentials(5, 1) && causingPotentials(5, 6) && !causingPotentials(2, 1));
            Func<int, int, bool> causingVisible = toTest.CausingVisibleToEnemiesEffectReader();
            Assert.IsTrue(causingVisible(2, 1) && !causingVisible(2, 7) && !causingVisible(1, 5));

            Assert.IsTrue(toTest.GetEdge(0, 1).IsCausingClearEffect && !toTest.GetEdge(0, 1).IsCausingControlledByTeamEffect);
            Assert.IsTrue(toTest.GetEdge(0, 3).IsCausingClearEffect && toTest.GetEdge(0, 3).IsCausingControlledByTeamEffect);
            Assert.IsTrue(toTest.GetEdge(2, 1).IsCausingVisibleToEnemiesEffect && toTest.GetEdge(2, 5).IsCausingVisibleToEnemiesEffect);
            Assert.IsTrue(toTest.GetEdge(5, 1).IsCausingPotentialEnemiesEffect);
        }

    }
}