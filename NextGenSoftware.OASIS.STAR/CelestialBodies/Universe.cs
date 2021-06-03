using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Universe : IUniverse
    {
        public IGrandSuperStar GrandSuperStar { get; set; }
        public List<IGalaxy> Galaxies { get; set; }
    }
}