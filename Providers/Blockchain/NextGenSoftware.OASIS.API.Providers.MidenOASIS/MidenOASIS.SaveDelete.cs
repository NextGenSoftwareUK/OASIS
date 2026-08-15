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
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
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
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    
                    if (saveResult.IsError)
                    {
                        errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, string.Join("; ", errors));
                            return result;
                        }
                    }
                    else if (saveResult.Result != null)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                }

                result.Result = savedHolons;
                result.IsError = errors.Any();
                result.Message = errors.Any() ? string.Join("; ", errors) : $"Successfully saved {savedHolons.Count} holons to Miden";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Miden: {ex.Message}", ex);
            }
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete avatar by username from Miden API
                var apiResult = await _apiClient.PostAsync<bool>($"/api/avatars/delete/username/{Uri.EscapeDataString(avatarUsername)}", new { softDelete });
                
                if (!apiResult.IsError)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully deleted avatar by username from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar by username from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
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

                // Delete holon by provider key from Miden API
                var apiResult = await _apiClient.PostAsync<Holon>($"/api/holons/delete/provider-key/{Uri.EscapeDataString(providerKey)}", new { });
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully deleted holon by provider key from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon by provider key from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
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

                // Delete holon by ID from Miden API
                var apiResult = await _apiClient.PostAsync<Holon>($"/api/holons/delete/{id}", new { });
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully deleted holon by ID from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon by ID from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by ID from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
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

                // Export all data for avatar by username from Miden API
                var apiResult = await _apiClient.GetAsync<List<Holon>>($"/api/avatars/username/{Uri.EscapeDataString(avatarUsername)}/export?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully exported {apiResult.Result.Count} holons for avatar by username from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data by username from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
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

                // Query avatar detail by ID from Miden API
                var apiResult = await _apiClient.GetAsync<AvatarDetail>($"/api/avatars/details/{id}?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar detail from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
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

                // Query avatar detail by username from Miden API
                var apiResult = await _apiClient.GetAsync<AvatarDetail>($"/api/avatars/details/username/{Uri.EscapeDataString(avatarUsername)}?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar detail by username from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail by username from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmail, int version = 0)
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

                // Export all data for avatar by email from Miden API
                var apiResult = await _apiClient.GetAsync<List<Holon>>($"/api/avatars/email/{Uri.EscapeDataString(avatarEmail)}/export?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully exported {apiResult.Result.Count} holons for avatar by email from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data by email from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmail, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
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

                // Query avatar by username from Miden API
                var apiResult = await _apiClient.GetAsync<Avatar>($"/api/avatars/username/{Uri.EscapeDataString(avatarUsername)}?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar by username from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by username from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
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
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Get wallet for the holon (use avatar's wallet if holon has CreatedByAvatarId)
                Guid avatarId = holon.CreatedByAvatarId != Guid.Empty ? holon.CreatedByAvatarId : holon.Id;
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarId, Core.Enums.ProviderType.MidenOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for holon");
                    return result;
                }

                // Serialize holon to JSON and store in Miden private note metadata
                string holonInfo = JsonSerializer.Serialize(holon);
                string holonId = holon.Id.ToString();
                
                // Use Miden private note to store holon data (metadata field stores the JSON)
                // Value is 0 since we're storing data, not tokens
                var privateNote = await _midenService.CreatePrivateNoteAsync(
                    value: 0m,
                    ownerPublicKey: walletResult.Result.WalletAddress,
                    assetId: "OASIS_HOLON", // Custom asset ID for holon storage
                    metadata: holonInfo);

                if (privateNote == null || string.IsNullOrEmpty(privateNote.NoteId))
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to create Miden private note for holon storage");
                    return result;
                }

                // Store the note ID in holon's provider unique storage key for retrieval
                if (holon.ProviderUniqueStorageKey == null)
                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.MidenOASIS] = privateNote.NoteId;

                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = $"Holon saved successfully to Miden private note: {privateNote.NoteId}";

                // Handle children if requested
                if (saveChildren && holon.Children != null && holon.Children.Any())
                {
                    var childResults = new List<OASISResult<IHolon>>();
                    foreach (var child in holon.Children)
                    {
                        var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                        childResults.Add(childResult);
                        
                        if (!continueOnError && childResult.IsError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Failed to save child holon {child.Id}: {childResult.Message}");
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid parentId, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Query holons for parent from Miden API
                var apiResult = await _apiClient.GetAsync<List<Holon>>($"/api/holons/parent/{parentId}?type={type}&version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    var holons = apiResult.Result.Where(h => type == HolonType.All || h.HolonType == type).Cast<IHolon>();
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count()} holons for parent from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons for parent from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid parentId, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(parentId, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

    }
}
