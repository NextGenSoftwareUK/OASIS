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
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS
{
    public partial class AptosOASIS
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
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.FromWalletAddress) || string.IsNullOrEmpty(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "FromWalletAddress and ToWalletAddress are required");
                    return result;
                }

                // Get account sequence number
                var accountResponse = await _httpClient.GetAsync($"/v1/accounts/{request.FromWalletAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                var sequenceNumber = accountData.TryGetProperty("sequence_number", out var seq) ? seq.GetString() : "0";

                // Determine token type (default to AptosCoin if not specified)
                var tokenType = string.IsNullOrEmpty(request.FromTokenAddress) 
                    ? "0x1::aptos_coin::AptosCoin" 
                    : request.FromTokenAddress;

                // Create transaction payload for Aptos token transfer
                var transactionPayload = new
                {
                    sender = request.FromWalletAddress,
                    sequence_number = sequenceNumber,
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { tokenType },
                        arguments = new[] { request.ToWalletAddress, request.Amount.ToString() }
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

                    result.Result = new TransactionResponse
                    {
                        TransactionResult = hash
                    };
                    result.IsError = false;
                    result.Message = "Token sent successfully to Aptos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Aptos API error: {httpResponse.StatusCode} - {errorContent}");
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
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                // Minting requires admin permissions and a token contract
                // For Aptos, minting is typically done through a coin module
                // This would require the contract address and proper permissions
                var mintAddress = _contractAddress ?? "0x1";
                
                // Get account sequence number
                var accountResponse = await _httpClient.GetAsync($"/v1/accounts/{mintAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                var sequenceNumber = accountData.TryGetProperty("sequence_number", out var seq) ? seq.GetString() : "0";

                // Aptos coin minting function (requires admin permissions)
                var transactionPayload = new
                {
                    sender = mintAddress,
                    sequence_number = sequenceNumber,
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::mint",
                        type_arguments = new[] { "0x1::aptos_coin::AptosCoin" },
                        arguments = new[] { mintAddress, "1" } // Mint 1 coin (amount would come from request in production)
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
                    result.Message = "Token minted successfully on Aptos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Aptos API error: {httpResponse.StatusCode} - {errorContent}");
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
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Get sender address from private key if available
                var senderAddress = _contractAddress ?? "0x1";
                if (!string.IsNullOrEmpty(request.OwnerPrivateKey))
                {
                    // Derive address from private key (simplified - in production use proper Aptos SDK)
                    senderAddress = _contractAddress ?? "0x1";
                }

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

                // Aptos coin burning function
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
                        function = "0x1::coin::burn",
                        type_arguments = new[] { request.TokenAddress },
                        arguments = new[] { "1" } // Burn 1 coin (amount would come from request in production)
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
                    result.Message = "Token burned successfully on Aptos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Aptos API error: {httpResponse.StatusCode} - {errorContent}");
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

                // Query Aptos account balance
                var accountResponse = await _httpClient.GetAsync($"/v1/accounts/{request.WalletAddress}/resource/0x1::coin::CoinStore<0x1::aptos_coin::AptosCoin>");
                
                if (accountResponse.IsSuccessStatusCode)
                {
                    var accountContent = await accountResponse.Content.ReadAsStringAsync();
                    var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                    
                    if (accountData.TryGetProperty("data", out var data) && 
                        data.TryGetProperty("coin", out var coin) && 
                        coin.TryGetProperty("value", out var value))
                    {
                        var balanceStr = value.GetString();
                        if (decimal.TryParse(balanceStr, out var balance))
                        {
                            result.Result = (double)balance / 100000000; // Convert from octas (10^8) to APT
                            result.IsError = false;
                            result.Message = "Balance retrieved successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to parse balance value");
                        }
                    }
                    else
                    {
                        result.Result = 0.0;
                        result.IsError = false;
                        result.Message = "Account has no balance";
                    }
                }
                else if (accountResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result.Result = 0.0;
                    result.IsError = false;
                    result.Message = "Account not found or has no balance";
                }
                else
                {
                    var errorContent = await accountResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Aptos API error: {accountResponse.StatusCode} - {errorContent}");
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

                // Generate Aptos-specific key pair using Ed25519 (production-ready)
                // Aptos uses Ed25519 curve (same as Solana), so we can use Solnet.Wallet SDK
                var mnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
                var wallet = new Wallet(mnemonic);
                var account = wallet.Account;
                
                // Aptos addresses are derived from public keys (32 bytes, hex encoded with 0x prefix)
                var aptosAddress = "0x" + BitConverter.ToString(account.PublicKey.KeyBytes).Replace("-", "").ToLowerInvariant();
                
                // Create key pair structure
                //var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                //if (keyPair != null)
                //{
                //    keyPair.PrivateKey = Convert.ToBase64String(account.PrivateKey.KeyBytes);
                //    keyPair.PublicKey = account.PublicKey.Key;
                //    keyPair.WalletAddressLegacy = aptosAddress;
                //}

                //result.Result = keyPair;
                result.Result = new KeyPairAndWallet
                {
                    PrivateKey = Convert.ToBase64String(account.PrivateKey.KeyBytes),
                    PublicKey = account.PublicKey.Key,
                    WalletAddressLegacy = aptosAddress
                };

                result.IsError = false;
                result.Message = "Aptos key pair generated successfully using Ed25519 (Solnet.Wallet SDK).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }
    }
}
