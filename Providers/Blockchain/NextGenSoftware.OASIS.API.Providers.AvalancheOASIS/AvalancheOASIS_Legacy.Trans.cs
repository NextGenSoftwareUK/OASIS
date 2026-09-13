using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Text;


using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;

public sealed partial class AvalancheOASIS_Legacy
{
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
        string errorMessage = "Error in SendTransactionByUsernameAsync method in AvalancheOASIS sending transaction. Reason: ";

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
        result = await SendAvalancheTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);

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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Avalanche provider: {activateResult.Message}");
                    return result;
                }
            }

            // Get avatar wallets by username
            var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByUsernameAsync(fromAvatarUsername, false, false, false, this.ProviderType.Value);
            var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByUsernameAsync(toAvatarUsername, false, false, false, this.ProviderType.Value);

            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatar wallets: {fromWalletResult.Message} {toWalletResult.Message}");
                return result;
            }

            // Send transaction using Avalanche client
            var transactionResult = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toWalletResult.Result.WalletAddress, amount);
            if (transactionResult.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Logs}");
                return result;
            }

            result.Result.TransactionResult = transactionResult.TransactionHash;
            result.IsError = false;
            result.Message = "Transaction sent successfully by username from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending transaction by username from Avalanche: {ex.Message}", ex);
        }
        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> SendAvalancheTransaction(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendAvalancheTransaction method in AvalancheOASIS sending transaction. Reason: ";

        try
        {
            Account senderEthAccount = new(senderAccountPrivateKey);

            TransactionReceipt receipt = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount);

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
        string errorMessage = "Error in SendNFTAsync method in AvalancheOASIS while sending nft. Reason: ";

        try
        {
            Function sendNftFunction = _contract.GetFunction(AvalancheContractHelper.SendNftFuncName);

            HexBigInteger gasEstimate = await sendNftFunction.EstimateGasAsync(
                from: transaction.FromWalletAddress,
                gas: null,
                value: null,
                transaction.FromWalletAddress,
                transaction.ToWalletAddress,
                transaction.TokenId,
                this.ProviderType.Value.ToString(),
                this.ProviderType.Value.ToString(),
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
                this.ProviderType.Value.ToString(),
                this.ProviderType.Value.ToString(),
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
        string errorMessage = "Error in MintNFTAsync method in AvalancheOASIS while minting nft. Reason: ";

        try
        {
            Function mintFunction = _contract.GetFunction(AvalancheContractHelper.MintFuncName);

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

    public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
    {
        return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
    }

    public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        var result = new OASISResult<IWeb3NFT>();

        if (!IsProviderActivated)
        {
            OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
            return result;
        }

        // Load NFT data from Avalanche blockchain using smart contract
        try
        {
            // Query Avalanche smart contract for NFT data
            var getNFTFunction = _contract.GetFunction(GetNFTDataFuncName);
            var nftData = await getNFTFunction.CallDeserializingToObjectAsync<NFTStruct>(nftTokenAddress);
            
                var nft = new Web3NFT();
                nft.Id = Guid.NewGuid();
                nft.NFTTokenAddress = nftTokenAddress;
                nft.MetaData.Add("AvalancheEntityId", nftData.EntityId.ToString());
                nft.MetaData.Add("AvalancheInfo", nftData.Info);
            
            result.Result = nft;
            result.IsError = false;
            result.Message = "NFT data loaded successfully from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from Avalanche: {ex.Message}");
        }

        return result;
    }

    // NFT-specific lock/unlock methods
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
    {
        return LockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Lock NFT by transferring to bridge pool
            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = string.Empty,
                ToWalletAddress = bridgePoolAddress,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
    {
        return UnlockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Unlock NFT by transferring from bridge pool back to owner
            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = bridgePoolAddress,
                ToWalletAddress = string.Empty,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
        }
        return result;
    }

    // NFT Bridge Methods
}
