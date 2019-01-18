using NUnit.Framework;
using Moq;
using System;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ContactPointCalculationSystem;
using System.Collections.Generic;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects {

    [TestFixture]
    public class ContactPointCalculatorImplementationTests {

        [Test]
        public void DefaultContactPointCalculator_RunOnHardCodedStaticState_ReturnsCorrectGroups() {
            StaticState s = HardCodedStateCreator.CreateTestStaticState();
            var reader = s.ContactPointGroupsReader();

            IContactPointCalculator calculator = new DefaultContactPointCalculator();
            
            CollectionAssert.AreEquivalent(new HashSet<HashSet<int>> {
                new HashSet<int> { 1 },
                new HashSet<int> { 3 }
            }, reader(0));

            CollectionAssert.AreEquivalent(new HashSet<HashSet<int>> {
                new HashSet<int> { 5 },
                new HashSet<int> { 0 }
            }, reader(1));

            CollectionAssert.AreEquivalent(new HashSet<HashSet<int>> {
                new HashSet<int> { 1 },
                new HashSet<int> { 5 }
            }, reader(2));

            CollectionAssert.AreEquivalent(new HashSet<HashSet<int>> {
                new HashSet<int> { 6 }
            }, reader(8));

            CollectionAssert.AreEquivalent(new HashSet<HashSet<int>> {
                new HashSet<int> { 5 }
            }, reader(6));
        }

    }
}
