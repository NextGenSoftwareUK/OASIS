using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each Multiverse (creates dimensions, universes & GalaxyClusters). Prime Creator
    public interface IGrandSuperStarCore : ICelestialBodyCore
    {
        IGrandSuperStar GrandSuperStar { get; set; }

        OASISResult<IDimension> AddDimensionToMultiverse(IDimension dimension);
        Task<OASISResult<IDimension>> AddDimensionToMultiverseAsync(IDimension dimension);
        Task<OASISResult<IThirdDimension>> AddThirdDimensionToMultiverseAsync();
        OASISResult<IThirdDimension> AddThirdDimensionToMultiverse();
        OASISResult<IUniverse> AddParallelUniverseToThirdDimension(IUniverse universe);
        Task<OASISResult<IUniverse>> AddParallelUniverseToThirdDimensionAsync(IUniverse universe);
        OASISResult<IGalaxyCluster> AddGalaxyClusterToUniverse(IUniverse universe, IGalaxyCluster galaxyCluster);
        Task<OASISResult<IGalaxyCluster>> AddGalaxyClusterToUniverseAsync(IUniverse universe, IGalaxyCluster galaxyCluster);
        Task<OASISResult<IGalaxy>> AddGalaxyToGalaxyClusterAsync(IGalaxyCluster galaxyCluster, IGalaxy galaxy);
        OASISResult<IGalaxy> AddGalaxyToGalaxyCluster(IGalaxyCluster galaxyCluster, IGalaxy galaxy);
        OASISResult<ISolarSystem> AddSolarSystemToUniverse(IUniverse universe, ISolarSystem solarSystem);
        Task<OASISResult<ISolarSystem>> AddSolarSystemToUniverseAsync(IUniverse universe, ISolarSystem solarSystem);
        OASISResult<IStar> AddStarToUniverse(IUniverse universe, IStar star);
        Task<OASISResult<IStar>> AddStarToUniverseAsync(IUniverse universe, IStar star);
        OASISResult<IPlanet> AddPlanetToUniverse(IUniverse universe, IPlanet planet);
        Task<OASISResult<IPlanet>> AddPlanetToUniverseAsync(IUniverse universe, IPlanet planet);
        OASISResult<IEnumerable<IDimension>> GetAllDimensionsForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IUniverse>> GetAllUniversesForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IGalaxyCluster>> GetAllGalaxyClustersForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForMultiverseAsync(bool refresh = true);
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
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxiesForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxiesForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxyClustersForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IMoon>> GetAllMoonsForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForMultiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForMultiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForMultiverseAsync(bool refresh = true);
    }
}