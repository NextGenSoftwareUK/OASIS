using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each Galaxy (creates Galaxies, SolarSystems & Stars) Creator
    public interface ISuperStarCore : ICelestialBodyCore
    {
        ISuperStar SuperStar { get; set; }
        Task<OASISResult<IStar>> AddStarAsync(IStar star);
        OASISResult<IStar> AddStar(IStar star);
        Task<OASISResult<ISolarSystem>> AddSolarSystemAsync(ISolarSystem solarSystem);
        OASISResult<ISolarSystem> AddSolarSystem(ISolarSystem solarSystem);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForGalaxyAsync(bool refresh = true); //Helper method which gets the Stars at the centre of each SolarSystem.
        OASISResult<IEnumerable<IStar>> GetAllStarsForGalaxy(bool refresh = true); //Helper method which gets the Stars at the centre of each SolarSystem.
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForGalaxyAsync(bool refresh = true); //Helper method which gets all Solar Systems.
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForGalaxy(bool refresh = true); //Helper method which gets all Solar Systems.
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForGalaxyAsync(bool refresh = true); //Helper method which gets the Planets within every Solar System.
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForGalaxy(bool refresh = true); //Helper method which gets the Planets within every Solar System.
        Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForGalaxyAsync(bool refresh = true); //Helper method which gets the Moons around every planet.
        OASISResult<IEnumerable<IMoon>> GetAllMoonsForGalaxy(bool refresh = true); //Helper method which gets the Moons around every planet.
    }
}