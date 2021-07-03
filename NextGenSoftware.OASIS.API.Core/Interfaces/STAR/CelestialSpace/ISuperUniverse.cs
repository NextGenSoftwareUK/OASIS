using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISuperUniverse : ICelestialSpace
    {
        List<IUniverse> Universes { get; set; }
    }
}