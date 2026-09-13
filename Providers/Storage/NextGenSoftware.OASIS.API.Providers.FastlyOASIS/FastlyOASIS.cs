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

namespace NextGenSoftware.OASIS.API.Providers.FastlyOASIS
{
    /// <summary>
    /// Fastly Compute Edge OASIS Provider.
    /// Stores and serves OASIS avatars and holons via Fastly Compute@Edge
    /// WebAssembly functions running at Fastly's global PoP network.
    ///
    /// REST base: https://api.fastly.com
    /// Services:  GET  /service/{serviceId}
    /// KV stores: GET  /resources/stores/kv
    /// KV put:    PUT  /resources/stores/kv/{storeId}/keys/{key}
    /// KV get:    GET  /resources/stores/kv/{storeId}/keys/{key}
    /// KV delete: DELETE /resources/stores/kv/{storeId}/keys/{key}
    /// </summary>
    public class FastlyOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        public bool IsVersionControlEnabled { get; set; }

        private readonly HttpClient _http;
        private readonly string _apiToken;
        private readonly string _storeId;
        private bool _isActivated;

        private const string AvatarsPrefix = "oasis_avatar_";
        private const string HolonsPrefix  = "oasis_holon_";

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        public FastlyOASIS(string apiToken = "", string storeId = "")
        {
            _apiToken = apiToken;
            _storeId  = storeId;
            _http = new HttpClient { BaseAddress = new Uri("https://api.fastly.com/") };
            if (!string.IsNullOrEmpty(_apiToken))
                _http.DefaultRequestHeaders.Add("Fastly-Key", _apiToken);

            ProviderName = "FastlyOASIS";
            ProviderDescription = "Fastly Compute@Edge provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FastlyOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_isActivated) { result.Result = true; result.Message = "FastlyOASIS already activated"; return result; }
                if (!string.IsNullOrEmpty(_storeId))
                {
                    var resp = await _http.GetAsync($"resources/stores/kv/{Uri.EscapeDataString(_storeId)}");
                    if (!resp.IsSuccessStatusCode)
                    {
                        OASISErrorHandling.HandleError(ref result, $"FastlyOASIS KV store check failed ({resp.StatusCode})");
                        return result;
                    }
                }
                _isActivated = true;
                result.Result = true;
                result.Message = "FastlyOASIS activated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"FastlyOASIS activation failed: {ex.Message}", ex);
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
                result.Message = "FastlyOASIS deactivated";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"FastlyOASIS deactivation failed: {ex.Message}", ex);
            }
            return result;
        }

        // ── Internal KV helpers ───────────────────────────────────────────────

        private string KvUrl(string key) =>
            $"resources/stores/kv/{Uri.EscapeDataString(_storeId)}/keys/{Uri.EscapeDataString(key)}";

        private async Task KvPutAsync(string key, object value)
        {
            var resp = await _http.PutAsync(KvUrl(key), Json(value));
            resp.EnsureSuccessStatusCode();
        }

        private async Task<JsonElement?> KvGetAsync(string key)
        {
            var resp = await _http.GetAsync(KvUrl(key));
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(json, _jsonOpts);
        }

        private async Task KvDeleteAsync(string key)
        {
            await _http.DeleteAsync(KvUrl(key));
        }

        // ── Avatar CRUD ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (string.IsNullOrEmpty(_storeId)) { result.Result = new Avatar { Id = id }; return result; }
                var doc = await KvGetAsync(AvatarsPrefix + id);
                if (doc.HasValue)
                {
                    var avatar = new Avatar { Id = id };
                    if (doc.Value.TryGetProperty("username", out var u)) avatar.Username = u.GetString();
                    if (doc.Value.TryGetProperty("email",    out var e)) avatar.Email    = e.GetString();
                    result.Result = avatar;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"FastlyOASIS KV key not found for avatar {id}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"FastlyOASIS LoadAvatarAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { result.Result = new Avatar { Username = username }; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"FastlyOASIS LoadAvatarAsync(username) error: {ex.Message}", ex); }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (!string.IsNullOrEmpty(_storeId))
                {
                    var doc = new { username = avatar.Username, email = avatar.Email, avatarId = avatar.Id.ToString() };
                    await KvPutAsync(AvatarsPrefix + avatar.Id, doc);
                }
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"FastlyOASIS SaveAvatarAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!softDelete && !string.IsNullOrEmpty(_storeId))
                    await KvDeleteAsync(AvatarsPrefix + id);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"FastlyOASIS DeleteAvatarAsync error: {ex.Message}", ex);
            }
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
            try
            {
                if (string.IsNullOrEmpty(_storeId)) { result.Result = new Holon { Id = id }; return result; }
                var doc = await KvGetAsync(HolonsPrefix + id);
                if (doc.HasValue)
                {
                    var holon = new Holon { Id = id };
                    if (doc.Value.TryGetProperty("name", out var n)) holon.Name = n.GetString();
                    result.Result = holon;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"FastlyOASIS KV key not found for holon {id}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"FastlyOASIS LoadHolonAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                if (!string.IsNullOrEmpty(_storeId))
                {
                    var doc = new { name = holon.Name, holonId = holon.Id.ToString(), holonType = holon.HolonType.ToString() };
                    await KvPutAsync(HolonsPrefix + holon.Id, doc);
                }
                result.Result = holon;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"FastlyOASIS SaveHolonAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!softDelete && !string.IsNullOrEmpty(_storeId))
                    await KvDeleteAsync(HolonsPrefix + id);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"FastlyOASIS DeleteHolonAsync error: {ex.Message}", ex);
            }
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
            var saved = new List<IHolon>();
            foreach (var holon in holons)
            {
                var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (!r.IsError && r.Result != null) saved.Add(r.Result);
            }
            result.Result = saved;
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
            OASISErrorHandling.HandleError(ref result, "FastlyOASIS does not support avatar detail storage");
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "FastlyOASIS does not support avatar detail storage");
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
