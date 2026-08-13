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

        public OASISResult<IEnumerable<IStar>> GetAllStarsForOmiverse(bool refresh = true)
        {
            return GetAllStarsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxyClustersResult, result);

            if (!galaxyClustersResult.IsError)
            {
                List<IPlanet> planets = new List<IPlanet>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    planets.AddRange(cluster.Planets);

                result.Result = planets;
            }

            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxiesForOmiverse(bool refresh = true)
        {
            return GetAllPlanetsOutSideOfGalaxiesForOmiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
        //    OASISResultHelper<IEnumerable<IDimension>, IEnumerable<IPlanet>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<IPlanet> planets = new List<IPlanet>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            planets.AddRange(dimension.Planets);

        //        result.Result = planets;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(universesResult, result);

            if (!universesResult.IsError)
            {
                List<IPlanet> planets = new List<IPlanet>();

                foreach (IUniverse universe in universesResult.Result)
                    planets.AddRange(universe.Planets);

                result.Result = planets;
            }

            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverse(bool refresh = true)
        {
            return GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<ISuperStar>> superStarsResult = await GetAllSuperStarsForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(superStarsResult, result);
            List<IPlanet> planets = new List<IPlanet>();

            if (!superStarsResult.IsError)
            {
                foreach (ISuperStar superStar in superStarsResult.Result)
                {
                    OASISResult<IEnumerable<IPlanet>> planetsResult = await ((ISuperStarCore)superStar.CelestialBodyCore).GetAllPlanetsForGalaxyAsync(refresh);

                    if (!planetsResult.IsError)
                        planets.AddRange(planetsResult.Result);
                }
            }

            OASISResult<IEnumerable<IPlanet>> planetsOutsideResult = await GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(refresh);

            if (!planetsOutsideResult.IsError)
                planets.AddRange(planetsOutsideResult.Result);

            planetsOutsideResult = await GetAllPlanetsOutSideOfGalaxiesForOmiverseAsync(refresh);

            if (!planetsOutsideResult.IsError)
                planets.AddRange(planetsOutsideResult.Result);

            result.Result = planets;
            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForOmiverse(bool refresh = true)
        {
            return GetAllPlanetsForOmiverseAsync(refresh).Result;
        }


        /*
        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<ISuperStar>> superStarsResult = await GetAllSuperStarsForOmiverseAsync(refresh);
            OASISResultHelper<IEnumerable<ISuperStar>, IEnumerable<IStar>>.CopyResult(superStarsResult, ref result);

            if (!superStarsResult.IsError)
            {
                List<IStar> stars = new List<IStar>();

                foreach (ISuperStar superStar in superStarsResult.Result)
                {
                    OASISResult<IEnumerable<IStar>> starsResult = await ((ISuperStarCore)superStar.CelestialBodyCore).GetAllStarsForGalaxyAsync(refresh);

                    if (!starsResult.IsError)
                        stars.AddRange(starsResult.Result);
                }

                result.Result = stars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsForOmiverse(bool refresh = true)
        {
            return GetAllStarsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IStar>> starsResult = await GetAllStarsForOmiverseAsync(refresh);
            OASISResultHelper<IEnumerable<IStar>, IEnumerable<IPlanet>>.CopyResult(starsResult, ref result);

            if (!starsResult.IsError)
            {
                List<IPlanet> planets = new List<IPlanet>();

                foreach (IStar star in starsResult.Result)
                {
                    OASISResult<IEnumerable<IPlanet>> planetsResult = await ((IStarCore)star.CelestialBodyCore).GetAllPlanetsForSolarSystemAsync(refresh);

                    if (!planetsResult.IsError)
                        planets.AddRange(planetsResult.Result);
                }

                result.Result = planets;
            }

            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForOmiverse(bool refresh = true)
        {
            return GetAllPlanetsForOmiverseAsync(refresh).Result;
        }*/


        public async Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IMoon>> result = new OASISResult<IEnumerable<IMoon>>();
            OASISResult<IEnumerable<IPlanet>> planetsResult = await GetAllPlanetsForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(planetsResult, result);

            if (!planetsResult.IsError)
            {
                List<IMoon> moons = new List<IMoon>();

                foreach (IPlanet planet in planetsResult.Result)
                {
                    OASISResult<IEnumerable<IMoon>> moonsResult = await ((IPlanetCore)planet.CelestialBodyCore).GetMoonsAsync(refresh);

                    if (!moonsResult.IsError)
                        moons.AddRange(moonsResult.Result);
                }

                result.Result = moons;
            }

            return result;
        }

        public OASISResult<IEnumerable<IZome>> GetAllZomesForOmiverse(bool refresh = true)
        {
            return GetAllZomesForOmiverseAsync(refresh).Result;
        }

        //TODO: Come back to this! :)
        public async Task<OASISResult<IEnumerable<IZome>>> GetAllZomesForOmiverseAsync(bool refresh = true)
        {
            List<IZome> zomes = new List<IZome>();
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IEnumerable<IStar>> starsResult = await GetAllStarsForOmiverseAsync(refresh);
            OASISResult<IEnumerable<IPlanet>> planetsResult = await GetAllPlanetsForOmiverseAsync(refresh);
            OASISResult<IEnumerable<IMoon>> moonsResult = await GetAllMoonsForOmiverseAsync(refresh);
            //OASISResultHelper<IEnumerable<IMoon>, IEnumerable<IZome>>.CopyResult(moonsResult, ref result);

            if (!moonsResult.IsError)
            {
                foreach (IMoon moon in moonsResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await ((IMoonCore)moon.CelestialBodyCore).LoadZomesAsync();

                    if (!zomesResult.IsError)
                        zomes.AddRange(zomesResult.Result);

                    if (moon.ParentPlanet.CelestialBodyCore.Zomes != null)
                        zomes.AddRange(moon.ParentPlanet.CelestialBodyCore.Zomes);
                    else
                    {
                        OASISResult<IEnumerable<IZome>> planetZomesResult = await moon.ParentPlanet.LoadZomesAsync();

                        if (!planetZomesResult.IsError && planetZomesResult.Result != null)
                            zomes.AddRange(planetZomesResult.Result);
                    }

                    /*
                    if (moon.ParentStar.CelestialBodyCore.Zomes != null)
                        zomes.AddRange(moon.ParentStar.CelestialBodyCore.Zomes);
                    else
                    {
                        OASISResult<IEnumerable<IZome>> starZomesResult = await moon.ParentStar.LoadZomesAsync();

                        if (!starZomesResult.IsError && starZomesResult.Result != null)
                            zomes.AddRange(starZomesResult.Result);
                    }*/
                }

                result.Result = zomes;
            }

            //TODO: Think this way is better than what is commented out above?
            if (!planetsResult.IsError)
            {
                foreach (IPlanet planet in planetsResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await ((IPlanetCore)planet.CelestialBodyCore).LoadZomesAsync();

                    if (!zomesResult.IsError)
                        zomes.AddRange(zomesResult.Result);
                }
            }

            if (!starsResult.IsError)
            {
                foreach (IStar star in starsResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await ((IStarCore)star.CelestialBodyCore).LoadZomesAsync();

                    if (!zomesResult.IsError)
                        zomes.AddRange(zomesResult.Result);
                }
            }

            //TODO: Be good to get this working so it will be 4 lines of code instead of 9 for each collection! :)
            //OASISResult<IEnumerable<ICelestialBody>> celestialBodyResult = new OASISResult<IEnumerable<ICelestialBody>>();
            //OASISResultHelper<IEnumerable<IMoon>, IEnumerable<ICelestialBody>>.CopyResult(moonsResult, ref celestialBodyResult);
            //celestialBodyResult.Result = Mapper<IMoon, CelestialBody>.MapBaseHolonProperties(moonsResult.Result);
            //zomes.AddRange(await LoadAlLZomesForCelestialBody(celestialBodyResult));

            result.Result = zomes;
            return result;
        }

        private async Task<List<IZome>> LoadAlLZomesForCelestialBody(OASISResult<IEnumerable<ICelestialBody>> celestialbodiesResult)
        {
            List<IZome> zomes = new List<IZome>();

            if (!celestialbodiesResult.IsError)
            {
                foreach (ICelestialBody celestialBody in celestialbodiesResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await celestialBody.CelestialBodyCore.LoadZomesAsync();

                    if (!zomesResult.IsError && zomesResult.Result != null)
                        zomes.AddRange(zomesResult.Result);
                }
            }

            return zomes;
        }

        public OASISResult<IEnumerable<IMoon>> GetAllMoonsForOmiverse(bool refresh = true)
        {
            return GetAllMoonsForOmiverseAsync(refresh).Result;
        }

        /*
        public async Task<OASISResult<IEnumerable<ICelestialHolon>>> GetCelestialCollection(OASISResult<IEnumerable<ICelestialHolon>> parentCollection, bool refresh = true)
        {
            OASISResult<IEnumerable<ICelestialHolon>> result = new OASISResult<IEnumerable<ICelestialHolon>>();
            //OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiverseForOmiverseAsync(refresh);
            OASISResultHelper<IEnumerable<ICelestialHolon>, IEnumerable<ICelestialHolon>>.CopyResult(parentCollection, ref result);

            if (!parentCollection.IsError)
            {
                List<ICelestialHolon> children = new List<ICelestialHolon>();

                foreach (ICelestialHolon parent in parentCollection.Result)
                    children.AddRange(parent.Universes); //TODO: Need to pass in a dyanmic property name somehow? If we can work out how to make this work we can save a lot of code with this generic method! ;-)

                result.Result = children;
            }

            return result;
        }*/
    }
}
