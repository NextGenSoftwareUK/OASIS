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
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
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

                // Real BNB Chain implementation: Load holons for parent by provider key using smart contract
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
                            data = "0x" + GetFunctionSelector("getHolonsForParentByProviderKey") + EncodeParameter(providerKey)
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
                        result.Message = $"Loaded {holons.Count()} holons for parent {providerKey} from BNB Chain successfully";
                    }
                    else
                    {
                        result.Result = new List<IHolon>();
                        result.IsError = false;
                        result.Message = "No holons found for parent on BNB Chain";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons for parent from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
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
                var loadResult = await LoadHolonAsync(id);
                if (loadResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon {id}: {loadResult.Message}");
                    return result;
                }

                // Real BNB Chain implementation: Delete holon using smart contract
                var deleteFunction = _contract.GetFunction("deleteHolon");
                var gasEstimate = await deleteFunction.EstimateGasAsync(id.ToString());

                var transactionReceipt = await deleteFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    id.ToString()
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = loadResult.Result;
                    result.IsError = false;
                    result.Message = $"Holon {id} deleted from BNB Chain successfully. Transaction hash: {transactionReceipt.TransactionHash}";
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

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
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

                // Real BNB Chain implementation: Delete avatar by username using smart contract
                var deleteFunction = _contract.GetFunction("deleteAvatarByUsername");
                var gasEstimate = await deleteFunction.EstimateGasAsync(username);

                var transactionReceipt = await deleteFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    username
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Avatar with username {username} deleted from BNB Chain successfully. Transaction hash: {transactionReceipt.TransactionHash}";
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

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
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

                // Real BNB Chain implementation: Load holons by metadata using smart contract
                var metadataJson = JsonSerializer.Serialize(metaData);
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
                            data = "0x" + GetFunctionSelector("getHolonsByMetaData") + EncodeParameter(metadataJson)
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
                        result.Message = $"Loaded {holons.Count()} holons by metadata from BNB Chain successfully";
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

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, matchMode, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
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

                // Real BNB Chain implementation: Search for avatar by username using smart contract
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("getAvatarByUsername") + EncodeParameter(username)
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var avatar = ParseBNBChainToAvatar(resultData.GetString());
                        if (avatar != null)
                        {
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded from BNB Chain by username successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Avatar not found with that username");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar not found with that username");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
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

                // Real BNB Chain implementation: Load avatar detail by email using smart contract
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
                            data = "0x" + GetFunctionSelector("getAvatarDetailByEmail") + EncodeParameter(email)
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
                            result.Message = "Avatar detail loaded from BNB Chain by email successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Avatar detail not found with that email");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar detail not found with that email");
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

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
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

                // Real BNB Chain implementation: Search for avatar by email using smart contract
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("getAvatarByEmail") + EncodeParameter(email)
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var avatar = ParseBNBChainToAvatar(resultData.GetString());
                        if (avatar != null)
                        {
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded from BNB Chain by email successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Avatar not found with that email");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar not found with that email");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from BNB Chain: {ex.Message}");
            }
            return result;
        }

    }
}
