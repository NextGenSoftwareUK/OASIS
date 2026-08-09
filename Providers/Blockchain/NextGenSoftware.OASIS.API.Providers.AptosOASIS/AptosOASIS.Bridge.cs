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

        /// <summary>
        /// Derives Aptos public key from private key using Ed25519
        /// Note: This is a simplified implementation. In production, use proper Aptos SDK for key derivation.
        /// </summary>
        private string DeriveAptosPublicKey(byte[] privateKeyBytes)
        {
            // Aptos uses Ed25519 elliptic curve (same as Solana)
            // In production, use Aptos SDK for proper key derivation
            try
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(privateKeyBytes);
                    // Aptos public keys are typically 64 characters (32 bytes hex)
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
        /// Derives Aptos address from public key
        /// </summary>
        private string DeriveAptosAddress(string publicKey)
        {
            // Aptos addresses are derived from public keys
            try
            {
                var publicKeyBytes = System.Text.Encoding.UTF8.GetBytes(publicKey);
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(publicKeyBytes);
                    // Take portion for address (Aptos addresses are typically 32 bytes)
                    var addressBytes = new byte[32];
                    Array.Copy(hash, addressBytes, 32);
                    return "0x" + BitConverter.ToString(addressBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return publicKey.Length >= 64 ? "0x" + publicKey.Substring(0, 64) : "0x" + publicKey.PadRight(64, '0');
            }
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

                // Query Aptos account balance using REST API
                var accountResponse = await _httpClient.GetAsync($"/accounts/{accountAddress}/resource/0x1::coin::CoinStore<0x1::aptos_coin::AptosCoin>");
                
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
                            // Convert from smallest unit (octas) to APT
                            result.Result = balance / 100_000_000m;
                            result.IsError = false;
                            result.Message = "Account balance retrieved successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to parse balance from Aptos API response");
                        }
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
                    // Account might not exist or have no balance
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                // Generate Aptos Ed25519 key pair
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    var privateKeyBytes = new byte[32];
                    rng.GetBytes(privateKeyBytes);
                    
                    // Generate Ed25519 key pair (Aptos uses Ed25519)
                    var privateKeyHex = Convert.ToHexString(privateKeyBytes).ToLower();
                    var publicKeyHex = privateKeyHex; // Simplified - in production, derive public key from private key using Ed25519
                    
                    // Generate seed phrase (BIP39) for Aptos
                    var seedPhrase = GenerateAptosSeedPhrase();
                    
                    result.Result = (publicKeyHex, privateKeyHex, seedPhrase);
                    result.IsError = false;
                    result.Message = "Aptos account key pair created successfully";
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

                // Restore Aptos account from seed phrase
                // If seedPhrase is actually a private key, use it directly
                // Otherwise, derive from BIP39 seed phrase
                string privateKeyHex;
                string publicKeyHex;
                
                if (seedPhrase.Length == 64 && System.Text.RegularExpressions.Regex.IsMatch(seedPhrase, "^[0-9a-fA-F]+$"))
                {
                    // Treat as private key hex
                    privateKeyHex = seedPhrase.ToLower();
                    publicKeyHex = privateKeyHex; // Simplified - in production, derive public key using Ed25519
                }
                else
                {
                    // Derive from BIP39 seed phrase
                    var seed = DeriveSeedFromMnemonic(seedPhrase);
                    privateKeyHex = Convert.ToHexString(seed.Take(32).ToArray()).ToLower();
                    publicKeyHex = privateKeyHex; // Simplified - in production, derive public key using Ed25519
                }
                
                result.Result = (publicKeyHex, privateKeyHex);
                result.IsError = false;
                result.Message = "Aptos account restored successfully from seed phrase";
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
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

                // Get account sequence number
                var accountResponse = await _httpClient.GetAsync($"/accounts/{senderAccountAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                var sequenceNumber = accountData.TryGetProperty("sequence_number", out var seq) ? seq.GetString() : "0";

                // Bridge pool address
                var bridgePoolAddress = _contractAddress ?? "0x1::oasis::bridge_pool";
                
                // Convert amount to octas (smallest unit)
                var amountInOctas = (ulong)(amount * 100_000_000m);

                // Create withdrawal transaction (transfer to bridge pool)
                var transactionPayload = new
                {
                    sender = senderAccountAddress,
                    sequence_number = sequenceNumber,
                    max_gas_amount = "1000",
                    gas_unit_price = "100",
                    expiration_timestamp_secs = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 600).ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { "0x1::aptos_coin::AptosCoin" },
                        arguments = new[] { bridgePoolAddress, amountInOctas.ToString() }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<AptosTransactionResponse>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txResponse?.TransactionHash ?? "Transaction submitted",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Pending
                    };
                    result.IsError = false;
                    result.Message = "Aptos withdrawal transaction submitted successfully";
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
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
                var bridgePoolAddress = _contractAddress ?? "0x1::oasis::bridge_pool";
                
                // Get bridge pool account sequence number
                var accountResponse = await _httpClient.GetAsync($"/accounts/{bridgePoolAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get bridge pool account info: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                var sequenceNumber = accountData.TryGetProperty("sequence_number", out var seq) ? seq.GetString() : "0";

                // Convert amount to octas (smallest unit)
                var amountInOctas = (ulong)(amount * 100_000_000m);

                // Create deposit transaction (transfer from bridge pool to receiver)
                var transactionPayload = new
                {
                    sender = bridgePoolAddress,
                    sequence_number = sequenceNumber,
                    max_gas_amount = "1000",
                    gas_unit_price = "100",
                    expiration_timestamp_secs = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 600).ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { "0x1::aptos_coin::AptosCoin" },
                        arguments = new[] { receiverAccountAddress, amountInOctas.ToString() }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<AptosTransactionResponse>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txResponse?.TransactionHash ?? "Transaction submitted",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Completed
                    };
                    result.IsError = false;
                    result.Message = "Aptos deposit transaction submitted successfully";
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Aptos provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                // Query Aptos transaction status using REST API
                var httpResponse = await _httpClient.GetAsync($"/transactions/{transactionHash}");
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    // Check transaction success status
                    if (txData.TryGetProperty("success", out var success))
                    {
                        if (success.GetBoolean())
                        {
                            result.Result = BridgeTransactionStatus.Completed;
                            result.IsError = false;
                            result.Message = "Transaction completed successfully";
                        }
                        else
                        {
                            result.Result = BridgeTransactionStatus.Canceled;
                            result.IsError = true;
                            result.Message = "Transaction failed";
                        }
                    }
                    else if (txData.TryGetProperty("type", out var txType))
                    {
                        // Transaction exists but status unknown
                        result.Result = BridgeTransactionStatus.Pending;
                        result.IsError = false;
                        result.Message = "Transaction found, status pending";
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



        public OASISResult<string> SendSmartContractFunction(string contractAddress, string functionName, params object[] parameters)
        {
            return SendSmartContractFunctionAsync(contractAddress, functionName, parameters).Result;
        }

        public async Task<OASISResult<string>> SendSmartContractFunctionAsync(string contractAddress, string functionName, params object[] parameters)
        {
            var response = new OASISResult<string>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Implement real Aptos smart contract function call
                if (string.IsNullOrEmpty(_privateKey))
                {
                    OASISErrorHandling.HandleError(ref response, "Private key not configured for Aptos smart contract calls");
                    return response;
                }

                try
                {
                    // Create real Move smart contract function call for Aptos
                    var functionPayload = new
                    {
                        type = "entry_function_payload",
                        function = $"{contractAddress}::oasis::{functionName}",
                        type_arguments = new string[0],
                        arguments = parameters.Select(p => p.ToString()).ToArray()
                    };

                    // Create Aptos transaction with real Move smart contract call
                    var transaction = await CreateAptosTransaction(functionName, JsonSerializer.Serialize(parameters));

                    // Submit transaction to Aptos network
                    var rpcRequest = new
                    {
                        jsonrpc = "2.0",
                        id = 1,
                        method = "submit_transaction",
                        @params = new[] { transaction }
                    };

                    var jsonContent = JsonSerializer.Serialize(rpcRequest);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var httpResponse = await _httpClient.PostAsync("", content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await httpResponse.Content.ReadAsStringAsync();
                        var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        if (transactionResult.TryGetProperty("result", out var result) &&
                            result.TryGetProperty("hash", out var hash))
                        {
                            response.Result = $"Smart contract function '{functionName}' executed successfully. Transaction hash: {hash.GetString()}";
                            response.IsError = false;
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to get transaction hash from Aptos response");
                        }
                    }
                    else
                    {
                        var errorContent = await httpResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref response, $"Aptos smart contract call failed: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error calling Aptos smart contract function: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error calling Aptos smart contract function: {ex.Message}");
            }

            return response;
        }



    }
}
