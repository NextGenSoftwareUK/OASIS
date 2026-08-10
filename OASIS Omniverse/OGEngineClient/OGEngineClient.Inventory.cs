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

    /// <summary>Mirror successful progress POST into cached quest objective dictionaries so UI updates without GET all-for-avatar. Only the <strong>active quest</strong> (<paramref name="questId"/> = cached ActiveQuestId), not every InProgress quest. Each delta is applied to every incomplete objective on that quest whose need dict matches <paramref name="gameSource"/> (same as server ApplyQuestProgressAsync). Processing order: active objective first, then by Order. Returns false if cache could not be updated (caller should refetch).</summary>
    private bool MergeQuestProgressIntoLocalCache(Guid questId, string gameSource, int monstersKilledDelta, int xpEarnedDelta, int keysCollectedDelta, int armorDelta, int healthDelta, int weaponsDelta, int powerupsDelta, int ammoDelta, int genericItemPickup)
    {
        var gs = string.IsNullOrWhiteSpace(gameSource) ? "ODOOM" : gameSource.Trim();
        var qid = questId.ToString("D");
        Guid? activeObjId;
        lock (_stateLock) { activeObjId = _cachedActiveObjectiveId; }

        lock (_questsCacheLock)
        {
            if (_cachedQuestList is null || _cachedQuestList.Count == 0)
            {
                try { OGEngineExports.StarApiLogFileOnly("[Quest] Merge cache SKIP: _cachedQuestList empty"); } catch { /* ignore */ }
                return false;
            }
            var idx = _cachedQuestList.FindIndex(q => string.Equals(q.Id, qid, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                try { OGEngineExports.StarApiLogFileOnly($"[Quest] Merge cache SKIP: quest {qid} not in cache"); } catch { /* ignore */ }
                return false;
            }
            var quest = _cachedQuestList[idx];
            if (quest.Objectives is null || quest.Objectives.Count == 0)
            {
                try { OGEngineExports.StarApiLogFileOnly($"[Quest] Merge cache SKIP: quest {qid} has no objectives"); } catch { /* ignore */ }
                return false;
            }

            var objIds = string.Join(",", quest.Objectives.Select(o => o.Id));
            var touched = new List<string>();
            var killMergedToObjective = false;
            var xpMergedToObjective = false;
            foreach (var target in OrderIncompleteObjectivesForProgressMerge(quest, activeObjId))
            {
                target.Dictionaries ??= new StarQuestObjectiveDictionaries();
                var d = target.Dictionaries;
                var hadAny = false;
                string? k;

                k = ResolveMergeGameKey(d.NeedToKillMonsters, gs);
                if (monstersKilledDelta != 0 && k != null)
                {
                    AddProgressToGameKeyedDict(d.MonstersKilled, k, monstersKilledDelta);
                    hadAny = true;
                    killMergedToObjective = true;
                }

                k = ResolveMergeGameKey(d.NeedToEarnXP, gs);
                if (xpEarnedDelta != 0 && k != null)
                {
                    AddProgressToGameKeyedDict(d.XPEarnt, k, xpEarnedDelta);
                    hadAny = true;
                    xpMergedToObjective = true;
                }

                k = ResolveMergeGameKey(d.NeedToCollectKeys, gs);
                if (keysCollectedDelta != 0 && k != null)
                {
                    AddProgressToGameKeyedDict(d.KeysCollected, k, keysCollectedDelta);
                    hadAny = true;
                }

                k = ResolveMergeGameKey(d.NeedToCollectArmor, gs);
                if (armorDelta != 0 && k != null)
                {
                    AddProgressToGameKeyedDict(d.ArmorCollected, k, armorDelta);
                    hadAny = true;
                }

                k = ResolveMergeGameKey(d.NeedToCollectHealth, gs);
                if (healthDelta != 0 && k != null)
                {
                    AddProgressToGameKeyedDict(d.HealthCollected, k, healthDelta);
                    hadAny = true;
                }

                var wneed = FirstNonEmptyWeaponsNeed(d);
                k = ResolveMergeGameKey(wneed, gs);
                if (weaponsDelta != 0 && k != null)
                {
                    AddProgressToGameKeyedDict(d.WeaponsCollected, k, weaponsDelta);
                    hadAny = true;
                }

                var pneed = FirstNonEmptyPowerupsNeed(d);
                k = ResolveMergeGameKey(pneed, gs);
                if (powerupsDelta != 0 && k != null)
                {
                    AddProgressToGameKeyedDict(d.PowerupsCollected, k, powerupsDelta);
                    hadAny = true;
                }

                k = ResolveMergeGameKey(d.NeedToCollectAmmo, gs);
                if (ammoDelta != 0 && k != null)
                {
                    AddProgressToGameKeyedDict(d.AmmoCollected, k, ammoDelta);
                    hadAny = true;
                }

                k = ResolveMergeGameKey(d.NeedToCollectItems, gs);
                if (genericItemPickup != 0 && k != null)
                {
                    AddProgressToGameKeyedDict(d.ItemsCollected, k, genericItemPickup);
                    hadAny = true;
                }

                if (hadAny)
                {
                    touched.Add(target.Id);
                    if (ObjectiveHasFormattedRequirementLines(d) && ObjectiveMeetsAllFormattedRequirements(d))
                        target.IsCompleted = true;
                }
            }

            /* Quest-level dictionaries: some payloads put NeedToKillMonsters / NeedToEarnXP only on the quest. Mirror onto first incomplete objective when no objective row matched that delta. */
            var qRoot = quest.Dictionaries;
            if (qRoot != null && monstersKilledDelta != 0 && !killMergedToObjective && qRoot.NeedToKillMonsters is { Count: > 0 })
            {
                var qk = ResolveMergeGameKey(qRoot.NeedToKillMonsters, gs);
                if (qk != null)
                {
                    foreach (var target in OrderIncompleteObjectivesForProgressMerge(quest, activeObjId))
                    {
                        target.Dictionaries ??= new StarQuestObjectiveDictionaries();
                        var d = target.Dictionaries;
                        var ok = ResolveMergeGameKey(d.NeedToKillMonsters, gs);
                        if (ok == null && (d.NeedToKillMonsters == null || d.NeedToKillMonsters.Count == 0))
                        {
                            d.NeedToKillMonsters = CloneStringListDict(qRoot.NeedToKillMonsters);
                            d.MonstersKilled = qRoot.MonstersKilled != null
                                ? CloneStringListDict(qRoot.MonstersKilled)
                                : new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                        }
                        ok = ResolveMergeGameKey(d.NeedToKillMonsters, gs);
                        if (ok == null) continue;
                        AddProgressToGameKeyedDict(d.MonstersKilled, ok, monstersKilledDelta);
                        qRoot.MonstersKilled ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                        AddProgressToGameKeyedDict(qRoot.MonstersKilled, qk, monstersKilledDelta);
                        touched.Add(target.Id);
                        if (ObjectiveHasFormattedRequirementLines(d) && ObjectiveMeetsAllFormattedRequirements(d))
                            target.IsCompleted = true;
                        break;
                    }
                }
            }
            if (qRoot != null && xpEarnedDelta != 0 && !xpMergedToObjective && qRoot.NeedToEarnXP is { Count: > 0 })
            {
                var qkXp = ResolveMergeGameKey(qRoot.NeedToEarnXP, gs);
                if (qkXp != null)
                {
                    foreach (var target in OrderIncompleteObjectivesForProgressMerge(quest, activeObjId))
                    {
                        target.Dictionaries ??= new StarQuestObjectiveDictionaries();
                        var d = target.Dictionaries;
                        var okXp = ResolveMergeGameKey(d.NeedToEarnXP, gs);
                        if (okXp == null && (d.NeedToEarnXP == null || d.NeedToEarnXP.Count == 0))
                        {
                            d.NeedToEarnXP = CloneStringListDict(qRoot.NeedToEarnXP);
                            d.XPEarnt = qRoot.XPEarnt != null
                                ? CloneStringListDict(qRoot.XPEarnt)
                                : new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                        }
                        okXp = ResolveMergeGameKey(d.NeedToEarnXP, gs);
                        if (okXp == null) continue;
                        AddProgressToGameKeyedDict(d.XPEarnt, okXp, xpEarnedDelta);
                        qRoot.XPEarnt ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                        AddProgressToGameKeyedDict(qRoot.XPEarnt, qkXp, xpEarnedDelta);
                        touched.Add(target.Id);
                        if (ObjectiveHasFormattedRequirementLines(d) && ObjectiveMeetsAllFormattedRequirements(d))
                            target.IsCompleted = true;
                        break;
                    }
                }
            }

            if (quest.Objectives is { Count: > 0 } && quest.Objectives.TrueForAll(o => o.IsCompleted))
                quest.Status = "Completed";

            var ocAfter = quest.Objectives.Count;
            var doneAfter = quest.Objectives.Count(o => o.IsCompleted);
            var pctAfter = ocAfter > 0 ? doneAfter * 100 / ocAfter : 0;
            try { OGEngineExports.StarApiLogFileOnly($"[Quest] Merge cache post-pct: quest={qid} completedObjectives={doneAfter}/{ocAfter} listPct={pctAfter} questStatus={quest.Status ?? ""}"); } catch { /* ignore */ }

            try
            {
                if (VerboseQuestListLogsEnabled)
                    OGEngineExports.StarApiLogFileOnly($"[Quest] Merge cache [verbose]: quest={qid} gs={gs} profileActiveObjective={activeObjId?.ToString("D") ?? ""} objectiveIds=[{objIds}] touchedObjectiveIds=[{string.Join(",", touched)}]");
                else
                    OGEngineExports.StarApiLogFileOnly($"[Quest] Merge cache: quest={qid} touchedObjectives={touched.Count} gs={gs}");
            }
            catch { /* ignore */ }

            var hadDelta = monstersKilledDelta != 0 || xpEarnedDelta != 0 || keysCollectedDelta != 0 || armorDelta != 0 || healthDelta != 0 || weaponsDelta != 0 || powerupsDelta != 0 || ammoDelta != 0 || genericItemPickup != 0;
            if (hadDelta && touched.Count == 0)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.Append("[Quest] Merge cache NO_MATCH: no objective need-dict matched this delta (check JSON parsing / game keys). deltas kills=").Append(monstersKilledDelta).Append(" xp=").Append(xpEarnedDelta).Append(" keys=").Append(keysCollectedDelta).Append(" armor=").Append(armorDelta).Append(" health=").Append(healthDelta).Append(" weapons=").Append(weaponsDelta).Append(" powerups=").Append(powerupsDelta).Append(" ammo=").Append(ammoDelta).Append(" generic=").Append(genericItemPickup).Append(" gs=").Append(gs);
                    foreach (var o in quest.Objectives.Where(x => !x.IsCompleted))
                    {
                        var d = o.Dictionaries;
                        if (d == null) { sb.Append(" | obj=").Append(o.Id).Append(" dicts=null"); continue; }
                        static string K(Dictionary<string, List<string>>? x) => x == null || x.Count == 0 ? "-" : string.Join(",", x.Keys);
                        sb.Append(" | obj=").Append(o.Id).Append(" armorNeed[").Append(K(d.NeedToCollectArmor)).Append("] healthNeed[").Append(K(d.NeedToCollectHealth)).Append("] ammoNeed[").Append(K(d.NeedToCollectAmmo)).Append("] killsNeed[").Append(K(d.NeedToKillMonsters)).Append(']');
                    }
                    OGEngineExports.StarApiLogFileOnly(sb.ToString());
                }
                catch { /* ignore */ }
            }

            _questsCacheString = SerializeQuestsForGame(_cachedQuestList);
            _questsFilterLastLogTop = (0, 0);
            _questsFilterLastLogObjectives = ("", -1);
            _questsFilterLastLogSubQuests = ("", -1);
            _questsFilterLastLogPrereqs = ("", -1);
            return true;
        }
    }

    /// <summary>POST /api/quests/{activeQuestId}/progress — realtime objective progress (kills, XP, pickups by type, level time). No-op if no active quest or all deltas are zero. Backend must expose this route (e.g. STAR ODK QuestsController); 404 means the URL (e.g. ONODE) may not have the progress endpoint.</summary>
    private async Task ApplyQuestProgressToActiveQuestAsync(string gameSource, int monstersKilledDelta, int xpEarnedDelta, string? itemCollectedName, int keysCollectedDelta, int armorDelta, int healthDelta, int weaponsDelta, int powerupsDelta, int ammoDelta, int genericItemPickup, int? levelTimeSeconds, string? monsterKilledClassname, CancellationToken cancellationToken)
    {
        if (!IsInitialized() || string.IsNullOrWhiteSpace(_baseApiUrl)) return;
        Guid? qid;
        Guid? activeObjectiveId;
        lock (_stateLock)
        {
            qid = _cachedActiveQuestId;
            activeObjectiveId = _cachedActiveObjectiveId;
        }
        if (!qid.HasValue || qid.Value == Guid.Empty)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quest] Progress SKIP: no cached active quest id (beam-in / start a quest so avatar profile loads ActiveQuestId). itemName={itemCollectedName ?? ""}"); } catch { /* ignore */ }
            return;
        }
        var gs = string.IsNullOrWhiteSpace(gameSource) ? "ODOOM" : gameSource.Trim();
        var mkc = string.IsNullOrWhiteSpace(monsterKilledClassname) ? null : monsterKilledClassname.Trim();
        var hasDeltas = monstersKilledDelta != 0 || xpEarnedDelta != 0 || keysCollectedDelta != 0 || armorDelta != 0 || healthDelta != 0 || weaponsDelta != 0 || powerupsDelta != 0 || ammoDelta != 0 || genericItemPickup != 0 || (levelTimeSeconds.HasValue && levelTimeSeconds.Value > 0) || mkc != null;
        if (!hasDeltas)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quest] Progress SKIP: all deltas zero (itemName={itemCollectedName ?? ""}, genericItem={genericItemPickup}, armor={armorDelta}, health={healthDelta})"); } catch { /* ignore */ }
            return; /* Do not send progress when nothing changed (avoids 0-delta calls and reduces 404s if backend route is missing). */
        }
        /* Always POST progress while quest popup is open. Skipping POST here previously dropped persistence (armor/keys-style deltas) while the UI still looked updated from earlier merges — reload then showed 0% from server. GET-all-for-avatar refresh remains discarded while popup is open (see quest cache refresh). */
        OGEngineExports.StarApiLogFileOnly($"[Quest] Progress: questId={qid.Value} gameSource={gs} kills={monstersKilledDelta} xp={xpEarnedDelta} keys={keysCollectedDelta} armor={armorDelta} health={healthDelta} weapons={weaponsDelta} powerups={powerupsDelta} ammo={ammoDelta} genericItem={genericItemPickup} itemName={itemCollectedName ?? ""} levelTimeSec={levelTimeSeconds} classname={mkc ?? ""} questPopupOpen={Volatile.Read(ref _questUiPopupOpen)}");
        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("gameSource", gs);
            if (activeObjectiveId.HasValue && activeObjectiveId.Value != Guid.Empty)
                writer.WriteString("activeObjectiveId", activeObjectiveId.Value.ToString("D"));
            writer.WriteNumber("monstersKilledDelta", monstersKilledDelta);
            writer.WriteNumber("xpEarnedDelta", xpEarnedDelta);
            writer.WriteNumber("keysCollectedDelta", keysCollectedDelta);
            writer.WriteNumber("armorCollectedDelta", armorDelta);
            writer.WriteNumber("healthCollectedDelta", healthDelta);
            writer.WriteNumber("weaponsCollectedDelta", weaponsDelta);
            writer.WriteNumber("powerupsCollectedDelta", powerupsDelta);
            writer.WriteNumber("ammoCollectedDelta", ammoDelta);
            writer.WriteNumber("genericItemPickup", genericItemPickup);
            writer.WriteString("itemCollectedName", itemCollectedName ?? string.Empty);
            if (levelTimeSeconds.HasValue)
                writer.WriteNumber("levelTimeSeconds", levelTimeSeconds.Value);
            if (mkc != null)
                writer.WriteString("monsterKilledClassname", mkc);
            writer.WriteEndObject();
        });
        QuestProgressCacheRefreshMode mode;
        lock (_stateLock) { mode = _questProgressCacheRefresh; }
        /* Client-merge mode: update local quest dictionaries before POST so HUD shows Killed X/Y immediately even if progress endpoint is slow or returns an error. */
        var mergedOptimistically = false;
        if (mode == QuestProgressCacheRefreshMode.ClientCacheMerge)
        {
            mergedOptimistically = MergeQuestProgressIntoLocalCache(qid.Value, gs, monstersKilledDelta, xpEarnedDelta, keysCollectedDelta, armorDelta, healthDelta, weaponsDelta, powerupsDelta, ammoDelta, genericItemPickup);
            if (mergedOptimistically)
                OGEngineExports.StarApiLogFileOnly("[Quest] Progress merge applied (native quests-cache-refreshed callback suppressed).");
        }
        var url = $"{_baseApiUrl}/api/quests/{qid.Value:D}/progress";
        try
        {
            var response = await SendRawAsync(HttpMethod.Post, url, payload, cancellationToken).ConfigureAwait(false);
            OGEngineExports.StarApiLogFileOnly($"[Quest] Progress result: {(response.IsError ? "FAIL" : "OK")} {(response.IsError ? response.Message ?? "" : "")}");
            if (!response.IsError)
            {
                lock (_stateLock) { _questLastProgressGameSource = gs; }
                try
                {
                    OGEngineExports.StarApiLogFileOnly($"[Quest] Progress OK: cache refresh mode={(mode == QuestProgressCacheRefreshMode.FullServerRefresh ? "server_GET" : "client_merge")}");
                }
                catch { /* ignore */ }
                DispatchCrossGameEventsFromProgressResponse(response.Result, gs);
                if (mode == QuestProgressCacheRefreshMode.FullServerRefresh)
                    RequestQuestCacheRefreshInBackground(forceRefetch: true);
                else if (!mergedOptimistically)
                {
                    var mergedOk = MergeQuestProgressIntoLocalCache(qid.Value, gs, monstersKilledDelta, xpEarnedDelta, keysCollectedDelta, armorDelta, healthDelta, weaponsDelta, powerupsDelta, ammoDelta, genericItemPickup);
                    if (mergedOk)
                        OGEngineExports.StarApiLogFileOnly("[Quest] Progress merge applied after POST (native quests-cache-refreshed callback suppressed).");
                    else
                        RequestQuestCacheRefreshInBackground(forceRefetch: true);
                }
            }
            else if (mergedOptimistically)
            {
                try { OGEngineExports.StarApiLogFileOnly("[Quest] Progress POST failed; HUD used optimistic local merge — fix /api/quests/{id}/progress or ONODE routing if server should persist."); } catch { /* ignore */ }
            }
        }
        catch (Exception ex)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quest] ApplyQuestProgress: {ex.Message}"); } catch { /* ignore */ }
        }
    }

    /// <summary>Parse CrossGameEventsToDispatch and InventoryItemsToGrant from a progress/start API response body and route them into the engine's pending event queues.</summary>
    private void DispatchCrossGameEventsFromProgressResponse(string? responseBody, string requestGameSource)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            /* Unwrap OASISResult envelope: Result → QuestProgressApplyResult */
            if (root.TryGetProperty("Result", out var resultEl) || root.TryGetProperty("result", out resultEl))
                root = resultEl;
            /* CrossGameEventsToDispatch */
            if ((root.TryGetProperty("CrossGameEventsToDispatch", out var evtsEl) || root.TryGetProperty("crossGameEventsToDispatch", out evtsEl))
                && evtsEl.ValueKind == JsonValueKind.Array)
            {
                var clientGame = _config?.ClientGameSource ?? requestGameSource;
                foreach (var evt in evtsEl.EnumerateArray())
                {
                    if (evt.ValueKind != JsonValueKind.Object) continue;
                    var eventType = (evt.TryGetProperty("EventType", out var et) || evt.TryGetProperty("eventType", out et)) ? (et.GetString() ?? string.Empty) : string.Empty;
                    var targetGame = (evt.TryGetProperty("TargetGame", out var tg) || evt.TryGetProperty("targetGame", out tg)) ? (tg.GetString() ?? string.Empty) : string.Empty;
                    /* Only dispatch events that target the current game. Cross-game targeting other games requires server-side storage (future). */
                    var isForThisGame = string.IsNullOrEmpty(targetGame) || string.IsNullOrEmpty(clientGame)
                        || string.Equals(targetGame, clientGame, StringComparison.OrdinalIgnoreCase);
                    if (!isForThisGame)
                    {
                        try { OGEngineExports.StarApiLogFileOnly($"[CrossGameEvent] Skipping event type={eventType} targetGame={targetGame} (current game={clientGame}); cross-game routing requires server-side storage."); } catch { /* ignore */ }
                        continue;
                    }
                    try { OGEngineExports.StarApiLogFileOnly($"[CrossGameEvent] Dispatching type={eventType} targetGame={targetGame}"); } catch { /* ignore */ }
                    /* SpawnEntity: route through the existing spawn temp-file mechanism */
                    if (string.Equals(eventType, "SpawnEntity", StringComparison.OrdinalIgnoreCase))
                    {
                        var classname = (evt.TryGetProperty("EntityClassname", out var ec) || evt.TryGetProperty("entityClassname", out ec)) ? (ec.GetString() ?? string.Empty) : string.Empty;
                        var count = (evt.TryGetProperty("SpawnCount", out var sc) || evt.TryGetProperty("spawnCount", out sc)) ? (sc.TryGetInt32(out var sci) ? sci : 1) : 1;
                        var category = (evt.TryGetProperty("EntityCategory", out var ecat) || evt.TryGetProperty("entityCategory", out ecat)) ? (ecat.GetString() ?? "Monster") : "Monster";
                        if (!string.IsNullOrEmpty(classname))
                            for (var i = 0; i < Math.Max(1, count); i++)
                                WriteSpawnEventToFile(classname, category, 0f, 0f, 0f);
                    }
                    /* TeleportTo: use existing RequestTeleport which writes oasis_teleport_{avatarId}.json */
                    else if (string.Equals(eventType, "TeleportTo", StringComparison.OrdinalIgnoreCase))
                    {
                        var destGame = (evt.TryGetProperty("TargetGame", out var dg) || evt.TryGetProperty("targetGame", out dg)) ? (dg.GetString() ?? string.Empty) : string.Empty;
                        var destMap = (evt.TryGetProperty("TargetMap", out var dm) || evt.TryGetProperty("targetMap", out dm)) ? (dm.GetString() ?? string.Empty) : string.Empty;
                        if (!string.IsNullOrEmpty(destGame))
                            RequestTeleport(destGame, destMap, 0f, 0f, 0f);
                    }
                    /* All other event types: enqueue as JSON for ogengine_poll_cross_game_event */
                    else
                    {
                        OGEngineExports.EnqueueCrossGameEvent(evt.GetRawText());
                    }
                }
            }
            /* InventoryItemsToGrant */
            if ((root.TryGetProperty("InventoryItemsToGrant", out var itemsEl) || root.TryGetProperty("inventoryItemsToGrant", out itemsEl))
                && itemsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsEl.EnumerateArray())
                {
                    var id = item.ValueKind == JsonValueKind.String ? (item.GetString() ?? string.Empty) : item.GetRawText().Trim('"');
                    if (!string.IsNullOrEmpty(id))
                        OGEngineExports.EnqueueInventoryGrant(id);
                }
            }
        }
        catch (Exception ex)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[CrossGameEvent] DispatchFromProgressResponse error: {ex.Message}"); } catch { /* ignore */ }
        }
    }

    /// <summary>Write a cross-game spawn event to the per-avatar temp file (oasis_spawn_{avatarId}.json). Games poll ogengine_poll_spawn_event to consume it.</summary>
    private void WriteSpawnEventToFile(string entityClassname, string entityCategory, float x, float y, float z)
    {
        try
        {
            var avatarId = GetCachedAvatarId() ?? "unknown";
            var path = Path.Combine(Path.GetTempPath(), $"oasis_spawn_{avatarId}.json");
            var json = $"{{\"entityId\":{JsonSerializer.Serialize(entityClassname)},\"entityCategory\":{JsonSerializer.Serialize(entityCategory)},\"x\":{x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"z\":{z.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
            File.WriteAllText(path, json);
            OGEngineExports.StarApiLogFileOnly($"[Spawn] WriteSpawnEventToFile: classname={entityClassname} category={entityCategory}");
        }
        catch (Exception ex)
        {
            OGEngineExports.StarApiLogFileOnly($"[Spawn] WriteSpawnEventToFile error: {ex.Message}");
        }
    }

    /// <summary>
    /// Queue realtime quest progress (non-blocking) for the avatar's cached active quest id (from profile after beam-in / start quest).
    /// Objectives are updated server-side by the API; when <see cref="QuestProgressCacheRefreshMode.ClientCacheMerge"/> is active, the client also mirrors those deltas into cached dictionaries for instant UI (see ApplyQuestProgressToActiveQuestAsync).
    /// Called for native <c>queue_add_item</c>, <c>queue_pickup_with_mint</c>, <c>queue_quest_progress_from_pickup</c>, and after successful <c>use_item</c>.
    /// </summary>
    public void EnqueueQuestProgressFromGame(string gameSource, int monstersKilledDelta, int xpEarnedDelta, string? itemCollectedName, int keysCollectedDelta, int genericItemPickup, int? levelTimeSeconds = null, string? itemType = null, string? monsterKilledClassname = null)
    {
        if (!IsInitialized())
        {
            try { OGEngineExports.StarApiLogFileOnly("[Quest] EnqueueQuestProgressFromGame SKIP: client not initialized"); } catch { /* ignore */ }
            return;
        }
        int armor = 0, health = 0, weapons = 0, powerups = 0, ammo = 0;
        if (!string.IsNullOrWhiteSpace(itemType))
        {
            var it = itemType.Trim();
            if (it.IndexOf("Armor", StringComparison.OrdinalIgnoreCase) >= 0) armor = 1;
            else if (it.IndexOf("Health", StringComparison.OrdinalIgnoreCase) >= 0) health = 1;
            else if (it.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0) weapons = 1;
            else if (it.IndexOf("Powerup", StringComparison.OrdinalIgnoreCase) >= 0 || it.IndexOf("Artifact", StringComparison.OrdinalIgnoreCase) >= 0) powerups = 1;
            else if (it.IndexOf("Ammo", StringComparison.OrdinalIgnoreCase) >= 0) ammo = 1;
        }
        /* itemType may be generic "Item" or "Powerup"; infer health/armor/weapons from display name (matches ODOOM ToStarItemName output). */
        if (!string.IsNullOrWhiteSpace(itemCollectedName))
        {
            var n = itemCollectedName;
            if (armor == 0 && health == 0)
            {
                if (n.IndexOf("Mega Sphere", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Megasphere", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    armor = 1;
                    health = 1;
                }
                else if (n.IndexOf("Armor", StringComparison.OrdinalIgnoreCase) >= 0) armor = 1;
                else if (n.IndexOf("Stimpack", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Medikit", StringComparison.OrdinalIgnoreCase) >= 0
                         || n.IndexOf("Health", StringComparison.OrdinalIgnoreCase) >= 0) health = 1;
            }
            /* Do not match "ShotgunGuy" / "ChaingunGuy" (monster inventory names). */
            if (weapons == 0 && n.IndexOf("Guy", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (n.IndexOf("Shotgun", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Chaingun", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Pistol", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("BFG", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Plasma", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Rocket Launcher", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("RocketLauncher", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Chainsaw", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Fist", StringComparison.OrdinalIgnoreCase) >= 0)
                    weapons = 1;
            }
        }
        if (genericItemPickup != 0 && armor == 0 && health == 0 && weapons == 0 && powerups == 0 && ammo == 0 && monstersKilledDelta == 0 && xpEarnedDelta == 0 && keysCollectedDelta == 0)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quest] EnqueueQuestProgressFromGame: genericItem=1 but typed deltas zero — check itemType/itemName. type={itemType ?? ""} name={itemCollectedName ?? ""}"); } catch { /* ignore */ }
        }
        _ = RunOnWorkerAsync(DedicatedWorker.Quests, async ct =>
        {
            await ApplyQuestProgressToActiveQuestAsync(gameSource, monstersKilledDelta, xpEarnedDelta, itemCollectedName, keysCollectedDelta, armor, health, weapons, powerups, ammo, genericItemPickup, levelTimeSeconds, monsterKilledClassname, ct).ConfigureAwait(false);
            return Success(true, StarApiResultCode.Success, "");
        }, CancellationToken.None);
    }

    /// <summary>Run complete-quest on the <see cref="DedicatedWorker.Quests"/> queue (same as start-quest / progress).</summary>
    public Task<OASISResult<bool>> QueueCompleteQuestAsync(string questId, CancellationToken cancellationToken = default) =>
        RunOnWorkerAsync(DedicatedWorker.Quests, ct => CompleteQuestAsync(questId, ct), cancellationToken);

    public async Task<OASISResult<StarQuestInfo?>> CreateCrossGameQuestAsync(string questName, string description, List<StarQuestObjective> objectives, string? questLinkedGeoHotSpotId = null, string? questExternalHandoffUri = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarQuestInfo?>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questName) || string.IsNullOrWhiteSpace(description) || objectives is null || objectives.Count == 0)
            return FailAndCallback<StarQuestInfo?>("Quest name, description and at least one objective are required.", StarApiResultCode.InvalidParam);
        foreach (var o in objectives)
        {
            if (string.IsNullOrWhiteSpace(o.Title) || string.IsNullOrWhiteSpace(o.Description))
                return FailAndCallback<StarQuestInfo?>("Each objective requires Title and Description.", StarApiResultCode.InvalidParam);
            if (!ObjectiveHasAuthoringRequirements(o))
                return FailAndCallback<StarQuestInfo?>("Each objective requires at least one Need* dictionary definition, a valid LinkedGeoHotSpotId, or ExternalHandoffUri.", StarApiResultCode.InvalidParam);
        }

        var avatarIdResult = await EnsureAvatarIdAsync(cancellationToken).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
            return FailAndCallback<StarQuestInfo?>(avatarIdResult.Message ?? "Could not resolve avatar ID.", ParseCode(avatarIdResult.ErrorCode, StarApiResultCode.ApiError), avatarIdResult.Exception);

        var games = objectives
            .Select(o => string.IsNullOrWhiteSpace(o.GameSource) ? "Unknown" : o.GameSource)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Name", questName);
            writer.WriteString("Description", description);
            writer.WriteNumber("HolonSubType", 8); /* HolonType.Quest */
            writer.WriteString("SourceFolderPath", string.Empty);
            if (!string.IsNullOrWhiteSpace(questLinkedGeoHotSpotId) && Guid.TryParse(questLinkedGeoHotSpotId.Trim(), out var questGh))
                writer.WriteString("LinkedGeoHotSpotId", questGh.ToString("D"));
            if (!string.IsNullOrWhiteSpace(questExternalHandoffUri))
                writer.WriteString("ExternalHandoffUri", questExternalHandoffUri.Trim());
            writer.WritePropertyName("CreateOptions");
            writer.WriteNullValue();
            writer.WritePropertyName("MetaData");
            writer.WriteStartObject();
            writer.WriteBoolean("CrossGameQuest", true);
            writer.WriteString("QuestType", "CrossGame");
            writer.WritePropertyName("Games");
            writer.WriteStartArray();
            foreach (var game in games)
                writer.WriteStringValue(game);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WritePropertyName("Objectives");
            writer.WriteStartArray();
            for (var i = 0; i < objectives.Count; i++)
            {
                var o = objectives[i];
                writer.WriteStartObject();
                writer.WriteString("Title", o.Title ?? string.Empty);
                writer.WriteString("Description", o.Description ?? string.Empty);
                writer.WriteString("GameSource", o.GameSource ?? string.Empty);
                writer.WriteNumber("Order", o.Order >= 0 ? o.Order : i);
                writer.WriteBoolean("IsCompleted", o.IsCompleted);
                if (o.CompletedAt.HasValue) writer.WriteString("CompletedAt", o.CompletedAt.Value.ToString("O"));
                if (!string.IsNullOrEmpty(o.CompletedBy)) writer.WriteString("CompletedBy", o.CompletedBy);
                if (!string.IsNullOrWhiteSpace(o.LinkedGeoHotSpotId) && Guid.TryParse(o.LinkedGeoHotSpotId.Trim(), out var objGh))
                    writer.WriteString("LinkedGeoHotSpotId", objGh.ToString("D"));
                if (!string.IsNullOrWhiteSpace(o.ExternalHandoffUri))
                    writer.WriteString("ExternalHandoffUri", o.ExternalHandoffUri.Trim());
                if (o.Dictionaries != null)
                {
                    writer.WritePropertyName("Dictionaries");
                    writer.WriteStartObject();
                    WriteObjectiveDictionaries(writer, o.Dictionaries);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/create", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarQuestInfo?>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        LogQuestParseChunkedFileOnly("[Quest][Parse] source=POST.api.quests/create full HTTP body", response.Result);
        StarQuestInfo? created = null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (parseResult && resultElement.ValueKind == JsonValueKind.Object)
        {
            LogQuestJsonShapeFileOnly("[Quest][Parse] source=POST.api.quests/create envelope", resultElement);
            created = ParseSingleQuestInfo(resultElement);
            LogParsedSingleQuestModelAudit("POST.api.quests/create", created);
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(created, StarApiResultCode.Success, "Cross-game quest created successfully.");
    }

    private static bool HasAtLeastOneNeedDefinition(StarQuestObjectiveDictionaries? dictionaries)
    {
        if (dictionaries == null) return false;
        return dictionaries.NeedToCollectArmor?.Count > 0 ||
               dictionaries.NeedToCollectAmmo?.Count > 0 ||
               dictionaries.NeedToCollectHealth?.Count > 0 ||
               dictionaries.NeedToCollectWeapons?.Count > 0 ||
               dictionaries.NeedToCollectPowerups?.Count > 0 ||
               dictionaries.NeedToCollectItems?.Count > 0 ||
               dictionaries.NeedToCollectKeys?.Count > 0 ||
               dictionaries.NeedToKillMonsters?.Count > 0 ||
               dictionaries.NeedToCompleteInMins?.Count > 0 ||
               dictionaries.NeedToEarnKarma?.Count > 0 ||
               dictionaries.NeedToEarnXP?.Count > 0 ||
               dictionaries.NeedToGoToGeoHotSpots?.Count > 0 ||
               dictionaries.NeedToCompleteLevel?.Count > 0 ||
               dictionaries.NeedToUseWeapons?.Count > 0 ||
               dictionaries.NeedToUsePowerups?.Count > 0 ||
               dictionaries.NeedToVisitLocations?.Count > 0 ||
               dictionaries.NeedToSurviveMins?.Count > 0;
    }

    /// <summary>True when an objective is valid for create/add: at least one Need* row, a parseable linked GeoHotSpot id, or a non-empty handoff URI (matches STAR WebAPI rules).</summary>
    private static bool ObjectiveHasAuthoringRequirements(StarQuestObjective? o)
    {
        if (o == null) return false;
        if (HasAtLeastOneNeedDefinition(o.Dictionaries)) return true;
        if (!string.IsNullOrWhiteSpace(o.LinkedGeoHotSpotId) && Guid.TryParse(o.LinkedGeoHotSpotId.Trim(), out _)) return true;
        if (!string.IsNullOrWhiteSpace(o.ExternalHandoffUri)) return true;
        return false;
    }

    /// <summary>Run create-cross-game-quest on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarQuestInfo?>> QueueCreateCrossGameQuestAsync(string questName, string description, List<StarQuestObjective> objectives, string? questLinkedGeoHotSpotId = null, string? questExternalHandoffUri = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => CreateCrossGameQuestAsync(questName, description, objectives, questLinkedGeoHotSpotId, questExternalHandoffUri, ct), cancellationToken);

    /// <summary>Adds an objective to an existing quest (Title, Description, explicit Dictionaries with at least one Need*).</summary>
    public async Task<OASISResult<StarQuestInfo?>> AddQuestObjectiveAsync(string questId, string title, string description, string? gameSource = null, int order = -1, StarQuestObjectiveDictionaries? dictionaries = null, string? linkedGeoHotSpotId = null, string? externalHandoffUri = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarQuestInfo?>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId))
            return FailAndCallback<StarQuestInfo?>("Quest ID is required.", StarApiResultCode.InvalidParam);

        if (string.IsNullOrWhiteSpace(title))
            return FailAndCallback<StarQuestInfo?>("Objective title is required.", StarApiResultCode.InvalidParam);

        if (string.IsNullOrWhiteSpace(description))
            return FailAndCallback<StarQuestInfo?>("Description is required.", StarApiResultCode.InvalidParam);

        var probe = new StarQuestObjective { Dictionaries = dictionaries, LinkedGeoHotSpotId = linkedGeoHotSpotId, ExternalHandoffUri = externalHandoffUri };
        if (!ObjectiveHasAuthoringRequirements(probe))
            return FailAndCallback<StarQuestInfo?>("At least one Need* dictionary definition, a valid LinkedGeoHotSpotId, or ExternalHandoffUri is required.", StarApiResultCode.InvalidParam);

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Title", title);
            writer.WriteString("Description", description);
            writer.WriteString("GameSource", gameSource ?? string.Empty);
            writer.WriteNumber("Order", order);
            if (!string.IsNullOrWhiteSpace(linkedGeoHotSpotId) && Guid.TryParse(linkedGeoHotSpotId.Trim(), out var gh))
                writer.WriteString("LinkedGeoHotSpotId", gh.ToString("D"));
            if (!string.IsNullOrWhiteSpace(externalHandoffUri))
                writer.WriteString("ExternalHandoffUri", externalHandoffUri.Trim());
            if (dictionaries != null)
            {
                writer.WritePropertyName("Dictionaries");
                writer.WriteStartObject();
                WriteObjectiveDictionaries(writer, dictionaries);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/{questId}/objectives", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarQuestInfo?>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var objSrc = $"POST.api.quests/{questId.Trim()}/objectives";
        LogQuestParseChunkedFileOnly($"[Quest][Parse] source={objSrc} full HTTP body", response.Result);
        StarQuestInfo? created = null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (parseResult && resultElement.ValueKind == JsonValueKind.Object)
        {
            LogQuestJsonShapeFileOnly($"[Quest][Parse] source={objSrc} envelope", resultElement);
            created = ParseSingleQuestInfo(resultElement);
            LogParsedSingleQuestModelAudit(objSrc, created);
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(created, StarApiResultCode.Success, "Quest objective added successfully.");
    }

    /// <summary>Run add-quest-objective on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarQuestInfo?>> QueueAddQuestObjectiveAsync(string questId, string title, string description, string? gameSource = null, int order = -1, StarQuestObjectiveDictionaries? dictionaries = null, string? linkedGeoHotSpotId = null, string? externalHandoffUri = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => AddQuestObjectiveAsync(questId, title, description, gameSource, order, dictionaries, linkedGeoHotSpotId, externalHandoffUri, ct), cancellationToken);

    /// <summary>Loads a GeoHotSpot by id from STAR WebAPI (<c>GET /api/GeoHotSpots/{id}</c>). Deserializes <c>audioData</c>/<c>videoData</c> when the API returns base64-encoded bytes.</summary>
    public async Task<OASISResult<StarGeoHotSpotDetails?>> GetGeoHotSpotAsync(string geoHotSpotId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarGeoHotSpotDetails?>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(geoHotSpotId) || !Guid.TryParse(geoHotSpotId.Trim(), out var ghId))
            return FailAndCallback<StarGeoHotSpotDetails?>("A valid GeoHotSpot id (GUID) is required.", StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/GeoHotSpots/{ghId:D}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarGeoHotSpotDetails?>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<StarGeoHotSpotDetails?>(parseErrorMessage, parseErrorCode);

        if (resultElement.ValueKind != JsonValueKind.Object)
            return FailAndCallback<StarGeoHotSpotDetails?>("GeoHotSpot response was not a JSON object.", StarApiResultCode.ApiError);

        StarGeoHotSpotDetails? details;
        try
        {
            var serializerOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, MaxDepth = 1024 };
            details = JsonSerializer.Deserialize<StarGeoHotSpotDetails>(resultElement.GetRawText(), serializerOpts);
        }
        catch (Exception ex)
        {
            return FailAndCallback<StarGeoHotSpotDetails?>($"Could not parse GeoHotSpot JSON: {ex.Message}", StarApiResultCode.ApiError, ex);
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(details, StarApiResultCode.Success, "GeoHotSpot loaded.");
    }

    /// <summary>Run <see cref="GetGeoHotSpotAsync"/> on the background worker.</summary>
    public Task<OASISResult<StarGeoHotSpotDetails?>> QueueGetGeoHotSpotAsync(string geoHotSpotId, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => GetGeoHotSpotAsync(geoHotSpotId, ct), cancellationToken);

    /// <summary>Removes an objective from a quest.</summary>
    public async Task<OASISResult<bool>> RemoveQuestObjectiveAsync(string questId, string objectiveId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId))
            return FailAndCallback<bool>("Quest ID and objective ID are required.", StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Delete, $"{_baseApiUrl}/api/quests/{questId}/objectives/{objectiveId}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Quest objective removed successfully.");
    }

    /// <summary>Run remove-quest-objective on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueRemoveQuestObjectiveAsync(string questId, string objectiveId, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => RemoveQuestObjectiveAsync(questId, objectiveId, ct), cancellationToken);

    /// <summary>Adds a sub-quest (full child quest) to an existing quest. Use for nested quests; use AddQuestObjectiveAsync for checklist objectives (Quest.Objectives).</summary>
    public async Task<OASISResult<StarQuestInfo?>> AddSubQuestAsync(string questId, string description, string? name = null, string? gameSource = null, int order = -1, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarQuestInfo?>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId))
            return FailAndCallback<StarQuestInfo?>("Quest ID is required.", StarApiResultCode.InvalidParam);

        if (string.IsNullOrWhiteSpace(description))
            return FailAndCallback<StarQuestInfo?>("Description is required.", StarApiResultCode.InvalidParam);

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Name", name ?? string.Empty);
            writer.WriteString("Description", description);
            writer.WriteString("GameSource", gameSource ?? string.Empty);
            writer.WriteNumber("Order", order);
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/{questId}/subquests", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarQuestInfo?>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var subSrc = $"POST.api.quests/{questId.Trim()}/subquests";
        LogQuestParseChunkedFileOnly($"[Quest][Parse] source={subSrc} full HTTP body", response.Result);
        StarQuestInfo? created = null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (parseResult && resultElement.ValueKind == JsonValueKind.Object)
        {
            LogQuestJsonShapeFileOnly($"[Quest][Parse] source={subSrc} envelope", resultElement);
            created = ParseSingleQuestInfo(resultElement);
            LogParsedSingleQuestModelAudit(subSrc, created);
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(created, StarApiResultCode.Success, "Sub-quest added successfully.");
    }

    /// <summary>Run add-sub-quest on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarQuestInfo?>> QueueAddSubQuestAsync(string questId, string description, string? name = null, string? gameSource = null, int order = -1, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => AddSubQuestAsync(questId, description, name, gameSource, order, ct), cancellationToken);

    /// <summary>Removes a sub-quest (child quest) from a quest.</summary>
    public async Task<OASISResult<bool>> RemoveSubQuestAsync(string parentQuestId, string subQuestId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(parentQuestId) || string.IsNullOrWhiteSpace(subQuestId))
            return FailAndCallback<bool>("Parent quest ID and sub-quest ID are required.", StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Delete, $"{_baseApiUrl}/api/quests/{parentQuestId}/subquests/{subQuestId}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Sub-quest removed successfully.");
    }

    /// <summary>Run remove-sub-quest on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueRemoveSubQuestAsync(string parentQuestId, string subQuestId, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => RemoveSubQuestAsync(parentQuestId, subQuestId, ct), cancellationToken);

    /// <summary>Sets prerequisite quest IDs on a quest (MetaData.PrerequisiteQuestIds). Loads the quest via GET, merges metaData, then PUTs. Use for seed data so the UI can show prerequisite chains.</summary>
    public async Task<OASISResult<bool>> SetQuestPrerequisitesAsync(string questId, IReadOnlyList<string> prerequisiteQuestIds, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (string.IsNullOrWhiteSpace(questId))
            return FailAndCallback<bool>("Quest ID is required.", StarApiResultCode.InvalidParam);

        var getResponse = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/quests/{questId}", null, cancellationToken).ConfigureAwait(false);
        if (getResponse.IsError)
            return FailAndCallback<bool>(getResponse.Message ?? "GET quest failed", ParseCode(getResponse.ErrorCode, StarApiResultCode.ApiError), getResponse.Exception);

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(getResponse.Result ?? "{}");
        }
        catch (Exception ex)
        {
            return FailAndCallback<bool>($"Failed to parse quest response: {ex.Message}", StarApiResultCode.ApiError, ex);
        }

        var quest = root?["result"] ?? root?["Result"];
        if (quest is not JsonObject questObj)
            return FailAndCallback<bool>("Quest response did not contain a result object.", StarApiResultCode.ApiError);

        var metaData = questObj["metaData"] ?? questObj["MetaData"];
        if (metaData is not JsonObject metaObj)
        {
            metaObj = new JsonObject();
            questObj["metaData"] = metaObj;
        }
        var arr = new JsonArray(prerequisiteQuestIds.Select(s => (JsonNode?)s).ToArray());
        metaObj["PrerequisiteQuestIds"] = arr;

        var putBody = questObj.ToJsonString();
        var putResponse = await SendRawAsync(HttpMethod.Put, $"{_baseApiUrl}/api/quests/{questId}", putBody, cancellationToken).ConfigureAwait(false);
        if (putResponse.IsError)
            return FailAndCallback<bool>(putResponse.Message ?? "PUT quest failed", ParseCode(putResponse.ErrorCode, StarApiResultCode.ApiError), putResponse.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Quest prerequisites set.");
    }

    /// <summary>
    /// Gets all quests for the current avatar (no status filter).
    /// Use this for the quest popup and filter by status (Not Started, In Progress, Completed) in the client with checkboxes.
    /// </summary>
    public async Task<OASISResult<List<StarQuestInfo>>> GetAllQuestsForAvatarAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarQuestInfo>>("Client is not initialized.", StarApiResultCode.NotInitialized);

        var avatarIdResult = await EnsureAvatarIdAsync(cancellationToken).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
            return FailAndCallback<List<StarQuestInfo>>(avatarIdResult.Message ?? "Could not resolve avatar ID.", ParseCode(avatarIdResult.ErrorCode, StarApiResultCode.ApiError), avatarIdResult.Exception);

        var url = $"{_baseApiUrl}/api/quests/all-for-avatar/game";
        if (string.IsNullOrEmpty(_baseApiUrl))
            return FailAndCallback<List<StarQuestInfo>>("STAR API base URL not set.", StarApiResultCode.NotInitialized);

        OGEngineExports.StarApiLogFileOnly($"[Quests] GET all-for-avatar/game (AvatarId={GetCachedAvatarId() ?? "(none)"}) (BaseApiUrl)");

        var response = await SendRawWithRetryAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            OGEngineExports.StarApiLogFileOnly("[Quests] GET all-for-avatar/game failed (error).");
            OGEngineExports.StarApiLogFileOnly($"[Quests] GET all-for-avatar/game failed: {response.Message ?? "Request failed"}");
            return FailAndCallback<List<StarQuestInfo>>(response.Message ?? "Request failed", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            OGEngineExports.StarApiLogFileOnly($"[Quests] GET all-for-avatar/game parse failed: {parseErrorMessage ?? "Parse error"}");
            return FailAndCallback<List<StarQuestInfo>>(parseErrorMessage ?? "Parse error", parseErrorCode);
        }

        LogQuestJsonShapeFileOnly("[Quest][Parse] source=all-for-avatar/game envelope before unwrap", resultElement);
        LogQuestParseChunkedFileOnly("[Quest][Parse] source=all-for-avatar/game full HTTP body (exact API/DB payload)", response.Result);
        var quests = ParseQuestInfos(resultElement, "all-for-avatar/game") ?? new List<StarQuestInfo>();
        LogParsedQuestListModelAudit("all-for-avatar/game", quests);
        int totalObjectives = quests.Sum(q => q.Objectives?.Count ?? 0);
        OGEngineExports.StarApiLogFileOnly($"[Quests] GET all-for-avatar/game success: {quests.Count} quests, {totalObjectives} objectives");
        var idSummary = quests.Count > 0 ? string.Join(", ", quests.Take(12).Select(q => q.Id ?? "(null)")) + (quests.Count > 12 ? "..." : "") : "(none)";
        OGEngineExports.StarApiLogFileOnly($"[Quests] all-for-avatar/game Response IsError=False Message=(ok) Parsed: Count={quests.Count} totalObjectives={totalObjectives} Ids={idSummary}");
        OGEngineExports.StarApiLogFileOnly($"[Quests] all-for-avatar/game parsed: {quests.Count} quests, {totalObjectives} objectives");
        // Update in-memory cache so GetQuestObjectivesFromCache / TryGetQuestObjectivesCache (and game detail panel) see this data without waiting for background refresh.
        UpdateQuestsCache(quests);
        InvokeCallback(StarApiResultCode.Success);
        return Success(quests, StarApiResultCode.Success, $"Loaded {quests.Count} quest(s) for avatar.");
    }

    /// <summary>Write a quest list into the in-memory cache so native/game cache readers (get_quests_string, get_quest_objectives_string, etc.) see it. Used after GetAllQuestsForAvatarAsync and by the background refresh.</summary>
    private void UpdateQuestsCache(List<StarQuestInfo> list)
    {
        if (list == null) return;
        lock (_questsCacheLock)
        {
            _questsCacheString = list.Count == 0 ? string.Empty : SerializeQuestsForGame(list);
            _cachedQuestList = list;
            _questsFilterLastLogTop = (0, 0);
            _questsFilterLastLogObjectives = ("", -1);
            _questsFilterLastLogSubQuests = ("", -1);
            _questsFilterLastLogPrereqs = ("", -1);
        }
    }

    /// <summary>Update a single quest's status in the cached list and re-serialize so the UI sees the change immediately without a full refetch. Call after start-quest API success.</summary>
    private void UpdateQuestStatusInCache(string questId, string newStatus)
    {
        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(newStatus)) return;
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return;
            for (var i = 0; i < _cachedQuestList.Count; i++)
            {
                var q = _cachedQuestList[i];
                if (!string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase)) continue;
                var updated = new StarQuestInfo
                {
                    Id = q.Id,
                    Name = q.Name,
                    Description = q.Description,
                    Status = newStatus,
                    Order = q.Order,
                    GameSource = q.GameSource ?? string.Empty,
                    Requirements = q.Requirements ?? new List<string>(),
                    RewardKarma = q.RewardKarma,
                    RewardXP = q.RewardXP,
                    CompletionNotes = q.CompletionNotes,
                    ParentMissionId = q.ParentMissionId ?? string.Empty,
                    ParentQuestId = q.ParentQuestId ?? string.Empty,
                    Objectives = q.Objectives ?? new List<StarQuestObjective>(),
                    PrerequisiteQuestIds = q.PrerequisiteQuestIds ?? new List<string>(),
                    LinkedGeoHotSpotId = q.LinkedGeoHotSpotId,
                    ExternalHandoffUri = q.ExternalHandoffUri,
                    Dictionaries = q.Dictionaries
                };
                _cachedQuestList[i] = updated;
                _questsCacheString = _cachedQuestList.Count == 0 ? string.Empty : SerializeQuestsForGame(_cachedQuestList);
                _questsFilterLastLogTop = (0, 0);
                _questsFilterLastLogObjectives = ("", -1);
                _questsFilterLastLogSubQuests = ("", -1);
                _questsFilterLastLogPrereqs = ("", -1);
                OGEngineExports.StarApiLog($"[Quests] Updated cached quest {questId} status to {newStatus}; UI will refresh from cache.");
                return;
            }
            OGEngineExports.StarApiLogFileOnly($"[Quests] UpdateQuestStatusInCache: quest id not in local cache ({questId}); status not patched in-memory (server may still have updated).");
        }
    }

    //TODO: Use Enum for status, try to use enums instead of strings generally.
    public async Task<OASISResult<List<StarQuestInfo>>> GetQuestsByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarQuestInfo>>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (string.IsNullOrWhiteSpace(status))
            return FailAndCallback<List<StarQuestInfo>>("Quest status is required (e.g. InProgress, NotStarted, Completed).", StarApiResultCode.InvalidParam);

        var avatarIdResult = await EnsureAvatarIdAsync(cancellationToken).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
            return FailAndCallback<List<StarQuestInfo>>(avatarIdResult.Message ?? "Could not resolve avatar ID.", ParseCode(avatarIdResult.ErrorCode, StarApiResultCode.ApiError), avatarIdResult.Exception);

        var url = $"{_baseApiUrl}/api/quests/by-status/{Uri.EscapeDataString(status.Trim())}/game";
        if (string.IsNullOrEmpty(_baseApiUrl))
            return FailAndCallback<List<StarQuestInfo>>("STAR API base URL not set.", StarApiResultCode.NotInitialized);

        var avatarIdForLog = GetCachedAvatarId() ?? "(none)";
        OGEngineExports.StarApiLog($"[Quests] Client AvatarId={avatarIdForLog} (compare with seed output and API log)");
        OGEngineExports.StarApiLog($"[Quests] GET {url}");

        var response = await SendRawAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);

        OGEngineExports.StarApiLog($"[Quests] Response IsError={response.IsError} Message={response.Message ?? "(ok)"}");
        if (response.IsError)
            OGEngineExports.StarApiLog($"[Quests] Error: {response.Message ?? "Request failed"}");
        else
            OGEngineExports.StarApiLog("[Quests] OK");

        if (response.IsError)
            return FailAndCallback<List<StarQuestInfo>>(response.Message ?? "Request failed", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<List<StarQuestInfo>>(parseErrorMessage ?? "Parse error", parseErrorCode);

        var statusTag = $"by-status/{Uri.EscapeDataString(status.Trim())}/game";
        LogQuestJsonShapeFileOnly($"[Quest][Parse] source={statusTag} envelope before unwrap", resultElement);
        LogQuestParseChunkedFileOnly($"[Quest][Parse] source={statusTag} full HTTP body (exact API/DB payload)", response.Result);
        var quests = ParseQuestInfos(resultElement, statusTag) ?? new List<StarQuestInfo>();
        LogParsedQuestListModelAudit(statusTag, quests);
        var idSummary = quests.Count > 0 ? string.Join(", ", quests.Take(12).Select(q => q.Id ?? "(null)")) + (quests.Count > 12 ? "..." : "") : "(none)";
        OGEngineExports.StarApiLogFileOnly($"[Quests] by-status parsed: Count={quests.Count} Ids={idSummary}");
        if (quests.Count > 0)
            OGEngineExports.StarApiLog($"[Quests] OK ({quests.Count} quests) Ids={string.Join(", ", quests.Select(q => q.Id ?? "(null)"))}");
        else
            OGEngineExports.StarApiLog("[Quests] OK (0 quests)");
        InvokeCallback(StarApiResultCode.Success);
        return Success(quests, StarApiResultCode.Success, $"Loaded {quests.Count} quest(s) (status={status}).");
    }

    public Task<OASISResult<List<StarQuestInfo>>> GetActiveQuestsAsync(CancellationToken cancellationToken = default) =>
        GetQuestsByStatusAsync("InProgress", cancellationToken);

    /// <summary>Run get-active-quests on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<List<StarQuestInfo>>> QueueGetActiveQuestsAsync(CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => GetActiveQuestsAsync(ct), cancellationToken);

    /// <summary>Serialize quests to a string for game UI: each quest block is "Q\tid\tname\tdesc\tstatus\tpct\n" then "O\tid\tdesc\tdone\n" per objective (sub-quests), then "P\tid1\tid2\n" (prereqs), then "---\n". Tabs/newlines in text are replaced with space. pct = completed objectives / total * 100.</summary>
    public static string SerializeQuestsForGame(List<StarQuestInfo>? quests)
    {
        if (quests is null || quests.Count == 0)
            return string.Empty;
        var sb = new StringBuilder();
        foreach (var q in quests)
        {
            var name = EscapeForQuestLine(q.Name);
            var desc = EscapeForQuestLine(q.Description);
            var status = QuestStatusToGameString(q.Status);
            var objCount = q.Objectives?.Count ?? 0;
            var completed = q.Objectives?.Count(o => o.IsCompleted) ?? 0;
            var pct = objCount > 0 ? (completed * 100 / objCount) : 0;
            /* Never show Completed in the list unless every embedded objective is completed (API MetaData/Status can be wrong after partial progress). */
            if (objCount > 0 && q.Objectives != null)
            {
                var allObjDone = q.Objectives.All(o => o.IsCompleted);
                if (allObjDone)
                    status = "Completed";
                else if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                    status = "InProgress";
            }

            sb.Append("Q\t").Append(q.Id).Append("\t").Append(name).Append("\t").Append(desc).Append("\t").Append(status).Append("\t").Append(pct).Append("\n");
            if (q.Objectives != null)
            {
                for (var i = 0; i < q.Objectives.Count; i++)
                {
                    var o = q.Objectives[i];
                    var oid = string.IsNullOrEmpty(o.Id) ? $"obj_{i}" : o.Id;
                    sb.Append("O\t").Append(oid).Append("\t").Append(FormatObjectiveLineForGameList(o, q)).Append("\t").Append(o.IsCompleted ? "1" : "0").Append("\n");
                }
            }
            if (q.PrerequisiteQuestIds != null && q.PrerequisiteQuestIds.Count > 0)
                sb.Append("P\t").AppendJoin("\t", q.PrerequisiteQuestIds).Append("\n");
            sb.Append("\n---\n");
        }
        return sb.ToString();
    }

    /// <summary>Map API status (enum number "0"/"1"/"2" or name) to game string: NotStarted, InProgress, Completed.</summary>
    private static string QuestStatusToGameString(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "InProgress";
        var t = s.Trim();
        if (t == "0" || string.Equals(t, "NotStarted", StringComparison.OrdinalIgnoreCase)) return "NotStarted";
        if (t == "1" || string.Equals(t, "InProgress", StringComparison.OrdinalIgnoreCase)) return "InProgress";
        if (t == "2" || string.Equals(t, "Completed", StringComparison.OrdinalIgnoreCase)) return "Completed";
        return NormalizeQuestStatus(t);
    }

    /// <summary>Normalize status for game parsing: "Not Started" -> "NotStarted", "In Progress" -> "InProgress", "Completed" unchanged.</summary>
    private static string NormalizeQuestStatus(string s)
    {
        if (string.IsNullOrEmpty(s)) return "InProgress";
        return EscapeForQuestLine(s).Replace(" ", "");
    }

}
