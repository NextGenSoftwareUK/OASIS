using System;
using Nethereum.Hex.HexConvertors.Extensions;
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
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS
{
    public partial class FantomOASIS_Legacy
    {
        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new FantomTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "ToWalletAddress is required");
                    return result;
                }

                // Fantom token transfer via Nethereum (EVM-compatible)
                if (string.IsNullOrWhiteSpace(request.FromTokenAddress))
                {
                    // Native FTM transfer
                    var transactionReceipt = await _web3Client.Eth.GetEtherTransferService()
                        .TransferEtherAndWaitForReceiptAsync(request.ToWalletAddress, (decimal)request.Amount, 2);
                    result.Result.TransactionResult = transactionReceipt.TransactionHash;
                    result.IsError = false;
                    result.Message = "FTM sent successfully";
                }
                else
                {
                    // ERC-20 token transfer
                    var contract = _web3Client.Eth.GetContract(GetERC20ABI(), request.FromTokenAddress);
                    var transferFunction = contract.GetFunction("transfer");
                    var amountInWei = new HexBigInteger((BigInteger)(request.Amount * 1000000000000000000)); // Convert to wei
                    var transactionReceipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                        _account.Address,
                        new HexBigInteger(21000),
                        null,
                        null,
                        request.ToWalletAddress,
                        amountInWei);
                    result.Result.TransactionResult = transactionReceipt.TransactionHash;
                    result.IsError = false;
                    result.Message = "Token sent successfully on Fantom";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new FantomTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || request.MetaData == null ||
                    !request.MetaData.ContainsKey("TokenAddress") || string.IsNullOrWhiteSpace(request.MetaData["TokenAddress"]?.ToString()) ||
                    !request.MetaData.ContainsKey("MintToWalletAddress") || string.IsNullOrWhiteSpace(request.MetaData["MintToWalletAddress"]?.ToString()))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and mint to wallet address are required in MetaData");
                    return result;
                }

                var tokenAddress = request.MetaData["TokenAddress"].ToString();
                var mintToWalletAddress = request.MetaData["MintToWalletAddress"].ToString();
                var amount = request.MetaData?.ContainsKey("Amount") == true && decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amt) ? amt : 0m;

                // Fantom token minting via Nethereum (requires ERC-20 mint function)
                var contract = _web3Client.Eth.GetContract(GetERC20ABI(), tokenAddress);
                var mintFunction = contract.GetFunction("mint");
                var amountInWei = new HexBigInteger((BigInteger)(amount * 1000000000000000000));
                var transactionReceipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    new HexBigInteger(21000),
                    null,
                    null,
                    mintToWalletAddress,
                    amountInWei);
                result.Result.TransactionResult = transactionReceipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token minted successfully on Fantom";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new FantomTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) ||
                    string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and owner private key are required");
                    return result;
                }

                var senderAccount = new Account(request.OwnerPrivateKey);
                var web3Client = new Web3(senderAccount, _rpcEndpoint);

                // Fantom token burning via Nethereum (requires ERC-20 burn function)
                var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[],\"type\":\"function\"}]";
                var contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
                var burnFunction = contract.GetFunction("burn");
                var decimalsFunction = contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = BigInteger.Pow(10, decimals);
                // IBurnWeb3TokenRequest doesn't have Amount property, so we'll burn the full balance
                var balanceFunction = contract.GetFunction("balanceOf");
                var balance = await balanceFunction.CallAsync<BigInteger>(senderAccount.Address);
                var amountBigInt = balance;
                var transactionReceipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(21000),
                    null,
                    null,
                    amountBigInt);
                result.Result.TransactionResult = transactionReceipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token burned successfully on Fantom";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new FantomTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) ||
                    string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                    return result;
                }

                // ILockWeb3TokenRequest doesn't have Amount or LockWalletAddress properties
                // Lock token by transferring to bridge pool (OASIS account)
                var senderAccount = new Account(request.FromWalletPrivateKey);
                var web3Client = new Web3(senderAccount, _rpcEndpoint);
                var erc20Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
                var contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
                var balanceFunction = contract.GetFunction("balanceOf");
                var balance = await balanceFunction.CallAsync<BigInteger>(senderAccount.Address);
                var transferFunction = contract.GetFunction("transfer");
                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var transactionReceipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(21000),
                    null,
                    null,
                    bridgePoolAddress,
                    balance);
                result.Result.TransactionResult = transactionReceipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token locked successfully on Fantom";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new FantomTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // IUnlockWeb3TokenRequest doesn't have UnlockWalletAddress or Amount properties
                // Unlock token by transferring from bridge pool (OASIS account) to recipient
                // Get recipient from Web3TokenId using real OASIS API
                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var unlockedToWalletAddress = "";

                // Get wallet address from Web3TokenId using real OASIS API
                if (request.Web3TokenId != Guid.Empty)
                {
                    try
                    {
                        // Query OASIS storage for the locked token record
                        var providerResult = ProviderManager.Instance == null
                            ? new OASISResult<IOASISStorageProvider> { IsError = true, Message = "ProviderManager not initialized" }
                            : await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(global::NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
                        var tokenResult = providerResult.IsError || providerResult.Result == null
                            ? new OASISResult<IHolon> { IsError = true, Message = providerResult.Message }
                            : await providerResult.Result.LoadHolonAsync(request.Web3TokenId);

                        if (!tokenResult.IsError && tokenResult.Result != null)
                        {
                            // Extract wallet address from token metadata
                            unlockedToWalletAddress = tokenResult.Result.MetaData?.ContainsKey("UnlockedToWalletAddress") == true
                                ? tokenResult.Result.MetaData["UnlockedToWalletAddress"]?.ToString()
                                : tokenResult.Result.MetaData?.ContainsKey("MintToWalletAddress") == true
                                    ? tokenResult.Result.MetaData["MintToWalletAddress"]?.ToString()
                                    : "";
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError($"Error getting wallet address from Web3TokenId: {ex.Message}", ex);
                    }
                }

                // Fallback: try to get from UnlockedByAvatarId if available
                if (string.IsNullOrWhiteSpace(unlockedToWalletAddress) && request.UnlockedByAvatarId != Guid.Empty)
                {
                    unlockedToWalletAddress = await GetWalletAddressForAvatarAsync(request.UnlockedByAvatarId);
                }

                if (string.IsNullOrWhiteSpace(unlockedToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Unlocked to wallet address is required. Please provide Web3TokenId or UnlockedByAvatarId.");
                    return result;
                }

                var senderAccount = new Account(_privateKey);
                var web3Client = new Web3(senderAccount, _rpcEndpoint);
                var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
                var contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
                var balanceFunction = contract.GetFunction("balanceOf");
                var balance = await balanceFunction.CallAsync<BigInteger>(bridgePoolAddress);
                var transferFunction = contract.GetFunction("transfer");
                var transactionReceipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(21000),
                    null,
                    null,
                    unlockedToWalletAddress,
                    balance);
                result.Result.TransactionResult = transactionReceipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token unlocked successfully on Fantom";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Fantom native FTM balance via Nethereum (real implementation)
                // IGetWeb3WalletBalanceRequest doesn't have TokenAddress property
                var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
                result.Result = (double)(balance.Value / (BigInteger)1000000000000000000); // Convert from wei to FTM
                result.IsError = false;
                result.Message = "FTM balance retrieved successfully";
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
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Fantom transactions via Nethereum
                var transactions = new List<IWalletTransaction>();
                var blockNumber = await _web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                var limit = 10;

                for (var i = 0; i < limit && blockNumber.Value > 0; i++)
                {
                    try
                    {
                        var block = await _web3Client.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockNumber);
                        foreach (var tx in block.Transactions)
                        {
                            if (tx.From == request.WalletAddress || tx.To == request.WalletAddress)
                            {
                                var walletTx = new WalletTransaction
                                {
                                    TransactionId = Guid.NewGuid(),
                                    FromWalletAddress = tx.From,
                                    ToWalletAddress = tx.To ?? string.Empty,
                                    Amount = (double)(tx.Value.Value / (BigInteger)1000000000000000000),
                                    Description = $"Fantom transaction: {tx.TransactionHash}"
                                };
                                transactions.Add(walletTx);
                            }
                        }
                        blockNumber = new HexBigInteger(blockNumber.Value - 1);
                    }
                    catch
                    {
                        break;
                    }
                }

                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} Fantom transactions";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
        {
            return GenerateKeyPairAsync(request).Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                // Generate Fantom key pair using KeyHelper (EVM-compatible secp256k1)
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Fantom key pair generated successfully using Nethereum";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Gets ERC-20 token ABI for Fantom token operations
        /// </summary>
        private string GetERC20ABI()
        {
            return @"[{""constant"":true,""inputs"":[{""name"":"""",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":"""",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""mint"",""outputs"":[],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_value"",""type"":""uint256""}],""name"":""burn"",""outputs"":[],""type"":""function""}]";
        }


    }
}
