using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.CivicOASIS
{
    /// <summary>
    /// Civic On-Chain KYC / Identity OASIS Provider.
    /// Provides decentralised identity verification and on-chain KYC via the
    /// Civic Pass program on Solana and EVM chains.
    ///
    /// REST base: https://api.civic.com/v1
    /// Verify:    GET  /pass/{walletAddress}
    /// Issue:     POST /pass
    /// Revoke:    DELETE /pass/{walletAddress}
    /// </summary>
    public class CivicOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private bool _isActivated;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        public CivicOASIS(string apiKey = "")
        {
            _apiKey = apiKey;
            _http = new HttpClient { BaseAddress = new Uri("https://api.civic.com/v1/") };
            if (!string.IsNullOrEmpty(_apiKey))
                _http.DefaultRequestHeaders.Add("X-API-Key", _apiKey);

            ProviderName = "CivicOASIS";
            ProviderDescription = "Civic on-chain KYC / identity provider via Civic Pass";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CivicOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_isActivated) { result.Result = true; result.Message = "CivicOASIS already activated"; return result; }
                _isActivated = true;
                result.Result = true;
                result.Message = "CivicOASIS activated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CivicOASIS activation failed: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                _isActivated = false;
                _http.Dispose();
                result.Result = true;
                result.Message = "CivicOASIS deactivated";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CivicOASIS deactivation failed: {ex.Message}", ex);
            }
            return result;
        }

        // ── Avatar CRUD ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var resp = await _http.GetAsync($"pass/{id}");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var doc = JsonSerializer.Deserialize<JsonElement>(json, _jsonOpts);
                    var avatar = new Avatar { Id = id };
                    if (doc.TryGetProperty("walletAddress", out var wa)) avatar.Username = wa.GetString();
                    result.Result = avatar;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Civic pass lookup failed ({resp.StatusCode}) for {id}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CivicOASIS LoadAvatarAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var resp = await _http.GetAsync($"pass/{Uri.EscapeDataString(username)}");
                if (resp.IsSuccessStatusCode)
                {
                    var avatar = new Avatar { Username = username };
                    result.Result = avatar;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Civic pass lookup failed ({resp.StatusCode}) for {username}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CivicOASIS LoadAvatarAsync(username) error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                var payload = new { walletAddress = avatar.Username, avatarId = avatar.Id.ToString() };
                var resp = await _http.PostAsync("pass", Json(payload));
                if (resp.IsSuccessStatusCode)
                    result.Result = avatar;
                else
                    OASISErrorHandling.HandleError(ref result, $"Civic pass issue failed ({resp.StatusCode})");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CivicOASIS SaveAvatarAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!softDelete)
                {
                    var resp = await _http.DeleteAsync($"pass/{id}");
                    result.Result = resp.IsSuccessStatusCode;
                    if (!resp.IsSuccessStatusCode)
                        OASISErrorHandling.HandleError(ref result, $"Civic pass revoke failed ({resp.StatusCode})");
                }
                else
                    result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CivicOASIS DeleteAvatarAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                result.Result = new List<IAvatar>();
                result.Message = "CivicOASIS does not support bulk avatar enumeration";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CivicOASIS LoadAllAvatarsAsync error: {ex.Message}", ex);
            }
            return result;
        }

        // ── Holon CRUD ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "CivicOASIS does not support holon storage");
            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "CivicOASIS does not support holon storage");
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "CivicOASIS does not support holon deletion");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            result.Result = new List<IHolon>();
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "CivicOASIS does not support holon storage");
            return result;
        }

        // ── Search ────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            result.Result = new SearchResults();
            return result;
        }

        // ── Avatar Detail ─────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "CivicOASIS does not support avatar detail storage");
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "CivicOASIS does not support avatar detail storage");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            result.Result = new List<IAvatarDetail>();
            return result;
        }
    }
}
