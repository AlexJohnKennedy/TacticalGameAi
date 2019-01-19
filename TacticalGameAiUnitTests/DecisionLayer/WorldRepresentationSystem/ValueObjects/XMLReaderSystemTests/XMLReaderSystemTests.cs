using NUnit.Framework;
using Moq;
using System;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ContactPointCalculationSystem;
using System.Collections.Generic;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects.XMLReaderSystemTests {
    [TestFixture]
    public class XMLReaderSystemTests {
        // [Test] // Make this a live test to easily run the generator code and create a new XML File to test with!
        public void GenerateTestStaticStateUnderivedXML() {
            HardCodedStateCreator.WriteTestStaticStateToXML("./TestStaticStateUnderivedXML.xml");
        }
    }
}
