using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS
    {
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Hashgraph provider is not activated");
                    return response;
                }

                // Load avatar from Hashgraph network using REAL Hashgraph API
                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.GetAccountInfoAsync(id.ToString());

                if (accountInfo != null)
                {
                    var avatar = ParseHashgraphToAvatar(accountInfo, id);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Hashgraph successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Hashgraph response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on Hashgraph network");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Hashgraph: {ex.Message}");
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
                // Load avatar by provider key from Hashgraph network using REAL Hashgraph API
                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.GetAccountInfoAsync(providerKey);

                if (accountInfo != null)
                {
                    var avatar = ParseHashgraphToAvatar(accountInfo, CreateDeterministicGuid($"{ProviderType.Value}:{accountInfo.AccountId ?? providerKey}"));
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Hashgraph by provider key successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Hashgraph response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on Hashgraph network");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Hashgraph: {ex.Message}");
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
                // Load avatar by email from Hashgraph network using REAL Hashgraph API
                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.GetAccountInfoByEmailAsync(avatarEmail);

                if (accountInfo != null)
                {
                    var avatar = ParseHashgraphToAvatar(accountInfo, CreateDeterministicGuid($"{ProviderType.Value}:{accountInfo.AccountId ?? avatarEmail}"));
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Hashgraph by email successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Hashgraph response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on Hashgraph network");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Hashgraph: {ex.Message}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Hedera mirror nodes do not index "username" on-chain. In this provider we treat username as the Hedera account ID.
                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.GetAccountInfoAsync(avatarUsername);

                if (accountInfo != null)
                {
                    var avatar = ParseHashgraphToAvatar(accountInfo, CreateDeterministicGuid($"{ProviderType.Value}:{accountInfo.AccountId ?? avatarUsername}"));
                    if (avatar != null)
                    {
                        avatar.Version = version;
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Hashgraph by username (account id) successfully";
                    }
                    else
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Hashgraph response");
                }
                else
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on Hashgraph network");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Hashgraph: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarResult = await LoadAvatarAsync(id, version);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var detail = new AvatarDetail
                    {
                        Id = avatarResult.Result.Id,
                        Username = avatarResult.Result.Username,
                        Email = avatarResult.Result.Email,
                        CreatedDate = avatarResult.Result.CreatedDate,
                        ModifiedDate = avatarResult.Result.ModifiedDate
                    };
                    result.Result = detail;
                    result.Message = "Avatar detail loaded from Hashgraph successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, avatarResult.Message ?? "Avatar not found for detail load.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Hashgraph: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail, version);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var detail = new AvatarDetail
                    {
                        Id = avatarResult.Result.Id,
                        Username = avatarResult.Result.Username,
                        Email = avatarResult.Result.Email,
                        CreatedDate = avatarResult.Result.CreatedDate,
                        ModifiedDate = avatarResult.Result.ModifiedDate
                    };
                    result.Result = detail;
                    result.Message = "Avatar detail loaded by email from Hashgraph successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, avatarResult.Message ?? "Avatar not found by email for detail load.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Hashgraph: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var detail = new AvatarDetail
                    {
                        Id = avatarResult.Result.Id,
                        Username = avatarResult.Result.Username,
                        Email = avatarResult.Result.Email,
                        CreatedDate = avatarResult.Result.CreatedDate,
                        ModifiedDate = avatarResult.Result.ModifiedDate
                    };
                    result.Result = detail;
                    result.Message = "Avatar detail loaded by username from Hashgraph successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, avatarResult.Message ?? "Avatar not found by username for detail load.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Hashgraph: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Mirror node supports listing accounts (paginated).
                var accounts = new List<IAvatar>();
                string nextUrl = $"{_httpClient.BaseAddress}/api/v1/accounts?limit=100";

                while (!string.IsNullOrWhiteSpace(nextUrl))
                {
                    var response = await _httpClient.GetAsync(nextUrl);
                    if (!response.IsSuccessStatusCode)
                        break;

                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("accounts", out var accountsArray) && accountsArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var accEl in accountsArray.EnumerateArray())
                        {
                            var accountId = accEl.TryGetProperty("account", out var accIdEl) ? accIdEl.GetString() : null;
                            if (string.IsNullOrWhiteSpace(accountId))
                                continue;

                            var info = new HashgraphAccountInfo
                            {
                                AccountId = accountId,
                                Balance = accEl.TryGetProperty("balance", out var balEl) && balEl.ValueKind == JsonValueKind.Number ? balEl.GetInt64() : 0,
                                AutoRenewPeriod = accEl.TryGetProperty("auto_renew_period", out var arpEl) && arpEl.ValueKind == JsonValueKind.Number ? arpEl.GetInt64() : 0,
                                Expiry = accEl.TryGetProperty("expiry_timestamp", out var expEl) ? expEl.GetString() : ""
                            };

                            var avatar = ParseHashgraphToAvatar(info, CreateDeterministicGuid($"{ProviderType.Value}:{accountId}"));
                            if (avatar != null)
                            {
                                avatar.Version = version;
                                accounts.Add(avatar);
                            }
                        }
                    }

                    nextUrl = null;
                    if (root.TryGetProperty("links", out var linksEl) &&
                        linksEl.TryGetProperty("next", out var nextEl) &&
                        nextEl.ValueKind == JsonValueKind.String)
                    {
                        var next = nextEl.GetString();
                        if (!string.IsNullOrWhiteSpace(next))
                            nextUrl = next.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                ? next
                                : $"{_httpClient.BaseAddress}{next}";
                    }
                }

                result.Result = accounts;
                result.IsError = false;
                result.Message = $"Loaded {accounts.Count} avatars from Hashgraph mirror node.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Hashgraph: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var avatarsResult = await LoadAllAvatarsAsync(version);
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, avatarsResult.Message ?? "Failed to load avatars for avatar details.");
                    return result;
                }

                var details = new List<IAvatarDetail>();
                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar == null) continue;
                    var detailResult = await LoadAvatarDetailAsync(avatar.Id, version);
                    if (!detailResult.IsError && detailResult.Result != null)
                        details.Add(detailResult.Result);
                }
                result.Result = details;
                result.IsError = false;
                result.Message = $"Loaded {details.Count} avatar details from Hashgraph.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

    }
}
