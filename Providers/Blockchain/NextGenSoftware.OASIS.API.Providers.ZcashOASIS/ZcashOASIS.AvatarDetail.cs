using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS
{
    public partial class ZcashOASIS
    {
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all avatars, then convert to details
                // Load avatar details as separate objects (holons with HolonType.AvatarDetail)
                var holonsResult = await LoadAllHolonsAsync(HolonType.AvatarDetail);
                var avatarDetails = new List<IAvatarDetail>();
                if (!holonsResult.IsError && holonsResult.Result != null)
                {
                    foreach (var holon in holonsResult.Result)
                    {
                        var detail = ConvertHolonToAvatarDetail(holon);
                        if (detail != null)
                            avatarDetails.Add(detail);
                    }
                }

                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = $"Loaded {avatarDetails.Count} avatar details from Zcash";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Save avatar as holon to Zcash
                var holon = Avatar as IHolon ?? new Holon
                {
                    Id = Avatar.Id,
                    Name = Avatar.Username,
                    Description = Avatar.Email,
                    HolonType = HolonType.Avatar
                };

                var saveResult = await SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = Avatar;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (avatarDetail == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail cannot be null");
                    return result;
                }

                // AvatarDetail is stored separately from Avatar: save as holon (HolonType.AvatarDetail)
                var holon = ConvertAvatarDetailToHolon(avatarDetail);
                var saveResult = await SaveHolonAsync(holon);
                
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    result.Result = ConvertHolonToAvatarDetail(saveResult.Result) ?? avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail saved successfully to Zcash";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail: {saveResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Zcash: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete holon (avatar is stored as holon)
                var deleteResult = await DeleteHolonAsync(id);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, deleteResult.Message);
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "Avatar deleted successfully from Zcash";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete holon by provider key
                var deleteResult = await DeleteHolonAsync(providerKey);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, deleteResult.Message);
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "Avatar deleted successfully from Zcash by provider key";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by email first
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                    return result;
                }

                // Delete the avatar
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                result.Result = deleteResult.Result;
                result.IsError = deleteResult.IsError;
                result.Message = deleteResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by username first
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                    return result;
                }

                // Delete the avatar
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                result.Result = deleteResult.Result;
                result.IsError = deleteResult.IsError;
                result.Message = deleteResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Use LoadHolonsByMetaData to search
                var holons = new List<IHolon>();
                
                if (searchParams != null && searchParams.SearchGroups != null)
                {
                    foreach (var group in searchParams.SearchGroups)
                    {
                        // SearchGroups can be different types (SearchTextGroup, SearchNumberGroup, etc.)
                        // For now, use a simplified search approach
                        // In production, would handle each group type appropriately
                        if (group is ISearchTextGroup textGroup && !string.IsNullOrEmpty(textGroup.SearchQuery))
                        {
                            // Use SearchQuery instead of SearchFields
                            var holonsResult = await LoadHolonsByMetaDataAsync("SearchQuery", textGroup.SearchQuery, HolonType.All);
                            if (!holonsResult.IsError && holonsResult.Result != null)
                            {
                                holons.AddRange(holonsResult.Result);
                            }
                        }
                    }
                }

                // Remove duplicates
                holons = holons.GroupBy(h => h.Id).Select(g => g.First()).ToList();

                result.Result = new SearchResults
                {
                    SearchResultHolons = holons,
                    NumberOfResults = holons.Count
                };
                result.IsError = false;
                result.Message = $"Found {holons.Count} results";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holon = await _zcashRepository.LoadHolonAsync(id);
                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holon = await _zcashRepository.LoadHolonByProviderKeyAsync(providerKey);
                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by parent ID in metadata
                var holonsResult = await LoadHolonsByMetaDataAsync("ParentId", id.ToString(), type);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                result.Result = holonsResult.Result ?? new List<IHolon>();
                result.IsError = false;
                result.Message = $"Loaded {result.Result.Count()} holons for parent";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by parent provider key in metadata
                var holonsResult = await LoadHolonsByMetaDataAsync("ParentProviderKey", providerKey, type);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                result.Result = holonsResult.Result ?? new List<IHolon>();
                result.IsError = false;
                result.Message = $"Loaded {result.Result.Count()} holons for parent by provider key";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

    }
}
