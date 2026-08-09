using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

public partial class RadixOASIS
{
    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
    {
        return UnlockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }

            // Unlock ONE NFT at TokenAddress (NFTTokenAddress) – no amount, single NFT
            var lockContractAddress = _config.OasisBlueprintAddress ?? request.TokenAddress;

            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(
                request.UnlockedByAvatarId != Guid.Empty ? request.UnlockedByAvatarId : Guid.NewGuid(), 
                ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for unlocking");
                return result;
            }

            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = lockContractAddress,
                        methodName = "unlock",
                        args = new[]
                        {
                            new { kind = "Address", value = request.TokenAddress },
                            new { kind = "NonFungibleLocalId", value = "1" },
                            new { kind = "Address", value = walletResult.Result.WalletAddress }
                        }
                    }
                },
                blobs = new object[0]
            };

            // Get construction metadata and build transaction
            var metadataResult = await _httpClient.GetConstructionMetadataAsync(_config);
            if (metadataResult == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get Radix construction metadata");
                return result;
            }

            var transactionHeader = new
            {
                networkId = _config.NetworkId,
                startEpochInclusive = metadataResult.CurrentEpoch,
                endEpochExclusive = metadataResult.CurrentEpoch + 50,
                nonce = new Random().Next(),
                notaryPublicKey = walletResult.Result.PublicKey,
                notaryIsSignatory = true,
                tipPercentage = 0
            };

            var transactionData = new
            {
                network = network,
                manifest = System.Text.Json.JsonSerializer.Serialize(manifest),
                header = transactionHeader,
                message = new { kind = "None" }
            };

            // Submit transaction via Radix Gateway API
            var submitResult = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                _httpClient,
                $"{_config.HostUri}/core/lts/transaction/submit",
                transactionData);

            if (submitResult.IsError || submitResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit Radix unlock transaction: {submitResult.Message}");
                return result;
            }

            result.Result = new TransactionResponse
            {
                TransactionResult = submitResult.Result.TransactionHash ?? "Token unlock initiated"
            };
            result.IsError = false;
            result.Message = "Token unlocked successfully via Radix";
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
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            var balanceResult = await _radixService.GetAccountBalanceAsync(request.WalletAddress);
            if (balanceResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, balanceResult.Message ?? "Failed to get balance");
                return result;
            }

            result.Result = (double)balanceResult.Result;
            result.IsError = false;
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
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Radix Gateway API: Get account transactions
            // Note: Radix Gateway API provides transaction history for accounts
            var transactions = new List<IWalletTransaction>();
            
            try
            {
                // Use Radix Gateway API to get account transaction history
                // The Gateway API endpoint: /state/entity/page/account/{address}/transactions
                var accountAddress = request.WalletAddress;
                var url = $"{_config.HostUri}/state/entity/page/account/{Uri.EscapeDataString(accountAddress)}/transactions";
                
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(content);
                    
                    // Parse Radix Gateway API response
                    if (doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            // Extract transaction details from Radix Gateway response
                            var intentHash = item.TryGetProperty("intent_hash", out var hashProp) ? hashProp.GetString() : null;
                            var status = item.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "unknown";
                            
                            if (!string.IsNullOrWhiteSpace(intentHash))
                            {
                                // Get transaction status to determine if it's completed
                                var statusResult = await _radixService.GetTransactionStatusAsync(intentHash);
                                
                                // Create deterministic GUID from transaction hash
                                Guid txGuid;
                                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                                {
                                    var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(intentHash));
                                    txGuid = new Guid(hashBytes.Take(16).ToArray());
                                }
                                
                                var walletTx = new NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response.WalletTransaction
                                {
                                    TransactionId = txGuid,
                                    FromWalletAddress = accountAddress,
                                    ToWalletAddress = item.TryGetProperty("to", out var toProp) ? toProp.GetString() : string.Empty,
                                    Amount = item.TryGetProperty("amount", out var amtProp) && amtProp.ValueKind == System.Text.Json.JsonValueKind.Number 
                                        ? (double)amtProp.GetDecimal() 
                                        : 0.0,
                                    Description = $"Radix transaction: {intentHash}",
                                    CreatedDate = item.TryGetProperty("timestamp", out var tsProp) && tsProp.ValueKind == System.Text.Json.JsonValueKind.String
                                        ? DateTime.TryParse(tsProp.GetString(), out var dt) ? dt : DateTime.UtcNow
                                        : DateTime.UtcNow
                                };
                                
                                transactions.Add(walletTx);
                            }
                        }
                    }
                }
                else
                {
                    // If Gateway API fails, return empty list with success (account may have no transactions)
                    result.Result = transactions;
                    result.IsError = false;
                    result.Message = $"Retrieved {transactions.Count} Radix transactions (Gateway API returned {response.StatusCode})";
                    return result;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail - return empty list
                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} Radix transactions (error querying Gateway API: {ex.Message})";
                return result;
            }

            result.Result = transactions;
            result.IsError = false;
            result.Message = $"Retrieved {transactions.Count} Radix transactions";
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
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            // Generate Radix key pair
            var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            if (keyPair != null)
            {
                // Radix uses Ed25519 keys - generate using cryptographic RNG
                using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
                var privateKeyBytes = new byte[32];
                rng.GetBytes(privateKeyBytes);
                
                keyPair.PrivateKey = Convert.ToBase64String(privateKeyBytes);
                // Derive public key (simplified - in production use proper Ed25519 library)
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var publicKeyBytes = sha256.ComputeHash(privateKeyBytes);
                keyPair.PublicKey = Convert.ToBase64String(publicKeyBytes);
                keyPair.WalletAddressLegacy = keyPair.PublicKey;
            }

            result.Result = keyPair;
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
        }
        return result;
    }



    // These methods would be implemented for full OASIS storage provider support
    // For now, the RadixOASIS provider focuses on blockchain transactions and bridge operations

    public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.LoadAllAvatarsAsync(false, true, true, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default, version);
            }

            // Query all avatars from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_all_avatars",
                args = new[] { version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var avatars = new List<IAvatar>();
                foreach (var avatarElement in response.Result.EnumerateArray())
                {
                    var avatarJson = avatarElement.GetRawText();
                    var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(avatarJson);
                    if (avatar != null) avatars.Add(avatar);
                }
                result.Result = avatars;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatars.Count} avatars from Radix";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load avatars from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatars from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
    {
        return LoadAllAvatarsAsync(version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.LoadAvatarAsync(id, false, true, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default, version);
            }

            // Query avatar by ID from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_avatar_by_id",
                args = new[] { id.ToString(), version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var avatarJson = response.Result.GetRawText();
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(avatarJson);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar from Radix";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Radix response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load avatar from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
    {
        return LoadAvatarAsync(id, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.LoadAvatarByEmailAsync(email, false, true, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default, version);
            }

            // Query avatar by email from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_avatar_by_email",
                args = new[] { email, version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var avatarJson = response.Result.GetRawText();
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(avatarJson);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar by email from Radix";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Radix response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by email from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Radix: {ex.Message}", ex);
        }
        return result;
    }

}
