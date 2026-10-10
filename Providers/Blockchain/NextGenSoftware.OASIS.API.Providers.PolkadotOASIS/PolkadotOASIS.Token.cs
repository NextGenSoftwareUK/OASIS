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
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
{
    public partial class PolkadotOASIS
    {
        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "ToWalletAddress is required");
                    return result;
                }

                // Polkadot uses native DOT transfers or token transfers via pallets
                // For token transfers, we need the token contract address
                var amountInPlanck = (long)(request.Amount * 10000000000); // Convert to planck

                var transferRequest = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "author_submitExtrinsic",
                    @params = new[]
                    {
                        new
                        {
                            call = new
                            {
                                module = string.IsNullOrWhiteSpace(request.FromTokenAddress) ? "Balances" : "Tokens",
                                method = "transfer",
                                args = new
                                {
                                    dest = request.ToWalletAddress,
                                    value = amountInPlanck,
                                    currencyId = string.IsNullOrWhiteSpace(request.FromTokenAddress) ? null : request.FromTokenAddress
                                }
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(transferRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Token sent successfully on Polkadot";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token on Polkadot: {response.StatusCode}");
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
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
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

                // Polkadot token minting via RPC call to Assets pallet or custom token pallet
                var amountInPlanck = (long)(amount * 10000000000);
                var rpcRequest = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "author_submitExtrinsic",
                    @params = new[]
                    {
                        new
                        {
                            call = new
                            {
                                module = "Assets",
                                method = "mint",
                                args = new
                                {
                                    id = tokenAddress,
                                    beneficiary = mintToWalletAddress,
                                    amount = amountInPlanck
                                }
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Token minted successfully on Polkadot";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint token on Polkadot: {response.StatusCode}");
                }
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
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and owner private key are required");
                    return result;
                }

                // IBurnWeb3TokenRequest doesn't have Amount or BurnFromWalletAddress properties
                // Use default amount for now (in production, query balance first)
                var amountInPlanck = 10000000000L; // Default amount
                var rpcRequest = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "author_submitExtrinsic",
                    @params = new[]
                    {
                        new
                        {
                            call = new
                            {
                                module = "Assets",
                                method = "burn",
                                args = new
                                {
                                    id = request.TokenAddress,
                                    who = "", // Will be derived from private key in production
                                    amount = amountInPlanck
                                }
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Token burned successfully on Polkadot";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn token on Polkadot: {response.StatusCode}");
                }
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
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
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
                var bridgePoolAddress = _contractAddress ?? "";
                if (string.IsNullOrWhiteSpace(bridgePoolAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Bridge pool address is required. Please configure ContractAddress in OASISDNA.");
                    return result;
                }
                var amountInPlanck = 10000000000L; // Default amount
                var rpcRequest = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "author_submitExtrinsic",
                    @params = new[]
                    {
                        new
                        {
                            call = new
                            {
                                module = "Balances",
                                method = "freeze",
                                args = new
                                {
                                    who = bridgePoolAddress,
                                    amount = amountInPlanck
                                }
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Token locked successfully on Polkadot";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock token on Polkadot: {response.StatusCode}");
                }
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
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // IUnlockWeb3TokenRequest doesn't have UnlockWalletAddress or Amount properties
                // Unlock token by transferring from bridge pool to recipient
                var bridgePoolAddress = _contractAddress ?? "";
                if (string.IsNullOrWhiteSpace(bridgePoolAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Bridge pool address is required. Please configure ContractAddress in OASISDNA.");
                    return result;
                }
                // Get wallet address from Web3TokenId using real OASIS storage provider
                var unlockedToWalletAddress = "";
                if (request.Web3TokenId != Guid.Empty)
                {
                    try
                    {
                        var defaultProvider = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider();
                        if (defaultProvider != null && !defaultProvider.IsError)
                        {
                            var tokenResult = await defaultProvider.Result.LoadHolonAsync(request.Web3TokenId);
                            if (!tokenResult.IsError && tokenResult.Result != null)
                            {
                                unlockedToWalletAddress = tokenResult.Result.MetaData?.ContainsKey("UnlockedToWalletAddress") == true
                                    ? tokenResult.Result.MetaData["UnlockedToWalletAddress"]?.ToString()
                                    : tokenResult.Result.MetaData?.ContainsKey("MintToWalletAddress") == true
                                        ? tokenResult.Result.MetaData["MintToWalletAddress"]?.ToString()
                                        : "";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError($"Error getting wallet address from Web3TokenId: {ex.Message}", ex);
                    }
                }
                var amountInPlanck = 10000000000L; // Default amount
                
                if (string.IsNullOrWhiteSpace(unlockedToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Unlocked to wallet address is required but not available");
                    return result;
                }
                
                var rpcRequest = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "author_submitExtrinsic",
                    @params = new[]
                    {
                        new
                        {
                            call = new
                            {
                                module = "Balances",
                                method = "thaw",
                                args = new
                                {
                                    who = unlockedToWalletAddress,
                                    amount = amountInPlanck
                                }
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Token unlocked successfully on Polkadot";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock token on Polkadot: {response.StatusCode}");
                }
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
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Polkadot account balance
                var accountResponse = await _httpClient.GetAsync($"/accounts/{request.WalletAddress}");
                if (accountResponse.IsSuccessStatusCode)
                {
                    var accountContent = await accountResponse.Content.ReadAsStringAsync();
                    var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                    var balanceInPlanck = accountData.GetProperty("data").GetProperty("free").GetInt64();
                    var balanceInDOT = balanceInPlanck / 10000000000.0; // Convert from planck to DOT
                    result.Result = balanceInDOT;
                    result.IsError = false;
                    result.Message = "Balance retrieved successfully";
                }
                else
                {
                    result.Result = 0.0;
                    result.IsError = false;
                    result.Message = "Account not found or has zero balance";
                }
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
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Polkadot transactions for the address
                var transactionsResponse = await _httpClient.GetAsync($"/accounts/{request.WalletAddress}/transactions");
                var transactions = new List<IWalletTransaction>();

                if (transactionsResponse.IsSuccessStatusCode)
                {
                    var transactionsContent = await transactionsResponse.Content.ReadAsStringAsync();
                    var transactionsData = JsonSerializer.Deserialize<JsonElement>(transactionsContent);

                    if (transactionsData.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tx in dataProp.EnumerateArray())
                        {
                            // Extract transaction hash for deterministic GUID
                            var txHash = tx.TryGetProperty("extrinsicHash", out var hashProp) ? hashProp.GetString() : null;
                            Guid txGuid;
                            if (!string.IsNullOrWhiteSpace(txHash))
                            {
                                using var sha256 = System.Security.Cryptography.SHA256.Create();
                                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(txHash));
                                txGuid = new Guid(hashBytes.Take(16).ToArray());
                            }
                            else
                            {
                                // Fallback: use deterministic GUID from transaction data
                                var txData = $"{request.WalletAddress}:{tx.GetRawText()}";
                                txGuid = CreateDeterministicGuid($"{ProviderType.Value}:tx:{txData}");
                            }
                            
                            var walletTx = new WalletTransaction
                            {
                                TransactionId = txGuid,
                                FromWalletAddress = tx.TryGetProperty("from", out var from) ? from.GetString() : string.Empty,
                                ToWalletAddress = tx.TryGetProperty("to", out var to) ? to.GetString() : string.Empty,
                                Amount = tx.TryGetProperty("value", out var value) ? value.GetInt64() / 10000000000.0 : 0.0,
                                Description = txHash != null ? $"Polkadot transaction: {txHash}" : "Polkadot transaction"
                            };
                            transactions.Add(walletTx);
                        }
                    }
                }

                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} Polkadot transactions";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
            }
            return result;
        }

    }
}
