using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each SolarSystem (creates Planets)
    public interface IStarCore : ICelestialBodyCore
    {
        IStar Star { get; set; }
        Task<OASISResult<IPlanet>> AddPlanetAsync(IPlanet planet);
        OASISResult<IPlanet> AddPlanet(IPlanet planet);
        Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetPlanets(bool refresh = true);
    }
}