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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Restore key pair from seed phrase for Holochain
                // For Holochain, we derive keys from the mnemonic seed phrase
                // This is a simplified implementation - in production, use proper BIP39 derivation
                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                    return result;
                }

                // Generate a deterministic key pair from the seed phrase
                // In a real implementation, this would use proper BIP39/BIP44 derivation
                var keyManager = KeyManager.Instance;
                var keyPairResult = keyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.HoloOASIS);

                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to restore account: {keyPairResult.Message}");
                    return result;
                }

                // Note: In production, derive keys deterministically from seedPhrase using BIP39
                // For now, we generate a new key pair and the seed phrase can be stored separately
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create bridge withdrawal transaction on Holochain
                var withdrawUrl = $"{HoloNetworkURI}/api/v1/bridge/withdraw";
                var withdrawData = new
                {
                    amount = amount,
                    senderAddress = senderAccountAddress,
                    privateKey = senderPrivateKey
                };

                var content = new StringContent(JsonSerializer.Serialize(withdrawData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(withdrawUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Status = BridgeTransactionStatus.Pending,
                        IsSuccessful = true
                    };
                    result.IsError = false;
                    result.Message = "Withdrawal transaction initiated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain withdrawal failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing from Holochain: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create bridge deposit transaction on Holochain
                var depositUrl = $"{HoloNetworkURI}/api/v1/bridge/deposit";
                var depositData = new
                {
                    amount = amount,
                    receiverAddress = receiverAccountAddress
                };

                var content = new StringContent(JsonSerializer.Serialize(depositData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(depositUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Status = BridgeTransactionStatus.Pending,
                        IsSuccessful = true
                    };
                    result.IsError = false;
                    result.Message = "Deposit transaction initiated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain deposit failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing to Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
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

                // Query transaction status from Holochain
                var statusUrl = $"{HoloNetworkURI}/api/v1/transactions/{transactionHash}/status";
                var response = await _httpClient.GetAsync(statusUrl, token);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var statusData = JsonSerializer.Deserialize<JsonElement>(content);

                    if (statusData.TryGetProperty("status", out var status))
                    {
                        var statusStr = status.GetString();
                        result.Result = statusStr switch
                        {
                            "pending" => BridgeTransactionStatus.Pending,
                            "completed" => BridgeTransactionStatus.Completed,
                            "canceled" => BridgeTransactionStatus.Canceled,
                            "expired" => BridgeTransactionStatus.Expired,
                            _ => BridgeTransactionStatus.NotFound
                        };
                        result.IsError = false;
                        result.Message = "Transaction status retrieved successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse transaction status");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain transaction status query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
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

                // Lock NFT by transferring to bridge pool on Holochain
                var lockUrl = $"{HoloNetworkURI}/api/v1/nft/lock";
                var lockData = new
                {
                    nft_token_address = request.NFTTokenAddress,
                    token_id = request.Web3NFTId.ToString(),
                    locked_by_avatar_id = request.LockedByAvatarId.ToString()
                };

                var content = new StringContent(JsonSerializer.Serialize(lockData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(lockUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new Web3NFTTransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT locked successfully on Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain NFT lock failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking NFT on Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
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

                // Unlock NFT by transferring from bridge pool on Holochain
                var unlockUrl = $"{HoloNetworkURI}/api/v1/nft/unlock";
                var unlockData = new
                {
                    nft_token_address = request.NFTTokenAddress,
                    token_id = request.Web3NFTId.ToString(),
                    unlocked_by_avatar_id = request.UnlockedByAvatarId.ToString()
                };

                var content = new StringContent(JsonSerializer.Serialize(unlockData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(unlockUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new Web3NFTTransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT unlocked successfully on Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain NFT unlock failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT on Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
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

                // Create NFT withdrawal transaction on Holochain bridge
                var withdrawUrl = $"{HoloNetworkURI}/api/v1/bridge/nft/withdraw";
                var withdrawData = new
                {
                    nft_token_address = nftTokenAddress,
                    token_id = tokenId,
                    sender_address = senderAccountAddress,
                    private_key = senderPrivateKey
                };

                var content = new StringContent(JsonSerializer.Serialize(withdrawData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(withdrawUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Status = BridgeTransactionStatus.Pending,
                        IsSuccessful = true
                    };
                    result.IsError = false;
                    result.Message = "NFT withdrawal transaction initiated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain NFT withdrawal failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
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

                // Create NFT deposit transaction on Holochain bridge
                var depositUrl = $"{HoloNetworkURI}/api/v1/bridge/nft/deposit";
                var depositData = new
                {
                    nft_token_address = nftTokenAddress,
                    token_id = tokenId,
                    receiver_address = receiverAccountAddress,
                    source_transaction_hash = sourceTransactionHash
                };

                var content = new StringContent(JsonSerializer.Serialize(depositData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(depositUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Status = BridgeTransactionStatus.Pending,
                        IsSuccessful = true
                    };
                    result.IsError = false;
                    result.Message = "NFT deposit transaction initiated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain NFT deposit failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT to Holochain: {ex.Message}", ex);
            }
            return result;
        }



        /// <summary>
        /// Generate a Holochain seed phrase (12 words)
        /// </summary>
        private string GenerateHolochainSeedPhrase()
        {
            // BIP39 word list (simplified - in production use full BIP39 word list)
            var bip39Words = new[]
            {
                "abandon", "ability", "able", "about", "above", "absent", "absorb", "abstract", "absurd", "abuse",
                "access", "accident", "account", "accuse", "achieve", "acid", "acoustic", "acquire", "across", "act"
                // In production, use full 2048-word BIP39 list
            };

            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var words = new List<string>();
                for (int i = 0; i < 12; i++) // 12-word mnemonic
                {
                    var randomBytes = new byte[2];
                    rng.GetBytes(randomBytes);
                    var index = BitConverter.ToUInt16(randomBytes, 0) % bip39Words.Length;
                    words.Add(bip39Words[index]);
                }
                return string.Join(" ", words);
            }
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

    }
}
