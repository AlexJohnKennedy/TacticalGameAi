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
            HardCodedStateCreator.CheckTestStaticState(originalWorld.StaticState);
        }
    }
}
