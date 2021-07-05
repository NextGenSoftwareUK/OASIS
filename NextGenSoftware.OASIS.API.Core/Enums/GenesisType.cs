
namespace NextGenSoftware.OASIS.API.Core.Enums
{
    public enum GenesisType
    {
        //GreatGrandSuperStar, // Built-in and there will only ever be ONE of this within the OASIS. (Omiverse)
       // GrandSuperStar, //level 166 (max level for avatar?) ( Universe)
       // SuperStar, //level 133 (Galaxy) Cannot create a SuperStar on its own, you create a Galaxy which comes with a new SuperStar at the centre.
        Moon, //Level 1
        Planet, //Level 33
        Star, //Level 99        
        SoloarSystem, //Level 99        
        Galaxy, //level 133 (Galaxy)
        Universe //level 166 (max level for avatar?) ( Universe)
        //CelestialBody // Should never need to use this one (is a fall-back default if none of the above are used).
    }
}