using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.Interfaces
{
    public interface IStar : ICelestialBody
    {
        List<IPlanet> Planets { get; set; }
    }
}