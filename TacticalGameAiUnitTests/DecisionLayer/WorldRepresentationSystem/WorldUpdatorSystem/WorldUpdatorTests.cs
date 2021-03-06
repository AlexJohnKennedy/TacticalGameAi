﻿using System;
using System.Collections.Generic;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using Moq;
using NUnit.Framework;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem {

    [TestFixture]
    class WorldUpdatorTests {

        [Test]
        public void WorldUpdator_PassedIncompleteFactAdderSetOnConstruction_Throws() {
            // Mock some fake FactAdders, and associate them with facttypes in order to build a WorldUpdator. These need not have any effect adders in them...
            Mock<IFactAdder> friendlyAdder = new Mock<IFactAdder>();
            Mock<IFactAdder> enemyAdder = new Mock<IFactAdder>();
            Mock<IFactAdder> unknownDangerAdder = new Mock<IFactAdder>();
            Dictionary<FactType, IFactAdder> dict = new Dictionary<FactType, IFactAdder> {
                { FactType.FriendlyPresence,        friendlyAdder.Object      },
                { FactType.EnemyPresence,           enemyAdder.Object         },
                { FactType.TakingFireFromUnknownSource, unknownDangerAdder.Object }
            };
            Assert.Throws<ArgumentException>(() => new WorldUpdator(dict));
        }

        [Test]
        public void WorldUpdator_ApplyDynamicWorldChange_SuccessfullyCallsCorrectAdderLogic() {
            // Mock some fake FactAdders, and associate them with facttypes in order to build a WorldUpdator. These need not have any effect adders in them...
            Dictionary<FactType, IFactAdder> dict = new Dictionary<FactType, IFactAdder>();
            Dictionary<FactType, Mock<IFactAdder>> mocks = new Dictionary<FactType, Mock<IFactAdder>>();
            foreach (FactType t in Enum.GetValues(typeof(FactType))) {
                mocks.Add(t, new Mock<IFactAdder>());
                dict.Add(t, mocks[t].Object);
            }
            
            WorldUpdator toTest = new WorldUpdator(dict);
            int time = 175;

            // Mock a fake 'DynamicStateChange' interface object, which represents the data it should attempt to change.
            // For this test, let's say the change represents some new enemies spotted in node 2, who kill friendlies in node 4 (removing them).
            // The new enemies also cause 'danger' in both node 4 and node 5.
            Mock<IDynamicStateChange> change1 = new Mock<IDynamicStateChange>();
            change1.Setup(c => c.AffectedNodes).Returns(new int[] { 2, 4, 5 });     // The change affects nodes 2, 4, and 5
            change1.Setup(c => c.TimeLearned).Returns(time);
            Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> beforeFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                // No facts are removed or updated in node 2 by this change.
                { 2, new (FactType, int, IEnumerable<int>)[] { } },

                // Node 4 has the friendlies fact removed by this change. Thus it must appear in the 'before' set so that this change is reversible.
                { 4, new (FactType, int, IEnumerable<int>)[] { (FactType.FriendlyPresence, 1, null) } },

                // No facts are removed or updated in node 5 by this change.
                { 5, new (FactType, int, IEnumerable<int>)[] { } }
            };
            change1.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> afterFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                // Enemies were spotted in node 2 during this event
                { 2, new (FactType, int, IEnumerable<int>)[] { (FactType.EnemyPresence, 2, null) } },

                // Node 4 has danger applied to it with a super high rating since friendlies were wiped out!
                { 4, new (FactType, int, IEnumerable<int>)[] { (FactType.TakingFire, 7, null) } },

                // Node 5 also had danger applied to it!
                { 5, new (FactType, int, IEnumerable<int>)[] { (FactType.TakingFire, 2, null) } }
            };
            change1.Setup(c => c.GetFactsAfter()).Returns(afterFacts);

            // Mock a fake world object.
            WorldRepresentation fakeWorld = WorldRep_ValueObjectMocker.NewWorldRepresentationMock();


            // Apply the change using the world updator!
            WorldRepresentation result = toTest.ApplyDynamicStateChange(fakeWorld, change1.Object);

            // VERIFY that the correct removal and add calls were made on the correct factAdder objects. ----------------------------

            // Friendly Adder should have been called once for removal on node 4!
            mocks[FactType.FriendlyPresence].Verify(f => f.RemoveFact(fakeWorld, 4, It.IsAny<Dictionary<FactType, Fact.MutableFact>>()), Times.Once());

            // Danger Adder should have been called once for node 4, and once for node 5, with the correct value levels!
            mocks[FactType.TakingFire].Verify(d => d.AddFact(fakeWorld, 4, 7, time, It.IsAny<Dictionary<FactType, Fact.MutableFact>>(), null), Times.Once());
            mocks[FactType.TakingFire].Verify(d => d.AddFact(fakeWorld, 5, 2, time, It.IsAny<Dictionary<FactType, Fact.MutableFact>>(), null), Times.Once());

            // Enemy Adder should have been called once on node 2 for adding.
            mocks[FactType.EnemyPresence].Verify(e => e.AddFact(fakeWorld, 2, 2, time, It.IsAny<Dictionary<FactType, Fact.MutableFact>>(), null), Times.Once());

            // Other adders should not have been called.
            foreach(Mock<IFactAdder> m in mocks.Values) {
                m.VerifyNoOtherCalls();
            }

            Assert.IsTrue(fakeWorld != result);
        }

        [Test]
        public void WorldUpdator_RevertDynamicWorldChange_SuccessfullyCallsCorrectAdderLogic() {
            // Mock some fake FactAdders, and associate them with facttypes in order to build a WorldUpdator. These need not have any effect adders in them...
            Dictionary<FactType, IFactAdder> dict = new Dictionary<FactType, IFactAdder>();
            Dictionary<FactType, Mock<IFactAdder>> mocks = new Dictionary<FactType, Mock<IFactAdder>>();
            foreach (FactType t in Enum.GetValues(typeof(FactType))) {
                mocks.Add(t, new Mock<IFactAdder>());
                dict.Add(t, mocks[t].Object);
            }

            int time = 100;
            WorldUpdator toTest = new WorldUpdator(dict);

            // Mock a fake 'DynamicStateChange' interface object, which represents the data it should attempt to change.
            // For this test, let's say the change represents some new enemies spotted in node 2, who kill friendlies in node 4 (removing them).
            // The new enemies also cause 'danger' in both node 4 and node 5.
            Mock<IDynamicStateChange> change1 = new Mock<IDynamicStateChange>();
            change1.Setup(c => c.AffectedNodes).Returns(new int[] { 2, 4, 5 });     // The change affects nodes 2, 4, and 5
            change1.Setup(c => c.TimeLearned).Returns(time);
            Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> beforeFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                // No facts are removed or updated in node 2 by this change.
                { 2, new (FactType, int, IEnumerable<int>)[] { } },

                // Node 4 has the friendlies fact removed by this change. Thus it must appear in the 'before' set so that this change is reversible.
                { 4, new (FactType, int, IEnumerable<int>)[] { (FactType.FriendlyPresence, 1, null) } },

                // No facts are removed or updated in node 5 by this change.
                { 5, new (FactType, int, IEnumerable<int>)[] { } }
            };
            change1.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> afterFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                // Enemies were spotted in node 2 during this event
                { 2, new (FactType, int, IEnumerable<int>)[] { (FactType.EnemyPresence, 2, null) } },

                // Node 4 has danger applied to it with a super high rating since friendlies were wiped out!
                { 4, new (FactType, int, IEnumerable<int>)[] { (FactType.TakingFire, 7, new int[] { 3 }) } },

                // Node 5 also had danger applied to it!
                { 5, new (FactType, int, IEnumerable<int>)[] { (FactType.TakingFire, 2, new int[] { 3 }) } }
            };
            change1.Setup(c => c.GetFactsAfter()).Returns(afterFacts);

            // Mock a fake world object.
            WorldRepresentation fakeWorld = WorldRep_ValueObjectMocker.NewWorldRepresentationMock();


            // Apply the change using the world updator!
            WorldRepresentation result = toTest.RevertDynamicStateChange(fakeWorld, change1.Object);

            // VERIFY that the correct removal and add calls were made on the correct factAdder objects. ----------------------------

            // Friendly Adder should have been called once for addition on node 4 with a value of 1 (the 'before' fact)
            mocks[FactType.FriendlyPresence].Verify(f => f.AddFact(fakeWorld, 4, 1, time, It.IsAny<Dictionary<FactType, Fact.MutableFact>>(), null), Times.Once());

            // Danger Adder should have called 'remove' once for node 4, and once for node 5.
            mocks[FactType.TakingFire].Verify(d => d.RemoveFact(fakeWorld, 4, It.IsAny<Dictionary<FactType, Fact.MutableFact>>()), Times.Once());
            mocks[FactType.TakingFire].Verify(d => d.RemoveFact(fakeWorld, 5, It.IsAny<Dictionary<FactType, Fact.MutableFact>>()), Times.Once());

            // Enemy Adder should have been called once on node 2 for adding.
            mocks[FactType.EnemyPresence].Verify(e => e.RemoveFact(fakeWorld, 2, It.IsAny<Dictionary<FactType, Fact.MutableFact>>()), Times.Once());

            // Other adders should not have been called.
            foreach (Mock<IFactAdder> m in mocks.Values) {
                m.VerifyNoOtherCalls();
            }

            Assert.IsTrue(fakeWorld != result);
        }
    }
}
