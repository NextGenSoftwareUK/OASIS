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
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Real Bitcoin implementation: Search for avatar data in OP_RETURN transactions
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
                                        if (asmString.StartsWith("OP_RETURN"))
                                        {
                                            // Extract OP_RETURN data
                                            var opReturnData = asmString.Substring("OP_RETURN ".Length);
                                            try
                                            {
                                                var avatarBytes = Convert.FromHexString(opReturnData);
                                                var avatarJson = Encoding.UTF8.GetString(avatarBytes);
                                                var avatar = ParseBitcoinToAvatar(avatarJson);
                                                
                                                if (avatar != null && avatar.Id == id)
                                                {
                                                    response.Result = avatar;
                                                    response.IsError = false;
                                                    response.Message = "Avatar loaded from Bitcoin blockchain successfully";
                                                    return response;
                                                }
                                            }
                                            catch
                                            {
                                                // Skip invalid OP_RETURN data
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Bitcoin blockchain");
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No transactions found for avatar ID");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search Bitcoin blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Bitcoin: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query Bitcoin blockchain for avatar by address
                var queryUrl = $"/address/{providerKey}";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var addressData = JsonSerializer.Deserialize<JsonElement>(content);

                    var avatar = ParseBitcoinToAvatar(addressData, providerKey);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Bitcoin successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Bitcoin address data");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query Bitcoin blockchain for avatar by email using OP_RETURN data
                var queryUrl = $"/address/{avatarEmail}/txs";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement[]>(content);

                    // Search for transactions containing email in OP_RETURN
                    foreach (var tx in txData)
                    {
                        if (tx.TryGetProperty("vout", out var vout))
                        {
                            foreach (var output in vout.EnumerateArray())
                            {
                                if (output.TryGetProperty("scriptpubkey", out var script) &&
                                    script.TryGetProperty("asm", out var asm) &&
                                    asm.GetString().Contains(avatarEmail))
                                {
                                    var avatar = ParseBitcoinToAvatar(tx, avatarEmail);
                                    if (avatar != null)
                                    {
                                        response.Result = avatar;
                                        response.IsError = false;
                                        response.Message = "Avatar loaded from Bitcoin by email successfully";
                                        return response;
                                    }
                                }
                            }
                        }
                    }

                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that email on Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query Bitcoin blockchain for avatar by username using OP_RETURN data
                var queryUrl = $"/address/{avatarUsername}/txs";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement[]>(content);

                    // Search for transactions containing username in OP_RETURN
                    foreach (var tx in txData)
                    {
                        if (tx.TryGetProperty("vout", out var vout))
                        {
                            foreach (var output in vout.EnumerateArray())
                            {
                                if (output.TryGetProperty("scriptpubkey", out var script) &&
                                    script.TryGetProperty("asm", out var asm) &&
                                    asm.GetString().Contains(avatarUsername))
                                {
                                    var avatar = ParseBitcoinToAvatar(tx, avatarUsername);
                                    if (avatar != null)
                                    {
                                        response.Result = avatar;
                                        response.IsError = false;
                                        response.Message = "Avatar loaded from Bitcoin by username successfully";
                                        return response;
                                    }
                                }
                            }
                        }
                    }

                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that username on Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

    }
}
