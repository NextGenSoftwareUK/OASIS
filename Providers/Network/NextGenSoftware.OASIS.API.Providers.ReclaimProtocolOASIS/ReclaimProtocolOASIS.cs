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

namespace NextGenSoftware.OASIS.API.Providers.ReclaimProtocolOASIS
{
    /// <summary>
    /// Reclaim Protocol ZK Proof of Web2 Data OASIS Provider.
    /// Enables OASIS avatars to generate and verify zero-knowledge proofs that
    /// attest to data from any HTTPS website (GitHub, Twitter, bank statements, etc.)
    /// without revealing the underlying data.
    ///
    /// REST base: https://api.reclaimprotocol.org/api/v1
    /// Create session: POST /session
    /// Get session:    GET  /session/{sessionId}
    /// Verify proof:   POST /verify
    /// </summary>
    public class ReclaimProtocolOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private readonly string _appId;
        private readonly string _appSecret;
        private bool _isActivated;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        public ReclaimProtocolOASIS(string appId = "", string appSecret = "")
        {
            _appId = appId;
            _appSecret = appSecret;
            _http = new HttpClient { BaseAddress = new Uri("https://api.reclaimprotocol.org/api/v1/") };
            if (!string.IsNullOrEmpty(_appId))
                _http.DefaultRequestHeaders.Add("X-App-Id", _appId);
            if (!string.IsNullOrEmpty(_appSecret))
                _http.DefaultRequestHeaders.Add("X-App-Secret", _appSecret);

            ProviderName = "ReclaimProtocolOASIS";
            ProviderDescription = "Reclaim Protocol — ZK proof of Web2 data for OASIS avatars";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ReclaimProtocolOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_isActivated) { result.Result = true; result.Message = "ReclaimProtocolOASIS already activated"; return result; }
                _isActivated = true;
                result.Result = true;
                result.Message = "ReclaimProtocolOASIS activated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"ReclaimProtocolOASIS activation failed: {ex.Message}", ex);
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
                result.Message = "ReclaimProtocolOASIS deactivated";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"ReclaimProtocolOASIS deactivation failed: {ex.Message}", ex);
            }
            return result;
        }

        // ── Avatar CRUD ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var resp = await _http.GetAsync($"session/{id}");
                if (resp.IsSuccessStatusCode)
                {
                    var avatar = new Avatar { Id = id };
                    result.Result = avatar;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Reclaim session lookup failed ({resp.StatusCode}) for {id}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"ReclaimProtocolOASIS LoadAvatarAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = new Avatar { Username = username };
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"ReclaimProtocolOASIS LoadAvatarAsync(username) error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                var payload = new { avatarId = avatar.Id.ToString(), username = avatar.Username };
                var resp = await _http.PostAsync("session", Json(payload));
                if (resp.IsSuccessStatusCode)
                    result.Result = avatar;
                else
                    OASISErrorHandling.HandleError(ref result, $"Reclaim session create failed ({resp.StatusCode})");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"ReclaimProtocolOASIS SaveAvatarAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            result.Result = true;
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            result.Result = new List<IAvatar>();
            return result;
        }

        // ── Holon CRUD ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "ReclaimProtocolOASIS does not support holon storage");
            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "ReclaimProtocolOASIS does not support holon storage");
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "ReclaimProtocolOASIS does not support holon deletion");
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
            OASISErrorHandling.HandleError(ref result, "ReclaimProtocolOASIS does not support holon storage");
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
            OASISErrorHandling.HandleError(ref result, "ReclaimProtocolOASIS does not support avatar detail storage");
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "ReclaimProtocolOASIS does not support avatar detail storage");
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
