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



        /// <summary>
        /// Saves the specified Omniverse instance using the underlying STAR implementation.
        /// The concrete Omniverse object must be created by the caller (in the STAR project) and
        /// passed in via the IOmiverse interface.
    }
}