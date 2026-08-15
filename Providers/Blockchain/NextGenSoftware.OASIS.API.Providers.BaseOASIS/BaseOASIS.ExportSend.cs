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
    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        ArgumentNullException.ThrowIfNull(holons);

        OASISResult<IEnumerable<IHolon>> result = new();
        string errorMessage = "Error in SaveHolonsAsync method in BaseOASIS while saving holons. Reason: ";

        try
        {
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> saveHolonResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (saveHolonResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, saveHolonResult.DetailedMessage));

                    if (!continueOnError) break;
                }
            }

            result.Result = holons;
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

    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
    }

    public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        var result = new OASISResult<ISearchResults>();
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

            // Search avatars and holons using Base blockchain
            var searchData = await SearchAsync(searchParams);
            if (searchData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching from Base: {searchData.Message}");
                return result;
            }

            result.Result = searchData.Result;
            result.IsError = false;
            result.Message = "Search completed successfully from Base";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error searching from Base: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionAsync method in BaseOASIS sending transaction. Reason: ";

        try
        {
            // Convert decimal amount to Wei (Base uses 18 decimals like Ethereum)
            var amountInWei = Nethereum.Util.UnitConversion.Convert.ToWei(amount, Nethereum.Util.UnitConversion.EthUnit.Ether);

            TransactionReceipt transactionResult = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toWalletAddress, (decimal)amountInWei);

            if (transactionResult.HasErrors() is true)
            {
                result.Message = string.Concat(errorMessage, "Base transaction performing failed! " +
                                 $"From: {transactionResult.From}, To: {transactionResult.To}, Amount: {amount}." +
                                 $"Reason: {transactionResult.Logs}");
                OASISErrorHandling.HandleError(ref result, result.Message);
                return result;
            }

            result.Result.TransactionResult = transactionResult.TransactionHash;
            result.Message = $"Base transaction successful. Hash: {transactionResult.TransactionHash}";
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
        result = await SendBaseTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, "ETH").Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return await SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, "ETH");
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Get wallet addresses for both avatars
            var fromAddress = await WalletHelper.GetWalletAddressAsync(fromAvatarEmail, Core.Enums.ProviderType.BaseOASIS);
            var toAddress = await WalletHelper.GetWalletAddressAsync(toAvatarEmail, Core.Enums.ProviderType.BaseOASIS);

            if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromAddress ?? "null"} {toAddress ?? "null"}");
                return result;
            }

            // Send transaction using Base client
            var transactionResult = await SendTransactionAsync($"{{\"from\":\"{fromAddress}\",\"to\":\"{toAddress}\",\"amount\":{amount},\"token\":\"{token}\"}}");
            if (!transactionResult.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.ReasonPhrase}");
                return result;
            }

            var content = await transactionResult.Content.ReadAsStringAsync();
            var transactionResponse = ParseBaseToTransactionResponse(content);
            result.Result = transactionResponse;
            result.IsError = false;
            result.Message = "Transaction sent successfully via Base";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Base: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, "ETH").Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, "ETH");
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Get wallet addresses for both avatars
            var fromAddress = await WalletHelper.GetWalletAddressAsync(fromAvatarId.ToString(), Core.Enums.ProviderType.BaseOASIS);
            var toAddress = await WalletHelper.GetWalletAddressAsync(toAvatarId.ToString(), Core.Enums.ProviderType.BaseOASIS);

            if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromAddress ?? "null"} {toAddress ?? "null"}");
                return result;
            }

            // Send transaction using Base client
            var transactionResult = await SendTransactionAsync($"{{\"from\":\"{fromAddress}\",\"to\":\"{toAddress}\",\"amount\":{amount},\"token\":\"{token}\"}}");
            if (!transactionResult.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.ReasonPhrase}");
                return result;
            }

            var content = await transactionResult.Content.ReadAsStringAsync();
            var transactionResponse = ParseBaseToTransactionResponse(content);
            result.Result = transactionResponse;
            result.IsError = false;
            result.Message = "Transaction sent successfully via Base";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Base: {ex.Message}", ex);
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
        string errorMessage = "Error in SendTransactionByUsernameAsync method in BaseOASIS sending transaction. Reason: ";

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
        result = await SendBaseTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);

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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Get wallet addresses for both avatars
            var fromAddress = await WalletHelper.GetWalletAddressAsync(fromAvatarUsername, Core.Enums.ProviderType.BaseOASIS);
            var toAddress = await WalletHelper.GetWalletAddressAsync(toAvatarUsername, Core.Enums.ProviderType.BaseOASIS);

            if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromAddress ?? "null"} {toAddress ?? "null"}");
                return result;
            }

            // Send transaction using Base client
            var transactionResult = await SendTransactionAsync($"{{\"from\":\"{fromAddress}\",\"to\":\"{toAddress}\",\"amount\":{amount},\"token\":\"{token}\"}}");
            if (!transactionResult.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.ReasonPhrase}");
                return result;
            }

            var content = await transactionResult.Content.ReadAsStringAsync();
            var transactionResponse = ParseBaseToTransactionResponse(content);
            result.Result = transactionResponse;
            result.IsError = false;
            result.Message = "Transaction sent successfully via Base";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Base: {ex.Message}", ex);
        }
        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> SendBaseTransaction(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendBaseTransaction method in BaseOASIS sending transaction. Reason: ";

        try
        {
            Account senderEthAccount = new(senderAccountPrivateKey);

            TransactionReceipt receipt = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, (decimal)amount);

            if (receipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receipt.Logs));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        => SendNFTAsync(transaction).Result;


    public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
    {
        OASISResult<IWeb3NFTTransactionResponse> result = new();
        string errorMessage = "Error in SendNFTAsync method in BaseOASIS while sending nft. Reason: ";

        try
        {
            Function sendNftFunction = _contract.GetFunction(BaseContractHelper.SendNftFuncName);

            HexBigInteger gasEstimate = await sendNftFunction.EstimateGasAsync(
                from: transaction.FromWalletAddress,
                gas: null,
                value: null,
                transaction.FromWalletAddress,
                transaction.ToWalletAddress,
                transaction.TokenId,
                transaction.Amount,
                transaction.MemoText
            );
            HexBigInteger gasPrice = await _web3Client.Eth.GasPrice.SendRequestAsync();

            TransactionReceipt txReceipt = await sendNftFunction.SendTransactionAndWaitForReceiptAsync(
                from: transaction.FromWalletAddress,
                gas: gasEstimate,
                value: gasPrice,
                receiptRequestCancellationToken: null,
                transaction.FromWalletAddress,
                transaction.ToWalletAddress,
                transaction.TokenId,
                transaction.Amount,
                transaction.MemoText
            );

            if (txReceipt.HasErrors() is true && txReceipt.Logs.Count > 0)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Status));
                return result;
            }

            IWeb3NFTTransactionResponse response = new Web3NFTTransactionResponse
            {
                Web3NFT = new Web3NFT()
                {
                    MemoText = transaction.MemoText,
                    MintTransactionHash = txReceipt.TransactionHash
                },
                TransactionResult = txReceipt.TransactionHash
            };

            result.Result = response;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError.Data), ex);
        }
        catch (SmartContractCustomErrorRevertException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.ExceptionEncodedData), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        => MintNFTAsync(transation).Result;

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transaction)
    {
        OASISResult<IWeb3NFTTransactionResponse> result = new();
        string errorMessage = "Error in MintNFTAsync method in BaseOASIS while minting nft. Reason: ";

        try
        {
            Function mintFunction = _contract.GetFunction(BaseContractHelper.MintFuncName);

            HexBigInteger gasEstimate = await mintFunction.EstimateGasAsync(
                from: _oasisAccount.Address,
                //from: transaction.MintWalletAddress,
                gas: null,
                value: null,
                //transaction.MintWalletAddress,
                _oasisAccount.Address,
                transaction.JSONMetaDataURL
            );
            HexBigInteger gasPrice = await _web3Client.Eth.GasPrice.SendRequestAsync();

            TransactionReceipt txReceipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                _oasisAccount.Address,
                //transaction.MintWalletAddress,
                gas: gasEstimate,
                value: gasPrice,
                receiptRequestCancellationToken: null,
                //transaction.MintWalletAddress,
                _oasisAccount.Address,
                transaction.JSONMetaDataURL
            );

            if (txReceipt.HasErrors() is true && txReceipt.Logs.Count > 0)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Logs));
                return result;
            }

            IWeb3NFTTransactionResponse response = new Web3NFTTransactionResponse
            {
                Web3NFT = new Web3NFT()
                {
                    MemoText = transaction.MemoText,
                    MintTransactionHash = txReceipt.TransactionHash
                },
                TransactionResult = txReceipt.TransactionHash
            };

            result.Result = response;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError.Message), ex);
        }
        catch (SmartContractCustomErrorRevertException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.ExceptionEncodedData), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

}
