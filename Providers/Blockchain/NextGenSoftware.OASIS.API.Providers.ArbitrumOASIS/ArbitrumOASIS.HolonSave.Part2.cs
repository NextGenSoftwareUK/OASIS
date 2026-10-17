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

    public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionAsync method in ArbitrumOASIS sending transaction. Reason: ";

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

            // Ensure the configured account matches the requested from address
            if (_oasisAccount == null || !string.Equals(_oasisAccount.Address, fromWalletAddress, StringComparison.OrdinalIgnoreCase))
            {
                OASISErrorHandling.HandleError(ref result, $"From address {fromWalletAddress} does not match configured provider account {_oasisAccount?.Address}. Configure provider with the correct private key.");
                return result;
            }

            // For EVM chains, a memo can be supplied via data field if needed; for plain transfers we omit it
            TransactionReceipt transactionResult = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toWalletAddress, amount);

            if (transactionResult.HasErrors() is true)
            {
                result.Message = string.Concat(errorMessage, "Arbitrum transaction performing failed! " +
                                 $"From: {transactionResult.From}, To: {transactionResult.To}, Amount: {amount}." +
                                 $"Reason: {transactionResult.Logs}");
                OASISErrorHandling.HandleError(ref result, result.Message);
                return result;
            }

            result.Result.TransactionResult = transactionResult.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
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

    public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByDefaultWalletAsync method in EthereumOASIS sending transaction. Reason: ";

        OASISResult<IProviderWallet> senderAvatarPrivateKeysResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
        OASISResult<IProviderWallet> receiverAvatarAddressesResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.EthereumOASIS);

        if (senderAvatarPrivateKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                senderAvatarPrivateKeysResult.Exception);
            return result;
        }

        if (receiverAvatarAddressesResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                receiverAvatarAddressesResult.Exception);
            return result;
        }

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result.PrivateKey;
        string receiverAvatarAddress = receiverAvatarAddressesResult.Result.WalletAddress;
        result = await SendArbitrumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
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

            // Get wallet addresses for emails using WalletHelper
            var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByEmailAsync(fromAvatarEmail, Core.Enums.ProviderType.ArbitrumOASIS);
            var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByEmailAsync(toAvatarEmail, Core.Enums.ProviderType.ArbitrumOASIS);
            
            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                return result;
            }
            
            var fromAddress = fromWalletResult.Result.WalletAddress;
            var toAddress = toWalletResult.Result.WalletAddress;

            // Send transaction using Arbitrum
            var transactionResult = await SendTransactionAsync(fromAddress, toAddress, amount, $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail}");
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result;
            result.IsError = false;
            result.Message = "Arbitrum transaction sent successfully by email";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
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

            // Get wallet addresses for emails using WalletHelper
            var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByEmailAsync(fromAvatarEmail, Core.Enums.ProviderType.ArbitrumOASIS);
            var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByEmailAsync(toAvatarEmail, Core.Enums.ProviderType.ArbitrumOASIS);
            
            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                return result;
            }
            
            var fromAddress = fromWalletResult.Result.WalletAddress;
            var toAddress = toWalletResult.Result.WalletAddress;

            // Send transaction using Arbitrum with token support
            var transactionResult = await SendTransactionAsync(fromAddress, toAddress, amount, $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail} with token {token}");
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result;
            result.IsError = false;
            result.Message = "Arbitrum transaction sent successfully by email with token";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync(token): {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
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

            // Get wallet addresses for avatars using WalletHelper
            var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, fromAvatarId);
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, toAvatarId);
            
            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                return result;
            }
            
            var fromAddress = fromWalletResult.Result;
            var toAddress = toWalletResult.Result;

            // Send transaction using Arbitrum
            var transactionResult = await SendTransactionAsync(fromAddress, toAddress, amount, $"OASIS transaction from {fromAvatarId} to {toAvatarId}");
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result;
            result.IsError = false;
            result.Message = "Arbitrum transaction sent successfully by ID";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
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

            // Get wallet addresses for avatars using WalletHelper
            var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, fromAvatarId);
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, toAvatarId);
            
            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                return result;
            }
            
            var fromAddress = fromWalletResult.Result;
            var toAddress = toWalletResult.Result;

            // Send transaction using Arbitrum with token support
            var transactionResult = await SendTransactionAsync(fromAddress, toAddress, amount, $"OASIS transaction from {fromAvatarId} to {toAvatarId} with token {token}");
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result;
            result.IsError = false;
            result.Message = "Arbitrum transaction sent successfully by ID with token";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync(token): {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByUsernameAsync method in ArbitrumOASIS sending transaction. Reason: ";

        OASISResult<List<string>> senderAvatarPrivateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, this.ProviderType.Value);
        OASISResult<List<string>> receiverAvatarAddressesResult = KeyManager.Instance.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername, this.ProviderType.Value);

        if (senderAvatarPrivateKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                senderAvatarPrivateKeysResult.Exception);
            return result;
        }

        if (receiverAvatarAddressesResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                receiverAvatarAddressesResult.Exception);
            return result;
        }

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
        string receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
        result = await SendArbitrumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
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

            // Get wallet addresses for usernames using WalletHelper
            var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, fromAvatarUsername);
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, toAvatarUsername);
            
            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for usernames");
                return result;
            }
            
            var fromAddress = fromWalletResult.Result;
            var toAddress = toWalletResult.Result;

            // Send transaction using Arbitrum with token support
            var transactionResult = await SendTransactionAsync(fromAddress, toAddress, amount, $"OASIS transaction from {fromAvatarUsername} to {toAvatarUsername} with token {token}");
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result;
            result.IsError = false;
            result.Message = "Arbitrum transaction sent successfully by username with token";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByUsernameAsync(token): {ex.Message}", ex);
        }
        return result;
    }
}
