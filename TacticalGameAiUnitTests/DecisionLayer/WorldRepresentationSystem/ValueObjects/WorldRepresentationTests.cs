using NUnit.Framework;
using Moq;
using System;
using System.Reflection;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects {

    public static class WorldRep_ValueObjectMocker {
        public static Mock<StaticState> NewStaticStateMock() {
            return new Mock<StaticState>(new StaticState.AreaNode[1000], new StaticState.AreaEdge[1000, 1000]);
        }
        public static WorldRepresentation NewWorldRepresentationMock() {
            return new WorldRepresentation(NewStaticStateMock().Object);
        }
    }

    [TestFixture]
    class WorldRepresentationTests {

        // Dummy arrays needed to satisfy the StaticState constructor, so that we can simply just mock them.
        private StaticState.AreaNode[] arr = new StaticState.AreaNode[1];
        private StaticState.AreaEdge[,] arr2 = new StaticState.AreaEdge[1, 1];

        public void Constructor_CreateWorldRepresentationWithEmptyDynamicStateAndEmptyInterpretation() {
            // Arrange
            Mock<StaticState> s = new Mock<StaticState>(arr,arr2);

            // Act
            WorldRepresentation rep1 = new WorldRepresentation(s.Object);

            // Assert
            Assert.AreEqual(rep1.StaticState, s.Object);
            Assert.IsTrue(rep1.DynamicState != null);       // Not allowed to be null! Instead, the world rep should have created a simple empty state.
            Assert.IsTrue(rep1.Interpretation != null);
        }
    }
}