using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Nethereum.JsonRpc.Client;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Nethereum.Contracts;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using System.Net.Http;
using Newtonsoft.Json;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Threading;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Nethereum.Util;
using Nethereum.Hex.HexTypes;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider
{
    public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        => SendNFTAsync(transaction).Result;


    public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
    {
        OASISResult<IWeb3NFTTransactionResponse> result = new();
        string errorMessage = "Error in SendNFTAsync method in Web3CoreOASIS while sending nft. Reason: ";

        if (transaction.Amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            //TODO: Double check this still works! lol
            string transactionHash = await _web3CoreOASIS.SendNFTAsync(
                transaction.FromWalletAddress,
                transaction.ToWalletAddress,
                Convert.ToInt64(transaction.TokenId),
                //transaction.FromProvider.Value.ToString(),
                //transaction.ToProvider.Value.ToString(),
                "None", //Obsolete.
                "None",
                new BigInteger(transaction.Amount),
                transaction.MemoText
            );

            IWeb3NFTTransactionResponse response = new Web3NFTTransactionResponse
            {
                Web3NFT = new Web3NFT()
                {
                    MemoText = transaction.MemoText,
                    MintTransactionHash = transactionHash
                },
                TransactionResult = transactionHash
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
        string errorMessage = "Error in MintNFTAsync method in Web3CoreOASIS while minting nft. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            string metadataJson = Newtonsoft.Json.JsonConvert.SerializeObject(transaction);
            //string transactionHash = await _web3CoreOASIS.MintAsync(transaction.MintWalletAddress, metadataJson);
            string transactionHash = await _web3CoreOASIS.MintAsync(transaction.SendToAddressAfterMinting, metadataJson);

            IWeb3NFTTransactionResponse response = new Web3NFTTransactionResponse
            {
                Web3NFT = new Web3NFT()
                {
                    MemoText = transaction.MemoText,
                    MintTransactionHash = transactionHash
                },
                TransactionResult = transactionHash
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
        string errorMessage = "Error in LoadOnChainNFTDataAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                return result;
            }

            // ERC721 ABI for name, symbol, tokenURI
            var erc721Abi = "[{\"constant\":true,\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenURI\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"ownerOf\",\"outputs\":[{\"name\":\"\",\"type\":\"address\"}],\"type\":\"function\"}]";
            var nftContract = _web3Client.Eth.GetContract(erc721Abi, nftTokenAddress);
            
            var nameFunction = nftContract.GetFunction("name");
            var symbolFunction = nftContract.GetFunction("symbol");
            
            var name = await nameFunction.CallAsync<string>();
            var symbol = await symbolFunction.CallAsync<string>();

            var nft = new Web3NFT
            {
                NFTTokenAddress = nftTokenAddress,
                Symbol = symbol
            };

            result.Result = nft;
            result.IsError = false;
            result.Message = "NFT data loaded successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
    {
        return BurnNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        string errorMessage = "Error in BurnNFTAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress) || 
                string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address and owner private key are required");
                return result;
            }

            var senderAccount = new Account(request.OwnerPrivateKey);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC721 burn function ABI
            var erc721Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[],\"type\":\"function\"}]";
            var nftContract = web3Client.Eth.GetContract(erc721Abi, request.NFTTokenAddress);
            var burnFunction = nftContract.GetFunction("burn");
            
            var tokenId = BigInteger.Parse(request.Web3NFTId.ToString());
            var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                new HexBigInteger(600000), 
                null, 
                null, 
                tokenId);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 burn failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "NFT burned successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
    {
        return SendTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in SendTokenAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.FromTokenAddress) || 
                string.IsNullOrWhiteSpace(request.ToWalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and to wallet address are required");
                return result;
            }

            // Get private key from request or KeyManager
            string privateKey = null;
            if (!string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                privateKey = request.OwnerPrivateKey;
            else if (request is SendWeb3TokenRequest sendRequest && !string.IsNullOrWhiteSpace(sendRequest.FromWalletPrivateKey))
                privateKey = sendRequest.FromWalletPrivateKey;
            
            if (string.IsNullOrWhiteSpace(privateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Private key is required (OwnerPrivateKey or FromWalletPrivateKey)");
                return result;
            }

            var senderAccount = new Account(privateKey);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC20 transfer ABI
            var erc20Abi = "[{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.FromTokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amountBigInt = new BigInteger(request.Amount * (decimal)multiplier);
            var transferFunction = erc20Contract.GetFunction("transfer");
            var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                new HexBigInteger(600000), 
                null, 
                null, 
                request.ToWalletAddress, 
                amountBigInt);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 transfer failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "Token sent successfully.";
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    {
        return MintTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in MintTokenAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null)
            {
                OASISErrorHandling.HandleError(ref result, "Mint request is required");
                return result;
            }

            // Get token address from contract address or use default
            var tokenAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            
            // Get private key from KeyManager using MintedByAvatarId
            var keysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarById(request.MintedByAvatarId, ProviderType.Value);
            if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                return result;
            }

            var senderAccount = new Account(keysResult.Result[0]);
            var web3Client = new Web3(senderAccount, _hostURI);
            var mintToAddress = senderAccount.Address; // Use sender address as default
            var mintAmount = 1m; // Default amount

            // ERC20 mint function ABI
            var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"mint\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, tokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amountBigInt = new BigInteger(mintAmount * (decimal)multiplier);
            var mintFunction = erc20Contract.GetFunction("mint");
            var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                new HexBigInteger(600000), 
                null, 
                null, 
                mintToAddress, 
                amountBigInt);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 mint failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "Token minted successfully.";
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
    {
        return BurnTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in BurnTokenAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and owner private key are required");
                return result;
            }

            var senderAccount = new Account(request.OwnerPrivateKey);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC20 burn function ABI
            var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            // Get burn amount from token balance or use default
            var balanceOfFunction = erc20Contract.GetFunction("balanceOf");
            var balance = await balanceOfFunction.CallAsync<BigInteger>(senderAccount.Address);
            var burnAmount = balance > 0 ? (decimal)balance / (decimal)multiplier : 1m;
            var amountBigInt = new BigInteger(burnAmount * (decimal)multiplier);
            var burnFunction = erc20Contract.GetFunction("burn");
            var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                new HexBigInteger(600000), 
                null, 
                null, 
                amountBigInt);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 burn failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "Token burned successfully.";
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
    {
        return LockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in LockTokenAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                return result;
            }

            // Lock token by transferring to bridge pool
            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            // Get token balance to determine lock amount
            var erc20Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"}]";
            var senderAccount = new Account(request.FromWalletPrivateKey);
            var web3Client = new Web3(senderAccount, _hostURI);
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var balanceOfFunction = erc20Contract.GetFunction("balanceOf");
            var balance = await balanceOfFunction.CallAsync<BigInteger>(senderAccount.Address);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var lockAmount = balance > 0 ? (decimal)balance / (decimal)multiplier : 1m;

            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = request.FromWalletPrivateKey,
                ToWalletAddress = bridgePoolAddress,
                Amount = lockAmount
            };

            return await SendTokenAsync(sendRequest);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
    {
        return UnlockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in UnlockTokenAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }

            // Get recipient address from KeyManager using UnlockedByAvatarId
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, ProviderType.Value, request.UnlockedByAvatarId);
            if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                return result;
            }

            // Unlock token by transferring from bridge pool to recipient
            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var bridgePoolPrivateKey = _chainPrivateKey ?? string.Empty;

            if (string.IsNullOrWhiteSpace(bridgePoolPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Bridge pool private key is not configured");
                return result;
            }

            // Get unlock amount from bridge pool balance
            var erc20Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"}]";
            var bridgeAccount = new Account(bridgePoolPrivateKey);
            var web3Client = new Web3(bridgeAccount, _hostURI);
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var balanceOfFunction = erc20Contract.GetFunction("balanceOf");
            var balance = await balanceOfFunction.CallAsync<BigInteger>(bridgeAccount.Address);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var unlockAmount = balance > 0 ? (decimal)balance / (decimal)multiplier : 1m;

            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = bridgePoolPrivateKey,
                ToWalletAddress = toWalletResult.Result,
                Amount = unlockAmount
            };

            return await SendTokenAsync(sendRequest);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
    {
        return GetBalanceAsync(request).Result;
    }

    public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        var result = new OASISResult<double>();
        string errorMessage = "Error in GetBalanceAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Get native token balance (ETH, BNB, etc.)
            // Note: ERC20 token balance would require a separate TokenAddress parameter
            var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
            result.Result = (double)UnitConversion.Convert.FromWei(balance.Value);

            result.IsError = false;
            result.Message = "Balance retrieved successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
    {
        return GetTransactionsAsync(request).Result;
    }

    public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
    {
        var result = new OASISResult<IList<IWalletTransaction>>();
        string errorMessage = "Error in GetTransactionsAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            var transactions = new List<IWalletTransaction>();
            
            // Get transaction count for the address
            var transactionCount = await _web3Client.Eth.Transactions.GetTransactionCount.SendRequestAsync(request.WalletAddress);
            
            // Get recent transactions (last 10 by default)
            var blockNumber = await _web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var startBlock = blockNumber.Value - BigInteger.Min(100, blockNumber.Value); // Last 100 blocks
            
            for (var i = startBlock; i <= blockNumber.Value; i++)
            {
                try
                {
                    var block = await _web3Client.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(i));
                    if (block?.Transactions != null)
                    {
                        foreach (var tx in block.Transactions)
                        {
                            if (tx.From?.ToLower() == request.WalletAddress.ToLower() || 
                                tx.To?.ToLower() == request.WalletAddress.ToLower())
                            {
                                var walletTx = new WalletTransaction
                                {
                                    FromWalletAddress = tx.From,
                                    ToWalletAddress = tx.To,
                                    Amount = (double)UnitConversion.Convert.FromWei(tx.Value.Value),
                                    Description = $"Block {tx.BlockNumber?.Value}"
                                };
                                transactions.Add(walletTx);
                            }
                        }
                    }
                }
                catch
                {
                    // Skip blocks that can't be retrieved
                    continue;
                }
            }

            result.Result = transactions.Take(10).ToList();
            result.IsError = false;
            result.Message = $"Retrieved {result.Result.Count} transactions.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
    {
        return GenerateKeyPairAsync().Result;
    }

    public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        string errorMessage = "Error in GenerateKeyPairAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPublicAddress();

            result.Result = new KeyPairAndWallet
            {
                PrivateKey = privateKey,
                PublicKey = publicKey,
                WalletAddressLegacy = publicKey //TODO: Generate proper ethereum address format if needed
            };
            result.IsError = false;
            result.Message = "Key pair generated successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    // NFT-specific lock/unlock methods
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
    {
        return LockNFTAsync(request).Result;
    }
}
