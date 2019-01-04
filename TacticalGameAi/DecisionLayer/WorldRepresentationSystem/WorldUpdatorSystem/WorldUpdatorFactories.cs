﻿using System;
using System.Collections.Generic;
using System.Text;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.DynamicStateHiddenTypes;
using TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem.EffectAdderImplementations;

namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.WorldUpdatorSystem {
    /* This interface represents some object which is responsible for constructing a given WorldUpdator.
     * Since a WorldUpdator is a completely generic and composable object, the factory is therefore the
     * object which defines the concrete logic a given WorldUpdator will actually use (By instantiating
     * concrete implementations of FactAdders, EffectAdders, and mapping them to FactTypes and EffectTypes,
     * and then handing them to a implementation-independent WorldUpdator which acts as a controller.) */
    public interface IWorldUpdatorFactory {
        WorldUpdator BuildWorldUpdator();
    }

    /* This class simply creates a basic world updator containing FactAdder and EffectAdder components
     * which will be considered the 'default' world updator for now. Later we can implement meta-factories
     * and more customised WorldUpdator objects */
    public class HardCodedDefaultWorldUpdatorBuilder : IWorldUpdatorFactory {
        public WorldUpdator BuildWorldUpdator() {
            Dictionary<FactType, IFactAdder> factAdderDict = new Dictionary<FactType, IFactAdder>();

            // Construct a FactAdder for each FactType, and create the appropriate EffectAdders for each FactAdder.
            factAdderDict.Add(FactType.FriendlyPresence, new FactAdder(FactType.FriendlyPresence,
                    new IEffectAdder[] {
                        new VisibilityBasedEffectAdder(EffectType.Clear),
                        new ControlBasedEffectAdder(EffectType.Controlled)
                    }
                )
            );
            factAdderDict.Add(FactType.EnemyPresence, new FactAdder(FactType.EnemyPresence,
                    new IEffectAdder[] {
                        new VisibilityBasedEffectAdder(EffectType.VisibleToEnemies),
                        new ControlBasedEffectAdder(EffectType.ControlledByEnemy)
                    }
                )
            );
            factAdderDict.Add(FactType.Danger, new FactAdder(FactType.Danger,
                    new IEffectAdder[] {
                    }
                )
            );
            factAdderDict.Add(FactType.DangerFromUnknownSource, new FactAdder(FactType.DangerFromUnknownSource,
                    new IEffectAdder[] {
                        new CanBeSeenByBasedEffectAdder(EffectType.PotentialEnemies)
                    }
                )
            );

            return new WorldUpdator(factAdderDict);
        }
    }
}