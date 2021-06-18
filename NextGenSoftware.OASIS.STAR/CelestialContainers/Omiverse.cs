using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialContainers
{
    public class Omiverse : Holon, IOmiverse
    {
        public IGreatGrandSuperStar GreatGrandSuperStar { get; set; }
        public List<IUniverse> Universes { get; set; }
    }
}