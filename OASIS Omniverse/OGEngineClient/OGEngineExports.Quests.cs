using System.Buffers;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STARAPI.Client;
public static unsafe partial class OGEngineExports
{
    /// <summary>Write serialized quest list (InProgress) to buf for game UI. Returns cached data immediately (never blocks). If cache is empty, starts a background refresh and returns "Loading...". Format: "Q\tid\tname\tdesc\tstatus\tpct\n" per quest, "O\tid\tdesc\tdone\n" per objective, "---\n" between quests. Returns bytes written (excluding null), or negative StarApiResultCode on error. Must not throw - native caller can crash.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_quests_string", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetQuestsString(sbyte* buf, nuint bufSize)
    {
        try
        {
            if (buf is null || bufSize == 0)
                return (int)SetErrorAndReturn("buf and bufSize must be non-null/non-zero.", StarApiResultCode.InvalidParam, StarApiOpGetQuestsString);

            var client = GetClient();
            if (client is null)
                return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetQuestsString);

            string fallback;
            if (!client.TryGetQuestsCache(out var str))
                fallback = "Loading...";
            else
                fallback = str ?? string.Empty;

            var bytesArr = Encoding.UTF8.GetBytes(fallback);
            var toCopy = (int)Math.Min((nuint)bytesArr.Length, bufSize - 1);
            /* Log only when return length changes (e.g. once for Loading, once when cache fills) to avoid log spam */
            try
            {
                if (toCopy != StarApiGetQuestsStringLastLoggedToCopy)
                {
                    StarApiGetQuestsStringLastLoggedToCopy = toCopy;
                    if (fallback == "Loading...")
                        StarApiLogFileOnly("[Quests] ogengine_get_quests_string: cache miss, returning Loading...");
                    else
                    {
                        var previewLen = Math.Min(250, fallback.Length);
                        var preview = previewLen > 0 ? fallback.Substring(0, previewLen).Replace("\t", "|").Replace("\r", " ").Replace("\n", "\\n") : "";
                        StarApiLogFileOnly($"[Quests] ogengine_get_quests_string: cache HIT len={fallback.Length} toCopy={toCopy} preview={preview}");
                    }
                }
            }
            catch { /* ignore */ }
            if (toCopy > 0)
                new ReadOnlySpan<byte>(bytesArr, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
            buf[toCopy] = 0;
            SetError(string.Empty);
            InvokeOperationCallback(StarApiResultCode.Success, StarApiOpGetQuestsString);
            return toCopy;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Quests] ogengine_get_quests_string exception: {ex.Message}"); } catch { /* ignore */ }
            try
            {
                const string err = "Error: Error loading quests. Check console or ogengine.log for details.";
                var bytes = Encoding.UTF8.GetBytes(err);
                var toCopy = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
                if (buf != null && bufSize > 0 && toCopy > 0)
                {
                    new ReadOnlySpan<byte>(bytes, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
                    buf[toCopy] = 0;
                }
                return toCopy;
            }
            catch
            {
                if (buf != null && bufSize > 0)
                {
                    buf[0] = (sbyte)'?';
                    if (bufSize > 1)
                        buf[1] = 0;
                    return 1;
                }
                return 0;
            }
        }
    }

