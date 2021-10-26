using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Omiverse : Holon, IOmiverse
    {
        public IGreatGrandSuperStar GreatGrandSuperStar { get; set; } = new GreatGrandSuperStar(); //let's you jump between Multiverses/Dimensions.
        public IOmniverseDimensions Dimensions { get; set; } = new OmniverseDimensions();
        public List<IMultiverse> Multiverses { get; set; } = new List<IMultiverse>();

        public Omiverse()
        {
            this.HolonType = API.Core.Enums.HolonType.Omiverse;
        }
    }
}