using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each Galaxy (creates SolarSystems & Stars) Creator
    public interface ISuperStarCore : ICelestialBodyCore
    {
        ISuperStar SuperStar { get; set; }

        //OASISResult<IGalaxy> AddGalaxy(IGalaxy solarSystem);
        //Task<OASISResult<IGalaxy>> AddGalaxyAsync(IGalaxy galaxy);
        OASISResult<IStar> AddStar(IStar star);
        Task<OASISResult<IStar>> AddStarAsync(IStar star);
        OASISResult<IPlanet> AddPlanet(IPlanet planet);
        Task<OASISResult<IPlanet>> AddPlanetAsync(IPlanet planet);
        OASISResult<IAsteroid> AddAsteroid(IAsteroid asteroid);
        Task<OASISResult<IAsteroid>> AddAsteroidAsync(IAsteroid asteroid);
        OASISResult<IComet> AddComet(IComet comet);
        Task<OASISResult<IComet>> AddCometAsync(IComet comet);
        OASISResult<IMeteroid> AddMeteroid(IMeteroid meteroid);
        Task<OASISResult<IMeteroid>> AddMeteroidAsync(IMeteroid meteroid);
        OASISResult<INebula> AddNebula(INebula nebula);
        Task<OASISResult<INebula>> AddNebulaAsync(INebula nebula);
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForGalaxy(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForGalaxyAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetAllStarsForGalaxy(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForGalaxyAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForGalaxy(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForGalaxyAsync(bool refresh = true);
        OASISResult<IEnumerable<IMoon>> GetAllMoonsForGalaxy(bool refresh = true);
        Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForGalaxyAsync(bool refresh = true);
    }
}