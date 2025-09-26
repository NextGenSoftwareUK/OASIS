using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each Solar System (creates Solar Systems, Planets & Moons).
    public interface IStarCore : ICelestialBodyCore
    {
        IStar Star { get; set; }
        Task<OASISResult<ISolarSystem>> AddSolarSystemAsync(ISolarSystem solarSystem);
        OASISResult<ISolarSystem> AddSolarSystem(ISolarSystem solarSystem);
        Task<OASISResult<IPlanet>> AddPlanetAsync(IPlanet planet);
        OASISResult<IPlanet> AddPlanet(IPlanet planet);
        Task<OASISResult<IMoon>> AddMoonAsync(IPlanet parentPlanet, IMoon moon);
        OASISResult<IMoon> AddMoon(IPlanet parentPlanet, IMoon moon);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForSolarSystemAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForSolarSystem(bool refresh = true);
        Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForSolarSystemAsync(bool refresh = true);
        OASISResult<IEnumerable<IMoon>> GetAllMoonsForSolarSystem(bool refresh = true);
    }
}