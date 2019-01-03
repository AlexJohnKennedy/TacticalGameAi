using System;
using System.Collections.Generic;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.ValueObjects;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace TacticalGameAiUnitTests.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem {
    public static class DynamicStateChangeMocker {
        public static Mock<IDynamicStateChange> Create(string affectedNodesJson, string beforeFactsJson, string afterFactsJson) {
            Mock<IDynamicStateChange> change = new Mock<IDynamicStateChange>();

            // Setup affected nodes.
            var affectedNodes = JsonConvert.DeserializeObject<IEnumerable<int>>(affectedNodesJson);
            change.Setup(c => c.AffectedNodes).Returns(affectedNodes);

            var beforeFacts = JsonConvert.DeserializeObject<Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>>>(beforeFactsJson);
            var afterFacts = JsonConvert.DeserializeObject<Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>>>(afterFactsJson);
            change.Setup(c => c.GetFactsBefore()).Returns(beforeFacts);
            change.Setup(c => c.GetFactsAfter()).Returns(afterFacts);

            // TODO 6 - MAKE DYNAMICSATECHANGEMOCKER able to parse JSON representing a state change to testing is faster!

            /*
            Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> beforeFacts = new Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> {
                // No facts are removed or updated in node 2 by this change.
                { 2, new KeyValuePair<FactType, int>[] { } },

                // Node 4 has the friendlies fact removed by this change. Thus it must appear in the 'before' set so that this change is reversible.
                { 4, new KeyValuePair<FactType, int>[] { new KeyValuePair<FactType, int>(FactType.FriendlyPresence, 1)} },

                // No facts are removed or updated in node 5 by this change.
                { 5, new KeyValuePair<FactType, int>[] { } }
            };*/
            /*
            Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> afterFacts = new Dictionary<int, IEnumerable<KeyValuePair<FactType, int>>> {
                // Enemies were spotted in node 2 during this event
                { 2, new KeyValuePair<FactType, int>[] { new KeyValuePair<FactType, int>(FactType.EnemyPresence, 2)} },

                // Node 4 has danger applied to it with a super high rating since friendlies were wiped out!
                { 4, new KeyValuePair<FactType, int>[] { new KeyValuePair<FactType, int>(FactType.Danger, 7)} },

                // Node 5 also had danger applied to it!
                { 5, new KeyValuePair<FactType, int>[] { new KeyValuePair<FactType, int>(FactType.Danger, 2)} }
            };*/

            return change;
        }
    }
}
