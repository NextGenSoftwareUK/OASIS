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
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS
{
    public partial class BitcoinOASIS
    {
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                // Load all avatar details as separate objects by scanning blocks for OP_RETURN (same approach as LoadAllHolonsAsync)
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }
                var avatarDetails = new List<IAvatarDetail>();
                var seenIds = new HashSet<Guid>();
                var blockHeightRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getblockcount",
                    @params = new object[0]
                };
                var blockHeightContent = new StringContent(JsonSerializer.Serialize(blockHeightRequest), Encoding.UTF8, "application/json");
                var blockHeightResponse = await _httpClient.PostAsync("", blockHeightContent);
                if (blockHeightResponse.IsSuccessStatusCode)
                {
                    var blockHeightResult = await blockHeightResponse.Content.ReadAsStringAsync();
                    var blockHeightData = JsonSerializer.Deserialize<JsonElement>(blockHeightResult);
                    if (blockHeightData.TryGetProperty("result", out var currentHeight))
                    {
                        var startHeight = Math.Max(0, currentHeight.GetInt32() - 100);
                        for (int height = currentHeight.GetInt32(); height >= startHeight; height--)
                        {
                            var blockHashRequest = new
                            {
                                jsonrpc = "2.0",
                                id = 1,
                                method = "getblockhash",
                                @params = new object[] { height }
                            };
                            var blockHashContent = new StringContent(JsonSerializer.Serialize(blockHashRequest), Encoding.UTF8, "application/json");
                            var blockHashResponse = await _httpClient.PostAsync("", blockHashContent);
                            if (!blockHashResponse.IsSuccessStatusCode) continue;
                            var blockHashResult = await blockHashResponse.Content.ReadAsStringAsync();
                            var blockHashData = JsonSerializer.Deserialize<JsonElement>(blockHashResult);
                            if (!blockHashData.TryGetProperty("result", out var blockHash)) continue;
                            var blockRequest = new
                            {
                                jsonrpc = "2.0",
                                id = 1,
                                method = "getblock",
                                @params = new object[] { blockHash.GetString(), 2 }
                            };
                            var blockContent = new StringContent(JsonSerializer.Serialize(blockRequest), Encoding.UTF8, "application/json");
                            var blockResponse = await _httpClient.PostAsync("", blockContent);
                            if (!blockResponse.IsSuccessStatusCode) continue;
                            var blockResult = await blockResponse.Content.ReadAsStringAsync();
                            var blockData = JsonSerializer.Deserialize<JsonElement>(blockResult);
                            if (!blockData.TryGetProperty("result", out var block) || !block.TryGetProperty("tx", out var transactions)) continue;
                            foreach (var tx in transactions.EnumerateArray())
                            {
                                if (!tx.TryGetProperty("vout", out var vouts)) continue;
                                foreach (var vout in vouts.EnumerateArray())
                                {
                                    if (!vout.TryGetProperty("scriptPubKey", out var scriptPubKey) || !scriptPubKey.TryGetProperty("asm", out var asm)) continue;
                                    var asmString = asm.GetString();
                                    if (asmString == null || !asmString.StartsWith("OP_RETURN")) continue;
                                    try
                                    {
                                        var detailBytes = Convert.FromHexString(asmString.Substring("OP_RETURN ".Length));
                                        var detailJson = Encoding.UTF8.GetString(detailBytes);
                                        var avatarDetail = ParseBitcoinToAvatarDetail(detailJson);
                                        if (avatarDetail != null && seenIds.Add(avatarDetail.Id))
                                            avatarDetails.Add(avatarDetail);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = $"Loaded {avatarDetails.Count} avatar details from Bitcoin blockchain (scanned last 100 blocks)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid parentId, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Search for holons with ParentHolonId in OP_RETURN transactions
                // Bitcoin doesn't natively support queries, so we search transactions containing the parent ID
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "searchrawtransactions",
                    @params = new object[] { parentId.ToString(), true, 0, 1000 }
                };

                var searchContent = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var searchResponse = await _httpClient.PostAsync("", searchContent);

                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchResult = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(searchResult);

                    var holons = new List<IHolon>();
                    if (searchData.TryGetProperty("result", out var transactions))
                    {
                        foreach (var transaction in transactions.EnumerateArray())
                        {
                            if (transaction.TryGetProperty("vout", out var vouts))
                            {
                                foreach (var vout in vouts.EnumerateArray())
                                {
                                    if (vout.TryGetProperty("scriptPubKey", out var scriptPubKey) &&
                                        scriptPubKey.TryGetProperty("asm", out var asm))
                                    {
                                        var asmString = asm.GetString();
                                        if (asmString != null && asmString.StartsWith("OP_RETURN"))
                                        {
                                            try
                                            {
                                                var opReturnData = asmString.Substring("OP_RETURN ".Length);
                                                var holonBytes = Convert.FromHexString(opReturnData);
                                                var holonJson = Encoding.UTF8.GetString(holonBytes);
                                                var holonData = JsonSerializer.Deserialize<JsonElement>(holonJson);
                                                
                                                // Check if this holon has the matching parent ID
                                                if (holonData.TryGetProperty("parent_holon_id", out var parentIdProp) &&
                                                    parentIdProp.GetString() == parentId.ToString())
                                                {
                                                    var holon = ParseBitcoinToHolon(holonJson);
                                                    if (holon != null && (holonType == HolonType.All || holon.HolonType == holonType))
                                                    {
                                                        holons.Add(holon);
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Loaded {holons.Count} holons for parent from Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search Bitcoin blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key cannot be null or empty");
                    return result;
                }

                // Search for holon by provider key (transaction hash) in OP_RETURN transactions
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getrawtransaction",
                    @params = new object[] { providerKey, true }
                };

                var searchContent = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var searchResponse = await _httpClient.PostAsync("", searchContent);

                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchResult = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(searchResult);

                    if (searchData.TryGetProperty("result", out var transaction))
                    {
                        if (transaction.TryGetProperty("vout", out var vouts))
                        {
                            foreach (var vout in vouts.EnumerateArray())
                            {
                                if (vout.TryGetProperty("scriptPubKey", out var scriptPubKey) &&
                                    scriptPubKey.TryGetProperty("asm", out var asm))
                                {
                                    var asmString = asm.GetString();
                                    if (asmString != null && asmString.StartsWith("OP_RETURN"))
                                    {
                                        try
                                        {
                                            var opReturnData = asmString.Substring("OP_RETURN ".Length);
                                            var holonBytes = Convert.FromHexString(opReturnData);
                                            var holonJson = Encoding.UTF8.GetString(holonBytes);
                                            var holon = ParseBitcoinToHolon(holonJson);
                                            
                                            if (holon != null)
                                            {
                                                // Load children if requested
                                                if (loadChildren && (recursive || maxChildDepth > 0))
                                                {
                                                    var childrenResult = await LoadHolonsForParentAsync(holon.Id, HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, continueOnErrorRecursive, version);
                                                    if (!childrenResult.IsError && childrenResult.Result != null)
                                                    {
                                                        holon.Children = childrenResult.Result.ToList();
                                                    }
                                                }
                                                
                                                result.Result = holon;
                                                result.IsError = false;
                                                result.Message = "Holon loaded from Bitcoin blockchain successfully";
                                                return result;
                                            }
                                        }
                                        catch
                                        {
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Bitcoin: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }
                // Bitcoin does not have a native index of avatars; return empty set (avatars are discovered via LoadAvatar by providerKey/email/username)
                result.Result = new List<IAvatar>();
                result.IsError = false;
                result.Message = "Bitcoin provider does not support LoadAllAvatars; returning empty list.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }


        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all holons by scanning recent blocks for OP_RETURN transactions
                // Note: This is computationally expensive but necessary for Bitcoin's UTXO model
                // In production, use an index service to maintain a searchable database
                var holons = new List<IHolon>();
                
                // Get recent blocks (last 100 blocks as a practical limit)
                var blockHeightRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getblockcount",
                    @params = new object[0]
                };

                var blockHeightContent = new StringContent(JsonSerializer.Serialize(blockHeightRequest), Encoding.UTF8, "application/json");
                var blockHeightResponse = await _httpClient.PostAsync("", blockHeightContent);

                if (blockHeightResponse.IsSuccessStatusCode)
                {
                    var blockHeightResult = await blockHeightResponse.Content.ReadAsStringAsync();
                    var blockHeightData = JsonSerializer.Deserialize<JsonElement>(blockHeightResult);
                    
                    if (blockHeightData.TryGetProperty("result", out var currentHeight))
                    {
                        var startHeight = Math.Max(0, currentHeight.GetInt32() - 100); // Last 100 blocks
                        
                        // Scan blocks for OP_RETURN transactions containing holon data
                        for (int height = currentHeight.GetInt32(); height >= startHeight; height--)
                        {
                            var blockHashRequest = new
                            {
                                jsonrpc = "2.0",
                                id = 1,
                                method = "getblockhash",
                                @params = new object[] { height }
                            };

                            var blockHashContent = new StringContent(JsonSerializer.Serialize(blockHashRequest), Encoding.UTF8, "application/json");
                            var blockHashResponse = await _httpClient.PostAsync("", blockHashContent);

                            if (blockHashResponse.IsSuccessStatusCode)
                            {
                                var blockHashResult = await blockHashResponse.Content.ReadAsStringAsync();
                                var blockHashData = JsonSerializer.Deserialize<JsonElement>(blockHashResult);
                                
                                if (blockHashData.TryGetProperty("result", out var blockHash))
                                {
                                    var blockRequest = new
                                    {
                                        jsonrpc = "2.0",
                                        id = 1,
                                        method = "getblock",
                                        @params = new object[] { blockHash.GetString(), 2 } // Verbose mode
                                    };

                                    var blockContent = new StringContent(JsonSerializer.Serialize(blockRequest), Encoding.UTF8, "application/json");
                                    var blockResponse = await _httpClient.PostAsync("", blockContent);

                                    if (blockResponse.IsSuccessStatusCode)
                                    {
                                        var blockResult = await blockResponse.Content.ReadAsStringAsync();
                                        var blockData = JsonSerializer.Deserialize<JsonElement>(blockResult);
                                        
                                        if (blockData.TryGetProperty("result", out var block) &&
                                            block.TryGetProperty("tx", out var transactions))
                                        {
                                            foreach (var tx in transactions.EnumerateArray())
                                            {
                                                if (tx.TryGetProperty("vout", out var vouts))
                                                {
                                                    foreach (var vout in vouts.EnumerateArray())
                                                    {
                                                        if (vout.TryGetProperty("scriptPubKey", out var scriptPubKey) &&
                                                            scriptPubKey.TryGetProperty("asm", out var asm))
                                                        {
                                                            var asmString = asm.GetString();
                                                            if (asmString != null && asmString.StartsWith("OP_RETURN"))
                                                            {
                                                                try
                                                                {
                                                                    var opReturnData = asmString.Substring("OP_RETURN ".Length);
                                                                    var holonBytes = Convert.FromHexString(opReturnData);
                                                                    var holonJson = Encoding.UTF8.GetString(holonBytes);
                                                                    var holonData = JsonSerializer.Deserialize<JsonElement>(holonJson);
                                                                    
                                                                    // Check if this is a holon (has holon_type field)
                                                                    if (holonData.TryGetProperty("holon_type", out var holonTypeProp))
                                                                    {
                                                                        var holon = ParseBitcoinToHolon(holonJson);
                                                                        if (holon != null && (holonType == HolonType.All || holon.HolonType == holonType))
                                                                        {
                                                                            holons.Add(holon);
                                                                        }
                                                                    }
                                                                }
                                                                catch
                                                                {
                                                                    continue;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Loaded {holons.Count} holons from Bitcoin blockchain (scanned last 100 blocks)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(metaData) || string.IsNullOrWhiteSpace(value))
                {
                    OASISErrorHandling.HandleError(ref result, "Metadata key and value are required");
                    return result;
                }

                // Load all holons and filter by metadata
                // Note: This is inefficient but necessary for Bitcoin's UTXO model
                var allHolonsResult = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version);
                
                if (allHolonsResult.IsError || allHolonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons: {allHolonsResult.Message}");
                    return result;
                }

                // Filter holons by metadata
                var matchingHolons = allHolonsResult.Result.Where(h => 
                    h.MetaData != null && 
                    h.MetaData.TryGetValue(metaData, out var metaValue) &&
                    metaValue?.ToString() == value).ToList();

                result.Result = matchingHolons;
                result.IsError = false;
                result.Message = $"Found {matchingHolons.Count} holons matching metadata criteria";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            // This method is already implemented above (around line 2690)
            // Delegate to the implementation that searches by ID
            return await LoadHolonAsync(id.ToString(), loadChildren, recursive, maxChildDepth, continueOnError, continueOnErrorRecursive, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        // LoadAllAvatarsAsync is already implemented above (around line 2700)

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }
                // Load avatar detail as separate object: search chain for OP_RETURN containing avatar detail with this email
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "searchrawtransactions",
                    @params = new object[] { email ?? "", true, 0, 100 }
                };
                var searchContent = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var searchResponse = await _httpClient.PostAsync("", searchContent);
                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchResult = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(searchResult);
                    if (searchData.TryGetProperty("result", out var transactions))
                    {
                        foreach (var transaction in transactions.EnumerateArray())
                        {
                            if (!transaction.TryGetProperty("vout", out var vouts)) continue;
                            foreach (var vout in vouts.EnumerateArray())
                            {
                                if (!vout.TryGetProperty("scriptPubKey", out var scriptPubKey) || !scriptPubKey.TryGetProperty("asm", out var asm)) continue;
                                var asmString = asm.GetString();
                                if (asmString == null || !asmString.StartsWith("OP_RETURN")) continue;
                                try
                                {
                                    var detailBytes = Convert.FromHexString(asmString.Substring("OP_RETURN ".Length));
                                    var detailJson = Encoding.UTF8.GetString(detailBytes);
                                    var avatarDetail = ParseBitcoinToAvatarDetail(detailJson);
                                    if (avatarDetail != null && string.Equals(avatarDetail.Email, email, StringComparison.OrdinalIgnoreCase))
                                    {
                                        result.Result = avatarDetail;
                                        result.IsError = false;
                                        result.Message = "Avatar detail loaded by email from Bitcoin blockchain successfully";
                                        return result;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found by email on Bitcoin blockchain");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        // LoadHolonsByMetaDataAsync (Dictionary) is already implemented above (around line 2738)

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, matchMode, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (metaData == null || metaData.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Metadata dictionary is required");
                    return result;
                }

                // Load all holons and filter by metadata
                var allHolonsResult = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version);
                
                if (allHolonsResult.IsError || allHolonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons: {allHolonsResult.Message}");
                    return result;
                }

                // Filter holons by metadata based on match mode
                IEnumerable<IHolon> matchingHolons;
                if (matchMode == MetaKeyValuePairMatchMode.All)
                {
                    matchingHolons = allHolonsResult.Result.Where(h => 
                        h.MetaData != null && 
                        metaData.All(kvp => h.MetaData.TryGetValue(kvp.Key, out var val) && val?.ToString() == kvp.Value));
                }
                else
                {
                    matchingHolons = allHolonsResult.Result.Where(h => 
                        h.MetaData != null && 
                        metaData.Any(kvp => h.MetaData.TryGetValue(kvp.Key, out var val) && val?.ToString() == kvp.Value));
                }

                result.Result = matchingHolons.ToList();
                result.IsError = false;
                result.Message = $"Found {result.Result.Count()} holons matching metadata criteria";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

    }
}
