using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    /// OASIS provider for the Ceramic Network decentralised data protocol (https://ceramic.network).
    /// Uses the Ceramic HTTP API. Configure CERAMIC_API_URL (default: https://ceramic-clay.3boxlabs.com).
    /// DIDs → OASIS Avatars; Ceramic streams → OASIS Holons.
    /// Avatar provider key = DID string (e.g. did:pkh:eip155:1:0x...).
    /// Holon provider key  = Ceramic stream ID (e.g. k2t6...abc).
    /// Write operations require a Ceramic node with an authenticated DID session (CERAMIC_DID env var).
    /// </summary>
    public class CeramicOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private readonly string _apiUrl;
        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public CeramicOASIS(string apiUrl = null)
        {
            _apiUrl = (apiUrl ?? Environment.GetEnvironmentVariable("CERAMIC_API_URL")
                       ?? "https://ceramic-clay.3boxlabs.com").TrimEnd('/');
            _http = new HttpClient { BaseAddress = new Uri(_apiUrl) };
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            ProviderName = "CeramicOASIS";
            ProviderDescription = "Ceramic Network decentralised mutable data streams provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CeramicOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var resp = await _http.GetAsync("/api/v0/node/healthcheck");
                resp.EnsureSuccessStatusCode();
                result.Result = true;
                result.Message = $"CeramicOASIS activated at {_apiUrl}.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS: Error activating: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync() =>
            await Task.FromResult(new OASISResult<bool> { Result = true, Message = "CeramicOASIS deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar: Load ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "CeramicOASIS: Use LoadAvatarByProviderKeyAsync(did:...) to load a DID profile.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) =>
            LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            // Load the BasicProfile stream for a DID via Ceramic's DID-linked streams
            // GET /api/v0/streams?did=<did>&model=<BasicProfile_model_id> or via ComposeDB
            // For broad compatibility we resolve the DID document directly
            var result = new OASISResult<IAvatar>();
            try
            {
                // Resolve DID document: GET /api/v0/identifiers/{did}
                var resp = await _http.GetAsync($"/api/v0/identifiers/{Uri.EscapeDataString(providerKey)}");
                resp.EnsureSuccessStatusCode();
                using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());

                string displayName = providerKey;
                // Try to extract a service endpoint or verification method for display
                if (doc.RootElement.TryGetProperty("didDocument", out var didDoc)
                    && didDoc.TryGetProperty("verificationMethod", out var vm)
                    && vm.ValueKind == JsonValueKind.Array && vm.GetArrayLength() > 0)
                {
                    // No human name in DID doc — use controller as display fallback
                    if (didDoc.TryGetProperty("controller", out var ctrl))
                        displayName = ctrl.GetString() ?? providerKey;
                }

                var avatar = new Avatar
                {
                    Id = DeriveGuid(providerKey),
                    Username = displayName,
                    Description = $"Ceramic DID: {providerKey}",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.CeramicOASIS] = providerKey;
                avatar.ProviderMetaData[Core.Enums.ProviderType.CeramicOASIS]["did"] = providerKey;
                result.Result = avatar;
                result.Message = $"CeramicOASIS: Loaded DID '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"CeramicOASIS: Error loading DID '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) =>
            LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) =>
            await LoadAvatarByProviderKeyAsync(avatarUsername, version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Email lookup is not supported — use DID.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: LoadAllAvatars is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) =>
            LoadAllAvatarsAsync(version).Result;

        // ─── Avatar: Save / Delete ────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            // Update BasicProfile stream content; requires DID session
            // We store the profile data as a stream keyed to the DID
            var result = new OASISResult<IAvatar>();
            try
            {
                string did = avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.CeramicOASIS)
                    ? avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.CeramicOASIS]
                    : avatar.Username;

                var body = new
                {
                    genesis = new
                    {
                        header = new
                        {
                            family = "OASIS",
                            model = "BasicProfile",
                            controllers = new[] { did }
                        },
                        data = new
                        {
                            name = avatar.Username,
                            description = avatar.Description,
                        }
                    },
                    opts = new { pin = true }
                };
                var resp = await _http.PostAsync("/api/v0/streams",
                    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
                if (resp.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                    string streamId = doc.RootElement.TryGetProperty("streamId", out var sid) ? sid.GetString() ?? string.Empty : string.Empty;
                    avatar.ProviderMetaData[Core.Enums.ProviderType.CeramicOASIS]["stream_id"] = streamId;
                    result.Result = avatar;
                    result.Message = $"CeramicOASIS: Avatar profile stream created '{streamId}'.";
                }
                else
                {
                    var bodyStr = await resp.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"CeramicOASIS: Create stream failed ({resp.StatusCode}): {bodyStr}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS: Error saving avatar: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: SaveAvatarDetail is not separately supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) =>
            SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Ceramic streams are immutable history — they cannot be permanently deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) =>
            DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Ceramic streams are immutable history — they cannot be permanently deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) =>
            DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Streams cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) =>
            DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Streams cannot be deleted.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) =>
            DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        // ─── Avatar Detail ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Use LoadAvatarDetailByUsernameAsync(did:...) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) =>
            LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var r = await LoadAvatarByUsernameAsync(avatarUsername, version);
            var result = new OASISResult<IAvatarDetail>();
            if (!r.IsError && r.Result != null)
                result.Result = new AvatarDetail { Id = r.Result.Id, Username = r.Result.Username, Description = r.Result.Description };
            else { result.IsError = r.IsError; result.Message = r.Message; result.Exception = r.Exception; }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Email not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: LoadAllAvatarDetails is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) =>
            LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon: Load ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "CeramicOASIS: Use LoadHolonAsync(streamId) to load a Ceramic stream.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError,
                loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var resp = await _http.GetAsync($"/api/v0/streams/{Uri.EscapeDataString(providerKey)}");
                resp.EnsureSuccessStatusCode();
                using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                result.Result = MapStreamToHolon(doc.RootElement, providerKey);
                result.Message = $"CeramicOASIS: Loaded stream '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"CeramicOASIS: Error loading stream '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError,
                loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result,
                "CeramicOASIS: LoadAllHolons is not supported — use LoadHolonsForParentAsync(did:...) to query streams for a DID.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) =>
            LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth,
                continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result,
                "CeramicOASIS: Use LoadHolonsForParentAsync(did:...) to query streams for a DID.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            // List streams pinned to this node; filter by controller (DID) if providerKey is a DID
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string url = "/api/v0/admin/pin/ls";
                var resp = await _http.GetAsync(url);
                var holons = new List<IHolon>();
                if (resp.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                    // Response: { pinnedStreamIds: [...] }
                    if (doc.RootElement.TryGetProperty("pinnedStreamIds", out var ids))
                    {
                        foreach (var sid in ids.EnumerateArray())
                        {
                            string streamId = sid.GetString() ?? string.Empty;
                            if (string.IsNullOrEmpty(streamId)) continue;
                            // Load each stream and filter by controller if providerKey is a DID
                            var streamResp = await _http.GetAsync($"/api/v0/streams/{Uri.EscapeDataString(streamId)}");
                            if (!streamResp.IsSuccessStatusCode) continue;
                            using var sd = await JsonDocument.ParseAsync(await streamResp.Content.ReadAsStreamAsync());
                            if (!string.IsNullOrEmpty(providerKey) && providerKey.StartsWith("did:"))
                            {
                                string controller = string.Empty;
                                if (sd.RootElement.TryGetProperty("metadata", out var meta)
                                    && meta.TryGetProperty("controllers", out var ctrl)
                                    && ctrl.ValueKind == JsonValueKind.Array && ctrl.GetArrayLength() > 0)
                                    controller = ctrl[0].GetString() ?? string.Empty;
                                if (controller != providerKey) continue;
                            }
                            holons.Add(MapStreamToHolon(sd.RootElement, streamId));
                        }
                    }
                }
                result.Result = holons;
                result.Message = $"CeramicOASIS: Loaded {holons.Count} streams.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"CeramicOASIS: Error loading streams for '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey,
            string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: LoadHolonsByMetaData is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(
            Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: LoadHolonsByMetaData is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(
            Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive,
                maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon: Save ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                string existingStreamId = holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.CeramicOASIS)
                    ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.CeramicOASIS] : string.Empty;

                if (string.IsNullOrEmpty(existingStreamId))
                {
                    // Create a new TileDocument stream
                    string controllerDid = Environment.GetEnvironmentVariable("CERAMIC_DID") ?? string.Empty;
                    var body = new
                    {
                        type = 0, // TileDocument
                        genesis = new
                        {
                            header = new
                            {
                                family = "OASIS",
                                tags = new[] { "oasis", "holon" },
                                controllers = string.IsNullOrEmpty(controllerDid)
                                    ? Array.Empty<string>() : new[] { controllerDid }
                            },
                            data = new
                            {
                                name = holon.Name,
                                holonType = holon.HolonType.ToString(),
                                description = holon.Description,
                                created = holon.CreatedDate.ToString("O"),
                            }
                        },
                        opts = new { anchor = true, publish = true, pin = true }
                    };
                    var resp = await _http.PostAsync("/api/v0/streams",
                        new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
                    if (resp.IsSuccessStatusCode)
                    {
                        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                        string streamId = doc.RootElement.TryGetProperty("streamId", out var sid) ? sid.GetString() ?? string.Empty : string.Empty;
                        holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.CeramicOASIS] = streamId;
                        holon.ProviderMetaData[Core.Enums.ProviderType.CeramicOASIS]["stream_id"] = streamId;
                        result.Result = holon;
                        result.Message = $"CeramicOASIS: Created stream '{streamId}'.";
                    }
                    else
                    {
                        var bodyStr = await resp.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref result,
                            $"CeramicOASIS: Create stream failed ({resp.StatusCode}): {bodyStr}");
                    }
                }
                else
                {
                    // Update existing stream with a new commit
                    var updateBody = new
                    {
                        streamId = existingStreamId,
                        data = new[]
                        {
                            new { op = "replace", path = "/name", value = holon.Name },
                            new { op = "replace", path = "/description", value = holon.Description ?? string.Empty },
                        },
                        opts = new { anchor = true, publish = true }
                    };
                    var resp = await _http.PostAsync("/api/v0/commits",
                        new StringContent(JsonSerializer.Serialize(updateBody), Encoding.UTF8, "application/json"));
                    if (resp.IsSuccessStatusCode)
                    {
                        result.Result = holon;
                        result.Message = $"CeramicOASIS: Updated stream '{existingStreamId}'.";
                    }
                    else
                    {
                        var bodyStr = await resp.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref result,
                            $"CeramicOASIS: Update stream failed ({resp.StatusCode}): {bodyStr}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS: Error saving holon: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool saveChildrenOnProvider = false) =>
            SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            try
            {
                foreach (var h in holons)
                {
                    var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (!r.IsError && r.Result != null) saved.Add(r.Result);
                    else if (!continueOnError) { OASISErrorHandling.HandleError(ref result, r.Message); return result; }
                }
                result.Result = saved;
                result.Message = $"CeramicOASIS: Saved {saved.Count} holons.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS: Error saving holons: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false) =>
            SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth,
                continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon: Delete ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "CeramicOASIS: Streams are append-only — use DeleteHolonAsync(streamId) to unpin from this node.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            // Unpin stream from local node (data still exists on other nodes)
            var result = new OASISResult<IHolon>();
            try
            {
                var resp = await _http.DeleteAsync($"/api/v0/admin/pin/{Uri.EscapeDataString(providerKey)}");
                if (resp.IsSuccessStatusCode)
                    result.Message = $"CeramicOASIS: Stream '{providerKey}' unpinned from this node.";
                else
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"CeramicOASIS: Unpin failed ({resp.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"CeramicOASIS: Error deleting holon '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                string q = string.Empty;
                if (searchParams.SearchGroups != null)
                    foreach (var g in searchParams.SearchGroups)
                        if (g is NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup tg && !string.IsNullOrEmpty(tg.SearchQuery))
                        { q = tg.SearchQuery; break; }

                // Load all pinned streams and filter by name
                var allHolons = await LoadHolonsForParentAsync(string.Empty);
                var searchResults = new SearchResults();
                if (!allHolons.IsError && allHolons.Result != null)
                    foreach (var h in allHolons.Result)
                        if (string.IsNullOrEmpty(q) || h.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                            searchResults.SearchResultHolons.Add(h);

                result.Result = searchResults;
                result.Message = $"CeramicOASIS: Found {searchResults.SearchResultHolons.Count} streams for '{q}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"CeramicOASIS: Error searching: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0) =>
            SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Import is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Use ExportAllDataForAvatarByUsernameAsync(did:...).");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) =>
            ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) =>
            await LoadHolonsForParentAsync(avatarUsername, version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) =>
            ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Email not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) =>
            ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) =>
            await LoadHolonsForParentAsync(string.Empty, version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Geolocation is not supported.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "CeramicOASIS: Geolocation is not supported.");
            return result;
        }

        // ─── Mapping ──────────────────────────────────────────────────────────────

        private static Holon MapStreamToHolon(JsonElement streamEl, string streamId)
        {
            string name = streamId;
            string description = string.Empty;
            DateTime created = DateTime.UtcNow;
            DateTime modified = DateTime.UtcNow;
            string controller = string.Empty;
            string family = string.Empty;

            // Stream state: { metadata: {controllers, family}, content: {...}, log: [...] }
            if (streamEl.TryGetProperty("state", out var state))
            {
                if (state.TryGetProperty("metadata", out var meta))
                {
                    if (meta.TryGetProperty("controllers", out var ctrl)
                        && ctrl.ValueKind == JsonValueKind.Array && ctrl.GetArrayLength() > 0)
                        controller = ctrl[0].GetString() ?? string.Empty;
                    if (meta.TryGetProperty("family", out var fam))
                        family = fam.GetString() ?? string.Empty;
                }
                if (state.TryGetProperty("content", out var content))
                {
                    if (content.TryGetProperty("name", out var n)) name = n.GetString() ?? streamId;
                    if (content.TryGetProperty("description", out var d)) description = d.GetString() ?? string.Empty;
                    if (content.TryGetProperty("created", out var cr) && DateTime.TryParse(cr.GetString(), out var c)) created = c;
                }
            }
            else if (streamEl.TryGetProperty("content", out var content))
            {
                if (content.TryGetProperty("name", out var n)) name = n.GetString() ?? streamId;
                if (content.TryGetProperty("description", out var d)) description = d.GetString() ?? string.Empty;
            }

            var h = new Holon
            {
                Id = DeriveGuid(streamId),
                Name = name,
                Description = description,
                HolonType = HolonType.Holon,
                CreatedDate = created,
                ModifiedDate = modified,
            };
            h.ProviderUniqueStorageKey[Core.Enums.ProviderType.CeramicOASIS] = streamId;
            h.ProviderMetaData[Core.Enums.ProviderType.CeramicOASIS]["stream_id"] = streamId;
            h.ProviderMetaData[Core.Enums.ProviderType.CeramicOASIS]["controller"] = controller;
            h.ProviderMetaData[Core.Enums.ProviderType.CeramicOASIS]["family"] = family;
            return h;
        }

        private static Guid DeriveGuid(string key)
        {
            if (Guid.TryParse(key, out var g)) return g;
            using var md5 = System.Security.Cryptography.MD5.Create();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(key)));
        }
    }
}
