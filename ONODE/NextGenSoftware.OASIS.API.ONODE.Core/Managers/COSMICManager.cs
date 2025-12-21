using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    /// <summary>
    /// COSMICManager exposes the full COSMIC ORM / Omniverse object model to the WEB4 OASIS API.
    /// It provides strongly-typed Create/Read/Update/Delete and rich Get X For Y operations
    /// for all CelestialBodies and CelestialSpaces defined in the STAR ontology.
    /// </summary>
    public class COSMICManager : COSMICManagerBase
    {
        private IOmiverse _omiverse = null;

        public COSMICManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
        }

        public COSMICManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {
        }

        public IOmiverse Omiverse
        {
            get
            {
                if (_omiverse == null)
                    _omiverse = GetOmniverseAsync().ConfigureAwait(false).GetAwaiter().GetResult().Result;

                return _omiverse;
            }
        }

        private async Task<OASISResult<IOmiverse>> GetOmniverseAsync()
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();

            // Try to load existing omniverse or create new one
            // This would need to be implemented based on your STAR project structure
            // For now, returning a result that indicates omniverse needs to be created
            return result;
        }

        #region Private Helpers

        private async Task<OASISResult<T>> LoadTypedHolonAsync<T>(Guid id, HolonType holonType)
            where T : class, IHolon
        {
            var result = new OASISResult<T>();

            try
            {
                var loadResult = await Data.LoadHolonAsync(id, childHolonType: holonType);
                OASISResultHelper.CopyResult(loadResult, result);

                if (!loadResult.IsError && loadResult.Result != null)
                {
                    if (loadResult.Result is T typed)
                        result.Result = typed;
                    else
                        OASISErrorHandling.HandleError(ref result,
                            $"Holon with id {id} is not of expected type {typeof(T).Name}.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error loading {typeof(T).Name} with id {id}: {ex.Message}", ex);
            }

            return result;
        }

        private async Task<OASISResult<T>> SaveHolonAsync<T>(T holon,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            where T : class, IHolon
        {
            var result = new OASISResult<T>();

            try
            {
                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{typeof(T).Name} cannot be null.");
                    return result;
                }

                // Use non-generic SaveAsync to avoid requiring T to have a public parameterless constructor.
                var saveResult = await holon.SaveAsync(saveChildren, recursive, maxChildDepth,
                    continueOnError, saveChildrenOnProvider, providerType);

                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);

                if (!saveResult.IsError && saveResult.Result is T typed)
                    result.Result = typed;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error saving {typeof(T).Name}: {ex.Message}", ex);
            }

            return result;
        }

        private async Task<OASISResult<bool>> DeleteHolonAsync<T>(T holon, Guid? avatarId = null,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            where T : class, IHolon
        {
            var result = new OASISResult<bool>();

            try
            {
                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{typeof(T).Name} cannot be null.");
                    return result;
                }

                var deleteResult = await holon.DeleteAsync(avatarId ?? AvatarId, softDelete, providerType);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteResult, result);
                result.Result = !deleteResult.IsError && deleteResult.Result != null;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error deleting {typeof(T).Name}: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<TChild>>> GetChildrenForParentAsync<TChild>(
            IHolon parent, HolonType childHolonType)
            where TChild : class, IHolon
        {
            var result = new OASISResult<IEnumerable<TChild>>();

            try
            {
                if (parent == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent holon cannot be null.");
                    return result;
                }

                var childrenResult = await parent.LoadChildHolonsAsync(
                    holonType: childHolonType,
                    loadChildren: false,
                    recursive: true,
                    maxChildDepth: 0,
                    continueOnError: true,
                    loadChildrenFromProvider: true,
                    version: 0,
                    providerType: ProviderType.Default,
                    cache: true);

                // Copy outer result metadata only, then cast inner collection to the requested interface type.
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(childrenResult, result);

                if (!childrenResult.IsError && childrenResult.Result != null)
                    result.Result = childrenResult.Result.OfType<TChild>().ToList();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error loading {typeof(TChild).Name} children: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<TChild>>> GetChildrenForParentAsync<TChild>(
            Guid parentId, HolonType parentHolonType, HolonType childHolonType)
            where TChild : class, IHolon
        {
            var result = new OASISResult<IEnumerable<TChild>>();

            try
            {
                var parentLoad = await Data.LoadHolonAsync(parentId, childHolonType: HolonType.All);

                if (parentLoad.IsError || parentLoad.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(parentLoad, result);
                    return result;
                }

                var parent = parentLoad.Result;
                return await GetChildrenForParentAsync<TChild>(parent, childHolonType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error loading {typeof(TChild).Name} children for parent {parentId}: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Performs a nested (recursive) search for all children of the given holon type
        /// under the specified parent, walking the full graph (children, grandchildren, etc.)
        /// and filtering by the supplied search term (Name/Description for now).
        /// </summary>
        public async Task<OASISResult<IEnumerable<IHolon>>> SearchChildrenForParentAsync(
            string searchTerm,
            IHolon parent,
            HolonType childHolonType)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (parent == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent holon cannot be null.");
                    return result;
                }

                var childrenResult = await GetChildrenForParentAsync<IHolon>(parent, childHolonType);

                // Copy outer metadata only, then apply in-memory filter for the search term.
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(childrenResult, result);

                if (!childrenResult.IsError && childrenResult.Result != null)
                {
                    var allChildren = childrenResult.Result;

                    if (string.IsNullOrWhiteSpace(searchTerm))
                    {
                        result.Result = allChildren.ToList();
                    }
                    else
                    {
                        var comparison = StringComparison.OrdinalIgnoreCase;
                        string term = searchTerm.Trim();

                        result.Result = allChildren.Where(h =>
                                (!string.IsNullOrEmpty(h.Name) &&
                                 h.Name.IndexOf(term, comparison) >= 0) ||
                                (!string.IsNullOrEmpty(h.Description) &&
                                 h.Description.IndexOf(term, comparison) >= 0))
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error searching children of type {childHolonType} for parent {parent?.Id}: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Overload that takes a parentId and loads the parent holon before performing the nested search.
        /// </summary>
        public async Task<OASISResult<IEnumerable<IHolon>>> SearchChildrenForParentAsync(
            string searchTerm,
            Guid parentId,
            HolonType parentHolonType,
            HolonType childHolonType)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                var parentLoad = await Data.LoadHolonAsync(parentId, childHolonType: HolonType.All);

                if (parentLoad.IsError || parentLoad.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(parentLoad, result);
                    return result;
                }

                return await SearchChildrenForParentAsync(searchTerm, parentLoad.Result, childHolonType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error searching children of type {childHolonType} for parent {parentId}: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region Generic Search Helpers (Cosmic Search API)

        /// <summary>
        /// Searches for holons of the given type anywhere in the Omniverse, optionally scoped to a parentId.
        /// This wraps the core COSMICManagerBase.SearchHolonsAsync helper and exposes it as a public API
        /// using the standard OASISResult wrapper so other layers (web API, CLI, etc) have a consistent interface.
        /// </summary>
        public async Task<OASISResult<IEnumerable<T>>> SearchHolonsForParentAsync<T>(
            string searchTerm,
            Guid avatarId,
            Guid parentId = default,
            Dictionary<string, string> filterByMetaData = null, 
            MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All,
            bool searchOnlyForCurrentAvatar = true,
            HolonType holonType = HolonType.All,
            ProviderType providerType = ProviderType.Default,
            bool loadChildren = true,
            bool recursive = true,
            int maxChildDepth = 0,
            bool continueOnError = true,
            bool loadChildrenFromProvider = false,
            HolonType childHolonType = HolonType.All,
            int version = 0)
            where T : IHolon, new()
        {
            return await SearchHolonsAsync<T>(
                searchTerm,
                avatarId,
                parentId,
                filterByMetaData,
                metaKeyValuePairMatchMode,
                searchOnlyForCurrentAvatar,
                providerType,
                "COSMICManager.SearchHolonsForParentAsync",
                holonType,
                loadChildren,
                recursive,
                maxChildDepth,
                continueOnError,
                loadChildrenFromProvider,
                childHolonType,
                version);
        }

        /// <summary>
        /// Non-async wrapper variant of SearchHolonsForParentAsync.
        /// </summary>
        public OASISResult<IEnumerable<T>> SearchHolonsForParent<T>(
            string searchTerm,
            Guid avatarId,
            Guid parentId = default,
            Dictionary<string, string> filterByMetaData = null,
            MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All,
            bool searchOnlyForCurrentAvatar = true,
            HolonType holonType = HolonType.All,
            ProviderType providerType = ProviderType.Default,
            bool loadChildren = true,
            bool recursive = true,
            int maxChildDepth = 0,
            bool continueOnError = true,
            bool loadChildrenFromProvider = false,
            HolonType childHolonType = HolonType.All,
            int version = 0)
            where T : IHolon, new()
        {
            return SearchHolons<T>(
                searchTerm,
                avatarId,
                parentId,
                filterByMetaData,
                metaKeyValuePairMatchMode,
                searchOnlyForCurrentAvatar,
                providerType,
                "COSMICManager.SearchHolonsForParent",
                holonType,
                loadChildren,
                recursive,
                maxChildDepth,
                continueOnError,
                loadChildrenFromProvider,
                childHolonType,
                version);
        }

        #endregion

        #region Omniverse Methods

        /// <summary>
        /// Saves the specified Omniverse instance using the underlying STAR implementation.
        /// The concrete Omniverse object must be created by the caller (in the STAR project) and
        /// passed in via the IOmiverse interface.
        /// </summary>
        public async Task<OASISResult<IOmiverse>> SaveOmniverseAsync(IOmiverse omniverse)
        {
            var result = new OASISResult<IOmiverse>();

            try
            {
                if (omniverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Omniverse cannot be null.");
                    return result;
                }

                if (omniverse.Id == Guid.Empty)
                {
                    omniverse.Id = Guid.NewGuid();
                    omniverse.IsNewHolon = true;
                }

                var saveResult = await omniverse.SaveAsync();
                OASISResultHelper.CopyResult(saveResult, result);
                result.Result = (IOmiverse)saveResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving Omniverse: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region Multiverse Methods

        /// <summary>
        /// Adds a Multiverse to an existing Omniverse. The concrete Multiverse is created in the STAR project
        /// and passed in via the IMultiverse interface.
        /// </summary>
        public async Task<OASISResult<IMultiverse>> AddMultiverseAsync(IOmiverse parentOmniverse, IMultiverse multiverse)
        {
            var result = new OASISResult<IMultiverse>();

            try
            {
                if (parentOmniverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Omniverse cannot be null.");
                    return result;
                }

                if (multiverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Multiverse cannot be null.");
                    return result;
                }

                if (multiverse.Id == Guid.Empty)
                {
                    multiverse.Id = Guid.NewGuid();
                    multiverse.IsNewHolon = true;
                }

                multiverse.ParentOmniverse = parentOmniverse;
                multiverse.ParentOmniverseId = parentOmniverse.Id;
                multiverse.ParentHolon = parentOmniverse;
                multiverse.ParentHolonId = parentOmniverse.Id;
                multiverse.ParentCelestialSpace = parentOmniverse;
                multiverse.ParentCelestialSpaceId = parentOmniverse.Id;

                var saveResult = await multiverse.SaveAsync();
                OASISResultHelper.CopyResult(saveResult, result);
                result.Result = (IMultiverse)saveResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Multiverse: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IMultiverse>> AddMultiverseAsync(Guid parentOmniverseId, IMultiverse multiverse)
        {
            var loadResult = await LoadTypedHolonAsync<IOmiverse>(parentOmniverseId, HolonType.Omniverse);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IMultiverse>();
                OASISResultHelper.CopyResult(loadResult, result);
                return result;
            }

            return await AddMultiverseAsync(loadResult.Result, multiverse);
        }

        #endregion

        #region Universe Methods

        /// <summary>
        /// Adds a Universe to an existing Multiverse using the existing STAR GrandSuperStar core API
        /// where available, otherwise falls back to saving the Universe directly.
        /// </summary>
        public async Task<OASISResult<IUniverse>> AddUniverseAsync(IMultiverse parentMultiverse, IUniverse universe)
        {
            var result = new OASISResult<IUniverse>();

            try
            {
                if (parentMultiverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Multiverse cannot be null.");
                    return result;
                }

                if (universe == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Universe cannot be null.");
                    return result;
                }

                if (universe.Id == Guid.Empty)
                {
                    universe.Id = Guid.NewGuid();
                    universe.IsNewHolon = true;
                }

                universe.ParentMultiverse = parentMultiverse;
                universe.ParentMultiverseId = parentMultiverse.Id;
                universe.ParentHolon = parentMultiverse;
                universe.ParentHolonId = parentMultiverse.Id;
                universe.ParentCelestialSpace = parentMultiverse;
                universe.ParentCelestialSpaceId = parentMultiverse.Id;

                // Use GrandSuperStar core to register the Universe in the 3rd Dimension when available.
                if (parentMultiverse.GrandSuperStar != null && parentMultiverse.GrandSuperStar.CelestialBodyCore is IGrandSuperStarCore grandCore)
                {
                    var universeResult = await grandCore.AddParallelUniverseToThirdDimensionAsync(universe);
                    OASISResultHelper.CopyResult(universeResult, result);
                }
                else
                {
                    var saveResult = await universe.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IUniverse)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Universe: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IUniverse>> AddUniverseAsync(Guid parentMultiverseId, IUniverse universe)
        {
            var loadResult = await LoadTypedHolonAsync<IMultiverse>(parentMultiverseId, HolonType.Multiverse);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IUniverse>();
                OASISResultHelper.CopyResult(loadResult, result);
                return result;
            }

            return await AddUniverseAsync(loadResult.Result, universe);
        }

        #endregion

        #region GalaxyCluster Methods

        public async Task<OASISResult<IGalaxyCluster>> AddGalaxyClusterAsync(IUniverse parentUniverse, IGalaxyCluster galaxyCluster)
        {
            var result = new OASISResult<IGalaxyCluster>();

            try
            {
                if (parentUniverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Universe cannot be null.");
                    return result;
                }

                if (galaxyCluster == null)
                {
                    OASISErrorHandling.HandleError(ref result, "GalaxyCluster cannot be null.");
                    return result;
                }

                if (galaxyCluster.Id == Guid.Empty)
                {
                    galaxyCluster.Id = Guid.NewGuid();
                    galaxyCluster.IsNewHolon = true;
                }

                var parentMultiverse = parentUniverse.ParentMultiverse;

                if (parentMultiverse != null &&
                    parentMultiverse.GrandSuperStar != null &&
                    parentMultiverse.GrandSuperStar.CelestialBodyCore is IGrandSuperStarCore grandCore)
                {
                    galaxyCluster.ParentMultiverse = parentMultiverse;
                    galaxyCluster.ParentMultiverseId = parentMultiverse.Id;
                    galaxyCluster.ParentHolon = parentMultiverse;
                    galaxyCluster.ParentHolonId = parentMultiverse.Id;
                    galaxyCluster.ParentCelestialSpace = parentMultiverse;
                    galaxyCluster.ParentCelestialSpaceId = parentMultiverse.Id;
                    galaxyCluster.ParentUniverse = parentUniverse;
                    galaxyCluster.ParentUniverseId = parentUniverse.Id;

                    var galaxyClusterResult = await grandCore.AddGalaxyClusterToUniverseAsync(parentUniverse, galaxyCluster);
                    OASISResultHelper.CopyResult(galaxyClusterResult, result);
                }
                else
                {
                    var saveResult = await galaxyCluster.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IGalaxyCluster)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding GalaxyCluster: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IGalaxyCluster>> AddGalaxyClusterAsync(Guid parentUniverseId, IGalaxyCluster galaxyCluster)
        {
            var loadResult = await LoadTypedHolonAsync<IUniverse>(parentUniverseId, HolonType.Universe);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IGalaxyCluster>();
                OASISResultHelper.CopyResult(loadResult, result);
                return result;
            }

            return await AddGalaxyClusterAsync(loadResult.Result, galaxyCluster);
        }

        #endregion

        #region Galaxy Methods

        public async Task<OASISResult<IGalaxy>> AddGalaxyAsync(IGalaxyCluster parentGalaxyCluster, IGalaxy galaxy)
        {
            var result = new OASISResult<IGalaxy>();

            try
            {
                if (parentGalaxyCluster == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent GalaxyCluster cannot be null.");
                    return result;
                }

                if (galaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Galaxy cannot be null.");
                    return result;
                }

                if (galaxy.Id == Guid.Empty)
                {
                    galaxy.Id = Guid.NewGuid();
                    galaxy.IsNewHolon = true;
                }

                var parentMultiverse = parentGalaxyCluster.ParentMultiverse;

                if (parentMultiverse != null &&
                    parentMultiverse.GrandSuperStar != null &&
                    parentMultiverse.GrandSuperStar.CelestialBodyCore is IGrandSuperStarCore grandCore)
                {
                    galaxy.ParentGalaxyCluster = parentGalaxyCluster;
                    galaxy.ParentGalaxyClusterId = parentGalaxyCluster.Id;
                    galaxy.ParentHolon = parentGalaxyCluster;
                    galaxy.ParentHolonId = parentGalaxyCluster.Id;
                    galaxy.ParentCelestialSpace = parentGalaxyCluster;
                    galaxy.ParentCelestialSpaceId = parentGalaxyCluster.Id;

                    var galaxyResult = await grandCore.AddGalaxyToGalaxyClusterAsync(parentGalaxyCluster, galaxy);
                    OASISResultHelper.CopyResult(galaxyResult, result);
                }
                else
                {
                    var saveResult = await galaxy.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IGalaxy)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Galaxy: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IGalaxy>> AddGalaxyAsync(Guid parentGalaxyClusterId, IGalaxy galaxy)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxyCluster>(parentGalaxyClusterId, HolonType.GalaxyCluster);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IGalaxy>();
                OASISResultHelper.CopyResult(loadResult, result);
                return result;
            }

            return await AddGalaxyAsync(loadResult.Result, galaxy);
        }

        #endregion

        #region SolarSystem Methods

        public async Task<OASISResult<ISolarSystem>> AddSolarSystemAsync(IGalaxy parentGalaxy, ISolarSystem solarSystem)
        {
            var result = new OASISResult<ISolarSystem>();

            try
            {
                if (parentGalaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Galaxy cannot be null.");
                    return result;
                }

                if (solarSystem == null)
                {
                    OASISErrorHandling.HandleError(ref result, "SolarSystem cannot be null.");
                    return result;
                }

                if (solarSystem.Id == Guid.Empty)
                {
                    solarSystem.Id = Guid.NewGuid();
                    solarSystem.IsNewHolon = true;
                }

                // We expect the caller to have created and wired the central Star, but if they have
                // not then we still persist the SolarSystem as a CelestialSpace.
                if (parentGalaxy.SuperStar != null &&
                    parentGalaxy.SuperStar.CelestialBodyCore is ISuperStarCore superCore &&
                    solarSystem.Star != null &&
                    solarSystem.Star.CelestialBodyCore is IStarCore starCore)
                {
                    solarSystem.Star.ParentGalaxy = parentGalaxy;
                    solarSystem.Star.ParentGalaxyId = parentGalaxy.Id;
                    solarSystem.Star.ParentHolon = parentGalaxy;
                    solarSystem.Star.ParentHolonId = parentGalaxy.Id;
                    solarSystem.Star.ParentCelestialSpace = parentGalaxy;
                    solarSystem.Star.ParentCelestialSpaceId = parentGalaxy.Id;
                    solarSystem.Star.ParentSolarSystem = solarSystem;
                    solarSystem.Star.ParentSolarSystemId = solarSystem.Id;

                    var starResult = await superCore.AddStarAsync(solarSystem.Star);

                    if (!starResult.IsError && starResult.Result != null)
                    {
                        solarSystem.Star = starResult.Result;
                        solarSystem.ParentStar = starResult.Result;
                        solarSystem.ParentStarId = starResult.Result.Id;
                        solarSystem.ParentHolon = solarSystem;
                        solarSystem.ParentHolonId = solarSystem.Id;
                        solarSystem.ParentCelestialSpace = solarSystem;
                        solarSystem.ParentCelestialSpaceId = solarSystem.Id;

                        var solarSystemResult = await starCore.AddSolarSystemAsync(solarSystem);
                        OASISResultHelper.CopyResult(solarSystemResult, result);
                    }
                    else
                    {
                        OASISResultHelper.CopyResult(starResult, result);
                    }
                }
                else
                {
                    var saveResult = await solarSystem.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (ISolarSystem)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding SolarSystem: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<ISolarSystem>> AddSolarSystemAsync(Guid parentGalaxyId, ISolarSystem solarSystem)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(parentGalaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<ISolarSystem>();
                OASISResultHelper.CopyResult(loadResult, result);
                return result;
            }

            return await AddSolarSystemAsync(loadResult.Result, solarSystem);
        }

        #endregion

        #region Star Methods

        public async Task<OASISResult<IStar>> AddStarAsync(IGalaxy parentGalaxy, IStar star)
        {
            var result = new OASISResult<IStar>();

            try
            {
                if (parentGalaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Galaxy cannot be null.");
                    return result;
                }

                if (star == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Star cannot be null.");
                    return result;
                }

                if (star.Id == Guid.Empty)
                {
                    star.Id = Guid.NewGuid();
                    star.IsNewHolon = true;
                }

                if (parentGalaxy.SuperStar != null &&
                    parentGalaxy.SuperStar.CelestialBodyCore is ISuperStarCore superCore)
                {
                    star.ParentGalaxy = parentGalaxy;
                    star.ParentGalaxyId = parentGalaxy.Id;
                    star.ParentHolon = parentGalaxy;
                    star.ParentHolonId = parentGalaxy.Id;
                    star.ParentCelestialSpace = parentGalaxy;
                    star.ParentCelestialSpaceId = parentGalaxy.Id;

                    var starResult = await superCore.AddStarAsync(star);
                    OASISResultHelper.CopyResult(starResult, result);
                }
                else
                {
                    var saveResult = await star.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IStar)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Star: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IStar>> AddStarAsync(Guid parentGalaxyId, IStar star)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(parentGalaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IStar>();
                OASISResultHelper.CopyResult(loadResult, result);
                return result;
            }

            return await AddStarAsync(loadResult.Result, star);
        }

        #endregion

        #region Planet Methods

        public async Task<OASISResult<IPlanet>> AddPlanetAsync(ISolarSystem parentSolarSystem, IPlanet planet)
        {
            var result = new OASISResult<IPlanet>();

            try
            {
                if (parentSolarSystem == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent SolarSystem cannot be null.");
                    return result;
                }

                if (planet == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Planet cannot be null.");
                    return result;
                }

                if (planet.Id == Guid.Empty)
                {
                    planet.Id = Guid.NewGuid();
                    planet.IsNewHolon = true;
                }

                if (parentSolarSystem.Star != null &&
                    parentSolarSystem.Star.CelestialBodyCore is IStarCore starCore)
                {
                    planet.ParentSolarSystem = parentSolarSystem;
                    planet.ParentSolarSystemId = parentSolarSystem.Id;
                    planet.ParentHolon = parentSolarSystem;
                    planet.ParentHolonId = parentSolarSystem.Id;
                    planet.ParentCelestialSpace = parentSolarSystem;
                    planet.ParentCelestialSpaceId = parentSolarSystem.Id;

                    var planetResult = await starCore.AddPlanetAsync(planet);
                    OASISResultHelper.CopyResult(planetResult, result);
                }
                else
                {
                    var saveResult = await planet.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IPlanet)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Planet: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IPlanet>> AddPlanetAsync(Guid parentSolarSystemId, IPlanet planet)
        {
            var loadResult = await LoadTypedHolonAsync<ISolarSystem>(parentSolarSystemId, HolonType.SolarSystem);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IPlanet>();
                OASISResultHelper.CopyResult(loadResult, result);
                return result;
            }

            return await AddPlanetAsync(loadResult.Result, planet);
        }

        #endregion

        #region Moon Methods

        public async Task<OASISResult<IMoon>> AddMoonAsync(IPlanet parentPlanet, IMoon moon)
        {
            var result = new OASISResult<IMoon>();

            try
            {
                if (parentPlanet == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Planet cannot be null.");
                    return result;
                }

                if (moon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Moon cannot be null.");
                    return result;
                }

                if (moon.Id == Guid.Empty)
                {
                    moon.Id = Guid.NewGuid();
                    moon.IsNewHolon = true;
                }

                var parentSolarSystem = parentPlanet.ParentSolarSystem;

                if (parentSolarSystem != null &&
                    parentSolarSystem.Star != null &&
                    parentSolarSystem.Star.CelestialBodyCore is IStarCore starCore)
                {
                    moon.ParentPlanet = parentPlanet;
                    moon.ParentPlanetId = parentPlanet.Id;
                    moon.ParentHolon = parentPlanet;
                    moon.ParentHolonId = parentPlanet.Id;

                    var moonResult = await starCore.AddMoonAsync(parentPlanet, moon);
                    OASISResultHelper.CopyResult(moonResult, result);
                }
                else
                {
                    var saveResult = await moon.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IMoon)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Moon: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region Other Celestial Bodies

        public async Task<OASISResult<IAsteroid>> AddAsteroidAsync(IGalaxy parentGalaxy, IAsteroid asteroid)
        {
            var result = new OASISResult<IAsteroid>();

            try
            {
                if (parentGalaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Galaxy cannot be null.");
                    return result;
                }

                if (asteroid == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Asteroid cannot be null.");
                    return result;
                }

                if (asteroid.Id == Guid.Empty)
                {
                    asteroid.Id = Guid.NewGuid();
                    asteroid.IsNewHolon = true;
                }

                if (parentGalaxy.SuperStar != null &&
                    parentGalaxy.SuperStar.CelestialBodyCore is ISuperStarCore superCore)
                {
                    asteroid.ParentGalaxy = parentGalaxy;
                    asteroid.ParentGalaxyId = parentGalaxy.Id;
                    asteroid.ParentHolon = parentGalaxy;
                    asteroid.ParentHolonId = parentGalaxy.Id;
                    asteroid.ParentCelestialSpace = parentGalaxy;
                    asteroid.ParentCelestialSpaceId = parentGalaxy.Id;

                    var asteroidResult = await superCore.AddAsteroidAsync(asteroid);
                    OASISResultHelper.CopyResult(asteroidResult, result);
                }
                else
                {
                    var saveResult = await asteroid.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IAsteroid)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Asteroid: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IComet>> AddCometAsync(IGalaxy parentGalaxy, IComet comet)
        {
            var result = new OASISResult<IComet>();

            try
            {
                if (parentGalaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Galaxy cannot be null.");
                    return result;
                }

                if (comet == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Comet cannot be null.");
                    return result;
                }

                if (comet.Id == Guid.Empty)
                {
                    comet.Id = Guid.NewGuid();
                    comet.IsNewHolon = true;
                }

                if (parentGalaxy.SuperStar != null &&
                    parentGalaxy.SuperStar.CelestialBodyCore is ISuperStarCore superCore)
                {
                    comet.ParentGalaxy = parentGalaxy;
                    comet.ParentGalaxyId = parentGalaxy.Id;
                    comet.ParentHolon = parentGalaxy;
                    comet.ParentHolonId = parentGalaxy.Id;
                    comet.ParentCelestialSpace = parentGalaxy;
                    comet.ParentCelestialSpaceId = parentGalaxy.Id;

                    var cometResult = await superCore.AddCometAsync(comet);
                    OASISResultHelper.CopyResult(cometResult, result);
                }
                else
                {
                    var saveResult = await comet.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IComet)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Comet: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IMeteroid>> AddMeteroidAsync(IGalaxy parentGalaxy, IMeteroid meteroid)
        {
            var result = new OASISResult<IMeteroid>();

            try
            {
                if (parentGalaxy == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Galaxy cannot be null.");
                    return result;
                }

                if (meteroid == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Meteroid cannot be null.");
                    return result;
                }

                if (meteroid.Id == Guid.Empty)
                {
                    meteroid.Id = Guid.NewGuid();
                    meteroid.IsNewHolon = true;
                }

                if (parentGalaxy.SuperStar != null &&
                    parentGalaxy.SuperStar.CelestialBodyCore is ISuperStarCore superCore)
                {
                    meteroid.ParentGalaxy = parentGalaxy;
                    meteroid.ParentGalaxyId = parentGalaxy.Id;
                    meteroid.ParentHolon = parentGalaxy;
                    meteroid.ParentHolonId = parentGalaxy.Id;
                    meteroid.ParentCelestialSpace = parentGalaxy;
                    meteroid.ParentCelestialSpaceId = parentGalaxy.Id;

                    var meteroidResult = await superCore.AddMeteroidAsync(meteroid);
                    OASISResultHelper.CopyResult(meteroidResult, result);
                }
                else
                {
                    var saveResult = await meteroid.SaveAsync();
                    OASISResultHelper.CopyResult(saveResult, result);
                    result.Result = (IMeteroid)saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding Meteroid: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region Update Methods

        public Task<OASISResult<IOmiverse>> UpdateOmniverseAsync(IOmiverse omniverse,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(omniverse, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IMultiverse>> UpdateMultiverseAsync(IMultiverse multiverse,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(multiverse, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IUniverse>> UpdateUniverseAsync(IUniverse universe,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(universe, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IGalaxyCluster>> UpdateGalaxyClusterAsync(IGalaxyCluster galaxyCluster,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(galaxyCluster, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IGalaxy>> UpdateGalaxyAsync(IGalaxy galaxy,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(galaxy, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ISolarSystem>> UpdateSolarSystemAsync(ISolarSystem solarSystem,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(solarSystem, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IStar>> UpdateStarAsync(IStar star,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(star, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IPlanet>> UpdatePlanetAsync(IPlanet planet,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(planet, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IMoon>> UpdateMoonAsync(IMoon moon,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(moon, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IAsteroid>> UpdateAsteroidAsync(IAsteroid asteroid,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(asteroid, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IComet>> UpdateCometAsync(IComet comet,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(comet, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IMeteroid>> UpdateMeteroidAsync(IMeteroid meteroid,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(meteroid, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        // Other bodies & spaces

        public Task<OASISResult<INebula>> UpdateNebulaAsync(INebula nebula,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(nebula, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ISuperVerse>> UpdateSuperVerseAsync(ISuperVerse superVerse,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(superVerse, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IWormHole>> UpdateWormHoleAsync(IWormHole wormHole,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(wormHole, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IBlackHole>> UpdateBlackHoleAsync(IBlackHole blackHole,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(blackHole, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IPortal>> UpdatePortalAsync(IPortal portal,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(portal, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IStarGate>> UpdateStarGateAsync(IStarGate starGate,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(starGate, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ISpaceTimeDistortion>> UpdateSpaceTimeDistortionAsync(ISpaceTimeDistortion distortion,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(distortion, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ISpaceTimeAbnormally>> UpdateSpaceTimeAbnormallyAsync(ISpaceTimeAbnormally abnormally,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(abnormally, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ITemporalRift>> UpdateTemporalRiftAsync(ITemporalRift rift,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(rift, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IStarDust>> UpdateStarDustAsync(IStarDust starDust,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(starDust, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ICosmicWave>> UpdateCosmicWaveAsync(ICosmicWave wave,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(wave, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<ICosmicRay>> UpdateCosmicRayAsync(ICosmicRay ray,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(ray, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        public Task<OASISResult<IGravitationalWave>> UpdateGravitationalWaveAsync(IGravitationalWave wave,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false,
            ProviderType providerType = ProviderType.Default)
            => SaveHolonAsync(wave, saveChildren, recursive, maxChildDepth,
                continueOnError, saveChildrenOnProvider, providerType);

        #endregion

        #region Delete Methods

        public Task<OASISResult<bool>> DeleteOmniverseAsync(IOmiverse omniverse,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(omniverse, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteOmniverseAsync(Guid omniverseId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteMultiverseAsync(IMultiverse multiverse,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(multiverse, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteMultiverseAsync(Guid multiverseId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteUniverseAsync(IUniverse universe,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(universe, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteUniverseAsync(Guid universeId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteGalaxyClusterAsync(IGalaxyCluster galaxyCluster,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(galaxyCluster, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteGalaxyClusterAsync(Guid galaxyClusterId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteGalaxyAsync(IGalaxy galaxy,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(galaxy, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteGalaxyAsync(Guid galaxyId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteSolarSystemAsync(ISolarSystem solarSystem,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(solarSystem, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteSolarSystemAsync(Guid solarSystemId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteStarAsync(IStar star,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(star, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteStarAsync(Guid starId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IStar>(starId, HolonType.Star);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeletePlanetAsync(IPlanet planet,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(planet, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeletePlanetAsync(Guid planetId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IPlanet>(planetId, HolonType.Planet);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteMoonAsync(IMoon moon,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(moon, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteMoonAsync(Guid moonId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IMoon>(moonId, HolonType.Moon);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteAsteroidAsync(IAsteroid asteroid,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(asteroid, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteAsteroidAsync(Guid asteroidId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IAsteroid>(asteroidId, HolonType.Asteroid);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteCometAsync(IComet comet,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(comet, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteCometAsync(Guid cometId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IComet>(cometId, HolonType.Comet);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteMeteroidAsync(IMeteroid meteroid,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(meteroid, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteMeteroidAsync(Guid meteroidId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IMeteroid>(meteroidId, HolonType.Meteroid);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        // Other bodies & spaces

        public Task<OASISResult<bool>> DeleteNebulaAsync(INebula nebula,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(nebula, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteNebulaAsync(Guid nebulaId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<INebula>(nebulaId, HolonType.Nebula);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteSuperVerseAsync(ISuperVerse superVerse,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(superVerse, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteSuperVerseAsync(Guid superVerseId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ISuperVerse>(superVerseId, HolonType.SuperVerse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteWormHoleAsync(IWormHole wormHole,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(wormHole, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteWormHoleAsync(Guid wormHoleId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IWormHole>(wormHoleId, HolonType.WormHole);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteBlackHoleAsync(IBlackHole blackHole,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(blackHole, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteBlackHoleAsync(Guid blackHoleId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IBlackHole>(blackHoleId, HolonType.BlackHole);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeletePortalAsync(IPortal portal,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(portal, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeletePortalAsync(Guid portalId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IPortal>(portalId, HolonType.Portal);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteStarGateAsync(IStarGate starGate,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(starGate, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteStarGateAsync(Guid starGateId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IStarGate>(starGateId, HolonType.StarGate);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteSpaceTimeDistortionAsync(ISpaceTimeDistortion distortion,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(distortion, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteSpaceTimeDistortionAsync(Guid distortionId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ISpaceTimeDistortion>(distortionId, HolonType.SpaceTimeDistortion);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteSpaceTimeAbnormallyAsync(ISpaceTimeAbnormally abnormally,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(abnormally, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteSpaceTimeAbnormallyAsync(Guid abnormallyId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ISpaceTimeAbnormally>(abnormallyId, HolonType.SpaceTimeAbnormally);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteTemporalRiftAsync(ITemporalRift rift,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(rift, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteTemporalRiftAsync(Guid riftId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ITemporalRift>(riftId, HolonType.TemporalRift);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteStarDustAsync(IStarDust starDust,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(starDust, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteStarDustAsync(Guid starDustId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IStarDust>(starDustId, HolonType.StarDust);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteCosmicWaveAsync(ICosmicWave wave,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(wave, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteCosmicWaveAsync(Guid waveId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ICosmicWave>(waveId, HolonType.CosmicWave);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteCosmicRayAsync(ICosmicRay ray,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(ray, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteCosmicRayAsync(Guid rayId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<ICosmicRay>(rayId, HolonType.CosmicRay);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        public Task<OASISResult<bool>> DeleteGravitationalWaveAsync(IGravitationalWave wave,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
            => DeleteHolonAsync(wave, null, softDelete, providerType);

        public async Task<OASISResult<bool>> DeleteGravitationalWaveAsync(Guid waveId,
            bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            var load = await LoadTypedHolonAsync<IGravitationalWave>(waveId, HolonType.GravitationalWave);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await DeleteHolonAsync(load.Result, null, softDelete, providerType);
        }

        #endregion

        #region Collections

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForSolarSystemAsync(ISolarSystem solarSystem)
        {
            var result = new OASISResult<IEnumerable<IPlanet>>();

            try
            {
                if (solarSystem?.Star?.CelestialBodyCore is IStarCore starCore)
                {
                    var planetsResult = await starCore.GetAllPlanetsForSolarSystemAsync();
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(planetsResult, result);

                    if (!planetsResult.IsError && planetsResult.Result != null)
                        result.Result = planetsResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "SolarSystem or its Star/CelestialBodyCore is null or not a valid IStarCore.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting planets for SolarSystem: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForSolarSystemAsync(Guid solarSystemId)
        {
            var loadResult = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetPlanetsForSolarSystemAsync(loadResult.Result);
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForGalaxyAsync(IGalaxy galaxy)
        {
            var result = new OASISResult<IEnumerable<ISolarSystem>>();

            try
            {
                if (galaxy?.SuperStar?.CelestialBodyCore is ISuperStarCore superCore)
                {
                    var ssResult = await superCore.GetAllSolarSystemsForGalaxyAsync();
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(ssResult, result);

                    if (!ssResult.IsError && ssResult.Result != null)
                        result.Result = ssResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "Galaxy or its SuperStar/CelestialBodyCore is null or not a valid ISuperStarCore.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting solar systems for Galaxy: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForGalaxyAsync(Guid galaxyId)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISolarSystem>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetSolarSystemsForGalaxyAsync(loadResult.Result);
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForGalaxyAsync(IGalaxy galaxy)
        {
            var result = new OASISResult<IEnumerable<IStar>>();

            try
            {
                if (galaxy?.SuperStar?.CelestialBodyCore is ISuperStarCore superCore)
                {
                    var starsResult = await superCore.GetAllStarsForGalaxyAsync();
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(starsResult, result);

                    if (!starsResult.IsError && starsResult.Result != null)
                        result.Result = starsResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "Galaxy or its SuperStar/CelestialBodyCore is null or not a valid ISuperStarCore.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting stars for Galaxy: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForGalaxyAsync(Guid galaxyId)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStar>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetStarsForGalaxyAsync(loadResult.Result);
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForGalaxyAsync(IGalaxy galaxy)
        {
            var result = new OASISResult<IEnumerable<IPlanet>>();

            try
            {
                if (galaxy?.SuperStar?.CelestialBodyCore is ISuperStarCore superCore)
                {
                    var planetsResult = await superCore.GetAllPlanetsForGalaxyAsync();
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(planetsResult, result);

                    if (!planetsResult.IsError && planetsResult.Result != null)
                        result.Result = planetsResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "Galaxy or its SuperStar/CelestialBodyCore is null or not a valid ISuperStarCore.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting planets for Galaxy: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForGalaxyAsync(Guid galaxyId)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetPlanetsForGalaxyAsync(loadResult.Result);
        }

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForGalaxyAsync(IGalaxy galaxy)
        {
            var result = new OASISResult<IEnumerable<IMoon>>();

            try
            {
                if (galaxy?.SuperStar?.CelestialBodyCore is ISuperStarCore superCore)
                {
                    var moonsResult = await superCore.GetAllMoonsForGalaxyAsync();
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(moonsResult, result);

                    if (!moonsResult.IsError && moonsResult.Result != null)
                        result.Result = moonsResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "Galaxy or its SuperStar/CelestialBodyCore is null or not a valid ISuperStarCore.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting moons for Galaxy: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForGalaxyAsync(Guid galaxyId)
        {
            var loadResult = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetMoonsForGalaxyAsync(loadResult.Result);
        }

        public async Task<OASISResult<IEnumerable<IUniverse>>> GetUniversesForMultiverseAsync(IMultiverse multiverse)
        {
            var result = new OASISResult<IEnumerable<IUniverse>>();

            try
            {
                var universes = new List<IUniverse>();

                var third = multiverse?.Dimensions?.ThirdDimension;

                if (third?.MagicVerse != null)
                    universes.Add(third.MagicVerse);

                if (third?.ParallelUniverses != null)
                    universes.AddRange(third.ParallelUniverses);

                result.Result = universes;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting universes for Multiverse: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IUniverse>>> GetUniversesForMultiverseAsync(Guid multiverseId)
        {
            var loadResult = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IUniverse>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                return result;
            }

            return await GetUniversesForMultiverseAsync(loadResult.Result);
        }

        // Generic moon collections for all parent levels.

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForPlanetAsync(IPlanet parentPlanet)
            => GetChildrenForParentAsync<IMoon>((IHolon)parentPlanet, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForPlanetAsync(Guid planetId)
        {
            var load = await LoadTypedHolonAsync<IPlanet>(planetId, HolonType.Planet);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForPlanetAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IMoon>((IHolon)solarSystem, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IMoon>((IHolon)galaxyCluster, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IMoon>((IHolon)universe, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IMoon>((IHolon)multiverse, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IMoon>((IHolon)omniverse, HolonType.Moon);

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMoon>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMoonsForOmniverseAsync(load.Result);
        }

        // Generic planets for all higher-level parents (beyond the core-specific helpers).

        public Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForGalaxyClusterGenericAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IPlanet>((IHolon)galaxyCluster, HolonType.Planet);

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForGalaxyClusterGenericAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPlanetsForGalaxyClusterGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IPlanet>((IHolon)universe, HolonType.Planet);

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPlanetsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForMultiverseGenericAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IPlanet>((IHolon)multiverse, HolonType.Planet);

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForMultiverseGenericAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPlanetsForMultiverseGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForOmniverseGenericAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IPlanet>((IHolon)omniverse, HolonType.Planet);

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetPlanetsForOmniverseGenericAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPlanet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPlanetsForOmniverseGenericAsync(load.Result);
        }

        // Generic stars for all higher-level parents beyond the core-specific Galaxy helper.

        public Task<OASISResult<IEnumerable<IStar>>> GetStarsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IStar>((IHolon)galaxyCluster, HolonType.Star);

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStar>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStar>>> GetStarsForUniverseGenericAsync(IUniverse universe)
            => GetChildrenForParentAsync<IStar>((IHolon)universe, HolonType.Star);

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForUniverseGenericAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStar>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarsForUniverseGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStar>>> GetStarsForMultiverseGenericAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IStar>((IHolon)multiverse, HolonType.Star);

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForMultiverseGenericAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStar>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarsForMultiverseGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStar>>> GetStarsForOmniverseGenericAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IStar>((IHolon)omniverse, HolonType.Star);

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsForOmniverseGenericAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStar>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarsForOmniverseGenericAsync(load.Result);
        }

        // Generic solar systems for all higher-level parents beyond the core-specific Galaxy helper.

        public Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForGalaxyClusterGenericAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<ISolarSystem>((IHolon)galaxyCluster, HolonType.SolarSystem);

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForGalaxyClusterGenericAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISolarSystem>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSolarSystemsForGalaxyClusterGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForUniverseGenericAsync(IUniverse universe)
            => GetChildrenForParentAsync<ISolarSystem>((IHolon)universe, HolonType.SolarSystem);

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForUniverseGenericAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISolarSystem>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSolarSystemsForUniverseGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForMultiverseGenericAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ISolarSystem>((IHolon)multiverse, HolonType.SolarSystem);

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForMultiverseGenericAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISolarSystem>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSolarSystemsForMultiverseGenericAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForOmniverseGenericAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ISolarSystem>((IHolon)omniverse, HolonType.SolarSystem);

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsForOmniverseGenericAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISolarSystem>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSolarSystemsForOmniverseGenericAsync(load.Result);
        }

        // Galaxy clusters & galaxies & universes & multiverses via generic child holon loading.

        public Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IGalaxyCluster>((IHolon)universe, HolonType.GalaxyCluster);

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxyCluster>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxyClustersForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IGalaxyCluster>((IHolon)multiverse, HolonType.GalaxyCluster);

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxyCluster>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxyClustersForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IGalaxyCluster>((IHolon)omniverse, HolonType.GalaxyCluster);

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetGalaxyClustersForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxyCluster>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxyClustersForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IGalaxy>((IHolon)galaxyCluster, HolonType.Galaxy);

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxy>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxiesForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IGalaxy>((IHolon)universe, HolonType.Galaxy);

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxy>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxiesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IGalaxy>((IHolon)multiverse, HolonType.Galaxy);

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxy>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxiesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IGalaxy>((IHolon)omniverse, HolonType.Galaxy);

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGalaxy>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGalaxiesForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMultiverse>>> GetMultiversesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IMultiverse>((IHolon)omniverse, HolonType.Multiverse);

        public async Task<OASISResult<IEnumerable<IMultiverse>>> GetMultiversesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMultiverse>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMultiversesForOmniverseAsync(load.Result);
        }

        // Generic asteroids, comets & meteroids for all parent types.

        // Asteroids

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)solarSystem, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)galaxy, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)galaxyCluster, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)universe, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)multiverse, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IAsteroid>((IHolon)omniverse, HolonType.Asteroid);

        public async Task<OASISResult<IEnumerable<IAsteroid>>> GetAsteroidsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IAsteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetAsteroidsForOmniverseAsync(load.Result);
        }

        // Comets

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IComet>((IHolon)solarSystem, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IComet>((IHolon)galaxy, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IComet>((IHolon)galaxyCluster, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IComet>((IHolon)universe, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IComet>((IHolon)multiverse, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IComet>>> GetCometsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IComet>((IHolon)omniverse, HolonType.Comet);

        public async Task<OASISResult<IEnumerable<IComet>>> GetCometsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IComet>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCometsForOmniverseAsync(load.Result);
        }

        // Meteroids

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)solarSystem, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)galaxy, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)galaxyCluster, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)universe, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)multiverse, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IMeteroid>((IHolon)omniverse, HolonType.Meteroid);

        public async Task<OASISResult<IEnumerable<IMeteroid>>> GetMeteroidsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IMeteroid>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetMeteroidsForOmniverseAsync(load.Result);
        }

        // Nebulas

        public Task<OASISResult<IEnumerable<INebula>>> GetNebulasForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<INebula>((IHolon)galaxy, HolonType.Nebula);

        public async Task<OASISResult<IEnumerable<INebula>>> GetNebulasForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<INebula>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetNebulasForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<INebula>>> GetNebulasForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<INebula>((IHolon)galaxyCluster, HolonType.Nebula);

        public async Task<OASISResult<IEnumerable<INebula>>> GetNebulasForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<INebula>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetNebulasForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<INebula>>> GetNebulasForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<INebula>((IHolon)universe, HolonType.Nebula);

        public async Task<OASISResult<IEnumerable<INebula>>> GetNebulasForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<INebula>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetNebulasForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<INebula>>> GetNebulasForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<INebula>((IHolon)multiverse, HolonType.Nebula);

        public async Task<OASISResult<IEnumerable<INebula>>> GetNebulasForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<INebula>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetNebulasForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<INebula>>> GetNebulasForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<INebula>((IHolon)omniverse, HolonType.Nebula);

        public async Task<OASISResult<IEnumerable<INebula>>> GetNebulasForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<INebula>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetNebulasForOmniverseAsync(load.Result);
        }

        // SuperVerses

        public Task<OASISResult<IEnumerable<ISuperVerse>>> GetSuperVersesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ISuperVerse>((IHolon)omniverse, HolonType.SuperVerse);

        public async Task<OASISResult<IEnumerable<ISuperVerse>>> GetSuperVersesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISuperVerse>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSuperVersesForOmniverseAsync(load.Result);
        }

        // WormHoles

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IWormHole>((IHolon)solarSystem, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IWormHole>((IHolon)galaxy, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IWormHole>((IHolon)galaxyCluster, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IWormHole>((IHolon)universe, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IWormHole>((IHolon)multiverse, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IWormHole>((IHolon)omniverse, HolonType.WormHole);

        public async Task<OASISResult<IEnumerable<IWormHole>>> GetWormHolesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IWormHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetWormHolesForOmniverseAsync(load.Result);
        }

        // BlackHoles

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)solarSystem, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)galaxy, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)galaxyCluster, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)universe, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)multiverse, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IBlackHole>((IHolon)omniverse, HolonType.BlackHole);

        public async Task<OASISResult<IEnumerable<IBlackHole>>> GetBlackHolesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IBlackHole>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetBlackHolesForOmniverseAsync(load.Result);
        }

        // Portals

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IPortal>((IHolon)solarSystem, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IPortal>((IHolon)galaxy, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IPortal>((IHolon)galaxyCluster, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IPortal>((IHolon)universe, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IPortal>((IHolon)multiverse, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IPortal>((IHolon)omniverse, HolonType.Portal);

        public async Task<OASISResult<IEnumerable<IPortal>>> GetPortalsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IPortal>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetPortalsForOmniverseAsync(load.Result);
        }

        // StarGates

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForSolarSystemAsync(ISolarSystem solarSystem)
            => GetChildrenForParentAsync<IStarGate>((IHolon)solarSystem, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForSolarSystemAsync(Guid solarSystemId)
        {
            var load = await LoadTypedHolonAsync<ISolarSystem>(solarSystemId, HolonType.SolarSystem);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForSolarSystemAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForGalaxyAsync(IGalaxy galaxy)
            => GetChildrenForParentAsync<IStarGate>((IHolon)galaxy, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForGalaxyAsync(Guid galaxyId)
        {
            var load = await LoadTypedHolonAsync<IGalaxy>(galaxyId, HolonType.Galaxy);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForGalaxyAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForGalaxyClusterAsync(IGalaxyCluster galaxyCluster)
            => GetChildrenForParentAsync<IStarGate>((IHolon)galaxyCluster, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForGalaxyClusterAsync(Guid galaxyClusterId)
        {
            var load = await LoadTypedHolonAsync<IGalaxyCluster>(galaxyClusterId, HolonType.GalaxyCluster);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForGalaxyClusterAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IStarGate>((IHolon)universe, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IStarGate>((IHolon)multiverse, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IStarGate>((IHolon)omniverse, HolonType.StarGate);

        public async Task<OASISResult<IEnumerable<IStarGate>>> GetStarGatesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarGate>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarGatesForOmniverseAsync(load.Result);
        }

        // SpaceTimeDistortions, SpaceTimeAbnormalies, TemporalRifts, StarDust, CosmicWaves, CosmicRays, GravitationalWaves
        // follow same pattern; for brevity they are grouped where they make sense (Universe/Multiverse/Omniverse).

        public Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<ISpaceTimeDistortion>((IHolon)universe, HolonType.SpaceTimeDistortion);

        public async Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeDistortion>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeDistortionsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ISpaceTimeDistortion>((IHolon)multiverse, HolonType.SpaceTimeDistortion);

        public async Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeDistortion>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeDistortionsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ISpaceTimeDistortion>((IHolon)omniverse, HolonType.SpaceTimeDistortion);

        public async Task<OASISResult<IEnumerable<ISpaceTimeDistortion>>> GetSpaceTimeDistortionsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeDistortion>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeDistortionsForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<ISpaceTimeAbnormally>((IHolon)universe, HolonType.SpaceTimeAbnormally);

        public async Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeAbnormally>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeAbnormaliesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ISpaceTimeAbnormally>((IHolon)multiverse, HolonType.SpaceTimeAbnormally);

        public async Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeAbnormally>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeAbnormaliesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ISpaceTimeAbnormally>((IHolon)omniverse, HolonType.SpaceTimeAbnormally);

        public async Task<OASISResult<IEnumerable<ISpaceTimeAbnormally>>> GetSpaceTimeAbnormaliesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ISpaceTimeAbnormally>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetSpaceTimeAbnormaliesForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<ITemporalRift>((IHolon)universe, HolonType.TemporalRift);

        public async Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ITemporalRift>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetTemporalRiftsForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ITemporalRift>((IHolon)multiverse, HolonType.TemporalRift);

        public async Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ITemporalRift>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetTemporalRiftsForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ITemporalRift>((IHolon)omniverse, HolonType.TemporalRift);

        public async Task<OASISResult<IEnumerable<ITemporalRift>>> GetTemporalRiftsForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ITemporalRift>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetTemporalRiftsForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IStarDust>((IHolon)universe, HolonType.StarDust);

        public async Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarDust>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarDustForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IStarDust>((IHolon)multiverse, HolonType.StarDust);

        public async Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarDust>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarDustForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<IStarDust>((IHolon)omniverse, HolonType.StarDust);

        public async Task<OASISResult<IEnumerable<IStarDust>>> GetStarDustForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IStarDust>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetStarDustForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<ICosmicWave>((IHolon)universe, HolonType.CosmicWave);

        public async Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicWavesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ICosmicWave>((IHolon)multiverse, HolonType.CosmicWave);

        public async Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicWavesForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ICosmicWave>((IHolon)omniverse, HolonType.CosmicWave);

        public async Task<OASISResult<IEnumerable<ICosmicWave>>> GetCosmicWavesForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicWavesForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<ICosmicRay>((IHolon)universe, HolonType.CosmicRay);

        public async Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicRay>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicRaysForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<ICosmicRay>((IHolon)multiverse, HolonType.CosmicRay);

        public async Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicRay>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicRaysForMultiverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForOmniverseAsync(IOmiverse omniverse)
            => GetChildrenForParentAsync<ICosmicRay>((IHolon)omniverse, HolonType.CosmicRay);

        public async Task<OASISResult<IEnumerable<ICosmicRay>>> GetCosmicRaysForOmniverseAsync(Guid omniverseId)
        {
            var load = await LoadTypedHolonAsync<IOmiverse>(omniverseId, HolonType.Omniverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<ICosmicRay>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetCosmicRaysForOmniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForUniverseAsync(IUniverse universe)
            => GetChildrenForParentAsync<IGravitationalWave>((IHolon)universe, HolonType.GravitationalWave);

        public async Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForUniverseAsync(Guid universeId)
        {
            var load = await LoadTypedHolonAsync<IUniverse>(universeId, HolonType.Universe);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGravitationalWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGravitationalWavesForUniverseAsync(load.Result);
        }

        public Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForMultiverseAsync(IMultiverse multiverse)
            => GetChildrenForParentAsync<IGravitationalWave>((IHolon)multiverse, HolonType.GravitationalWave);

        public async Task<OASISResult<IEnumerable<IGravitationalWave>>> GetGravitationalWavesForMultiverseAsync(Guid multiverseId)
        {
            var load = await LoadTypedHolonAsync<IMultiverse>(multiverseId, HolonType.Multiverse);
            if (load.IsError || load.Result == null)
            {
                var result = new OASISResult<IEnumerable<IGravitationalWave>>();
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(load, result);
                return result;
            }
            return await GetGravitationalWavesForMultiverseAsync(load.Result);
        }

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

        #endregion

        #region Scenario Helpers

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

        #endregion
    }
}
