using NUnit.Framework;
using Moq;
using System;
using System.Reflection;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem;
using TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using System.Collections.Generic;

namespace TacticalGameAiIntegrationTests.DecisionLayer.WorldRepresentationSystem {

    [TestFixture]
    class DefaultWorldUpdatorTest {
        [Test]
        public void WorldUpdatorSystemAndWorldRepresentationSystem_CanCorrectlyBuildDynamicStateByApplyingStateChanges() {
            // Create the default World Update using the default (hard coded) WorldUpdatorFactory.
            WorldUpdator worldUpdator = new HardCodedDefaultWorldUpdatorBuilder().BuildWorldUpdator();

            // Create the testable WorldRepresentation with pre-defined static state, and with blank DynamicState and Interpretation
            WorldRepresentation originalWorld = new WorldRepresentation(HardCodedStateCreator.CreateTestStaticState());

            // Ensure that everything was created correctly.
            HardCodedStateCreator.CheckTestStaticState(originalWorld.StaticState);      // Checks Static state.
            for (int i=0; i < originalWorld.NumberOfNodes; i++) {
                Assert.IsTrue(DynamicStateInternalReader.GetNodeFact(i, originalWorld.DynamicState).Count == 0);    // Checks that no Facts exist.
                Assert.IsTrue(DynamicStateInternalReader.GetNodeEffectSum(i, originalWorld.DynamicState).Count == 0);   // Checks that no Effects exist.
            }

            // Mock DynamicStateChange: spotting 2 friendlies at node 0
            Mock<IDynamicStateChange> change1 = new Mock<IDynamicStateChange>();
            change1.Setup(c => c.AffectedNodes).Returns(new int[] { 0 });
            Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> beforeFacts = new Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> {
                { 0, new KeyValuePair<FactType, int>[] { } },
            };
            change1.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> afterFacts = new Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> {
                { 0, new KeyValuePair<FactType, int>[] { new KeyValuePair<FactType, int>(FactType.FriendlyPresence, 2)} },
            };
            change1.Setup(c => c.GetFactsAfter()).Returns(afterFacts);

            // Mock DynamicStateChange: spotting 1 friendly at node 4 and DangerfromunknownSource at node 5
            Mock<IDynamicStateChange> change2 = new Mock<IDynamicStateChange>();
            change2.Setup(c => c.AffectedNodes).Returns(new int[] { 4, 5 });
            beforeFacts = new Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> {
                { 4, new KeyValuePair<FactType, int>[] { } },
                { 5, new KeyValuePair<FactType, int>[] { } },
            };
            change2.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            afterFacts = new Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> {
                { 4, new KeyValuePair<FactType, int>[] { new KeyValuePair<FactType, int>(FactType.FriendlyPresence, 1)} },
                { 5, new KeyValuePair<FactType, int>[] { new KeyValuePair<FactType, int>(FactType.DangerFromUnknownSource, 5)} },
            };
            change2.Setup(c => c.GetFactsAfter()).Returns(afterFacts);

            // Mock DynamicStateChange: spotting 3 enemies at node 2
            Mock<IDynamicStateChange> change3 = new Mock<IDynamicStateChange>();
            change3.Setup(c => c.AffectedNodes).Returns(new int[] { 2 });
            beforeFacts = new Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> {
                { 2, new KeyValuePair<FactType, int>[] { } },
            };
            change3.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            afterFacts = new Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> {
                { 2, new KeyValuePair<FactType, int>[] { new KeyValuePair<FactType, int>(FactType.EnemyPresence, 3)} },
            };
            change3.Setup(c => c.GetFactsAfter()).Returns(afterFacts);


            // --------------------------
            // ACT: APPLY ALL THE CHANGES
            // --------------------------

            WorldRepresentation changed1 = worldUpdator.ApplyDynamicStateChange(originalWorld, change1.Object);
            WorldRepresentation changed2 = worldUpdator.ApplyDynamicStateChange(changed1, change2.Object);
            WorldRepresentation changed3 = worldUpdator.ApplyDynamicStateChange(changed2, change3.Object);

            // -----------------------------------------------------------
            // ASSERT: Final result should match our Default dynamic state
            // -----------------------------------------------------------

            HardCodedStateCreator.CheckTestStaticState(changed3.StaticState);
            HardCodedStateCreator.CheckTestDynamicState(changed3.DynamicState);
        }
    }
}
