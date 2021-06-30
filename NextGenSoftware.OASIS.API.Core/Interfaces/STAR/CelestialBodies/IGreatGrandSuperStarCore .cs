using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of the Omiverse (there can only be ONE) ;-) (creates Omiverses (with a GrandSuperStar at the centre of each).  Spirit/God/The Divine, etc
    public interface IGreatGrandSuperStarCore : ICelestialBodyCore
    {
        IGreatGrandSuperStar GreatGrandSuperStar { get; set; }
        // Task<OASISResult<IGrandSuperStar>> AddGrandSuperStarAsync(IGrandSuperStar grandSuperStar);

        Task<OASISResult<IOmiverse>> AddOmiverseAsync(IOmiverse omiverse);
        OASISResult<IOmiverse> AddOmiverse(IOmiverse omiverse);
        Task<OASISResult<IMultiverse>> AddMultiverseAsync(IMultiverse multiverse);
        OASISResult<IMultiverse> AddMultiverse(IMultiverse multiverse);
        Task<OASISResult<IEnumerable<IMultiverse>>> GetAllMultiversesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IMultiverse>> GetAllMultiversesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IUniverse>> GetAllUniversesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IDimension>> GetAllDimensionsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IGalaxyCluster>> GetAllGalaxyClustersForOmiverse(bool refresh = true);
        OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForOmiverseAsync(bool refresh = true);
        Task<OASISResult<IEnumerable<IGrandSuperStar>>> GetAllGrandSuperStarsForOmiverseAsync(bool refresh = true); //Helper method which gets the GrandSuperStars at the centre of each Universe.
        OASISResult<IEnumerable<IGrandSuperStar>> GetAllGrandSuperStarsForOmiverse(bool refresh = true); //Helper method which gets the GrandSuperStars at the centre of each Universe.
        Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForOmiverseAsync(bool refresh = true); //Helper method which gets the SuperStars at the centre of each Galaxy.
        OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForOmiverse(bool refresh = true); //Helper method which gets the SuperStars at the centre of each Galaxy.
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxiesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetAllStarsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxiesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxyClustersForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxiesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true);
        Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForOmiverseAsync(bool refresh = true); //Helper method which gets the Moons around every planet.
        OASISResult<IEnumerable<IMoon>> GetAllMoonsForOmiverse(bool refresh = true); //Helper method which gets the Moons around every planet.
    }
}