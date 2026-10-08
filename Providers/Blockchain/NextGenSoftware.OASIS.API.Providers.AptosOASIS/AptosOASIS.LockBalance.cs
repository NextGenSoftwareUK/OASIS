using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NBitcoin;
using AptosEd25519Account = Aptos.Ed25519Account;
using AptosEd25519PrivateKey = Aptos.Ed25519PrivateKey;
using AptosEd25519PublicKey = Aptos.Ed25519PublicKey;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS
{
    public partial class AptosOASIS
    {
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
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.TokenAddress) || string.IsNullOrEmpty(request.FromWalletPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                    return result;
                }

                // Lock token by transferring to bridge pool address
                var bridgePoolAddress = _contractAddress ?? "0x1"; // Bridge pool address
                
                // Get sender address (would derive from private key in production)
                var senderAddress = bridgePoolAddress; // Simplified - would derive from private key

                // Get account sequence number
                var accountResponse = await _httpClient.GetAsync($"/v1/accounts/{senderAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                var sequenceNumber = accountData.TryGetProperty("sequence_number", out var seq) ? seq.GetString() : "0";

                // Transfer token to bridge pool (locking)
                var transactionPayload = new
                {
                    sender = senderAddress,
                    sequence_number = sequenceNumber,
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { request.TokenAddress },
                        arguments = new[] { bridgePoolAddress, "1" } // Lock amount (would come from request in production)
                    }
                };

                var jsonContent = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var hash = transactionResult.TryGetProperty("hash", out var hashProp) 
                        ? hashProp.GetString() 
                        : "unknown";

                    result.Result = new TransactionResponse { TransactionResult = hash };
                    result.IsError = false;
                    result.Message = "Token locked successfully on Aptos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Aptos API error: {httpResponse.StatusCode} - {errorContent}");
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
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Unlock token by transferring from bridge pool to recipient
                var bridgePoolAddress = _contractAddress ?? "0x1"; // Bridge pool address
                
                // Get recipient address (would get from UnlockedByAvatarId in production)
                var recipientAddress = bridgePoolAddress; // Simplified - would get from avatar

                // Get bridge pool account sequence number
                var accountResponse = await _httpClient.GetAsync($"/v1/accounts/{bridgePoolAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                var sequenceNumber = accountData.TryGetProperty("sequence_number", out var seq) ? seq.GetString() : "0";

                // Transfer token from bridge pool to recipient (unlocking)
                var transactionPayload = new
                {
                    sender = bridgePoolAddress,
                    sequence_number = sequenceNumber,
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { request.TokenAddress },
                        arguments = new[] { recipientAddress, "1" } // Unlock amount (would come from request in production)
                    }
                };

                var jsonContent = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var hash = transactionResult.TryGetProperty("hash", out var hashProp) 
                        ? hashProp.GetString() 
                        : "unknown";

                    result.Result = new TransactionResponse { TransactionResult = hash };
                    result.IsError = false;
                    result.Message = "Token unlocked successfully on Aptos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Aptos API error: {httpResponse.StatusCode} - {errorContent}");
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                result.Result = await GetNativeAptBalanceOctasAsync(request.WalletAddress) / 100_000_000d;
                result.Message = "Native APT balance retrieved through the Aptos SDK.";
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query Aptos transaction history
                var transactionsResponse = await _httpClient.GetAsync($"/v1/accounts/{request.WalletAddress}/transactions?limit=100");
                
                if (transactionsResponse.IsSuccessStatusCode)
                {
                    var transactionsContent = await transactionsResponse.Content.ReadAsStringAsync();
                    var transactionsData = JsonSerializer.Deserialize<JsonElement>(transactionsContent);
                    
                    var transactions = new List<IWalletTransaction>();
                    
                    if (transactionsData.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tx in transactionsData.EnumerateArray())
                        {
                            // Extract transaction hash as the transaction ID
                            var txHash = tx.TryGetProperty("hash", out var hashProp) ? hashProp.GetString() : 
                                        tx.TryGetProperty("version", out var versionProp) ? versionProp.GetString() : 
                                        CreateDeterministicGuid($"{ProviderType.Value}:tx:{tx.GetRawText()}").ToString();
                            
                            // Try to parse hash as GUID, otherwise use hash string directly
                            Guid txGuid;
                            if (!Guid.TryParse(txHash, out txGuid))
                            {
                                // Use hash of transaction hash string as GUID
                                var hashBytes = System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(txHash ?? ""));
                                txGuid = new Guid(hashBytes.Take(16).ToArray());
                            }
                            
                            var walletTx = new NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response.WalletTransaction
                            {
                                TransactionId = txGuid,
                                FromWalletAddress = tx.TryGetProperty("sender", out var sender) ? sender.GetString() : string.Empty,
                                ToWalletAddress = tx.TryGetProperty("payload", out var payload) && 
                                                 payload.TryGetProperty("arguments", out var args) && 
                                                 args.GetArrayLength() > 0 ? args[0].GetString() : string.Empty,
                                Amount = tx.TryGetProperty("payload", out var payload2) && 
                                        payload2.TryGetProperty("arguments", out var args2) && 
                                        args2.GetArrayLength() > 1 ? 
                                        (double.TryParse(args2[1].GetString(), out var amt) ? amt / 100000000 : 0) : 0, // Convert from octas
                                Description = tx.TryGetProperty("hash", out var hash) ? $"Aptos transaction: {hash.GetString()}" : "Aptos transaction"
                            };
                            transactions.Add(walletTx);
                        }
                    }
                    
                    result.Result = transactions;
                    result.IsError = false;
                    result.Message = $"Retrieved {transactions.Count} transactions";
                }
                else
                {
                    var errorContent = await transactionsResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Aptos API error: {transactionsResponse.StatusCode} - {errorContent}");
                }
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
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                var account = AptosEd25519Account.Generate();

                //result.Result = keyPair;
                result.Result = new KeyPairAndWallet
                {
                    PrivateKey = account.PrivateKey.ToAIP80String(),
                    PublicKey = account.PublicKey.ToString(),
                    WalletAddressLegacy = account.Address.ToString()
                };

                result.IsError = false;
                result.Message = "Aptos key pair generated successfully using the Aptos Labs .NET SDK.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Derives the Aptos Ed25519 public key with the Aptos Labs SDK.
        /// </summary>
        private string DeriveAptosPublicKey(byte[] privateKeyBytes)
        {
            using var key = new AptosEd25519PrivateKey(privateKeyBytes);
            return key.PublicKey().ToString();
        }

        /// <summary>
        /// Derives Aptos address from public key
        /// </summary>
        private string DeriveAptosAddress(string publicKey)
        {
            var key = new AptosEd25519PublicKey(publicKey);
            return key.AuthKey().DerivedAddress().ToString();
        }

        // Bridge methods
        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                result.Result = await GetNativeAptBalanceOctasAsync(accountAddress) / 100_000_000m;
                result.Message = "Native APT balance retrieved through the Aptos SDK.";
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
                var account = AptosEd25519Account.FromDerivationPath("m/44'/637'/0'/0'/0'", mnemonic.ToString());
                result.Result = (account.PublicKey.ToString(), account.PrivateKey.ToAIP80String(), mnemonic.ToString());
                result.IsError = false;
                result.Message = "Aptos account created using the Aptos Labs .NET SDK";
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                    return result;
                }

                var account = seedPhrase.Contains(' ')
                    ? AptosEd25519Account.FromDerivationPath("m/44'/637'/0'/0'/0'", seedPhrase)
                    : new AptosEd25519Account(new AptosEd25519PrivateKey(seedPhrase, strict: false));
                result.Result = (account.PublicKey.ToString(), account.PrivateKey.ToAIP80String());
                result.IsError = false;
                result.Message = "Aptos account restored successfully from seed phrase";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
            }
            return result;
        }

    }
}
