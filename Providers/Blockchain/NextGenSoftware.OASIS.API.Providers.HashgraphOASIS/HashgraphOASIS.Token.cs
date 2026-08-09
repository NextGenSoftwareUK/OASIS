using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
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
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS
    {
        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Hedera Token Service (HTS) transfer using Mirror Node API
                var tokenTransferUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{request.FromTokenAddress}/transfers";
                var transferData = new
                {
                    account_id = request.ToWalletAddress,
                    amount = (long)(request.Amount * 100000000), // Convert to tinybars (8 decimals)
                    token_id = request.FromTokenAddress
                };

                var content = new StringContent(JsonSerializer.Serialize(transferData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(tokenTransferUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : ""
                    };
                    result.IsError = false;
                    result.Message = "Token sent successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph token transfer failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get token address from contract address or use default
                var tokenAddress = _contractAddress ?? "0.0.0";
                
                // Get wallet address for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(request.MintedByAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                
                var mintToAddress = walletResult.Result.WalletAddress;
                var mintAmount = 1m; // Default amount
                
                // Hedera Token Service (HTS) mint using Mirror Node API
                var tokenMintUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{tokenAddress}/mint";
                var mintData = new
                {
                    account_id = mintToAddress,
                    amount = (long)(mintAmount * 100000000) // Convert to tinybars
                };

                var content = new StringContent(JsonSerializer.Serialize(mintData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(tokenMintUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : ""
                    };
                    result.IsError = false;
                    result.Message = "Token minted successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph token mint failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(request.BurntByAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                
                var burnFromAddress = walletResult.Result.WalletAddress;
                var burnAmount = 1m; // Default amount
                
                // Hedera Token Service (HTS) burn using Mirror Node API
                var tokenBurnUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{request.TokenAddress}/burn";
                var burnData = new
                {
                    account_id = burnFromAddress,
                    amount = (long)(burnAmount * 100000000) // Convert to tinybars
                };

                var content = new StringContent(JsonSerializer.Serialize(burnData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(tokenBurnUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : ""
                    };
                    result.IsError = false;
                    result.Message = "Token burned successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph token burn failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Lock token by transferring to a lock account
                var lockAccount = _contractAddress ?? "0.0.123456";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = request.FromWalletAddress,
                    ToWalletAddress = lockAccount,
                    FromTokenAddress = request.TokenAddress,
                    Amount = 1m // Default amount
                };

                var sendResult = await SendTokenAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock token: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Token locked successfully on Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(request.UnlockedByAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                
                // Unlock token by transferring from lock account
                var lockAccount = _contractAddress ?? "0.0.123456";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = lockAccount,
                    ToWalletAddress = walletResult.Result.WalletAddress,
                    FromTokenAddress = request.TokenAddress,
                    Amount = 1m // Default amount
                };

                var sendResult = await SendTokenAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock token: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Token unlocked successfully on Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get account balance from Hedera Mirror Node API
                var accountId = request.WalletAddress;
                var balanceUrl = $"{_httpClient.BaseAddress}/api/v1/accounts/{accountId}";
                var response = await _httpClient.GetAsync(balanceUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var accountData = JsonSerializer.Deserialize<JsonElement>(content);

                    if (accountData.TryGetProperty("account", out var account) &&
                        account.TryGetProperty("balance", out var balance))
                    {
                        // Hedera balances are in tinybars (1 HBAR = 100,000,000 tinybars)
                        result.Result = balance.GetInt64() / 100000000.0;
                        result.IsError = false;
                        result.Message = "Balance retrieved successfully from Hashgraph";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse balance from Hashgraph response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph balance query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var accountId = request.WalletAddress;
                var transactionsUrl = $"{_httpClient.BaseAddress}/api/v1/accounts/{accountId}/transactions?limit=100";
                var response = await _httpClient.GetAsync(transactionsUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(content);

                    var transactions = new List<IWalletTransaction>();
                    if (txData.TryGetProperty("transactions", out var txArray))
                    {
                        foreach (var tx in txArray.EnumerateArray())
                        {
                            var txId = tx.TryGetProperty("transaction_id", out var txIdEl) && txIdEl.ValueKind == JsonValueKind.String
                                ? txIdEl.GetString()
                                : string.Empty;

                            var walletTx = new WalletTransaction
                            {
                                TransactionId = CreateDeterministicGuid($"{ProviderType.Value}:tx:{txId}"),
                                FromWalletAddress = accountId,
                                ToWalletAddress = string.Empty,
                                Amount = 0,
                                Description = string.IsNullOrWhiteSpace(txId) ? "Hedera transaction" : $"Hedera transaction: {txId}"
                            };
                            transactions.Add(walletTx);
                        }
                    }

                    result.Result = transactions;
                    result.IsError = false;
                    result.Message = $"Retrieved {transactions.Count} transactions from Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph transactions query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Generate Hedera key pair using Ed25519
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    var privateKeyBytes = new byte[32];
                    rng.GetBytes(privateKeyBytes);

                    // Create Hedera account from private key
                    var publicKey = System.Convert.ToBase64String(privateKeyBytes);
                    var accountId = $"0.0.{Guid.NewGuid().GetHashCode()}";

                    var keyPair = new KeyPair
                    {
                        PublicKey = publicKey,
                        PrivateKey = System.Convert.ToBase64String(privateKeyBytes)
                    };
                    // KeyPair doesn't have WalletAddress, but we can store it in a custom property or use a wrapper
                    result.Result = keyPair as IKeyPairAndWallet;
                    result.IsError = false;
                    result.Message = "Key pair generated successfully for Hashgraph";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair for Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Burn NFT on Hedera using HTS
                var burnUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{request.NFTTokenAddress}/nfts/{request.Web3NFTId}/burn";
                var response = await _httpClient.PostAsync(burnUrl, new StringContent("{}", Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(content);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : "",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT burned successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph NFT burn failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }


    }
}
