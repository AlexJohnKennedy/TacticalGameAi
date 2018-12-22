using NUnit.Framework;
using Moq;
using System;
using System.Reflection;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects {

    [TestFixture]
    class WorldRepresentationTests {

        // Dummy arrays needed to satisfy the StaticState constructor, so that we can simply just mock them.
        private StaticState.AreaNode[] arr = new StaticState.AreaNode[1];
        private StaticState.AreaEdge[,] arr2 = new StaticState.AreaEdge[1,1];

        //[Test]
        public void Contructor_ReceivesThreeComponents_ReturnsSameInstances() {
            // Arrange
            Mock<StaticState> s = new Mock<StaticState>(arr,arr2);
            Mock<DynamicState> d = new Mock<DynamicState>();
            Mock<Interpretation> i = new Mock<Interpretation>();

            // Act
            WorldRepresentation rep1 = new WorldRepresentation(s.Object, d.Object, i.Object);

            // Assert
            Assert.AreEqual(rep1.StaticState, s.Object);
            Assert.AreEqual(rep1.DynamicState, d.Object);
            Assert.AreEqual(rep1.Interpretation, i.Object);
        }

        //[Test]
        public void Contructor_ReceivesNoInterpretation_ReturnsSameInstances() {
            // Arrange
            Mock<StaticState> s = new Mock<StaticState>(arr,arr2);
            Mock<DynamicState> d = new Mock<DynamicState>();

            // Act
            WorldRepresentation rep1 = new WorldRepresentation(s.Object, d.Object);

            // Assert
            Assert.AreEqual(rep1.StaticState, s.Object);
            Assert.AreEqual(rep1.DynamicState, d.Object);
            Assert.AreEqual(rep1.Interpretation, null);
        }

        //[Test]
        public void Contructor_ReceivesOnlyStaticState_ReturnsSameInstances() {
            // Arrange
            Mock<StaticState> s = new Mock<StaticState>(arr,arr2);

            // Act
            WorldRepresentation rep1 = new WorldRepresentation(s.Object);

            // Assert
            Assert.AreEqual(rep1.StaticState, s.Object);
            Assert.AreEqual(rep1.DynamicState, null);
            Assert.AreEqual(rep1.Interpretation, null);
        }

        //[Test]
        public void Contructor_ReceivesInterpretationButNoDynamicState_ThrowsException() {
            // Arrange
            Mock<StaticState> s = new Mock<StaticState>(arr,arr2);
            Mock<Interpretation> i = new Mock<Interpretation>();

            // Assert - Exception is Expected
            ArgumentNullException e = Assert.Throws<ArgumentNullException>(() => new WorldRepresentation(s.Object, null, i.Object));
        }

        //[Test]
        public void Contructor_ReceivesNoStaticState_ThrowsException() {
            Assert.Throws<ArgumentNullException>(() => new WorldRepresentation(null));
        }

        //[Test]
        public void Contructor_ReceivesNoStaticStateButDoesReceiveDynamicState_ThrowsException() {
            // Arrange
            Mock<DynamicState> d = new Mock<DynamicState>();
            Mock<Interpretation> i = new Mock<Interpretation>();

            // Act
            Assert.Throws<ArgumentNullException>(() => new WorldRepresentation(null, d.Object));

            // Assert - Exception is Expected
        }

        //[Test]
        public void Contructor_CopiesOtherWorldRepresentation_CorrectlyCopiesAllMembers() {
            // Arrange
            Mock<StaticState> s = new Mock<StaticState>(arr,arr2);
            Mock<DynamicState> d = new Mock<DynamicState>();
            Mock<DynamicState> d2 = new Mock<DynamicState>();
            Mock<Interpretation> i = new Mock<Interpretation>();
            Mock<Interpretation> i2 = new Mock<Interpretation>();

            // Act
            WorldRepresentation rep1 = new WorldRepresentation(s.Object, d.Object, i.Object);
            WorldRepresentation rep2 = new WorldRepresentation(rep1, d2.Object, i2.Object);

            // Assert
            Assert.AreEqual(s.Object, rep2.StaticState);
            Assert.AreEqual(d2.Object, rep2.DynamicState);
            Assert.AreEqual(i2.Object, rep2.Interpretation);
        }
    }
}