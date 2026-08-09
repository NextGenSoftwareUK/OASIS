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
        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new BNBChainTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "ToWalletAddress is required");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.FromTokenAddress))
                {
                    var receipt = await _web3Client.Eth.GetEtherTransferService()
                        .TransferEtherAndWaitForReceiptAsync(request.ToWalletAddress, (decimal)request.Amount, 2);
                    result.Result.TransactionResult = receipt.TransactionHash;
                    result.IsError = false;
                    result.Message = "BNB sent successfully";
                }
                else
                {
                    var contract = _web3Client.Eth.GetContract(GetERC20ABI(), request.FromTokenAddress);
                    var transfer = contract.GetFunction("transfer");
                    var amountInWei = new HexBigInteger((BigInteger)(request.Amount * 1000000000000000000));
                    var receipt = await transfer.SendTransactionAndWaitForReceiptAsync(_account.Address, new HexBigInteger(60000), null, CancellationToken.None, request.ToWalletAddress, amountInWei);
                    result.Result.TransactionResult = receipt.TransactionHash;
                    result.IsError = false;
                    result.Message = "Token sent successfully on BNB Chain";
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
            var result = new OASISResult<ITransactionResponse>(new BNBChainTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                // IMintWeb3TokenRequest has TokenAddress and MintToWalletAddress in MetaData
                var tokenAddress = request.MetaData?.ContainsKey("TokenAddress") == true 
                    ? request.MetaData["TokenAddress"]?.ToString() 
                    : "";
                var mintToWalletAddress = request.MetaData?.ContainsKey("MintToWalletAddress") == true 
                    ? request.MetaData["MintToWalletAddress"]?.ToString() 
                    : "";

                if (request == null || string.IsNullOrWhiteSpace(tokenAddress) || string.IsNullOrWhiteSpace(mintToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "TokenAddress and MintToWalletAddress are required");
                    return result;
                }

                var contract = _web3Client.Eth.GetContract(GetERC20ABI(), tokenAddress);
                var mint = contract.GetFunction("mint");
                var amountInWei = new HexBigInteger((BigInteger)(1 * 1000000000000000000)); // IMintWeb3TokenRequest has no Amount; using 1 token
                var receipt = await mint.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    new HexBigInteger(60000),
                    null,
                    CancellationToken.None,
                    mintToWalletAddress,
                    amountInWei);
                result.Result.TransactionResult = receipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token minted successfully on BNB Chain";
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
            var result = new OASISResult<ITransactionResponse>(new BNBChainTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "TokenAddress is required");
                    return result;
                }

                var contract = _web3Client.Eth.GetContract(GetERC20ABI(), request.TokenAddress);
                var burn = contract.GetFunction("burn");
                var amountInWei = new HexBigInteger(0); // IBurnWeb3TokenRequest has no Amount; burn typically uses balance
                var receipt = await burn.SendTransactionAndWaitForReceiptAsync(_account.Address, CancellationToken.None, amountInWei);
                result.Result.TransactionResult = receipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token burned successfully on BNB Chain";
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
            var result = new OASISResult<ITransactionResponse>(new BNBChainTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || string.IsNullOrWhiteSpace(request.FromWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "TokenAddress and FromWalletAddress are required");
                    return result;
                }

                var contract = _web3Client.Eth.GetContract(GetERC20ABI(), request.TokenAddress);
                var lockFn = contract.GetFunction("lock");
                var amountInWei = new HexBigInteger(0); // ILockWeb3TokenRequest has no Amount
                var receipt = await lockFn.SendTransactionAndWaitForReceiptAsync(_account.Address, new HexBigInteger(60000), null, CancellationToken.None, request.FromWalletAddress, amountInWei);
                result.Result.TransactionResult = receipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token locked successfully on BNB Chain";
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
            var result = new OASISResult<ITransactionResponse>(new BNBChainTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "TokenAddress is required");
                    return result;
                }

                var unlockToAddress = string.Empty;
                if (request.UnlockedByAvatarId != Guid.Empty)
                {
                    var walletResult = await NextGenSoftware.OASIS.API.Core.Helpers.WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, ProviderType.Value, request.UnlockedByAvatarId);
                    if (!walletResult.IsError && !string.IsNullOrWhiteSpace(walletResult.Result))
                        unlockToAddress = walletResult.Result;
                }
                if (string.IsNullOrWhiteSpace(unlockToAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "UnlockWalletAddress could not be determined from UnlockedByAvatarId");
                    return result;
                }

                var contract = _web3Client.Eth.GetContract(GetERC20ABI(), request.TokenAddress);
                var unlockFn = contract.GetFunction("unlock");
                var amountInWei = new HexBigInteger(0);
                var receipt = await unlockFn.SendTransactionAndWaitForReceiptAsync(_account.Address, new HexBigInteger(60000), null, CancellationToken.None, unlockToAddress, amountInWei);
                result.Result.TransactionResult = receipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token unlocked successfully on BNB Chain";
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
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
                result.Result = (double)(balance.Value / (BigInteger)1000000000000000000);
                result.IsError = false;
                result.Message = "BNB balance retrieved successfully";
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
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

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
                                    Description = $"BNB Chain transaction: {tx.TransactionHash}"
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
                result.Message = $"Retrieved {transactions.Count} BNB Chain transactions";
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
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                result.Result = keyPair;
                result.IsError = false;
                result.Message = "BNB Chain key pair generated successfully using Nethereum";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        private string GetERC20ABI()
        {
            return @"[{""constant"":true,""inputs"":[{""name"":"""",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":"""",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""mint"",""outputs"":[],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_value"",""type"":""uint256""}],""name"":""burn"",""outputs"":[],""type"":""function""}]";
        }

    }
}
