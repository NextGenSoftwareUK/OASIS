using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each Universe (creates Galaxies (with a SuperStar at the centre of each).
    public interface IGrandSuperStarCore : ICelestialBodyCore
    {
        IGrandSuperStar GrandSuperStar { get; set; }

        OASISResult<IGalaxy> AddGalaxy(IGalaxy solarSystem);
        Task<OASISResult<IGalaxy>> AddGalaxyAsync(IGalaxy galaxy);
        OASISResult<IStar> AddStar(IStar star); //TODO: Not sure if we should allow them to add stars to a Universe outside of a Glaxy? In real-life they exist so maybe ok?
        Task<OASISResult<IStar>> AddStarAsync(IStar star); //TODO: Not sure if we should allow them to add stars to a Universe outside of a Glaxy? In real-life they exist so maybe ok?
        OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForUniverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForUniverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForUniverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForUniverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForUniverse(bool refresh = true); //Helper method which gets the SuperStars at the centre of each Galaxy.
        Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForUniverseAsync(bool refresh = true); //Helper method which gets the SuperStars at the centre of each Galaxy.
        OASISResult<IEnumerable<IStar>> GetAllStarsForUniverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForUniverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetStarsOutSideOfGalaxies(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetStarsOutSideOfGalaxiesAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForUniverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForUniverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IMoon>> GetAllMoonsForUniverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForUniverseAsync(bool refresh = true);
    }
}