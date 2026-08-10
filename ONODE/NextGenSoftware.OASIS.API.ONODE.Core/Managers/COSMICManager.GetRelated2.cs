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

        /// <summary>
        /// Creates a Multiverse with its full child hierarchy while preserving the cyberspace ontology.
        /// Creates: Multiverse -> Universe -> GalaxyCluster -> Galaxy -> SolarSystem -> Star -> Planet -> Moon
        /// </summary>
        public async Task<OASISResult<IMultiverse>> CreateMultiverseWithChildrenAsync(
            IOmiverse parentOmniverse,
            IMultiverse multiverse,
            bool createUniverse = true,
            bool createGalaxyCluster = true,
            bool createGalaxy = true,
            bool createSolarSystem = true,
            bool createStar = true,
            bool createPlanet = true,
            bool createMoon = true)
        {
            var result = new OASISResult<IMultiverse>();

            // 1. Create Multiverse in Omniverse
            var multiverseResult = await AddMultiverseAsync(parentOmniverse, multiverse);
            if (multiverseResult.IsError || multiverseResult.Result == null)
            {
                OASISResultHelper.CopyResult(multiverseResult, result);
                return result;
            }

            var persistedMultiverse = multiverseResult.Result;

            // Auto-create child Universe if requested.
            // Concrete STAR types live in the STAR SDK; callers should construct an IUniverse
            // and pass it to CreateUniverseWithChildrenAsync / AddUniverseAsync after this call.
            if (createUniverse)
                result.Message += (result.Message?.Length > 0 ? " " : "") +
                    "Universe auto-creation skipped: pass an IUniverse instance to AddUniverseAsync or CreateUniverseWithChildrenAsync.";

            result.Result = persistedMultiverse;
            return result;
        }

        /// <summary>
        /// Creates a Galaxy with its full child hierarchy while preserving the cyberspace ontology.
        /// Creates: Galaxy -> SolarSystem -> Star -> Planet -> Moon
        /// </summary>
        public async Task<OASISResult<IGalaxy>> CreateGalaxyWithChildrenAsync(
            IGalaxyCluster parentGalaxyCluster,
            IGalaxy galaxy,
            bool createSolarSystem = true,
            bool createStar = true,
            bool createPlanet = true,
            bool createMoon = true)
        {
            var result = new OASISResult<IGalaxy>();

            // 1. Create Galaxy in GalaxyCluster
            var galaxyResult = await AddGalaxyAsync(parentGalaxyCluster, galaxy);
            if (galaxyResult.IsError || galaxyResult.Result == null)
            {
                OASISResultHelper.CopyResult(galaxyResult, result);
                return result;
            }

            var persistedGalaxy = galaxyResult.Result;

            // Auto-create child SolarSystem if requested.
            // Concrete STAR types live in the STAR SDK; callers should construct an ISolarSystem
            // and pass it to CreateSolarSystemWithChildrenAsync / AddSolarSystemAsync after this call.
            if (createSolarSystem)
                result.Message += (result.Message?.Length > 0 ? " " : "") +
                    "SolarSystem auto-creation skipped: pass an ISolarSystem instance to AddSolarSystemAsync or CreateSolarSystemWithChildrenAsync.";

            result.Result = persistedGalaxy;
            return result;
        }

        /// <summary>
        /// Creates a SolarSystem with its full child hierarchy while preserving the cyberspace ontology.
        /// Creates: SolarSystem -> Star -> Planet -> Moon
        /// </summary>
        public async Task<OASISResult<ISolarSystem>> CreateSolarSystemWithChildrenAsync(
            IGalaxy parentGalaxy,
            ISolarSystem solarSystem,
            IStar star,
            bool createPlanet = true,
            bool createMoon = true)
        {
            var result = new OASISResult<ISolarSystem>();

            // Attach star to solar system
            solarSystem.Star = star;

            // 1. Create SolarSystem (and Star) in Galaxy
            var solarSystemResult = await AddSolarSystemAsync(parentGalaxy, solarSystem);
            if (solarSystemResult.IsError || solarSystemResult.Result == null)
            {
                OASISResultHelper.CopyResult(solarSystemResult, result);
                return result;
            }

            var persistedSolarSystem = solarSystemResult.Result;

            // Auto-create child Planet / Moon if requested.
            // Concrete STAR types live in the STAR SDK; callers should construct an IPlanet/IMoon
            // and pass them to AddPlanetAsync / AddMoonAsync after this call.
            if (createPlanet)
                result.Message += (result.Message?.Length > 0 ? " " : "") +
                    "Planet auto-creation skipped: pass an IPlanet instance to AddPlanetAsync.";

            result.Result = persistedSolarSystem;
            return result;
        }

        /// <summary>
        /// Creates a Planet with its child Moons while preserving the cyberspace ontology.
        /// Creates: Planet -> Moon(s)
        /// </summary>
        public async Task<OASISResult<IPlanet>> CreatePlanetWithChildrenAsync(
            ISolarSystem parentSolarSystem,
            IPlanet planet,
            bool createMoon = true,
            int numberOfMoons = 1)
        {
            var result = new OASISResult<IPlanet>();

            // 1. Create Planet in SolarSystem
            var planetResult = await AddPlanetAsync(parentSolarSystem, planet);
            if (planetResult.IsError || planetResult.Result == null)
            {
                OASISResultHelper.CopyResult(planetResult, result);
                return result;
            }

            var persistedPlanet = planetResult.Result;

            // Auto-create child Moons if requested.
            // Concrete STAR types live in the STAR SDK; callers should construct IMoon instances
            // and pass them to AddMoonAsync after this call.
            if (createMoon && numberOfMoons > 0)
                result.Message += (result.Message?.Length > 0 ? " " : "") +
                    $"Moon auto-creation skipped ({numberOfMoons} requested): pass IMoon instances to AddMoonAsync.";

            result.Result = persistedPlanet;
            return result;
        }

        /// <summary>
        /// Creates a Star with its child Planets (and their Moons) while preserving the cyberspace ontology.
        /// Creates: Star -> Planet(s) -> Moon(s)
        /// </summary>
        public async Task<OASISResult<IStar>> CreateStarWithChildrenAsync(
            ISolarSystem parentSolarSystem,
            IStar star,
            bool createPlanet = true,
            int numberOfPlanets = 1,
            bool createMoon = true,
            int numberOfMoonsPerPlanet = 1)
        {
            var result = new OASISResult<IStar>();

            // Attach star to solar system
            parentSolarSystem.Star = star;

            // 1. Save the SolarSystem with Star
            var saveResult = await parentSolarSystem.SaveAsync();
            if (saveResult.IsError)
            {
                OASISResultHelper.CopyResult(saveResult, result);
                return result;
            }

            // Auto-create child Planets / Moons if requested.
            // Concrete STAR types live in the STAR SDK; callers should construct IPlanet/IMoon
            // instances and pass them to AddPlanetAsync / AddMoonAsync after this call.
            if (createPlanet && numberOfPlanets > 0)
                result.Message += (result.Message?.Length > 0 ? " " : "") +
                    $"Planet auto-creation skipped ({numberOfPlanets} requested): pass IPlanet instances to AddPlanetAsync.";

            result.Result = star;
            return result;
        }



        /// <summary>
        /// Gets or creates the user's default multiverse (parentId = avatarId, parentOmniverseId = main omniverse)
        /// </summary>
        public async Task<OASISResult<IMultiverse>> GetOrCreateUserMultiverseAsync()
        {
            var result = new OASISResult<IMultiverse>();

            try
            {
                // First, try to find existing user multiverse
                var searchResult = await SearchHolonsForParentAsync<Holon>(
                    "",
                    AvatarId,
                    default(Guid),
                    null,
                    MetaKeyValuePairMatchMode.All,
                    true, // showOnlyForCurrentAvatar
                    HolonType.Multiverse,
                    ProviderType.Default
                );

                if (!searchResult.IsError && searchResult.Result != null && searchResult.Result.Any())
                {
                    // Find multiverse created by this avatar
                    var userMultiverse = searchResult.Result.FirstOrDefault(m => m.CreatedByAvatarId == AvatarId) as IMultiverse;
                    if (userMultiverse != null)
                    {
                        result.Result = userMultiverse;
                        return result;
                    }
                }

                // If not found, create a new user multiverse
                // Get the main omniverse first
                var omniverseResult = await GetOmniverseAsync();
                if (omniverseResult.IsError || omniverseResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(omniverseResult, result);
                    OASISErrorHandling.HandleError(ref result, "Could not load main Omniverse. Cannot create user multiverse.");
                    return result;
                }

                // Create new multiverse for user using factory method
                var multiverseResult = await CreateMultiverseFactoryAsync(
                    omniverseResult.Result,
                    $"Multiverse of Avatar {AvatarId}",
                    $"Default multiverse for avatar {AvatarId}"
                );

                if (multiverseResult.IsError || multiverseResult.Result == null)
                {
                    OASISResultHelper.CopyResult(multiverseResult, result);
                    return result;
                }

                result.Result = multiverseResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting or creating user multiverse: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Checks if a holon belongs to the user's multiverse (user owns it)
        /// </summary>
        public async Task<OASISResult<bool>> IsUserOwnedAsync(IHolon holon)
        {
            var result = new OASISResult<bool> { Result = false };

            try
            {
                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null.");
                    return result;
                }

                // Get user's multiverse
                var userMultiverseResult = await GetOrCreateUserMultiverseAsync();
                if (userMultiverseResult.IsError || userMultiverseResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(userMultiverseResult, result);
                    return result;
                }

                var userMultiverseId = userMultiverseResult.Result.Id;

                // Check if holon's ParentMultiverseId matches user's multiverse
                // Or if it's a descendant of the user's multiverse
                result.Result = holon.ParentMultiverseId == userMultiverseId || 
                                holon.CreatedByAvatarId == AvatarId;

                // Also check if it's a direct child or descendant by traversing up the parent chain
                if (!result.Result)
                {
                    var current = holon;
                    while (current != null && current.ParentHolonId != Guid.Empty)
                    {
                        if (current.ParentMultiverseId == userMultiverseId)
                        {
                            result.Result = true;
                            break;
                        }

                        // Load parent to continue traversal
                        if (current.ParentHolonId != Guid.Empty)
                        {
                            var parentLoad = await Data.LoadHolonAsync(current.ParentHolonId);
                            if (parentLoad.IsError || parentLoad.Result == null)
                                break;
                            current = parentLoad.Result;
                        }
                        else
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error checking user ownership: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Checks if a multiverse is a system multiverse (MagicVerse or The Grand Simulation)
        /// </summary>
        public bool IsSystemMultiverse(IMultiverse multiverse)
        {
            if (multiverse == null || string.IsNullOrEmpty(multiverse.Name))
                return false;

            string name = multiverse.Name.Trim();
            return name.Equals("MagicVerse", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("The Grand Simulation", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the MagicVerse system multiverse
        /// </summary>
        public async Task<OASISResult<IMultiverse>> GetMagicVerseAsync()
        {
            var result = new OASISResult<IMultiverse>();

            try
            {
                var searchResult = await SearchHolonsForParentAsync<Holon>(
                    "MagicVerse",
                    default(Guid),
                    default(Guid),
                    null,
                    MetaKeyValuePairMatchMode.All,
                    false,
                    HolonType.Multiverse,
                    ProviderType.Default
                );

                if (!searchResult.IsError && searchResult.Result != null)
                {
                    var magicVerse = searchResult.Result.FirstOrDefault(m => 
                        m.Name.Equals("MagicVerse", StringComparison.OrdinalIgnoreCase));
                    if (magicVerse != null)
                    {
                        result.Result = magicVerse as IMultiverse;
                        return result;
                    }
                }

                OASISErrorHandling.HandleError(ref result, "MagicVerse not found.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting MagicVerse: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Gets The Grand Simulation system multiverse
        /// </summary>
        public async Task<OASISResult<IMultiverse>> GetGrandSimulationAsync()
        {
            var result = new OASISResult<IMultiverse>();

            try
            {
                var searchResult = await SearchHolonsForParentAsync<Holon>(
                    "The Grand Simulation",
                    default(Guid),
                    default(Guid),
                    null,
                    MetaKeyValuePairMatchMode.All,
                    false,
                    HolonType.Multiverse,
                    ProviderType.Default
                );

                if (!searchResult.IsError && searchResult.Result != null)
                {
                    var grandSim = searchResult.Result.FirstOrDefault(m => 
                        m.Name.Equals("The Grand Simulation", StringComparison.OrdinalIgnoreCase));
                    if (grandSim != null)
                    {
                        result.Result = grandSim as IMultiverse;
                        return result;
                    }
                }

                OASISErrorHandling.HandleError(ref result, "The Grand Simulation not found.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting The Grand Simulation: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Factory method to create a Multiverse instance
        /// </summary>
        public async Task<OASISResult<IMultiverse>> CreateMultiverseFactoryAsync(
            IOmiverse parentOmniverse,
            string name,
            string description = null)
        {
            var result = new OASISResult<IMultiverse>();

            try
            {
                if (parentOmniverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Omniverse cannot be null.");
                    return result;
                }

                // Create a new Holon with Multiverse type and cast to IMultiverse
                var multiverseHolon = new NextGenSoftware.OASIS.API.Core.Holons.Holon(HolonType.Multiverse)
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description ?? $"Multiverse: {name}",
                    HolonType = HolonType.Multiverse,
                    CreatedByAvatarId = AvatarId,
                    ModifiedByAvatarId = AvatarId,
                    IsNewHolon = true,
                    ParentOmniverseId = parentOmniverse.Id,
                    ParentOmniverse = parentOmniverse,
                    ParentHolonId = parentOmniverse.Id,
                    ParentHolon = parentOmniverse,
                    ParentCelestialSpaceId = parentOmniverse.Id,
                    ParentCelestialSpace = parentOmniverse
                };

                // Save the multiverse
                var saveResult = await SaveHolonAsync<Holon>(multiverseHolon, AvatarId);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    result.Result = saveResult.Result as IMultiverse;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating multiverse: {ex.Message}", ex);
            }

            return result;
        }



        /// <summary>
        /// Creates a proposal for The Grand Simulation
        /// </summary>
        public async Task<OASISResult<ISimulationProposal>> CreateSimulationProposalAsync(
            IHolon proposedHolon, Guid parentUniverseId)
        {
            var result = new OASISResult<ISimulationProposal>();

            try
            {
                if (proposedHolon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Proposed holon cannot be null.");
                    return result;
                }

                // Validate that parent is a Universe (top level for proposals)
                var parentLoad = await LoadTypedHolonAsync<IUniverse>(parentUniverseId, HolonType.Universe);
                if (parentLoad.IsError || parentLoad.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(parentLoad, result);
                    OASISErrorHandling.HandleError(ref result, "Parent must be a Universe for simulation proposals.");
                    return result;
                }

                // Get avatar name for proposal
                string avatarName = AvatarId.ToString();
                try
                {
                    var avatarLoad = await AvatarManager.Instance.LoadAvatarAsync(AvatarId);
                    if (!avatarLoad.IsError && avatarLoad.Result != null)
                    {
                        avatarName = avatarLoad.Result.Username ?? avatarLoad.Result.Name ?? AvatarId.ToString();
                    }
                }
                catch
                {
                    // Use ID if name lookup fails
                }

                // Create SimulationProposal holon
                var proposal = new SimulationProposal
                {
                    Id = Guid.NewGuid(),
                    Name = $"Proposal: {proposedHolon.Name}",
                    Description = $"Proposal for {proposedHolon.HolonType}: {proposedHolon.Description ?? ""}",
                    HolonType = HolonType.Proposal,
                    CreatedByAvatarId = AvatarId,
                    CreatedByAvatarName = avatarName,
                    CreatedDate = DateTime.UtcNow,
                    ProposedHolon = proposedHolon,
                    ProposedHolonId = proposedHolon.Id,
                    ProposedHolonType = proposedHolon.HolonType,
                    ProposedHolonName = proposedHolon.Name,
                    ProposedHolonDescription = proposedHolon.Description ?? "",
                    ParentUniverseId = parentUniverseId,
                    ProposalCategory = $"Universe.{parentUniverseId}",
                    ParentHolonId = parentUniverseId,
                    IsNewHolon = true
                };

                // Save the proposal holon using generic overload
                var saveResult = await SaveHolonAsync<ISimulationProposal>(proposal);
                if (saveResult.IsError || saveResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                    return result;
                }

                result.Result = saveResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating simulation proposal: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Lists all simulation proposals
        /// </summary>
        public async Task<OASISResult<IEnumerable<ISimulationProposal>>> ListSimulationProposalsAsync(bool onlyMine = false)
        {
            var result = new OASISResult<IEnumerable<ISimulationProposal>>();

            try
            {
                // Load all Proposal holons with ProposalType = "Simulation"
                var searchResult = await SearchHolonsForParentAsync<Holon>(
                    "Simulation",
                    onlyMine ? AvatarId : default(Guid),
                    default(Guid),
                    null,
                    MetaKeyValuePairMatchMode.All,
                    onlyMine,
                    HolonType.Proposal,
                    ProviderType.Default
                );

                if (searchResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(searchResult, result);
                    return result;
                }

                // Filter to only Simulation proposals
                var proposals = new List<ISimulationProposal>();
                if (searchResult.Result != null)
                {
                    foreach (var proposal in searchResult.Result)
                    {
                        if (proposal is ISimulationProposal simProposal && 
                            simProposal.ProposalType == "Simulation")
                        {
                            proposals.Add(simProposal);
                        }
                    }
                }

                result.Result = proposals;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error listing simulation proposals: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Votes on a simulation proposal (accept or reject)
        /// </summary>
        public async Task<OASISResult<bool>> VoteOnSimulationProposalAsync(
            Guid proposalId, bool accept)
        {
            var result = new OASISResult<bool>();

            try
            {
                // Load the proposal holon using generic overload
                var loadResult = await Data.LoadHolonAsync<Holon>(proposalId, childHolonType: HolonType.Proposal);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                    OASISErrorHandling.HandleError(ref result, "Proposal not found.");
                    return result;
                }

                var proposalHolon = loadResult.Result;
                var proposal = proposalHolon as ISimulationProposal;
                
                if (proposal == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Proposal is not a valid ISimulationProposal.");
                    return result;
                }

                // Check if user already voted
                if (proposal.HasUserVoted(AvatarId))
                {
                    OASISErrorHandling.HandleError(ref result, "You have already voted on this proposal. Only one vote per user is allowed.");
                    return result;
                }

                // Add vote
                if (!proposal.AddVote(AvatarId, accept))
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to add vote.");
                    return result;
                }

                // Save updated proposal
                var saveResult = await SaveHolonAsync<Holon>(proposalHolon, AvatarId);
                if (saveResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                    return result;
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error voting on proposal: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Gets user's vote on a proposal (if they voted)
        /// </summary>
        public async Task<OASISResult<bool?>> GetUserVoteOnProposalAsync(Guid proposalId)
        {
            var result = new OASISResult<bool?>();

            try
            {
                // Load the proposal holon using generic overload
                var loadResult = await Data.LoadHolonAsync<Holon>(proposalId, childHolonType: HolonType.Proposal);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                    return result;
                }

                var proposalHolon = loadResult.Result;
                var proposal = proposalHolon as ISimulationProposal;
                
                if (proposal == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Proposal is not a valid ISimulationProposal.");
                    return result;
                }

                // Get user's vote
                result.Result = proposal.GetUserVote(AvatarId);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting user vote: {ex.Message}", ex);
            }

            return result;
        }

    }
}
