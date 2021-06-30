using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each Multiverse (creates dimensions, universes & GalaxyClusters). Prime Creator
    public interface IGrandSuperStarCore : ICelestialBodyCore
    {
        IGrandSuperStar GrandSuperStar { get; set; }
        OASISResult<IUniverse> AddUniverse(IUniverse universe);
        Task<OASISResult<IUniverse>> AddUniverseAsync(IUniverse universe);
        OASISResult<IDimension> AddDimensionToUniverse(IUniverse universe, IDimension dimension);
        Task<OASISResult<IDimension>> AddDimensionToUniverseAsync(IUniverse universe, IDimension dimension);
        OASISResult<IGalaxyCluster> AddGalaxyClusterToDimension(IDimension dimension, IGalaxyCluster galaxyCluster);
        Task<OASISResult<IGalaxyCluster>> AddGalaxyClusterToDimensionAsync(IDimension dimension, IGalaxyCluster galaxyCluster);
        OASISResult<ISolarSystem> AddSolarSystemToDimension(IDimension dimension, ISolarSystem solarSystem);
        Task<OASISResult<ISolarSystem>> AddSolarSystemToDimensionAsync(IDimension dimension, ISolarSystem solarSystem);
        OASISResult<IStar> AddStarToDimension(IDimension dimension, IStar star);
        Task<OASISResult<IStar>> AddStarToDimensionAsync(IDimension dimension, IStar star);
        OASISResult<IPlanet> AddPlanetToDimension(IDimension dimension, IPlanet planet);
        Task<OASISResult<IPlanet>> AddPlanetToDimensionAsync(IDimension dimension, IPlanet planet);
        OASISResult<IEnumerable<IUniverse>> GetAllUniversesForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IDimension>> GetAllDimensionsForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IGalaxyCluster>> GetAllGalaxyClustersForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForMultiverse(bool refresh = true); //Helper method which gets the SuperStars at the centre of each Galaxy.
        Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForMultiverseAsync(bool refresh = true); //Helper method which gets the SuperStars at the centre of each Galaxy.
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxiesForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxiesForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxyClustersForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxyClustersForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetAllStarsForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxiesForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxiesForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxyClustersForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxyClustersForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxyClustersForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxiesForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxiesForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IMoon>> GetAllMoonsForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForMultiverseAsync(bool refresh = true);
    }
}