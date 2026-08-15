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

    private static JsonElement UnwrapQuestListRoot(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object && TryGetProperty(element, "Quests", out var questsElement))
            element = questsElement;
        if (element.ValueKind == JsonValueKind.Object && (TryGetProperty(element, "Result", out var resultArr) || TryGetProperty(element, "result", out resultArr)) && resultArr.ValueKind == JsonValueKind.Array)
            element = resultArr;
        return element;
    }

    private static void AppendDictionaryOfStringListsAudit(StringBuilder sb, string indent, string dictLabel, Dictionary<string, List<string>>? d)
    {
        if (d == null || d.Count == 0) return;
        foreach (var kv in d)
        {
            var vals = kv.Value != null ? string.Join("|", kv.Value) : "";
            sb.Append(indent).Append(dictLabel).Append('[').Append(kv.Key).Append("]=[").Append(vals).Append(']').AppendLine();
        }
    }

    private static void AppendObjectiveDictionariesAudit(StringBuilder sb, string indent, StarQuestObjectiveDictionaries? dicts)
    {
        if (dicts == null)
        {
            sb.Append(indent).AppendLine("dictionaries: (null — no Need*/progress fields parsed; inspect raw JSON chunks above for MetaData vs root keys)");
            return;
        }

        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToKillMonsters", dicts.NeedToKillMonsters);
        AppendDictionaryOfStringListsAudit(sb, indent, "MonstersKilled", dicts.MonstersKilled);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToCollectKeys", dicts.NeedToCollectKeys);
        AppendDictionaryOfStringListsAudit(sb, indent, "KeysCollected", dicts.KeysCollected);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToCollectItems", dicts.NeedToCollectItems);
        AppendDictionaryOfStringListsAudit(sb, indent, "ItemsCollected", dicts.ItemsCollected);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToEarnXP", dicts.NeedToEarnXP);
        AppendDictionaryOfStringListsAudit(sb, indent, "XPEarnt", dicts.XPEarnt);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToCompleteLevel", dicts.NeedToCompleteLevel);
        AppendDictionaryOfStringListsAudit(sb, indent, "LevelsCompleted", dicts.LevelsCompleted);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToCollectArmor", dicts.NeedToCollectArmor);
        AppendDictionaryOfStringListsAudit(sb, indent, "ArmorCollected", dicts.ArmorCollected);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToCollectHealth", dicts.NeedToCollectHealth);
        AppendDictionaryOfStringListsAudit(sb, indent, "HealthCollected", dicts.HealthCollected);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToCollectAmmo", dicts.NeedToCollectAmmo);
        AppendDictionaryOfStringListsAudit(sb, indent, "AmmoCollected", dicts.AmmoCollected);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToCollectWeapons", dicts.NeedToCollectWeapons);
        AppendDictionaryOfStringListsAudit(sb, indent, "WeaponsCollected", dicts.WeaponsCollected);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToCollectPowerups", dicts.NeedToCollectPowerups);
        AppendDictionaryOfStringListsAudit(sb, indent, "PowerupsCollected", dicts.PowerupsCollected);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToUseWeapons", dicts.NeedToUseWeapons);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToUsePowerups", dicts.NeedToUsePowerups);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToEarnKarma", dicts.NeedToEarnKarma);
        AppendDictionaryOfStringListsAudit(sb, indent, "KarmaEarnt", dicts.KarmaEarnt);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToGoToGeoHotSpots", dicts.NeedToGoToGeoHotSpots);
        AppendDictionaryOfStringListsAudit(sb, indent, "GeoHotSpotsArrived", dicts.GeoHotSpotsArrived);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToCompleteInMins", dicts.NeedToCompleteInMins);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToVisitLocations", dicts.NeedToVisitLocations);
        AppendDictionaryOfStringListsAudit(sb, indent, "NeedToSurviveMins", dicts.NeedToSurviveMins);
        AppendDictionaryOfStringListsAudit(sb, indent, "TimeStarted", dicts.TimeStarted);
        AppendDictionaryOfStringListsAudit(sb, indent, "TimeEnded", dicts.TimeEnded);
        AppendDictionaryOfStringListsAudit(sb, indent, "TimeTaken", dicts.TimeTaken);
    }

    private static void LogParsedQuestListModelAudit(string parseSource, List<StarQuestInfo> quests)
    {
        if (!VerboseQuestParseLogsEnabled) return;
        try
        {
            var sb = new StringBuilder(Math.Max(4096, quests.Count * 256));
            sb.Append("[Quest][Parse] MODEL after parse source=").Append(parseSource)
                .Append(" questRowCount=").AppendLine(quests.Count.ToString(CultureInfo.InvariantCulture));
            for (var q = 0; q < quests.Count; q++)
            {
                var qi = quests[q];
                sb.Append("  [").Append(q).Append("] Id=").Append(qi.Id ?? "")
                    .Append(" Name=").Append(qi.Name ?? "")
                    .Append(" Status=").Append(qi.Status ?? "")
                    .Append(" ParentQuestId=").Append(qi.ParentQuestId ?? "")
                    .Append(" GameSource=").Append(qi.GameSource ?? "")
                    .Append(" objectiveCount=").AppendLine((qi.Objectives?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("      quest-level dicts:");
                AppendObjectiveDictionariesAudit(sb, "        ", qi.Dictionaries);
                if (qi.Objectives == null || qi.Objectives.Count == 0)
                {
                    sb.AppendLine("      objectives: (none)");
                    continue;
                }

                for (var o = 0; o < qi.Objectives.Count; o++)
                {
                    var oj = qi.Objectives[o];
                    sb.Append("      objective[").Append(o).Append("] Id=").Append(oj.Id)
                        .Append(" Order=").Append(oj.Order)
                        .Append(" IsCompleted=").Append(oj.IsCompleted)
                        .Append(" GameSource=").Append(oj.GameSource ?? "").AppendLine();
                    sb.Append("         Title=").Append(oj.Title ?? "").AppendLine();
                    sb.Append("         Description=").Append(oj.Description ?? "").AppendLine();
                    sb.Append("         ProgressSummary=").Append(oj.ProgressSummary ?? "").AppendLine();
                    AppendObjectiveDictionariesAudit(sb, "         ", oj.Dictionaries);
                }
            }

            OGEngineExports.StarApiLogFileOnly(sb.ToString());
        }
        catch (Exception ex)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quest][Parse] LogParsedQuestListModelAudit error: {ex.Message}"); } catch { /* ignore */ }
        }
    }

    private static void LogParsedSingleQuestModelAudit(string parseSource, StarQuestInfo? qi)
    {
        if (qi == null)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quest][Parse] MODEL source={parseSource} quest=(null)"); } catch { /* ignore */ }
            return;
        }

        LogParsedQuestListModelAudit(parseSource, new List<StarQuestInfo> { qi });
    }

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

}
