using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of the Omiverse (creates Universes (with a GrandSuperStar at the centre of each).
    public interface IGreatGrandSuperStarCore : ICelestialBodyCore
    {
        IGreatGrandSuperStar GreatGrandSuperStar { get; set; }
       // Task<OASISResult<IGrandSuperStar>> AddGrandSuperStarAsync(IGrandSuperStar grandSuperStar);
        Task<OASISResult<IUniverse>> AddUniverseAsync(IUniverse universe);
        OASISResult<IUniverse> AddUniverse(IUniverse universe);
        Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IUniverse>> GetAllUniversesForOmiverse(bool refresh = true);
        OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForOmiverseAsync(bool refresh = true);
        Task<OASISResult<IEnumerable<IGrandSuperStar>>> GetAllGrandSuperStarsForOmiverseAsync(bool refresh = true); //Helper method which gets the GrandSuperStars at the centre of each Universe.
        OASISResult<IEnumerable<IGrandSuperStar>> GetAllGrandSuperStarsForOmiverse(bool refresh = true); //Helper method which gets the GrandSuperStars at the centre of each Universe.
        Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForOmiverseAsync(bool refresh = true); //Helper method which gets the SuperStars at the centre of each Galaxy.
        OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForOmiverse(bool refresh = true); //Helper method which gets the SuperStars at the centre of each Galaxy.
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForOmiverseAsync(bool refresh = true); //Helper method which gets the Stars at the centre of each Soloar System.
        OASISResult<IEnumerable<IStar>> GetAllStarsForOmiverse(bool refresh = true); //Helper method which gets the Stars at the centre of each Soloar System..
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForOmiverseAsync(bool refresh = true); //Helper method which gets the Planets within every Solar System.
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForOmiverse(bool refresh = true); //Helper method which gets the Planets within every Solar System.
        Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForOmiverseAsync(bool refresh = true); //Helper method which gets the Moons around every planet.
        OASISResult<IEnumerable<IMoon>> GetAllMoonsForOmiverse(bool refresh = true); //Helper method which gets the Moons around every planet.
    }
}