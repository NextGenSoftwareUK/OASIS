using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.Interfaces
{
    public interface IPlanet : ICelestialBody, OASIS.API.Core.Interfaces.IPlanet
    {
        public List<IMoon> Moons { get; set; }
    }
}