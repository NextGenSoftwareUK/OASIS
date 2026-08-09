using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS;

public sealed partial class BaseOASIS
{
    public override Task<OASISResult<bool>> ActivateProviderAsync()
    {
        OASISResult<bool> result;

        try
        {
            result = ActivateProvider();
        }
        catch (Exception ex)
        {
            return Task.FromException<OASISResult<bool>>(ex);
        }

        return Task.FromResult(result);
    }

    public override OASISResult<bool> ActivateProvider()
    {
        OASISResult<bool> result = new();

        try
        {
            if (!this.IsProviderActivated)
            {
                if (_hostURI is { Length: > 0 } &&
                _chainPrivateKey is { Length: > 0 } &&
                _chainId > 0 &&
                _contractAddress is { Length: > 0 })
                {
                    _oasisAccount = new Account(_chainPrivateKey, _chainId);
                    _web3Client = new Web3(_oasisAccount, _hostURI);
                    _contract = _web3Client.Eth.GetContract(BaseContractHelper.Abi, _contractAddress);
                    _contractHandler = _web3Client.Eth.GetContractHandler(_contractAddress);

                    this.IsProviderActivated = true;
                }
            }
        }
        catch (Exception ex)
        {
            this.IsProviderActivated = false;
            OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateProviderAsync in EthereumOASIS Provider. Reason: {ex}");
        }

        return result;
    }

    public override Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        OASISResult<bool> result;

        try
        {
            result = DeActivateProvider();
        }
        catch (Exception ex)
        {
            return Task.FromException<OASISResult<bool>>(ex);
        }

        return Task.FromResult(result);
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        _oasisAccount = null;
        _web3Client = null;

        IsProviderActivated = false;

        return new(value: true);
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
    {
        return DeleteAvatarAsync(id, softDelete).Result;
    }

    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        return DeleteAvatarAsync(providerKey, softDelete).Result;
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Delete avatar directly by provider key without loading first
            var deleteRequest = new
            {
                providerKey = providerKey,
                softDelete = softDelete
            };

            var jsonContent = JsonSerializer.Serialize(deleteRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var deleteResponse = await _httpClient.PostAsync("/api/v1/avatars/delete/by-provider-key", content);
            if (deleteResponse.IsSuccessStatusCode)
            {
                result.Result = true;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by provider key from Base";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar by provider key from Base: {deleteResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Base: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarAsync method in BaseOASIS while deleting holon. Reason: ";

        try
        {
            int avatarEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            Function deleteAvatarFunc = _contract.GetFunction(BaseContractHelper.DeleteAvatarFuncName);
            TransactionReceipt txReceipt = await deleteAvatarFunc.SendTransactionAndWaitForReceiptAsync(
                fromAccountWallet.Result.WalletAddress, receiptRequestCancellationToken: null, avatarEntityId);

            if (txReceipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Logs));
                return result;
            }

            result.Result = true;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
    {
        return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Delete avatar by ID
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                    return result;
                }

                result.Result = deleteResult.Result;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by email from Base";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Base: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
    {
        return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Delete avatar directly by username without loading first
            var deleteRequest = new
            {
                username = avatarUsername,
                softDelete = softDelete
            };

            var jsonContent = JsonSerializer.Serialize(deleteRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var deleteResponse = await _httpClient.PostAsync("/api/v1/avatars/delete/by-username", content);
            if (deleteResponse.IsSuccessStatusCode)
            {
                result.Result = true;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by username from Base";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar by username from Base: {deleteResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Base: {ex.Message}", ex);
        }
        return result;
    }


    public override OASISResult<IHolon> DeleteHolon(Guid id)
    {
        return DeleteHolonAsync(id).Result;
    }

    public override OASISResult<IHolon> DeleteHolon(string providerKey)
    {
        return DeleteHolonAsync(providerKey).Result;
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load holon by provider key first
            var holonResult = await LoadHolonAsync(providerKey);
            if (holonResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {holonResult.Message}");
                return result;
            }

            if (holonResult.Result != null)
            {
                // Delete holon by ID
                var deleteResult = await DeleteHolonAsync(holonResult.Result.Id);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {deleteResult.Message}");
                    return result;
                }

                result.Result = holonResult.Result;
                result.IsError = false;
                result.Message = "Holon deleted successfully by provider key from Base";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Base: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
    {
        OASISResult<IHolon> result = new();
        string errorMessage = "Error in DeleteHolonAsync method in BaseOASIS while deleting holon. Reason: ";

        try
        {
            OASISResult<IHolon> holonToDeleteResult = await LoadHolonAsync(id);
            if (holonToDeleteResult.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, holonToDeleteResult.Message), holonToDeleteResult.Exception);
                return result;
            }

            int holonEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            Function deleteHolonFunc = _contract.GetFunction(BaseContractHelper.DeleteHolonFuncName);
            TransactionReceipt txReceipt = await deleteHolonFunc.SendTransactionAndWaitForReceiptAsync(
                fromAccountWallet.Result.WalletAddress, receiptRequestCancellationToken: null, holonEntityId);

            if (txReceipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Logs));
                return result;
            }

            result.Result = holonToDeleteResult.Result;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }


    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        return ExportAllAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();

        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Export all data from Base blockchain
            var exportRequest = new
            {
                version = version,
                includeDeleted = false
            };

            var jsonContent = JsonSerializer.Serialize(exportRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var exportResponse = await _httpClient.PostAsync("/api/v1/export", content);
            if (exportResponse.IsSuccessStatusCode)
            {
                var responseContent = await exportResponse.Content.ReadAsStringAsync();
                var exportData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                var holons = new List<IHolon>();
                // Parse export data and populate holons list
                if (exportData.TryGetProperty("holons", out var holonsArray))
                {
                    foreach (var holonElement in holonsArray.EnumerateArray())
                    {
                        var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = "Export completed successfully from Base blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to export from Base blockchain: {exportResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting from Base blockchain: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
    {
        return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();

        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Export all data for specific avatar by email from Base blockchain
            var exportRequest = new
            {
                avatarEmail = avatarEmailAddress,
                version = version,
                includeDeleted = false
            };

            var jsonContent = JsonSerializer.Serialize(exportRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/email", content);
            if (exportResponse.IsSuccessStatusCode)
            {
                var responseContent = await exportResponse.Content.ReadAsStringAsync();
                var exportData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                var holons = new List<IHolon>();
                // Parse export data and populate holons list
                if (exportData.TryGetProperty("holons", out var holonsArray))
                {
                    foreach (var holonElement in holonsArray.EnumerateArray())
                    {
                        var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = "Avatar data export completed successfully from Base blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Base blockchain: {exportResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Base blockchain: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
    }

}
