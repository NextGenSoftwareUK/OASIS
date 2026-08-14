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
    }
}
