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

    public async Task<OASISResult<bool>> CompleteQuestAsync(string questId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId))
            return FailAndCallback<bool>("Quest ID is required.", StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/{questId}/complete", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvalidateQuestCache();
        RequestQuestCacheRefreshInBackground();
        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Quest completed successfully.");
    }

    private static Dictionary<string, List<string>> CloneStringListDict(Dictionary<string, List<string>> src)
    {
        var d = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in src)
            d[kv.Key] = kv.Value != null ? new List<string>(kv.Value) : new List<string>();
        return d;
    }

    /// <summary>Increment progress counter for one game key in a requirement dictionary (first list element = tally).</summary>
    private static void AddProgressToGameKeyedDict(Dictionary<string, List<string>> dict, string game, int delta)
    {
        if (delta == 0) return;
        if (!dict.TryGetValue(game, out var list) || list is null)
        {
            list = new List<string> { "0" };
            dict[game] = list;
        }
        if (list.Count < 1) list.Add("0");
        var cur = int.TryParse(list[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
        list[0] = (cur + delta).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Pick the progress-dictionary game key that matches <paramref name="preferredGame"/> to a Need* dictionary key (e.g. ODOOM vs Doom). Requirement lines use Need keys when reading progress.</summary>
    private static string ResolveProgressDictionaryKey(Dictionary<string, List<string>>? need, string preferredGame)
    {
        if (string.IsNullOrWhiteSpace(preferredGame)) preferredGame = "ODOOM";
        if (need == null || need.Count == 0) return preferredGame;
        foreach (var kv in need)
        {
            if (string.Equals(kv.Key, preferredGame, StringComparison.OrdinalIgnoreCase))
                return kv.Key;
        }
        static bool GameKeysAlias(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            static string Norm(string s) =>
                s.Replace(" ", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
            var na = Norm(a);
            var nb = Norm(b);
            if (na.Equals(nb, StringComparison.OrdinalIgnoreCase)) return true;
            var aDoom = na.Equals("DOOM", StringComparison.OrdinalIgnoreCase) || na.Equals("ODOOM", StringComparison.OrdinalIgnoreCase);
            var bDoom = nb.Equals("DOOM", StringComparison.OrdinalIgnoreCase) || nb.Equals("ODOOM", StringComparison.OrdinalIgnoreCase);
            return aDoom && bDoom;
        }
        foreach (var kv in need)
        {
            if (GameKeysAlias(kv.Key, preferredGame))
                return kv.Key;
        }
        if (need.Count == 1)
            return need.Keys.First();
        return preferredGame;
    }

    /// <summary>Matches server <see cref="QuestManager"/> game-key resolution: exact id or Doom/Quake aliases only — no single-key fallback so cross-game rows are not credited incorrectly.</summary>
    private static string? ResolveMergeGameKey(Dictionary<string, List<string>>? need, string gs)
    {
        if (need == null || need.Count == 0) return null;
        if (string.IsNullOrWhiteSpace(gs)) gs = "ODOOM";
        if (need.ContainsKey(gs)) return gs;
        foreach (var k in need.Keys)
        {
            if (string.Equals(k, gs, StringComparison.OrdinalIgnoreCase)) return k;
            static string Norm(string s) =>
                s.Replace(" ", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
            var na = Norm(k);
            var nb = Norm(gs);
            if (na.Equals(nb, StringComparison.OrdinalIgnoreCase)) return k;
            var aDoom = na.Equals("DOOM", StringComparison.OrdinalIgnoreCase) || na.Equals("ODOOM", StringComparison.OrdinalIgnoreCase);
            var bDoom = nb.Equals("DOOM", StringComparison.OrdinalIgnoreCase) || nb.Equals("ODOOM", StringComparison.OrdinalIgnoreCase);
            if (aDoom && bDoom) return k;
            var aQ = na.Equals("QUAKE", StringComparison.OrdinalIgnoreCase) || na.Equals("OQUAKE", StringComparison.OrdinalIgnoreCase);
            var bQ = nb.Equals("QUAKE", StringComparison.OrdinalIgnoreCase) || nb.Equals("OQUAKE", StringComparison.OrdinalIgnoreCase);
            if (aQ && bQ) return k;
        }
        return null;
    }

    private static void AddProgressForNeedPair(Dictionary<string, List<string>>? need, Dictionary<string, List<string>> progress, string preferredGame, int delta)
    {
        if (delta == 0) return;
        var key = ResolveProgressDictionaryKey(need, preferredGame);
        AddProgressToGameKeyedDict(progress, key, delta);
    }

    private static Dictionary<string, List<string>>? FirstNonEmptyWeaponsNeed(StarQuestObjectiveDictionaries d)
    {
        if (d.NeedToCollectWeapons is { Count: > 0 }) return d.NeedToCollectWeapons;
        if (d.NeedToUseWeapons is { Count: > 0 }) return d.NeedToUseWeapons;
        return null;
    }

    private static Dictionary<string, List<string>>? FirstNonEmptyPowerupsNeed(StarQuestObjectiveDictionaries d)
    {
        if (d.NeedToCollectPowerups is { Count: > 0 }) return d.NeedToCollectPowerups;
        if (d.NeedToUsePowerups is { Count: > 0 }) return d.NeedToUsePowerups;
        return null;
    }

    /// <summary>True if any Need* dict used by <see cref="FormatRequirementProgressLines"/> has a positive requirement.</summary>
    private static bool ObjectiveHasFormattedRequirementLines(StarQuestObjectiveDictionaries d)
    {
        static bool AnyPositive(Dictionary<string, List<string>>? need)
        {
            if (need == null) return false;
            foreach (var kv in need)
            {
                if (GetFirstPositiveIntFromStringList(kv.Value) > 0) return true;
            }
            return false;
        }
        return AnyPositive(d.NeedToKillMonsters)
               || AnyPositive(d.NeedToCollectArmor)
               || AnyPositive(d.NeedToCollectAmmo)
               || AnyPositive(d.NeedToCollectHealth)
               || AnyPositive(d.NeedToCollectWeapons)
               || AnyPositive(d.NeedToCollectPowerups)
               || AnyPositive(d.NeedToCollectItems)
               || AnyPositive(d.NeedToCollectKeys)
               || AnyPositive(d.NeedToCompleteLevel)
               || AnyPositive(d.NeedToEarnKarma)
               || AnyPositive(d.NeedToEarnXP)
               || AnyPositive(d.NeedToGoToGeoHotSpots)
               || AnyPositive(d.NeedToUseWeapons)
               || AnyPositive(d.NeedToUsePowerups);
    }

    /// <summary>Whether cached progress satisfies every Need* row that <see cref="FormatRequirementProgressLines"/> would emit (same key pairing).</summary>
    private static bool ObjectiveMeetsAllFormattedRequirements(StarQuestObjectiveDictionaries d)
    {
        static bool PairMet(Dictionary<string, List<string>>? need, Dictionary<string, List<string>>? progress)
        {
            if (need == null || need.Count == 0) return true;
            foreach (var kv in need)
            {
                var reqList = kv.Value;
                var required = GetFirstPositiveIntFromStringList(reqList);
                if (required <= 0) continue;
                var current = 0;
                if (progress != null && progress.TryGetValue(kv.Key, out var pl) && pl is { Count: > 0 })
                    current = GetFirstNonNegativeIntFromStringList(pl);
                if (current < required) return false;
            }
            return true;
        }
        return PairMet(d.NeedToKillMonsters, d.MonstersKilled)
               && PairMet(d.NeedToCollectArmor, d.ArmorCollected)
               && PairMet(d.NeedToCollectAmmo, d.AmmoCollected)
               && PairMet(d.NeedToCollectHealth, d.HealthCollected)
               && PairMet(d.NeedToCollectWeapons, d.WeaponsCollected)
               && PairMet(d.NeedToCollectPowerups, d.PowerupsCollected)
               && PairMet(d.NeedToCollectItems, d.ItemsCollected)
               && PairMet(d.NeedToCollectKeys, d.KeysCollected)
               && PairMet(d.NeedToCompleteLevel, d.LevelsCompleted)
               && PairMet(d.NeedToEarnKarma, d.KarmaEarnt)
               && PairMet(d.NeedToEarnXP, d.XPEarnt)
               && PairMet(d.NeedToGoToGeoHotSpots, d.GeoHotSpotsArrived)
               && PairMet(d.NeedToUseWeapons, d.WeaponsCollected)
               && PairMet(d.NeedToUsePowerups, d.PowerupsCollected);
    }

    /// <summary>Incomplete objectives on one quest, in merge order: profile <paramref name="activeObjectiveId"/> first (if in list), then others by <see cref="StarQuestObjective.Order"/>.</summary>
    private static IEnumerable<StarQuestObjective> OrderIncompleteObjectivesForProgressMerge(StarQuestInfo quest, Guid? activeObjectiveId)
    {
        if (quest.Objectives is not { Count: > 0 }) yield break;
        var incomplete = quest.Objectives.Where(o => !o.IsCompleted).ToList();
        StarQuestObjective? activeFirst = null;
        if (activeObjectiveId.HasValue && activeObjectiveId.Value != Guid.Empty)
        {
            var sid = activeObjectiveId.Value.ToString("D");
            activeFirst = incomplete.FirstOrDefault(o => string.Equals(o.Id, sid, StringComparison.OrdinalIgnoreCase));
        }
        if (activeFirst != null)
        {
            yield return activeFirst;
            incomplete.Remove(activeFirst);
        }
        foreach (var o in incomplete.OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            yield return o;
    }

}
