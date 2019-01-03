using NUnit.Framework;
using Moq;
using System;
using System.Reflection;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects {

    public static class WorldRep_ValueObjectMocker {
        public static Mock<StaticState> NewStaticStateMock() {
            var n = new StaticState.AreaNode[100];
            var e = new StaticState.AreaEdge[100, 100];
            for (int i=0; i<100; i++) {
                n[i] = new StaticState.AreaNode(i, i, 0, 0, false, 0, 0, false, false, false);
                for (int j=0; j<100; j++) {
                    e[i, j] = new StaticState.AreaEdge(i, j, false, false, 0, 0, 0, 0, false);
                }
            }
            return new Mock<StaticState>(n, e);
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