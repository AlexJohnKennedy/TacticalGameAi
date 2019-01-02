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
            // Arrange
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

        [Test]
        public void FactAdder_AddFactToANodeWhenNodeHasNoFactOfThatType_SuccessfullyAddsFact() {
            // Arrange some mock IEffectAdders. The IEffectAdders expect to be called ONCE and only ONCE, with the correct node id that the fact is being added to,
            // and the correct World object (which also must be mocked).
            int NODE = 3;
            int VALUE = 2;
            Mock<IEffectAdder> e1 = new Mock<IEffectAdder>();
            Mock<IEffectAdder> e2 = new Mock<IEffectAdder>();
            Fact.MutableFact existingFact = new Fact.MutableFact(FactType.EnemyPresence, 1, null);
            WorldRepresentation fakeWorld = WorldRep_ValueObjectMocker.NewWorldRepresentationMock();
            Dictionary<FactType, Fact.MutableFact> toPopulate = new Dictionary<FactType, Fact.MutableFact> { { FactType.EnemyPresence, existingFact } };   // This should become populated by the fact adder

            FactAdder toTest = new FactAdder(FactType.FriendlyPresence, new IEffectAdder[] { e1.Object, e2.Object });

            // ACT
            toTest.AddFact(fakeWorld, NODE, VALUE, toPopulate);

            // ASSERT
            Assert.IsTrue(existingFact.Value == 1);    // Verify that the fact was not mutated.
            Assert.IsTrue(existingFact.FactType == FactType.EnemyPresence);

            // assert that other facttype was added correctly
            foreach (FactType t in Enum.GetValues(typeof(FactType))) {
                if (t == FactType.FriendlyPresence) {
                    Assert.IsTrue(toPopulate.ContainsKey(t));
                    Assert.IsTrue(toPopulate[t].Value == VALUE);
                }
                else if (t == FactType.EnemyPresence) {
                    Assert.IsTrue(toPopulate.ContainsKey(t));
                    Assert.IsTrue(toPopulate[t].Value == 1);
                }
                else Assert.IsFalse(toPopulate.ContainsKey(t));
            }

            // verify each adder was called once, with the correct node id and world object
            e1.Verify(e => e.AddEffects(fakeWorld, NODE, It.IsAny<Fact.MutableFact>()), Times.Once());
            e2.Verify(e => e.AddEffects(fakeWorld, NODE, It.IsAny<Fact.MutableFact>()), Times.Once());
        }
    }
}
