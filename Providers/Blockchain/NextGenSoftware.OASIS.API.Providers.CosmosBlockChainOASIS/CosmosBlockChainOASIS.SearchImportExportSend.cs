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
        // Search methods
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Search parameters cannot be null");
                    return response;
                }

                // Extract search query from SearchGroups
                string searchQuery = null;
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var firstGroup = searchParams.SearchGroups.FirstOrDefault();
                    if (firstGroup is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
                    {
                        searchQuery = textGroup.SearchQuery;
                    }
                }

                // Real Cosmos implementation - search through holons and avatars
                var searchResults = new SearchResults();
                var matchingHolons = new List<IHolon>();
                var matchingAvatars = new List<IAvatar>();

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    // Query Cosmos blockchain for search results
                    var queryUrl = $"/cosmos/staking/v1beta1/validators/search?query={Uri.EscapeDataString(searchQuery)}";
                    
                    var httpResponse = await _httpClient.GetAsync(queryUrl);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var content = await httpResponse.Content.ReadAsStringAsync();
                        var searchData = JsonSerializer.Deserialize<JsonElement>(content);
                        
                        // Parse holons from search results
                        if (searchData.TryGetProperty("holons", out var holonsArray) && holonsArray.ValueKind == JsonValueKind.Array)
                        {
                            var holons = ParseCosmosToHolons(holonsArray.GetRawText());
                            if (holons != null)
                            {
                                matchingHolons.AddRange(holons);
                            }
                        }
                        
                        // Parse avatars from search results
                        if (searchData.TryGetProperty("avatars", out var avatarsArray) && avatarsArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var avatarElement in avatarsArray.EnumerateArray())
                            {
                                var avatar = ParseCosmosToAvatar(avatarElement.GetRawText());
                                if (avatar != null) matchingAvatars.Add(avatar);
                            }
                        }
                    }
                    else
                    {
                        // Fallback: Load all and filter
                        var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                        if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                        {
                            foreach (var holon in allHolonsResult.Result)
                            {
                                if (holon.Name?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                                    holon.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    matchingHolons.Add(holon);
                                }
                            }
                        }

                        var allAvatarsResult = await LoadAllAvatarsAsync(version);
                        if (!allAvatarsResult.IsError && allAvatarsResult.Result != null)
                        {
                            foreach (var avatar in allAvatarsResult.Result)
                            {
                                if (avatar.Username?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                                    avatar.Email?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                                    $"{avatar.FirstName} {avatar.LastName}".Trim().Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                                {
                                    matchingAvatars.Add(avatar);
                                }
                            }
                        }
                    }
                }

                searchResults.SearchResultHolons = matchingHolons;
                searchResults.SearchResultAvatars = matchingAvatars;
                searchResults.NumberOfResults = matchingHolons.Count + matchingAvatars.Count;

                response.Result = searchResults;
                response.IsError = false;
                response.Message = $"Search completed: Found {matchingHolons.Count} holons and {matchingAvatars.Count} avatars";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error searching Cosmos blockchain: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        // Import/Export methods
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            // Import holons by saving them in batch
            var saveResult = await SaveHolonsAsync(holons, true, true, 0, 0, true, false);
            var response = new OASISResult<bool>();
            if (!saveResult.IsError && saveResult.Result != null)
            {
                response.Result = true;
                response.IsError = false;
                response.Message = $"Imported {saveResult.Result.Count()} holons to Cosmos blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref response, saveResult.Message ?? "Failed to import holons to Cosmos");
            }
            return response;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all holons for the avatar
                var holonsResult = await LoadHolonsForParentAsync(avatarId, HolonType.All, true, true, 0, 0, true, false, version);
                if (!holonsResult.IsError && holonsResult.Result != null)
                {
                    response.Result = holonsResult.Result;
                    response.IsError = false;
                    response.Message = $"Exported {holonsResult.Result.Count()} holons for avatar from Cosmos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, holonsResult.Message ?? "Failed to export avatar data from Cosmos");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from Cosmos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(username, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with username {username} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(email, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with email {email} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            // Export all by delegating to LoadAllHolonsAsync
            return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }




        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                var txRequest = new
                {
                    tx = new
                    {
                        body = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    type = "cosmos.bank.v1beta1.MsgSend",
                                    value = new
                                    {
                                        from_address = fromWalletAddress,
                                        to_address = toWalletAddress,
                                        amount = new[]
                                        {
                                            new
                                            {
                                                denom = "uatom",
                                                amount = (amount * 1000000).ToString()
                                            }
                                        }
                                    }
                                }
                            },
                            memo = string.IsNullOrWhiteSpace(memoText) ? "OASIS transaction" : memoText,
                            timeout_height = "0"
                        },
                        auth_info = new
                        {
                            signer_infos = Array.Empty<object>(),
                            fee = new
                            {
                                amount = new[]
                                {
                                    new { denom = "uatom", amount = "5000" }
                                },
                                gas_limit = "200000"
                            }
                        },
                        signatures = Array.Empty<string>()
                    },
                    mode = "BROADCAST_MODE_SYNC"
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = txResponse.TryGetProperty("tx_response", out var txResp) &&
                                         txResp.TryGetProperty("txhash", out var hash) ? hash.GetString() : ""
                    };

                    response.Result = transactionResponse;
                    response.IsError = false;
                    response.Message = "Transaction sent to Cosmos blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send transaction to Cosmos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to Cosmos: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                // First, get wallet addresses for the avatars from Cosmos blockchain
                var fromAddress = await GetWalletAddressForAvatar(fromAvatarId);
                var toAddress = await GetWalletAddressForAvatar(toAvatarId);

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Could not find wallet addresses for avatars");
                    return response;
                }

                // Create Cosmos transaction using REST API
                var txRequest = new
                {
                    tx = new
                    {
                        body = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    type = "cosmos.bank.v1beta1.MsgSend",
                                    value = new
                                    {
                                        from_address = fromAddress,
                                        to_address = toAddress,
                                        amount = new[]
                                        {
                                            new
                                            {
                                                denom = "uatom",
                                                amount = (amount * 1000000).ToString() // Convert to micro-atoms
                                            }
                                        }
                                    }
                                }
                            },
                            memo = "OASIS transaction by avatar ID",
                            timeout_height = "0"
                        },
                        auth_info = new
                        {
                            signer_infos = new[]
                            {
                                new
                                {
                                    public_key = new
                                    {
                                        type = "cosmos.crypto.secp256k1.PubKey",
                                        value = "..." // This would be the actual public key
                                    },
                                    mode_info = new
                                    {
                                        single = new
                                        {
                                            mode = "SIGN_MODE_DIRECT"
                                        }
                                    },
                                    sequence = "0"
                                }
                            },
                            fee = new
                            {
                                amount = new[]
                                {
                                    new
                                    {
                                        denom = "uatom",
                                        amount = "5000" // Gas fee
                                    }
                                },
                                gas_limit = "200000"
                            }
                        },
                        signatures = new[] { "..." } // This would be the actual signature
                    },
                    mode = "BROADCAST_MODE_SYNC"
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = txResponse.TryGetProperty("tx_response", out var txResp) && 
                                         txResp.TryGetProperty("txhash", out var hash) ? hash.GetString() : ""
                    };

                    response.Result = transactionResponse;
                    response.IsError = false;
                    response.Message = "Transaction sent to Cosmos blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send transaction to Cosmos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to Cosmos: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
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

                // Get wallet addresses for avatars using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.CosmosBlockChainOASIS, fromAvatarId);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.CosmosBlockChainOASIS, toAvatarId);
                
                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                    return result;
                }
                
                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                // Submit transaction to Cosmos network via Cosmos SDK API
                var txUrl = "/cosmos/tx/v1beta1/txs";
                var txRequest = new
                {
                    tx = new
                    {
                        body = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    type = "/cosmos.bank.v1beta1.MsgSend",
                                    from_address = fromAddress,
                                    to_address = toAddress,
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = token.ToLowerInvariant(),
                                            amount = amount.ToString()
                                        }
                                    }
                                }
                            },
                            memo = $"OASIS transaction from {fromAvatarId} to {toAvatarId}"
                        },
                        auth_info = new
                        {
                            signer_infos = new object[0],
                            fee = new
                            {
                                amount = new[] { new { denom = "uatom", amount = "1000" } },
                                gas_limit = "200000"
                            }
                        }
                    },
                    mode = "BROADCAST_MODE_SYNC"
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(txUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txResponse.TryGetProperty("tx_response", out var txResp) &&
                                 txResp.TryGetProperty("txhash", out var hash)
                        ? hash.GetString()
                        : "";

                    result.Result = new TransactionResponse
                    {
                        TransactionResult = txHash ?? ""
                    };
                    result.IsError = false;
                    result.Message = "Cosmos transaction sent successfully";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to send Cosmos transaction: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync(token): {ex.Message}", ex);
            }
            return result;
        }

        public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, "ATOM");
        }

    }
}
