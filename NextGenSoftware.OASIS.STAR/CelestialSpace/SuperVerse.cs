using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SuperVerse : Holon, ISuperVerse
    {
        public List<IUniverse> Universes { get; set; } = new List<IUniverse>();
    }
}