    /// <summary>Write serialized top-level quests only (no sub-quests) to buf for left list. Same format as get_quests_string. Returns bytes written or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_top_level_quests_string", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetTopLevelQuestsString(sbyte* buf, nuint bufSize)
    {
        try
        {
            if (buf is null || bufSize == 0)
                return (int)SetErrorAndReturn("buf and bufSize must be non-null/non-zero.", StarApiResultCode.InvalidParam, StarApiOpGetTopLevelQuestsString);
            var client = GetClient();
            if (client is null)
                return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetTopLevelQuestsString);
            string fallback;
            if (!client.TryGetTopLevelQuestsCache(out var str))
            {
                fallback = "Loading...";
                try { if (!_topLevelQuestsLastLoggedLoading) { _topLevelQuestsLastLoggedLoading = true; StarApiLogFileOnly("[STAR] ogengine_get_top_level_quests_string cache miss -> Loading... (once until cache fills)"); } } catch { }
            }
            else
            {
                fallback = str ?? string.Empty;
                _topLevelQuestsLastLoggedLoading = false;
            }
            var bytesArr = Encoding.UTF8.GetBytes(fallback);
            var toCopy = (int)Math.Min((nuint)bytesArr.Length, bufSize - 1);
            if (toCopy > 0)
                new ReadOnlySpan<byte>(bytesArr, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
            buf[toCopy] = 0;
            SetError(string.Empty);
            return toCopy;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Quests] ogengine_get_top_level_quests_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetTopLevelQuestsString);
        }
    }

    /// <summary>Write serialized sub-quests of parent_quest_id to buf for right panel. Same format as get_quests_string. Returns bytes written or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_quest_sub_quests_string", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetQuestSubQuestsString(sbyte* parentQuestId, sbyte* buf, nuint bufSize)
    {
        try
        {
            if (buf is null || bufSize == 0)
                return (int)SetErrorAndReturn("buf and bufSize must be non-null/non-zero.", StarApiResultCode.InvalidParam, StarApiOpGetQuestSubQuestsString);
            var client = GetClient();
            if (client is null)
                return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetQuestSubQuestsString);
            var parentId = parentQuestId != null ? Marshal.PtrToStringUTF8((IntPtr)parentQuestId) : null;
            if (!client.TryGetQuestSubQuestsCache(parentId, out var str))
            {
                var loading = "Loading...";
                var bytesArr = Encoding.UTF8.GetBytes(loading);
                var toCopy = (int)Math.Min((nuint)bytesArr.Length, bufSize - 1);
                if (toCopy > 0)
                    new ReadOnlySpan<byte>(bytesArr, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
                buf[toCopy] = 0;
                return toCopy;
            }
            var fallback = str ?? string.Empty;
            var bytes = Encoding.UTF8.GetBytes(fallback);
            var toCopyVal = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
            if (toCopyVal > 0)
                new ReadOnlySpan<byte>(bytes, 0, toCopyVal).CopyTo(new Span<byte>(buf, toCopyVal));
            buf[toCopyVal] = 0;
            SetError(string.Empty);
            return toCopyVal;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Quests] ogengine_get_quest_sub_quests_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetQuestSubQuestsString);
        }
    }

    /// <summary>Write serialized objectives from the quest's Objectives collection (Quest.Objectives) for parent_quest_id to buf for right panel. Same format as get_quests_string. Returns bytes written or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_quest_objectives_string", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetQuestObjectivesString(sbyte* parentQuestId, sbyte* buf, nuint bufSize)
    {
        try
        {
            if (buf is null || bufSize == 0)
                return (int)SetErrorAndReturn("buf and bufSize must be non-null/non-zero.", StarApiResultCode.InvalidParam, StarApiOpGetQuestObjectivesString);
            var client = GetClient();
            if (client is null)
                return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetQuestObjectivesString);
            var parentId = parentQuestId != null ? Marshal.PtrToStringUTF8((IntPtr)parentQuestId) : null;
            if (!client.TryGetQuestObjectivesCache(parentId, out var str))
            {
                var loading = "Loading...";
                var bytesArr = Encoding.UTF8.GetBytes(loading);
                var toCopy = (int)Math.Min((nuint)bytesArr.Length, bufSize - 1);
                if (toCopy > 0)
                    new ReadOnlySpan<byte>(bytesArr, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
                buf[toCopy] = 0;
                return toCopy;
            }
            var fallback = str ?? string.Empty;
            var bytes = Encoding.UTF8.GetBytes(fallback);
            var toCopyVal = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
            if (toCopyVal > 0)
                new ReadOnlySpan<byte>(bytes, 0, toCopyVal).CopyTo(new Span<byte>(buf, toCopyVal));
            buf[toCopyVal] = 0;
            SetError(string.Empty);
            var lineCount = fallback.Split('\n').Count(s => s.TrimStart().StartsWith("Q\t", StringComparison.Ordinal));
            return toCopyVal;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Quests] ogengine_get_quest_objectives_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetQuestObjectivesString);
        }
    }

    /// <summary>Return objectives cache version; increments when on-demand fetch merges objectives. UI should poll each frame and re-call get_quest_objectives_string when this changes to refresh the list.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_quest_objectives_cache_version", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetQuestObjectivesCacheVersion()
    {
        try
        {
            var client = GetClient();
            return client?.GetQuestObjectivesCacheVersion() ?? 0;
        }
        catch { return 0; }
    }

    /// <summary>Write serialized prerequisite quests (id, name, desc) for quest_id to buf for right panel. Same format as get_quests_string. Returns bytes written or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_quest_prereqs_string", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetQuestPrereqsString(sbyte* questId, sbyte* buf, nuint bufSize)
    {
        try
        {
            if (buf is null || bufSize == 0)
                return (int)SetErrorAndReturn("buf and bufSize must be non-null/non-zero.", StarApiResultCode.InvalidParam, StarApiOpGetQuestPrereqsString);
            var client = GetClient();
            if (client is null)
                return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetQuestPrereqsString);
            var qId = questId != null ? Marshal.PtrToStringUTF8((IntPtr)questId) : null;
            if (!client.TryGetQuestPrereqsCache(qId, out var str))
            {
                var loading = "Loading...";
                var bytesArr = Encoding.UTF8.GetBytes(loading);
                var toCopy = (int)Math.Min((nuint)bytesArr.Length, bufSize - 1);
                if (toCopy > 0)
                    new ReadOnlySpan<byte>(bytesArr, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
                buf[toCopy] = 0;
                return toCopy;
            }
            var fallback = str ?? string.Empty;
            var bytes = Encoding.UTF8.GetBytes(fallback);
            var toCopyVal = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
            if (toCopyVal > 0)
                new ReadOnlySpan<byte>(bytes, 0, toCopyVal).CopyTo(new Span<byte>(buf, toCopyVal));
            buf[toCopyVal] = 0;
            SetError(string.Empty);
            return toCopyVal;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Quests] ogengine_get_quest_prereqs_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetQuestPrereqsString);
        }
    }

    /// <summary>Write requirement/progress lines for quest and optional objective to buf. Returns bytes written or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_quest_objective_requirements_string", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetQuestObjectiveRequirementsString(sbyte* questId, sbyte* objectiveId, sbyte* buf, nuint bufSize)
    {
        try
        {
            if (buf is null || bufSize == 0)
                return (int)SetErrorAndReturn("buf and bufSize must be non-null/non-zero.", StarApiResultCode.InvalidParam, StarApiOpGetQuestObjectiveRequirementsString);
            var client = GetClient();
            if (client is null)
                return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetQuestObjectiveRequirementsString);
            var qId = questId != null ? Marshal.PtrToStringUTF8((IntPtr)questId) : null;
            var oId = objectiveId != null ? Marshal.PtrToStringUTF8((IntPtr)objectiveId) : null;
            if (!client.TryGetQuestObjectiveRequirementsForGame(qId, oId, out var str))
            {
                var loading = "Loading...";
                var bytesArr = Encoding.UTF8.GetBytes(loading);
                var toCopy = (int)Math.Min((nuint)bytesArr.Length, bufSize - 1);
                if (toCopy > 0)
                    new ReadOnlySpan<byte>(bytesArr, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
                buf[toCopy] = 0;
                return toCopy;
            }
            var fallback = str ?? string.Empty;
            var bytes = Encoding.UTF8.GetBytes(fallback);
            var toCopyVal = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
            if (toCopyVal > 0)
                new ReadOnlySpan<byte>(bytes, 0, toCopyVal).CopyTo(new Span<byte>(buf, toCopyVal));
            buf[toCopyVal] = 0;
            SetError(string.Empty);
            return toCopyVal;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Quests] ogengine_get_quest_objective_requirements_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetQuestObjectiveRequirementsString);
        }
    }

    /// <summary>Write one progress line per objective for the tracker. Returns bytes written or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_quest_tracker_objectives_string", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetQuestTrackerObjectivesString(sbyte* questId, sbyte* buf, nuint bufSize)
    {
        try
        {
            if (buf is null || bufSize == 0)
                return (int)SetErrorAndReturn("buf and bufSize must be non-null/non-zero.", StarApiResultCode.InvalidParam, StarApiOpGetQuestTrackerObjectivesString);
            var client = GetClient();
            if (client is null)
                return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetQuestTrackerObjectivesString);
            var qId = questId != null ? Marshal.PtrToStringUTF8((IntPtr)questId) : null;
            if (!client.TryGetQuestTrackerObjectivesProgress(qId, out var str, out _))
            {
                var empty = "";
                var bytesArr = Encoding.UTF8.GetBytes(empty);
                buf[0] = 0;
                return 0;
            }
            var fallback = str ?? string.Empty;
            var bytes = Encoding.UTF8.GetBytes(fallback);
            var toCopyVal = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
            if (toCopyVal > 0)
                new ReadOnlySpan<byte>(bytes, 0, toCopyVal).CopyTo(new Span<byte>(buf, toCopyVal));
            buf[toCopyVal] = 0;
            SetError(string.Empty);
            return toCopyVal;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Quests] ogengine_get_quest_tracker_objectives_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetQuestTrackerObjectivesString);
        }
    }

    /// <summary>Return 0-based index of first incomplete objective for the tracked quest.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_quest_tracker_active_objective_index", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetQuestTrackerActiveObjectiveIndex(sbyte* questId)
    {
        try
        {
            var client = GetClient();
            if (client is null) return 0;
            var qId = questId != null ? Marshal.PtrToStringUTF8((IntPtr)questId) : null;
            if (!client.TryGetQuestTrackerObjectivesProgress(qId, out _, out var activeIndex))
                return 0;
            return activeIndex;
        }
        catch { return 0; }
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_invalidate_inventory_cache", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiInvalidateInventoryCache()
    {
        var client = GetClient();
        client?.InvalidateInventoryCache();
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_invalidate_quest_cache", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiInvalidateQuestCache()
    {
        try
        {
            var client = GetClient();
            client?.InvalidateQuestCache();
        }
        catch
        {
            /* Must not throw - native caller can crash. */
        }
    }

    /// <summary>Start a background refresh of the quest cache without clearing it. UI can show existing cache immediately and will update when the callback returns.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_refresh_quest_cache_in_background", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiRefreshQuestCacheInBackground()
    {
        try
        {
            var client = GetClient();
            client?.RequestQuestCacheRefreshInBackground(forceRefetch: true);
        }
        catch
        {
            /* Must not throw - native caller can crash. */
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_set_quest_popup_open", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetQuestPopupOpen(int isOpen)
    {
        try
        {
            var client = GetClient();
            client?.NotifyQuestPopupOpenChanged(isOpen);
        }
        catch
        {
            /* Must not throw - native caller can crash. */
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_clear_cache", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiClearCache()
    {
        var client = GetClient();
        client?.ClearCache();
    }

    /// <summary>Add item to avatar inventory. quantity: amount to add; stack: 1 = increment if exists, 0 = error if exists.</summary>
}
