﻿using NUnit.Framework;
using Moq;
using System;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem;
using TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using System.Collections.Generic;

namespace TacticalGameAiIntegrationTests.DecisionLayer.WorldRepresentationSystem {

    [TestFixture]
    class DefaultWorldUpdatorTest {
        private Mock<IDynamicStateChange> change1;  // Mock DynamicStateChange: spotting 2 friendlies at node 0
        private Mock<IDynamicStateChange> change2;  // Mock DynamicStateChange: spotting 1 friendly at node 4 and DangerfromunknownSource at node 5
        private Mock<IDynamicStateChange> change3;  // Mock DynamicStateChange: spotting 3 enemies at node 2
        private Mock<IDynamicStateChange> change4;  // Mock DynamicStateChange: 2 last known friendlies at node 6, 1 last known enemy at node 8
        private Mock<IDynamicStateChange> changeToRevert1;  // Spotting 1 enemy at node 5, and losing friendlies at node 0, and danger from unknown source leaving. (applied and reverted after change1)
        private int time1 = 50;   // Specify an arbitrary 'time learned' for each change.
        private int time2 = 70;
        private int time3 = 90;
        private int time4 = 110;
        private int timeToRevert1 = 130;

        public DefaultWorldUpdatorTest() {
            change1 = new Mock<IDynamicStateChange>();
            change1.Setup(c => c.AffectedNodes).Returns(new int[] { 0 });
            change1.Setup(c => c.TimeLearned).Returns(time1);
            Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> beforeFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                { 0, new (FactType, int, IEnumerable<int>)[] { } },
            };
            change1.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> afterFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                { 0, new (FactType, int, IEnumerable<int>)[] { (FactType.SquadMemberPresence, 2, null)} },
            };
            change1.Setup(c => c.GetFactsAfter()).Returns(afterFacts);
            change2 = new Mock<IDynamicStateChange>();
            change2.Setup(c => c.AffectedNodes).Returns(new int[] { 4, 5 });
            change2.Setup(c => c.TimeLearned).Returns(time2);
            beforeFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                { 4, new (FactType, int, IEnumerable<int>)[] { } },
                { 5, new (FactType, int, IEnumerable<int>)[] { } },
            };
            change2.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            afterFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                { 4, new (FactType, int, IEnumerable<int>)[] { (FactType.SquadMemberPresence, 1, null)} },
                { 5, new (FactType, int, IEnumerable<int>)[] { (FactType.TakingFireFromUnknownSource, 5, null)} },
            };
            change2.Setup(c => c.GetFactsAfter()).Returns(afterFacts);
            change3 = new Mock<IDynamicStateChange>();
            change3.Setup(c => c.AffectedNodes).Returns(new int[] { 2, 9 });
            change3.Setup(c => c.TimeLearned).Returns(time3);
            beforeFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                { 2, new (FactType, int, IEnumerable<int>)[] { } },
                { 9, new (FactType, int, IEnumerable<int>)[] { } },
            };
            change3.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            afterFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                { 2, new (FactType, int, IEnumerable<int>)[] { (FactType.EnemyPresence, 3, null)} },
                { 9, new (FactType, int, IEnumerable<int>)[] { (FactType.TakingFire, 1, new int[] { 8 })} }
            };
            change3.Setup(c => c.GetFactsAfter()).Returns(afterFacts);
            change4 = new Mock<IDynamicStateChange>();
            change4.Setup(c => c.AffectedNodes).Returns(new int[] { 6, 8 });
            change4.Setup(c => c.TimeLearned).Returns(time4);
            beforeFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                { 6, new (FactType, int, IEnumerable<int>)[] { } },
                { 8, new (FactType, int, IEnumerable<int>)[] { } }
            };
            change4.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            afterFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                { 6, new (FactType, int, IEnumerable<int>)[] { (FactType.LastKnownFriendlyPosition, 2, null)} },
                { 8, new (FactType, int, IEnumerable<int>)[] { (FactType.LastKnownEnemyPosition, 1, null)} },
            };
            change4.Setup(c => c.GetFactsAfter()).Returns(afterFacts);

            changeToRevert1 = new Mock<IDynamicStateChange>();
            changeToRevert1.Setup(c => c.AffectedNodes).Returns(new int[] { 0, 5 });
            changeToRevert1.Setup(c => c.TimeLearned).Returns(timeToRevert1);
            beforeFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                { 0, new (FactType, int, IEnumerable<int>)[] { (FactType.SquadMemberPresence, 2, null)} },
                { 5, new (FactType, int, IEnumerable<int>)[] { (FactType.TakingFireFromUnknownSource, 5, null)} }
            };
            changeToRevert1.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            afterFacts = new Dictionary<int, IEnumerable<(FactType, int, IEnumerable<int>)>> {
                { 5, new (FactType, int, IEnumerable<int>)[] { (FactType.EnemyPresence, 1, null)} },
            };
            changeToRevert1.Setup(c => c.GetFactsAfter()).Returns(afterFacts);
        }

        [Test]
        public void WorldUpdatorSystemAndWorldRepresentationSystem_ApplyConcecutiveStateChanges() {
            // Create the default World Update using the default (hard coded) WorldUpdatorFactory.
            WorldUpdator worldUpdator = new HardCodedDefaultWorldUpdatorBuilder().BuildWorldUpdator();

            // Create the testable WorldRepresentation with pre-defined static state, and with blank DynamicState and Interpretation
            WorldRepresentation originalWorld = new WorldRepresentation(HardCodedStateCreator.CreateTestStaticStateFromHardCode());

            // Ensure that everything was created correctly.
            HardCodedStateCreator.CheckTestStaticState(originalWorld.StaticState);      // Checks Static state.
            for (int i=0; i < originalWorld.NumberOfNodes; i++) {
                Assert.IsTrue(DynamicStateInternalReader.GetNodeFact(i, originalWorld.DynamicState).Count == 0);    // Checks that no Facts exist.
                Assert.IsTrue(DynamicStateInternalReader.GetNodeEffectSum(i, originalWorld.DynamicState).Count == 0);   // Checks that no Effects exist.
            }

            // Apply all the changes in various orders, asserting each time that the result is correct

            WorldRepresentation changed1 = worldUpdator.ApplyDynamicStateChange(originalWorld, change1.Object);
            WorldRepresentation changed2 = worldUpdator.ApplyDynamicStateChange(changed1, change2.Object);
            WorldRepresentation changed3 = worldUpdator.ApplyDynamicStateChange(changed2, change3.Object);
            WorldRepresentation changed4 = worldUpdator.ApplyDynamicStateChange(changed3, change4.Object);

            HardCodedStateCreator.CheckTestStaticState(changed4.StaticState);
            HardCodedStateCreator.CheckTestDynamicState(changed4.DynamicState);

            changed1 = worldUpdator.ApplyDynamicStateChange(originalWorld, change4.Object);
            changed2 = worldUpdator.ApplyDynamicStateChange(changed1, change3.Object);
            changed3 = worldUpdator.ApplyDynamicStateChange(changed2, change1.Object);
            changed4 = worldUpdator.ApplyDynamicStateChange(changed3, change2.Object);

            HardCodedStateCreator.CheckTestStaticState(changed4.StaticState);
            HardCodedStateCreator.CheckTestDynamicState(changed4.DynamicState);


            changed1 = worldUpdator.ApplyDynamicStateChangesSequentially(originalWorld, change3.Object, change2.Object, change1.Object, change4.Object);
            HardCodedStateCreator.CheckTestStaticState(changed1.StaticState);
            HardCodedStateCreator.CheckTestDynamicState(changed1.DynamicState);

            changed2 = worldUpdator.ApplyDynamicStateChangesSequentially(originalWorld, change2.Object, change4.Object, change1.Object, change3.Object);
            HardCodedStateCreator.CheckTestStaticState(changed2.StaticState);
            HardCodedStateCreator.CheckTestDynamicState(changed2.DynamicState);
        }

        [Test]
        public void WorldUpdatorSystemAndWorldRepresentationSystem_BuildDefaultStateWithRevertsInvolved() {
            WorldUpdator worldUpdator = new HardCodedDefaultWorldUpdatorBuilder().BuildWorldUpdator();
            WorldRepresentation originalWorld = new WorldRepresentation(HardCodedStateCreator.CreateTestStaticStateFromHardCode());

            // Ensure that everything was created correctly.
            HardCodedStateCreator.CheckTestStaticState(originalWorld.StaticState);      // Checks Static state.
            for (int i = 0; i < originalWorld.NumberOfNodes; i++) {
                Assert.IsTrue(DynamicStateInternalReader.GetNodeFact(i, originalWorld.DynamicState).Count == 0);    // Checks that no Facts exist.
                Assert.IsTrue(DynamicStateInternalReader.GetNodeEffectSum(i, originalWorld.DynamicState).Count == 0);   // Checks that no Effects exist.
            }

            // Apply all changes, including the one to be reverted.
            WorldRepresentation result = worldUpdator.ApplyDynamicStateChangesSequentially(originalWorld, change1.Object, change2.Object, change3.Object, change4.Object, changeToRevert1.Object);

            // Simple assertions to see if the 'change to revert' event worked correctly..
            Func<int, int> friendlies = result.DynamicState.KnownSquadMemberPresenceReader();
            Func<int, int> enemies = result.DynamicState.KnownEnemyPresenceReader();
            for (int i=0; i < result.NumberOfNodes; i++) {
                if (i == 4) { Assert.IsTrue(friendlies(i) == 1); }
                else { Assert.IsTrue(friendlies(i) == 0); }
                if (i == 2) { Assert.IsTrue(enemies(i) == 3); }
                else if (i == 5) { Assert.IsTrue(enemies(i) == 1); Assert.IsTrue(result.DynamicState.GetNodeData(i).TakingFireMagnitudeLevel == 0); }
                else { Assert.IsTrue(enemies(i) == 0); }
            }

            // Apply again, but in a different order. Any order should be valid so long as Change1 preceeds changeToRevert1. (since changetorevert1 removes a fact added by change1).
            WorldRepresentation result2 = worldUpdator.ApplyDynamicStateChangesSequentially(originalWorld, change4.Object, change1.Object, changeToRevert1.Object, change3.Object, change2.Object);

            friendlies = result2.DynamicState.KnownSquadMemberPresenceReader();
            enemies = result2.DynamicState.KnownEnemyPresenceReader();
            for (int i = 0; i < result.NumberOfNodes; i++) {
                if (i == 4) { Assert.IsTrue(friendlies(i) == 1); }
                else { Assert.IsTrue(friendlies(i) == 0); }
                if (i == 2) { Assert.IsTrue(enemies(i) == 3); }
                else if (i == 5) { Assert.IsTrue(enemies(i) == 1); Assert.IsTrue(result.DynamicState.GetNodeData(i).TakingFireMagnitudeLevel == 0); }
                else { Assert.IsTrue(enemies(i) == 0); }
            }

            // Revert the final change, which should result in the 'default' state being true.
            result = worldUpdator.RevertDynamicStateChange(result, changeToRevert1.Object);
            HardCodedStateCreator.CheckTestDynamicState(result.DynamicState);
            HardCodedStateCreator.CheckTestStaticState(result.StaticState);

            result2 = worldUpdator.RevertDynamicStateChange(result2, changeToRevert1.Object);
            HardCodedStateCreator.CheckTestDynamicState(result2.DynamicState);
            HardCodedStateCreator.CheckTestStaticState(result2.StaticState);

            // Revert all the changes, which should result in an empty dynamic state once again.
            // Note we reverted in a different order than applied, valid here since none of these 3 changes have any 'before facts' (they only applied, not removed)
            result = worldUpdator.RevertDynamicStateChangesSequentially(result, change2.Object, change1.Object, change4.Object, change3.Object);

            HardCodedStateCreator.CheckTestStaticState(result.StaticState);      // Checks Static state.
            for (int i = 0; i < originalWorld.NumberOfNodes; i++) {
                Assert.IsTrue(DynamicStateInternalReader.GetNodeFact(i, result.DynamicState).Count == 0);    // Checks that no Facts exist.
                Assert.IsTrue(DynamicStateInternalReader.GetNodeEffectSum(i, result.DynamicState).Count == 0);   // Checks that no Effects exist.
            }
        }
    }
}
