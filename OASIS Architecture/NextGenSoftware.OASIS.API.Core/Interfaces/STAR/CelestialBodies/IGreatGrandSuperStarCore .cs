using NextGenSoftware.OASIS.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of the Omniverse (there can only be ONE) ;-) (creates Omiverses (with a GrandSuperStar at the centre of each).  Spirit/God/The Divine, etc
    public interface IGreatGrandSuperStarCore
    {
        IGreatGrandSuperStar GreatGrandSuperStar { get; set; }

        OASISResult<IOmiverse> AddOmiverse(IOmiverse omniverse);
        Task<OASISResult<IOmiverse>> AddOmiverseAsync(IOmiverse omniverse);
        OASISResult<IDimension> AddDimensionToOmniverse(IDimension dimension);
        Task<OASISResult<IDimension>> AddDimensionToOmniverseAsync(IDimension omniverse);
        OASISResult<IMultiverse> AddMultiverse(IMultiverse multiverse);
        Task<OASISResult<IMultiverse>> AddMultiverseAsync(IMultiverse multiverse);
        OASISResult<IEnumerable<IMultiverse>> GetAllMultiversesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IMultiverse>>> GetAllMultiversesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IUniverse>> GetAllUniversesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IDimension>> GetAllDimensionsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IGalaxyCluster>> GetAllGalaxyClustersForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IGrandSuperStar>> GetAllGrandSuperStarsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IGrandSuperStar>>> GetAllGrandSuperStarsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetAllStarsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxiesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxyClustersForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxiesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxiesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IMoon>> GetAllMoonsForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForOmiverseAsync(bool refresh = true);
        OASISResult<IEnumerable<IZome>> GetAllZomesForOmiverse(bool refresh = true);
        Task<OASISResult<IEnumerable<IZome>>> GetAllZomesForOmiverseAsync(bool refresh = true);
    }
}