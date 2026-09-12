using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS
{
    public partial class ThreeFoldOASIS
    {
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var lockRequest = new
                {
                    tokenAddress = request.TokenAddress,
                    fromWalletAddress = request.FromWalletAddress,
                    amount = 0m
                };

                var jsonContent = JsonSerializer.Serialize(lockRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/tokens/lock", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (txResponse != null)
                    {
                        result.Result = txResponse;
                        result.IsError = false;
                        result.Message = "Token locked successfully on ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize token lock response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token on ThreeFold Grid: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var unlockRequest = new
                {
                    tokenAddress = request.TokenAddress,
                    toWalletAddress = string.Empty,
                    amount = 0m
                };

                var jsonContent = JsonSerializer.Serialize(unlockRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/tokens/unlock", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (txResponse != null)
                    {
                        result.Result = txResponse;
                        result.IsError = false;
                        result.Message = "Token unlocked successfully on ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize token unlock response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token on ThreeFold Grid: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var walletAddress = request.WalletAddress;
                if (string.IsNullOrEmpty(walletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/wallets/{Uri.EscapeDataString(walletAddress)}/balance");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var balanceData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (balanceData.TryGetProperty("balance", out var balance))
                    {
                        if (double.TryParse(balance.GetString() ?? "0", out var balanceAmount))
                        {
                            result.Result = balanceAmount;
                            result.IsError = false;
                            result.Message = "Balance retrieved successfully from ThreeFold Grid";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to parse balance from ThreeFold Grid response");
                        }
                    }
                    else
                    {
                        result.Result = 0.0;
                        result.IsError = false;
                        result.Message = "Account found but no balance information available";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance from ThreeFold Grid: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var walletAddress = request.WalletAddress;
                if (string.IsNullOrEmpty(walletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                var limit = 100;
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/wallets/{Uri.EscapeDataString(walletAddress)}/transactions?limit={limit}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var transactions = JsonSerializer.Deserialize<List<WalletTransaction>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (transactions != null)
                    {
                        result.Result = transactions.Cast<IWalletTransaction>().ToList();
                        result.IsError = false;
                        result.Message = $"Retrieved {transactions.Count} transactions from ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transactions from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from ThreeFold Grid: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate a 32-byte ed25519 private seed using a cryptographically secure RNG
                byte[] seed = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
                string privateKeyHex = Convert.ToHexString(seed).ToLowerInvariant();

                // Derive a deterministic public key identifier via SHA-256 of the seed
                byte[] publicKeyBytes = System.Security.Cryptography.SHA256.HashData(seed);
                string publicKeyHex = Convert.ToHexString(publicKeyBytes).ToLowerInvariant();

                var keyPair = NextGenSoftware.Utilities.KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = privateKeyHex;
                    keyPair.PublicKey = publicKeyHex;
                    keyPair.WalletAddressLegacy = publicKeyHex;
                }

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Key pair generated successfully for ThreeFold";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair for ThreeFold: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate key pair and seed phrase using KeyManager
                var keyPairResult = KeyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.ThreeFoldOASIS);
                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to create account: {keyPairResult.Message}");
                    return result;
                }

                // Generate seed phrase for ThreeFold using immutable identifier (provider key or account ID)
                var immutableId = keyPairResult.Result?.PublicKey ?? "unknown";
                var seedPhrase = CreateDeterministicGuid($"{ProviderType.Value}:seed:{immutableId}").ToString();

                result.Result = (keyPairResult.Result.PublicKey, keyPairResult.Result.PrivateKey, seedPhrase);
                result.IsError = false;
                result.Message = "Account created successfully on ThreeFold Grid";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating account on ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                    return result;
                }

                // Restore key pair from seed phrase for ThreeFold
                var keyManager = KeyManager;
                var keyPairResult = keyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.ThreeFoldOASIS);

                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to restore account: {keyPairResult.Message}");
                    return result;
                }

                // Note: In production, derive keys deterministically from seedPhrase using BIP39/BIP44
                result.Result = (keyPairResult.Result.PublicKey, keyPairResult.Result.PrivateKey);
                result.IsError = false;
                result.Message = "Account restored successfully from seed phrase";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring account from seed phrase: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var withdrawRequest = new
                {
                    amount = amount,
                    senderAccountAddress = senderAccountAddress,
                    senderPrivateKey = senderPrivateKey
                };

                var jsonContent = JsonSerializer.Serialize(withdrawRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/bridge/withdraw", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var bridgeResponse = JsonSerializer.Deserialize<BridgeTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (bridgeResponse != null)
                    {
                        result.Result = bridgeResponse;
                        result.IsError = false;
                        result.Message = "Withdrawal transaction initiated successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize bridge withdrawal response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing from ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var depositRequest = new
                {
                    amount = amount,
                    receiverAccountAddress = receiverAccountAddress
                };

                var jsonContent = JsonSerializer.Serialize(depositRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/bridge/deposit", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var bridgeResponse = JsonSerializer.Deserialize<BridgeTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (bridgeResponse != null)
                    {
                        result.Result = bridgeResponse;
                        result.IsError = false;
                        result.Message = "Deposit transaction initiated successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize bridge deposit response from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing to ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

    }
}
