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
        private FactSet[] factSets;
        private Fact[][] facts;
        
        public DynamicStateTests() {
            //BuildDynamicStateToTest();
        }

        private void BuildDynamicStateToTest() {
            int numNodes = 8;   // Let's test this with 8 areas
            facts = new Fact[8][];

            // Set up the effects that each fact is causing.
            // Node 0 causes 'clear' on node 1 and 3, and 'control' on 3
            Effect[] e = {
                new Effect(EffectType.Clear, 0, 1, new EffectType[] {EffectType.PotentialEnemies}),
                new Effect(EffectType.Clear, 0, 3, new EffectType[] {EffectType.PotentialEnemies}),
                new Effect(EffectType.Controlled, 0, 3, new EffectType[] {EffectType.PotentialEnemies})
            };
            facts[0] = new Fact[] { new Fact(FactType.FriendlyPresence, 2, e) };

            // Node 1 has no facts
            facts[1] = new Fact[0];

            // Node 2 causes 'visible to enemies' on 1 and 5
            e = new Effect[] {
                new Effect(EffectType.VisibleToEnemies, 0, 1, new EffectType[0]),
                new Effect(EffectType.VisibleToEnemies, 0, 5, new EffectType[0])
            };
            facts[2] = new Fact[] { new Fact(FactType.EnemyPresence, 3, e) };

            // Node 3 has no facts
            facts[3] = new Fact[0];

            // Node 4 causes 'clear' on node 3
            e = new Effect[] {
                new Effect(EffectType.Clear, 0, 3, new EffectType[] {EffectType.PotentialEnemies}),
            };
            facts[4] = new Fact[] { new Fact(FactType.FriendlyPresence, 1, e) };

            // Node 5 causes 'potential enemies' on node 1 and 6
            e = new Effect[] {
                new Effect(EffectType.PotentialEnemies, 0, 1, new EffectType[0]),
                new Effect(EffectType.PotentialEnemies, 0, 6, new EffectType[0])
            };
            facts[5] = new Fact[] { new Fact(FactType.DangerFromUnknownSource, 5, e) };

            // Node 6 and 7 has no facts
            facts[6] = new Fact[0];
            facts[7] = new Fact[0];

            factSets = new FactSet[8];
            for (int i = 0; i < 8; i++) {
                factSets[i] = new FactSet(new HashSet<Fact>(facts[i]));
            }
            toTest = new DynamicState(factSets);
        }

        [Test]
        public void DynamicState_ReadNodeData_ReturnsCorrectInformation() {
            // Build (to see how long it takes to do repeatedly)
            for (int i=0; i < 1000; i++) {
                BuildDynamicStateToTest();
            }

            Assert.IsTrue(toTest.GetNodeData(0).FriendlyPresence == 2);
            Assert.IsTrue(toTest.GetNodeData(0).EnemyPresence == 0);
            Assert.IsTrue(toTest.GetNodeData(0).IsFriendlyArea);
            Assert.IsTrue(!toTest.GetNodeData(1).IsControlledByTeam);
            Assert.IsTrue(toTest.GetNodeData(1).IsClear);
            Assert.IsTrue(toTest.GetNodeData(1).VisibleToEnemies);
            Assert.IsTrue(!toTest.GetNodeData(1).PotentialEnemies);
            Assert.IsTrue(toTest.GetNodeData(2).EnemyPresence == 3);
            Assert.IsTrue(toTest.GetNodeData(2).IsEnemyArea);
            Assert.IsTrue(!toTest.GetNodeData(2).IsContestedArea);
            Assert.IsTrue(!toTest.GetNodeData(2).IsClear);
            Assert.IsTrue(toTest.GetNodeData(3).IsClear);
            Assert.IsTrue(toTest.GetNodeData(3).IsControlledByTeam);
            Assert.IsTrue(!toTest.GetNodeData(3).IsControlledByEnemies);
            Assert.IsTrue(!toTest.GetNodeData(4).IsControlledByEnemies);
            Assert.IsTrue(toTest.GetNodeData(4).FriendlyPresence == 1);
            Assert.IsTrue(toTest.GetNodeData(4).IsFriendlyArea);
            Assert.IsTrue(toTest.GetNodeData(5).IsDangerSourceKnown == false);
            Assert.IsTrue(toTest.GetNodeData(5).DangerLevel == 5);
            Assert.IsTrue(toTest.GetNodeData(5).VisibleToEnemies);
            Assert.IsTrue(toTest.GetNodeData(5).IsClear == false);
            Assert.IsTrue(toTest.GetNodeData(6).PotentialEnemies);
            Assert.IsTrue(toTest.GetNodeData(6).IsEnemyArea == false);
            Assert.IsTrue(toTest.GetNodeData(6).IsControlledByTeam == false);
            Assert.IsTrue(!toTest.GetNodeData(7).IsControlledByTeam);
            Assert.IsTrue(!toTest.GetNodeData(7).IsDangerSourceKnown);
            Assert.IsTrue(toTest.GetNodeData(7).EnemyPresence == 0);
            Assert.IsTrue(toTest.GetNodeData(7).FriendlyPresence == 0);
        }

    }
}
