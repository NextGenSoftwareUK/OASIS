using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public partial class TelosOASIS
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all avatar details from Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_table_rows",
                    @params = new
                    {
                        code = "oasis.telos", // Telos smart contract account
                        scope = "oasis.telos",
                        table = "avatardetails",
                        limit = 1000, // Load up to 1000 avatar details
                        reverse = false,
                        show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array)
                    {
                        var avatarDetails = new List<IAvatarDetail>();
                        foreach (var avatarDetailData in rows.EnumerateArray())
                        {
                            var avatarDetail = ParseTelosToAvatarDetail(avatarDetailData);
                            if (avatarDetail != null)
                                avatarDetails.Add(avatarDetail);
                        }

                        result.Result = avatarDetails;
                        result.IsError = false;
                        result.Message = $"Loaded {avatarDetails.Count} avatar details from Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to load avatar details from Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar details from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Telos: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = new OASISResult<IAvatar>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Save avatar to Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "push_transaction",
                    @params = new
                    {
                        signatures = new string[0], // Will be filled by wallet
                        compression = "none",
                        packed_context_free_data = "",
                        packed_trx = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            expiration = DateTime.UtcNow.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
                            ref_block_num = 0,
                            ref_block_prefix = 0,
                            max_net_usage_words = 0,
                            max_cpu_usage_ms = 0,
                            delay_sec = 0,
                            context_free_actions = new object[0],
                            actions = new[]
                            {
                                new
                                {
                                    account = "oasis.telos",
                                    name = "upsertavatar",
                                    authorization = new[]
                                    {
                                        new
                                        {
                                            actor = "oasis.telos",
                                            permission = "active"
                                        }
                                    },
                                    data = new
                                    {
                                        id = Avatar.Id.ToString(),
                                        username = Avatar.Username ?? "",
                                        email = Avatar.Email ?? "",
                                        first_name = Avatar.FirstName ?? "",
                                        last_name = Avatar.LastName ?? "",
                                        title = Avatar.Title ?? "",
                                        password = Avatar.Password ?? "",
                                        avatar_type = (int)Avatar.AvatarType.Value,
                                        accept_terms = Avatar.AcceptTerms,
                                        jwt_token = Avatar.JwtToken ?? "",
                                        password_reset = Avatar.PasswordReset.HasValue ? ((DateTimeOffset)Avatar.PasswordReset.Value).ToUnixTimeSeconds() : 0,
                                        refresh_token = Avatar.RefreshToken ?? "",
                                        reset_token = Avatar.ResetToken ?? "",
                                        reset_token_expires = Avatar.ResetTokenExpires.HasValue ? ((DateTimeOffset)Avatar.ResetTokenExpires.Value).ToUnixTimeSeconds() : 0,
                                        verification_token = Avatar.VerificationToken ?? "",
                                        verified = Avatar.Verified.HasValue ? ((DateTimeOffset)Avatar.Verified.Value).ToUnixTimeSeconds() : 0,
                                        last_beamed_in = Avatar.LastBeamedIn.HasValue ? ((DateTimeOffset)Avatar.LastBeamedIn.Value).ToUnixTimeSeconds() : 0,
                                        last_beamed_out = Avatar.LastBeamedOut.HasValue ? ((DateTimeOffset)Avatar.LastBeamedOut.Value).ToUnixTimeSeconds() : 0,
                                        is_beamed_in = Avatar.IsBeamedIn,
                                        created_date = ((DateTimeOffset)Avatar.CreatedDate).ToUnixTimeSeconds(),
                                        modified_date = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                                        description = Avatar.Description ?? "",
                                        is_active = Avatar.IsActive
                                    }
                                }
                            }
                        })))
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/push_transaction", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement))
                    {
                        result.Result = Avatar;
                        result.IsError = false;
                        result.Message = "Avatar saved to Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to save avatar to Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar to Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Telos: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = new OASISResult<IAvatarDetail>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Save avatar detail to Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "push_transaction",
                    @params = new
                    {
                        signatures = new string[0], // Will be filled by wallet
                        compression = "none",
                        packed_context_free_data = "",
                        packed_trx = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            expiration = DateTime.UtcNow.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
                            ref_block_num = 0,
                            ref_block_prefix = 0,
                            max_net_usage_words = 0,
                            max_cpu_usage_ms = 0,
                            delay_sec = 0,
                            context_free_actions = new object[0],
                            actions = new[]
                            {
                                new
                                {
                                    account = "oasis.telos",
                                    name = "upsertavatardetail",
                                    authorization = new[]
                                    {
                                        new
                                        {
                                            actor = "oasis.telos",
                                            permission = "active"
                                        }
                                    },
                                    data = new
                                    {
                                        id = Avatar.Id.ToString(),
                                        username = Avatar.Username ?? "",
                                        email = Avatar.Email ?? "",
                                        karma = Avatar.Karma,
                                        xp = Avatar.XP,
                                        model3d = Avatar.Model3D ?? "",
                                        uma_json = Avatar.UmaJson ?? "",
                                        portrait = Avatar.Portrait ?? "",
                                        town = Avatar.Town ?? "",
                                        county = Avatar.County ?? "",
                                        dob = ((DateTimeOffset)Avatar.DOB).ToUnixTimeSeconds(),
                                        address = Avatar.Address ?? "",
                                        country = Avatar.Country ?? "",
                                        postcode = Avatar.Postcode ?? "",
                                        landline = Avatar.Landline ?? "",
                                        mobile = Avatar.Mobile ?? "",
                                        favourite_colour = (int)Avatar.FavouriteColour,
                                        starcli_colour = (int)Avatar.STARCLIColour,
                                        created_date = ((DateTimeOffset)Avatar.CreatedDate).ToUnixTimeSeconds(),
                                        modified_date = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                                        description = Avatar.Description ?? "",
                                        is_active = Avatar.IsActive
                                    }
                                }
                            }
                        })))
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/push_transaction", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement))
                    {
                        result.Result = Avatar;
                        result.IsError = false;
                        result.Message = "Avatar detail saved to Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to save avatar detail to Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Telos: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar first to get account info
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar not found: {id}");
                    return result;
                }

                // Send delete transaction to Telos smart contract
                var deleteUrl = $"{TELOS_API_BASE_URL}/v1/chain/push_transaction";
                var deleteData = new
                {
                    actions = new[]
                    {
                        new
                        {
                            account = "oasis.telos",
                            name = softDelete ? "softdeleteavatar" : "deleteavatar",
                            authorization = new[]
                            {
                                new { actor = "oasis.telos", permission = "active" }
                            },
                            data = new
                            {
                                avatar_id = id.ToString()
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(deleteData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(deleteUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Avatar {id} {(softDelete ? "soft deleted" : "deleted")} from Telos blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from Telos blockchain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar not found: {avatarEmail}");
                    return result;
                }
                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar not found: {avatarUsername}");
                    return result;
                }
                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar not found: {providerKey}");
                    return result;
                }
                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all holons and avatars, then filter by search params
                var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                var allAvatarsResult = await LoadAllAvatarsAsync(version);

                var searchResults = new SearchResults
                {
                    SearchResultHolons = new List<IHolon>(),
                    NumberOfResults = 0
                };

                // Search in holons
                if (allHolonsResult.Result != null)
                {
                    var matchingHolons = allHolonsResult.Result.Where(h =>
                    {
                        if (h == null) return false;
                        var searchText = (searchParams?.SearchGroups?.FirstOrDefault() as NextGenSoftware.OASIS.API.Core.Objects.Search.ISearchTextGroup)?.SearchQuery?.ToLower() ?? "";
                        return (!string.IsNullOrEmpty(searchText) && (
                            h.Name?.ToLower().Contains(searchText) == true ||
                            h.Description?.ToLower().Contains(searchText) == true ||
                            h.MetaData?.Values.Any(v => v?.ToString()?.ToLower().Contains(searchText) == true) == true
                        ));
                    }).ToList();
                    searchResults.SearchResultHolons.AddRange(matchingHolons);
                }

                // Search in avatars (convert to holons for consistency)
                if (allAvatarsResult.Result != null)
                {
                    var matchingAvatars = allAvatarsResult.Result.Where(a =>
                    {
                        if (a == null) return false;
                        var searchText = (searchParams?.SearchGroups?.FirstOrDefault() as NextGenSoftware.OASIS.API.Core.Objects.Search.ISearchTextGroup)?.SearchQuery?.ToLower() ?? "";
                        return (!string.IsNullOrEmpty(searchText) && (
                            a.Username?.ToLower().Contains(searchText) == true ||
                            a.Email?.ToLower().Contains(searchText) == true ||
                            a.FirstName?.ToLower().Contains(searchText) == true ||
                            a.LastName?.ToLower().Contains(searchText) == true
                        ));
                    }).ToList();
                    
                    // Convert avatars to holons for search results
                    foreach (var avatar in matchingAvatars)
                    {
                        var holon = new Holon
                        {
                            Id = avatar.Id,
                            Name = avatar.Username,
                            Description = $"{avatar.FirstName} {avatar.LastName}",
                            HolonType = HolonType.Avatar
                        };
                        searchResults.SearchResultHolons.Add(holon);
                    }
                }

                searchResults.NumberOfResults = searchResults.SearchResultHolons.Count;
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Found {searchResults.NumberOfResults} results matching '{(searchParams?.SearchGroups?.FirstOrDefault() as NextGenSoftware.OASIS.API.Core.Objects.Search.ISearchTextGroup)?.SearchQuery}'";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error performing search on Telos: {ex.Message}", ex);
            }
            return result;
        }

    }
}
