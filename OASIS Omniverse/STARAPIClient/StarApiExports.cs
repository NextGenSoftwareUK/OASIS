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

public static unsafe class StarApiExports
{
    private static readonly object Sync = new();
    private static readonly object NativeStateLock = new();
    private static readonly object BackgroundErrorLock = new();
    private static string? _lastBackgroundError;
    private static readonly ConcurrentQueue<string> _consoleLogQueue = new();
    private const int MaxConsoleLogMessages = 64;
    /// <summary>Cap UTF-8 size for queued console lines so star_api_consume_console_log never hits GetBytes buffer-too-small (native crash).</summary>
    private const int MaxConsoleLogUtf8Bytes = 1536;
    /// <summary>Per-line cap for star_api.log (file only). Large enough for API bodies; avoids multi-megabyte single-line OOM.</summary>
    private const int MaxStarApiFileLineUtf8Bytes = 1_048_576;
    private static StarApiClient? _client;
    private static byte* _lastError;
    private static delegate* unmanaged[Cdecl]<int, void*, void> _callback;
    private static void* _callbackUserData;
    /// <summary>Optional: (result, operation_type, user_data). When set, profile refresh uses this instead of _callback so the game can filter by operation_type.</summary>
    private static delegate* unmanaged[Cdecl]<int, int, void*, void> _operationCallback;
    private static void* _operationCallbackUserData;
    private static volatile int _starDebug;
    private static int StarApiGetQuestsStringLastLoggedToCopy = -1;
    private static bool _topLevelQuestsLastLoggedLoading;

    /// <summary>Whether STAR debug logging is on (games set via star_api_set_debug). When true, quest API and other requests log URI and response to file and console.</summary>
    internal static bool GetStarDebug() => _starDebug != 0;

    /// <summary>Set the last background error (mint/add_item failure or pickup not queued). Consumed by star_api_consume_last_background_error for game console display.</summary>
    public static void SetLastBackgroundError(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        lock (BackgroundErrorLock)
            _lastBackgroundError = message;
    }

    /// <summary>Return and clear the last background error. Used by star_api_consume_last_background_error.</summary>
    public static string? TryConsumeLastBackgroundError()
    {
        lock (BackgroundErrorLock)
        {
            var msg = _lastBackgroundError;
            _lastBackgroundError = null;
            return msg;
        }
    }

