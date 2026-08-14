using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Contracts;

namespace NextGenSoftware.OASIS.STARAPI.Client;
public sealed partial class OGEngineClient
{

    /// <summary>Username of the currently logged-in avatar (for persistence to oasisstar.json). Empty if not logged in.</summary>
    public string? GetCurrentUsername()
    {
        lock (_stateLock) return _loggedInUsername;
    }

    /// <summary>Current JWT (for persistence to oasisstar.json). Empty if not logged in. Caller should not log or display.</summary>
    public string? GetCurrentJwt()
    {
        lock (_stateLock) return _jwtToken;
    }

    public OASISResult<bool> SetWeb4OasisApiBaseUrl(string web4OasisApiBaseUrl)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(web4OasisApiBaseUrl) || !Uri.TryCreate(web4OasisApiBaseUrl.TrimEnd('/'), UriKind.Absolute, out var uri))
            return FailAndCallback<bool>("A valid OASIS WEB4 API base URL is required.", StarApiResultCode.InvalidParam);

        var normalized = uri.ToString().TrimEnd('/');
        var apiIndex = normalized.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
        if (apiIndex >= 0)
            normalized = normalized[..apiIndex];

        lock (_stateLock)
            _oasisBaseUrl = normalized;

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "WEB4 OASIS API base URL updated.");
    }

    public OASISResult<bool> SetWeb5StarApiBaseUrl(string web5StarApiBaseUrl)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(web5StarApiBaseUrl) || !Uri.TryCreate(web5StarApiBaseUrl.TrimEnd('/'), UriKind.Absolute, out var uri))
            return FailAndCallback<bool>("A valid WEB5 STAR API base URL is required.", StarApiResultCode.InvalidParam);

        var normalized = uri.ToString().TrimEnd('/');
        lock (_stateLock)
        {
            _baseApiUrl = normalized;
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "WEB5 STAR API base URL updated.");
    }

    public async Task<OASISResult<StarAvatarProfile>> GetCurrentAvatarAsync(CancellationToken cancellationToken = default, bool invokeCallback = true)
    {
        if (!IsInitialized())
            return invokeCallback ? FailAndCallback<StarAvatarProfile>("Client is not initialized.", StarApiResultCode.NotInitialized) : Fail<StarAvatarProfile>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
            return invokeCallback ? FailAndCallback<StarAvatarProfile>(missingWeb4, StarApiResultCode.InvalidParam) : Fail<StarAvatarProfile>(missingWeb4, StarApiResultCode.InvalidParam);

        var url = $"{web4Base}{Web4GetLoggedInAvatarWithXpPath}";
        OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 get-logged-in-avatar-with-xp url={url}");
        var response = await SendRawWithRetryAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            /* Do NOT return Success with a stub profile when GET fails: game would get "profile loaded" but cache has no XP/quest (causes 0 XP in Quake). Always return Fail so callback is not invoked with Success. */
            OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 avatar profile FAILED: IsError=True Message={response.Message ?? "null"} (returning Fail, not stub)");
            return invokeCallback ? FailAndCallback<StarAvatarProfile>(response.Message ?? "Request failed.", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception) : Fail<StarAvatarProfile>(response.Message ?? "Request failed.", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var len = response.Result?.Length ?? 0;
        if (OGEngineExports.GetStarDebug())
        {
            var responsePreview = len > 0
                ? (len <= 500 ? response.Result! : response.Result!.Substring(0, 500) + "...")
                : "(empty)";
            OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 avatar profile response OK len={len} preview={responsePreview}");
        }
        else
            OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 avatar profile response OK len={len}");

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 avatar profile parse failed: {parseErrorMessage}");
            return invokeCallback ? FailAndCallback<StarAvatarProfile>(parseErrorMessage, parseErrorCode) : Fail<StarAvatarProfile>(parseErrorMessage, parseErrorCode);
        }

        var avatar = ParseAvatarProfile(resultElement, response.Result);
        if (avatar is null || avatar.Id == Guid.Empty)
        {
            OGEngineExports.StarApiLogFileOnly("[Avatar] GET WEB4 avatar profile parse failed: no avatar in response");
            return invokeCallback ? FailAndCallback<StarAvatarProfile>("Could not parse current avatar profile.", StarApiResultCode.ApiError) : Fail<StarAvatarProfile>("Could not parse current avatar profile.", StarApiResultCode.ApiError);
        }

        lock (_stateLock)
        {
            _avatarId = avatar.Id.ToString();
            _cachedAvatarXp = avatar.XP;
            Volatile.Write(ref _cachedAvatarKarma, avatar.Karma);
            /* If user saved a quest/objective after this GET was started, do not let stale response overwrite their choice (fixes "wrong quest" on load). */
            if (!_questTrackerSavedSinceLastGet)
            {
                _cachedActiveQuestId = avatar.ActiveQuestId;
                _cachedActiveObjectiveId = avatar.ActiveObjectiveId;
            }
            else
            {
                _questTrackerSavedSinceLastGet = false;
                try { OGEngineExports.StarApiLogFileOnly($"[Quest] GET WEB4 avatar profile: ignoring quest/objective in response (user saved since GET started; keeping cache)"); } catch { /* ignore */ }
            }
        }

        lock (_stateLock) { if (!string.IsNullOrWhiteSpace(avatar.Username)) _loggedInUsername = avatar.Username; }
        Guid? loadQuestId;
        Guid? loadObjectiveId;
        lock (_stateLock) { loadQuestId = _cachedActiveQuestId; loadObjectiveId = _cachedActiveObjectiveId; }
        OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 avatar profile OK: XP={avatar.XP} ActiveQuestId={loadQuestId} ActiveObjectiveId={loadObjectiveId} (cache updated)");
        var (loadQuestName, loadObjName) = TryGetQuestAndObjectiveNamesFromCache(loadQuestId, loadObjectiveId);
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] LOAD questId={loadQuestId} objectiveId={loadObjectiveId} questName={loadQuestName ?? "(not in cache)"} objectiveName={loadObjName ?? "(not in cache)"}"); } catch { /* ignore */ }
        LogActiveQuestSnapshot("after_web4_avatar_profile_loaded");
        if (OGEngineExports.GetStarDebug())
        {
            try
            {
                if (loadQuestId.HasValue || loadObjectiveId.HasValue)
                    OGEngineExports.StarApiLog($"[Avatar] Profile loaded: quest={loadQuestId} objective={loadObjectiveId} (tracker can restore)");
                else
                    OGEngineExports.StarApiLog("[Avatar] Profile loaded: no ActiveQuestId/ActiveObjectiveId (tracker will stay clear)");
            }
            catch { /* ignore */ }
        }
        if (invokeCallback) InvokeCallback(StarApiResultCode.Success);
        return Success(avatar, StarApiResultCode.Success, "Current avatar loaded.");
    }

    /// <summary>Run get-current-avatar on the profile worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarAvatarProfile>> QueueGetCurrentAvatarAsync(CancellationToken cancellationToken = default) =>
        RunOnWorkerAsync(DedicatedWorker.Profile, ct => GetCurrentAvatarAsync(ct), cancellationToken);

    public OASISResult<bool> Cleanup()
    {
        StopWorkers();

        lock (_stateLock)
        {
            _restoreSessionInFlight = null;
            _httpClient?.Dispose();
            _httpClient = null;
            _initialized = false;
            _jwtToken = null;
            _refreshToken = null;
            _avatarId = null;
            _lastError = string.Empty;
            _loggedInUsername = null;
            _cachedActiveQuestId = null;
            _cachedActiveObjectiveId = null;
            _questTrackerSavedSinceLastGet = false;
            Volatile.Write(ref _cachedAvatarXp, 0);
        }

        return Success(true, StarApiResultCode.Success, "WEB5 STAR API client cleaned up.");
    }

    /// <summary>Check if the avatar has an item by name. Uses local cache first; only hits the API when cache is null (e.g. first load).</summary>
    public async Task<OASISResult<bool>> HasItemAsync(string itemName, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<bool>("Item name is required.", StarApiResultCode.InvalidParam);

        static string NormalizeKeyName(string s) =>
            string.IsNullOrWhiteSpace(s) ? string.Empty : s.Replace('_', ' ').Trim();

        var matches = (string a, string b) =>
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            var na = NormalizeKeyName(a);
            var nb = NormalizeKeyName(b);
            return string.Equals(na, nb, StringComparison.OrdinalIgnoreCase);
        };

        // Fuzzy match for keycards: e.g. "Red Keycard" matches any item whose name contains "red" and "key"
        static bool FuzzyKeycardMatch(string itemNameQuery, string inventoryName)
        {
            if (string.IsNullOrWhiteSpace(inventoryName)) return false;
            var n = NormalizeKeyName(inventoryName);
            var q = NormalizeKeyName(itemNameQuery);
            var ni = n.ToLowerInvariant();
            var qi = q.ToLowerInvariant();
            if (qi.Contains("red") && (qi.Contains("key") || qi.Contains("keycard")))
                return ni.Contains("red") && (ni.Contains("key") || ni.Contains("keycard"));
            if (qi.Contains("blue") && (qi.Contains("key") || qi.Contains("keycard")))
                return ni.Contains("blue") && (ni.Contains("key") || ni.Contains("keycard"));
            if (qi.Contains("yellow") && (qi.Contains("key") || qi.Contains("keycard")))
                return ni.Contains("yellow") && (ni.Contains("key") || ni.Contains("keycard"));
            if (qi.Contains("skull") && qi.Contains("key"))
                return ni.Contains("skull") && ni.Contains("key");
            if (qi.Contains("gold") && qi.Contains("key"))
                return ni.Contains("gold") && (ni.Contains("key") || ni.Contains("keycard"));
            if (qi.Contains("silver") && qi.Contains("key"))
                return ni.Contains("silver") && (ni.Contains("key") || ni.Contains("keycard"));
            return false;
        }

        bool hasItem(IEnumerable<StarItem> items) =>
            items.Any(x => matches(x.Name, itemName) || matches(x.Description, itemName) || FuzzyKeycardMatch(itemName, x.Name) || FuzzyKeycardMatch(itemName, x.Description));

        lock (_inventoryCacheLock)
        {
            if (_cachedInventory is not null)
            {
                var merged = MergeLocalPendingIntoInventory(_cachedInventory);
                var hasItemResult = hasItem(merged);
                InvokeCallback(StarApiResultCode.Success);
                return Success(hasItemResult, StarApiResultCode.Success, hasItemResult ? "Item found in inventory (cached)." : "Item not found in inventory.");
            }
        }

        var inventory = await GetInventoryAsync(cancellationToken).ConfigureAwait(false);
        if (inventory.IsError)
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = inventory.Message,
                ErrorCode = inventory.ErrorCode,
                Exception = inventory.Exception
            };
        }

        var found = hasItem(inventory.Result!);

        InvokeCallback(StarApiResultCode.Success);
        return Success(found, StarApiResultCode.Success, found ? "Item found in inventory." : "Item not found in inventory.");
    }

    /// <summary>Run has-item on the inventory worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueHasItemAsync(string itemName, CancellationToken cancellationToken = default) =>
        RunOnWorkerAsync(DedicatedWorker.Inventory, ct => HasItemAsync(itemName, ct), cancellationToken);

    /// <summary>Get avatar inventory. Returns cache (or fetches) then merges with local pickup deltas so one row per type = API qty + pending. Single-flight fetch when cache is null.</summary>
    public async Task<OASISResult<List<StarItem>>> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarItem>>("Client is not initialized.", StarApiResultCode.NotInitialized);

        Task<OASISResult<List<StarItem>>>? task;
        lock (_inventoryCacheLock)
        {
            if (_cachedInventory is not null)
            {
                var merged = MergeLocalPendingIntoInventory(_cachedInventory);
                InvokeCallback(StarApiResultCode.Success);
                return Success(merged, StarApiResultCode.Success, $"Loaded {merged.Count} item(s) (cached + pending).");
            }
            if (_inventoryFetchTask is null)
                _inventoryFetchTask = FetchInventoryOnceAsync();
            task = _inventoryFetchTask;
        }

        var result = await task.ConfigureAwait(false);
        lock (_inventoryCacheLock)
        {
            _inventoryFetchTask = null;
            if (result.Result is not null)
            {
                var fetched = result.Result;
                /* Don't replace a non-empty cache with an empty fetch: avoids keys/items vanishing when a refetch (e.g. after sync) returns empty due to timing or API. */
                if (fetched.Count == 0 && _cachedInventory is not null && _cachedInventory.Count > 0)
                {
                    var merged = MergeLocalPendingIntoInventory(_cachedInventory);
                    return Success(merged, StarApiResultCode.Success, $"Loaded {merged.Count} item(s) (cached + pending, kept prior cache).");
                }
                _cachedInventory = new List<StarItem>(fetched);
            }
        }
        if (result.Result is not null)
        {
            var merged = MergeLocalPendingIntoInventory(result.Result);
            return Success(merged, StarApiResultCode.Success, result.Message ?? $"Loaded {merged.Count} item(s).");
        }
        return result;
    }

    /// <summary>Run get-inventory on the inventory worker so the calling thread does not block.</summary>
    public Task<OASISResult<List<StarItem>>> QueueGetInventoryAsync(CancellationToken cancellationToken = default) =>
        RunOnWorkerAsync(DedicatedWorker.Inventory, ct => GetInventoryAsync(ct), cancellationToken);

    /// <summary>Return current inventory from cache only (merged with pending). No network. Returns null if cache not populated yet.</summary>
    public List<StarItem>? TryGetCachedInventory()
    {
        lock (_inventoryCacheLock)
        {
            if (_cachedInventory is null)
                return null;
            return MergeLocalPendingIntoInventory(_cachedInventory);
        }
    }

    /// <summary>Request inventory fetch in background. When done, operation_callback is invoked with StarApiOpGetInventory. Non-blocking.</summary>
    public void RequestInventoryInBackground()
    {
        if (!IsInitialized())
        {
            // Defer callback so the export returns immediately; avoids blocking/hang when not beamed in (no re-entrant C# from native callback).
            _ = Task.Run(() => OGEngineExports.InvokeOperationCallback(StarApiResultCode.NotInitialized, OGEngineExports.StarApiOpGetInventory));
            return;
        }
        _ = QueueGetInventoryAsync().ContinueWith((Task<OASISResult<List<StarItem>>> task) =>
        {
            var result = task.IsCompletedSuccessfully ? task.Result : new OASISResult<List<StarItem>> { IsError = true, Message = task.Exception?.Message ?? "Inventory fetch failed." };
            var code = result.IsError ? ParseCode(result.ErrorCode, StarApiResultCode.ApiError) : StarApiResultCode.Success;
            OGEngineExports.InvokeOperationCallback(code, OGEngineExports.StarApiOpGetInventory);
        }, TaskContinuationOptions.None);
    }

    /// <summary>Merge API list with local pending: one row per type, qty = API qty + pending for that name. Types only in pending get a new row.</summary>
    private List<StarItem> MergeLocalPendingIntoInventory(List<StarItem> apiList)
    {
        Dictionary<string, LocalPendingEntry> snapshot;
        lock (_localPendingLock)
        {
            snapshot = new Dictionary<string, LocalPendingEntry>(_localPending, StringComparer.OrdinalIgnoreCase);
        }
        if (snapshot.Count == 0)
            return new List<StarItem>(apiList);

        var nameToPending = snapshot;
        var merged = new List<StarItem>(apiList.Count + nameToPending.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in apiList)
        {
            seen.Add(item.Name);
            var extra = nameToPending.TryGetValue(item.Name, out var pe) ? pe.Quantity : 0;
            merged.Add(new StarItem
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                GameSource = item.GameSource,
                ItemType = item.ItemType,
                NftId = item.NftId,
                Quantity = Math.Max(1, item.Quantity + extra)
            });
        }
        foreach (var kv in nameToPending)
        {
            if (seen.Contains(kv.Key))
                continue;
            merged.Add(new StarItem
            {
                Id = Guid.Empty,
                Name = kv.Value.Name,
                Description = kv.Value.Description,
                GameSource = kv.Value.GameSource,
                ItemType = kv.Value.ItemType,
                NftId = kv.Value.NftId ?? string.Empty,
                Quantity = Math.Max(1, kv.Value.Quantity)
            });
        }
        return merged;
    }

    private async Task<OASISResult<List<StarItem>>> FetchInventoryOnceAsync()
    {
        var avatarIdResult = await EnsureAvatarIdAsync(CancellationToken.None).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
        {
            return new OASISResult<List<StarItem>>
            {
                IsError = true,
                Message = avatarIdResult.Message,
                ErrorCode = avatarIdResult.ErrorCode,
                Exception = avatarIdResult.Exception
            };
        }

        try
        {
            if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
                return FailAndCallback<List<StarItem>>(missingWeb4, StarApiResultCode.InvalidParam);

            var response = await SendRawWithRetryAsync(HttpMethod.Get, $"{web4Base}/api/avatar/inventory", null, CancellationToken.None).ConfigureAwait(false);
            if (response.IsError)
            {
                return FailAndCallback<List<StarItem>>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
            }

            var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
            if (!parseResult)
            {
                return FailAndCallback<List<StarItem>>(parseErrorMessage, parseErrorCode);
            }

            var mapped = ParseInventoryItems(resultElement);
            InvokeCallback(StarApiResultCode.Success);
            return Success(mapped, StarApiResultCode.Success, $"Loaded {mapped.Count} item(s).");
        }
        catch (Exception ex)
        {
            return FailAndCallback<List<StarItem>>($"Failed to load inventory: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    /// <summary>Clear the local inventory cache. Next GetInventory/HasItem will hit the API. Call after external inventory changes if needed.</summary>
    public void InvalidateInventoryCache()
    {
        lock (_inventoryCacheLock)
        {
            _cachedInventory = null;
            _inventoryFetchTask = null;
        }
    }

    /// <summary>Clear all client caches (e.g. inventory, quests). Next GetInventory/HasItem/GetQuests will hit the API.</summary>
    public void ClearCache()
    {
        InvalidateInventoryCache();
        InvalidateQuestCache();
    }

    /// <summary>Clear the local quest cache. Next ogengine_get_quests_string will trigger a background refresh. Call after completing objectives if you want the popup to show fresh data.</summary>
    public void InvalidateQuestCache()
    {
        lock (_questsCacheLock)
        {
            _questsCacheString = null;
            _cachedQuestList = null;
            _questsFilterLastLogTop = (0, 0);
            _questsFilterLastLogObjectives = ("", -1);
            _questsFilterLastLogSubQuests = ("", -1);
            _questsFilterLastLogPrereqs = ("", -1);
            _questObjectivesHydrating.Clear();
        }
    }

}
