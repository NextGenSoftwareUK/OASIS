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

    // Only the provider's own (OASIS) account can sign without a wallet lookup, so the from address must be that account.
    public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionAsync method in BaseOASIS sending transaction. Reason: ";

        var activation = await EnsureActivatedForSendAsync();
        if (activation.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, activation.Message));
            return result;
        }

        if (!string.Equals(fromWalletAddress, _oasisAccount.Address, StringComparison.OrdinalIgnoreCase))
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage,
                $"BaseOASIS holds the signing key only for {_oasisAccount.Address}. Use SendTransactionById/ByUsername/ByEmail to send from an avatar wallet."));
            return result;
        }

        return await SendBaseTransaction(_chainPrivateKey, toWalletAddress, amount, null, memoText);
    }

    public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return await SendBetweenAvatarWalletsAsync(
            WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.BaseOASIS, showPrivateKeys: true),
            WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.BaseOASIS),
            amount, null, nameof(SendTransactionByDefaultWalletAsync));
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, null).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return await SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, null);
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        return await SendBetweenAvatarWalletsAsync(
            WalletManager.Instance.GetAvatarDefaultWalletByEmailAsync(fromAvatarEmail, Core.Enums.ProviderType.BaseOASIS, showPrivateKeys: true),
            WalletManager.Instance.GetAvatarDefaultWalletByEmailAsync(toAvatarEmail, Core.Enums.ProviderType.BaseOASIS),
            amount, token, nameof(SendTransactionByEmailAsync));
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, null).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, null);
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return await SendBetweenAvatarWalletsAsync(
            WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.BaseOASIS, showPrivateKeys: true),
            WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.BaseOASIS),
            amount, token, nameof(SendTransactionByIdAsync));
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, null).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        return await SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, null);
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        return await SendBetweenAvatarWalletsAsync(
            WalletManager.Instance.GetAvatarDefaultWalletByUsernameAsync(fromAvatarUsername, showPrivateKeys: true, providerType: Core.Enums.ProviderType.BaseOASIS),
            WalletManager.Instance.GetAvatarDefaultWalletByUsernameAsync(toAvatarUsername, providerType: Core.Enums.ProviderType.BaseOASIS),
            amount, token, nameof(SendTransactionByUsernameAsync));
    }

    private async Task<OASISResult<bool>> EnsureActivatedForSendAsync()
    {
        if (IsProviderActivated && _web3Client != null && _oasisAccount != null)
            return new OASISResult<bool>(true);

        return await ActivateProviderAsync();
    }

    private async Task<OASISResult<ITransactionResponse>> SendBetweenAvatarWalletsAsync(
        Task<OASISResult<IProviderWallet>> senderWalletTask,
        Task<OASISResult<IProviderWallet>> receiverWalletTask,
        decimal amount, string token, string caller)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = $"Error in {caller} method in BaseOASIS sending transaction. Reason: ";

        var activation = await EnsureActivatedForSendAsync();
        if (activation.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, activation.Message));
            return result;
        }

        var senderWallet = await senderWalletTask;
        if (senderWallet.IsError || senderWallet.Result == null || string.IsNullOrWhiteSpace(senderWallet.Result.PrivateKey))
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage,
                senderWallet.IsError ? senderWallet.Message : "The sender has no Base wallet with a private key."), senderWallet.Exception);
            return result;
        }

        var receiverWallet = await receiverWalletTask;
        if (receiverWallet.IsError || receiverWallet.Result == null || string.IsNullOrWhiteSpace(receiverWallet.Result.WalletAddress))
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage,
                receiverWallet.IsError ? receiverWallet.Message : "The receiver has no Base wallet address."), receiverWallet.Exception);
            return result;
        }

        result = await SendBaseTransaction(senderWallet.Result.PrivateKey, receiverWallet.Result.WalletAddress, amount, token, null);
        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    // token: null/"ETH" sends native ETH; otherwise it must be an ERC-20 contract address on Base.
    private async Task<OASISResult<ITransactionResponse>> SendBaseTransaction(string senderPrivateKey, string receiverAddress, decimal amount, string token, string memoText)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendBaseTransaction method in BaseOASIS sending transaction. Reason: ";

        try
        {
            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Amount must be greater than zero."));
                return result;
            }

            if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(receiverAddress))
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"'{receiverAddress}' is not a valid Base address."));
                return result;
            }

            var senderAccount = new Account(senderPrivateKey, _chainId);
            var senderWeb3 = new Web3(senderAccount, _hostURI);
            TransactionReceipt receipt;

            if (string.IsNullOrWhiteSpace(token) || string.Equals(token, "ETH", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(memoText))
                {
                    receipt = await senderWeb3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(receiverAddress, amount);
                }
                else
                {
                    var input = new TransactionInput
                    {
                        From = senderAccount.Address,
                        To = receiverAddress,
                        Value = new HexBigInteger(Web3.Convert.ToWei(amount)),
                        Data = Encoding.UTF8.GetBytes(memoText).ToHex(true)
                    };
                    input.Gas = await senderWeb3.Eth.TransactionManager.EstimateGasAsync(input);
                    receipt = await senderWeb3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(input);
                }
            }
            else
            {
                if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(token))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Token '{token}' must be 'ETH' or an ERC-20 contract address on Base."));
                    return result;
                }

                var erc20 = senderWeb3.Eth.ERC20.GetContractService(token);
                var decimals = await erc20.DecimalsQueryAsync();
                receipt = await erc20.TransferRequestAndWaitForReceiptAsync(receiverAddress, Web3.Convert.ToWei(amount, decimals));
            }

            if (receipt == null || receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage,
                    $"Base transaction {receipt?.TransactionHash} failed (status {receipt?.Status?.Value})."));
                return result;
            }

            result.Result = new TransactionResponse { TransactionResult = receipt.TransactionHash };
            result.Message = $"Base transaction successful. Hash: {receipt.TransactionHash}";
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError?.Message ?? ex.Message), ex);
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
