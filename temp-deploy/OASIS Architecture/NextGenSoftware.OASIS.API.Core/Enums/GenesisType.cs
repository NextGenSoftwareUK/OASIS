
namespace NextGenSoftware.OASIS.API.Core.Enums
{
    //TODO: New thinking is that a OAPP can ONLY ever be a CelestialBody, because it needs to be a physical object that the rest of the eco-system orbits around.
    public enum GenesisType
    {
        //GreatGrandSuperStar, // Built-in and there will only ever be ONE of this within the OASIS. (Omniverse)
       // GrandSuperStar, //level 166 (max level for avatar?) ( Universe)
       // SuperStar, //level 133 (Galaxy) Cannot create a SuperStar on its own, you create a Galaxy which comes with a new SuperStar at the centre.
        ZomesAndHolonsOnly,
        Moon, //Level 1
        Planet, //Level 33
        Star, //Level 99        
        SuperStar, // level 133 (Galaxy)
        GrandSuperStar, // level 166  (max level for avatar?) ( Universe)
        //SoloarSystem, //Level 99        
        //Galaxy, //level 133 (Galaxy)
        //Universe //level 166 (max level for avatar?) ( Universe)
        //CelestialBody // Should never need to use this one (is a fall-back default if none of the above are used).
    }
}