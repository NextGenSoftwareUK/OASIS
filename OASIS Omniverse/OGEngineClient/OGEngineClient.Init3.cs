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

    /// <summary>Replace the quest in _cachedQuestList with a copy that has the given objectives, so the next TryGetQuestObjectivesCache lookup returns them. Caller must hold _questsCacheLock. Increments _questObjectivesCacheVersion so UI can re-read.</summary>
    private void MergeObjectivesIntoCachedQuest(string questId, List<StarQuestObjective> objectives)
    {
        if (_cachedQuestList == null || objectives == null || objectives.Count == 0) return;
        for (var i = 0; i < _cachedQuestList.Count; i++)
        {
            var q = _cachedQuestList[i];
            if (!string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase)) continue;
            var updated = new StarQuestInfo
            {
                Id = q.Id,
                Name = q.Name,
                Description = q.Description,
                Status = q.Status,
                Order = q.Order,
                GameSource = q.GameSource ?? string.Empty,
                Requirements = q.Requirements ?? new List<string>(),
                RewardKarma = q.RewardKarma,
                RewardXP = q.RewardXP,
                CompletionNotes = q.CompletionNotes,
                ParentMissionId = q.ParentMissionId ?? string.Empty,
                ParentQuestId = q.ParentQuestId ?? string.Empty,
                Objectives = objectives,
                PrerequisiteQuestIds = q.PrerequisiteQuestIds ?? new List<string>(),
                LinkedGeoHotSpotId = q.LinkedGeoHotSpotId,
                ExternalHandoffUri = q.ExternalHandoffUri,
                Dictionaries = q.Dictionaries
            };
            _cachedQuestList[i] = updated;
            _questObjectivesCacheVersion++;
            OGEngineExports.StarApiLogFileOnly($"[Quests] Merged {objectives.Count} objectives into cached quest {questId}; cache version now {_questObjectivesCacheVersion}. UI should re-call get_quest_objectives_string to refresh.");
            break;
        }
    }

    /// <summary>Objectives cache version; increments when on-demand fetch merges objectives. UI polls this each frame and re-calls get_quest_objectives_string when it changes so the list refreshes.</summary>
    internal int GetQuestObjectivesCacheVersion()
    {
        lock (_questsCacheLock) { return _questObjectivesCacheVersion; }
    }

    /// <summary>Fetch a single quest by id and return its Objectives (for on-demand fill when all-for-avatar had 0).</summary>
    private async Task<List<StarQuestObjective>?> FetchSingleQuestObjectivesAsync(string questId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrEmpty(_baseApiUrl)) return null;
        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/quests/{questId}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError || string.IsNullOrWhiteSpace(response.Result)) return null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out _, out _);
        if (!parseResult || resultElement.ValueKind != JsonValueKind.Object) return null;
        var src = $"GET.api.quests/{questId.Trim()}";
        LogQuestParseChunkedFileOnly($"[Quest][Parse] source={src} full HTTP body", response.Result);
        LogQuestJsonShapeFileOnly($"[Quest][Parse] source={src} envelope object", resultElement);
        var quest = ParseSingleQuestInfo(resultElement);
        LogParsedSingleQuestModelAudit(src, quest);
        return quest?.Objectives;
    }

    /// <summary>Get serialized sub-quests (child quests with ParentQuestId set) for a parent quest for right panel. Objectives are on Quest.Objectives, not in this list.</summary>
    internal bool TryGetQuestSubQuestsCache(string? parentQuestId, out string? cached)
    {
        cached = null;
        if (string.IsNullOrWhiteSpace(parentQuestId)) { cached = string.Empty; return true; }
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _questsCacheString == null) { EnsureQuestsCacheInBackground(); return false; }
            var id = parentQuestId.Trim();
            var sub = _cachedQuestList.Where(q => string.Equals(q.ParentQuestId, id, StringComparison.OrdinalIgnoreCase)).ToList();
            if (_questsFilterLastLogSubQuests != (id, sub.Count))
            {
                _questsFilterLastLogSubQuests = (id, sub.Count);
            }
            cached = sub.Count == 0 ? string.Empty : SerializeQuestsForGame(sub);
            return true;
        }
    }

    /// <summary>Prefer runtime client game, then objective/quest source, then last progress POST game key.</summary>
    private static string? ResolvePreferredGameKeyForQuestUi(string? clientGs, string? objectiveGameSource, string? questGameSource, string? lastProgressGs)
    {
        foreach (var c in new[] { clientGs, objectiveGameSource, questGameSource, lastProgressGs })
        {
            if (!string.IsNullOrWhiteSpace(c)) return c.Trim();
        }
        return null;
    }

    /// <summary>Requirement payloads often interleave labels (e.g. monster names) with counts; use the first positive integer in the list as the required tally.</summary>
    private static int GetFirstPositiveIntFromStringList(List<string>? list)
    {
        if (list == null) return 0;
        foreach (var s in list)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0)
                return n;
        }
        return 0;
    }

    /// <summary>Progress lists usually store the tally in the first parseable non-negative integer.</summary>
    private static int GetFirstNonNegativeIntFromStringList(List<string>? list)
    {
        if (list == null) return 0;
        foreach (var s in list)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 0)
                return n;
        }
        return 0;
    }

    /// <summary>When API/DB only has ONODE-style <c>Objective</c> text (no parsed dictionaries), map phrases to HUD lines so ODOOM shows Killed 0/N not "Kill N in …".</summary>
    private static void AppendLegacyObjectiveDescriptionProgressLines(string? desc, List<string> outLines)
    {
        if (string.IsNullOrWhiteSpace(desc)) return;
        var t = desc.Trim();
        var parts = t.Split(new[] { " and " }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        foreach (var p in parts)
            TryAppendLegacyObjectivePhrase(p, outLines);
    }

}
