using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISolarSystem : ICelestialSpace
    {
        IStar Star { get; set; }
        List<IPlanet> Planets { get; set; }
    }
}