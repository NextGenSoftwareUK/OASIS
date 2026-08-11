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
    /// <summary>Set the last background error (mint/add_item failure or pickup not queued). Consumed by ogengine_consume_last_background_error for game console display.</summary>
    public static void SetLastBackgroundError(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        lock (BackgroundErrorLock)
            _lastBackgroundError = message;
    }

    /// <summary>Return and clear the last background error. Used by ogengine_consume_last_background_error.</summary>
    public static string? TryConsumeLastBackgroundError()
    {
        lock (BackgroundErrorLock)
        {
            var msg = _lastBackgroundError;
            _lastBackgroundError = null;
            return msg;
        }
    }

    /// <summary>Enqueue a message for the game console (consumed by ogengine_consume_console_log).</summary>
    public static void EnqueueConsoleLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        /* Games pass a small native buffer (~512 bytes). Encoding.UTF8.GetBytes throws if the string is larger than the span — that aborts the process inside UnmanagedCallersOnly. */
        message = TruncateUtf8ForInterop(message, MaxConsoleLogUtf8Bytes);
        while (_consoleLogQueue.Count >= MaxConsoleLogMessages && _consoleLogQueue.TryDequeue(out _)) { }
        _consoleLogQueue.Enqueue(message);
    }

    /// <summary>Dequeue one console log message for the game to display. Used by ogengine_consume_console_log.</summary>
    public static string? TryConsumeConsoleLog()
    {
        return _consoleLogQueue.TryDequeue(out var msg) ? msg : null;
    }

    /// <summary>Enqueue a cross-game event JSON for ogengine_poll_cross_game_event. Called by OGEngineClient when progress/start returns events.</summary>
    internal static void EnqueueCrossGameEvent(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        while (_pendingCrossGameEvents.Count >= MaxPendingCrossGameEvents && _pendingCrossGameEvents.TryDequeue(out _)) { }
        _pendingCrossGameEvents.Enqueue(json);
    }

    /// <summary>Enqueue an inventory item GUID for ogengine_poll_inventory_grant. Called by OGEngineClient when progress/start returns InventoryItemsToGrant.</summary>
    internal static void EnqueueInventoryGrant(string itemGuid)
    {
        if (string.IsNullOrWhiteSpace(itemGuid)) return;
        _pendingInventoryGrants.Enqueue(itemGuid);
    }

    static OGEngineExports()
    {
        SetError(string.Empty);
    }

    /// <summary>Trimmer root: keep all members so session/JWT exports stay in ogengine.dll for forwarders and autologin.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OGEngineExports))]
    [DynamicDependency("StarApiGetCurrentJwt", typeof(OGEngineExports))]
    [DynamicDependency("StarApiGetCurrentRefreshToken", typeof(OGEngineExports))]
    [DynamicDependency("StarApiGetCurrentUsername", typeof(OGEngineExports))]
    [DynamicDependency("StarApiSetSavedSession", typeof(OGEngineExports))]
    [DynamicDependency("StarApiSetRefreshToken", typeof(OGEngineExports))]
    [DynamicDependency("StarApiIsSessionExpired", typeof(OGEngineExports))]
    [DynamicDependency("StarApiRestoreSession", typeof(OGEngineExports))]
    [DynamicDependency("StarApiAuthenticateWithJwtOut", typeof(OGEngineExports))]
    [UnmanagedCallersOnly(EntryPoint = "ogengine_init", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiInit(ogengine_config_t* config)
    {
        if (config is null)
            return (int)SetErrorAndReturn("Invalid configuration.", StarApiResultCode.InvalidParam, StarApiOpInit);

        int tr = config->transport;
        if (tr == (int)OGEngineTransport.Native)
            return (int)SetErrorAndReturn(
                "Native STAR transport is not available in this ogengine build. Use star_transport \"remote\" with WEB5/WEB4 URLs, or a native OASIS host that implements ogengine_init with HyperDrive.",
                StarApiResultCode.InitFailed,
                StarApiOpInit);
        if (tr != 0 && tr != (int)OGEngineTransport.Remote)
            return (int)SetErrorAndReturn("Invalid ogengine_config_t.transport value.", StarApiResultCode.InvalidParam, StarApiOpInit);

        if (config->base_url is null)
            return (int)SetErrorAndReturn("Invalid configuration.", StarApiResultCode.InvalidParam, StarApiOpInit);

        var baseUrl = PtrToString(config->base_url) ?? string.Empty;
        var managedConfig = new OGEngineConfig
        {
            Web5StarApiBaseUrl = baseUrl,
            ApiKey = PtrToString(config->api_key),
            AvatarId = PtrToString(config->avatar_id),
            TimeoutSeconds = config->timeout_seconds,
            ClientGameSource = config->client_game_source != null ? PtrToString(config->client_game_source) : null,
            Transport = OGEngineTransport.Remote,
            OasisDnaPath = config->oasis_dna_path != null ? PtrToString(config->oasis_dna_path) : null
        };

        lock (Sync)
        {
            _client?.Dispose();
            _client = new OGEngineClient();
        }

        var result = _client.Init(managedConfig);
        return (int)FinalizeResult(result, StarApiOpInit);
    }

    /// <summary>0 = merge progress into local quest cache after OK (no GET). 1 = GET all quests after each OK. Call after ogengine_init; safe anytime.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_set_quest_progress_cache_refresh", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetQuestProgressCacheRefresh(int mode)
    {
        var client = GetClient();
        if (client is null) return;
        client.SetQuestProgressCacheRefreshMode(mode != 0 ? QuestProgressCacheRefreshMode.FullServerRefresh : QuestProgressCacheRefreshMode.ClientCacheMerge);
    }

    /// <summary>Shared implementation for authenticate; used by both ogengine_authenticate and ogengine_authenticate_with_jwt_out (UnmanagedCallersOnly cannot call UnmanagedCallersOnly).</summary>
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

    [UnmanagedCallersOnly(EntryPoint = "ogengine_authenticate", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiAuthenticate(sbyte* username, sbyte* password)
    {
        return AuthenticateWithJwtOutImpl(username, password, null, 0);
    }

    /// <summary>Authenticate and optionally write JWT to buf so games can persist to oasisstar.json without relying on get_current_jwt export.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_authenticate_with_jwt_out", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiAuthenticateWithJwtOut(sbyte* username, sbyte* password, sbyte* jwt_buf, nuint jwt_size)
    {
        return AuthenticateWithJwtOutImpl(username, password, jwt_buf, jwt_size);
    }

    /// <summary>Set JWT from persisted session (e.g. oasisstar.json). Call ogengine_restore_session to validate and load profile.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_set_saved_session", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSetSavedSession(sbyte* jwt)
    {
        var client = GetClient();
        if (client is null) return (int)StarApiResultCode.NotInitialized;
        var jwtStr = PtrToString(jwt);
        var result = client.SetSavedSession(jwtStr ?? string.Empty);
        return result.IsError ? (int)StarApiResultCode.ApiError : (int)StarApiResultCode.Success;
    }

    /// <summary>Start async session restore (GET avatar/current). Callback is invoked on success/failure. Does not block.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_restore_session", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiRestoreSession()
    {
        var client = GetClient();
        if (client is null) return (int)StarApiResultCode.NotInitialized;
        _ = client.QueueRestoreSessionAsync(CancellationToken.None);
        return (int)StarApiResultCode.Success; /* restore started; callback will fire when done */
    }

    /// <summary>Write current username to buf (for saving to oasisstar.json). Returns bytes written or 0.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_current_username", CallConvs = [typeof(CallConvCdecl)])]
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
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_current_jwt", CallConvs = [typeof(CallConvCdecl)])]
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
        OGEngineExports.StarApiLogFileOnly($"[Auth] GetCurrentJwt: returning length={jwt.Length} (for oasisstar.json)");
        var bytes = Encoding.UTF8.GetBytes(jwt);
        var toCopy = (int)Math.Min((nuint)bytes.Length, bufSize - 1);
        if (toCopy > 0) new ReadOnlySpan<byte>(bytes, 0, toCopy).CopyTo(new Span<byte>(buf, toCopy));
        buf[toCopy] = 0;
        return toCopy;
    }

    /// <summary>Set refresh token from persisted session (e.g. oasisstar.json). Call after ogengine_set_saved_session when restoring so 401 can trigger token refresh.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_set_refresh_token", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetRefreshToken(sbyte* refreshToken)
    {
        var client = GetClient();
        if (client is null) return;
        var s = refreshToken is null ? null : PtrToString(refreshToken);
        client.SetRefreshToken(s);
    }

    /// <summary>Write current refresh token to buf (for saving to oasisstar.json). Returns bytes written or 0. Caller should not log.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_current_refresh_token", CallConvs = [typeof(CallConvCdecl)])]
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
    [UnmanagedCallersOnly(EntryPoint = "ogengine_is_session_expired", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiIsSessionExpired()
    {
        var client = GetClient();
        return (client != null && client.IsSessionExpired()) ? 1 : 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_cleanup", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiCleanup()
    {
        lock (Sync)
        {
            _client?.Dispose();
            _client = null;
        }
    }

    /// <summary>Native export for ogengine_has_item. Prefer checking already-loaded inventory (local cache) for optimization; use this as last resort.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_has_item", CallConvs = [typeof(CallConvCdecl)])]
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

    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_inventory", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetInventory(ogengine_item_list_t** itemList)
    {
        if (itemList is null)
            return (int)SetErrorAndReturn("itemList must not be null.", StarApiResultCode.InvalidParam, StarApiOpGetInventory);

        *itemList = null;

        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpGetInventory);

        var cached = client.TryGetCachedInventory();
        if (cached is null)
            return (int)SetErrorAndReturn("Inventory not loaded. Call ogengine_request_inventory_in_background first.", StarApiResultCode.ApiError, StarApiOpGetInventory);

        var count = (nuint)cached.Count;
        var listPtr = (ogengine_item_list_t*)NativeMemory.Alloc((nuint)1, (nuint)sizeof(ogengine_item_list_t));
        if (listPtr is null)
            return (int)SetErrorAndReturn("Memory allocation failed for item list.", StarApiResultCode.InitFailed, StarApiOpGetInventory);

        listPtr->count = count;
        listPtr->capacity = count;
        listPtr->items = null;

        if (count > 0)
        {
            listPtr->items = (ogengine_item_t*)NativeMemory.Alloc(count, (nuint)sizeof(ogengine_item_t));
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

    [UnmanagedCallersOnly(EntryPoint = "ogengine_request_inventory_in_background", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiRequestInventoryInBackground()
    {
        var client = GetClient();
        if (client is null)
        {
            // Defer callback so the export never blocks the game thread (avoids hang on Linux when not connected).
            _ = Task.Run(() => OGEngineExports.InvokeOperationCallback(StarApiResultCode.NotInitialized, OGEngineExports.StarApiOpGetInventory));
            return;
        }
        client.RequestInventoryInBackground();
    }

    /// <summary>Internal helper for StarSyncExports; same logic as ogengine_free_item_list so C# can call without going through UnmanagedCallersOnly.</summary>
    internal static unsafe void FreeItemListInternal(ogengine_item_list_t* itemList)
    {
        if (itemList is null)
            return;

        if (itemList->items is not null)
            NativeMemory.Free(itemList->items);

        NativeMemory.Free(itemList);
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_free_item_list", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiFreeItemList(ogengine_item_list_t* itemList)
    {
        FreeItemListInternal(itemList);
    }
}