    /// <summary>Enqueue a message for the game console (consumed by star_api_consume_console_log).</summary>
    public static void EnqueueConsoleLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        /* Games pass a small native buffer (~512 bytes). Encoding.UTF8.GetBytes throws if the string is larger than the span — that aborts the process inside UnmanagedCallersOnly. */
        message = TruncateUtf8ForInterop(message, MaxConsoleLogUtf8Bytes);
        while (_consoleLogQueue.Count >= MaxConsoleLogMessages && _consoleLogQueue.TryDequeue(out _)) { }
        _consoleLogQueue.Enqueue(message);
    }

    /// <summary>Dequeue one console log message for the game to display. Used by star_api_consume_console_log.</summary>
    public static string? TryConsumeConsoleLog()
    {
        return _consoleLogQueue.TryDequeue(out var msg) ? msg : null;
    }

    static StarApiExports()
    {
        SetError(string.Empty);
    }

    /// <summary>Trimmer root: keep all members so session/JWT exports stay in star_api.dll for forwarders and autologin.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StarApiExports))]
    [DynamicDependency("StarApiGetCurrentJwt", typeof(StarApiExports))]
    [DynamicDependency("StarApiGetCurrentRefreshToken", typeof(StarApiExports))]
    [DynamicDependency("StarApiGetCurrentUsername", typeof(StarApiExports))]
    [DynamicDependency("StarApiSetSavedSession", typeof(StarApiExports))]
    [DynamicDependency("StarApiSetRefreshToken", typeof(StarApiExports))]
    [DynamicDependency("StarApiIsSessionExpired", typeof(StarApiExports))]
    [DynamicDependency("StarApiRestoreSession", typeof(StarApiExports))]
    [DynamicDependency("StarApiAuthenticateWithJwtOut", typeof(StarApiExports))]
    [UnmanagedCallersOnly(EntryPoint = "star_api_init", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiInit(star_api_config_t* config)
    {
        if (config is null)
            return (int)SetErrorAndReturn("Invalid configuration.", StarApiResultCode.InvalidParam, StarApiOpInit);

        int tr = config->transport;
        if (tr == (int)StarApiTransport.Native)
            return (int)SetErrorAndReturn(
                "Native STAR transport is not available in this star_api build. Use star_transport \"remote\" with WEB5/WEB4 URLs, or a native OASIS host that implements star_api_init with HyperDrive.",
                StarApiResultCode.InitFailed,
                StarApiOpInit);
        if (tr != 0 && tr != (int)StarApiTransport.Remote)
            return (int)SetErrorAndReturn("Invalid star_api_config_t.transport value.", StarApiResultCode.InvalidParam, StarApiOpInit);

        if (config->base_url is null)
            return (int)SetErrorAndReturn("Invalid configuration.", StarApiResultCode.InvalidParam, StarApiOpInit);

        var baseUrl = PtrToString(config->base_url) ?? string.Empty;
        var managedConfig = new StarApiConfig
        {
            Web5StarApiBaseUrl = baseUrl,
            ApiKey = PtrToString(config->api_key),
            AvatarId = PtrToString(config->avatar_id),
            TimeoutSeconds = config->timeout_seconds,
            ClientGameSource = config->client_game_source != null ? PtrToString(config->client_game_source) : null,
            Transport = StarApiTransport.Remote,
            OasisDnaPath = config->oasis_dna_path != null ? PtrToString(config->oasis_dna_path) : null
        };

        lock (Sync)
        {
            _client?.Dispose();
            _client = new StarApiClient();
        }

        var result = _client.Init(managedConfig);
        return (int)FinalizeResult(result, StarApiOpInit);
    }

    /// <summary>0 = merge progress into local quest cache after OK (no GET). 1 = GET all quests after each OK. Call after star_api_init; safe anytime.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_set_quest_progress_cache_refresh", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetQuestProgressCacheRefresh(int mode)
    {
        var client = GetClient();
        if (client is null) return;
        client.SetQuestProgressCacheRefreshMode(mode != 0 ? QuestProgressCacheRefreshMode.FullServerRefresh : QuestProgressCacheRefreshMode.ClientCacheMerge);
    }

    /// <summary>Shared implementation for authenticate; used by both star_api_authenticate and star_api_authenticate_with_jwt_out (UnmanagedCallersOnly cannot call UnmanagedCallersOnly).</summary>
    private static int AuthenticateWithJwtOutImpl(sbyte* username, sbyte* password, sbyte* jwt_buf, nuint jwt_size)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpAuthenticate);

        var user = PtrToString(username);
        var pass = PtrToString(password);
        var result = client.AuthenticateAsync(user ?? string.Empty, pass ?? string.Empty).GetAwaiter().GetResult();
        if (!result.IsError && jwt_buf != null && jwt_size > 0)
        {
            var jwt = client.GetCurrentJwt();
            if (!string.IsNullOrEmpty(jwt))
            {
                var bytes = Encoding.UTF8.GetBytes(jwt);
                var toCopy = (int)Math.Min((nuint)bytes.Length, jwt_size - 1);
                if (toCopy > 0)
                    new ReadOnlySpan<byte>(bytes, 0, toCopy).CopyTo(new Span<byte>(jwt_buf, toCopy));
                jwt_buf[toCopy] = 0;
            }
            else
                jwt_buf[0] = 0;
        }
        return (int)FinalizeResultNoCallback(result);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_authenticate", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiAuthenticate(sbyte* username, sbyte* password)
    {
        return AuthenticateWithJwtOutImpl(username, password, null, 0);
    }

    /// <summary>Authenticate and optionally write JWT to buf so games can persist to oasisstar.json without relying on get_current_jwt export.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_authenticate_with_jwt_out", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiAuthenticateWithJwtOut(sbyte* username, sbyte* password, sbyte* jwt_buf, nuint jwt_size)
    {
        return AuthenticateWithJwtOutImpl(username, password, jwt_buf, jwt_size);
    }

    /// <summary>Set JWT from persisted session (e.g. oasisstar.json). Call star_api_restore_session to validate and load profile.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_set_saved_session", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSetSavedSession(sbyte* jwt)
    {
        var client = GetClient();
        if (client is null) return (int)StarApiResultCode.NotInitialized;
        var jwtStr = PtrToString(jwt);
        var result = client.SetSavedSession(jwtStr ?? string.Empty);
        return result.IsError ? (int)StarApiResultCode.ApiError : (int)StarApiResultCode.Success;
    }

    /// <summary>Start async session restore (GET avatar/current). Callback is invoked on success/failure. Does not block.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_restore_session", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiRestoreSession()
    {
        var client = GetClient();
        if (client is null) return (int)StarApiResultCode.NotInitialized;
        _ = client.QueueRestoreSessionAsync(CancellationToken.None);
        return (int)StarApiResultCode.Success; /* restore started; callback will fire when done */
    }

    /// <summary>Write current username to buf (for saving to oasisstar.json). Returns bytes written or 0.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_current_username", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetCurrentUsername(sbyte* buf, nuint bufSize)
    {
        if (buf is null || bufSize == 0) return 0;
        var client = GetClient();
        var name = client?.GetCurrentUsername();
        if (string.IsNullOrEmpty(name)) { buf[0] = 0; return 0; }
        var bytes = Encoding.UTF8.GetBytes(name);
        var toCopy = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
        if (toCopy > 0) new ReadOnlySpan<byte>(bytes, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
        buf[toCopy] = 0;
        return toCopy;
    }

    /// <summary>Write current JWT to buf (for saving to oasisstar.json). Returns bytes written or 0. Caller should not log.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_current_jwt", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetCurrentJwt(sbyte* buf, nuint bufSize)
    {
        if (buf is null || bufSize == 0) return 0;
        var client = GetClient();
        var jwt = client?.GetCurrentJwt();
        if (string.IsNullOrEmpty(jwt))
        {
            buf[0] = 0;
            return 0;
        }
        StarApiExports.StarApiLogFileOnly($"[Auth] GetCurrentJwt: returning length={jwt.Length} (for oasisstar.json)");
        var bytes = Encoding.UTF8.GetBytes(jwt);
        var toCopy = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
        if (toCopy > 0) new ReadOnlySpan<byte>(bytes, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
        buf[toCopy] = 0;
        return toCopy;
    }

    /// <summary>Set refresh token from persisted session (e.g. oasisstar.json). Call after star_api_set_saved_session when restoring so 401 can trigger token refresh.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_set_refresh_token", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetRefreshToken(sbyte* refreshToken)
    {
        var client = GetClient();
        if (client is null) return;
        var s = refreshToken is null ? null : PtrToString(refreshToken);
        client.SetRefreshToken(s);
    }

    /// <summary>Write current refresh token to buf (for saving to oasisstar.json). Returns bytes written or 0. Caller should not log.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_current_refresh_token", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetCurrentRefreshToken(sbyte* buf, nuint bufSize)
    {
        if (buf is null || bufSize == 0) return 0;
        var client = GetClient();
        var token = client?.GetCurrentRefreshToken();
        if (string.IsNullOrEmpty(token))
        {
            buf[0] = 0;
            return 0;
        }
        var bytes = Encoding.UTF8.GetBytes(token);
        var toCopy = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
        if (toCopy > 0) new ReadOnlySpan<byte>(bytes, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
        buf[toCopy] = 0;
        return toCopy;
    }

    /// <summary>Returns 1 if session was cleared due to expired JWT and refresh failed (or no refresh token). Games should clear jwt_token/refresh_token in oasisstar.json when saving so the next launch does not try to restore a dead session.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_is_session_expired", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiIsSessionExpired()
    {
        var client = GetClient();
        return (client != null && client.IsSessionExpired()) ? 1 : 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_cleanup", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiCleanup()
    {
        lock (Sync)
        {
            _client?.Dispose();
            _client = null;
        }
    }

    /// <summary>Native export for star_api_has_item. Prefer checking already-loaded inventory (local cache) for optimization; use this as last resort.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_has_item", CallConvs = [typeof(CallConvCdecl)])]
    public static byte StarApiHasItem(sbyte* itemName)
    {
        var client = GetClient();
        if (client is null)
        {
            SetError("Client is not initialized.");
            InvokeOperationCallback(StarApiResultCode.NotInitialized, StarApiOpHasItem);
            return 0;
        }

        var result = client.HasItemAsync(PtrToString(itemName) ?? string.Empty).GetAwaiter().GetResult();
        var code = FinalizeResult(result, StarApiOpHasItem);
        return code == StarApiResultCode.Success && result.Result ? (byte)1 : (byte)0;
    }

    /// <summary>Coerce item_type for native games: holons may use enum names containing "Weapon" for monster NFTs. Monster mint rows use description "Monster defeated in ...".</summary>
    private static string GetNativeItemType(StarItem src)
    {
        if (!string.IsNullOrEmpty(src.Description) && src.Description.Contains("Monster defeated", StringComparison.OrdinalIgnoreCase))
            return "Monster";
        return src.ItemType ?? string.Empty;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_get_inventory", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetInventory(star_item_list_t** itemList)
    {
        if (itemList is null)
            return (int)SetErrorAndReturn("itemList must not be null.", StarApiResultCode.InvalidParam, StarApiOpGetInventory);

        *itemList = null;

        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetInventory);

        var cached = client.TryGetCachedInventory();
        if (cached is null)
            return (int)SetErrorAndReturn("Inventory not loaded. Call star_api_request_inventory_in_background first.", StarApiResultCode.ApiError, StarApiOpGetInventory);

        var count = (nuint)cached.Count;
        var listPtr = (star_item_list_t*)NativeMemory.Alloc((nuint)1, (nuint)sizeof(star_item_list_t));
        if (listPtr is null)
            return (int)SetErrorAndReturn("Memory allocation failed for item list.", StarApiResultCode.InitFailed, StarApiOpGetInventory);

        listPtr->count = count;
        listPtr->capacity = count;
        listPtr->items = null;

        if (count > 0)
        {
            listPtr->items = (star_item_t*)NativeMemory.Alloc(count, (nuint)sizeof(star_item_t));
            if (listPtr->items is null)
            {
                NativeMemory.Free(listPtr);
                return (int)SetErrorAndReturn("Memory allocation failed for inventory items.", StarApiResultCode.InitFailed, StarApiOpGetInventory);
            }

            for (var i = 0; i < cached.Count; i++)
            {
                var src = cached[i];
                var dst = &listPtr->items[i];
                WriteFixedUtf8(src.Id.ToString(), dst->id, 64);
                WriteFixedUtf8(src.Name, dst->name, 256);
                WriteFixedUtf8(src.Description, dst->description, 512);
                WriteFixedUtf8(src.GameSource, dst->game_source, 64);
                WriteFixedUtf8(GetNativeItemType(src), dst->item_type, 64);
                WriteFixedUtf8(src.NftId ?? string.Empty, dst->nft_id, 128);
                dst->quantity = src.Quantity;
            }
        }

        *itemList = listPtr;
        SetError(string.Empty);
        return (int)StarApiResultCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_request_inventory_in_background", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiRequestInventoryInBackground()
    {
        var client = GetClient();
        if (client is null)
        {
            // Defer callback so the export never blocks the game thread (avoids hang on Linux when not connected).
            _ = Task.Run(() => StarApiExports.InvokeOperationCallback(StarApiResultCode.NotInitialized, StarApiExports.StarApiOpGetInventory));
            return;
        }
        client.RequestInventoryInBackground();
    }

    /// <summary>Internal helper for StarSyncExports; same logic as star_api_free_item_list so C# can call without going through UnmanagedCallersOnly.</summary>
    internal static unsafe void FreeItemListInternal(star_item_list_t* itemList)
    {
        if (itemList is null)
            return;

        if (itemList->items is not null)
            NativeMemory.Free(itemList->items);

        NativeMemory.Free(itemList);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_free_item_list", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiFreeItemList(star_item_list_t* itemList)
    {
        FreeItemListInternal(itemList);
    }

    /// <summary>Write serialized quest list (InProgress) to buf for game UI. Returns cached data immediately (never blocks). If cache is empty, starts a background refresh and returns "Loading...". Format: "Q\tid\tname\tdesc\tstatus\tpct\n" per quest, "O\tid\tdesc\tdone\n" per objective, "---\n" between quests. Returns bytes written (excluding null), or negative StarApiResultCode on error. Must not throw - native caller can crash.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_quests_string", CallConvs = [typeof(CallConvCdecl)])]
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
                        StarApiLogFileOnly("[Quests] star_api_get_quests_string: cache miss, returning Loading...");
                    else
                    {
                        var previewLen = Math.Min(250, fallback.Length);
                        var preview = previewLen > 0 ? fallback.Substring(0, previewLen).Replace("\t", "|").Replace("\r", " ").Replace("\n", "\\n") : "";
                        StarApiLogFileOnly($"[Quests] star_api_get_quests_string: cache HIT len={fallback.Length} toCopy={toCopy} preview={preview}");
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
            try { StarApiLogFileOnly($"[Quests] star_api_get_quests_string exception: {ex.Message}"); } catch { /* ignore */ }
            try
            {
                const string err = "Error: Error loading quests. Check console or star_api.log for details.";
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
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_top_level_quests_string", CallConvs = [typeof(CallConvCdecl)])]
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
                try { if (!_topLevelQuestsLastLoggedLoading) { _topLevelQuestsLastLoggedLoading = true; StarApiLogFileOnly("[STAR] star_api_get_top_level_quests_string cache miss -> Loading... (once until cache fills)"); } } catch { }
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
            try { StarApiLogFileOnly($"[Quests] star_api_get_top_level_quests_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetTopLevelQuestsString);
        }
    }

    /// <summary>Write serialized sub-quests of parent_quest_id to buf for right panel. Same format as get_quests_string. Returns bytes written or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_quest_sub_quests_string", CallConvs = [typeof(CallConvCdecl)])]
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
            try { StarApiLogFileOnly($"[Quests] star_api_get_quest_sub_quests_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetQuestSubQuestsString);
        }
    }

    /// <summary>Write serialized objectives from the quest's Objectives collection (Quest.Objectives) for parent_quest_id to buf for right panel. Same format as get_quests_string. Returns bytes written or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_quest_objectives_string", CallConvs = [typeof(CallConvCdecl)])]
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
            try { StarApiLogFileOnly($"[Quests] star_api_get_quest_objectives_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetQuestObjectivesString);
        }
    }

    /// <summary>Return objectives cache version; increments when on-demand fetch merges objectives. UI should poll each frame and re-call get_quest_objectives_string when this changes to refresh the list.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_quest_objectives_cache_version", CallConvs = [typeof(CallConvCdecl)])]
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
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_quest_prereqs_string", CallConvs = [typeof(CallConvCdecl)])]
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
            try { StarApiLogFileOnly($"[Quests] star_api_get_quest_prereqs_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetQuestPrereqsString);
        }
    }

    /// <summary>Write requirement/progress lines for quest and optional objective to buf. Returns bytes written or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_quest_objective_requirements_string", CallConvs = [typeof(CallConvCdecl)])]
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
            try { StarApiLogFileOnly($"[Quests] star_api_get_quest_objective_requirements_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetQuestObjectiveRequirementsString);
        }
    }

    /// <summary>Write one progress line per objective for the tracker. Returns bytes written or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_quest_tracker_objectives_string", CallConvs = [typeof(CallConvCdecl)])]
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
            try { StarApiLogFileOnly($"[Quests] star_api_get_quest_tracker_objectives_string exception: {ex.Message}"); } catch { /* ignore */ }
            return (int)SetErrorAndReturn(ex.Message ?? "Unknown error", StarApiResultCode.ApiError, StarApiOpGetQuestTrackerObjectivesString);
        }
    }

    /// <summary>Return 0-based index of first incomplete objective for the tracked quest.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_quest_tracker_active_objective_index", CallConvs = [typeof(CallConvCdecl)])]
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

    [UnmanagedCallersOnly(EntryPoint = "star_api_invalidate_inventory_cache", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiInvalidateInventoryCache()
    {
        var client = GetClient();
        client?.InvalidateInventoryCache();
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_invalidate_quest_cache", CallConvs = [typeof(CallConvCdecl)])]
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
    [UnmanagedCallersOnly(EntryPoint = "star_api_refresh_quest_cache_in_background", CallConvs = [typeof(CallConvCdecl)])]
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

    [UnmanagedCallersOnly(EntryPoint = "star_api_set_quest_popup_open", CallConvs = [typeof(CallConvCdecl)])]
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

    [UnmanagedCallersOnly(EntryPoint = "star_api_clear_cache", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiClearCache()
    {
        var client = GetClient();
        client?.ClearCache();
    }

    /// <summary>Add item to avatar inventory. quantity: amount to add; stack: 1 = increment if exists, 0 = error if exists.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_add_item", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiAddItem(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, sbyte* nftId, int quantity, int stack)
    {
        var client = GetClient();
        if (client is null)
        {
            StarApiLog("star_api_add_item: client is null");
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpAddItem);
        }

        var name = PtrToString(itemName) ?? string.Empty;
        var desc = PtrToString(description) ?? string.Empty;
        var source = PtrToString(gameSource) ?? string.Empty;
        var type = PtrToString(itemType) ?? "KeyItem";
        var nftIdStr = PtrToString(nftId);
        var nftIdOpt = string.IsNullOrWhiteSpace(nftIdStr) ? null : nftIdStr;
        var qty = quantity < 1 ? 1 : quantity;
        var doStack = stack != 0;

        StarApiLog($"star_api_add_item: name='{name}' quantity={qty} stack={doStack} (calling AddItemAsync on thread pool)");

        var result = Task.Run(() => client.AddItemAsync(name, desc, source, type, nftIdOpt, qty, doStack).GetAwaiter().GetResult()).GetAwaiter().GetResult();

        var code = FinalizeResult(result, StarApiOpAddItem);
        StarApiLog($"star_api_add_item: result IsError={result.IsError} code={(int)code} message={result.Message ?? "(ok)"}");
        return (int)code;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_add_item", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueAddItem(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, sbyte* nftId, int quantity, int stack)
    {
        var client = GetClient();
        if (client is null)
            return;
        var nftIdStr = PtrToString(nftId);
        var qty = quantity < 1 ? 1 : quantity;
        var doStack = stack != 0;
        client.EnqueueAddItemJobOnly(
            PtrToString(itemName) ?? string.Empty,
            PtrToString(description) ?? string.Empty,
            PtrToString(gameSource) ?? string.Empty,
            PtrToString(itemType) ?? "KeyItem",
            string.IsNullOrWhiteSpace(nftIdStr) ? null : nftIdStr,
            qty,
            doStack);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_quest_progress_from_pickup", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueQuestProgressFromPickup(sbyte* gameSource, sbyte* itemType, sbyte* itemName)
    {
        var client = GetClient();
        if (client is null)
        {
            try { StarApiExports.StarApiLogFileOnly("[Quest] star_api_queue_quest_progress_from_pickup: no client"); } catch { /* ignore */ }
            return;
        }
        var gs = PtrToString(gameSource);
        if (string.IsNullOrWhiteSpace(gs)) gs = "ODOOM";
        var it = PtrToString(itemType);
        var name = PtrToString(itemName);
        try { StarApiExports.StarApiLogFileOnly($"[Quest] star_api_queue_quest_progress_from_pickup: gs={gs} itemType={it ?? ""} itemName={name ?? ""}"); } catch { /* ignore */ }
        var keysDeltaPickup = !string.IsNullOrWhiteSpace(it) && it.IndexOf("Key", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0;
        client.EnqueueQuestProgressFromGame(gs, 0, 0, name, keysDeltaPickup, 1, null, it);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_add_xp", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueAddXp(int amount)
    {
        var client = GetClient();
        if (client is null) return;
        if (amount < 0) return;
        client.EnqueueAddXpJobOnly(amount);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_monster_kill", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueMonsterKill(sbyte* engineName, sbyte* displayName, int xp, int isBoss, int doMint, sbyte* provider, sbyte* gameSource)
    {
        var client = GetClient();
        if (client is null) return;
        client.EnqueueMonsterKillJobOnly(
            PtrToString(engineName) ?? string.Empty,
            PtrToString(displayName) ?? string.Empty,
            xp,
            isBoss != 0,
            doMint != 0,
            PtrToString(provider),
            PtrToString(gameSource));
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_quest_level_time", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueQuestLevelTime(sbyte* gameSource, int levelElapsedSeconds)
    {
        var client = GetClient();
        if (client is null || levelElapsedSeconds < 0) return;
        var gs = PtrToString(gameSource);
        if (string.IsNullOrWhiteSpace(gs)) gs = "Quake";
        client.EnqueueQuestProgressFromGame(gs, 0, 0, null, 0, 0, levelElapsedSeconds);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_get_avatar_xp", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetAvatarXp(int* xpOut)
    {
        var client = GetClient();
        if (client is null) { try { StarApiExports.StarApiLogFileOnly("[STAR] star_api_get_avatar_xp: no client"); } catch { } return 0; }
        var xp = client.GetCachedAvatarXp();
        if (xpOut is not null)
            *xpOut = xp;
        return 1;
    }

    // REDUNDANT / REMOVED: star_api_refresh_avatar_xp was a duplicate. Use star_api_refresh_avatar_profile() only.
    // [UnmanagedCallersOnly(EntryPoint = "star_api_refresh_avatar_xp", ...)]
    // public static void StarApiRefreshAvatarXp() { ... }

    /// <summary>Kick off avatar profile refresh (XP + active quest/objective) in background; returns immediately. Callback is invoked when the load completes. Call on beam-in so the game can update the tracker in the callback.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_refresh_avatar_profile", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiRefreshAvatarProfile()
    {
        try { StarApiLogFileOnly("[STAR] star_api_refresh_avatar_profile called"); } catch { }
        var client = GetClient();
        if (client is null) return;
        client.RefreshAvatarProfileInBackground();
    }

    /// <summary>Get display name of the current tracked quest from cache (so HUD can show name as soon as quest list loads after beam-in). Writes UTF-8 name to buf, null-terminated. Returns bytes written (excluding null), or 0 if not available.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_tracker_quest_name", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetTrackerQuestName(sbyte* buf, nuint bufSize)
    {
        if (buf is null || bufSize == 0) return 0;
        var client = GetClient();
        if (client is null) return 0;
        var name = client.TryGetTrackerQuestNameFromCache();
        if (string.IsNullOrEmpty(name)) return 0;
        var bytes = Encoding.UTF8.GetBytes(name);
        var toCopy = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
        if (toCopy > 0) new ReadOnlySpan<byte>(bytes, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
        buf[toCopy] = 0;
        return toCopy;
    }

    private static Guid? _lastLoggedActiveQuestId;
    private static Guid? _lastLoggedActiveObjectiveId;

    /// <summary>Get last active quest ID from avatar detail (restored after beam-in). Writes GUID string to buf, null-terminated. Returns 1 if had value, 0 otherwise.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_active_quest_id", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetActiveQuestId(sbyte* buf, nuint bufSize)
    {
        if (buf is null || bufSize == 0) return 0;
        var client = GetClient();
        if (client is null) return 0;
        var id = client.GetCachedActiveQuestId();
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            _lastLoggedActiveQuestId = null;
            buf[0] = 0; return 0;
        }
        if (!_lastLoggedActiveQuestId.HasValue || _lastLoggedActiveQuestId.Value != id.Value)
        {
            try { StarApiExports.StarApiLog($"[Quest] star_api_get_active_quest_id: returning {id}"); } catch { }
            _lastLoggedActiveQuestId = id.Value;
        }
        var str = id.Value.ToString();
        var bytes = Encoding.UTF8.GetBytes(str);
        var toCopy = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
        if (toCopy > 0) new ReadOnlySpan<byte>(bytes, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
        buf[toCopy] = 0;
        return 1;
    }

    /// <summary>Get last active objective ID from avatar detail (restored after beam-in). Writes GUID string to buf, null-terminated. Returns 1 if had value, 0 otherwise.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_get_active_objective_id", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetActiveObjectiveId(sbyte* buf, nuint bufSize)
    {
        if (buf is null || bufSize == 0) return 0;
        var client = GetClient();
        if (client is null) return 0;
        var id = client.GetCachedActiveObjectiveId();
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            _lastLoggedActiveObjectiveId = null;
            buf[0] = 0; return 0;
        }
        if (!_lastLoggedActiveObjectiveId.HasValue || _lastLoggedActiveObjectiveId.Value != id.Value)
        {
            try { StarApiExports.StarApiLog($"[Quest] star_api_get_active_objective_id: returning {id}"); } catch { }
            _lastLoggedActiveObjectiveId = id.Value;
        }
        var str = id.Value.ToString();
        var bytes = Encoding.UTF8.GetBytes(str);
        var toCopy = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
        if (toCopy > 0) new ReadOnlySpan<byte>(bytes, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
        buf[toCopy] = 0;
        return 1;
    }

    /// <summary>Persist active quest and objective on avatar detail (restored after beam-in). quest_id and objective_id can be null/empty to clear. Call when user sets tracker in game.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_set_active_quest", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSetActiveQuest(sbyte* questId, sbyte* objectiveId)
    {
        var client = GetClient();
        if (client is null) return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpSetActiveQuest);
        Guid? q = null;
        Guid? o = null;
        var qStr = PtrToString(questId);
        if (!string.IsNullOrWhiteSpace(qStr) && Guid.TryParse(qStr, out var qGuid)) q = qGuid;
        var oStr = PtrToString(objectiveId);
        if (!string.IsNullOrWhiteSpace(oStr) && Guid.TryParse(oStr, out var oGuid)) o = oGuid;
        try { StarApiExports.StarApiLog($"[Quest] star_api_set_active_quest called from native (user set tracker in game): questId={qStr ?? "(null)"}, objectiveId={oStr ?? "(null)"}"); } catch { }
        try { StarApiExports.StarApiLogFileOnly($"[Quest] star_api_set_active_quest (native): questId={qStr ?? "(null)"}, objectiveId={oStr ?? "(null)"}"); } catch { }
        var result = client.SetActiveQuestAndObjectiveAsync(q, o).GetAwaiter().GetResult();
        try { StarApiExports.StarApiLog($"[Quest] star_api_set_active_quest result: IsError={result?.IsError}, Message={result?.Message}"); } catch { }
        return (int)FinalizeResult(result, StarApiOpSetActiveQuest);
    }

    /// <summary>Queue pickup with optional mint; C# client does mint (if do_mint) then add_item in background. Same pattern as queue_add_item.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_pickup_with_mint", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueuePickupWithMint(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, int doMint, sbyte* provider, sbyte* sendToAddressAfterMinting, int quantity)
    {
        var client = GetClient();
        if (client is null)
        {
            SetLastBackgroundError("STAR: Pickup not queued (client not initialized).");
            return;
        }
        var qty = quantity < 1 ? 1 : quantity;
        client.EnqueuePickupWithMintJobOnly(
            PtrToString(itemName) ?? string.Empty,
            PtrToString(description) ?? string.Empty,
            PtrToString(gameSource) ?? string.Empty,
            PtrToString(itemType) ?? "KeyItem",
            doMint != 0,
            PtrToString(provider),
            PtrToString(sendToAddressAfterMinting),
            qty);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_flush_add_item_jobs", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiFlushAddItemJobs()
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpFlushAddItemJobs);
        var result = client.FlushAddItemJobsAsync(CancellationToken.None).GetAwaiter().GetResult();
        return (int)FinalizeResult(result, StarApiOpFlushAddItemJobs);
    }

    /// <summary>Mint an NFT for an inventory item via WEB4 OASIS API (NFTHolon). Returns NFT ID to pass to star_api_add_item as nft_id. Optional hash_out for tx hash/signature. provider defaults to SolanaOASIS. Note: mint is currently synchronous (blocking); add_item is queued and flushed async.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_mint_inventory_nft", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiMintInventoryNft(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, sbyte* provider, sbyte* nftIdOut, sbyte* hashOut, sbyte* sendToAddressAfterMinting)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpMintInventoryNft);
        if (nftIdOut is null)
            return (int)SetErrorAndReturn("nftIdOut buffer must not be null.", StarApiResultCode.InvalidParam, StarApiOpMintInventoryNft);

        var result = client.MintInventoryItemNftAsync(
            PtrToString(itemName) ?? string.Empty,
            PtrToString(description),
            PtrToString(gameSource) ?? string.Empty,
            PtrToString(itemType) ?? "KeyItem",
            PtrToString(provider),
            PtrToString(sendToAddressAfterMinting)).GetAwaiter().GetResult();

        if (result.IsError)
        {
            SetError(result.Message ?? "Mint failed.");
            InvokeOperationCallback(ExtractCode(result), StarApiOpMintInventoryNft);
            return (int)ExtractCode(result);
        }

        var (nftId, hash) = result.Result;
        WriteUtf8ToOutput(nftId ?? string.Empty, nftIdOut, 128);
        if (hashOut is not null)
            WriteUtf8ToOutput(hash ?? string.Empty, hashOut, 128);
        InvokeOperationCallback(StarApiResultCode.Success, StarApiOpMintInventoryNft);
        return (int)StarApiResultCode.Success;
    }

    /// <summary>Native export for star_api_use_item. quantity: number to consume (default 1).</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_use_item", CallConvs = [typeof(CallConvCdecl)])]
    public static byte StarApiUseItem(sbyte* itemName, sbyte* context, int quantity)
    {
        var client = GetClient();
        if (client is null)
        {
            SetError("Client is not initialized.");
            InvokeOperationCallback(StarApiResultCode.NotInitialized, StarApiOpUseItem);
            return 0;
        }

        int q = quantity > 0 ? quantity : 1;
        var result = client.UseItemAsync(PtrToString(itemName) ?? string.Empty, PtrToString(context), q).GetAwaiter().GetResult();
        var code = FinalizeResult(result, StarApiOpUseItem);
        return code == StarApiResultCode.Success && result.Result ? (byte)1 : (byte)0;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_use_item", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueUseItem(sbyte* itemName, sbyte* context, int quantity)
    {
        var client = GetClient();
        if (client is null)
            return;
        int q = quantity > 0 ? quantity : 1;
        client.EnqueueUseItemJobOnly(PtrToString(itemName) ?? string.Empty, PtrToString(context), q);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_flush_use_item_jobs", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiFlushUseItemJobs()
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpFlushUseItemJobs);
        var result = client.FlushUseItemJobsAsync(CancellationToken.None).GetAwaiter().GetResult();
        return (int)FinalizeResult(result, StarApiOpFlushUseItemJobs);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_start_quest", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiStartQuest(sbyte* questId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpStartQuest);

        var questIdStr = PtrToString(questId) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(questIdStr))
            return (int)SetErrorAndReturn("Quest ID required.", StarApiResultCode.InvalidParam, StarApiOpStartQuest);

        StarApiExports.StarApiLog($"[Quests] Start quest requested: QuestId={questIdStr}");
        /* Run start-quest on background thread so UI does not hang. Log outcome when the HTTP call finishes (native return is immediate Success). */
        _ = client.QueueStartQuestAsync(questIdStr).ContinueWith(
            t =>
            {
                try
                {
                    if (t.IsCanceled)
                    {
                        StarApiExports.StarApiLog($"[Quests] Start quest async: CANCELED QuestId={questIdStr}");
                        return;
                    }
                    if (t.IsFaulted)
                    {
                        var ex = t.Exception?.GetBaseException()?.Message ?? "faulted";
                        StarApiExports.StarApiLog($"[Quests] Start quest async: FAULT QuestId={questIdStr} {ex}");
                        return;
                    }
                    var r = t.Result;
                    if (r.IsError)
                        StarApiExports.StarApiLog($"[Quests] Start quest async: API rejected QuestId={questIdStr} — {r.Message ?? "(no message)"}");
                    else
                        StarApiExports.StarApiLog($"[Quests] Start quest async: OK QuestId={questIdStr} (status patched in local cache)");
                }
                catch (Exception ex)
                {
                    try { StarApiExports.StarApiLog($"[Quests] Start quest async: log error QuestId={questIdStr} {ex.Message}"); } catch { /* ignore */ }
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
        SetError(string.Empty);
        InvokeOperationCallback(StarApiResultCode.Success, StarApiOpStartQuest);
        return (int)StarApiResultCode.Success;
    }

    /// <summary>Native export: start quest on worker; after start succeeds, persist active quest + objective (ordered, no race with one-shot persist CVars).</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_start_quest_then_set_active_objective", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiStartQuestThenSetActiveObjective(sbyte* questId, sbyte* objectiveId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpStartQuest);

        var questIdStr = PtrToString(questId) ?? string.Empty;
        var objIdStr = PtrToString(objectiveId) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(questIdStr))
            return (int)SetErrorAndReturn("Quest ID required.", StarApiResultCode.InvalidParam, StarApiOpStartQuest);
        if (string.IsNullOrWhiteSpace(objIdStr))
            return (int)SetErrorAndReturn("Objective ID required.", StarApiResultCode.InvalidParam, StarApiOpStartQuest);
        if (!Guid.TryParse(questIdStr, out var qGuid))
            return (int)SetErrorAndReturn("Quest ID must be a GUID.", StarApiResultCode.InvalidParam, StarApiOpStartQuest);
        if (!Guid.TryParse(objIdStr, out var oGuid))
            return (int)SetErrorAndReturn("Objective ID must be a GUID.", StarApiResultCode.InvalidParam, StarApiOpStartQuest);

        Guid? q = qGuid;
        Guid? o = oGuid;
        try { StarApiExports.StarApiLogFileOnly($"[Quests] start_then_set_active: queue start questId={questIdStr} objectiveId={objIdStr}"); } catch { /* ignore */ }

        _ = client.QueueStartQuestAsync(questIdStr).ContinueWith(
            t =>
            {
                try
                {
                    if (t.IsCanceled)
                    {
                        StarApiExports.StarApiLogFileOnly($"[Quests] start_then_set_active: start CANCELED questId={questIdStr}");
                        return;
                    }
                    if (t.IsFaulted)
                    {
                        var ex = t.Exception?.GetBaseException()?.Message ?? "faulted";
                        StarApiExports.StarApiLogFileOnly($"[Quests] start_then_set_active: start FAULT questId={questIdStr} {ex}");
                        return;
                    }
                    var r = t.Result;
                    if (r.IsError || !r.Result)
                    {
                        StarApiExports.StarApiLogFileOnly($"[Quests] start_then_set_active: start failed questId={questIdStr} — {r.Message ?? "(no message)"}");
                        return;
                    }
                    _ = client.SetActiveQuestAndObjectiveAsync(q, o, CancellationToken.None).ContinueWith(
                        st =>
                        {
                            try
                            {
                                if (st.IsCanceled)
                                    return;
                                if (st.IsFaulted)
                                {
                                    var ex2 = st.Exception?.GetBaseException()?.Message ?? "faulted";
                                    StarApiExports.StarApiLogFileOnly($"[Quests] start_then_set_active: set_active FAULT questId={questIdStr} {ex2}");
                                    return;
                                }
                                var sr = st.Result;
                                if (sr.IsError)
                                    StarApiExports.StarApiLogFileOnly($"[Quests] start_then_set_active: set_active failed questId={questIdStr} — {sr.Message ?? "(no message)"}");
                                else
                                    StarApiExports.StarApiLogFileOnly($"[Quests] start_then_set_active: OK questId={questIdStr} objectiveId={objIdStr}");
                            }
                            catch (Exception ex3)
                            {
                                try { StarApiExports.StarApiLogFileOnly($"[Quests] start_then_set_active: set_active log err {ex3.Message}"); } catch { /* ignore */ }
                            }
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);
                }
                catch (Exception ex)
                {
                    try { StarApiExports.StarApiLogFileOnly($"[Quests] start_then_set_active: continuation err questId={questIdStr} {ex.Message}"); } catch { /* ignore */ }
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        SetError(string.Empty);
        InvokeOperationCallback(StarApiResultCode.Success, StarApiOpStartQuest);
        return (int)StarApiResultCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_complete_quest_objective", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiCompleteQuestObjective(sbyte* questId, sbyte* objectiveId, sbyte* gameSource)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpCompleteQuestObjective);

        var qId = PtrToString(questId) ?? string.Empty;
        var oId = PtrToString(objectiveId) ?? string.Empty;
        var gs = PtrToString(gameSource) ?? string.Empty;
        try { StarApiLogFileOnly($"[Quest] star_api_complete_quest_objective called: questId={qId} objectiveId={oId} gameSource={gs}"); } catch { /* ignore */ }

        /* Queue like start-quest: avoid blocking the game thread on HTTP (reduces deadlock / hard-freeze risk in native engines). */
        _ = client.QueueCompleteQuestObjectiveAsync(qId, oId, string.IsNullOrWhiteSpace(gs) ? null : gs);
        SetError(string.Empty);
        InvokeOperationCallback(StarApiResultCode.Success, StarApiOpCompleteQuestObjective);
        try { StarApiLogFileOnly("[Quest] star_api_complete_quest_objective: queued (async completion)"); } catch { /* ignore */ }
        return (int)StarApiResultCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_complete_quest", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiCompleteQuest(sbyte* questId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpCompleteQuest);

        var result = client.CompleteQuestAsync(PtrToString(questId) ?? string.Empty).GetAwaiter().GetResult();
        return (int)FinalizeResult(result, StarApiOpCompleteQuest);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_create_monster_nft", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiCreateMonsterNft(sbyte* monsterName, sbyte* description, sbyte* gameSource, sbyte* monsterStats, sbyte* provider, sbyte* nftIdOut)
    {
        if (nftIdOut is null)
            return (int)SetErrorAndReturn("nftIdOut buffer must not be null.", StarApiResultCode.InvalidParam, StarApiOpCreateMonsterNft);

        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpCreateMonsterNft);

        var result = client.CreateMonsterNftAsync(
            PtrToString(monsterName) ?? string.Empty,
            PtrToString(description),
            PtrToString(gameSource),
            PtrToString(monsterStats),
            PtrToString(provider)).GetAwaiter().GetResult();

        var code = FinalizeResult(result, StarApiOpCreateMonsterNft);
        if (code == StarApiResultCode.Success && !string.IsNullOrWhiteSpace(result.Result.NftId))
            WriteUtf8ToOutput(result.Result.NftId, nftIdOut, 64);
        else
            WriteUtf8ToOutput(string.Empty, nftIdOut, 64);

        return (int)code;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_deploy_boss_nft", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiDeployBossNft(sbyte* nftId, sbyte* targetGame, sbyte* location)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpDeployBossNft);

        var result = client.DeployBossNftAsync(
            PtrToString(nftId) ?? string.Empty,
            PtrToString(targetGame) ?? string.Empty,
            PtrToString(location)).GetAwaiter().GetResult();

        return (int)FinalizeResult(result, StarApiOpDeployBossNft);
    }

    /// <summary>Send item to avatar. Uses the client's HTTP timeout (no extra cancellation).</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_send_item_to_avatar", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSendItemToAvatar(sbyte* targetUsernameOrAvatarId, sbyte* itemName, int quantity, sbyte* itemId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpSendItemToAvatar);

        var idStr = PtrToString(itemId);
        Guid? guid = Guid.TryParse(idStr ?? string.Empty, out var g) && g != Guid.Empty ? g : null;
        var result = client.SendItemToAvatarAsync(
            PtrToString(targetUsernameOrAvatarId) ?? string.Empty,
            PtrToString(itemName) ?? string.Empty,
            quantity < 1 ? 1 : quantity,
            guid,
            CancellationToken.None).GetAwaiter().GetResult();
        return (int)FinalizeResult(result, StarApiOpSendItemToAvatar);
    }

    /// <summary>Send item to clan. Uses the client's HTTP timeout (no extra cancellation).</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_send_item_to_clan", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSendItemToClan(sbyte* clanNameOrTarget, sbyte* itemName, int quantity, sbyte* itemId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpSendItemToClan);

        var idStr = PtrToString(itemId);
        Guid? guid = Guid.TryParse(idStr ?? string.Empty, out var g) && g != Guid.Empty ? g : null;
        var result = client.SendItemToClanAsync(
            PtrToString(clanNameOrTarget) ?? string.Empty,
            PtrToString(itemName) ?? string.Empty,
            quantity < 1 ? 1 : quantity,
            guid,
            CancellationToken.None).GetAwaiter().GetResult();

        if (result.IsError && !string.IsNullOrEmpty(result.Message) && result.Message.IndexOf("avatar", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            SetError("Clan not found.");
            var code = ExtractCode(result);
            InvokeOperationCallback(code, StarApiOpSendItemToClan);
            return (int)code;
        }
        return (int)FinalizeResult(result, StarApiOpSendItemToClan);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_get_last_error", CallConvs = [typeof(CallConvCdecl)])]
    public static sbyte* StarApiGetLastError()
    {
        lock (NativeStateLock)
            return (sbyte*)_lastError;
    }

    /// <summary>Consume last mint result (from background pickup-with-mint). Returns 1 if result was available and written to buffers, 0 otherwise. Buffers are null-terminated. Use from game pump/frame to show NFT ID and hash in console.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_consume_last_mint_result", CallConvs = [typeof(CallConvCdecl)])]
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
    [UnmanagedCallersOnly(EntryPoint = "star_api_consume_last_background_error", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiConsumeLastBackgroundError(sbyte* buf, nuint size)
    {
        var msg = TryConsumeLastBackgroundError();
        if (msg is null || buf == null || size == 0) return 0;
        var len = (int)Math.Min(size, int.MaxValue);
        WriteUtf8ToOutput(msg, buf, len);
        return 1;
    }

    /// <summary>Consume one STAR log message for the game console. Returns 1 if a message was copied to buf, 0 otherwise. Call from game pump each frame to show STAR logs in console.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_consume_console_log", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiConsumeConsoleLog(sbyte* buf, nuint size)
    {
        var msg = TryConsumeConsoleLog();
        if (msg is null || buf == null || size == 0) return 0;
        var len = (int)Math.Min(size, int.MaxValue);
        WriteUtf8ToOutput(msg, buf, len);
        return 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_set_callback", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetCallback(delegate* unmanaged[Cdecl]<int, void*, void> callback, void* userData)
    {
        lock (NativeStateLock)
        {
            _callback = callback;
            _callbackUserData = userData;
        }
    }

    /// <summary>Operation type for star_api_set_operation_callback. Game can filter and only run "profile loaded" when type is ProfileLoaded.</summary>
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

    /// <summary>Invoke operation callback on the game-thread pump when available; fallback to direct invoke.</summary>
    internal static void InvokeOperationCallback(StarApiResultCode code, int operationType)
    {
        if (StarSyncExports.TryEnqueueOperationCallback(code, operationType))
            return;

        InvokeOperationCallbackOnCurrentThread(code, operationType);
    }

    /// <summary>Invoke callback immediately on the current thread (used by star_sync_pump dispatch).</summary>
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

    [UnmanagedCallersOnly(EntryPoint = "star_api_set_operation_callback", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetOperationCallback(delegate* unmanaged[Cdecl]<int, int, void*, void> callback, void* userData)
    {
        lock (NativeStateLock)
        {
            _operationCallback = callback;
            _operationCallbackUserData = userData;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_set_oasis_base_url", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSetOasisBaseUrl(sbyte* oasisBaseUrl)
    {
        var url = PtrToString(oasisBaseUrl) ?? string.Empty;
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpSetOasisBaseUrl);

        var result = client.SetWeb4OasisApiBaseUrl(url);
        return (int)FinalizeResult(result, StarApiOpSetOasisBaseUrl);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_get_avatar_id", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetAvatarId(sbyte* avatarIdOut, nuint avatarIdSize)
    {
        if (avatarIdOut is null || avatarIdSize == 0)
            return (int)SetErrorAndReturn("avatarIdOut must not be null and size must be > 0.", StarApiResultCode.InvalidParam, StarApiOpGetAvatarId);

        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetAvatarId);

        // Use cached avatar ID when available (set by AuthenticateAsync or init with api_key+avatar_id) to avoid a second GET WEB4 get-logged-in-avatar-with-xp when the game then calls star_api_refresh_avatar_profile().
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

    [UnmanagedCallersOnly(EntryPoint = "star_api_set_avatar_id", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSetAvatarId(sbyte* avatarId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpSetAvatarId);

        var result = client.SetAvatarId(PtrToString(avatarId) ?? string.Empty);
        return (int)FinalizeResultNoCallback(result);
    }

    /// <summary>Used by StarSyncExports (star_sync_* in-client implementation).</summary>
    internal static StarApiClient? GetClient()
    {
        lock (Sync)
            return _client;
    }

    /// <summary>Build native star_item_list_t from managed list for star_sync inventory result. Caller must call StarApiFreeItemList when done.</summary>
    internal static unsafe star_item_list_t* BuildItemListFromInventory(List<StarItem> list)
    {
        if (list is null || list.Count == 0)
        {
            var empty = (star_item_list_t*)NativeMemory.Alloc((nuint)1, (nuint)sizeof(star_item_list_t));
            if (empty is not null)
            {
                empty->items = null;
                empty->count = 0;
                empty->capacity = 0;
            }
            return empty;
        }
        var count = (nuint)list.Count;
        var listPtr = (star_item_list_t*)NativeMemory.Alloc((nuint)1, (nuint)sizeof(star_item_list_t));
        if (listPtr is null) return null;
        listPtr->count = count;
        listPtr->capacity = count;
        listPtr->items = (star_item_t*)NativeMemory.Alloc(count, (nuint)sizeof(star_item_t));
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
        return Path.Combine(dir, "star_api.log");
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
                        $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] [STAR] star_api.log path: {path}" + Environment.NewLine +
                        $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] [STAR] Diagnostics: full HTTP/API lines are written here; the game console shows short previews only (crash-safe)." +
                        Environment.NewLine);
                }
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch { /* ignore file write errors */ }
    }

    /// <summary>Write to star_api.log and Trace only; do NOT enqueue for game console. Use for quest API logs so Quake/Doom don't consume them and crash.</summary>
    internal static void StarApiLogFileOnly(string message)
    {
        AppendStarApiDiagnosticsFileLine(message, prefixWithStarTag: false);
    }

    /// <summary>Full line to star_api.log; console gets a short preview (same directory as star_api.dll).</summary>
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
            var shortLine = TruncateUtf8ForInterop(raw, Math.Max(64, MaxConsoleLogUtf8Bytes - 48)) + " …[full line in star_api.log]";
            EnqueueConsoleLog(shortLine);
        }
    }

    /// <summary>Append a line to star_api.log and enqueue for Doom console so game (e.g. Doom) door-check and debug messages appear in both.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_log_to_file", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiLogToFile(sbyte* message)
    {
        var msg = PtrToString(message);
        if (string.IsNullOrWhiteSpace(msg)) return;
        AppendStarApiDiagnosticsFileLine(msg, prefixWithStarTag: true);
        if (Encoding.UTF8.GetByteCount(msg) <= MaxConsoleLogUtf8Bytes)
            EnqueueConsoleLog(msg);
        else
        {
            var shortLine = TruncateUtf8ForInterop(msg, Math.Max(64, MaxConsoleLogUtf8Bytes - 48)) + " …[full line in star_api.log]";
            EnqueueConsoleLog(shortLine);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_set_debug", CallConvs = [typeof(CallConvCdecl)])]
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

    /// <summary>Set last error and return code without invoking the shared callback. Use for auth/set_avatar_id so the game only runs "profile loaded" when star_api_refresh_avatar_profile completes (cache has XP/quest).</summary>
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
}

