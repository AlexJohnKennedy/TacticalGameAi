using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// All Value Objects are IMMUTABLE objects and are primarily used as Data Structures which pass information between functional logic components.
/// 
/// The Value Objects in the WorldRepresentationSystem namespace are the fundamental representations of the world information.
/// 
/// Almost all the Logic Modules in the DecisionLayer will use WorldRepresentation Data to perform their calculations, since these structures
/// define the 'pre-baked' information about the game world. Note that most of them will not access this directly, but instead process
/// Adapted versions which are built by volatile adaptor components in each Module's respective anti-corruption layer. 
/// 
/// This namespace should have NO dependencies on other parts of the system because it is the most basic piece of information 
/// from which all the logic is conducted.
/// </summary>
namespace TacticalGameAi.DecisionLayer.WorldRepresentationSystem.ValueObjects {

    public class WorldRepresentation {
        public StaticState StaticState { get; }
        public DynamicState DynamicState { get; }
        public Interpretation Interpretation { get; }

        // Constructors
        public WorldRepresentation(StaticState staticState, DynamicState dynamicState = null, Interpretation interpretation = null) {
            StaticState = staticState;
            DynamicState = dynamicState;
            Interpretation = interpretation;
        }
        private WorldRepresentation(WorldRepresentation other, StaticState staticState, DynamicState dynamicState, Interpretation interpretation) {
            StaticState = staticState ?? other.StaticState;
            DynamicState = dynamicState ?? other.DynamicState;
            Interpretation = interpretation ?? other.Interpretation;
        }
        public WorldRepresentation(WorldRepresentation other, DynamicState dynamicState, Interpretation interpretation) : this(other, null, dynamicState, interpretation) { }
        public WorldRepresentation(WorldRepresentation other, Interpretation interpretation) : this(other, null, null, interpretation) { }


    }
}
