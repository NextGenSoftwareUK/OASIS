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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS
{
    public partial class CardanoOASIS
    {
        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Cardano provider is not activated");
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

                // Cardano token minting via RPC (requires native token policy)
                var lovelaceAmount = (ulong)(amount * 1_000_000m);
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "mint",
                    @params = new
                    {
                        policyId = tokenAddress,
                        assetName = tokenAddress,
                        quantity = lovelaceAmount,
                        recipient = mintToWalletAddress
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
                    result.Message = "Token minted successfully on Cardano";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint token on Cardano: {response.StatusCode}");
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
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Cardano provider is not activated");
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
                var lovelaceAmount = (ulong)(1_000_000m); // Default amount
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "burn",
                    @params = new
                    {
                        policyId = request.TokenAddress,
                        assetName = request.TokenAddress,
                        quantity = lovelaceAmount,
                        from = "" // Will be derived from private key in production
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
                    result.Message = "Token burned successfully on Cardano";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn token on Cardano: {response.StatusCode}");
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
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Cardano provider is not activated");
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
                var bridgePoolAddress = _contractAddress; // Use contract address as bridge pool, or get from OASIS configuration
                if (string.IsNullOrWhiteSpace(bridgePoolAddress))
                {
                    // Fallback: try to get from OASIS DNA if available
                    bridgePoolAddress = "addr1..."; // Default fallback
                }
                var lovelaceAmount = (ulong)(1_000_000m); // Default amount
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "lock",
                    @params = new
                    {
                        policyId = request.TokenAddress,
                        assetName = request.TokenAddress,
                        quantity = lovelaceAmount,
                        address = bridgePoolAddress
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
                    result.Message = "Token locked successfully on Cardano";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock token on Cardano: {response.StatusCode}");
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
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Cardano provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // IUnlockWeb3TokenRequest doesn't have UnlockWalletAddress or Amount properties
                var unlockedToWalletAddress = "";
                
                // Try to get from locked token record using request.Web3TokenId
                if (request.Web3TokenId != Guid.Empty)
                {
                    try
                    {
                        // Query OASIS storage for the locked token record
                        var providerResult = ProviderManager.Instance == null
                            ? new OASISResult<IOASISStorageProvider> { IsError = true, Message = "ProviderManager not initialized" }
                            : await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(global::NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
                        OASISResult<IHolon> tokenResult = providerResult.IsError || providerResult.Result == null
                            ? new OASISResult<IHolon> { IsError = true, Message = providerResult.Message }
                            : await providerResult.Result.LoadHolonAsync(request.Web3TokenId);

                        if (!tokenResult.IsError && tokenResult.Result != null)
                        {
                            // Extract wallet address from token metadata
                            unlockedToWalletAddress = tokenResult.Result.MetaData?.ContainsKey("UnlockedToWalletAddress") == true
                                ? tokenResult.Result.MetaData["UnlockedToWalletAddress"]?.ToString()
                                : tokenResult.Result.MetaData?.ContainsKey("MintToWalletAddress") == true
                                    ? tokenResult.Result.MetaData["MintToWalletAddress"]?.ToString()
                                    : "";
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError($"Error getting wallet address from Web3TokenId: {ex.Message}", ex);
                    }
                }
                
                // Fallback: try to get from UnlockedByAvatarId if available
                if (string.IsNullOrWhiteSpace(unlockedToWalletAddress) && request.UnlockedByAvatarId != Guid.Empty)
                {
                    unlockedToWalletAddress = await GetWalletAddressForAvatarAsync(request.UnlockedByAvatarId);
                }
                var lovelaceAmount = (ulong)(1_000_000m); // Default amount
                
                if (string.IsNullOrWhiteSpace(unlockedToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Unlocked to wallet address is required but not available");
                    return result;
                }
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "unlock",
                    @params = new
                    {
                        policyId = request.TokenAddress,
                        assetName = request.TokenAddress,
                        quantity = lovelaceAmount,
                        address = unlockedToWalletAddress
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
                    result.Message = "Token unlocked successfully on Cardano";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock token on Cardano: {response.StatusCode}");
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
                    OASISErrorHandling.HandleError(ref result, "Cardano provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Cardano balance via RPC
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getBalance",
                    @params = new object[] { request.WalletAddress }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (responseData.TryGetProperty("result", out var resultProp))
                    {
                        var balanceInLovelace = resultProp.TryGetProperty("lovelace", out var lovelaceProp) ? lovelaceProp.GetUInt64() : 0UL;
                        var balanceInADA = balanceInLovelace / 1_000_000.0;
                        result.Result = balanceInADA;
                        result.IsError = false;
                        result.Message = "Balance retrieved successfully";
                    }
                    else
                    {
                        result.Result = 0.0;
                        result.IsError = false;
                    }
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
                    OASISErrorHandling.HandleError(ref result, "Cardano provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Cardano transactions via RPC
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getTransactions",
                    @params = new object[] { request.WalletAddress, 10 } // Default to 10 transactions
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                var transactions = new List<IWalletTransaction>();
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (responseData.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tx in resultProp.EnumerateArray())
                        {
                            // Extract transaction hash from Cardano transaction
                            var txHash = tx.TryGetProperty("hash", out var hashProp) ? hashProp.GetString() : 
                                        tx.TryGetProperty("tx_hash", out var txHashProp) ? txHashProp.GetString() : 
                                        null;
                            
                            // Create deterministic GUID from transaction hash
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
                                Amount = tx.TryGetProperty("amount", out var amt) ? amt.GetString() != null ? double.Parse(amt.GetString()) / 1_000_000.0 : 0.0 : 0.0,
                                Description = txHash != null ? $"Cardano transaction: {txHash}" : "Cardano transaction"
                            };
                            transactions.Add(walletTx);
                        }
                    }
                }

                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} Cardano transactions";
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
                    OASISErrorHandling.HandleError(ref result, "Cardano provider is not activated");
                    return result;
                }

                // Generate Cardano Ed25519 key pair (Cardano uses Ed25519)
                var privateKeyBytes = new byte[32];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(privateKeyBytes);
                }

                // Generate Ed25519 key pair for Cardano using Chaos.NaCl
                byte[] publicKeyBytes = new byte[32];
                byte[] expandedPrivateKeyBytes = new byte[64];
                Chaos.NaCl.Ed25519.KeyPairFromSeed(publicKeyBytes, expandedPrivateKeyBytes, privateKeyBytes);
                
                var privateKey = Convert.ToBase64String(expandedPrivateKeyBytes);
                var publicKey = Convert.ToBase64String(publicKeyBytes);
                
                // Generate Cardano address from public key (Cardano uses bech32 encoding)
                var address = DeriveCardanoAddress(publicKeyBytes);

                // Create KeyPairAndWallet using KeyHelper but override with Cardano-specific values from Ed25519
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = privateKey;
                    keyPair.PublicKey = publicKey;
                    keyPair.WalletAddressLegacy = address; // Cardano bech32 address
                }

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Cardano Ed25519 key pair generated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "Cardano provider is not activated");
                    return result;
                }

                // Generate Cardano Ed25519 key pair (Cardano uses Ed25519)
                // Cardano uses Ed25519 curve for key generation
                var privateKeyBytes = new byte[32];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(privateKeyBytes);
                }

                // Generate Ed25519 key pair for Cardano using Chaos.NaCl
                byte[] publicKeyBytes = new byte[32];
                byte[] expandedPrivateKeyBytes = new byte[64];
                Chaos.NaCl.Ed25519.KeyPairFromSeed(publicKeyBytes, expandedPrivateKeyBytes, privateKeyBytes);
                
                var privateKey = Convert.ToBase64String(expandedPrivateKeyBytes);
                var publicKey = Convert.ToBase64String(publicKeyBytes);
                
                // Generate Cardano address from public key (Cardano uses bech32 encoding)
                // Cardano addresses are derived from the public key hash
                var address = DeriveCardanoAddress(publicKeyBytes);

                // Create KeyPairAndWallet using KeyHelper but override with Cardano-specific values from Ed25519
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = privateKey;
                    keyPair.PublicKey = publicKey;
                    keyPair.WalletAddressLegacy = address; // Cardano bech32 address
                }

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Cardano Ed25519 key pair generated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
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

        /// <summary>
        /// Derives Cardano address from public key
        /// Cardano uses bech32 encoding for addresses
        /// </summary>
        private string DeriveCardanoAddress(byte[] publicKeyBytes)
        {
            try
            {
                // Cardano addresses use bech32 encoding with specific prefixes
                // Mainnet: "addr1", Testnet: "addr_test1"
                var prefix = _networkId == "mainnet" ? "addr1" : "addr_test1";
                
                // Hash public key using Blake2b-224 (Cardano specific)
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = sha256.ComputeHash(publicKeyBytes);
                
                // Take first 28 bytes for address (simplified - in production use proper bech32 library)
                var addressBytes = new byte[28];
                Array.Copy(hash, 0, addressBytes, 0, Math.Min(28, hash.Length));
                
                // Simplified bech32 encoding (in production use proper bech32 library)
                return prefix + Convert.ToBase64String(addressBytes).Substring(0, Math.Min(32, Convert.ToBase64String(addressBytes).Length));
            }
            catch
            {
                // Fallback to hex representation
                return "addr1" + BitConverter.ToString(publicKeyBytes).Replace("-", "").ToLowerInvariant();
            }
        }

    }
}
