using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
// using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS
{
    public partial class CosmosBlockChainOASIS
    {
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            // First load the avatar to get its ID, then delete
            var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref result, $"Avatar with provider key {providerKey} not found");
                return result;
            }

            // Delete using the avatar's ID
            return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(username);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref result, $"Avatar with username {username} not found");
                return result;
            }

            // Then delete using the avatar ID
            return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(username, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(email);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref result, $"Avatar with email {email} not found");
                return result;
            }

            // Then delete using the avatar ID
            return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(email, softDelete).Result;
        }

        // Avatar Detail methods
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            // Load avatar first, then create avatar detail from it
            var avatarResult = await LoadAvatarAsync(id, version);
            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                var response = new OASISResult<IAvatarDetail>();
                var avatarDetail = new AvatarDetail
                {
                    Id = avatarResult.Result.Id,
                    Username = avatarResult.Result.Username,
                    Email = avatarResult.Result.Email,
                    CreatedDate = avatarResult.Result.CreatedDate,
                    ModifiedDate = avatarResult.Result.ModifiedDate
                };
                response.Result = avatarDetail;
                response.IsError = false;
                response.Message = "Avatar detail loaded from Cosmos successfully";
                return response;
            }
            else
            {
                var response = new OASISResult<IAvatarDetail>();
                OASISErrorHandling.HandleError(ref response, avatarResult.Message ?? "Avatar not found for detail load");
                return response;
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            // First load the avatar by username, then create avatar detail
            var avatarResult = await LoadAvatarByUsernameAsync(username, version);
            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                var response = new OASISResult<IAvatarDetail>();
                var avatarDetail = new AvatarDetail
                {
                    Id = avatarResult.Result.Id,
                    Username = avatarResult.Result.Username,
                    Email = avatarResult.Result.Email,
                    CreatedDate = avatarResult.Result.CreatedDate,
                    ModifiedDate = avatarResult.Result.ModifiedDate
                };
                response.Result = avatarDetail;
                response.IsError = false;
                response.Message = "Avatar detail loaded from Cosmos by username successfully";
                return response;
            }
            else
            {
                var response = new OASISResult<IAvatarDetail>();
                OASISErrorHandling.HandleError(ref response, avatarResult.Message ?? "Avatar not found by username for detail load");
                return response;
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            // First load the avatar by email, then create avatar detail
            var avatarResult = await LoadAvatarByEmailAsync(email, version);
            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                var response = new OASISResult<IAvatarDetail>();
                var avatarDetail = new AvatarDetail
                {
                    Id = avatarResult.Result.Id,
                    Username = avatarResult.Result.Username,
                    Email = avatarResult.Result.Email,
                    CreatedDate = avatarResult.Result.CreatedDate,
                    ModifiedDate = avatarResult.Result.ModifiedDate
                };
                response.Result = avatarDetail;
                response.IsError = false;
                response.Message = "Avatar detail loaded from Cosmos by email successfully";
                return response;
            }
            else
            {
                var response = new OASISResult<IAvatarDetail>();
                OASISErrorHandling.HandleError(ref response, avatarResult.Message ?? "Avatar not found by email for detail load");
                return response;
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (avatarDetail == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail cannot be null");
                    return result;
                }

                // Load the avatar first to get wallet
                var avatarResult = await LoadAvatarAsync(avatarDetail.Id, 0);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with ID {avatarDetail.Id} not found");
                    return result;
                }

                // Get wallet for the avatar
                var walletAddress = await GetWalletAddressForAvatar(avatarDetail.Id);
                if (string.IsNullOrWhiteSpace(walletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }

                // Save avatar detail to Cosmos blockchain using transaction
                var txUrl = "/cosmos/tx/v1beta1/txs";
                var avatarDetailJson = JsonSerializer.Serialize(avatarDetail);
                
                var content = new StringContent(avatarDetailJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(txUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (txResponse.TryGetProperty("tx_response", out var txResp) && 
                        txResp.TryGetProperty("txhash", out var txHash))
                    {
                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.IsSaved = true;
                        result.Message = $"Avatar detail saved successfully to Cosmos blockchain: {txHash.GetString()}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to save avatar detail to Cosmos blockchain - no transaction hash returned");
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to Cosmos blockchain: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Cosmos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatars first, then convert to avatar details
                var avatarsResult = await LoadAllAvatarsAsync(version);
                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    var avatarDetails = avatarsResult.Result.Select(a => new AvatarDetail
                    {
                        Id = a.Id,
                        Username = a.Username,
                        Email = a.Email,
                        CreatedDate = a.CreatedDate,
                        ModifiedDate = a.ModifiedDate
                    }).ToList();

                    response.Result = avatarDetails;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Cosmos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, avatarsResult.Message ?? "Failed to load avatars for avatar details");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from Cosmos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query Cosmos blockchain for all avatars
                var queryUrl = "/cosmos/staking/v1beta1/validators";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var cosmosData = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    var avatars = new List<IAvatar>();
                    if (cosmosData.TryGetProperty("validators", out var validatorsArray) && validatorsArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var validator in validatorsArray.EnumerateArray())
                        {
                            var avatar = ParseCosmosToAvatar(validator.GetRawText());
                            if (avatar != null) avatars.Add(avatar);
                        }
                    }

                    response.Result = avatars;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {avatars.Count} avatars from Cosmos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from Cosmos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from Cosmos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        // Holon methods
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load holons for parent from Cosmos blockchain
                var queryUrl = $"/cosmos/staking/v1beta1/validators/{id}/holons/children";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse Cosmos JSON and create Holon collection
                    var holons = ParseCosmosToHolons(content);
                    if (holons != null)
                    {
                        // Filter by holon type if specified
                        if (type != HolonType.All)
                        {
                            holons = holons.Where(h => h.HolonType == type);
                        }
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Successfully loaded {holons.Count()} holons for parent from Cosmos";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Cosmos JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons for parent from Cosmos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons for parent from Cosmos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load holons for parent by provider key from Cosmos blockchain
                var queryUrl = $"/cosmos/staking/v1beta1/validators/{providerKey}/holons/children";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse Cosmos JSON and create Holon collection
                    var holons = ParseCosmosToHolons(content);
                    if (holons != null)
                    {
                        // Filter by holon type if specified
                        if (type != HolonType.All)
                        {
                            holons = holons.Where(h => h.HolonType == type);
                        }
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Successfully loaded {holons.Count()} holons for parent by provider key from Cosmos";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Cosmos JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons for parent by provider key from Cosmos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons for parent by provider key from Cosmos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // Convert single metadata key-value pair to dictionary and delegate to the dictionary version
            var metaKeyValuePairs = new Dictionary<string, string> { { metaKey, metaValue } };
            return await LoadHolonsByMetaDataAsync(metaKeyValuePairs, MetaKeyValuePairMatchMode.All, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all holons and filter by metadata
                var allHolonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                {
                    var matchingHolons = allHolonsResult.Result.Where(holon =>
                    {
                        if (holon.MetaData == null) return false;
                        
                        if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                        {
                            return metaKeyValuePairs.All(kvp => 
                                holon.MetaData.ContainsKey(kvp.Key) && 
                                holon.MetaData[kvp.Key]?.ToString() == kvp.Value);
                        }
                        else // OR mode
                        {
                            return metaKeyValuePairs.Any(kvp => 
                                holon.MetaData.ContainsKey(kvp.Key) && 
                                holon.MetaData[kvp.Key]?.ToString() == kvp.Value);
                        }
                    }).ToList();

                    response.Result = matchingHolons;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {matchingHolons.Count} holons matching metadata from Cosmos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, allHolonsResult.Message ?? "Failed to load holons for metadata search");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons by metadata from Cosmos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Holons cannot be null");
                    return response;
                }

                var savedHolons = new List<IHolon>();
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (!saveResult.IsError && saveResult.Result != null)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                    else if (!continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to save holon {holon.Id}: {saveResult.Message}");
                        return response;
                    }
                }

                response.Result = savedHolons;
                response.IsError = false;
                response.Message = $"Saved {savedHolons.Count} holons to Cosmos blockchain";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holons to Cosmos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

    }
}
