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
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_last_error", CallConvs = [typeof(CallConvCdecl)])]
    public static sbyte* StarApiGetLastError()
    {
        lock (NativeStateLock)
            return (sbyte*)_lastError;
    }

    /// <summary>Consume last mint result (from background pickup-with-mint). Returns 1 if result was available and written to buffers, 0 otherwise. Buffers are null-terminated. Use from game pump/frame to show NFT ID and hash in console.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_consume_last_mint_result", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiConsumeLastMintResult(sbyte* itemNameOut, nuint itemNameSize, sbyte* nftIdOut, nuint nftIdSize, sbyte* hashOut, nuint hashSize)
    {
        var client = GetClient();
        if (client is null || itemNameOut is null || nftIdOut is null || hashOut is null)
            return 0;
        if (!client.ConsumeLastMintResult(out var itemName, out var nftId, out var hash))
            return 0;
        var isize = (int)Math.Min(itemNameSize, int.MaxValue);
        var nsize = (int)Math.Min(nftIdSize, int.MaxValue);
        var hsize = (int)Math.Min(hashSize, int.MaxValue);
        if (isize > 0) WriteUtf8ToOutput(itemName ?? string.Empty, itemNameOut, isize);
        if (nsize > 0) WriteUtf8ToOutput(nftId ?? string.Empty, nftIdOut, nsize);
        if (hsize > 0) WriteUtf8ToOutput(hash ?? string.Empty, hashOut, hsize);
        return 1;
    }

    /// <summary>Consume last background error (mint/add_item failure or pickup not queued). Writes message to buf (null-terminated). Returns 1 if an error was available, 0 otherwise. Call from game pump to show errors in console.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_consume_last_background_error", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiConsumeLastBackgroundError(sbyte* buf, nuint size)
    {
        var msg = TryConsumeLastBackgroundError();
        if (msg is null || buf == null || size == 0) return 0;
        var len = (int)Math.Min(size, int.MaxValue);
        WriteUtf8ToOutput(msg, buf, len);
        return 1;
    }

    /// <summary>Consume one STAR log message for the game console. Returns 1 if a message was copied to buf, 0 otherwise. Call from game pump each frame to show STAR logs in console.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_consume_console_log", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiConsumeConsoleLog(sbyte* buf, nuint size)
    {
        var msg = TryConsumeConsoleLog();
        if (msg is null || buf == null || size == 0) return 0;
        var len = (int)Math.Min(size, int.MaxValue);
        WriteUtf8ToOutput(msg, buf, len);
        return 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_set_callback", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetCallback(delegate* unmanaged[Cdecl]<int, void*, void> callback, void* userData)
    {
        lock (NativeStateLock)
        {
            _callback = callback;
            _callbackUserData = userData;
        }
    }

    /// <summary>Operation type for ogengine_set_operation_callback. Game can filter and only run "profile loaded" when type is ProfileLoaded.</summary>
    public const int StarApiOpProfileLoaded = 0;
    public const int StarApiOpGetAvatarId = 1;
    public const int StarApiOpHasItem = 2;
    public const int StarApiOpGetInventory = 3;
    public const int StarApiOpGetQuestsString = 4;
    public const int StarApiOpMintInventoryNft = 5;
    public const int StarApiOpUseItem = 6;
    public const int StarApiOpStartQuest = 7;
    public const int StarApiOpCompleteQuestObjective = 8;
    public const int StarApiOpCompleteQuest = 9;
    public const int StarApiOpAddItem = 10;
    public const int StarApiOpFlushAddItemJobs = 11;
    public const int StarApiOpFlushUseItemJobs = 12;
    public const int StarApiOpSendItemToAvatar = 13;
    public const int StarApiOpSendItemToClan = 14;
    public const int StarApiOpSetActiveQuest = 15;
    public const int StarApiOpCreateMonsterNft = 16;
    public const int StarApiOpDeployBossNft = 17;
    public const int StarApiOpInit = 18;
    public const int StarApiOpGetTopLevelQuestsString = 19;
    public const int StarApiOpGetQuestSubQuestsString = 20;
    public const int StarApiOpGetQuestObjectivesString = 21;
    public const int StarApiOpGetQuestPrereqsString = 22;
    public const int StarApiOpGetQuestObjectiveRequirementsString = 23;
    public const int StarApiOpGetQuestTrackerObjectivesString = 24;
    public const int StarApiOpAuthenticate = 25;
    public const int StarApiOpSetOasisBaseUrl = 26;
    public const int StarApiOpSetAvatarId = 27;
    /// <summary>Fired after a successful background quest list fetch updated the in-memory quest cache (progress POST, popup refresh, or cold Ensure). Native should re-read tracker/popup CVars.</summary>
    public const int StarApiOpQuestsCacheRefreshed = 28;
    public const int StarApiOpRequestTeleport = 29;
    public const int StarApiOpPollTeleportRequest = 30;
    public const int StarApiOpConfirmTeleportArrival = 31;
    public const int StarApiOpPollSpawnEvent = 32;
    public const int StarApiOpConfirmSpawn = 33;
    public const int StarApiOpGetMapEntities = 34;

    /// <summary>Invoke operation callback on the game-thread pump when available; fallback to direct invoke.</summary>
    internal static void InvokeOperationCallback(StarApiResultCode code, int operationType)
    {
        if (StarSyncExports.TryEnqueueOperationCallback(code, operationType))
            return;

        InvokeOperationCallbackOnCurrentThread(code, operationType);
    }

    /// <summary>Invoke callback immediately on the current thread (used by ogengine_sync_pump dispatch).</summary>
    internal static void InvokeOperationCallbackOnCurrentThread(StarApiResultCode code, int operationType)
    {
        delegate* unmanaged[Cdecl]<int, int, void*, void> opCb;
        void* opUserData;
        lock (NativeStateLock)
        {
            opCb = _operationCallback;
            opUserData = _operationCallbackUserData;
        }
        if (opCb != null)
            opCb((int)code, operationType, opUserData);
        else
            InvokeCallback(code);
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_set_operation_callback", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetOperationCallback(delegate* unmanaged[Cdecl]<int, int, void*, void> callback, void* userData)
    {
        lock (NativeStateLock)
        {
            _operationCallback = callback;
            _operationCallbackUserData = userData;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_set_oasis_base_url", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSetOasisBaseUrl(sbyte* oasisBaseUrl)
    {
        var url = PtrToString(oasisBaseUrl) ?? string.Empty;
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpSetOasisBaseUrl);

        var result = client.SetWeb4OasisApiBaseUrl(url);
        return (int)FinalizeResult(result, StarApiOpSetOasisBaseUrl);
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_avatar_id", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetAvatarId(sbyte* avatarIdOut, nuint avatarIdSize)
    {
        if (avatarIdOut is null || avatarIdSize == 0)
            return (int)SetErrorAndReturn("avatarIdOut must not be null and size must be > 0.", StarApiResultCode.InvalidParam, StarApiOpGetAvatarId);

        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetAvatarId);

        // Use cached avatar ID when available (set by AuthenticateAsync or init with api_key+avatar_id) to avoid a second GET WEB4 get-logged-in-avatar-with-xp when the game then calls ogengine_refresh_avatar_profile().
        string? avatarId = client.GetCachedAvatarId();
        if (string.IsNullOrWhiteSpace(avatarId))
        {
            // Not set yet (e.g. rare path); resolve from API
            var result = client.GetCurrentAvatarAsync().GetAwaiter().GetResult();
            if (result.IsError || result.Result is null)
                return (int)SetErrorAndReturn(result.Message ?? "Failed to get avatar ID. Authenticate first.", ExtractCode(result), StarApiOpGetAvatarId);
            avatarId = result.Result.Id.ToString();
        }
        if (string.IsNullOrWhiteSpace(avatarId))
            return (int)SetErrorAndReturn("Avatar ID not available. Authenticate first.", StarApiResultCode.NotInitialized, StarApiOpGetAvatarId);

        var bytes = Encoding.UTF8.GetBytes(avatarId);
        var copySize = Math.Min((int)avatarIdSize - 1, bytes.Length);
        if (copySize > 0)
        {
            Marshal.Copy(bytes, 0, (nint)avatarIdOut, copySize);
            avatarIdOut[copySize] = 0;
        }
        else
        {
            avatarIdOut[0] = 0;
        }

        SetError(string.Empty);
        InvokeOperationCallback(StarApiResultCode.Success, StarApiOpGetAvatarId);
        return (int)StarApiResultCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_set_avatar_id", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSetAvatarId(sbyte* avatarId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpSetAvatarId);

        var result = client.SetAvatarId(PtrToString(avatarId) ?? string.Empty);
        return (int)FinalizeResultNoCallback(result);
    }

    /// <summary>Used by StarSyncExports (ogengine_sync_* in-client implementation).</summary>
    internal static OGEngineClient? GetClient()
    {
        lock (Sync)
            return _client;
    }

    /// <summary>Build native ogengine_item_list_t from managed list for star_sync inventory result. Caller must call StarApiFreeItemList when done.</summary>
    internal static unsafe ogengine_item_list_t* BuildItemListFromInventory(List<StarItem> list)
    {
        if (list is null || list.Count == 0)
        {
            var empty = (ogengine_item_list_t*)NativeMemory.Alloc((nuint)1, (nuint)sizeof(ogengine_item_list_t));
            if (empty is not null)
            {
                empty->items = null;
                empty->count = 0;
                empty->capacity = 0;
            }
            return empty;
        }
        var count = (nuint)list.Count;
        var listPtr = (ogengine_item_list_t*)NativeMemory.Alloc((nuint)1, (nuint)sizeof(ogengine_item_list_t));
        if (listPtr is null) return null;
        listPtr->count = count;
        listPtr->capacity = count;
        listPtr->items = (ogengine_item_t*)NativeMemory.Alloc(count, (nuint)sizeof(ogengine_item_t));
        if (listPtr->items is null)
        {
            NativeMemory.Free(listPtr);
            return null;
        }
        for (var i = 0; i < list.Count; i++)
        {
            var src = list[i];
            var dst = &listPtr->items[i];
            WriteFixedUtf8(src.Id.ToString(), dst->id, 64);
            WriteFixedUtf8(src.Name, dst->name, 256);
            WriteFixedUtf8(src.Description, dst->description, 512);
            WriteFixedUtf8(src.GameSource, dst->game_source, 64);
            WriteFixedUtf8(src.ItemType, dst->item_type, 64);
            WriteFixedUtf8(src.NftId ?? string.Empty, dst->nft_id, 128);
            dst->quantity = src.Quantity;
        }
        return listPtr;
    }

    private static readonly object LogLock = new();
    private static bool _logPathLogged;

    private static string GetStarApiLogPath()
    {
        var dir = AppContext.BaseDirectory;
        if (string.IsNullOrEmpty(dir)) dir = Environment.CurrentDirectory ?? ".";
        dir = Path.GetFullPath(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return Path.Combine(dir, "ogengine.log");
    }

    private static void AppendStarApiDiagnosticsFileLine(string messageBody, bool prefixWithStarTag = false)
    {
        var body = TruncateUtf8ForInterop(messageBody ?? string.Empty, MaxStarApiFileLineUtf8Bytes);
        var line = prefixWithStarTag
            ? $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] [STAR] {body}"
            : $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] {body}";
        Trace.WriteLine(line);
        try
        {
            var path = GetStarApiLogPath();
            lock (LogLock)
            {
                if (!_logPathLogged)
                {
                    _logPathLogged = true;
                    File.AppendAllText(path,
                        $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] [STAR] ogengine.log path: {path}" + Environment.NewLine +
                        $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] [STAR] Diagnostics: full HTTP/API lines are written here; the game console shows short previews only (crash-safe)." +
                        Environment.NewLine);
                }
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch { /* ignore file write errors */ }
    }

    /// <summary>Write to ogengine.log and Trace only; do NOT enqueue for game console. Use for quest API logs so Quake/Doom don't consume them and crash.</summary>
    internal static void StarApiLogFileOnly(string message)
    {
        AppendStarApiDiagnosticsFileLine(message, prefixWithStarTag: false);
    }

    /// <summary>Full line to ogengine.log; console gets a short preview (same directory as ogengine.dll).</summary>
    internal static void StarApiLog(string message)
    {
        var raw = message ?? string.Empty;
        AppendStarApiDiagnosticsFileLine(raw, prefixWithStarTag: false);
        // Quake console is sensitive to high-volume quest lines; keep them file-only.
        if (raw.StartsWith("[Quest]", StringComparison.Ordinal) || raw.StartsWith("[Quests]", StringComparison.Ordinal))
            return;
        if (Encoding.UTF8.GetByteCount(raw) <= MaxConsoleLogUtf8Bytes)
            EnqueueConsoleLog(raw);
        else
        {
            var shortLine = TruncateUtf8ForInterop(raw, Math.Max(64, MaxConsoleLogUtf8Bytes - 48)) + " …[full line in ogengine.log]";
            EnqueueConsoleLog(shortLine);
        }
    }

    /// <summary>Append a line to ogengine.log and enqueue for Doom console so game (e.g. Doom) door-check and debug messages appear in both.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_log_to_file", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiLogToFile(sbyte* message)
    {
        var msg = PtrToString(message);
        if (string.IsNullOrWhiteSpace(msg)) return;
        AppendStarApiDiagnosticsFileLine(msg, prefixWithStarTag: true);
        if (Encoding.UTF8.GetByteCount(msg) <= MaxConsoleLogUtf8Bytes)
            EnqueueConsoleLog(msg);
        else
        {
            var shortLine = TruncateUtf8ForInterop(msg, Math.Max(64, MaxConsoleLogUtf8Bytes - 48)) + " …[full line in ogengine.log]";
            EnqueueConsoleLog(shortLine);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_set_debug", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetDebug(int enabled)
    {
        _starDebug = enabled != 0 ? 1 : 0;
    }

    private static StarApiResultCode FinalizeResult<T>(OASISResult<T> result, int operationType)
    {
        var code = ExtractCode(result);
        if (result.IsError)
            SetError(result.Message ?? "Unknown error.");
        else
            SetError(string.Empty);

        InvokeOperationCallback(code, operationType);
        return code;
    }

    /// <summary>Set last error and return code without invoking the shared callback. Use for auth/set_avatar_id so the game only runs "profile loaded" when ogengine_refresh_avatar_profile completes (cache has XP/quest).</summary>
    private static StarApiResultCode FinalizeResultNoCallback<T>(OASISResult<T> result)
    {
        var code = ExtractCode(result);
        if (result.IsError)
            SetError(result.Message ?? "Unknown error.");
        else
            SetError(string.Empty);
        return code;
    }

    private static StarApiResultCode ExtractCode<T>(OASISResult<T> result)
    {
        if (!result.IsError)
            return StarApiResultCode.Success;

        if (!string.IsNullOrWhiteSpace(result.ErrorCode) && int.TryParse(result.ErrorCode, out var code))
            return Enum.IsDefined(typeof(StarApiResultCode), code) ? (StarApiResultCode)code : StarApiResultCode.ApiError;

        return StarApiResultCode.ApiError;
    }

    private static StarApiResultCode SetErrorAndReturn(string message, StarApiResultCode code, int operationType)
    {
        SetError(message);
        InvokeOperationCallback(code, operationType);
        return code;
    }

    private static void SetError(string message)
    {
        var value = message ?? string.Empty;
        var bytes = Encoding.UTF8.GetBytes(value);
        var buffer = (byte*)NativeMemory.Alloc((nuint)bytes.Length + 1);
        if (buffer is null)
            return;

        new ReadOnlySpan<byte>(bytes).CopyTo(new Span<byte>(buffer, bytes.Length));
        buffer[bytes.Length] = 0;

        lock (NativeStateLock)
        {
            var previous = _lastError;
            _lastError = buffer;
            if (previous is not null)
                NativeMemory.Free(previous);
        }
    }

    private static void InvokeCallback(StarApiResultCode code)
    {
        delegate* unmanaged[Cdecl]<int, void*, void> callback;
        void* callbackUserData;

        lock (NativeStateLock)
        {
            callback = _callback;
            callbackUserData = _callbackUserData;
        }

        if (callback != null)
            callback((int)code, callbackUserData);
    }

    internal static string? PtrToString(sbyte* ptr)
    {
        return ptr is null ? null : Marshal.PtrToStringUTF8((nint)ptr);
    }

    private static void WriteUtf8ToOutput(string value, sbyte* destination, int size)
    {
        if (destination is null || size <= 1)
            return;

        var buffer = new Span<byte>((byte*)destination, size);
        buffer.Clear();
        if (string.IsNullOrEmpty(value))
            return;

        var maxBytes = size - 1;
        if (Encoding.UTF8.GetByteCount(value) > maxBytes)
            value = TruncateUtf8ForInterop(value, maxBytes);

        try
        {
            _ = Encoding.UTF8.GetBytes(value.AsSpan(), buffer[..maxBytes]);
        }
        catch
        {
            /* Last resort: never throw across native boundary */
            const string ell = "…";
            var fallback = TruncateUtf8ForInterop(value, Math.Max(1, maxBytes - Encoding.UTF8.GetByteCount(ell))) + ell;
            try { _ = Encoding.UTF8.GetBytes(fallback.AsSpan(), buffer[..maxBytes]); } catch { /* leave cleared */ }
        }
    }

    private static void WriteFixedUtf8(string value, byte* destination, int size)
    {
        var span = new Span<byte>(destination, size);
        span.Clear();
        if (string.IsNullOrEmpty(value))
            return;

        var maxBytes = size - 1;
        if (maxBytes <= 0) return;
        if (Encoding.UTF8.GetByteCount(value) > maxBytes)
            value = TruncateUtf8ForInterop(value, maxBytes);
        try
        {
            _ = Encoding.UTF8.GetBytes(value.AsSpan(), span[..maxBytes]);
        }
        catch
        {
            /* Field too small for even one char — leave zeroed */
        }
    }

    /// <summary>Shorten a string so its UTF-8 encoding fits in <paramref name="maxUtf8Bytes"/> (avoids GetBytes(span) throwing into native callers).</summary>
    private static string TruncateUtf8ForInterop(string s, int maxUtf8Bytes)
    {
        if (string.IsNullOrEmpty(s) || maxUtf8Bytes <= 0)
            return string.Empty;
        if (Encoding.UTF8.GetByteCount(s) <= maxUtf8Bytes)
            return s;
        var len = s.Length;
        while (len > 0)
        {
            len--;
            if (len > 0 && char.IsLowSurrogate(s[len]) && char.IsHighSurrogate(s[len - 1]))
                len--;
            if (Encoding.UTF8.GetByteCount(s.AsSpan(0, len)) <= maxUtf8Bytes)
                return len == s.Length ? s : s.Substring(0, len);
        }
        return string.Empty;
    }

    /* ── Cross-game teleportation ─────────────────────────────────────────── */

    /// <summary>Request teleport to another game+map. Writes oasis_teleport_{avatarId}.json to %TEMP% for OmniverseKernel pickup.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_request_teleport", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiRequestTeleport(sbyte* targetGame, sbyte* targetMap, float x, float y, float z)
    {
        try
        {
            var client = GetClient();
            if (client is null) return;
            client.RequestTeleport(
                PtrToString(targetGame) ?? string.Empty,
                PtrToString(targetMap) ?? string.Empty,
                x, y, z);
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Teleport] ogengine_request_teleport exception: {ex.Message}"); } catch { /* ignore */ }
        }
    }

    /// <summary>Poll for an incoming teleport request (oasis_teleport_arrive_{avatarId}.json). Returns 1 and fills out_map/out_x/y/z if found; deletes the file. Returns 0 otherwise.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_poll_teleport_request", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiPollTeleportRequest(sbyte* outMap, nuint mapLen, float* outX, float* outY, float* outZ)
    {
        try
        {
            var client = GetClient();
            if (client is null) return 0;
            if (!client.PollTeleportRequest(out var map, out var x, out var y, out var z)) return 0;
            if (outMap != null && mapLen > 0)
                WriteUtf8ToOutput(map ?? string.Empty, outMap, (int)Math.Min(mapLen, (nuint)int.MaxValue));
            if (outX != null) *outX = x;
            if (outY != null) *outY = y;
            if (outZ != null) *outZ = z;
            return 1;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Teleport] ogengine_poll_teleport_request exception: {ex.Message}"); } catch { /* ignore */ }
            return 0;
        }
    }

    /// <summary>Notify STAR API that the avatar has arrived at the teleport destination. Fire-and-forget; posts to /api/teleport/confirm-arrival.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_confirm_teleport_arrival", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiConfirmTeleportArrival()
    {
        try
        {
            var client = GetClient();
            if (client is null) return;
            _ = Task.Run(() =>
            {
                try { client.ConfirmTeleportArrivalAsync(CancellationToken.None).GetAwaiter().GetResult(); }
                catch (Exception ex2) { try { StarApiLogFileOnly($"[Teleport] confirm_teleport_arrival background error: {ex2.Message}"); } catch { /* ignore */ } }
            });
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Teleport] ogengine_confirm_teleport_arrival exception: {ex.Message}"); } catch { /* ignore */ }
        }
    }

    /* ── Cross-game entity spawning ───────────────────────────────────────── */

    /// <summary>Poll for a pending spawn event (oasis_spawn_{avatarId}.json). Returns 1 and fills out_entity_id/out_x/y/z if found; deletes the file. Returns 0 otherwise.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_poll_spawn_event", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiPollSpawnEvent(sbyte* outEntityId, nuint idLen, float* outX, float* outY, float* outZ)
    {
        try
        {
            var client = GetClient();
            if (client is null) return 0;
            if (!client.PollSpawnEvent(out var entityId, out var x, out var y, out var z)) return 0;
            if (outEntityId != null && idLen > 0)
                WriteUtf8ToOutput(entityId ?? string.Empty, outEntityId, (int)Math.Min(idLen, (nuint)int.MaxValue));
            if (outX != null) *outX = x;
            if (outY != null) *outY = y;
            if (outZ != null) *outZ = z;
            return 1;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Spawn] ogengine_poll_spawn_event exception: {ex.Message}"); } catch { /* ignore */ }
            return 0;
        }
    }

    /// <summary>Notify STAR API that the named cross-game entity has been spawned. Fire-and-forget; posts to /api/spawn-events/confirm.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_confirm_spawn", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiConfirmSpawn(sbyte* entityId)
    {
        try
        {
            var client = GetClient();
            if (client is null) return;
            var id = PtrToString(entityId) ?? string.Empty;
            _ = Task.Run(() =>
            {
                try { client.ConfirmSpawnAsync(id, CancellationToken.None).GetAwaiter().GetResult(); }
                catch (Exception ex2) { try { StarApiLogFileOnly($"[Spawn] confirm_spawn background error: {ex2.Message}"); } catch { /* ignore */ } }
            });
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Spawn] ogengine_confirm_spawn exception: {ex.Message}"); } catch { /* ignore */ }
        }
    }

    /* ── Portal unlock notification ──────────────────────────────────────── */

    /// <summary>Notify OGEditor/OmniverseKernel that a portal has been unlocked. Writes oasis_portal_unlock_{portalId}.json to %TEMP%.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_notify_portal_unlock", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiNotifyPortalUnlock(sbyte* portalId)
    {
        try
        {
            var client = GetClient();
            if (client is null) return;
            var id = PtrToString(portalId) ?? string.Empty;
            client.NotifyPortalUnlock(id);
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[Portal] ogengine_notify_portal_unlock exception: {ex.Message}"); } catch { /* ignore */ }
        }
    }

    /* ── Cross-game objective events (ShowNarration, PlayAudio, PlayVideo, OpenWebsite, UnlockPortal) ─── */

    /// <summary>
    /// Poll for the next pending cross-game event from quest progress (ShowNarration, PlayAudio, PlayVideo, OpenWebsite, UnlockPortal).
    /// Returns 1 and writes event JSON to out_json if available; returns 0 otherwise.
    /// SpawnEntity and TeleportTo are routed to their dedicated exports (ogengine_poll_spawn_event / ogengine_poll_teleport_request) — not here.
    /// JSON keys: EventType, TargetGame, TargetMap, NarrationText, AudioUrl, AudioTitle, VideoUrl, VideoTitle, WebsiteUrl, PortalId.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_poll_cross_game_event", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiPollCrossGameEvent(sbyte* outJson, nuint bufLen)
    {
        try
        {
            if (outJson == null || bufLen == 0) return 0;
            if (!_pendingCrossGameEvents.TryDequeue(out var json) || string.IsNullOrEmpty(json)) return 0;
            WriteUtf8ToOutput(json, outJson, (int)Math.Min(bufLen, (nuint)int.MaxValue));
            return 1;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[CrossGameEvent] ogengine_poll_cross_game_event exception: {ex.Message}"); } catch { /* ignore */ }
            return 0;
        }
    }

    /// <summary>
    /// Poll for the next inventory item GUID to grant the avatar (from objective/quest completion RewardInventoryItemIds).
    /// Returns 1 and writes the GUID string to out_guid if available; returns 0 otherwise.
    /// Call ogengine_get_inventory after granting to refresh the local inventory display.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_poll_inventory_grant", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiPollInventoryGrant(sbyte* outGuid, nuint bufLen)
    {
        try
        {
            if (outGuid == null || bufLen == 0) return 0;
            if (!_pendingInventoryGrants.TryDequeue(out var guid) || string.IsNullOrEmpty(guid)) return 0;
            WriteUtf8ToOutput(guid, outGuid, (int)Math.Min(bufLen, (nuint)int.MaxValue));
            return 1;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[CrossGameEvent] ogengine_poll_inventory_grant exception: {ex.Message}"); } catch { /* ignore */ }
            return 0;
        }
    }

    /* ── Map entity list ──────────────────────────────────────────────────── */

    /// <summary>Fetch the cross-game entity list for a given map from STAR API. Writes JSON array to out_json buffer.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_map_entities", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetMapEntities(sbyte* gameId, sbyte* mapName, sbyte* outJson, nuint bufLen)
    {
        try
        {
            if (outJson is null || bufLen == 0)
                return (int)SetErrorAndReturn("outJson and bufLen must be non-null/non-zero.", StarApiResultCode.InvalidParam, StarApiOpGetMapEntities);
            var client = GetClient();
            if (client is null)
                return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetMapEntities);
            var gId = PtrToString(gameId) ?? string.Empty;
            var mName = PtrToString(mapName) ?? string.Empty;
            var result = client.GetMapEntitiesAsync(gId, mName, CancellationToken.None).GetAwaiter().GetResult();
            if (result.IsError)
                return (int)SetErrorAndReturn(result.Message ?? "Failed to get map entities.", StarApiResultCode.ApiError, StarApiOpGetMapEntities);
            var json = result.Result ?? "[]";
            WriteUtf8ToOutput(json, outJson, (int)Math.Min(bufLen, (nuint)int.MaxValue));
            SetError(string.Empty);
            InvokeOperationCallback(StarApiResultCode.Success, StarApiOpGetMapEntities);
            return (int)StarApiResultCode.Success;
        }
        catch (Exception ex)
        {
            try { StarApiLogFileOnly($"[MapEntities] ogengine_get_map_entities exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetMapEntities);
        }
    }
}
