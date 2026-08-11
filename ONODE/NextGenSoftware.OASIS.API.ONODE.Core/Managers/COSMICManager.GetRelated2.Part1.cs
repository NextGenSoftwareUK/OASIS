using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class COSMICManager
    {
        public Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IGravitationalWave>((IHolon)omniverse, HolonType.GravitationalWave);

        public async Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGravitationalWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGravitationalWavesForOmniverseAsync(load.Result);
        }



        /// <summary>
        /// Creates a complete Galaxy hierarchy:
        /// Universe -> GalaxyCluster -> Galaxy using the supplied STAR instances.
        /// The caller is responsible for constructing the concrete STAR classes and wiring
        /// any additional properties; this helper wires parents and persists them using
        /// the existing Add*Async methods.
        /// </summary>
        public async Task<OASISResult<IGalaxy>> CreateGalaxyHierarchyAsync(
            IMultiverse parentMultiverse,
            IUniverse universe,
            IGalaxyCluster galaxyCluster,
            IGalaxy galaxy)
        {
            var result = new OASISResult<IGalaxy>();

            // 1. Add Universe to Multiverse.
            var universeResult = await AddUniverseAsync(parentMultiverse, universe);
            if (universeResult.IsError || universeResult.Result == null)
            {
                OASISResultHelper.CopyResult(universeResult, result);
                return result;
            }

            // 2. Add GalaxyCluster to Universe.
            var galaxyClusterResult = await AddGalaxyClusterAsync(universeResult.Result, galaxyCluster);
            if (galaxyClusterResult.IsError || galaxyClusterResult.Result == null)
            {
                OASISResultHelper.CopyResult(galaxyClusterResult, result);
                return result;
            }

            // 3. Add Galaxy to GalaxyCluster.
            var galaxyResult = await AddGalaxyAsync(galaxyClusterResult.Result, galaxy);
            OASISResultHelper.CopyResult(galaxyResult, result);
            result.Result = galaxyResult.Result;

            return result;
        }

        /// <summary>
        /// Creates a full SolarSystem hierarchy inside an existing Galaxy:
        /// Galaxy -> SolarSystem (with central Star) -> Planet -> optional Moons.
        /// The caller constructs the concrete STAR instances; this helper wires parents
        /// and saves them in the correct order using the Add*Async APIs.
        /// </summary>
        public async Task<OASISResult<ISolarSystem>> CreateSolarSystemHierarchyAsync(
            IGalaxy parentGalaxy,
            ISolarSystem solarSystem,
            IStar star,
            IPlanet planet,
            IEnumerable<IMoon> moons = null)
        {
            var result = new OASISResult<ISolarSystem>();

            // Attach the Star to the SolarSystem before calling AddSolarSystemAsync.
            solarSystem.Star = star;

            // 1. Add SolarSystem (and Star) to Galaxy.
            var solarSystemResult = await AddSolarSystemAsync(parentGalaxy, solarSystem);
            if (solarSystemResult.IsError || solarSystemResult.Result == null)
            {
                OASISResultHelper.CopyResult(solarSystemResult, result);
                return result;
            }

            var persistedSolarSystem = solarSystemResult.Result;

            // 2. Add Planet to SolarSystem.
            var planetResult = await AddPlanetAsync(persistedSolarSystem, planet);
            if (planetResult.IsError || planetResult.Result == null)
            {
                OASISResultHelper.CopyResult(planetResult, result);
                return result;
            }

            var persistedPlanet = planetResult.Result;

            // 3. Optionally add Moons to Planet.
            if (moons != null)
            {
                foreach (var moon in moons)
                {
                    var moonResult = await AddMoonAsync(persistedPlanet, moon);
                    if (moonResult.IsError)
                    {
                        // We propagate the first error but still return the SolarSystem that exists so far.
                        OASISResultHelper.CopyResult(moonResult, result);
                        result.Result = persistedSolarSystem;
                        return result;
                    }
                }
            }

            result.Result = persistedSolarSystem;
            return result;
        }

        /// <summary>
        /// Creates a full SolarSystem hierarchy inside an existing Galaxy with a central Star
        /// and a collection of Planets (each with optional Moons).
        /// This is a higher-level convenience wrapper over AddSolarSystemAsync, AddPlanetAsync
        /// and AddMoonAsync.
        /// </summary>
        /// <param name="parentGalaxy">The parent Galaxy that will own the new SolarSystem.</param>
        /// <param name="solarSystem">The SolarSystem to create.</param>
        /// <param name="star">The central Star for the SolarSystem.</param>
        /// <param name="planetsWithMoons">
        /// A collection of tuples where each entry contains a Planet and an optional collection of its Moons.
        /// </param>
        public async Task<OASISResult<ISolarSystem>> CreateSolarSystemWithPlanetsAsync(
            IGalaxy parentGalaxy,
            ISolarSystem solarSystem,
            IStar star,
            IEnumerable<(IPlanet Planet, IEnumerable<IMoon> Moons)> planetsWithMoons)
        {
            var result = new OASISResult<ISolarSystem>();

            // Attach star to solar system.
            solarSystem.Star = star;

            // 1. Create the SolarSystem (and Star) in the Galaxy.
            var solarSystemResult = await AddSolarSystemAsync(parentGalaxy, solarSystem);
            if (solarSystemResult.IsError || solarSystemResult.Result == null)
            {
                OASISResultHelper.CopyResult(solarSystemResult, result);
                return result;
            }

            var persistedSolarSystem = solarSystemResult.Result;

            if (planetsWithMoons != null)
            {
                foreach (var (planet, moons) in planetsWithMoons)
                {
                    // 2. Create each Planet in the SolarSystem.
                    var planetResult = await AddPlanetAsync(persistedSolarSystem, planet);
                    if (planetResult.IsError || planetResult.Result == null)
                    {
                        OASISResultHelper.CopyResult(planetResult, result);
                        result.Result = persistedSolarSystem;
                        return result;
                    }

                    var persistedPlanet = planetResult.Result;

                    // 3. Create Moons for this Planet.
                    if (moons != null)
                    {
                        foreach (var moon in moons)
                        {
                            var moonResult = await AddMoonAsync(persistedPlanet, moon);
                            if (moonResult.IsError)
                            {
                                OASISResultHelper.CopyResult(moonResult, result);
                                result.Result = persistedSolarSystem;
                                return result;
                            }
                        }
                    }
                }
            }

            result.Result = persistedSolarSystem;
            return result;
        }

        /// <summary>
        /// Creates a full SolarSystem hierarchy inside an existing Galaxy with a central Star
        /// and a collection of Planets (without specifying Moons).
        /// This is a convenience overload that wraps CreateSolarSystemWithPlanetsAsync and
        /// assumes no Moons for the supplied Planets.
        /// </summary>
        public Task<OASISResult<ISolarSystem>> CreateSolarSystemWithPlanetsAsync(
            IGalaxy parentGalaxy,
            ISolarSystem solarSystem,
            IStar star,
            IEnumerable<IPlanet> planets)
        {
            IEnumerable<(IPlanet Planet, IEnumerable<IMoon> Moons)> planetsWithMoons = null;

            if (planets != null)
            {
                var list = new List<(IPlanet Planet, IEnumerable<IMoon> Moons)>();
                foreach (var planet in planets)
                    list.Add((planet, null));

                planetsWithMoons = list;
            }

            return CreateSolarSystemWithPlanetsAsync(parentGalaxy, solarSystem, star, planetsWithMoons);
        }

        /// <summary>
        /// Creates a Universe with its initial GalaxyClusters and Galaxies:
        /// Multiverse -> Universe -> GalaxyClusters -> Galaxies.
        /// This uses AddUniverseAsync, AddGalaxyClusterAsync and AddGalaxyAsync under the hood.
        /// </summary>
        /// <param name="parentMultiverse">The Multiverse that will own the new Universe.</param>
        /// <param name="universe">The Universe to create.</param>
        /// <param name="galaxyClustersWithGalaxies">
        /// A collection of tuples where each entry contains a GalaxyCluster and its Galaxies.
        /// </param>
        public async Task<OASISResult<IUniverse>> CreateUniverseWithStructureAsync(
            IMultiverse parentMultiverse,
            IUniverse universe,
            IEnumerable<(IGalaxyCluster GalaxyCluster, IEnumerable<IGalaxy> Galaxies)> galaxyClustersWithGalaxies)
        {
            var result = new OASISResult<IUniverse>();

            // 1. Create Universe in Multiverse.
            var universeResult = await AddUniverseAsync(parentMultiverse, universe);
            if (universeResult.IsError || universeResult.Result == null)
            {
                OASISResultHelper.CopyResult(universeResult, result);
                return result;
            }

            var persistedUniverse = universeResult.Result;

            if (galaxyClustersWithGalaxies != null)
            {
                foreach (var (cluster, galaxies) in galaxyClustersWithGalaxies)
                {
                    // 2. Create GalaxyCluster in Universe.
                    var clusterResult = await AddGalaxyClusterAsync(persistedUniverse, cluster);
                    if (clusterResult.IsError || clusterResult.Result == null)
                    {
                        OASISResultHelper.CopyResult(clusterResult, result);
                        result.Result = persistedUniverse;
                        return result;
                    }

                    var persistedCluster = clusterResult.Result;

                    // 3. Create Galaxies in this GalaxyCluster.
                    if (galaxies != null)
                    {
                        foreach (var galaxy in galaxies)
                        {
                            var galaxyResult = await AddGalaxyAsync(persistedCluster, galaxy);
                            if (galaxyResult.IsError)
                            {
                                OASISResultHelper.CopyResult(galaxyResult, result);
                                result.Result = persistedUniverse;
                                return result;
                            }
                        }
                    }
                }
            }

            result.Result = persistedUniverse;
            return result;
        }

        /// <summary>
        /// Creates a Multiverse under an Omniverse and populates it with one or more Universes.
        /// This is a convenience wrapper over AddMultiverseAsync and AddUniverseAsync.
        /// </summary>
        /// <param name="parentOmniverse">The Omniverse that will own the new Multiverse.</param>
        /// <param name="multiverse">The Multiverse to create.</param>
        /// <param name="universes">The Universes to add to the Multiverse (e.g. MagicVerse, parallel universes, etc.).</param>
        public async Task<OASISResult<IMultiverse>> CreateMultiverseWithUniversesAsync(
            IOmiverse parentOmniverse,
            IMultiverse multiverse,
            IEnumerable<IUniverse> universes)
        {
            var result = new OASISResult<IMultiverse>();

            // 1. Create Multiverse in Omniverse.
            var multiverseResult = await AddMultiverseAsync(parentOmniverse, multiverse);
            if (multiverseResult.IsError || multiverseResult.Result == null)
            {
                OASISResultHelper.CopyResult(multiverseResult, result);
                return result;
            }

            var persistedMultiverse = multiverseResult.Result;

            // 2. Create Universes in this Multiverse.
            if (universes != null)
            {
                foreach (var universe in universes)
                {
                    var universeResult = await AddUniverseAsync(persistedMultiverse, universe);
                    if (universeResult.IsError)
                    {
                        OASISResultHelper.CopyResult(universeResult, result);
                        result.Result = persistedMultiverse;
                        return result;
                    }
                }
            }

            result.Result = persistedMultiverse;
            return result;
        }

        /// <summary>
        /// Creates a Universe with its full child hierarchy while preserving the cyberspace ontology.
        /// Creates: Universe -> GalaxyCluster -> Galaxy -> SolarSystem -> Star -> Planet -> Moon
        /// </summary>
        public async Task<OASISResult<IUniverse>> CreateUniverseWithChildrenAsync(
            IMultiverse parentMultiverse,
            IUniverse universe,
            bool createGalaxyCluster = true,
            bool createGalaxy = true,
            bool createSolarSystem = true,
            bool createStar = true,
            bool createPlanet = true,
            bool createMoon = true)
        {
            var result = new OASISResult<IUniverse>();

            // 1. Create Universe in Multiverse
            var universeResult = await AddUniverseAsync(parentMultiverse, universe);
            if (universeResult.IsError || universeResult.Result == null)
            {
                OASISResultHelper.CopyResult(universeResult, result);
                return result;
            }

            var persistedUniverse = universeResult.Result;

            // 2–3. Auto-create child GalaxyCluster / Galaxy if requested.
            // Concrete STAR types (GalaxyCluster, Galaxy, etc.) live in the STAR SDK which cannot
            // be referenced from ONODE.Core without a circular dependency.  Callers that need
            // default children should construct those objects themselves and pass them to
            // AddGalaxyClusterAsync / AddGalaxyAsync after this call.
            if (createGalaxyCluster)
                result.Message += (result.Message?.Length > 0 ? " " : "") +
                    "GalaxyCluster auto-creation skipped: pass an IGalaxyCluster instance to AddGalaxyClusterAsync.";

            if (createGalaxy)
                result.Message += (result.Message?.Length > 0 ? " " : "") +
                    "Galaxy auto-creation skipped: pass an IGalaxy instance to AddGalaxyAsync.";

            result.Result = persistedUniverse;
            return result;
        }
    }
}
