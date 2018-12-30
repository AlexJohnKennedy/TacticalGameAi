using System;
using System.Collections.Generic;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using Moq;
using NUnit.Framework;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem {
    [TestFixture]
    class FactAdderTests {
        [Test]
        public void FactAdder_AddFactToANodeWhenNodeAlreadyHasFactOfSameType_SuccessfullyAddsFactTypeValue() {
            // Arrange some mock IEffectAdders. The IEffectAdders expect to be called ONCE and only ONCE, with the correct node id that the fact is being added to,
            // and the correct World object (which also must be mocked).
            int NODE = 3;
            int VALUE = 2;

            Fact.MutableFact existingFact = new Fact.MutableFact(FactType.EnemyPresence, 1, null);

            WorldRepresentation fakeWorld = WorldRep_ValueObjectMocker.NewWorldRepresentationMock();
            Dictionary<FactType, Fact.MutableFact> toPopulate = new Dictionary<FactType, Fact.MutableFact> { { FactType.EnemyPresence, existingFact } };   // This should become populated by the fact adder!

            FactAdder toTest = new FactAdder(FactType.EnemyPresence, new IEffectAdder[] { });

            // ACT
            toTest.AddFact(fakeWorld, NODE, VALUE, toPopulate);

            // ASSERT
            Assert.IsTrue(existingFact.Value == 2);    // Verify that the fact was mutated with the correct value.
            // assert that no other facttypes were added
            foreach (FactType t in Enum.GetValues(typeof(FactType))) {
                if (t == FactType.EnemyPresence) Assert.IsTrue(toPopulate.ContainsKey(t));
                else Assert.IsFalse(toPopulate.ContainsKey(t));
            }
        }
    }
}
