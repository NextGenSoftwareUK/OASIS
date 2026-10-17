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
    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
    {
        return BurnTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in BurnTokenAsync method in BaseOASIS. Reason: ";

        try
        {
            if (!_isActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
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
            var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_amount\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            // IBurnWeb3TokenRequest doesn't have Amount property, so we'll burn the full balance
            var balanceFunction = erc20Contract.GetFunction("balanceOf");
            var balance = await balanceFunction.CallAsync<BigInteger>(senderAccount.Address);
            var amountBigInt = balance;
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
            result.Message = "Token burned successfully on Base.";
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
        string errorMessage = "Error in LockTokenAsync method in BaseOASIS. Reason: ";

        try
        {
            if (!_isActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                return result;
            }

            // Lock token by transferring to bridge pool (OASIS account)
            // ILockWeb3TokenRequest doesn't have Amount property, so we'll lock the full balance
            var web3Client = new Web3(new Account(request.FromWalletPrivateKey), _hostURI);
            var erc20Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var balanceFunction = erc20Contract.GetFunction("balanceOf");
            var account = new Account(request.FromWalletPrivateKey);
            var balance = await balanceFunction.CallAsync<BigInteger>(account.Address);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amount = (decimal)(balance / multiplier);
            
            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = request.FromWalletPrivateKey,
                ToWalletAddress = _oasisAccount.Address,
                Amount = amount
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
        string errorMessage = "Error in UnlockTokenAsync method in BaseOASIS. Reason: ";

        try
        {
            if (!_isActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }
            
            // IUnlockWeb3TokenRequest doesn't have UnlockedToWalletAddress or Amount properties
            // Query the bridge pool balance to determine unlock amount
            var web3Client = new Web3(_oasisAccount, _hostURI);
            var erc20Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var balanceFunction = erc20Contract.GetFunction("balanceOf");
            var bridgeBalance = await balanceFunction.CallAsync<BigInteger>(_oasisAccount.Address);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amount = bridgeBalance > 0 ? (decimal)(bridgeBalance / multiplier) : 0m;
            
            // Get wallet address from avatar ID if available, otherwise use a default
            var unlockedToWalletAddress = "";
            if (request.UnlockedByAvatarId != Guid.Empty)
            {
                var walletResult = await NextGenSoftware.OASIS.API.Core.Helpers.WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, this.ProviderType.Value, request.UnlockedByAvatarId);
                if (!walletResult.IsError && !string.IsNullOrWhiteSpace(walletResult.Result))
                {
                    unlockedToWalletAddress = walletResult.Result;
                }
            }
            
            if (string.IsNullOrWhiteSpace(unlockedToWalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Unlocked to wallet address is required but could not be determined from avatar ID");
                return result;
            }
            
            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "No tokens available in bridge pool to unlock");
                return result;
            }

            // Unlock token by transferring from bridge pool (OASIS account) to recipient
            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = _chainPrivateKey, // OASIS account private key
                ToWalletAddress = unlockedToWalletAddress,
                Amount = amount
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
        try
        {
            if (!_isActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
            var balanceInEther = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
            result.Result = (double)balanceInEther;
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
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
        try
        {
            if (!_isActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Get transactions for the wallet address
            // Note: This is a simplified implementation. In production, you'd query a block explorer API
            // or maintain an index of transactions
            var transactions = new List<IWalletTransaction>();
            result.Result = transactions;
            result.IsError = false;
            result.Message = "Transactions retrieved successfully (simplified implementation).";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
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
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Generate a new Ethereum key pair
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPubKey();
            var address = ecKey.GetPublicAddress();

            // Base uses secp256k1 (same as Ethereum); keys already generated above via Nethereum.
            var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            if (keyPair != null)
            {
                keyPair.PrivateKey = privateKey;
                keyPair.PublicKey = publicKey.ToHex();
                keyPair.WalletAddressLegacy = address;
            }

            result.Result = keyPair;
            result.IsError = false;
            result.Message = "Key pair generated successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
        }
        return result;
    }



    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        try
        {
            if (!_isActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(accountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Account address is required");
                return result;
            }

            var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(accountAddress);
            result.Result = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting Base account balance: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPublicAddress();

            result.Result = (publicKey, privateKey, string.Empty);
            result.IsError = false;
            result.Message = "Base account created successfully. Seed phrase not applicable for direct key generation.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error creating Base account: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey)>();
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            var wallet = new Nethereum.HdWallet.Wallet(seedPhrase, null);
            var account = wallet.GetAccount(0);

            result.Result = (account.Address, account.PrivateKey);
            result.IsError = false;
            result.Message = "Base account restored successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error restoring Base account: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!_isActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                return result;
            }

            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                return result;
            }

            var account = new Account(senderPrivateKey, _chainId);
            var web3 = new Web3(account, _hostURI);

            var bridgePoolAddress = _oasisAccount?.Address ?? _contractAddress;
            var transactionReceipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(bridgePoolAddress, amount, 2);

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = transactionReceipt.TransactionHash,
                IsSuccessful = transactionReceipt.Status.Value == 1,
                Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

}
