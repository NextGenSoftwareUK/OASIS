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
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
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

                // Load avatar detail as a separate object from Bitcoin OP_RETURN (same pattern as avatar, but deserialize as AvatarDetail)
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "searchrawtransactions",
                    @params = new object[] { id.ToString(), true, 0, 100 }
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
                                            var opReturnData = asmString.Substring("OP_RETURN ".Length);
                                            try
                                            {
                                                var detailBytes = Convert.FromHexString(opReturnData);
                                                var detailJson = Encoding.UTF8.GetString(detailBytes);
                                                var avatarDetail = ParseBitcoinToAvatarDetail(detailJson);
                                                if (avatarDetail != null && avatarDetail.Id == id)
                                                {
                                                    result.Result = avatarDetail;
                                                    result.IsError = false;
                                                    result.Message = "Avatar detail loaded from Bitcoin blockchain successfully";
                                                    return result;
                                                }
                                            }
                                            catch { continue; }
                                        }
                                    }
                                }
                            }
                        }
                        OASISErrorHandling.HandleError(ref result, "Avatar detail not found in Bitcoin blockchain");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "No transactions found for avatar detail ID");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Failed to search Bitcoin blockchain: {searchResponse.StatusCode}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid parentId, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonsForParentAsync(parentId, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
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
                // Load avatar detail as separate object: search chain for OP_RETURN containing avatar detail with this username
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "searchrawtransactions",
                    @params = new object[] { username ?? "", true, 0, 100 }
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
                                    if (avatarDetail != null && string.Equals(avatarDetail.Username, username, StringComparison.OrdinalIgnoreCase))
                                    {
                                        result.Result = avatarDetail;
                                        result.IsError = false;
                                        result.Message = "Avatar detail loaded by username from Bitcoin blockchain successfully";
                                        return result;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found by username on Bitcoin blockchain");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key cannot be null or empty");
                    return result;
                }

                // First load the parent holon by provider key
                var parentResult = await LoadHolonAsync(providerKey, false, false, 0, continueOnError, continueOnErrorRecursive, version);
                
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Parent holon not found: {parentResult.Message}");
                    return result;
                }

                // Then load children for the parent
                var childrenResult = await LoadHolonsForParentAsync(parentResult.Result.Id, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version);
                
                result.Result = childrenResult.Result;
                result.IsError = childrenResult.IsError;
                result.Message = childrenResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, value, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        // NFT Provider interface methods
    }
}
