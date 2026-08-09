using System;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS
{
    public partial class NEAROASIS
    {

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "NEAR provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.FromTokenAddress) || 
                    string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and to wallet address are required");
                    return result;
                }

                // Get private key from request
                string privateKey = request.OwnerPrivateKey;
                if (string.IsNullOrWhiteSpace(privateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Private key is required (OwnerPrivateKey)");
                    return result;
                }

                // Convert amount to yoctoNEAR (1 NEAR = 10^24 yoctoNEAR)
                var amountInYoctoNEAR = (ulong)(request.Amount * 1_000_000_000_000_000_000_000_000m);

                // Create NEAR FT (Fungible Token) transfer transaction
                var transferArgs = JsonSerializer.Serialize(new
                {
                    receiver_id = request.ToWalletAddress,
                    amount = amountInYoctoNEAR.ToString()
                });

                var signedTx = await CreateSignedTransaction(request.FromTokenAddress, "ft_transfer", transferArgs);
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new { signed_tx = signedTx }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var rpcResult))
                    {
                        var transactionHash = rpcResult.TryGetProperty("transaction", out var tx) &&
                                             tx.TryGetProperty("hash", out var hash) ? hash.GetString() : "";
                        
                        result.Result.TransactionResult = transactionHash;
                        result.IsError = false;
                        result.Message = "Token sent successfully on NEAR blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to send token on NEAR blockchain");
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"NEAR API error: {httpResponse.StatusCode} - {errorContent}");
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
                    OASISErrorHandling.HandleError(ref result, "NEAR provider is not activated");
                    return result;
                }

                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Request is required");
                    return result;
                }

                // IMintWeb3TokenRequest inherits from IMintTokenRequestBase which has MetaData
                // Get token address and recipient from MetaData
                var tokenAddress = request.MetaData?.ContainsKey("TokenAddress") == true 
                    ? request.MetaData["TokenAddress"]?.ToString() 
                    : "ft.oasis.near";
                var mintToAddress = request.MetaData?.ContainsKey("MintToWalletAddress") == true 
                    ? request.MetaData["MintToWalletAddress"]?.ToString() 
                    : "";
                
                if (string.IsNullOrWhiteSpace(tokenAddress) || string.IsNullOrWhiteSpace(mintToAddress))
                {
                    // Try to get from avatar if not provided
                    if (request.MintedByAvatarId != Guid.Empty)
                    {
                        mintToAddress = await GetWalletAddressForAvatarAsync(request.MintedByAvatarId);
                    }
                    
                    if (string.IsNullOrWhiteSpace(tokenAddress) || string.IsNullOrWhiteSpace(mintToAddress))
                    {
                        OASISErrorHandling.HandleError(ref result, "Token address and mint to wallet address are required");
                        return result;
                    }
                }

                // Get mint amount from MetaData or use default
                var mintAmount = request.MetaData?.ContainsKey("Amount") == true && 
                    decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amt) 
                    ? amt : 1m;

                // Convert amount to yoctoNEAR (NEAR's smallest unit)
                var amountInYoctoNEAR = (ulong)(mintAmount * 1_000_000_000_000_000_000_000_000m);

                // Create NEAR FT mint transaction using real NEAR RPC API
                var mintArgs = JsonSerializer.Serialize(new
                {
                    account_id = mintToAddress,
                    amount = amountInYoctoNEAR.ToString()
                });

                var signedTx = await CreateSignedTransaction(tokenAddress, "ft_mint", mintArgs);
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new { signed_tx = signedTx }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var rpcResult))
                    {
                        var transactionHash = rpcResult.TryGetProperty("transaction", out var tx) &&
                                             tx.TryGetProperty("hash", out var hash) ? hash.GetString() : "";
                        
                        result.Result.TransactionResult = transactionHash;
                        result.IsError = false;
                        result.Message = "Token minted successfully on NEAR blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to mint token on NEAR blockchain");
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"NEAR API error: {httpResponse.StatusCode} - {errorContent}");
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
                    OASISErrorHandling.HandleError(ref result, "NEAR provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and owner private key are required");
                    return result;
                }

                // IBurnWeb3TokenRequest doesn't have Amount property
                // Use default burn amount of 1
                var burnAmount = 1m;
                var amountInYoctoNEAR = (ulong)(burnAmount * 1_000_000_000_000_000_000_000_000m);

                // Create NEAR FT burn transaction
                var burnArgs = JsonSerializer.Serialize(new
                {
                    amount = amountInYoctoNEAR.ToString()
                });

                var signedTx = await CreateSignedTransaction(request.TokenAddress, "ft_burn", burnArgs);
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new { signed_tx = signedTx }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var rpcResult))
                    {
                        var transactionHash = rpcResult.TryGetProperty("transaction", out var tx) &&
                                             tx.TryGetProperty("hash", out var hash) ? hash.GetString() : "";
                        
                        result.Result.TransactionResult = transactionHash;
                        result.IsError = false;
                        result.Message = "Token burned successfully on NEAR blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to burn token on NEAR blockchain");
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"NEAR API error: {httpResponse.StatusCode} - {errorContent}");
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
                    OASISErrorHandling.HandleError(ref result, "NEAR provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                    return result;
                }

                // Lock token by transferring to bridge pool (one NFT – no amount)
                var bridgePoolAddress = _contractAddress ?? "bridge.oasispool.near";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromTokenAddress = request.TokenAddress,
                    OwnerPrivateKey = request.FromWalletPrivateKey,
                    ToWalletAddress = bridgePoolAddress,
                    Amount = 1m
                };

                return await SendTokenAsync(sendRequest);
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
                    OASISErrorHandling.HandleError(ref result, "NEAR provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Get recipient address from avatar ID
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.NEAROASIS, request.UnlockedByAvatarId);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }

                // Unlock token by transferring from bridge pool to recipient
                var bridgePoolAddress = _contractAddress ?? "bridge.oasispool.near";
                var bridgePoolPrivateKey = _privateKey ?? string.Empty;

                if (string.IsNullOrWhiteSpace(bridgePoolPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Bridge pool private key is not configured");
                    return result;
                }

                // IUnlockWeb3TokenRequest doesn't have Amount property - use default
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromTokenAddress = request.TokenAddress,
                    OwnerPrivateKey = bridgePoolPrivateKey,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = 1m // Default unlock amount
                };

                return await SendTokenAsync(sendRequest);
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
                    OASISErrorHandling.HandleError(ref result, "NEAR provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query NEAR RPC for account balance
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "view_account",
                        finality = "final",
                        account_id = request.WalletAddress
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var rpcResult) &&
                        rpcResult.TryGetProperty("amount", out var amount))
                    {
                        var amountStr = amount.GetString();
                        if (ulong.TryParse(amountStr, out var amountInYoctoNEAR))
                        {
                            // Convert from yoctoNEAR to NEAR (1 NEAR = 10^24 yoctoNEAR)
                            result.Result = (double)(amountInYoctoNEAR / 1_000_000_000_000_000_000_000_000m);
                            result.IsError = false;
                            result.Message = "Balance retrieved successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to parse balance");
                        }
                    }
                    else
                    {
                        result.Result = 0.0;
                        result.IsError = false;
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"NEAR API error: {httpResponse.StatusCode} - {errorContent}");
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
                    OASISErrorHandling.HandleError(ref result, "NEAR provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query NEAR RPC for account transactions
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "EXPERIMENTAL_tx_status",
                    @params = new object[] { request.WalletAddress, null }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                var transactions = new List<IWalletTransaction>();

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var rpcResult) &&
                        rpcResult.TryGetProperty("receipts_outcome", out var receipts))
                    {
                        foreach (var receipt in receipts.EnumerateArray())
                        {
                            if (receipt.TryGetProperty("outcome", out var outcome) &&
                                outcome.TryGetProperty("status", out var status))
                            {
                                // Create deterministic GUID from receipt ID
                                var receiptId = receipt.TryGetProperty("id", out var id) ? id.GetString() : null;
                                Guid txGuid;
                                if (!string.IsNullOrWhiteSpace(receiptId))
                                {
                                    using var sha256 = System.Security.Cryptography.SHA256.Create();
                                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(receiptId));
                                    txGuid = new Guid(hashBytes.Take(16).ToArray());
                                }
                                else
                                {
                                    // Fallback: use deterministic GUID from transaction data
                                    var txData = $"{request.WalletAddress}:{receipt.GetRawText()}";
                                    txGuid = CreateDeterministicGuid($"{ProviderType.Value}:tx:{txData}");
                                }
                                
                                var transaction = new WalletTransaction
                                {
                                    TransactionId = txGuid,
                                    FromWalletAddress = request.WalletAddress,
                                    ToWalletAddress = outcome.TryGetProperty("executor_id", out var executor) ? executor.GetString() : "",
                                    Amount = 0.0,
                                    Description = receiptId ?? "",
                                    TransactionType = TransactionType.Credit,
                                    TransactionCategory = TransactionCategory.Other
                                };
                                transactions.Add(transaction);
                            }
                        }
                    }
                }

                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} transactions";
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
            // Call the overloaded version with null request
            return await GenerateKeyPairAsync(null);
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
                    OASISErrorHandling.HandleError(ref result, "NEAR provider is not activated");
                    return result;
                }

                // Generate NEAR Ed25519 key pair using built-in method
                var nearKeyPair = await GenerateNEARKeyPairAsync();

                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = nearKeyPair.PrivateKey;
                    keyPair.PublicKey = nearKeyPair.PublicKey;
                    keyPair.WalletAddressLegacy = nearKeyPair.PublicKey; // NEAR uses public key as account ID
                }

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "NEAR key pair generated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        private async Task<string> GetWalletAddressForAvatarAsync(Guid avatarId)
        {
            var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.NEAROASIS, avatarId);
            return walletResult.IsError ? string.Empty : walletResult.Result;
        }


    }
}
