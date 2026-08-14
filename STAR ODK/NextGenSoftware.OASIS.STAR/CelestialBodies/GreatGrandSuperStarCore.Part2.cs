using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.CelestialSpace;

namespace NextGenSoftware.OASIS.STAR
{
    public partial class GreatGrandSuperStarCore
    {

        public OASISResult<IEnumerable<IDimension>> GetAllDimensionsForOmiverse(bool refresh = true)
        {
            return GetAllDimensionsForOmiverseAsync(refresh).Result;
        }



        //public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IGalaxyCluster>> result = new OASISResult<IEnumerable<IGalaxyCluster>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
        //    OASISResultHelper<IEnumerable<IDimension>, IEnumerable<IGalaxyCluster>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<IGalaxyCluster> clusters = new List<IGalaxyCluster>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            clusters.AddRange(dimension.GalaxyClusters);

        //        result.Result = clusters;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxyCluster>> result = new OASISResult<IEnumerable<IGalaxyCluster>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(universesResult, result);

            if (!universesResult.IsError)
            {
                List<IGalaxyCluster> clusters = new List<IGalaxyCluster>();

                foreach (IUniverse universe in universesResult.Result)
                    clusters.AddRange(universe.GalaxyClusters);

                result.Result = clusters;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGalaxyCluster>> GetAllGalaxyClustersForOmiverse(bool refresh = true)
        {
            return GetAllGalaxyClustersForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxy>> result = new OASISResult<IEnumerable<IGalaxy>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxyClustersResult, result);

            if (!galaxyClustersResult.IsError)
            {
                List<IGalaxy> galaxies = new List<IGalaxy>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    galaxies.AddRange(cluster.Galaxies);

                result.Result = galaxies;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForOmiverse(bool refresh = true)
        {
            return GetAllGalaxiesForOmiverseAsync(refresh).Result;
        }

        /*
        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IGalaxy>> galaxiesResult = await GetAllGalaxiesForOmiverseAsync(refresh);
            OASISResultHelper<IEnumerable<IGalaxy>, IEnumerable<ISolarSystem>>.CopyResult(galaxiesResult, ref result);

            if (!galaxiesResult.IsError)
            {
                List<ISolarSystem> solarSystems = new List<ISolarSystem>();

                foreach (IGalaxy galaxy in galaxiesResult.Result)
                    solarSystems.AddRange(galaxy.SolarSystems);

                result.Result = solarSystems;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForOmiverse(bool refresh = true)
        {
            return GetAllSolarSystemsForOmiverseAsync(refresh).Result;
        }*/

        // Helper method to get the GrandSuperStars at the centre of each Multiverse.
        public async Task<OASISResult<IEnumerable<IGrandSuperStar>>> GetAllGrandSuperStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGrandSuperStar>> result = new OASISResult<IEnumerable<IGrandSuperStar>>();
            OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiversesForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(multiversesResult, result);

            if (!multiversesResult.IsError)
            {
                List<IGrandSuperStar> grandSuperstars = new List<IGrandSuperStar>();

                foreach (IMultiverse multiverse in multiversesResult.Result)
                    grandSuperstars.Add(multiverse.GrandSuperStar);

                result.Result = grandSuperstars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGrandSuperStar>> GetAllGrandSuperStarsForOmiverse(bool refresh = true)
        {
            return GetAllGrandSuperStarsForOmiverseAsync(refresh).Result;
        }

        // Helper method to get the SuperStars at the centre of each Galaxy.
        public async Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISuperStar>> result = new OASISResult<IEnumerable<ISuperStar>>();
            OASISResult<IEnumerable<IGrandSuperStar>> grandSuperStarsResult = await GetAllGrandSuperStarsForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(grandSuperStarsResult, result);

            if (!grandSuperStarsResult.IsError)
            {
                List<ISuperStar> superstars = new List<ISuperStar>();

                foreach (IGrandSuperStar grandSuperStar in grandSuperStarsResult.Result)
                {
                    OASISResult<IEnumerable<ISuperStar>> superStarsResult = await ((IGrandSuperStarCore)grandSuperStar.CelestialBodyCore).GetAllSuperStarsForMultiverseAsync(refresh);

                    if (!superStarsResult.IsError)
                        superstars.AddRange(superStarsResult.Result);
                }

                result.Result = superstars;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForOmiverse(bool refresh = true)
        {
            return GetAllSuperStarsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxyClustersResult, result);

            if (!galaxyClustersResult.IsError)
            {
                List<ISolarSystem> solarSystems = new List<ISolarSystem>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    solarSystems.AddRange(cluster.SolarSystems);

                result.Result = solarSystems;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxiesForOmiverse(bool refresh = true)
        {
            return GetAllSolarSystemsOutSideOfGalaxiesForOmiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
        //    OASISResultHelper<IEnumerable<IDimension>, IEnumerable<ISolarSystem>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<ISolarSystem> solarSystems = new List<ISolarSystem>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            solarSystems.AddRange(dimension.SoloarSystems);

        //        result.Result = solarSystems;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(universesResult, result);

            if (!universesResult.IsError)
            {
                List<ISolarSystem> solarSystems = new List<ISolarSystem>();

                foreach (IUniverse universe in universesResult.Result)
                    solarSystems.AddRange(universe.SolarSystems);

                result.Result = solarSystems;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverse(bool refresh = true)
        {
            return GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IGalaxy>> galaxiesResult = await GetAllGalaxiesForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxiesResult, result);
            List<ISolarSystem> solarSystems = new List<ISolarSystem>();

            if (!galaxiesResult.IsError)
            {
                foreach (IGalaxy galaxy in galaxiesResult.Result)
                    solarSystems.AddRange(galaxy.SolarSystems);

                result.Result = solarSystems;
            }

            OASISResult<IEnumerable<ISolarSystem>> solarSystemsOutsideResult = await GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(refresh);

            if (!solarSystemsOutsideResult.IsError)
                solarSystems.AddRange(solarSystemsOutsideResult.Result);

            solarSystemsOutsideResult = await GetAllSolarSystemsOutSideOfGalaxiesForOmiverseAsync(refresh);

            if (!solarSystemsOutsideResult.IsError)
                solarSystems.AddRange(solarSystemsOutsideResult.Result);

            result.Result = solarSystems;
            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForOmiverse(bool refresh = true)
        {
            return GetAllSolarSystemsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxyClustersResult, result);

            if (!galaxyClustersResult.IsError)
            {
                List<IStar> stars = new List<IStar>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    stars.AddRange(cluster.Stars);

                result.Result = stars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxiesForOmiverse(bool refresh = true)
        {
            return GetAllStarsOutSideOfGalaxiesForOmiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
        //    OASISResultHelper<IEnumerable<IDimension>, IEnumerable<IStar>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<IStar> stars = new List<IStar>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            stars.AddRange(dimension.Stars);

        //        result.Result = stars;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(universesResult, result);

            if (!universesResult.IsError)
            {
                List<IStar> stars = new List<IStar>();

                foreach (IUniverse universe in universesResult.Result)
                    stars.AddRange(universe.Stars);

                result.Result = stars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxyClustersForOmiverse(bool refresh = true)
        {
            return GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<ISuperStar>> superStarsResult = await GetAllSuperStarsForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(superStarsResult, result);
            List<IStar> stars = new List<IStar>();

            if (!superStarsResult.IsError)
            {
                foreach (ISuperStar superStar in superStarsResult.Result)
                {
                    OASISResult<IEnumerable<IStar>> starsResult = await ((ISuperStarCore)superStar.CelestialBodyCore).GetAllStarsForGalaxyAsync(refresh);

                    if (!starsResult.IsError)
                        stars.AddRange(starsResult.Result);
                }
            }

            OASISResult<IEnumerable<IStar>> starsOutsideResult = await GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(refresh);

            if (!starsOutsideResult.IsError)
                stars.AddRange(starsOutsideResult.Result);

            starsOutsideResult = await GetAllStarsOutSideOfGalaxiesForOmiverseAsync(refresh);

            if (!starsOutsideResult.IsError)
                stars.AddRange(starsOutsideResult.Result);

            result.Result = stars;
            return result;
        }
    }
}
