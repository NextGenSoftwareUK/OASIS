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

        // Avatar-related methods
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
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

                // Query Cosmos blockchain for avatar by username
                var queryUrl = $"/cosmos/staking/v1beta1/validators?moniker={username}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var cosmosData = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    if (cosmosData.TryGetProperty("validators", out var validators) && validators.GetArrayLength() > 0)
                    {
                        var validator = validators[0];
                        var avatar = ParseCosmosToAvatar(validator.GetRawText());
                        if (avatar != null)
                        {
                            response.Result = avatar;
                            response.Message = "Avatar loaded from Cosmos by username successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to parse Cosmos JSON response");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found by username in Cosmos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Cosmos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Cosmos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
        {
            return LoadAvatarByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
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

                // Query Cosmos blockchain for avatar by email
                // Note: Cosmos doesn't have native email lookup, so we search through validators
                var queryUrl = $"/cosmos/staking/v1beta1/validators";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var cosmosData = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    if (cosmosData.TryGetProperty("validators", out var validators))
                    {
                        foreach (var validator in validators.EnumerateArray())
                        {
                            var validatorJson = validator.GetRawText();
                            if (validatorJson.Contains(email, StringComparison.OrdinalIgnoreCase))
                            {
                                var avatar = ParseCosmosToAvatar(validatorJson);
                                if (avatar != null && avatar.Email?.Equals(email, StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    response.Result = avatar;
                                    response.Message = "Avatar loaded from Cosmos by email successfully";
                                    return response;
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref response, "Avatar not found by email in Cosmos blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Cosmos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Cosmos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
        {
            return LoadAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
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

                // Query Cosmos blockchain for avatar by provider key (address or transaction hash)
                var queryUrl = $"/cosmos/auth/v1beta1/accounts/{providerKey}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatar = ParseCosmosToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.Message = "Avatar loaded from Cosmos by provider key successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Cosmos JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Cosmos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Cosmos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();
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

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar cannot be null");
                    return response;
                }

                // Get wallet for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, Core.Enums.ProviderType.CosmosBlockChainOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Could not retrieve wallet address for avatar");
                    return response;
                }

                // Save avatar to Cosmos blockchain using transaction
                var txUrl = "/cosmos/tx/v1beta1/txs";
                var cosmosJson = ConvertAvatarToCosmos(avatar);
                
                var content = new StringContent(cosmosJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(txUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (txResponse.TryGetProperty("tx_response", out var txResp) && 
                        txResp.TryGetProperty("txhash", out var txHash))
                    {
                        // Store transaction hash in provider unique storage key
                        if (avatar.ProviderUniqueStorageKey == null)
                            avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                        avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.CosmosBlockChainOASIS] = txHash.GetString() ?? string.Empty;

                        response.Result = avatar;
                        response.IsError = false;
                        response.IsSaved = true;
                        response.Message = $"Avatar saved successfully to Cosmos blockchain: {txHash.GetString()}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar to Cosmos blockchain - no transaction hash returned");
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to Cosmos blockchain: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Cosmos: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
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

                // Load the avatar first
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with ID {id} not found");
                    return result;
                }

                if (softDelete)
                {
                    // For soft delete, set DeletedDate (IsDeleted is derived from it)
                    avatarResult.Result.DeletedDate = DateTime.UtcNow;
                    var saveResult = await SaveAvatarAsync(avatarResult.Result);
                    result.Result = !saveResult.IsError;
                    result.IsError = saveResult.IsError;
                    result.Message = saveResult.Message;
                }
                else
                {
                    // For hard delete, create a transaction to remove the avatar from Cosmos
                    var walletAddress = await GetWalletAddressForAvatar(id);
                    if (string.IsNullOrWhiteSpace(walletAddress))
                    {
                        OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar deletion");
                        return result;
                    }

                    var txUrl = "/cosmos/tx/v1beta1/txs";
                    var deletePayload = new
                    {
                        body = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    type = "/cosmos.staking.v1beta1.MsgUndelegate",
                                    value = new
                                    {
                                        delegator_address = walletAddress,
                                        validator_address = walletAddress,
                                        amount = new { denom = "uatom", amount = "0" }
                                    }
                                }
                            }
                        }
                    };

                    var json = JsonSerializer.Serialize(deletePayload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var httpResponse = await _httpClient.PostAsync(txUrl, content);

                    result.Result = httpResponse.IsSuccessStatusCode;
                    result.IsError = !httpResponse.IsSuccessStatusCode;
                    result.Message = httpResponse.IsSuccessStatusCode ? "Avatar deleted successfully from Cosmos blockchain" : $"Failed to delete avatar: {httpResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Cosmos: {ex.Message}", ex);
            }
            return result;
        }

    }
}
