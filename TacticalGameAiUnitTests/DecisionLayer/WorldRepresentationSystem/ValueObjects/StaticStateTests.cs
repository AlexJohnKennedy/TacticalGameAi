using NUnit.Framework;
using Moq;
using System;
using System.Reflection;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects {
    [TestFixture]
    class StaticStateTests {

        [Test]
        public void StaticState_TestingDataReads_CorrectValues() {
            // Arrange the static state object to test.
            StaticState s = HardCodedStateCreator.CreateTestStaticState();

            // Test that it's working correctly.
            HardCodedStateCreator.CheckTestStaticState(s);
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
