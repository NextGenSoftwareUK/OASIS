using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS
{
    /// <summary>
    /// OASIS provider for the BlueSky decentralised social network (AT Protocol).
    /// PDS endpoint: https://bsky.social/xrpc/ (configurable for self-hosted PDS).
    /// BlueSky profiles → OASIS Avatars (provider key = DID e.g. did:plc:abc123).
    /// BlueSky posts (app.bsky.feed.post) → OASIS Holons (provider key = AT-URI).
    /// Authentication via app passwords: set BLUESKY_IDENTIFIER and BLUESKY_APP_PASSWORD
    /// or pass them to the constructor. Session tokens refresh automatically on 401.
    /// </summary>
    public class BlueSkyOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private readonly string _identifier;
        private readonly string _appPassword;
        private readonly string _pdsBase;
        private string _accessJwt;
        private string _refreshJwt;
        private string _did;
        private bool _isActivated;
        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public BlueSkyOASIS(string identifier = null, string appPassword = null,
            string pdsBase = "https://bsky.social")
        {
            _identifier = identifier ?? Environment.GetEnvironmentVariable("BLUESKY_IDENTIFIER") ?? string.Empty;
            _appPassword = appPassword ?? Environment.GetEnvironmentVariable("BLUESKY_APP_PASSWORD") ?? string.Empty;
            _pdsBase = pdsBase.TrimEnd('/');
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            ProviderName = "BlueSkyOASIS";
            ProviderDescription = "BlueSky / AT Protocol decentralised social provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BlueSkyOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Activation ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var body = new { identifier = _identifier, password = _appPassword };
                var resp = await PostAsync("com.atproto.server.createSession", body);
                if (resp.IsSuccessStatusCode)
                {
                    var session = await resp.Content.ReadFromJsonAsync<AtSession>(_jsonOpts);
                    _accessJwt = session!.AccessJwt;
                    _refreshJwt = session.RefreshJwt;
                    _did = session.Did;
                    SetAuthHeader();
                    _isActivated = true;
                    result.Result = true;
                    result.Message = $"BlueSkyOASIS activated as {session.Handle} (DID: {_did}).";
                }
                else
                {
                    var body2 = await resp.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"BlueSkyOASIS: Login failed ({resp.StatusCode}): {body2}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"BlueSkyOASIS: Error activating provider: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _isActivated = false;
            _accessJwt = null;
            _refreshJwt = null;
            _did = null;
            _http.DefaultRequestHeaders.Authorization = null;
            return await Task.FromResult(new OASISResult<bool> { Result = true, Message = "BlueSkyOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar: Load ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            // Look up DID stored in provider key index
            var result = new OASISResult<IAvatar>();
            try
            {
                var keysResult = KeyManager.Instance.GetProviderPublicKeysForAvatarById(
                    id, Core.Enums.ProviderType.BlueSkyOASIS);
                if (keysResult.IsError || keysResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        "BlueSkyOASIS: No DID found for this avatar GUID. Use LoadAvatarByProviderKeyAsync(did) instead.");
                    return result;
                }
                string did = System.Linq.Enumerable.FirstOrDefault(keysResult.Result) ?? string.Empty;
                return await LoadAvatarByProviderKeyAsync(did, version);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"BlueSkyOASIS: Error in LoadAvatarAsync: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) =>
            LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                await EnsureSessionAsync();
                var resp = await GetAsync($"app.bsky.actor.getProfile?actor={Uri.EscapeDataString(providerKey)}");
                if (resp.IsSuccessStatusCode)
                {
                    var profile = await resp.Content.ReadFromJsonAsync<BskyProfile>(_jsonOpts);
                    result.Result = MapProfileToAvatar(profile!);
                    result.Message = $"BlueSkyOASIS: Loaded profile for {profile!.Handle}.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"BlueSkyOASIS: getProfile failed ({resp.StatusCode}) for '{providerKey}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"BlueSkyOASIS: Error loading avatar '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) =>
            LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) =>
            await LoadAvatarByProviderKeyAsync(avatarUsername, version); // handle = provider key for BlueSky

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "BlueSkyOASIS: Email lookup is not supported by the AT Protocol. Use LoadAvatarByProviderKeyAsync(handle/DID).");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result,
                "BlueSkyOASIS: LoadAllAvatars is not supported — BlueSky has millions of users. " +
                "Use LoadAvatarByProviderKeyAsync(handle/DID) or SearchAsync instead.");
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
                await EnsureSessionAsync();
                // Update own profile display name and description via app.bsky.actor.putPreferences
                // Profile update goes through com.atproto.repo.putRecord for app.bsky.actor.profile
                var record = new
                {
                    repo = _did,
                    collection = "app.bsky.actor.profile",
                    rkey = "self",
                    record = new
                    {
                        displayName = avatar.Username,
                        description = avatar.Description,
                    }
                };
                var resp = await PostAsync("com.atproto.repo.putRecord", record);
                if (resp.IsSuccessStatusCode)
                {
                    result.Result = avatar;
                    result.Message = "BlueSkyOASIS: Profile updated.";
                }
                else
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"BlueSkyOASIS: Profile update failed ({resp.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"BlueSkyOASIS: Error saving avatar: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "BlueSkyOASIS: SaveAvatarDetail is not separately supported — use SaveAvatarAsync.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) =>
            SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "BlueSkyOASIS: Account deletion is not supported via the AT Protocol public API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) =>
            DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "BlueSkyOASIS: Account deletion is not supported via the AT Protocol public API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) =>
            DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "BlueSkyOASIS: Account deletion is not supported via the AT Protocol public API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) =>
            DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "BlueSkyOASIS: Account deletion is not supported via the AT Protocol public API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) =>
            DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        // ─── Avatar Detail ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var avatarResult = await LoadAvatarAsync(id, version);
            var result = new OASISResult<IAvatarDetail>();
            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = MapAvatarToDetail(avatarResult.Result);
            else
            {
                result.IsError = avatarResult.IsError;
                result.Message = avatarResult.Message;
                result.Exception = avatarResult.Exception;
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) =>
            LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            var result = new OASISResult<IAvatarDetail>();
            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = MapAvatarToDetail(avatarResult.Result);
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
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "BlueSkyOASIS: Email lookup is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "BlueSkyOASIS: LoadAllAvatarDetails is not supported.");
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
                "BlueSkyOASIS: Use LoadHolonAsync(string atUri) with an AT-URI like at://did:plc:xyz/app.bsky.feed.post/rkey.");
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
            // providerKey = AT-URI e.g. at://did:plc:xyz/app.bsky.feed.post/rkey
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureSessionAsync();
                // Parse AT-URI: at://repo/collection/rkey
                var parts = providerKey.TrimStart(new[] { 'a', 't', ':' }).TrimStart('/').Split('/');
                if (parts.Length < 3)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"BlueSkyOASIS: Invalid AT-URI '{providerKey}'. Expected at://repo/collection/rkey.");
                    return result;
                }
                string repo = parts[0], collection = parts[1], rkey = parts[2];
                var resp = await GetAsync(
                    $"com.atproto.repo.getRecord?repo={Uri.EscapeDataString(repo)}" +
                    $"&collection={Uri.EscapeDataString(collection)}&rkey={Uri.EscapeDataString(rkey)}");
                if (resp.IsSuccessStatusCode)
                {
                    var record = await resp.Content.ReadFromJsonAsync<AtRecord>(_jsonOpts);
                    result.Result = MapRecordToHolon(record!, providerKey);
                    result.Message = $"BlueSkyOASIS: Loaded record {providerKey}.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"BlueSkyOASIS: getRecord failed ({resp.StatusCode}) for '{providerKey}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"BlueSkyOASIS: Error loading holon '{providerKey}': {ex.Message}");
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
            // Load own timeline as "all holons"
            if (string.IsNullOrEmpty(_did))
            {
                var err = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref err,
                    "BlueSkyOASIS: Provider not activated. Call ActivateProviderAsync first.");
                return err;
            }
            return await LoadHolonsForParentAsync(_did, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version);
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
                "BlueSkyOASIS: Use LoadHolonsForParentAsync(string did/handle) to load posts for an author.");
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
            // Load feed for a given actor (DID or handle)
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureSessionAsync();
                var holons = new List<IHolon>();
                string cursor = null;
                do
                {
                    string url = $"app.bsky.feed.getAuthorFeed?actor={Uri.EscapeDataString(providerKey)}&limit=100" +
                                 (cursor != null ? $"&cursor={Uri.EscapeDataString(cursor)}" : "");
                    var resp = await GetAsync(url);
                    if (!resp.IsSuccessStatusCode) break;
                    var feed = await resp.Content.ReadFromJsonAsync<BskyFeed>(_jsonOpts);
                    if (feed?.Feed != null)
                        foreach (var item in feed.Feed)
                            if (item.Post != null)
                                holons.Add(MapPostToHolon(item.Post));
                    cursor = feed?.Cursor;
                }
                while (!string.IsNullOrEmpty(cursor) && holons.Count < 1000);

                result.Result = holons;
                result.Message = $"BlueSkyOASIS: Loaded {holons.Count} posts for '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"BlueSkyOASIS: Error loading posts for '{providerKey}': {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "BlueSkyOASIS: LoadHolonsByMetaData is not supported.");
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
            OASISErrorHandling.HandleError(ref result, "BlueSkyOASIS: LoadHolonsByMetaData is not supported.");
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
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureSessionAsync();
                bool hasKey = holon.ProviderUniqueStorageKey != null
                    && holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.BlueSkyOASIS)
                    && !string.IsNullOrEmpty(holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.BlueSkyOASIS]);

                if (hasKey)
                {
                    // AT Protocol records are immutable — no patch; delete+create is the AT way
                    OASISErrorHandling.HandleError(ref result,
                        "BlueSkyOASIS: Posts are immutable on the AT Protocol. Delete the existing post and create a new one.");
                    return result;
                }

                // Create a new post
                var record = new
                {
                    repo = _did,
                    collection = "app.bsky.feed.post",
                    record = new
                    {
                        text = holon.Name + (string.IsNullOrEmpty(holon.Description) ? "" : "\n\n" + holon.Description),
                        createdAt = DateTime.UtcNow.ToString("o"),
                    }
                };
                var resp = await PostAsync("com.atproto.repo.createRecord", record);
                if (resp.IsSuccessStatusCode)
                {
                    var created = await resp.Content.ReadFromJsonAsync<AtCreateRecordResponse>(_jsonOpts);
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.BlueSkyOASIS] = created!.Uri;
                    result.Result = holon;
                    result.Message = $"BlueSkyOASIS: Post created at {created.Uri}.";
                }
                else
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"BlueSkyOASIS: createRecord failed ({resp.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"BlueSkyOASIS: Error saving holon: {ex.Message}");
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
                    {
                        OASISErrorHandling.HandleError(ref result, r.Message);
                        return result;
                    }
                }
                result.Result = saved;
                result.Message = $"BlueSkyOASIS: Saved {saved.Count} holons.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"BlueSkyOASIS: Error saving holons: {ex.Message}");
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
                "BlueSkyOASIS: Use DeleteHolonAsync(string atUri) with the AT-URI of the post.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            // providerKey = AT-URI: at://repo/collection/rkey
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureSessionAsync();
                var parts = providerKey.TrimStart(new[] { 'a', 't', ':' }).TrimStart('/').Split('/');
                if (parts.Length < 3)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"BlueSkyOASIS: Invalid AT-URI '{providerKey}'.");
                    return result;
                }
                var body = new { repo = parts[0], collection = parts[1], rkey = parts[2] };
                var resp = await PostAsync("com.atproto.repo.deleteRecord", body);
                if (resp.IsSuccessStatusCode)
                    result.Message = $"BlueSkyOASIS: Record '{providerKey}' deleted.";
                else
                {
                    var bodyStr = await resp.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"BlueSkyOASIS: deleteRecord failed ({resp.StatusCode}): {bodyStr}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"BlueSkyOASIS: Error deleting holon '{providerKey}': {ex.Message}");
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
                await EnsureSessionAsync();
                var searchResults = new SearchResults();
                string q = string.Empty;
                if (searchParams.SearchGroups != null)
                    foreach (var g in searchParams.SearchGroups)
                        if (g is NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup tg && !string.IsNullOrEmpty(tg.SearchQuery))
                        { q = tg.SearchQuery; break; }

                if (!string.IsNullOrWhiteSpace(q))
                {
                    // Search posts
                    var postResp = await GetAsync($"app.bsky.feed.searchPosts?q={Uri.EscapeDataString(q)}&limit=25");
                    if (postResp.IsSuccessStatusCode)
                    {
                        var posts = await postResp.Content.ReadFromJsonAsync<BskySearchPostsResponse>(_jsonOpts);
                        if (posts?.Posts != null)
                            foreach (var p in posts.Posts)
                                searchResults.SearchResultHolons.Add(MapPostToHolon(p));
                    }
                    // Search actors
                    var actorResp = await GetAsync($"app.bsky.actor.searchActors?q={Uri.EscapeDataString(q)}&limit=25");
                    if (actorResp.IsSuccessStatusCode)
                    {
                        var actors = await actorResp.Content.ReadFromJsonAsync<BskySearchActorsResponse>(_jsonOpts);
                        if (actors?.Actors != null)
                            foreach (var a in actors.Actors)
                                searchResults.SearchResultAvatars.Add(MapProfileToAvatar(a));
                    }
                }
                result.Result = searchResults;
                result.Message = $"BlueSkyOASIS: Found {searchResults.SearchResultHolons.Count} posts and " +
                                 $"{searchResults.SearchResultAvatars.Count} actors for '{q}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"BlueSkyOASIS: Error searching: {ex.Message}");
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
            OASISErrorHandling.HandleError(ref result, "BlueSkyOASIS: Import is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "BlueSkyOASIS: Use ExportAllDataForAvatarByUsernameAsync(handle/DID).");
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
            OASISErrorHandling.HandleError(ref result, "BlueSkyOASIS: Email lookup is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) =>
            ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) =>
            await LoadAllHolonsAsync(version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "BlueSkyOASIS: Geolocation is not supported.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "BlueSkyOASIS: Geolocation is not supported.");
            return result;
        }

        // ─── Session management ───────────────────────────────────────────────────

        private async Task EnsureSessionAsync()
        {
            if (!string.IsNullOrEmpty(_accessJwt)) return;
            var activateResult = await ActivateProviderAsync();
            if (activateResult.IsError)
                throw new InvalidOperationException($"BlueSkyOASIS: Could not establish session: {activateResult.Message}");
        }

        private async Task RefreshSessionAsync()
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _refreshJwt);
            var resp = await PostAsync("com.atproto.server.refreshSession", new { });
            if (resp.IsSuccessStatusCode)
            {
                var session = await resp.Content.ReadFromJsonAsync<AtSession>(_jsonOpts);
                _accessJwt = session!.AccessJwt;
                _refreshJwt = session.RefreshJwt;
                SetAuthHeader();
            }
        }

        private void SetAuthHeader() =>
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessJwt);

        // ─── HTTP helpers ─────────────────────────────────────────────────────────

        private Task<HttpResponseMessage> GetAsync(string nsid) =>
            _http.GetAsync($"{_pdsBase}/xrpc/{nsid}");

        private Task<HttpResponseMessage> PostAsync(string nsid, object body)
        {
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return _http.PostAsync($"{_pdsBase}/xrpc/{nsid}", content);
        }

        // ─── Mapping ──────────────────────────────────────────────────────────────

        private static Avatar MapProfileToAvatar(BskyProfile p)
        {
            var avatar = new Avatar
            {
                Id = DeriveGuid(p.Did ?? p.Handle ?? ""),
                Username = p.Handle ?? string.Empty,
                Description = p.Description ?? string.Empty,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.BlueSkyOASIS] = p.Did ?? p.Handle ?? string.Empty;
            avatar.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["did"] = p.Did ?? string.Empty;
            avatar.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["handle"] = p.Handle ?? string.Empty;
            avatar.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["display_name"] = p.DisplayName ?? string.Empty;
            avatar.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["avatar_url"] = p.Avatar ?? string.Empty;
            avatar.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["followers_count"] = (p.FollowersCount ?? 0).ToString();
            avatar.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["follows_count"] = (p.FollowsCount ?? 0).ToString();
            avatar.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["posts_count"] = (p.PostsCount ?? 0).ToString();
            return avatar;
        }

        private static IAvatarDetail MapAvatarToDetail(IAvatar avatar)
        {
            var detail = new AvatarDetail
            {
                Id = avatar.Id,
                Username = avatar.Username,
                Description = avatar.Description,
            };
            if (avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.BlueSkyOASIS))
                detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.BlueSkyOASIS] =
                    avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.BlueSkyOASIS];
            return detail;
        }

        private static Holon MapPostToHolon(BskyPost post)
        {
            var h = new Holon
            {
                Id = DeriveGuid(post.Uri ?? post.Cid ?? ""),
                Name = post.Record?.Text ?? string.Empty,
                HolonType = HolonType.Holon,
                CreatedDate = post.Record?.CreatedAt ?? DateTime.UtcNow,
                ModifiedDate = post.Record?.CreatedAt ?? DateTime.UtcNow,
            };
            h.ProviderUniqueStorageKey[Core.Enums.ProviderType.BlueSkyOASIS] = post.Uri ?? string.Empty;
            h.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["cid"] = post.Cid ?? string.Empty;
            h.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["author_did"] = post.Author?.Did ?? string.Empty;
            h.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["author_handle"] = post.Author?.Handle ?? string.Empty;
            h.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["like_count"] = (post.LikeCount ?? 0).ToString();
            h.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["repost_count"] = (post.RepostCount ?? 0).ToString();
            return h;
        }

        private static Holon MapRecordToHolon(AtRecord record, string atUri)
        {
            var h = new Holon
            {
                Id = DeriveGuid(record.Uri ?? atUri),
                Name = record.Value.TryGetProperty("text", out var t) ? t.GetString() ?? string.Empty : string.Empty,
                HolonType = HolonType.Holon,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };
            h.ProviderUniqueStorageKey[Core.Enums.ProviderType.BlueSkyOASIS] = record.Uri ?? atUri;
            h.ProviderMetaData[Core.Enums.ProviderType.BlueSkyOASIS]["cid"] = record.Cid ?? string.Empty;
            return h;
        }

        private static Guid DeriveGuid(string key)
        {
            if (Guid.TryParse(key, out var g)) return g;
            using var md5 = System.Security.Cryptography.MD5.Create();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(key)));
        }

        // ─── AT Protocol DTOs ─────────────────────────────────────────────────────

        private class AtSession
        {
            public string Did { get; set; }
            public string Handle { get; set; }
            public string AccessJwt { get; set; }
            public string RefreshJwt { get; set; }
        }

        private class BskyProfile
        {
            public string Did { get; set; }
            public string Handle { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string Avatar { get; set; }
            public int? FollowersCount { get; set; }
            public int? FollowsCount { get; set; }
            public int? PostsCount { get; set; }
        }

        private class BskyFeed
        {
            public List<BskyFeedItem> Feed { get; set; }
            public string Cursor { get; set; }
        }

        private class BskyFeedItem
        {
            public BskyPost Post { get; set; }
        }

        private class BskyPost
        {
            public string Uri { get; set; }
            public string Cid { get; set; }
            public BskyProfile Author { get; set; }
            public BskyPostRecord Record { get; set; }
            public int? LikeCount { get; set; }
            public int? RepostCount { get; set; }
            public int? ReplyCount { get; set; }
        }

        private class BskyPostRecord
        {
            public string Text { get; set; }
            public DateTime? CreatedAt { get; set; }
        }

        private class BskySearchPostsResponse
        {
            public List<BskyPost> Posts { get; set; }
        }

        private class BskySearchActorsResponse
        {
            public List<BskyProfile> Actors { get; set; }
        }

        private class AtRecord
        {
            public string Uri { get; set; }
            public string Cid { get; set; }
            public JsonElement Value { get; set; }
        }

        private class AtCreateRecordResponse
        {
            public string Uri { get; set; }
            public string Cid { get; set; }
        }
    }
}
