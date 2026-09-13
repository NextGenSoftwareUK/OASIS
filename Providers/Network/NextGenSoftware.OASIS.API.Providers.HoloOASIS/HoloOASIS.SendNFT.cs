using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
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
using System.IO;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using DataHelper = NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers.DataHelper;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public partial class HoloOASIS
    {
        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get mint to address from avatar ID
                var mintToWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, request.MintedByAvatarId);
                var mintToAddress = mintToWalletResult.IsError || string.IsNullOrWhiteSpace(mintToWalletResult.Result) 
                    ? "holo-pool" 
                    : mintToWalletResult.Result;

                // Get amount from metadata or use default
                var mintAmount = request.MetaData?.ContainsKey("Amount") == true && decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amount)
                    ? amount 
                    : 1m;
                var symbol = request.Symbol ?? "HOT";

                // Create Holochain token mint transaction
                var tokenMint = new
                {
                    to = mintToAddress,
                    amount = mintAmount,
                    symbol = symbol,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(tokenMint);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-mints", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "token-mint-completed",
                    };
                    
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = $"Token minted successfully: {mintAmount} {symbol} to {mintToAddress}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint token via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token via Holochain: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get from address from avatar ID
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, request.BurntByAvatarId);
                if (fromWalletResult.IsError || string.IsNullOrWhiteSpace(fromWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                var fromAddress = fromWalletResult.Result;

                // Use default amount and symbol (IBurnWeb3TokenRequest doesn't have these properties)
                var burnAmount = 1m;
                var symbol = "HOT";

                // Create Holochain token burn transaction
                var tokenBurn = new
                {
                    from = fromAddress,
                    amount = burnAmount,
                    symbol = symbol,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(tokenBurn);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-burns", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "token-burn-completed",
                    };
                    
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = $"Token burned successfully: {burnAmount} {symbol} from {fromAddress}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn token via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token via Holochain: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Lock token by transferring to bridge pool account on Holochain (one NFT – no amount)
                var bridgePoolAccount = "holo-pool";
                var tokenLock = new
                {
                    from = request.FromWalletAddress,
                    to = bridgePoolAccount,
                    amount = 1m,
                    symbol = "HOT",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(tokenLock);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-locks", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "token-lock-completed",
                    };
                    
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = "Token locked successfully on Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock token via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token via Holochain: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Unlock token by transferring from bridge pool account on Holochain
                var bridgePoolAccount = "holo-pool";
                // Get to address from avatar ID (IUnlockWeb3TokenRequest doesn't have ToWalletAddress)
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, request.UnlockedByAvatarId);
                var toAddress = toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result) 
                    ? "holo-pool" 
                    : toWalletResult.Result;
                var tokenUnlock = new
                {
                    from = bridgePoolAccount,
                    to = toAddress,
                    amount = 1m, // Default amount (IUnlockWeb3TokenRequest doesn't have Amount)
                    symbol = "HOT",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(tokenUnlock);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-unlocks", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "token-unlock-completed",
                    };
                    
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = "Token unlocked successfully on Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock token via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Use the existing GetAccountBalanceAsync method
                var balanceResult = await GetAccountBalanceAsync(request.WalletAddress);
                if (balanceResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, balanceResult.Message, balanceResult.Exception);
                    return result;
                }

                result.Result = (double)balanceResult.Result;
                result.IsError = false;
                result.Message = "Balance retrieved successfully from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance from Holochain: {ex.Message}", ex);
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query Holochain for transactions
                var transactionsUrl = $"{HoloNetworkURI}/api/v1/accounts/{request.WalletAddress}/transactions";
                var response = await _httpClient.GetAsync(transactionsUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var transactionsData = JsonSerializer.Deserialize<JsonElement>(content);

                    var transactions = new List<IWalletTransaction>();
                    if (transactionsData.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var txElement in transactionsData.EnumerateArray())
                        {
                            var transaction = new WalletTransaction
                            {
                                TransactionId = txElement.TryGetProperty("id", out var id) ? Guid.Parse(id.GetString()) : CreateDeterministicGuid($"{ProviderType.Value}:tx:{(txElement.TryGetProperty("hash", out var hash) ? hash.GetString() : (txElement.TryGetProperty("from", out var fromAddr) ? fromAddr.GetString() : "unknown"))}"),
                                FromWalletAddress = txElement.TryGetProperty("from", out var fromWallet) ? fromWallet.GetString() : "",
                                ToWalletAddress = txElement.TryGetProperty("to", out var to) ? to.GetString() : "",
                                Amount = txElement.TryGetProperty("amount", out var amount) ? (double)amount.GetDecimal() : 0.0,
                                Description = txElement.TryGetProperty("memo", out var memo) ? memo.GetString() : "",
                                TransactionType = TransactionType.Debit,
                                TransactionCategory = TransactionCategory.Other
                            };
                            transactions.Add(transaction);
                        }
                    }

                    result.Result = transactions;
                    result.IsError = false;
                    result.Message = $"Successfully retrieved {transactions.Count} transactions from Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain transactions query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from Holochain: {ex.Message}", ex);
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate Holochain key pair using KeyManager
                var keyManager = KeyManager.Instance;
                var keyPairResult = keyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.HoloOASIS);

                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to generate key pair: {keyPairResult.Message}");
                    return result;
                }

                result.Result = keyPairResult.Result;
                result.IsError = false;
                result.Message = "Key pair generated successfully for Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair for Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query Holochain for account balance
                var balanceUrl = $"{HoloNetworkURI}/api/v1/accounts/{accountAddress}/balance";
                var response = await _httpClient.GetAsync(balanceUrl, token);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var balanceData = JsonSerializer.Deserialize<JsonElement>(content);

                    if (balanceData.TryGetProperty("balance", out var balance))
                    {
                        result.Result = balance.GetDecimal();
                        result.IsError = false;
                        result.Message = "Balance retrieved successfully from Holochain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse balance from Holochain response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain balance query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate key pair and seed phrase using KeyManager
                var keyManager = KeyManager.Instance;
                var keyPairResult = keyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.HoloOASIS);

                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to create account: {keyPairResult.Message}");
                    return result;
                }

                // Generate seed phrase (12 words) for Holochain
                var seedPhrase = GenerateHolochainSeedPhrase();

                result.Result = (keyPairResult.Result.PublicKey, keyPairResult.Result.PrivateKey, seedPhrase);
                result.IsError = false;
                result.Message = "Account created successfully on Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating account on Holochain: {ex.Message}", ex);
            }
            return result;
        }

    }
}
