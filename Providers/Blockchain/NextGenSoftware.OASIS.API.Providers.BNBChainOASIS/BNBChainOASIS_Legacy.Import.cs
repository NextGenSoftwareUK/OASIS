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
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
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

                // Real BNB Chain implementation: Import holons using smart contract
                var importData = JsonSerializer.Serialize(holons);
                var importRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_sendTransaction",
                    @params = new object[]
                    {
                        new
                        {
                            from = _account.Address,
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("importHolons") + EncodeParameter(importData),
                            gas = "0x" + (500000).ToString("x")
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(importRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && !string.IsNullOrEmpty(resultData.GetString()))
                    {
                        result.Result = true;
                        result.IsError = false;
                        result.Message = $"Imported {holons.Count()} holons to BNB Chain successfully. Transaction: {resultData.GetString()}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to import holons to BNB Chain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to import holons to BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
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

                // Real BNB Chain implementation: Delete avatar by provider key using smart contract
                var deleteFunction = _contract.GetFunction("deleteAvatarByProviderKey");
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
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Avatar {providerKey} deleted from BNB Chain successfully. Transaction hash: {transactionReceipt.TransactionHash}";
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

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(email, softDelete).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(email, version).Result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
        {
            return LoadAvatarByUsernameAsync(username, version).Result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
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

                // Real BNB Chain implementation: Save avatar using smart contract
                var avatarData = new
                {
                    avatarId = avatar.Id.ToString(),
                    title = avatar.Title ?? "",
                    firstName = avatar.FirstName ?? "",
                    lastName = avatar.LastName ?? "",
                    username = avatar.Username ?? "",
                    email = avatar.Email ?? "",
                    password = avatar.Password ?? "",
                    avatarType = avatar.AvatarType?.Value.ToString() ?? "User",
                    acceptTerms = avatar.AcceptTerms,
                    isVerified = avatar.IsVerified,
                    jwtToken = avatar.JwtToken ?? "",
                    passwordReset = avatar.PasswordReset != null ? ((DateTimeOffset)avatar.PasswordReset).ToUnixTimeSeconds() : 0,
                    refreshToken = avatar.RefreshToken ?? "",
                    resetToken = avatar.ResetToken ?? "",
                    resetTokenExpires = avatar.ResetTokenExpires != null ? ((DateTimeOffset)avatar.ResetTokenExpires).ToUnixTimeSeconds() : 0,
                    verificationToken = avatar.VerificationToken ?? "",
                    verified = avatar.Verified != null ? ((DateTimeOffset)avatar.Verified).ToUnixTimeSeconds() : 0,
                    lastBeamedIn = avatar.LastBeamedIn != null ? ((DateTimeOffset)avatar.LastBeamedIn).ToUnixTimeSeconds() : 0,
                    lastBeamedOut = avatar.LastBeamedOut != null ? ((DateTimeOffset)avatar.LastBeamedOut).ToUnixTimeSeconds() : 0,
                    isBeamedIn = avatar.IsBeamedIn,
                    providerWallets = JsonSerializer.Serialize(avatar.ProviderWallets ?? new Dictionary<ProviderType, List<IProviderWallet>>()),
                    providerUsername = JsonSerializer.Serialize(avatar.ProviderUsername ?? new Dictionary<ProviderType, string>()),
                    metadata = JsonSerializer.Serialize(avatar.MetaData ?? new Dictionary<string, object>())
                };

                // Call smart contract function to create/update avatar
                var createAvatarFunction = _contract.GetFunction("createAvatar");
                var gasEstimate = createAvatarFunction.EstimateGasAsync(
                    avatarData.avatarId,
                    avatarData.title,
                    avatarData.firstName,
                    avatarData.lastName,
                    avatarData.username,
                    avatarData.email,
                    avatarData.password,
                    avatarData.avatarType,
                    avatarData.acceptTerms,
                    avatarData.isVerified,
                    avatarData.jwtToken,
                    avatarData.passwordReset,
                    avatarData.refreshToken,
                    avatarData.resetToken,
                    avatarData.resetTokenExpires,
                    avatarData.verificationToken,
                    avatarData.verified,
                    avatarData.lastBeamedIn,
                    avatarData.lastBeamedOut,
                    avatarData.isBeamedIn,
                    avatarData.providerWallets,
                    avatarData.providerUsername,
                    avatarData.metadata
                ).Result;

                var transactionReceipt = createAvatarFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    avatarData.avatarId,
                    avatarData.title,
                    avatarData.firstName,
                    avatarData.lastName,
                    avatarData.username,
                    avatarData.email,
                    avatarData.password,
                    avatarData.avatarType,
                    avatarData.acceptTerms,
                    avatarData.isVerified,
                    avatarData.jwtToken,
                    avatarData.passwordReset,
                    avatarData.refreshToken,
                    avatarData.resetToken,
                    avatarData.resetTokenExpires,
                    avatarData.verificationToken,
                    avatarData.verified,
                    avatarData.lastBeamedIn,
                    avatarData.lastBeamedOut,
                    avatarData.isBeamedIn,
                    avatarData.providerWallets,
                    avatarData.providerUsername,
                    avatarData.metadata
                ).Result;

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

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
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

                // Real BNB Chain implementation: Export all data for avatar by email using smart contract
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
                            data = "0x" + GetFunctionSelector("exportAllDataForAvatarByEmail") + EncodeParameter(email)
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
                        result.Message = $"Exported {holons.Count()} holons for avatar {email} from BNB Chain successfully";
                    }
                    else
                    {
                        result.Result = new List<IHolon>();
                        result.IsError = false;
                        result.Message = "No data found for avatar on BNB Chain";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export data for avatar from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, continueOnErrorRecursive).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid parentId, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
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

                // Real BNB Chain implementation: Load holons for parent using smart contract
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
                            data = "0x" + GetFunctionSelector("getHolonsForParent") + EncodeParameter(parentId.ToString())
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
                        result.Message = $"Loaded {holons.Count()} holons for parent {parentId} from BNB Chain successfully";
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

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(username, softDelete).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
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

                // Real BNB Chain implementation: Load holon by ID using smart contract
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
                            data = "0x" + GetFunctionSelector("getHolon") + EncodeParameter(id.ToString())
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
                        var holon = ParseBNBChainToHolon(resultData.GetString());
                        if (holon != null)
                        {
                            result.Result = holon;
                            result.IsError = false;
                            result.Message = "Holon loaded from BNB Chain successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Holon not found with that ID");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Holon not found with that ID");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from BNB Chain: {ex.Message}");
            }
            return result;
        }
    }
}
