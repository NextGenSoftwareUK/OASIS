using System.Collections.Generic;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IOmiverse
    {
        //public IGreatGrandSuperStar IGreatGrandSuperStar { get; set; }
        List<IUniverse> Universes { get; set; }
    }
}