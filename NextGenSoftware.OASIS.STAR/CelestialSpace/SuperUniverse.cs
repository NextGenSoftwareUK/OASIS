using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SuperUniverse : Holon, ISuperUniverse
    {
       public List<IUniverse> Universes { get; set; }
    }
}