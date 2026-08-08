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
    /// <summary>
    /// COSMICManager exposes the full COSMIC ORM / Omniverse object model to the WEB4 OASIS API.
    /// It provides strongly-typed Create/Read/Update/Delete and rich Get X For Y operations
    /// for all CelestialBodies and CelestialSpaces defined in the STAR ontology.
    /// </summary>
    public partial class COSMICManager : COSMICManagerBase
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

            try
            {
                // Try to search for existing omniverse
                var searchResult = await SearchHolonsForParentAsync<Holon>(
                    "",
                    default(Guid),
                    default(Guid),
                    null,
                    MetaKeyValuePairMatchMode.All,
                    false,
                    HolonType.Omniverse,
                    ProviderType.Default
                );

                if (!searchResult.IsError && searchResult.Result != null && searchResult.Result.Any())
                {
                    // Return the first omniverse found (should only be one)
                    result.Result = searchResult.Result.FirstOrDefault() as IOmiverse;
                    return result;
                }

                // If not found, try to load by a known ID or create one
                // For now, return error - omniverse should be created on system boot
                OASISErrorHandling.HandleError(ref result, "Omniverse not found. The Omniverse should be created during system initialization.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Omniverse: {ex.Message}", ex);
            }

            return result;
        }


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
                    result.IsError = true;
                    result.Message = $"The {typeof(T).Name} field is required. Please provide a valid object in the request body.";
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
                    result.IsError = true;
                    result.Message = $"The {typeof(T).Name} field is required. Please provide a valid object in the request body.";
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
    }
}