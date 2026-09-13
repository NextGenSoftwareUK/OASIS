using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
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

namespace NextGenSoftware.OASIS.API.Providers.MatrixOASIS
{
    /// <summary>
    /// OASIS provider for the Matrix open protocol (https://matrix.org).
    /// Uses the Matrix Client-Server API v3 against a configurable homeserver.
    /// Matrix users (@user:server) → OASIS Avatars; room events → OASIS Holons.
    /// Auth: POST /_matrix/client/v3/login with user/password or token.
    /// Set MATRIX_HOMESERVER, MATRIX_USERNAME, MATRIX_PASSWORD env vars, or pass
    /// them to the constructor. MATRIX_ACCESS_TOKEN bypasses login if already known.
    /// Holons provider key format: "!roomId:server/eventId" for events, "!roomId:server" for rooms.
    /// </summary>
    public class MatrixOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private readonly string _homeserver;
        private readonly string _username;
        private readonly string _password;
        private string _accessToken;
        private string _userId;
        private long _txnCounter;
        private bool _isActivated;
        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public MatrixOASIS(string homeserver = null, string username = null,
            string password = null, string accessToken = null)
        {
            _homeserver = (homeserver ?? Environment.GetEnvironmentVariable("MATRIX_HOMESERVER")
                           ?? "https://matrix.org").TrimEnd('/');
            _username = username ?? Environment.GetEnvironmentVariable("MATRIX_USERNAME") ?? string.Empty;
            _password = password ?? Environment.GetEnvironmentVariable("MATRIX_PASSWORD") ?? string.Empty;
            _accessToken = accessToken ?? Environment.GetEnvironmentVariable("MATRIX_ACCESS_TOKEN") ?? string.Empty;
            _http = new HttpClient { BaseAddress = new Uri(_homeserver) };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(_accessToken))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _accessToken);

            ProviderName = "MatrixOASIS";
            ProviderDescription = "Matrix open protocol decentralised communications provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MatrixOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Activation ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    var body = new
                    {
                        type = "m.login.password",
                        identifier = new { type = "m.id.user", user = _username },
                        password = _password,
                    };
                    var resp = await PostRawAsync("/_matrix/client/v3/login", body);
                    if (!resp.IsSuccessStatusCode)
                    {
                        var err = await resp.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref result,
                            $"MatrixOASIS: Login failed ({resp.StatusCode}): {err}");
                        return result;
                    }
                    var session = await resp.Content.ReadFromJsonAsync<MatrixLoginResponse>(_jsonOpts);
                    _accessToken = session!.AccessToken;
                    _userId = session.UserId;
                    _http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _accessToken);
                }
                else
                {
                    // Validate token by calling /whoami
                    var resp = await _http.GetAsync("/_matrix/client/v3/account/whoami");
                    if (!resp.IsSuccessStatusCode)
                    {
                        OASISErrorHandling.HandleError(ref result,
                            $"MatrixOASIS: Token validation failed ({resp.StatusCode}).");
                        return result;
                    }
                    var whoami = await resp.Content.ReadFromJsonAsync<MatrixWhoami>(_jsonOpts);
                    _userId = whoami!.UserId;
                }
                _isActivated = true;
                result.Result = true;
                result.Message = $"MatrixOASIS activated as {_userId} on {_homeserver}.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"MatrixOASIS: Error activating: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            try
            {
                await _http.PostAsync("/_matrix/client/v3/logout",
                    new StringContent("{}", Encoding.UTF8, "application/json"));
            }
            catch { /* best-effort */ }
            _accessToken = null;
            _userId = null;
            _http.DefaultRequestHeaders.Authorization = null;
            _isActivated = false;
            return await Task.FromResult(new OASISResult<bool> { Result = true, Message = "MatrixOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar: Load ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "MatrixOASIS: Use LoadAvatarByProviderKeyAsync(@user:server) to load a Matrix user.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) =>
            LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            // providerKey = Matrix user ID, e.g. @alice:matrix.org
            var result = new OASISResult<IAvatar>();
            try
            {
                var encoded = Uri.EscapeDataString(providerKey);
                var resp = await _http.GetAsync($"/_matrix/client/v3/profile/{encoded}");
                if (resp.IsSuccessStatusCode)
                {
                    var profile = await resp.Content.ReadFromJsonAsync<MatrixProfile>(_jsonOpts);
                    result.Result = MapProfileToAvatar(providerKey, profile!);
                    result.Message = $"MatrixOASIS: Loaded profile for {providerKey}.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"MatrixOASIS: GET /profile failed ({resp.StatusCode}) for '{providerKey}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"MatrixOASIS: Error loading avatar '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) =>
            LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            // avatarUsername may be just the local part; prepend homeserver domain if needed
            string matrixId = avatarUsername.StartsWith("@") ? avatarUsername
                : $"@{avatarUsername}:{new Uri(_homeserver).Host}";
            return await LoadAvatarByProviderKeyAsync(matrixId, version);
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            // Matrix supports 3PID (third-party identifier) lookup
            var result = new OASISResult<IAvatar>();
            try
            {
                var body = new
                {
                    threepids = new[] { new { medium = "email", address = avatarEmail } }
                };
                var resp = await PostRawAsync("/_matrix/client/v3/account/3pid/lookup", body);
                if (resp.IsSuccessStatusCode)
                {
                    var lookup = await resp.Content.ReadFromJsonAsync<MatrixLookupResponse>(_jsonOpts);
                    if (lookup?.Threepids != null && lookup.Threepids.Count > 0)
                        return await LoadAvatarByProviderKeyAsync(lookup.Threepids[0].Mxid, version);
                    OASISErrorHandling.HandleError(ref result,
                        $"MatrixOASIS: No Matrix account found for email '{avatarEmail}'.");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"MatrixOASIS: 3PID lookup failed ({resp.StatusCode}).");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"MatrixOASIS: Error looking up avatar by email '{avatarEmail}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result,
                "MatrixOASIS: LoadAllAvatars is not supported — Matrix has no global user enumeration API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) =>
            LoadAllAvatarsAsync(version).Result;

        // ─── Avatar: Save / Delete ────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                // Update display name
                var resp = await _http.PutAsync(
                    $"/_matrix/client/v3/profile/{Uri.EscapeDataString(_userId)}/displayname",
                    new StringContent(JsonSerializer.Serialize(new { displayname = avatar.Username }),
                        Encoding.UTF8, "application/json"));
                if (resp.IsSuccessStatusCode)
                {
                    result.Result = avatar;
                    result.Message = "MatrixOASIS: Display name updated.";
                }
                else
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"MatrixOASIS: Display name update failed ({resp.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"MatrixOASIS: Error saving avatar: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "MatrixOASIS: SaveAvatarDetail is not separately supported — use SaveAvatarAsync.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) =>
            SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "MatrixOASIS: Account deactivation must be done via the Matrix homeserver admin API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) =>
            DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "MatrixOASIS: Account deactivation must be done via the Matrix homeserver admin API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) =>
            DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "MatrixOASIS: Account deactivation must be done via the Matrix homeserver admin API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) =>
            DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "MatrixOASIS: Account deactivation must be done via the Matrix homeserver admin API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) =>
            DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        // ─── Avatar Detail ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "MatrixOASIS: Use LoadAvatarDetailByUsernameAsync(@user:server) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) =>
            LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            var result = new OASISResult<IAvatarDetail>();
            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                var detail = new AvatarDetail
                {
                    Id = avatarResult.Result.Id,
                    Username = avatarResult.Result.Username,
                    Description = avatarResult.Result.Description,
                };
                if (avatarResult.Result.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MatrixOASIS))
                    detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.MatrixOASIS] =
                        avatarResult.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.MatrixOASIS];
                result.Result = detail;
            }
            else
            {
                result.IsError = avatarResult.IsError;
                result.Message = avatarResult.Message;
                result.Exception = avatarResult.Exception;
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var r = await LoadAvatarByEmailAsync(avatarEmail, version);
            var result = new OASISResult<IAvatarDetail>();
            if (!r.IsError && r.Result != null)
                result.Result = new AvatarDetail { Id = r.Result.Id, Username = r.Result.Username };
            else { result.IsError = r.IsError; result.Message = r.Message; result.Exception = r.Exception; }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "MatrixOASIS: LoadAllAvatarDetails is not supported.");
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
                "MatrixOASIS: Use LoadHolonAsync(\"!roomId:server/eventId\") or LoadHolonsForParentAsync(\"!roomId:server\").");
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
            // providerKey = "!roomId:server/eventId"
            var result = new OASISResult<IHolon>();
            try
            {
                int slash = providerKey.IndexOf('/');
                if (slash < 0)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"MatrixOASIS: providerKey must be '!roomId:server/eventId', got '{providerKey}'.");
                    return result;
                }
                string roomId = providerKey[..slash];
                string eventId = providerKey[(slash + 1)..];
                var resp = await _http.GetAsync(
                    $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomId)}/event/{Uri.EscapeDataString(eventId)}");
                if (resp.IsSuccessStatusCode)
                {
                    var ev = await resp.Content.ReadFromJsonAsync<MatrixEvent>(_jsonOpts);
                    result.Result = MapEventToHolon(ev!, providerKey);
                    result.Message = $"MatrixOASIS: Loaded event {providerKey}.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"MatrixOASIS: GET event failed ({resp.StatusCode}) for '{providerKey}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"MatrixOASIS: Error loading holon '{providerKey}': {ex.Message}");
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
            // Load events from all joined rooms
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var resp = await _http.GetAsync("/_matrix/client/v3/joined_rooms");
                if (!resp.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"MatrixOASIS: joined_rooms failed ({resp.StatusCode}).");
                    return result;
                }
                var rooms = await resp.Content.ReadFromJsonAsync<MatrixJoinedRooms>(_jsonOpts);
                var holons = new List<IHolon>();
                foreach (var roomId in rooms!.JoinedRooms ?? new List<string>())
                {
                    var sub = await LoadHolonsForParentAsync(roomId, type, loadChildren, recursive,
                        maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                    if (!sub.IsError && sub.Result != null)
                        holons.AddRange(sub.Result);
                }
                result.Result = holons;
                result.Message = $"MatrixOASIS: Loaded {holons.Count} events from {rooms.JoinedRooms?.Count ?? 0} rooms.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"MatrixOASIS: Error loading all holons: {ex.Message}");
            }
            return result;
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
                "MatrixOASIS: Use LoadHolonsForParentAsync(\"!roomId:server\") to load events from a room.");
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
            // providerKey = Matrix room ID, e.g. !abc123:matrix.org
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                string from = null;
                bool first = true;
                do
                {
                    string url = $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(providerKey)}/messages" +
                                 $"?dir=b&limit=100" +
                                 (from != null ? $"&from={Uri.EscapeDataString(from)}" : "");
                    var resp = await _http.GetAsync(url);
                    if (!resp.IsSuccessStatusCode) break;
                    var chunk = await resp.Content.ReadFromJsonAsync<MatrixMessagesResponse>(_jsonOpts);
                    if (chunk?.Chunk != null)
                        foreach (var ev in chunk.Chunk)
                            if (ev.Type == "m.room.message")
                                holons.Add(MapEventToHolon(ev, $"{providerKey}/{ev.EventId}"));
                    from = chunk?.End;
                    first = false;
                }
                while (!string.IsNullOrEmpty(from) && holons.Count < 2000);

                result.Result = holons;
                result.Message = $"MatrixOASIS: Loaded {holons.Count} messages from room '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"MatrixOASIS: Error loading events for room '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── MetaData ─────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey,
            string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "MatrixOASIS: LoadHolonsByMetaData is not supported.");
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
            OASISErrorHandling.HandleError(ref result, "MatrixOASIS: LoadHolonsByMetaData is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(
            Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren,
                recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon: Save ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool saveChildrenOnProvider = false)
        {
            // Send a message to the room stored in ParentHolonId metadata, or use holon's
            // ProviderUniqueStorageKey as "!roomId:server" to target a room
            var result = new OASISResult<IHolon>();
            try
            {
                string roomId = null;
                if (holon.ProviderUniqueStorageKey != null
                    && holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MatrixOASIS))
                {
                    var key = holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.MatrixOASIS];
                    int slash = key.IndexOf('/');
                    roomId = slash >= 0 ? key[..slash] : key;
                }
                if (string.IsNullOrEmpty(roomId))
                {
                    OASISErrorHandling.HandleError(ref result,
                        "MatrixOASIS: Set holon.ProviderUniqueStorageKey[MatrixOASIS] to '!roomId:server' before saving.");
                    return result;
                }

                long txn = Interlocked.Increment(ref _txnCounter);
                var body = new { msgtype = "m.text", body = holon.Name };
                var resp = await _http.PutAsync(
                    $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomId)}/send/m.room.message/{txn}",
                    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

                if (resp.IsSuccessStatusCode)
                {
                    var sent = await resp.Content.ReadFromJsonAsync<MatrixSendResponse>(_jsonOpts);
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.MatrixOASIS] =
                        $"{roomId}/{sent!.EventId}";
                    result.Result = holon;
                    result.Message = $"MatrixOASIS: Event sent: {sent.EventId} in {roomId}.";
                }
                else
                {
                    var body2 = await resp.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"MatrixOASIS: Send event failed ({resp.StatusCode}): {body2}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"MatrixOASIS: Error saving holon: {ex.Message}");
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
                foreach (var holon in holons)
                {
                    var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth,
                        continueOnError, saveChildrenOnProvider);
                    if (!r.IsError && r.Result != null) saved.Add(r.Result);
                    else if (!continueOnError)
                    { OASISErrorHandling.HandleError(ref result, r.Message); return result; }
                }
                result.Result = saved;
                result.Message = $"MatrixOASIS: Saved {saved.Count} holons.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"MatrixOASIS: Error saving holons: {ex.Message}");
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
                "MatrixOASIS: Use DeleteHolonAsync(\"!roomId:server/eventId\") to redact an event.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            // Redact the event: providerKey = "!roomId:server/eventId"
            var result = new OASISResult<IHolon>();
            try
            {
                int slash = providerKey.IndexOf('/');
                if (slash < 0)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"MatrixOASIS: providerKey must be '!roomId:server/eventId', got '{providerKey}'.");
                    return result;
                }
                string roomId = providerKey[..slash];
                string eventId = providerKey[(slash + 1)..];
                long txn = Interlocked.Increment(ref _txnCounter);
                var resp = await _http.PutAsync(
                    $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomId)}/redact/{Uri.EscapeDataString(eventId)}/{txn}",
                    new StringContent("{}", Encoding.UTF8, "application/json"));
                if (resp.IsSuccessStatusCode)
                    result.Message = $"MatrixOASIS: Event '{providerKey}' redacted.";
                else
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"MatrixOASIS: Redact failed ({resp.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"MatrixOASIS: Error deleting holon '{providerKey}': {ex.Message}");
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

                var searchResults = new SearchResults();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var body = new { search_categories = new { room_events = new { search_term = q } } };
                    var resp = await PostRawAsync("/_matrix/client/v3/search", body);
                    if (resp.IsSuccessStatusCode)
                    {
                        var data = await resp.Content.ReadFromJsonAsync<MatrixSearchResponse>(_jsonOpts);
                        var results = data?.SearchCategories?.RoomEvents?.Results;
                        if (results != null)
                            foreach (var r in results)
                                if (r.Result != null)
                                    searchResults.SearchResultHolons.Add(
                                        MapEventToHolon(r.Result, $"{r.Result.RoomId}/{r.Result.EventId}"));
                    }
                }
                result.Result = searchResults;
                result.Message = $"MatrixOASIS: Found {searchResults.SearchResultHolons.Count} events for '{q}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"MatrixOASIS: Error searching: {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "MatrixOASIS: Import is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "MatrixOASIS: Use ExportAllAsync() to export joined room events.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) =>
            ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) =>
            await LoadAllHolonsAsync(version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) =>
            ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) =>
            await LoadAllHolonsAsync(version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) =>
            ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) =>
            await LoadAllHolonsAsync(version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "MatrixOASIS: Geolocation is not supported.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "MatrixOASIS: Geolocation is not supported.");
            return result;
        }

        // ─── HTTP helpers ─────────────────────────────────────────────────────────

        private Task<HttpResponseMessage> PostRawAsync(string path, object body)
        {
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            return _http.PostAsync(path, content);
        }

        // ─── Mapping ──────────────────────────────────────────────────────────────

        private Avatar MapProfileToAvatar(string userId, MatrixProfile p)
        {
            var avatar = new Avatar
            {
                Id = DeriveGuid(userId),
                Username = p.Displayname ?? userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MatrixOASIS] = userId;
            avatar.ProviderMetaData[Core.Enums.ProviderType.MatrixOASIS]["user_id"] = userId;
            avatar.ProviderMetaData[Core.Enums.ProviderType.MatrixOASIS]["display_name"] = p.Displayname ?? string.Empty;
            avatar.ProviderMetaData[Core.Enums.ProviderType.MatrixOASIS]["avatar_url"] = p.AvatarUrl ?? string.Empty;
            return avatar;
        }

        private static Holon MapEventToHolon(MatrixEvent ev, string providerKey)
        {
            string body = ev.Content.TryGetProperty("body", out var b)
                ? b.GetString() ?? string.Empty : string.Empty;
            var h = new Holon
            {
                Id = DeriveGuid(ev.EventId ?? providerKey),
                Name = body,
                HolonType = HolonType.Holon,
                CreatedDate = DateTimeOffset.FromUnixTimeMilliseconds(ev.OriginServerTs).UtcDateTime,
                ModifiedDate = DateTimeOffset.FromUnixTimeMilliseconds(ev.OriginServerTs).UtcDateTime,
            };
            h.ProviderUniqueStorageKey[Core.Enums.ProviderType.MatrixOASIS] = providerKey;
            h.ProviderMetaData[Core.Enums.ProviderType.MatrixOASIS]["event_id"] = ev.EventId ?? string.Empty;
            h.ProviderMetaData[Core.Enums.ProviderType.MatrixOASIS]["room_id"] = ev.RoomId ?? string.Empty;
            h.ProviderMetaData[Core.Enums.ProviderType.MatrixOASIS]["sender"] = ev.Sender ?? string.Empty;
            h.ProviderMetaData[Core.Enums.ProviderType.MatrixOASIS]["type"] = ev.Type ?? string.Empty;
            return h;
        }

        private static Guid DeriveGuid(string key)
        {
            if (Guid.TryParse(key, out var g)) return g;
            using var md5 = System.Security.Cryptography.MD5.Create();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(key)));
        }

        // ─── Matrix API DTOs ──────────────────────────────────────────────────────

        private class MatrixLoginResponse
        {
            public string AccessToken { get; set; }
            public string UserId { get; set; }
        }

        private class MatrixWhoami { public string UserId { get; set; } }

        private class MatrixProfile
        {
            public string Displayname { get; set; }
            public string AvatarUrl { get; set; }
        }

        private class MatrixJoinedRooms { public List<string> JoinedRooms { get; set; } }

        private class MatrixMessagesResponse
        {
            public List<MatrixEvent> Chunk { get; set; }
            public string End { get; set; }
        }

        private class MatrixEvent
        {
            public string EventId { get; set; }
            public string Type { get; set; }
            public string Sender { get; set; }
            public string RoomId { get; set; }
            public long OriginServerTs { get; set; }
            public JsonElement Content { get; set; }
        }

        private class MatrixSendResponse { public string EventId { get; set; } }

        private class MatrixSearchResponse
        {
            public MatrixSearchCategories SearchCategories { get; set; }
        }

        private class MatrixSearchCategories { public MatrixRoomEventResults RoomEvents { get; set; } }

        private class MatrixRoomEventResults { public List<MatrixSearchResult> Results { get; set; } }

        private class MatrixSearchResult { public MatrixEvent Result { get; set; } }

        private class MatrixLookupResponse { public List<MatrixThreePid> Threepids { get; set; } }

        private class MatrixThreePid { public string Mxid { get; set; } }
    }
}
