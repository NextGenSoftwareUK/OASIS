
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IPlanet : ICelestialBody
    {
        public List<IMoon> Moons { get; set; }
    }
}