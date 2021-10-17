using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISuperVerse : ICelestialSpace
    {
        List<IUniverse> Universes { get; set; }
    }
}