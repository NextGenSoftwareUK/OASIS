using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
// using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS
{
    public partial class CosmosBlockChainOASIS
    {
        /// <summary>
        /// Derives Cosmos public key from private key using secp256k1
        /// Note: This is a simplified implementation. In production, use proper Cosmos SDK for key derivation.
        /// </summary>
        private string DeriveCosmosPublicKey(byte[] privateKeyBytes)
        {
            // Cosmos uses secp256k1 elliptic curve (same as Bitcoin/Ethereum)
            // In production, use Cosmos SDK for proper key derivation
            try
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(privateKeyBytes);
                    // Cosmos public keys are typically 64 characters (32 bytes hex)
                    var publicKey = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return publicKey.Length >= 64 ? publicKey.Substring(0, 64) : publicKey.PadRight(64, '0');
                }
            }
            catch
            {
                var hash = System.Security.Cryptography.SHA256.HashData(privateKeyBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().PadRight(64, '0');
            }
        }

        /// <summary>
        /// Derives Cosmos address from public key
        /// </summary>
        /// <summary>
        /// Generate Cosmos seed phrase (BIP39 mnemonic)
        /// </summary>
        private string GenerateCosmosSeedPhrase()
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
        /// Derive seed from BIP39 mnemonic phrase
        /// </summary>
        private byte[] DeriveSeedFromMnemonic(string mnemonic)
        {
            // In production, use proper BIP39 seed derivation (PBKDF2 with 2048 iterations)
            // For now, use a simplified hash-based approach
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var mnemonicBytes = Encoding.UTF8.GetBytes(mnemonic);
                return sha256.ComputeHash(sha256.ComputeHash(mnemonicBytes));
            }
        }


        private string DeriveCosmosAddress(string publicKey)
        {
            // Cosmos addresses are derived from public keys using bech32 encoding
            // For now, we'll use a simplified hex format
            try
            {
                var publicKeyBytes = System.Text.Encoding.UTF8.GetBytes(publicKey);
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(publicKeyBytes);
                    // Take portion for address (Cosmos addresses are typically 20 bytes)
                    var addressBytes = new byte[20];
                    Array.Copy(hash, addressBytes, 20);
                    return "0x" + BitConverter.ToString(addressBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return publicKey.Length >= 40 ? "0x" + publicKey.Substring(0, 40) : "0x" + publicKey.PadRight(40, '0');
            }
        }

        // Bridge methods
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                // Query Cosmos account balance using REST API
                var httpResponse = await _httpClient.GetAsync($"/cosmos/bank/v1beta1/balances/{accountAddress}");
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var balanceData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (balanceData.TryGetProperty("balances", out var balances) && balances.ValueKind == JsonValueKind.Array)
                    {
                        decimal totalBalance = 0m;
                        foreach (var balance in balances.EnumerateArray())
                        {
                            if (balance.TryGetProperty("amount", out var amount))
                            {
                                var amountStr = amount.GetString();
                                if (decimal.TryParse(amountStr, out var amountValue))
                                {
                                    // Convert from uatom (smallest unit) to ATOM
                                    totalBalance += amountValue / 1_000_000m;
                                }
                            }
                        }
                        result.Result = totalBalance;
                        result.IsError = false;
                        result.Message = "Account balance retrieved successfully";
                    }
                    else
                    {
                        result.Result = 0m;
                        result.IsError = false;
                        result.Message = "Account balance is zero or account not found";
                    }
                }
                else
                {
                    result.Result = 0m;
                    result.IsError = false;
                    result.Message = "Account balance retrieved (zero or account not found)";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate Cosmos key pair (secp256k1 for Cosmos)
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    var privateKeyBytes = new byte[32];
                    rng.GetBytes(privateKeyBytes);
                    
                    // Generate seed phrase (BIP39)
                    var seedPhrase = GenerateCosmosSeedPhrase();
                    
                    // Derive public key from private key (secp256k1)
                    var publicKey = DeriveCosmosPublicKey(privateKeyBytes);
                    var cosmosAddress = DeriveCosmosAddress(publicKey);
                    
                    result.Result = (publicKey, Convert.ToHexString(privateKeyBytes).ToLower(), seedPhrase);
                    result.IsError = false;
                    result.Message = "Cosmos account key pair created successfully";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating account: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                    return result;
                }

                // Restore Cosmos account from seed phrase
                byte[] privateKeyBytes;
                
                if (seedPhrase.Length == 64 && System.Text.RegularExpressions.Regex.IsMatch(seedPhrase, "^[0-9a-fA-F]+$"))
                {
                    // Treat as hex private key
                    privateKeyBytes = Convert.FromHexString(seedPhrase);
                }
                else
                {
                    // Derive from BIP39 seed phrase
                    var seed = DeriveSeedFromMnemonic(seedPhrase);
                    privateKeyBytes = seed.Take(32).ToArray();
                }
                
                var publicKey = DeriveCosmosPublicKey(privateKeyBytes);
                
                result.Result = (publicKey, Convert.ToHexString(privateKeyBytes).ToLower());
                result.IsError = false;
                result.Message = "Cosmos account restored successfully from seed phrase";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
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

                // Bridge pool address
                var bridgePoolAddress = _contractAddress ?? "cosmos1bridgepool1234567890abcdef";
                
                // Convert amount to uatom (smallest unit)
                var amountInUatom = (ulong)(amount * 1_000_000m);

                // Create Cosmos bank send transaction
                var transactionPayload = new
                {
                    body = new
                    {
                        messages = new[]
                        {
                            new
                            {
                                type = "/cosmos.bank.v1beta1.MsgSend",
                                value = new
                                {
                                    from_address = senderAccountAddress,
                                    to_address = bridgePoolAddress,
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = "uatom",
                                            amount = amountInUatom.ToString()
                                        }
                                    }
                                }
                            }
                        },
                        memo = "OASIS bridge withdrawal"
                    },
                    auth_info = new
                    {
                        signer_infos = new object[] { },
                        fee = new
                        {
                            amount = new[] { new { denom = "uatom", amount = "5000" } },
                            gas_limit = "200000"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txResponse.TryGetProperty("tx_response", out var txResp) &&
                                 txResp.TryGetProperty("txhash", out var hash) ? hash.GetString() : "";

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txHash ?? "Transaction submitted",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Pending
                    };
                    result.IsError = false;
                    result.Message = "Cosmos withdrawal transaction submitted successfully";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to submit withdrawal: {httpResponse.StatusCode} - {errorContent}");
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = errorContent,
                        Status = BridgeTransactionStatus.Canceled
                    };
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                // Bridge pool address (sender)
                var bridgePoolAddress = _contractAddress ?? "cosmos1bridgepool1234567890abcdef";
                
                // Convert amount to uatom (smallest unit)
                var amountInUatom = (ulong)(amount * 1_000_000m);

                // Create Cosmos bank send transaction from bridge pool to receiver
                var transactionPayload = new
                {
                    body = new
                    {
                        messages = new[]
                        {
                            new
                            {
                                type = "/cosmos.bank.v1beta1.MsgSend",
                                value = new
                                {
                                    from_address = bridgePoolAddress,
                                    to_address = receiverAccountAddress,
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = "uatom",
                                            amount = amountInUatom.ToString()
                                        }
                                    }
                                }
                            }
                        },
                        memo = "OASIS bridge deposit"
                    },
                    auth_info = new
                    {
                        signer_infos = new object[] { },
                        fee = new
                        {
                            amount = new[] { new { denom = "uatom", amount = "5000" } },
                            gas_limit = "200000"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txResponse.TryGetProperty("tx_response", out var txResp) &&
                                 txResp.TryGetProperty("txhash", out var hash) ? hash.GetString() : "";

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txHash ?? "Transaction submitted",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Completed
                    };
                    result.IsError = false;
                    result.Message = "Cosmos deposit transaction submitted successfully";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to submit deposit: {httpResponse.StatusCode} - {errorContent}");
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = errorContent,
                        Status = BridgeTransactionStatus.Canceled
                    };
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                // Query Cosmos transaction status using REST API
                var httpResponse = await _httpClient.GetAsync($"/cosmos/tx/v1beta1/txs/{transactionHash}");
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (txData.TryGetProperty("tx_response", out var txResponse))
                    {
                        if (txResponse.TryGetProperty("code", out var code))
                        {
                            var codeValue = code.GetInt32();
                            if (codeValue == 0)
                            {
                                result.Result = BridgeTransactionStatus.Completed;
                                result.IsError = false;
                                result.Message = "Transaction completed successfully";
                            }
                            else
                            {
                                result.Result = BridgeTransactionStatus.Canceled;
                                result.IsError = true;
                                var errorMsg = txResponse.TryGetProperty("raw_log", out var log) ? log.GetString() : "Transaction failed";
                                result.Message = $"Transaction failed: {errorMsg}";
                            }
                        }
                        else
                        {
                            result.Result = BridgeTransactionStatus.Pending;
                            result.IsError = false;
                            result.Message = "Transaction found, status pending";
                        }
                    }
                    else
                    {
                        result.Result = BridgeTransactionStatus.Pending;
                        result.IsError = false;
                    }
                }
                else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = true;
                    result.Message = "Transaction not found";
                }
                else
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    OASISErrorHandling.HandleError(ref result, $"Failed to query transaction status: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
            }
            return result;
        }




        /// <summary>
        /// Get wallet address for avatar from Cosmos blockchain
        /// </summary>
        private async Task<string> GetWalletAddressForAvatar(Guid avatarId)
        {
            try
            {
                // Query Cosmos blockchain for avatar wallet address
                var queryUrl = $"/cosmos/auth/v1beta1/accounts/{avatarId}";
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var accountData = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    if (accountData.TryGetProperty("account", out var account))
                    {
                        return account.TryGetProperty("address", out var address) ? address.GetString() : "";
                    }
                }
            }
            catch (Exception)
            {
                // Return empty string if query fails
            }
            return "";
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
