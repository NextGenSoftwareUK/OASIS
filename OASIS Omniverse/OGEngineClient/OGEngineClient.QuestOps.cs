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
    /// <summary>Persist active quest and objective on the avatar detail so they are restored after beam-in. Call when the user sets the tracker in the game.</summary>
    public async Task<OASISResult<bool>> SetActiveQuestAndObjectiveAsync(Guid? questId, Guid? objectiveId, CancellationToken cancellationToken = default)
    {
        try { OGEngineExports.StarApiLog($"[Quest] SetActiveQuestAndObjectiveAsync called: questId={questId}, objectiveId={objectiveId}"); } catch { /* ignore */ }
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] Set active quest requested: questId={questId}, objectiveId={objectiveId}"); } catch { /* ignore */ }
        if (!IsInitialized())
        {
            try { OGEngineExports.StarApiLog("[Quest] SetActiveQuestAndObjectiveAsync failed: client not initialized"); } catch { /* ignore */ }
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        }
        if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
            return FailAndCallback<bool>(missingWeb4, StarApiResultCode.InvalidParam);

        var url = $"{web4Base}/api/avatar/set-active-quest";
        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("activeQuestId");
            if (questId.HasValue) writer.WriteStringValue(questId.Value.ToString());
            else writer.WriteNullValue();
            writer.WritePropertyName("activeObjectiveId");
            if (objectiveId.HasValue) writer.WriteStringValue(objectiveId.Value.ToString());
            else writer.WriteNullValue();
            writer.WriteEndObject();
        });
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] SetActiveQuestAndObjectiveAsync POST {url} body={payload}"); } catch { /* ignore */ }
        var response = await SendRawAsync(HttpMethod.Post, url, payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quest] SetActiveQuestAndObjectiveAsync FAILED url={url} message={response.Message} body={response.Result ?? "(null)"}"); } catch { /* ignore */ }
            try { OGEngineExports.StarApiLog($"[Quest] Set active quest API error: {response.Message} (body in ogengine.log)"); } catch { /* ignore */ }
            return FailAndCallback<bool>(response.Message ?? "Set active quest failed.", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] SetActiveQuestAndObjectiveAsync OK url={url} responseBody={response.Result ?? "(null)"}"); } catch { /* ignore */ }
        lock (_stateLock)
        {
            _cachedActiveQuestId = questId;
            _cachedActiveObjectiveId = objectiveId;
            _questTrackerSavedSinceLastGet = true;  /* Any in-flight GET WEB4 avatar profile must not overwrite this save */
        }
        var (questName, objectiveName) = TryGetQuestAndObjectiveNamesFromCache(questId, objectiveId);
        try { OGEngineExports.StarApiLog($"[Quest] SetActiveQuestAndObjectiveAsync saved OK: questId={questId}, objectiveId={objectiveId} (cache updated)"); } catch { /* ignore */ }
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] SAVE OK questId={questId} objectiveId={objectiveId} questName={questName ?? "(not in cache)"} objectiveName={objectiveName ?? "(not in cache)"}"); } catch { /* ignore */ }
        LogActiveQuestSnapshot("after_set_active_quest_saved");
        return Success(true, StarApiResultCode.Success, "Active quest/objective saved.");
    }

    /// <summary>Returns the cached avatar ID (set by AuthenticateAsync or init with api_key+avatar_id). Used by ogengine_get_avatar_id to avoid a second GET when the game then calls refresh XP.</summary>
    public string? GetCachedAvatarId()
    {
        lock (_stateLock)
            return _avatarId;
    }

    // REDUNDANT / REMOVED: RefreshAvatarXp() was a duplicate of RefreshAvatarProfileInBackground() (same GET avatar/current).
    // Use RefreshAvatarProfileInBackground() only. It refreshes XP + ActiveQuestId/ActiveObjectiveId and invokes the callback.
    // public void RefreshAvatarXp() { RefreshAvatarProfileInBackground(); }

    /// <summary>Kick off a full avatar load (GET avatar/current) on the background worker so XP and ActiveQuestId/ActiveObjectiveId are updated without blocking the UI. Returns immediately; the same callback set via SetCallback is invoked when the load completes (Success or error), so the game can then read the cache and update the tracker.</summary>
    public void RefreshAvatarProfileInBackground()
    {
        OGEngineExports.StarApiLogFileOnly("[Avatar] RefreshAvatarProfileInBackground called");
        if (!IsInitialized())
        {
            OGEngineExports.StarApiLogFileOnly("[Avatar] RefreshAvatarProfileInBackground skipped (not initialized)");
            OGEngineExports.InvokeOperationCallback(StarApiResultCode.NotInitialized, OGEngineExports.StarApiOpProfileLoaded);
            return;
        }
        _ = RunOnWorkerAsync(DedicatedWorker.Profile, async ct =>
        {
            Task? restoreWait;
            lock (_stateLock) { restoreWait = _restoreSessionInFlight; }
            if (restoreWait is { IsCompleted: false })
            {
                OGEngineExports.StarApiLogFileOnly("[Avatar] RefreshAvatarProfileInBackground: awaiting session restore (refresh token + validate) before GET");
                try
                {
                    await restoreWait.ConfigureAwait(false);
                }
                catch
                {
                    /* Restore task faulted; still attempt GET — 401 path may refresh or surface error. */
                }
            }
            OGEngineExports.StarApiLogFileOnly("[Avatar] RefreshAvatarProfileInBackground: GET avatar/current started");
            var result = await GetCurrentAvatarAsync(ct, invokeCallback: false).ConfigureAwait(false);
            if (result is not null && !result.IsError && result.Result is not null)
            {
                var xp = result.Result.XP;
                var qid = result.Result.ActiveQuestId;
                var oid = result.Result.ActiveObjectiveId;
                OGEngineExports.StarApiLogFileOnly($"[Avatar] RefreshAvatarProfileInBackground done SUCCESS: XP={xp} ActiveQuestId={qid} ActiveObjectiveId={oid} (invoking callback Success)");
                /* Quest list: warm once via auth Invalidate+Request or first Ensure; do not GET all-for-avatar on every profile poll. Popup / ogengine_refresh_quest_cache_in_background forces refetch. */
                RequestQuestCacheRefreshInBackground(forceRefetch: false);
                OGEngineExports.InvokeOperationCallback(StarApiResultCode.Success, OGEngineExports.StarApiOpProfileLoaded);
            }
            else
            {
                OGEngineExports.StarApiLogFileOnly($"[Avatar] RefreshAvatarProfileInBackground done FAIL: IsError={result?.IsError ?? true} Message={result?.Message ?? "null"} cachedXp={GetCachedAvatarXp()} (invoking callback error)");
                var errCode = result != null && result.IsError ? ParseCode(result.ErrorCode, StarApiResultCode.ApiError) : StarApiResultCode.ApiError;
                OGEngineExports.InvokeOperationCallback(errCode, OGEngineExports.StarApiOpProfileLoaded);
            }
            return result ?? Fail<StarAvatarProfile>("Refresh failed.", StarApiResultCode.ApiError);
        }, CancellationToken.None);
    }

    /// <summary>Consume the last mint result (item name, NFT ID, hash) from background pickup-with-mint. Returns true if a result was available and copies into the provided buffers; clears the stored result. Call from game each frame/pump to show mint results in console.</summary>
    public bool ConsumeLastMintResult(out string? itemName, out string? nftId, out string? hash)
    {
        lock (_lastMintLock)
        {
            itemName = _lastMintItemName;
            nftId = _lastMintNftId;
            hash = _lastMintHash;
            _lastMintItemName = _lastMintNftId = _lastMintHash = null;
        }
        return itemName is not null || nftId is not null;
    }

    /// <summary>Queue a pickup that may include mint (all work in background worker). Game calls this instead of mint+queue_add when do_mint is true; C# client mints then adds in ProcessAddItemJobsAsync.</summary>
    public void EnqueuePickupWithMintJobOnly(string itemName, string description, string gameSource, string itemType = "KeyItem", bool doMint = false, string? provider = null, string? sendToAddressAfterMinting = null, int quantity = 1)
    {
        if (!IsInitialized())
        {
            OGEngineExports.SetLastBackgroundError("STAR: Pickup not queued (client not initialized).");
            return;
        }
        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(gameSource))
        {
            OGEngineExports.SetLastBackgroundError("STAR: Pickup not queued (item name or game source empty).");
            return;
        }
        var type = string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType;
        var qty = quantity < 1 ? 1 : quantity;
        var storageName = ItemNameWithGameSource(itemName, gameSource);
        _pendingPickupWithMint.Enqueue(new PendingPickupWithMintJob(storageName, description ?? string.Empty, gameSource, type, doMint, provider, sendToAddressAfterMinting, qty));
        /* Show in overlay immediately: merge in GetInventoryAsync uses _localPending, so add here; worker will deduct when add completes. */
        lock (_localPendingLock)
        {
            if (_localPending.TryGetValue(storageName, out var existing))
                existing.Quantity += qty;
            else
                _localPending[storageName] = new LocalPendingEntry { Name = storageName, Description = description ?? string.Empty, GameSource = gameSource, ItemType = type, Quantity = qty };
        }
        _addItemSignal.Release();
        var keysDeltaP = (type.IndexOf("Key", StringComparison.OrdinalIgnoreCase) >= 0) ? 1 : 0;
        EnqueueQuestProgressFromGame(gameSource, 0, 0, itemName, keysDeltaP, 1, null, itemType);
    }

    /// <summary>Queue a monster kill (XP + optional mint + add to inventory). All work runs on the add-item background worker; never blocks.</summary>
    public void EnqueueMonsterKillJobOnly(string engineName, string displayName, int xp, bool isBoss, bool doMint, string? provider, string? gameSource = null)
    {
        if (!IsInitialized())
        {
            OGEngineExports.StarApiLog($"Monster kill NOT queued (client not initialized): {displayName} {xp} XP");
            return;
        }
        if (string.IsNullOrWhiteSpace(engineName) || string.IsNullOrWhiteSpace(displayName))
        {
            OGEngineExports.StarApiLog($"Monster kill NOT queued (empty name): engine='{engineName}' display='{displayName}'");
            return;
        }
        var xpVal = xp < 0 ? 0 : xp;
        var gs = string.IsNullOrWhiteSpace(gameSource) ? "ODOOM" : gameSource;
        OGEngineExports.StarApiLog($"Monster kill queued: {displayName} ({engineName}) {xpVal} XP doMint={doMint} gameSource={gs}");
        /* Optimistic XP update so HUD shows new XP immediately without waiting for background worker. */
        if (xpVal > 0)
            Volatile.Write(ref _cachedAvatarXp, Volatile.Read(ref _cachedAvatarXp) + xpVal);
        StartAddItemWorker();
        _pendingMonsterKill.Enqueue(new PendingMonsterKillJob(engineName, displayName, xpVal, isBoss, doMint, provider ?? "SolanaOASIS", gs));
        _addItemSignal.Release();
        EnqueueQuestProgressFromGame(gs, 1, xpVal, null, 0, 0, null, null, engineName);
    }

    public async Task<OASISResult<bool>> FlushAddItemJobsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        while ((GetLocalPendingCount() > 0 || Volatile.Read(ref _activeAddItemJobs) > 0) && !cancellationToken.IsCancellationRequested)
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
            return FailAndCallback<bool>("Flush add-item jobs was cancelled.", StarApiResultCode.Network);

        return Success(true, StarApiResultCode.Success, "Add-item queue flushed.");
    }

    private int GetLocalPendingCount()
    {
        lock (_localPendingLock)
            return _localPending.Count;
    }

    /// <summary>Subtract quantity from _localPending for itemName (after pickup-with-mint add succeeds so we don't double-count in merge).</summary>
    private void DeductLocalPending(string itemName, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0) return;
        lock (_localPendingLock)
        {
            if (!_localPending.TryGetValue(itemName, out var entry)) return;
            entry.Quantity -= quantity;
            if (entry.Quantity <= 0)
                _localPending.Remove(itemName);
        }
    }

    private async Task<OASISResult<StarItem>> AddItemCoreAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", string? nftId = null, int quantity = 1, bool stack = true, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
        {
            OGEngineExports.StarApiLog("AddItemCoreAsync: not initialized");
            return FailAndCallback<StarItem>("Client is not initialized.", StarApiResultCode.NotInitialized);
        }

        string? avatarId;
        lock (_stateLock)
            avatarId = _avatarId;
        if (string.IsNullOrWhiteSpace(avatarId))
        {
            OGEngineExports.StarApiLog("AddItemCoreAsync: Avatar ID not set (beam-in required)");
            return FailAndCallback<StarItem>("Avatar ID is not set. Complete beam-in (authenticate) first; add_item requires avatar context.", StarApiResultCode.NotInitialized);
        }

        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(gameSource))
        {
            OGEngineExports.StarApiLog("AddItemCoreAsync: missing required param");
            return FailAndCallback<StarItem>("Item name, description, and game source are required.", StarApiResultCode.InvalidParam);
        }

        if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
            return FailAndCallback<StarItem>(missingWeb4, StarApiResultCode.InvalidParam);

        var url = $"{web4Base}/api/avatar/inventory";
        OGEngineExports.StarApiLog($"AddItemCoreAsync: sending POST to {url} name='{itemName}' avatarId={avatarId}");

        try
        {
            var payload = BuildJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("Name", itemName);
                writer.WriteString("Description", $"{description} | Source: {gameSource}");
                writer.WriteNumber("HolonType", 11);
                writer.WriteNumber("Quantity", quantity < 1 ? 1 : quantity);
                writer.WriteBoolean("Stack", stack);
                writer.WriteString("GameSource", gameSource);
                writer.WriteString("ItemType", string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType);
                if (!string.IsNullOrWhiteSpace(nftId))
                {
                    writer.WriteString("NftId", nftId);
                    writer.WritePropertyName("MetaData");
                    writer.WriteStartObject();
                    writer.WriteString("NFTId", nftId);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            });

            var response = await SendRawAsync(HttpMethod.Post, url, payload, cancellationToken).ConfigureAwait(false);
            if (response.IsError)
            {
                OGEngineExports.StarApiLog($"AddItemCoreAsync: response IsError=true message='{response.Message}'");
                return FailAndCallback<StarItem>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
            }
            OGEngineExports.StarApiLog($"AddItemCoreAsync: POST succeeded, parsing response");

            var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
            if (!parseResult)
                return FailAndCallback<StarItem>(parseErrorMessage, parseErrorCode);

            var item = ParseInventoryItemResponse(resultElement);
            if (item is null)
                return FailAndCallback<StarItem>("API did not return the created inventory item.", StarApiResultCode.ApiError);

            /* Use NftId from response when API returns it; otherwise use the nftId we sent so [NFT] prefix shows on first display (Doom/Quake) even if API does not echo it yet. */
            var itemNftId = !string.IsNullOrWhiteSpace(item.NftId) ? item.NftId
                : ExtractMeta(item.MetaData, "NFTId", string.Empty) ?? ExtractMeta(item.MetaData, "OASISNFTId", string.Empty)
                ?? (!string.IsNullOrWhiteSpace(nftId) ? nftId : string.Empty);
            var mapped = new StarItem
            {
                Id = item.Id,
                Name = item.Name ?? itemName,
                Description = item.Description ?? description,
                GameSource = !string.IsNullOrWhiteSpace(item.GameSource) ? item.GameSource : gameSource,
                ItemType = !string.IsNullOrWhiteSpace(item.ItemType) ? item.ItemType : (string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType),
                NftId = itemNftId,
                Quantity = item.Quantity
            };

            lock (_inventoryCacheLock)
            {
                _cachedInventory ??= new List<StarItem>();
                if (stack)
                {
                    var idx = _cachedInventory.FindIndex(x => string.Equals(x.Name, itemName, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                        _cachedInventory[idx] = mapped;
                    else
                        _cachedInventory.Add(mapped);
                }
                else
                    _cachedInventory.Add(mapped);
            }

            OGEngineExports.StarApiLog($"AddItemCoreAsync: item added id={mapped.Id} name='{mapped.Name}' quantity={mapped.Quantity}");
            InvokeCallback(StarApiResultCode.Success);
            return Success(mapped, StarApiResultCode.Success, "Item added successfully.");
        }
        catch (Exception ex)
        {
            OGEngineExports.StarApiLog($"AddItemCoreAsync: exception {ex.GetType().Name} message='{ex.Message}'");
            return FailAndCallback<StarItem>($"Failed to add item: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    /// <summary>Strip UI-only display prefix so we match API-stored names. Backend does not store "[NFT]" or "[BOSSNFT]" in the name.</summary>
    private static string StripNftDisplayPrefix(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name ?? string.Empty;
        var n = name.AsSpan().Trim();
        if (n.StartsWith("[NFT] ", StringComparison.OrdinalIgnoreCase))
            return n.Slice(6).Trim().ToString();
        if (n.StartsWith("[BOSSNFT] ", StringComparison.OrdinalIgnoreCase))
            return n.Slice(10).Trim().ToString();
        return name;
    }

    private void RemoveFromInventoryCache(string itemName, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0) return;
        lock (_inventoryCacheLock)
        {
            if (_cachedInventory is null || _cachedInventory.Count == 0) return;
            var idx = _cachedInventory.FindIndex(x => string.Equals(x.Name, itemName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return;
            var item = _cachedInventory[idx];
            var newQty = item.Quantity - quantity;
            if (newQty <= 0)
                _cachedInventory.RemoveAt(idx);
            else
                _cachedInventory[idx] = new StarItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    GameSource = item.GameSource,
                    ItemType = item.ItemType,
                    NftId = item.NftId,
                    Quantity = newQty
                };
        }
    }

    /// <summary>Record use of an item in a context (e.g. door). quantity: number to consume (default 1). For optimization, prefer deciding access from the already-loaded inventory (local cache) and only call this when you need to record use or when cache is unavailable.</summary>
    public async Task<OASISResult<bool>> UseItemAsync(string itemName, string? context = null, int quantity = 1, CancellationToken cancellationToken = default)
    {
        return await UseItemCoreAsync(itemName, context, quantity, cancellationToken).ConfigureAwait(false);
    }

    public Task<OASISResult<bool>> QueueUseItemAsync(string itemName, string? context = null, int quantity = 1, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized));

        if (string.IsNullOrWhiteSpace(itemName))
            return Task.FromResult(FailAndCallback<bool>("Item name is required.", StarApiResultCode.InvalidParam));

        var tcs = new TaskCompletionSource<OASISResult<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingUseItemJobs.Enqueue(new PendingUseItemJob(itemName, context, quantity, cancellationToken, tcs));
        _useItemSignal.Release();
        return tcs.Task;
    }

    /// <summary>Enqueue one use-item job without returning a completion task. Used by native C sync lib for batching. quantity: number to consume (default 1).</summary>
    public void EnqueueUseItemJobOnly(string itemName, string? context = null, int quantity = 1)
    {
        if (!IsInitialized() || string.IsNullOrWhiteSpace(itemName))
            return;
        _pendingUseItemJobs.Enqueue(new PendingUseItemJob(itemName, context, quantity, CancellationToken.None, null));
        _useItemSignal.Release();
    }

    public async Task<OASISResult<bool>> FlushUseItemJobsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        while ((!_pendingUseItemJobs.IsEmpty || Volatile.Read(ref _activeUseItemJobs) > 0) && !cancellationToken.IsCancellationRequested)
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
            return FailAndCallback<bool>("Flush use-item jobs was cancelled.", StarApiResultCode.Network);

        return Success(true, StarApiResultCode.Success, "Use-item queue flushed.");
    }

    private async Task<OASISResult<bool>> UseItemCoreAsync(string itemName, string? context = null, int quantity = 1, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<bool>("Item name is required.", StarApiResultCode.InvalidParam);

        itemName = StripNftDisplayPrefix(itemName);
        int useQty = quantity > 0 ? quantity : 1;

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

        var item = inventory.Result!.FirstOrDefault(i => string.Equals(i.Name, itemName, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            OGEngineExports.StarApiLog($"UseItem: item not in inventory name='{itemName}'");
            InvokeCallback(StarApiResultCode.Success);
            return Success(false, StarApiResultCode.Success, $"Item '{itemName}' is not in inventory.");
        }

        try
        {
            var payload = BuildJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("Context", string.IsNullOrWhiteSpace(context) ? "game_use" : context);
                writer.WriteString("UsedAt", DateTime.UtcNow.ToString("O"));
                writer.WriteEndObject();
            });

            if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
                return FailAndCallback<bool>(missingWeb4, StarApiResultCode.InvalidParam);

            var url = $"{web4Base}/api/avatar/inventory/{item.Id}";
            if (useQty > 0)
                url += $"?quantity={useQty}";

            OGEngineExports.StarApiLog($"UseItem: DELETE {url} name='{itemName}' context='{context ?? "game_use"}' quantity={useQty} itemId={item.Id}");

            var response = await SendRawAsync(HttpMethod.Delete, url, payload, cancellationToken).ConfigureAwait(false);

            if (response.IsError)
            {
                OGEngineExports.StarApiLog($"UseItem: failed response IsError=true message='{response.Message}'");
                return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
            }

            OGEngineExports.StarApiLog($"UseItem: success name='{itemName}' quantity={useQty} (removed from cache)");
            RemoveFromInventoryCache(itemName, useQty);
            /* Same as pickups: notify active-quest progress API with item identity; server matches objectives (oasisstar.json does not skip this). */
            var gsUse = string.IsNullOrWhiteSpace(item.GameSource) ? "ODOOM" : item.GameSource.Trim();
            if (gsUse.Equals("Quake", StringComparison.OrdinalIgnoreCase)) gsUse = "OQUAKE";
            try { OGEngineExports.StarApiLogFileOnly($"[Quest] use_item -> EnqueueQuestProgressFromGame gs={gsUse} name={item.Name} type={item.ItemType}"); } catch { /* ignore */ }
            EnqueueQuestProgressFromGame(gsUse, 0, 0, item.Name, 0, 1, null, item.ItemType);
            InvokeCallback(StarApiResultCode.Success);
            return Success(true, StarApiResultCode.Success, "Item used successfully.");
        }
        catch (Exception ex)
        {
            OGEngineExports.StarApiLog($"UseItem: exception {ex.GetType().Name} message='{ex.Message}'");
            return FailAndCallback<bool>($"Failed to use item: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    /// <summary>
    /// Checks whether the current avatar can start the quest (NotStarted and prerequisites met). Use for the quest popup to enable/disable the Start button.
    /// </summary>
    public async Task<OASISResult<bool>> CanStartQuestAsync(string questId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId))
            return FailAndCallback<bool>("Quest ID is required.", StarApiResultCode.InvalidParam);

        var avatarIdResult = await EnsureAvatarIdAsync(cancellationToken).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
            return FailAndCallback<bool>(avatarIdResult.Message ?? "Could not resolve avatar ID.", ParseCode(avatarIdResult.ErrorCode, StarApiResultCode.ApiError), avatarIdResult.Exception);

        var canStartUrl = $"{_baseApiUrl}/api/quests/{questId}/can-start";
        var response = await SendRawAsync(HttpMethod.Get, canStartUrl, null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            OGEngineExports.StarApiLogFileOnly($"[Quests] CanStartQuestAsync failed questId={questId} url={canStartUrl} message={response.Message} body={response.Result ?? ""}");
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            OGEngineExports.StarApiLogFileOnly($"[Quests] CanStartQuestAsync parse error questId={questId} url={canStartUrl} err={parseErrorMessage} body={response.Result ?? ""}");
            return FailAndCallback<bool>(parseErrorMessage ?? "Parse error", parseErrorCode);
        }

        var canStart = GetBoolProperty(resultElement, "Result") || GetBoolProperty(resultElement, "result");
        var message = GetStringProperty(resultElement, "Message") ?? GetStringProperty(resultElement, "message");
        InvokeCallback(StarApiResultCode.Success);
        return Success(canStart, StarApiResultCode.Success, message ?? (canStart ? "Quest can be started." : "Quest cannot be started."));
    }

    public async Task<OASISResult<bool>> StartQuestAsync(string questId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
        {
            OGEngineExports.StarApiLog("[Quests] StartQuestAsync: client not initialized");
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        }

        if (string.IsNullOrWhiteSpace(questId))
        {
            OGEngineExports.StarApiLog("[Quests] StartQuestAsync: quest ID is empty");
            return FailAndCallback<bool>("Quest ID is required.", StarApiResultCode.InvalidParam);
        }

        var url = $"{_baseApiUrl}/api/quests/{questId}/start";
        OGEngineExports.StarApiLog($"[Quests] StartQuestAsync: POST {url}");
        /* JSON empty string so ASP.NET [FromBody] string binds; bare POST with no body often yields 400 on newer frameworks. */
        var response = await SendRawAsync(HttpMethod.Post, url, "\"\"", cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            OGEngineExports.StarApiLogFileOnly($"[Quests] StartQuestAsync FAILED questId={questId} url={url} message={response.Message} body={response.Result ?? ""}");
            OGEngineExports.StarApiLog($"[Quests] StartQuestAsync failed: {response.Message} (body in ogengine.log)");
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        OGEngineExports.StarApiLogFileOnly($"[Quests] StartQuestAsync OK questId={questId} url={url} body={response.Result ?? ""}");
        OGEngineExports.StarApiLog("[Quests] StartQuestAsync: OK (see ogengine.log for response body)");
        UpdateQuestStatusInCache(questId, "InProgress");
        try
        {
            var eventsUrl = $"{_baseApiUrl}/api/quests/{questId}/first-objective-events";
            var eventsResponse = await SendRawAsync(HttpMethod.Get, eventsUrl, null, cancellationToken).ConfigureAwait(false);
            if (!eventsResponse.IsError)
                DispatchCrossGameEventsFromProgressResponse(eventsResponse.Result, string.Empty);
        }
        catch (Exception ex2)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quests] StartQuestAsync: first-objective-events fetch failed (non-fatal): {ex2.Message}"); } catch { /* ignore */ }
        }
        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Quest started successfully.");
    }

    /// <summary>Run start-quest on the <see cref="DedicatedWorker.Quests"/> queue (same as progress POST) so it is not stuck behind generic jobs (NFT mint, add-item, etc.). On success, the cached quest's status is updated in-place.</summary>
    public Task<OASISResult<bool>> QueueStartQuestAsync(string questId, CancellationToken cancellationToken = default) =>
        RunOnWorkerAsync(DedicatedWorker.Quests, ct => StartQuestAsync(questId, ct), cancellationToken);

    public async Task<OASISResult<bool>> CompleteQuestObjectiveAsync(string questId, string objectiveId, string? gameSource = null, CancellationToken cancellationToken = default)
    {
        return await CompleteQuestObjectiveCoreAsync(questId, objectiveId, gameSource, cancellationToken).ConfigureAwait(false);
    }

    public Task<OASISResult<bool>> QueueCompleteQuestObjectiveAsync(string questId, string objectiveId, string? gameSource = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized));

        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId))
            return Task.FromResult(FailAndCallback<bool>("Quest ID and objective ID are required.", StarApiResultCode.InvalidParam));

        var tcs = new TaskCompletionSource<OASISResult<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingQuestObjectiveJobs.Enqueue(new PendingQuestObjectiveJob(questId, objectiveId, gameSource, cancellationToken, tcs));
        _questObjectiveSignal.Release();
        return tcs.Task;
    }

    public async Task<OASISResult<bool>> FlushQuestObjectiveJobsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        while ((!_pendingQuestObjectiveJobs.IsEmpty || Volatile.Read(ref _activeQuestObjectiveJobs) > 0) && !cancellationToken.IsCancellationRequested)
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
            return FailAndCallback<bool>("Flush quest objective jobs was cancelled.", StarApiResultCode.Network);

        return Success(true, StarApiResultCode.Success, "Quest objective queue flushed.");
    }

    private async Task<OASISResult<bool>> CompleteQuestObjectiveCoreAsync(string questId, string objectiveId, string? gameSource = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId))
            return FailAndCallback<bool>("Quest ID and objective ID are required.", StarApiResultCode.InvalidParam);

        var gs = string.IsNullOrWhiteSpace(gameSource) ? "Unknown" : gameSource;
        OGEngineExports.StarApiLogFileOnly($"[Quest] Complete objective: questId={questId} objectiveId={objectiveId} gameSource={gs}");

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("questId", questId);
            writer.WriteString("objectiveId", objectiveId);
            writer.WriteString("gameSource", gs);
            writer.WriteString("completionNotes", $"Completed objective {objectiveId} at {DateTime.UtcNow:O}");
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/objectives/complete", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            OGEngineExports.StarApiLogFileOnly($"[Quest] Complete objective failed: questId={questId} objectiveId={objectiveId} error={response.Message}");
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        OGEngineExports.StarApiLogFileOnly($"[Quest] Complete objective OK: questId={questId} objectiveId={objectiveId} gameSource={gs}");
        InvalidateQuestCache();
        RequestQuestCacheRefreshInBackground();
        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Quest objective completed successfully.");
    }

}
