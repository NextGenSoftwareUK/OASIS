
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IStar : ICelestialBody
    {
        List<IPlanet> Planets { get; set; }
    }
}