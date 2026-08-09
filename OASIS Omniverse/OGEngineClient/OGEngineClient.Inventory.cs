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

    /// <summary>End of a quest-worker fetch: clear in-flight flag, run a coalesced refresh if one was requested while busy.</summary>
    private void ReleaseQuestRefreshInProgressSlot(bool invokeQuestsCacheRefreshedCallback)
    {
        bool pending;
        lock (_questsCacheLock)
        {
            _questsRefreshInProgress = false;
            pending = _questsRefreshPending;
            _questsRefreshPending = false;
        }
        if (invokeQuestsCacheRefreshedCallback)
            OGEngineExports.StarApiLogFileOnly("[Quests] Cache refresh complete (native callback suppressed; UI reads cache by polling).");
        if (pending)
            RequestQuestCacheRefreshInBackground(forceRefetch: true);
    }

    /// <summary>Start a background refresh of the quest cache without clearing it. When the fetch completes, the cache is updated. Use when opening the quest popup so the UI shows the previous list immediately and updates when the callback returns.</summary>
    /// <param name="forceRefetch">When false, skips scheduling a network fetch if both structured and string caches are already populated (avoids GET all-for-avatar/game after every profile refresh while playing). Popup / <c>ogengine_refresh_quest_cache_in_background</c> should pass true. After <see cref="InvalidateQuestCache"/>, caches are null so a fetch still runs.</param>
    public void RequestQuestCacheRefreshInBackground(bool forceRefetch = true)
    {
        lock (_questsCacheLock)
        {
            if (_questsRefreshInProgress)
            {
                _questsRefreshPending = true;
                return;
            }
            if (!forceRefetch && _cachedQuestList != null && _questsCacheString != null)
                return;
            _questsRefreshInProgress = true;
        }
        _ = RunOnWorkerAsync(DedicatedWorker.Quests, async ct =>
        {
            var cacheUpdatedOk = false;
            try
            {
                var result = await GetAllQuestsForAvatarAsync(ct).ConfigureAwait(false);
                if (result.IsError)
                {
                    OGEngineExports.StarApiLog("[Quests] Refresh failed (all-for-avatar).");
                    OGEngineExports.StarApiLogFileOnly($"[Quests] Refresh failed: {result.Message ?? "unknown"}");
                    return FailAndCallback<bool>("Quest refresh failed.", StarApiResultCode.Network);
                }
                if (result.Result is null || result.Result.Count == 0)
                {
                    OGEngineExports.StarApiLogFileOnly("[Quests] Refresh OK (0 quests)");
                }
                else
                {
                    var list = result.Result;
                    int withObjectives = list.Count(q => q.Objectives != null && q.Objectives.Count > 0);
                    OGEngineExports.StarApiLogFileOnly($"[Quests] Cache refreshed: {list.Count} quests, {withObjectives} with objectives");
                }
                var serialized = result.Result is null || result.Result.Count == 0
                    ? string.Empty
                    : SerializeQuestsForGame(result.Result);
                Guid? activeForSnap;
                lock (_stateLock) { activeForSnap = _cachedActiveQuestId; }
                if (Volatile.Read(ref _questUiPopupOpen) != 0)
                {
                    LogTopLevelQuestPctSnapshotFromList("GET_all_for_avatar_DISCARDED_incoming_snapshot", result.Result, activeForSnap);
                    try { OGEngineExports.StarApiLogFileOnly("[Quests] GET all-for-avatar DISCARDED (quest popup open — cache unchanged)"); } catch { /* ignore */ }
                    return Success(true, StarApiResultCode.Success, "Quests refresh discarded (popup open).");
                }
                lock (_questsCacheLock)
                {
                    LogTopLevelQuestPctSnapshotUnderQuestLock("GET_all_for_avatar_before_assign", activeForSnap);
                    _questsCacheString = serialized;
                    _cachedQuestList = result.Result;
                    _questsFilterLastLogTop = (0, 0);
                    _questsFilterLastLogObjectives = ("", -1);
                    _questsFilterLastLogSubQuests = ("", -1);
                    _questsFilterLastLogPrereqs = ("", -1);
                    LogTopLevelQuestPctSnapshotUnderQuestLock("GET_all_for_avatar_after_assign", activeForSnap);
                }
                cacheUpdatedOk = true;
                return Success(true, StarApiResultCode.Success, "Quests cache refreshed.");
            }
            catch (Exception ex)
            {
                OGEngineExports.StarApiLogFileOnly($"[Quests] Refresh exception: {ex.Message}");
                return FailAndCallback<bool>("Quest refresh failed.", StarApiResultCode.Network);
            }
            finally
            {
                ReleaseQuestRefreshInProgressSlot(cacheUpdatedOk);
            }
        }, default);
    }

    /// <summary>Filter cached quest list to top-level only (no ParentQuestId or empty). Returns empty list if cache not ready.</summary>
    public List<StarQuestInfo> GetTopLevelQuestsFromCache()
    {
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return new List<StarQuestInfo>();
            return _cachedQuestList.Where(q => string.IsNullOrWhiteSpace(q.ParentQuestId) || q.ParentQuestId == Guid.Empty.ToString()).ToList();
        }
    }

    /// <summary>Get objectives for a parent quest from the quest's Objectives collection (Quest.Objectives). Returns one StarQuestInfo per objective so callers get a list; objectives are no longer separate child quests.</summary>
    public List<StarQuestInfo> GetQuestObjectivesFromCache(string parentQuestId)
    {
        if (string.IsNullOrWhiteSpace(parentQuestId)) return new List<StarQuestInfo>();
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return new List<StarQuestInfo>();
            var id = parentQuestId.Trim();
            var parent = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, id, StringComparison.OrdinalIgnoreCase));
            if (parent?.Objectives == null || parent.Objectives.Count == 0) return new List<StarQuestInfo>();
            var list = new List<StarQuestInfo>();
            for (var i = 0; i < parent.Objectives.Count; i++)
            {
                var o = parent.Objectives[i];
                var objTitle = GetObjectiveRawTitle(o, parent);
                var objBody = GetObjectiveRawDescription(o, objTitle);
                list.Add(new StarQuestInfo
                {
                    Id = string.IsNullOrEmpty(o.Id) ? $"obj_{i}" : o.Id,
                    Name = objTitle,
                    Description = objBody,
                    Status = o.IsCompleted ? "Completed" : "InProgress",
                    Order = o.Order,
                    GameSource = o.GameSource ?? string.Empty,
                    Objectives = new List<StarQuestObjective>(),
                    ParentQuestId = id,
                    LinkedGeoHotSpotId = o.LinkedGeoHotSpotId,
                    ExternalHandoffUri = o.ExternalHandoffUri,
                    Dictionaries = o.Dictionaries
                });
            }
            return list;
        }
    }

    /// <summary>Filter cached quest list to sub-quests (child quests with ParentQuestId set). Sub-quests are full nested quests; objectives are on Quest.Objectives.</summary>
    public List<StarQuestInfo> GetQuestSubQuestsFromCache(string parentQuestId)
    {
        if (string.IsNullOrWhiteSpace(parentQuestId)) return new List<StarQuestInfo>();
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return new List<StarQuestInfo>();
            var id = parentQuestId.Trim();
            return _cachedQuestList.Where(q => string.Equals(q.ParentQuestId, id, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    /// <summary>Resolve prerequisite quest IDs for the given quest to full StarQuestInfo from cache. Returns empty list if cache not ready or quest not found.</summary>
    public List<StarQuestInfo> GetQuestPrereqsFromCache(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId)) return new List<StarQuestInfo>();
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return new List<StarQuestInfo>();
            var quest = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, questId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (quest?.PrerequisiteQuestIds == null || quest.PrerequisiteQuestIds.Count == 0) return new List<StarQuestInfo>();
            var set = new HashSet<string>(quest.PrerequisiteQuestIds, StringComparer.OrdinalIgnoreCase);
            return _cachedQuestList.Where(q => set.Contains(q.Id)).ToList();
        }
    }

    /// <summary>Ensure quest cache is populated in the background. Called from ogengine_get_quests_string when cache is empty so the game thread never blocks.</summary>
    private void EnsureQuestsCacheInBackground()
    {
        lock (_questsCacheLock)
        {
            if (_questsCacheString != null || _questsRefreshInProgress)
                return;
            _questsRefreshInProgress = true;
        }
        OGEngineExports.StarApiLogFileOnly("[Quests] EnsureQuestsCacheInBackground started (fetching all-for-avatar)");
        _ = RunOnWorkerAsync(DedicatedWorker.Quests, async ct =>
        {
            var invokeUi = false;
            try
            {
                var result = await GetAllQuestsForAvatarAsync(ct).ConfigureAwait(false);
                string serialized;
                if (result.IsError)
                {
                    serialized = "Error: Error loading quests. Check console or ogengine.log for details.";
                    OGEngineExports.StarApiLog("[Quests] Load failed (all-for-avatar). See [HTTP] line above or ogengine.log.");
                    OGEngineExports.StarApiLogFileOnly($"[Quests] Load failed detail: {result.Message ?? "unknown"}");
                }
                else if (result.Result is null || result.Result.Count == 0)
                {
                    serialized = string.Empty;
                    OGEngineExports.StarApiLog("[Quests] OK (0 quests)");
                    invokeUi = true;
                }
                else
                {
                    serialized = SerializeQuestsForGame(result.Result);
                    var list = result.Result;
                    int withObjectives = list.Count(q => q.Objectives != null && q.Objectives.Count > 0);
                    OGEngineExports.StarApiLog($"[Quests] Cache updated: {list.Count} quests, {withObjectives} with objectives");
                    invokeUi = true;
                }
                Guid? activeEnsureSnap;
                lock (_stateLock) { activeEnsureSnap = _cachedActiveQuestId; }
                lock (_questsCacheLock)
                {
                    if (!result.IsError && result.Result is { Count: > 0 })
                        LogTopLevelQuestPctSnapshotUnderQuestLock("EnsureQuests_first_load_before_assign", activeEnsureSnap);
                    _questsCacheString = serialized;
                    _cachedQuestList = result.Result;
                    _questsFilterLastLogTop = (0, 0);
                    _questsFilterLastLogObjectives = ("", -1);
                    _questsFilterLastLogSubQuests = ("", -1);
                    _questsFilterLastLogPrereqs = ("", -1);
                    if (!result.IsError && result.Result is { Count: > 0 })
                        LogTopLevelQuestPctSnapshotUnderQuestLock("EnsureQuests_first_load_after_assign", activeEnsureSnap);
                }
                if (invokeUi && !result.IsError)
                    LogActiveQuestSnapshot("after_quest_list_cache_updated");
                return Success(true, StarApiResultCode.Success, "Quests cached.");
            }
            catch (Exception ex)
            {
                var serialized = "Error: Error loading quests. Check console or ogengine.log for details.";
                OGEngineExports.StarApiLog($"[Quests] Exception: {ex.Message}");
                lock (_questsCacheLock)
                {
                    _questsCacheString = serialized;
                    _cachedQuestList = null;
                    _questsFilterLastLogTop = (0, 0);
                    _questsFilterLastLogObjectives = ("", -1);
                    _questsFilterLastLogSubQuests = ("", -1);
                    _questsFilterLastLogPrereqs = ("", -1);
                }
                return FailAndCallback<bool>("Quest refresh failed.", StarApiResultCode.Network);
            }
            finally
            {
                ReleaseQuestRefreshInProgressSlot(invokeUi);
            }
        }, default);
    }

    /// <summary>Get current quest cache for native ogengine_get_quests_string. Returns cached string if available; otherwise starts background refresh and returns null (caller shows "Loading..."). Never blocks.</summary>
    internal bool TryGetQuestsCache(out string? cached)
    {
        lock (_questsCacheLock)
        {
            if (_questsCacheString != null)
            {
                cached = _questsCacheString;
                return true;
            }
        }
        EnsureQuestsCacheInBackground();
        cached = null;
        return false;
    }

    /// <summary>Get display name for the current tracked quest (ActiveQuestId) from cache. Returns null if cache not ready or quest not in list.</summary>
    internal string? TryGetTrackerQuestNameFromCache()
    {
        var questId = GetCachedActiveQuestId();
        if (!questId.HasValue || questId.Value == Guid.Empty) return null;
        var idStr = questId.Value.ToString();
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return null;
            var q = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, idStr, StringComparison.OrdinalIgnoreCase));
            return q?.Name;
        }
    }

    /// <summary>Get serialized top-level-only quest list for left panel. Filters from cache; returns null if cache not ready.</summary>
    internal bool TryGetTopLevelQuestsCache(out string? cached)
    {
        cached = null;
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _questsCacheString == null) { EnsureQuestsCacheInBackground(); return false; }
            /* Stable order by Id so the same quest always has the same index across reloads and cache refreshes (fixes popup "1 above" drift). */
            var top = _cachedQuestList
                .Where(q => string.IsNullOrWhiteSpace(q.ParentQuestId) || q.ParentQuestId == Guid.Empty.ToString())
                .OrderBy(q => q.Id ?? string.Empty, StringComparer.Ordinal)
                .ToList();
            var total = _cachedQuestList.Count;
            if (_questsFilterLastLogTop != (total, top.Count))
            {
                _questsFilterLastLogTop = (total, top.Count);
            }
            cached = top.Count == 0 ? string.Empty : SerializeQuestsForGame(top);
            return true;
        }
    }

    /// <summary>Get serialized objectives for a parent quest. We do NOT cache a single "right panel" list: every call is filtered by the requested parentQuestId.
    /// Data path: Game passes the selected quest id → TryGetQuestObjectivesCache(id) → find that quest in _cachedQuestList by Id → return that quest's Objectives only.
    /// If the main cache has 0 objectives for that quest, we start an on-demand fetch and merge the result into _cachedQuestList so the next call (next frame) returns them.</summary>
    internal bool TryGetQuestObjectivesCache(string? parentQuestId, out string? cached)
    {
        cached = null;
        if (string.IsNullOrWhiteSpace(parentQuestId)) { cached = string.Empty; return true; }
        var id = parentQuestId.Trim();
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _questsCacheString == null) { EnsureQuestsCacheInBackground(); return false; }
            // 1) Find the requested quest in the cache by Id (not by index – selection change always uses the new id). On-demand fetch merges objectives into this list.
            var parent = _cachedQuestList.Where(q => string.Equals(q.Id, id, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(q => q.Objectives?.Count ?? 0)
                .FirstOrDefault();
            if (parent != null && parent.Objectives != null && parent.Objectives.Count > 0)
            {
                cached = SerializeObjectivesAsQuestLines(parent);
                return true;
            }
            if (parent != null)
            {
                cached = SerializeObjectivesAsQuestLines(parent);
                return true;
            }
            cached = string.Empty;
            return true;
        }
    }

}
