using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS
{
    public partial class BNBChainOASIS_Legacy
    {
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Save avatar to smart contract
                var avatarData = new
                {
                    avatarId = avatar.Id.ToString(),
                    username = avatar.Username,
                    email = avatar.Email,
                    firstName = avatar.FirstName,
                    lastName = avatar.LastName,
                    avatarType = avatar.AvatarType.Value.ToString(),
                    metadata = JsonSerializer.Serialize(avatar.MetaData)
                };

                // Call smart contract function to create/update avatar
                var createAvatarFunction = _contract.GetFunction("createAvatar");
                var gasEstimate = await createAvatarFunction.EstimateGasAsync(
                    avatarData.avatarId,
                    avatarData.username,
                    avatarData.email,
                    avatarData.firstName,
                    avatarData.lastName,
                    avatarData.avatarType,
                    avatarData.metadata
                );

                var transactionReceipt = await createAvatarFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    avatarData.avatarId,
                    avatarData.username,
                    avatarData.email,
                    avatarData.firstName,
                    avatarData.lastName,
                    avatarData.avatarType,
                    avatarData.metadata
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = $"Avatar saved to BNB Chain successfully. Transaction hash: {transactionReceipt.TransactionHash}";

                    // Store transaction hash in avatar metadata
                    avatar.ProviderMetaData[Core.Enums.ProviderType.BNBChainOASIS]["transactionHash"] = transactionReceipt.TransactionHash;
                    avatar.ProviderMetaData[Core.Enums.ProviderType.BNBChainOASIS]["savedAt"] = DateTime.UtcNow.ToString("O");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction failed on BNB Chain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to BNB Chain: {ex.Message}");
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Load holons by metadata key-value using smart contract
                var metadataPair = new { key = metaData, value = value };
                var metadataJson = JsonSerializer.Serialize(metadataPair);
                var loadRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("getHolonsByMetaDataKeyValue") + EncodeParameter(metadataJson)
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var holons = ParseBNBChainToHolons(resultData.GetString());
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"Loaded {holons.Count()} holons by metadata {metaData}={value} from BNB Chain successfully";
                    }
                    else
                    {
                        result.Result = new List<IHolon>();
                        result.IsError = false;
                        result.Message = "No holons found with matching metadata on BNB Chain";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Load avatar detail by ID using smart contract
                var loadRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("getAvatarDetail") + EncodeParameter(id.ToString())
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var avatarDetail = ParseBNBChainToAvatarDetail(resultData.GetString());
                        if (avatarDetail != null)
                        {
                            result.Result = avatarDetail;
                            result.IsError = false;
                            result.Message = "Avatar detail loaded from BNB Chain successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Avatar detail not found with that ID");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar detail not found with that ID");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Load all avatars using smart contract
                var loadRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("getAllAvatars")
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var avatars = ParseBNBChainToAvatars(resultData.GetString());
                        result.Result = avatars;
                        result.IsError = false;
                        result.Message = $"Loaded {avatars.Count()} avatars from BNB Chain successfully";
                    }
                    else
                    {
                        result.Result = new List<IAvatar>();
                        result.IsError = false;
                        result.Message = "No avatars found on BNB Chain";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatars from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Delete avatar by email using smart contract
                var deleteFunction = _contract.GetFunction("deleteAvatarByEmail");
                var gasEstimate = await deleteFunction.EstimateGasAsync(email);

                var transactionReceipt = await deleteFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    email
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Avatar with email {email} deleted from BNB Chain successfully. Transaction hash: {transactionReceipt.TransactionHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction failed on BNB Chain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(username, version).Result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(id, version).Result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // First load the holon to return it
                var loadResult = await LoadHolonAsync(providerKey);
                if (loadResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon {providerKey}: {loadResult.Message}");
                    return result;
                }

                // Real BNB Chain implementation: Delete holon by provider key using smart contract
                var deleteFunction = _contract.GetFunction("deleteHolonByProviderKey");
                var gasEstimate = await deleteFunction.EstimateGasAsync(providerKey);

                var transactionReceipt = await deleteFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    providerKey
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = loadResult.Result;
                    result.IsError = false;
                    result.Message = $"Holon {providerKey} deleted from BNB Chain successfully. Transaction hash: {transactionReceipt.TransactionHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction failed on BNB Chain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Export all data using smart contract
                var exportRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("exportAllData")
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var holons = ParseBNBChainToHolons(resultData.GetString());
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"Exported {holons.Count()} holons from BNB Chain successfully";
                    }
                    else
                    {
                        result.Result = new List<IHolon>();
                        result.IsError = false;
                        result.Message = "No data found on BNB Chain";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export data from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error exporting data from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

    }
}
