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

    /// <summary>Queue XP to add to the beamed-in avatar (e.g. on monster kill). Flushed with add-item worker. Amount 0 allowed (temp) for refresh / get newTotal from server.</summary>
    public void EnqueueAddXpJobOnly(int amount)
    {
        if (!IsInitialized()) return;
        if (amount < 0) return;
        Interlocked.Add(ref _pendingXp, amount);
        _addItemSignal.Release();
    }

    /// <summary>Send pending XP to the API (POST add-xp). Returns new total on success. Used by background worker and flush. amount 0 is allowed: no-op add but response newTotal updates cache (same code path as monster kill).</summary>
    public async Task<OASISResult<int>> AddXpAsync(int amount, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<int>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (amount < 0)
            return FailAndCallback<int>("XP amount must be non-negative.", StarApiResultCode.InvalidParam);
        string? avatarId;
        lock (_stateLock)
            avatarId = _avatarId;
        if (string.IsNullOrWhiteSpace(avatarId))
            return FailAndCallback<int>("Avatar ID not set. Beam in first.", StarApiResultCode.NotInitialized);

        if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
            return FailAndCallback<int>(missingWeb4, StarApiResultCode.InvalidParam);

        var url = $"{web4Base}/api/avatar/add-xp";
        try
        {
            var payload = BuildJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteNumber("amount", amount);
                writer.WriteEndObject();
            });
            if (amount == 0)
                OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] POST url={url}");
            var response = await SendRawAsync(HttpMethod.Post, url, payload, cancellationToken).ConfigureAwait(false);
            var rawPreview = response.Result != null && response.Result.Length > 0
                ? (response.Result.Length <= 400 ? response.Result : response.Result[..400] + "...")
                : "(null or empty)";
            if (amount == 0)
                OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] response IsError={response.IsError} body={rawPreview}");
            if (response.IsError)
            {
                if (amount == 0)
                    OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] failed: {response.Message}");
                return FailAndCallback<int>(response.Message ?? "Add XP failed.", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
            }

            var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
            if (!parseResult)
            {
                if (amount == 0)
                    OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] parse failed: {parseErrorMessage}");
                return FailAndCallback<int>(parseErrorMessage, parseErrorCode);
            }

            var newTotal = GetIntProperty(resultElement, "newTotal") ?? GetIntProperty(resultElement, "NewTotal")
                ?? GetIntProperty(resultElement, "xp") ?? GetIntProperty(resultElement, "XP");
            if (amount == 0)
                OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] parsed newTotal={newTotal?.ToString() ?? "null"}");
            if (newTotal.HasValue && newTotal.Value >= 0)
            {
                Volatile.Write(ref _cachedAvatarXp, newTotal.Value);
                if (amount == 0)
                    OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] cache updated to {newTotal.Value}");
                InvokeCallback(StarApiResultCode.Success);
                return Success(newTotal.Value, StarApiResultCode.Success, amount == 0 ? "XP refreshed." : "XP added.");
            }
            /* No newTotal in response: assume current + amount (skip when amount is 0). */
            if (amount == 0)
            {
                OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] no newTotal in response; cache stays at {Volatile.Read(ref _cachedAvatarXp)}");
                InvokeCallback(StarApiResultCode.Success);
                return Success(Volatile.Read(ref _cachedAvatarXp), StarApiResultCode.Success, "XP refresh (no newTotal in response).");
            }
            var updated = Volatile.Read(ref _cachedAvatarXp) + amount;
            Volatile.Write(ref _cachedAvatarXp, updated);
            InvokeCallback(StarApiResultCode.Success);
            return Success(updated, StarApiResultCode.Success, "XP added.");
        }
        catch (Exception ex)
        {
            if (amount == 0)
                OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] exception: {ex.Message}");
            return FailAndCallback<int>($"Add XP failed: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    /// <summary>Last known avatar XP (from get-current-avatar or add-xp). For ogengine_get_avatar_xp.</summary>
    public int GetCachedAvatarXp() => Volatile.Read(ref _cachedAvatarXp);
    public long GetCachedAvatarKarma() => Volatile.Read(ref _cachedAvatarKarma);

    /// <summary>Last active quest ID from avatar detail (restored after beam-in).</summary>
    public Guid? GetCachedActiveQuestId()
    {
        lock (_stateLock) return _cachedActiveQuestId;
    }
    /// <summary>Last active objective ID from avatar detail (restored after beam-in).</summary>
    public Guid? GetCachedActiveObjectiveId()
    {
        lock (_stateLock) return _cachedActiveObjectiveId;
    }

    /// <summary>Resolve quest and objective names from cache for logging (save/load debug). Returns (null, null) if cache not ready or ids not found.</summary>
    private (string? questName, string? objectiveName) TryGetQuestAndObjectiveNamesFromCache(Guid? questId, Guid? objectiveId)
    {
        if (!questId.HasValue) return (null, null);
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return (null, null);
            var idStr = questId.Value.ToString();
            var q = _cachedQuestList.FirstOrDefault(x => string.Equals(x.Id, idStr, StringComparison.OrdinalIgnoreCase));
            if (q == null) return (null, null);
            string? objName = null;
            if (objectiveId.HasValue && q.Objectives != null)
            {
                var oidStr = objectiveId.Value.ToString();
                var o = q.Objectives.FirstOrDefault(o => string.Equals(o.Id, oidStr, StringComparison.OrdinalIgnoreCase));
                objName = o?.Title ?? o?.Description;
            }
            return (q.Name, objName);
        }
    }

    /// <summary>File log: default one totals line; per-quest lines when <c>STAR_VERBOSE_QUEST_LIST=1</c>. Caller must hold <see cref="_questsCacheLock"/>.</summary>
    private void LogTopLevelQuestPctSnapshotUnderQuestLock(string reason, Guid? activeQuestId)
    {
        if (_cachedQuestList is null || _cachedQuestList.Count == 0)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quests][PctSnapshot] reason={reason} cache=empty activeQuest={(activeQuestId.HasValue ? activeQuestId.Value.ToString("D") : "(null)")}"); } catch { /* ignore */ }
            return;
        }
        var top = _cachedQuestList
            .Where(q => string.IsNullOrWhiteSpace(q.ParentQuestId) || q.ParentQuestId == Guid.Empty.ToString())
            .OrderBy(q => q.Id ?? string.Empty, StringComparer.Ordinal)
            .ToList();
        var totalObj = 0;
        var doneObj = 0;
        foreach (var q in top)
        {
            totalObj += q.Objectives?.Count ?? 0;
            doneObj += q.Objectives?.Count(o => o.IsCompleted) ?? 0;
        }
        try
        {
            OGEngineExports.StarApiLogFileOnly($"[Quests][PctSnapshot] reason={reason} questsInCache={_cachedQuestList.Count} topLevel={top.Count} objectivesCompleted={doneObj}/{totalObj} activeQuest={activeQuestId?.ToString("D") ?? "(null)"}");
        }
        catch { /* ignore */ }
        if (!VerboseQuestListLogsEnabled) return;
        var sb = new StringBuilder(512 + top.Count * 120);
        sb.Append("[Quests][PctSnapshot][verbose] reason=").Append(reason)
            .Append(" topLevel=").Append(top.Count)
            .Append(" activeQuest=").Append(activeQuestId?.ToString("D") ?? "(null)");
        var n = 0;
        foreach (var q in top)
        {
            if (n++ >= 32)
            {
                sb.Append(" | ...(truncated)");
                break;
            }
            var oc = q.Objectives?.Count ?? 0;
            var done = q.Objectives?.Count(o => o.IsCompleted) ?? 0;
            var pct = oc > 0 ? done * 100 / oc : 0;
            var mark = activeQuestId.HasValue && activeQuestId.Value != Guid.Empty && string.Equals(q.Id, activeQuestId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase) ? "*" : "";
            sb.Append(" | ").Append(mark).Append(q.Id).Append(':').Append(EscapeForQuestLine(q.Name ?? ""))
                .Append(" st=").Append(q.Status ?? "")
                .Append(" obj=").Append(done).Append('/').Append(oc)
                .Append(" pct=").Append(pct);
        }
        try { OGEngineExports.StarApiLogFileOnly(sb.ToString()); } catch { /* ignore */ }
    }

    /// <summary>Log top-level quest pct from an in-memory list (e.g. incoming GET payload that will not be applied).</summary>
    private void LogTopLevelQuestPctSnapshotFromList(string reason, List<StarQuestInfo>? list, Guid? activeQuestId)
    {
        if (list is null || list.Count == 0)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quests][PctSnapshot] reason={reason} list=empty activeQuest={(activeQuestId.HasValue ? activeQuestId.Value.ToString("D") : "(null)")}"); } catch { /* ignore */ }
            return;
        }
        var top = list
            .Where(q => string.IsNullOrWhiteSpace(q.ParentQuestId) || q.ParentQuestId == Guid.Empty.ToString())
            .OrderBy(q => q.Id ?? string.Empty, StringComparer.Ordinal)
            .ToList();
        var totalObj = 0;
        var doneObj = 0;
        foreach (var q in top)
        {
            totalObj += q.Objectives?.Count ?? 0;
            doneObj += q.Objectives?.Count(o => o.IsCompleted) ?? 0;
        }
        try
        {
            OGEngineExports.StarApiLogFileOnly($"[Quests][PctSnapshot] reason={reason} questsInList={list.Count} topLevel={top.Count} objectivesCompleted={doneObj}/{totalObj} activeQuest={activeQuestId?.ToString("D") ?? "(null)"}");
        }
        catch { /* ignore */ }
        if (!VerboseQuestListLogsEnabled) return;
        var sb = new StringBuilder(512 + top.Count * 120);
        sb.Append("[Quests][PctSnapshot][verbose] reason=").Append(reason)
            .Append(" topLevel=").Append(top.Count)
            .Append(" activeQuest=").Append(activeQuestId?.ToString("D") ?? "(null)");
        var n = 0;
        foreach (var q in top)
        {
            if (n++ >= 32)
            {
                sb.Append(" | ...(truncated)");
                break;
            }
            var oc = q.Objectives?.Count ?? 0;
            var done = q.Objectives?.Count(o => o.IsCompleted) ?? 0;
            var pct = oc > 0 ? done * 100 / oc : 0;
            var mark = activeQuestId.HasValue && activeQuestId.Value != Guid.Empty && string.Equals(q.Id, activeQuestId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase) ? "*" : "";
            sb.Append(" | ").Append(mark).Append(q.Id).Append(':').Append(EscapeForQuestLine(q.Name ?? ""))
                .Append(" st=").Append(q.Status ?? "")
                .Append(" obj=").Append(done).Append('/').Append(oc)
                .Append(" pct=").Append(pct);
        }
        try { OGEngineExports.StarApiLogFileOnly(sb.ToString()); } catch { /* ignore */ }
    }

    /// <summary>Native: quest list popup opened (1) or closed (0). While open, progress and quest-cache replacement from refresh are ignored.</summary>
    public void NotifyQuestPopupOpenChanged(int isOpen)
    {
        var newVal = isOpen != 0 ? 1 : 0;
        Interlocked.Exchange(ref _questUiPopupOpen, newVal);
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] quest popup open={newVal}"); } catch { /* ignore */ }
    }

    /// <summary>File log: default one-line summary; full objective/tracker dump when <c>STAR_VERBOSE_QUEST_LIST=1</c>. Search <c>[Quest][ActiveSnapshot]</c>.</summary>
    private void LogActiveQuestSnapshot(string reason)
    {
        Guid? qid;
        Guid? oid;
        lock (_stateLock)
        {
            qid = _cachedActiveQuestId;
            oid = _cachedActiveObjectiveId;
        }

        if (!VerboseQuestListLogsEnabled)
        {
            string? qn = null;
            var objN = 0;
            if (qid.HasValue && qid.Value != Guid.Empty)
            {
                lock (_questsCacheLock)
                {
                    if (_cachedQuestList != null)
                    {
                        var qs = qid.Value.ToString("D");
                        var quest = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, qs, StringComparison.OrdinalIgnoreCase));
                        if (quest != null)
                        {
                            qn = quest.Name;
                            objN = quest.Objectives?.Count ?? 0;
                        }
                    }
                }
            }
            try
            {
                OGEngineExports.StarApiLogFileOnly($"[Quest][ActiveSnapshot] reason={reason} activeQuest={qid?.ToString("D") ?? "(null)"} activeObjective={oid?.ToString("D") ?? "(null)"} questName={qn ?? "(n/a)"} objectiveCount={objN}");
            }
            catch { /* ignore */ }
            return;
        }

        var sb = new StringBuilder(1024);
        sb.Append("[Quest][ActiveSnapshot] reason=").Append(reason)
            .Append(" | cachedActiveQuestId=").Append(qid?.ToString("D") ?? "(null)")
            .Append(" | cachedActiveObjectiveId=").Append(oid?.ToString("D") ?? "(null)")
            .AppendLine();

        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _cachedQuestList.Count == 0)
            {
                sb.Append("  questListCache: empty (no quests loaded yet)").AppendLine();
            }
            else if (!qid.HasValue || qid.Value == Guid.Empty)
            {
                sb.Append("  questListCache: ").Append(_cachedQuestList.Count).Append(" quests | no active quest id in client cache").AppendLine();
            }
            else
            {
                var qs = qid.Value.ToString("D");
                var quest = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, qs, StringComparison.OrdinalIgnoreCase));
                if (quest == null)
                {
                    sb.Append("  activeQuestId NOT FOUND in questListCache: ").Append(qs).AppendLine();
                    sb.Append("  hint: refresh quest cache or check id spelling vs API").AppendLine();
                }
                else
                {
                    sb.Append("  resolvedQuest: Id=").Append(quest.Id)
                        .Append(" Name=").Append(quest.Name ?? "")
                        .Append(" Status=").Append(quest.Status ?? "")
                        .Append(" GameSource=").Append(quest.GameSource ?? "")
                        .AppendLine();
                    if (quest.Objectives == null || quest.Objectives.Count == 0)
                    {
                        sb.Append("  objectives: (none on this quest in cache)").AppendLine();
                    }
                    else
                    {
                        for (var i = 0; i < quest.Objectives.Count; i++)
                        {
                            var o = quest.Objectives[i];
                            var isMarked = oid.HasValue && string.Equals(o.Id, oid.Value.ToString("D"), StringComparison.OrdinalIgnoreCase);
                            var listLine = FormatObjectiveLineForGameList(o, quest);
                            sb.Append("  objective[").Append(i).Append("] Id=").Append(o.Id)
                                .Append(" Order=").Append(o.Order)
                                .Append(" IsCompleted=").Append(o.IsCompleted)
                                .Append(" GameSource=").Append(o.GameSource ?? "")
                                .Append(isMarked ? " **matches cachedActiveObjectiveId**" : "")
                                .AppendLine();
                            sb.Append("             Title=").Append(o.Title ?? "").AppendLine();
                            sb.Append("             Description=").Append(o.Description ?? "").AppendLine();
                            sb.Append("             listLine(Q/O tab text)=").Append(listLine).AppendLine();
                        }
                    }
                }
            }
        }

        var qidStr = qid?.ToString("D");
        if (!string.IsNullOrWhiteSpace(qidStr))
        {
            if (TryGetQuestTrackerObjectivesProgress(qidStr, out var trk, out var hudIdx))
            {
                sb.Append("  hudTrackerObjectiveIndex (green / first incomplete): ").Append(hudIdx).AppendLine();
                if (string.IsNullOrWhiteSpace(trk))
                    sb.Append("  hudTrackerRows: (empty — no per-objective lines from Need/Progress dicts)").AppendLine();
                else
                {
                    sb.Append("  hudTrackerRows (same as odoom_quest_tracker_objectives, one row per objective):").AppendLine();
                    var rows = trk.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    for (var r = 0; r < rows.Length; r++)
                        sb.Append("    [").Append(r).Append("] ").Append(rows[r]).AppendLine();
                }
            }
            else
                sb.Append("  hudTrackerRows: (TryGetQuestTrackerObjectivesProgress false — quest cache not ready)").AppendLine();
        }

        try { OGEngineExports.StarApiLogFileOnly(sb.ToString()); } catch { /* ignore */ }
        try { OGEngineExports.StarApiLogFileOnly($"[Quest][ActiveSnapshot] reason={reason} (verbose block logged above)"); } catch { /* ignore */ }
    }

    private static readonly bool VerboseQuestParseLogsEnabled =
        string.Equals(Environment.GetEnvironmentVariable("STAR_VERBOSE_QUEST_PARSE"), "1", StringComparison.OrdinalIgnoreCase);

    /// <summary>Per-quest pct lines in file log, full <see cref="LogActiveQuestSnapshot"/>, and console merge-detail. Default off so STAR debug stays readable; set <c>STAR_VERBOSE_QUEST_LIST=1</c> for deep quest list logging.</summary>
    private static readonly bool VerboseQuestListLogsEnabled =
        string.Equals(Environment.GetEnvironmentVariable("STAR_VERBOSE_QUEST_LIST"), "1", StringComparison.OrdinalIgnoreCase);

    /// <summary>File-only: logs large strings in fixed-size chunks (search <c>[Quest][Parse]</c> in ogengine.log).</summary>
    private static void LogQuestParseChunkedFileOnly(string linePrefix, string? text)
    {
        if (!VerboseQuestParseLogsEnabled) return;
        try
        {
            if (string.IsNullOrEmpty(text))
            {
                OGEngineExports.StarApiLogFileOnly($"{linePrefix} (empty)");
                return;
            }

            const int maxChunk = 3200;
            if (text.Length <= maxChunk)
            {
                OGEngineExports.StarApiLogFileOnly($"{linePrefix} len={text.Length} {text}");
                return;
            }

            OGEngineExports.StarApiLogFileOnly($"{linePrefix} len={text.Length} chunkSize={maxChunk}");
            for (var i = 0; i < text.Length; i += maxChunk)
            {
                var take = Math.Min(maxChunk, text.Length - i);
                OGEngineExports.StarApiLogFileOnly($"{linePrefix} offset={i} len={take} {text.AsSpan(i, take).ToString()}");
            }
        }
        catch (Exception ex)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quest][Parse] LogQuestParseChunkedFileOnly error: {ex.Message}"); } catch { /* ignore */ }
        }
    }

    private static void LogQuestJsonShapeFileOnly(string prefix, JsonElement el)
    {
        if (!VerboseQuestParseLogsEnabled) return;
        try
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Array:
                    OGEngineExports.StarApiLogFileOnly($"{prefix} ValueKind=Array Length={el.GetArrayLength()}");
                    break;
                case JsonValueKind.Object:
                    var names = string.Join(", ", el.EnumerateObject().Select(p => p.Name));
                    OGEngineExports.StarApiLogFileOnly($"{prefix} ValueKind=Object PropertyNames=[{names}]");
                    break;
                default:
                    OGEngineExports.StarApiLogFileOnly($"{prefix} ValueKind={el.ValueKind}");
                    break;
            }
        }
        catch (Exception ex)
        {
            try { OGEngineExports.StarApiLogFileOnly($"{prefix} shapeLogError={ex.Message}"); } catch { /* ignore */ }
        }
    }
}
