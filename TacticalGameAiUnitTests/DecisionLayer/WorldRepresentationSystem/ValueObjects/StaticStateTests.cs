using NUnit.Framework;
using Moq;
using System;
using System.Reflection;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    [TestFixture]
    class StaticStateTests {

        // 8 Nodes designed to match the dynamic state test.
        private StaticState.AreaNode[] PrepareNodeData() {
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
        private StaticState.AreaEdge[,] PrepareEdgeData() {
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

        [Test]
        public void StaticState_TestingDataReads_CorrectValues() {
            // Arrange the static state object to test.
            StaticState s = new StaticState(PrepareNodeData(), PrepareEdgeData());

            // Assert the overwatch locations
            Func<int, bool> reader = s.IsOverwatchLocation();
            for (int i=0; i<8; i++) {
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
            for (int i=0; i < 8; i++) {
                for (int j=0; j < 8; j++) {
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
                    if ( i == 0 && j == 3 || i == 6 && j == 7) {
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
                    if ((i == 0 && j == 1 || i == 1 && j == 0)
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

        [Test]
        public void Constructor_PassedMismatchingSizeArrays_Throws() {
            StaticState.AreaEdge[,] e1 = new StaticState.AreaEdge[5, 5];
            StaticState.AreaNode[]  n1 = new StaticState.AreaNode[4];

            Assert.Throws<ArgumentException>(() => new StaticState(n1, e1));
        }
        [Test]
        public void Constructor_PassedNonSqaureMatrix_Throws() {
            StaticState.AreaEdge[,] e1 = new StaticState.AreaEdge[5, 6];
            StaticState.AreaNode[] n1 = new StaticState.AreaNode[5];

            Assert.Throws<ArgumentException>(() => new StaticState(n1, e1));
        }
        [Test]
        public void Constructor_PassedNullArrays_Throws() {
            StaticState.AreaEdge[,] e1 = new StaticState.AreaEdge[5, 5];
            StaticState.AreaNode[] n1 = new StaticState.AreaNode[5];

            Assert.Throws<ArgumentNullException>(() => new StaticState(n1, null));
            Assert.Throws<ArgumentNullException>(() => new StaticState(n1, null));
            Assert.Throws<ArgumentNullException>(() => new StaticState(null, null));
        }
    }
}
