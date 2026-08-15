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
    [UnmanagedCallersOnly(EntryPoint = "ogengine_add_item", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiAddItem(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, sbyte* nftId, int quantity, int stack)
    {
        var client = GetClient();
        if (client is null)
        {
            StarApiLog("ogengine_add_item: client is null");
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

        StarApiLog($"ogengine_add_item: name='{name}' quantity={qty} stack={doStack} (calling AddItemAsync on thread pool)");

        var result = Task.Run(() => client.AddItemAsync(name, desc, source, type, nftIdOpt, qty, doStack).GetAwaiter().GetResult()).GetAwaiter().GetResult();

        var code = FinalizeResult(result, StarApiOpAddItem);
        StarApiLog($"ogengine_add_item: result IsError={result.IsError} code={(int)code} message={result.Message ?? "(ok)"}");
        return (int)code;
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_queue_add_item", CallConvs = [typeof(CallConvCdecl)])]
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

    [UnmanagedCallersOnly(EntryPoint = "ogengine_queue_quest_progress_from_pickup", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueQuestProgressFromPickup(sbyte* gameSource, sbyte* itemType, sbyte* itemName)
    {
        var client = GetClient();
        if (client is null)
        {
            try { OGEngineExports.StarApiLogFileOnly("[Quest] ogengine_queue_quest_progress_from_pickup: no client"); } catch { /* ignore */ }
            return;
        }
        var gs = PtrToString(gameSource);
        if (string.IsNullOrWhiteSpace(gs)) gs = "ODOOM";
        var it = PtrToString(itemType);
        var name = PtrToString(itemName);
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] ogengine_queue_quest_progress_from_pickup: gs={gs} itemType={it ?? ""} itemName={name ?? ""}"); } catch { /* ignore */ }
        var keysDeltaPickup = !string.IsNullOrWhiteSpace(it) && it.IndexOf("Key", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0;
        client.EnqueueQuestProgressFromGame(gs, 0, 0, name, keysDeltaPickup, 1, null, it);
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_queue_add_xp", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueAddXp(int amount)
    {
        var client = GetClient();
        if (client is null) return;
        if (amount < 0) return;
        client.EnqueueAddXpJobOnly(amount);
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_queue_monster_kill", CallConvs = [typeof(CallConvCdecl)])]
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

    [UnmanagedCallersOnly(EntryPoint = "ogengine_queue_quest_level_time", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueQuestLevelTime(sbyte* gameSource, int levelElapsedSeconds)
    {
        var client = GetClient();
        if (client is null || levelElapsedSeconds < 0) return;
        var gs = PtrToString(gameSource);
        if (string.IsNullOrWhiteSpace(gs)) gs = "Quake";
        client.EnqueueQuestProgressFromGame(gs, 0, 0, null, 0, 0, levelElapsedSeconds);
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_avatar_xp", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetAvatarXp(int* xpOut)
    {
        var client = GetClient();
        if (client is null) { try { OGEngineExports.StarApiLogFileOnly("[STAR] ogengine_get_avatar_xp: no client"); } catch { } return 0; }
        var xp = client.GetCachedAvatarXp();
        if (xpOut is not null)
            *xpOut = xp;
        return 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_avatar_karma", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetAvatarKarma(long* karmaOut)
    {
        var client = GetClient();
        if (client is null) { try { OGEngineExports.StarApiLogFileOnly("[STAR] ogengine_get_avatar_karma: no client"); } catch { } return 0; }
        var karma = client.GetCachedAvatarKarma();
        if (karmaOut is not null)
            *karmaOut = karma;
        return 1;
    }

    // REDUNDANT / REMOVED: ogengine_refresh_avatar_xp was a duplicate. Use ogengine_refresh_avatar_profile() only.
    // [UnmanagedCallersOnly(EntryPoint = "ogengine_refresh_avatar_xp", ...)]
    // public static void StarApiRefreshAvatarXp() { ... }

    /// <summary>Kick off avatar profile refresh (XP + active quest/objective) in background; returns immediately. Callback is invoked when the load completes. Call on beam-in so the game can update the tracker in the callback.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_refresh_avatar_profile", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiRefreshAvatarProfile()
    {
        try { StarApiLogFileOnly("[STAR] ogengine_refresh_avatar_profile called"); } catch { }
        var client = GetClient();
        if (client is null) return;
        client.RefreshAvatarProfileInBackground();
    }

    /// <summary>Get display name of the current tracked quest from cache (so HUD can show name as soon as quest list loads after beam-in). Writes UTF-8 name to buf, null-terminated. Returns bytes written (excluding null), or 0 if not available.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_tracker_quest_name", CallConvs = [typeof(CallConvCdecl)])]
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
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_active_quest_id", CallConvs = [typeof(CallConvCdecl)])]
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
            try { OGEngineExports.StarApiLog($"[Quest] ogengine_get_active_quest_id: returning {id}"); } catch { }
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
    [UnmanagedCallersOnly(EntryPoint = "ogengine_get_active_objective_id", CallConvs = [typeof(CallConvCdecl)])]
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
            try { OGEngineExports.StarApiLog($"[Quest] ogengine_get_active_objective_id: returning {id}"); } catch { }
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
    [UnmanagedCallersOnly(EntryPoint = "ogengine_set_active_quest", CallConvs = [typeof(CallConvCdecl)])]
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
        try { OGEngineExports.StarApiLog($"[Quest] ogengine_set_active_quest called from native (user set tracker in game): questId={qStr ?? "(null)"}, objectiveId={oStr ?? "(null)"}"); } catch { }
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] ogengine_set_active_quest (native): questId={qStr ?? "(null)"}, objectiveId={oStr ?? "(null)"}"); } catch { }
        var result = client.SetActiveQuestAndObjectiveAsync(q, o).GetAwaiter().GetResult();
        try { OGEngineExports.StarApiLog($"[Quest] ogengine_set_active_quest result: IsError={result?.IsError}, Message={result?.Message}"); } catch { }
        return (int)FinalizeResult(result, StarApiOpSetActiveQuest);
    }

    /// <summary>Queue pickup with optional mint; C# client does mint (if do_mint) then add_item in background. Same pattern as queue_add_item.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_queue_pickup_with_mint", CallConvs = [typeof(CallConvCdecl)])]
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

    [UnmanagedCallersOnly(EntryPoint = "ogengine_flush_add_item_jobs", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiFlushAddItemJobs()
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpFlushAddItemJobs);
        var result = client.FlushAddItemJobsAsync(CancellationToken.None).GetAwaiter().GetResult();
        return (int)FinalizeResult(result, StarApiOpFlushAddItemJobs);
    }

    /// <summary>Mint an NFT for an inventory item via WEB4 OASIS API (NFTHolon). Returns NFT ID to pass to ogengine_add_item as nft_id. Optional hash_out for tx hash/signature. provider defaults to SolanaOASIS. Note: mint is currently synchronous (blocking); add_item is queued and flushed async.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_mint_inventory_nft", CallConvs = [typeof(CallConvCdecl)])]
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

    /// <summary>Native export for ogengine_use_item. quantity: number to consume (default 1).</summary>
    [UnmanagedCallersOnly(EntryPoint = "ogengine_use_item", CallConvs = [typeof(CallConvCdecl)])]
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

    [UnmanagedCallersOnly(EntryPoint = "ogengine_queue_use_item", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueUseItem(sbyte* itemName, sbyte* context, int quantity)
    {
        var client = GetClient();
        if (client is null)
            return;
        int q = quantity > 0 ? quantity : 1;
        client.EnqueueUseItemJobOnly(PtrToString(itemName) ?? string.Empty, PtrToString(context), q);
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_flush_use_item_jobs", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiFlushUseItemJobs()
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpFlushUseItemJobs);
        var result = client.FlushUseItemJobsAsync(CancellationToken.None).GetAwaiter().GetResult();
        return (int)FinalizeResult(result, StarApiOpFlushUseItemJobs);
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_start_quest", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiStartQuest(sbyte* questId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpStartQuest);

        var questIdStr = PtrToString(questId) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(questIdStr))
            return (int)SetErrorAndReturn("Quest ID required.", StarApiResultCode.InvalidParam, StarApiOpStartQuest);

        OGEngineExports.StarApiLog($"[Quests] Start quest requested: QuestId={questIdStr}");
        /* Run start-quest on background thread so UI does not hang. Log outcome when the HTTP call finishes (native return is immediate Success). */
        _ = client.QueueStartQuestAsync(questIdStr).ContinueWith(
            t =>
            {
                try
                {
                    if (t.IsCanceled)
                    {
                        OGEngineExports.StarApiLog($"[Quests] Start quest async: CANCELED QuestId={questIdStr}");
                        return;
                    }
                    if (t.IsFaulted)
                    {
                        var ex = t.Exception?.GetBaseException()?.Message ?? "faulted";
                        OGEngineExports.StarApiLog($"[Quests] Start quest async: FAULT QuestId={questIdStr} {ex}");
                        return;
                    }
                    var r = t.Result;
                    if (r.IsError)
                        OGEngineExports.StarApiLog($"[Quests] Start quest async: API rejected QuestId={questIdStr} — {r.Message ?? "(no message)"}");
                    else
                        OGEngineExports.StarApiLog($"[Quests] Start quest async: OK QuestId={questIdStr} (status patched in local cache)");
                }
                catch (Exception ex)
                {
                    try { OGEngineExports.StarApiLog($"[Quests] Start quest async: log error QuestId={questIdStr} {ex.Message}"); } catch { /* ignore */ }
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
    [UnmanagedCallersOnly(EntryPoint = "ogengine_start_quest_then_set_active_objective", CallConvs = [typeof(CallConvCdecl)])]
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
        try { OGEngineExports.StarApiLogFileOnly($"[Quests] start_then_set_active: queue start questId={questIdStr} objectiveId={objIdStr}"); } catch { /* ignore */ }

        _ = client.QueueStartQuestAsync(questIdStr).ContinueWith(
            t =>
            {
                try
                {
                    if (t.IsCanceled)
                    {
                        OGEngineExports.StarApiLogFileOnly($"[Quests] start_then_set_active: start CANCELED questId={questIdStr}");
                        return;
                    }
                    if (t.IsFaulted)
                    {
                        var ex = t.Exception?.GetBaseException()?.Message ?? "faulted";
                        OGEngineExports.StarApiLogFileOnly($"[Quests] start_then_set_active: start FAULT questId={questIdStr} {ex}");
                        return;
                    }
                    var r = t.Result;
                    if (r.IsError || !r.Result)
                    {
                        OGEngineExports.StarApiLogFileOnly($"[Quests] start_then_set_active: start failed questId={questIdStr} — {r.Message ?? "(no message)"}");
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
                                    OGEngineExports.StarApiLogFileOnly($"[Quests] start_then_set_active: set_active FAULT questId={questIdStr} {ex2}");
                                    return;
                                }
                                var sr = st.Result;
                                if (sr.IsError)
                                    OGEngineExports.StarApiLogFileOnly($"[Quests] start_then_set_active: set_active failed questId={questIdStr} — {sr.Message ?? "(no message)"}");
                                else
                                    OGEngineExports.StarApiLogFileOnly($"[Quests] start_then_set_active: OK questId={questIdStr} objectiveId={objIdStr}");
                            }
                            catch (Exception ex3)
                            {
                                try { OGEngineExports.StarApiLogFileOnly($"[Quests] start_then_set_active: set_active log err {ex3.Message}"); } catch { /* ignore */ }
                            }
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);
                }
                catch (Exception ex)
                {
                    try { OGEngineExports.StarApiLogFileOnly($"[Quests] start_then_set_active: continuation err questId={questIdStr} {ex.Message}"); } catch { /* ignore */ }
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        SetError(string.Empty);
        InvokeOperationCallback(StarApiResultCode.Success, StarApiOpStartQuest);
        return (int)StarApiResultCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_complete_quest_objective", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiCompleteQuestObjective(sbyte* questId, sbyte* objectiveId, sbyte* gameSource)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpCompleteQuestObjective);

        var qId = PtrToString(questId) ?? string.Empty;
        var oId = PtrToString(objectiveId) ?? string.Empty;
        var gs = PtrToString(gameSource) ?? string.Empty;
        try { StarApiLogFileOnly($"[Quest] ogengine_complete_quest_objective called: questId={qId} objectiveId={oId} gameSource={gs}"); } catch { /* ignore */ }

        /* Queue like start-quest: avoid blocking the game thread on HTTP (reduces deadlock / hard-freeze risk in native engines). */
        _ = client.QueueCompleteQuestObjectiveAsync(qId, oId, string.IsNullOrWhiteSpace(gs) ? null : gs);
        SetError(string.Empty);
        InvokeOperationCallback(StarApiResultCode.Success, StarApiOpCompleteQuestObjective);
        try { StarApiLogFileOnly("[Quest] ogengine_complete_quest_objective: queued (async completion)"); } catch { /* ignore */ }
        return (int)StarApiResultCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_complete_quest", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiCompleteQuest(sbyte* questId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized, StarApiOpCompleteQuest);

        var result = client.CompleteQuestAsync(PtrToString(questId) ?? string.Empty).GetAwaiter().GetResult();
        return (int)FinalizeResult(result, StarApiOpCompleteQuest);
    }

    [UnmanagedCallersOnly(EntryPoint = "ogengine_create_monster_nft", CallConvs = [typeof(CallConvCdecl)])]
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

    [UnmanagedCallersOnly(EntryPoint = "ogengine_deploy_boss_nft", CallConvs = [typeof(CallConvCdecl)])]
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
    [UnmanagedCallersOnly(EntryPoint = "ogengine_send_item_to_avatar", CallConvs = [typeof(CallConvCdecl)])]
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
    [UnmanagedCallersOnly(EntryPoint = "ogengine_send_item_to_clan", CallConvs = [typeof(CallConvCdecl)])]
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
}
