using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using static NextGenSoftware.Utilities.KeyHelper;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;


namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;

public sealed partial class ArbitrumOASIS
{
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar by provider key first
            var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {avatarResult.Message}");
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
                result.Message = "Avatar deleted successfully by provider key from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarAsync method in ArbitrumOASIS while deleting holon. Reason: ";

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

            Function deleteAvatarFunc = _contract.GetFunction(ArbitrumContractHelper.DeleteAvatarFuncName);
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


    public async Task<OASISResult<bool>> DeleteAvatarByProviderKeyAsync(string providerKey, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar by provider key first
            var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {avatarResult.Message}");
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
                result.Message = "Avatar deleted successfully by provider key from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Arbitrum: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
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
                result.Message = "Avatar deleted successfully by email from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Arbitrum: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar by username first
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
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
                result.Message = "Avatar deleted successfully by username from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Arbitrum: {ex.Message}", ex);
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

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
    {
        OASISResult<IHolon> result = new();
        string errorMessage = "Error in DeleteHolonAsync method in ArbitrumOASIS while deleting holon. Reason: ";

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

            Function deleteHolonFunc = _contract.GetFunction(ArbitrumContractHelper.DeleteHolonFuncName);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
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
                result.Message = "Holon deleted successfully by provider key from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }
}
