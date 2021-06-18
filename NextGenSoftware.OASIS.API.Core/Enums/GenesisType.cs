
namespace NextGenSoftware.OASIS.API.Core.Enums
{
    public enum GenesisType
    {
        GreatGrandSuperStar, // Built-in and there will only ever be ONE of this within the OASIS. (Omiverse)
        GrandSuperStar, //level 166 (max level for avatar?) ( Universe)
        SuperStar, //level 133 (Galaxy)
        Star, //Level 99
        Planet, //Level 33
        Moon, //Level 1
        //CelestialBody // Should never need to use this one (is a fall-back default if none of the above are used).
    }
}
