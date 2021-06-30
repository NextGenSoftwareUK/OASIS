using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Multiverse : Holon, IMultiverse
    {
        public IGrandSuperStar GrandSuperStar { get; set; } //Lets you jump between universes within this multiverse.
        public List<IUniverse> Universes { get; set; }
    }
}