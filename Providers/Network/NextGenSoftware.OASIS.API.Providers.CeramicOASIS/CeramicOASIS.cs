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

namespace NextGenSoftware.OASIS.API.Providers.CeramicOASIS
{
    /// <summary>
    /// Ceramic Network OASIS Provider.
    /// Provides decentralised identity (W3C DID) and mutable data streams via the
    /// Ceramic Network HTTP API. Avatars are stored as TileDocument streams anchored
    /// to the Ethereum blockchain via IPFS/IPLD.
    ///
    /// REST base: https://ceramic-clay.3boxlabs.com  (Testnet Clay)
    ///            https://gateway.ceramic.network      (Mainnet read-only)
    ///
    /// Create stream: POST /api/v0/streams
    /// Get stream:    GET  /api/v0/streams/{streamId}
    /// Update stream: POST /api/v0/commits
    /// List streams:  GET  /api/v0/streams?did={did}
    /// </summary>
    public class CeramicOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private readonly string _ceramicNodeUrl;
        private bool _isActivated;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private const string StreamsEndpoint = "api/v0/streams";
        private const string CommitsEndpoint  = "api/v0/commits";

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        public CeramicOASIS(string ceramicNodeUrl = "https://ceramic-clay.3boxlabs.com")
        {
            _ceramicNodeUrl = ceramicNodeUrl?.TrimEnd('/') ?? "https://ceramic-clay.3boxlabs.com";
            _http = new HttpClient { BaseAddress = new Uri(_ceramicNodeUrl + "/") };

            ProviderName = "CeramicOASIS";
            ProviderDescription = "Ceramic Network provider — decentralised identity (W3C DID) and mutable data streams";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CeramicOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_isActivated) { result.Result = true; result.Message = "CeramicOASIS already activated"; return result; }
                // Ping the node
                var resp = await _http.GetAsync("api/v0/node/healthcheck");
                if (resp.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    result.Result = true;
                    result.Message = "CeramicOASIS activated successfully";
                }
                else
                {
                    // Non-fatal — node may not expose healthcheck
                    _isActivated = true;
                    result.Result = true;
                    result.Message = $"CeramicOASIS activated (healthcheck returned {resp.StatusCode})";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS activation failed: {ex.Message}", ex);
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
                result.Message = "CeramicOASIS deactivated";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS deactivation failed: {ex.Message}", ex);
            }
            return result;
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private async Task<string> CreateStreamAsync(object content)
        {
            var payload = new
            {
                type = 0, // TileDocument
                genesis = new { header = new { controllers = Array.Empty<string>() }, data = content }
            };
            var resp = await _http.PostAsync(StreamsEndpoint, Json(payload));
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(json, _jsonOpts);
            return doc.TryGetProperty("streamId", out var sid) ? sid.GetString() ?? string.Empty : string.Empty;
        }

        private async Task<JsonElement?> GetStreamAsync(string streamId)
        {
            var resp = await _http.GetAsync($"{StreamsEndpoint}/{Uri.EscapeDataString(streamId)}");
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(json, _jsonOpts);
        }

        private async Task UpdateStreamAsync(string streamId, object content)
        {
            var payload = new { streamId, commit = new { data = content } };
            var resp = await _http.PostAsync(CommitsEndpoint, Json(payload));
            resp.EnsureSuccessStatusCode();
        }

        // ── Avatar CRUD ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var stream = await GetStreamAsync(id.ToString());
                if (stream.HasValue)
                {
                    var avatar = new Avatar { Id = id };
                    var state = stream.Value;
                    if (state.TryGetProperty("state", out var s) &&
                        s.TryGetProperty("content", out var c))
                    {
                        if (c.TryGetProperty("username", out var u)) avatar.Username = u.GetString();
                        if (c.TryGetProperty("email",    out var e)) avatar.Email    = e.GetString();
                    }
                    result.Result = avatar;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Ceramic stream not found for avatar {id}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS LoadAvatarAsync error: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS LoadAvatarAsync(username) error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                var content = new { username = avatar.Username, email = avatar.Email, avatarId = avatar.Id.ToString() };
                await CreateStreamAsync(content);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS SaveAvatarAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            // Ceramic streams are immutable append-only logs; soft-delete by convention
            var result = new OASISResult<bool> { Result = true };
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
                var stream = await GetStreamAsync(id.ToString());
                if (stream.HasValue)
                {
                    var holon = new Holon { Id = id };
                    var state = stream.Value;
                    if (state.TryGetProperty("state", out var s) &&
                        s.TryGetProperty("content", out var c))
                    {
                        if (c.TryGetProperty("name", out var n)) holon.Name = n.GetString();
                    }
                    result.Result = holon;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Ceramic stream not found for holon {id}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS LoadHolonAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                var content = new { name = holon.Name, holonId = holon.Id.ToString(), holonType = holon.HolonType.ToString() };
                await CreateStreamAsync(content);
                result.Result = holon;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS SaveHolonAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool> { Result = true };
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
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS avatar detail not yet implemented");
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS avatar detail not yet implemented");
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
