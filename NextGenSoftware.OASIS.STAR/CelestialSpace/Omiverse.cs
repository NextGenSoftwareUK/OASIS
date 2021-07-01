using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Omiverse : Holon, IOmiverse
    {
        public IGreatGrandSuperStar GreatGrandSuperStar { get; set; } //let's you jump between Multiverses/Dimensions.
        public IOmniverseDimensions Dimensions { get; set; }
        public List<IMultiverse> Multiverses { get; set; }
    }
}