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
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using LockWeb3TokenRequest = NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.LockWeb3TokenRequest;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Infrastructure.Services.Miden;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Models;
using NextGenSoftware.OASIS.Common;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS
{
    public partial class MidenOASIS
    {
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query avatar detail by email from Miden API
                var apiResult = await _apiClient.GetAsync<AvatarDetail>($"/api/avatars/details/email/{Uri.EscapeDataString(email)}?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar detail by email from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail by email from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(email, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete avatar by provider key from Miden API
                var apiResult = await _apiClient.PostAsync<bool>($"/api/avatars/delete/provider-key/{Uri.EscapeDataString(providerKey)}", new { softDelete });
                
                if (!apiResult.IsError)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully deleted avatar by provider key from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar by provider key from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query holon by provider key from Miden API
                var apiResult = await _apiClient.GetAsync<Holon>($"/api/holons/provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully loaded holon by provider key from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon by provider key from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query avatar by provider key from Miden API
                var apiResult = await _apiClient.GetAsync<Avatar>($"/api/avatars/provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar by provider key from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by provider key from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query avatar by email from Miden API
                var apiResult = await _apiClient.GetAsync<Avatar>($"/api/avatars/email/{Uri.EscapeDataString(email)}?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar by email from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by email from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
        {
            return LoadAvatarByEmailAsync(email, version).Result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (avatarDetail == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail cannot be null");
                    return result;
                }

                // Save avatar detail to Miden API
                var apiResult = await _apiClient.PostAsync<AvatarDetail>("/api/avatars/details", avatarDetail);
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully saved avatar detail to Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all data for avatar by ID from Miden API
                var apiResult = await _apiClient.GetAsync<List<Holon>>($"/api/avatars/{avatarId}/export?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully exported {apiResult.Result.Count} holons for avatar from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query all holons from Miden API
                var apiResult = await _apiClient.GetAsync<List<Holon>>($"/api/holons?type={type}&version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    var holons = apiResult.Result.Where(h => type == HolonType.All || h.HolonType == type).Cast<IHolon>();
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count()} holons from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load all holons from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query holon by ID from Miden API
                var apiResult = await _apiClient.GetAsync<Holon>($"/api/holons/{id}?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully loaded holon from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query holons for parent by provider key from Miden API
                var apiResult = await _apiClient.GetAsync<List<Holon>>($"/api/holons/parent/key/{Uri.EscapeDataString(providerKey)}?type={type}&version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    var holons = apiResult.Result.Where(h => type == HolonType.All || h.HolonType == type).Cast<IHolon>();
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count()} holons for parent by provider key from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons for parent by provider key from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Miden: {ex.Message}", ex);
            }
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete avatar by email from Miden API
                var apiResult = await _apiClient.PostAsync<bool>($"/api/avatars/delete/email/{Uri.EscapeDataString(avatarEmail)}", new { softDelete });
                
                if (!apiResult.IsError)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully deleted avatar by email from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar by email from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                    return result;
                }

                // Get wallet for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(Avatar.Id, Core.Enums.ProviderType.MidenOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }

                // Serialize avatar to JSON and store in Miden private note metadata
                string avatarInfo = JsonSerializer.Serialize(Avatar);
                string avatarId = Avatar.Id.ToString();
                
                // Use Miden private note to store avatar data (metadata field stores the JSON)
                // Value is 0 since we're storing data, not tokens
                var privateNote = await _midenService.CreatePrivateNoteAsync(
                    value: 0m,
                    ownerPublicKey: walletResult.Result.WalletAddress,
                    assetId: "OASIS_AVATAR", // Custom asset ID for avatar storage
                    metadata: avatarInfo);

                if (privateNote == null || string.IsNullOrEmpty(privateNote.NoteId))
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to create Miden private note for avatar storage");
                    return result;
                }

                // Store the note ID in avatar's provider unique storage key for retrieval
                if (Avatar.ProviderUniqueStorageKey == null)
                    Avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                Avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MidenOASIS] = privateNote.NoteId;

                result.Result = Avatar;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = $"Avatar saved successfully to Miden private note: {privateNote.NoteId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }
    }
}
