using NUnit.Framework;
using Moq;
using System;
using System.Reflection;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using System.Collections.Generic;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects {

    [TestFixture]
    class DynamicStateTests {

        private DynamicState toTest;
        private Dictionary<FactType, Fact>[] facts;

        public DynamicStateTests() {
            //BuildDynamicStateToTest();
        }

        private void PrepareFactData() {
            int numNodes = 8;   // Let's test this with 8 areas
            facts = new Dictionary<FactType, Fact>[8];

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
        }
        private void BuildDynamicStateToTest() {
            toTest = new DynamicState(facts);
        }

        [Test]
        public void DynamicState_ReadNodeData_ReturnsCorrectInformation() {
            // Build (to see how long it takes to do repeatedly)
            PrepareFactData();
            for (int i = 0; i < 1; i++) {
                BuildDynamicStateToTest();
                //PrepareFactData();
            }
            for (int i = 0; i < 1; i++) {
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
                Assert.IsTrue(clearing(0, 1) && clearing(0, 3) && !clearing(0, 2) && !clearing(2, 1) && clearing(4,3));
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
}
