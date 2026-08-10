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
    private static StarQuestObjectiveDictionaries? ParseObjectiveDictionariesBody(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        var names = new[] { "NeedToCollectArmor", "NeedToCollectAmmo", "NeedToCollectHealth", "NeedToCollectWeapons", "NeedToCollectPowerups", "NeedToCollectItems", "NeedToCollectKeys", "NeedToKillMonsters", "NeedToKillMonstersByType", "NeedToCompleteInMins", "NeedToEarnKarma", "NeedToEarnXP", "NeedToGoToGeoHotSpots", "NeedToCompleteLevel", "NeedToUseWeapons", "NeedToUsePowerups", "NeedToVisitLocations", "NeedToSurviveMins", "ArmorCollected", "AmmoCollected", "HealthCollected", "WeaponsCollected", "PowerupsCollected", "ItemsCollected", "KeysCollected", "MonstersKilled", "MonstersKilledByType", "TimeStarted", "TimeEnded", "TimeTaken", "KarmaEarnt", "XPEarnt", "GeoHotSpotsArrived", "LevelsCompleted" };
        var dicts = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var camel = char.ToLowerInvariant(name[0]) + name[1..];
            if (TryGetProperty(element, name, out var el) || TryGetProperty(element, camel, out el))
            {
                var d = ParseStringListDictionary(el);
                if (d.Count > 0) dicts[name] = d;
            }
        }
        if (dicts.Count == 0) return null;
        return new StarQuestObjectiveDictionaries
        {
            NeedToCollectArmor = dicts.GetValueOrDefault("NeedToCollectArmor") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToCollectAmmo = dicts.GetValueOrDefault("NeedToCollectAmmo") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToCollectHealth = dicts.GetValueOrDefault("NeedToCollectHealth") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToCollectWeapons = dicts.GetValueOrDefault("NeedToCollectWeapons") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToCollectPowerups = dicts.GetValueOrDefault("NeedToCollectPowerups") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToCollectItems = dicts.GetValueOrDefault("NeedToCollectItems") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToCollectKeys = dicts.GetValueOrDefault("NeedToCollectKeys") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToKillMonsters = dicts.GetValueOrDefault("NeedToKillMonsters") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToKillMonstersByType = dicts.GetValueOrDefault("NeedToKillMonstersByType") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToCompleteInMins = dicts.GetValueOrDefault("NeedToCompleteInMins") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToEarnKarma = dicts.GetValueOrDefault("NeedToEarnKarma") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToEarnXP = dicts.GetValueOrDefault("NeedToEarnXP") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToGoToGeoHotSpots = dicts.GetValueOrDefault("NeedToGoToGeoHotSpots") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToCompleteLevel = dicts.GetValueOrDefault("NeedToCompleteLevel") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToUseWeapons = dicts.GetValueOrDefault("NeedToUseWeapons") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToUsePowerups = dicts.GetValueOrDefault("NeedToUsePowerups") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToVisitLocations = dicts.GetValueOrDefault("NeedToVisitLocations") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            NeedToSurviveMins = dicts.GetValueOrDefault("NeedToSurviveMins") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            ArmorCollected = dicts.GetValueOrDefault("ArmorCollected") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            AmmoCollected = dicts.GetValueOrDefault("AmmoCollected") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            HealthCollected = dicts.GetValueOrDefault("HealthCollected") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            WeaponsCollected = dicts.GetValueOrDefault("WeaponsCollected") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            PowerupsCollected = dicts.GetValueOrDefault("PowerupsCollected") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            ItemsCollected = dicts.GetValueOrDefault("ItemsCollected") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            KeysCollected = dicts.GetValueOrDefault("KeysCollected") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            MonstersKilled = dicts.GetValueOrDefault("MonstersKilled") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            MonstersKilledByType = dicts.GetValueOrDefault("MonstersKilledByType") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            TimeStarted = dicts.GetValueOrDefault("TimeStarted") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            TimeEnded = dicts.GetValueOrDefault("TimeEnded") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            TimeTaken = dicts.GetValueOrDefault("TimeTaken") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            KarmaEarnt = dicts.GetValueOrDefault("KarmaEarnt") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            XPEarnt = dicts.GetValueOrDefault("XPEarnt") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            GeoHotSpotsArrived = dicts.GetValueOrDefault("GeoHotSpotsArrived") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
            LevelsCompleted = dicts.GetValueOrDefault("LevelsCompleted") ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static void WriteObjectiveDictionaries(Utf8JsonWriter writer, StarQuestObjectiveDictionaries dicts)
    {
        void WriteDict(string name, Dictionary<string, List<string>> d)
        {
            if (d == null || d.Count == 0) return;
            writer.WritePropertyName(name);
            writer.WriteStartObject();
            foreach (var kv in d)
            {
                writer.WritePropertyName(kv.Key);
                writer.WriteStartArray();
                foreach (var s in kv.Value ?? new List<string>())
                    writer.WriteStringValue(s);
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
        WriteDict("NeedToCollectArmor", dicts.NeedToCollectArmor);
        WriteDict("NeedToCollectAmmo", dicts.NeedToCollectAmmo);
        WriteDict("NeedToCollectHealth", dicts.NeedToCollectHealth);
        WriteDict("NeedToCollectWeapons", dicts.NeedToCollectWeapons);
        WriteDict("NeedToCollectPowerups", dicts.NeedToCollectPowerups);
        WriteDict("NeedToCollectItems", dicts.NeedToCollectItems);
        WriteDict("NeedToCollectKeys", dicts.NeedToCollectKeys);
        WriteDict("NeedToKillMonsters", dicts.NeedToKillMonsters);
        WriteDict("NeedToKillMonstersByType", dicts.NeedToKillMonstersByType);
        WriteDict("NeedToCompleteInMins", dicts.NeedToCompleteInMins);
        WriteDict("NeedToEarnKarma", dicts.NeedToEarnKarma);
        WriteDict("NeedToEarnXP", dicts.NeedToEarnXP);
        WriteDict("NeedToGoToGeoHotSpots", dicts.NeedToGoToGeoHotSpots);
        WriteDict("NeedToCompleteLevel", dicts.NeedToCompleteLevel);
        WriteDict("NeedToUseWeapons", dicts.NeedToUseWeapons);
        WriteDict("NeedToUsePowerups", dicts.NeedToUsePowerups);
        WriteDict("NeedToVisitLocations", dicts.NeedToVisitLocations);
        WriteDict("NeedToSurviveMins", dicts.NeedToSurviveMins);
        WriteDict("ArmorCollected", dicts.ArmorCollected);
        WriteDict("AmmoCollected", dicts.AmmoCollected);
        WriteDict("HealthCollected", dicts.HealthCollected);
        WriteDict("WeaponsCollected", dicts.WeaponsCollected);
        WriteDict("PowerupsCollected", dicts.PowerupsCollected);
        WriteDict("ItemsCollected", dicts.ItemsCollected);
        WriteDict("KeysCollected", dicts.KeysCollected);
        WriteDict("MonstersKilled", dicts.MonstersKilled);
        WriteDict("MonstersKilledByType", dicts.MonstersKilledByType);
        WriteDict("TimeStarted", dicts.TimeStarted);
        WriteDict("TimeEnded", dicts.TimeEnded);
        WriteDict("TimeTaken", dicts.TimeTaken);
        WriteDict("KarmaEarnt", dicts.KarmaEarnt);
        WriteDict("XPEarnt", dicts.XPEarnt);
        WriteDict("GeoHotSpotsArrived", dicts.GeoHotSpotsArrived);
        WriteDict("LevelsCompleted", dicts.LevelsCompleted);
    }

    /// <summary>Read objective <c>Title</c> and <c>Description</c> from JSON (Option B model).</summary>
    private static void ParseObjectiveStringsFromJsonObject(JsonElement objective, out string title, out string description)
    {
        title = (GetStringProperty(objective, "Title") ?? GetStringProperty(objective, "title") ?? string.Empty).Trim();
        description = (GetStringProperty(objective, "Description") ?? GetStringProperty(objective, "description") ?? string.Empty).Trim();
    }

    /// <summary>Parse objectives from a JsonElement that may be an array or a JSON string containing an array (e.g. from MetaData).</summary>
    private static List<StarQuestObjective> ParseObjectivesFromElement(JsonElement element)
    {
        var objectives = new List<StarQuestObjective>();
        if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var objective in element.EnumerateArray())
            {
                if (objective.ValueKind != JsonValueKind.Object) continue;
                var id = GetStringProperty(objective, "Id") ?? GetStringProperty(objective, "id") ?? string.Empty;
                try { LogQuestParseChunkedFileOnly($"[Quest][Parse][Raw] objectiveFromArray idx={index} id={id} json", objective.GetRawText()); } catch { /* ignore */ }
                ParseObjectiveStringsFromJsonObject(objective, out var title, out var desc);
                var gameSource = GetStringProperty(objective, "GameSource") ?? GetStringProperty(objective, "gameSource") ?? string.Empty;
                var order = GetIntProperty(objective, "Order") ?? GetIntProperty(objective, "order") ?? index;
                var isCompleted = GetBoolProperty(objective, "IsCompleted") || GetBoolProperty(objective, "isCompleted");
                var completedAt = GetDateTimeProperty(objective, "CompletedAt") ?? GetDateTimeProperty(objective, "completedAt");
                var completedBy = GetStringProperty(objective, "CompletedBy") ?? GetStringProperty(objective, "completedBy");
                var linkedGh = GetStringProperty(objective, "LinkedGeoHotSpotId") ?? GetStringProperty(objective, "linkedGeoHotSpotId");
                var handoff = GetStringProperty(objective, "ExternalHandoffUri") ?? GetStringProperty(objective, "externalHandoffUri");
                var dicts = ParseObjectiveDictionaries(objective);
                objectives.Add(new StarQuestObjective
                {
                    Id = id,
                    Title = title,
                    Description = desc ?? string.Empty,
                    GameSource = gameSource,
                    Order = order,
                    IsCompleted = isCompleted,
                    CompletedAt = completedAt,
                    CompletedBy = completedBy,
                    LinkedGeoHotSpotId = string.IsNullOrWhiteSpace(linkedGh) ? null : linkedGh.Trim(),
                    ExternalHandoffUri = string.IsNullOrWhiteSpace(handoff) ? null : handoff.Trim(),
                    Dictionaries = dicts
                });
                index++;
            }
            return objectives;
        }
        if (element.ValueKind == JsonValueKind.String)
        {
            var json = element.GetString();
            if (string.IsNullOrWhiteSpace(json)) return objectives;
            try { LogQuestParseChunkedFileOnly("[Quest][Parse][Raw] objectivesMetaDataString (JSON text inside string property)", json); } catch { /* ignore */ }
            try
            {
                using var doc = JsonDocument.Parse(json);
                return ParseObjectivesFromElement(doc.RootElement);
            }
            catch
            {
                /* ignore */
            }
        }
        return objectives;
    }

    private static bool GetBoolProperty(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.True)
            return true;

        if (prop.ValueKind == JsonValueKind.False)
            return false;

        var text = GetStringProperty(element, name);
        return bool.TryParse(text, out var value) && value;
    }

    private static int? GetIntProperty(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n))
            return n;
        var text = GetStringProperty(element, name);
        return int.TryParse(text, out var parsed) ? parsed : null;
    }

    private static DateTime? GetDateTimeProperty(JsonElement element, string name)
    {
        var text = GetStringProperty(element, name);
        if (string.IsNullOrWhiteSpace(text)) return null;
        return DateTime.TryParse(text, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : null;
    }

    private static long? GetLongProperty(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var n))
            return n;
        var text = GetStringProperty(element, name);
        return long.TryParse(text, out var parsed) ? parsed : null;
    }

    /// <summary>Try common WEB4/OASIS mint response property names for tx hash. Also checks Result.Web3NFTs[0].MintTransactionHash (WEB4 mint returns hash on the Web3NFT).</summary>
    private static string? GetMintResponseHash(JsonElement resultElement, string? rawResponseBody)
    {
        var hashKeys = new[] { "Hash", "TransactionHash", "Signature", "TxHash", "MintTransactionHash", "TransactionResult", "transactionHash", "mintTransactionHash", "transactionResult" };
        foreach (var key in hashKeys)
        {
            var v = GetStringProperty(resultElement, key);
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }
        var fromWeb3Nfts = GetHashFromWeb3NFTsCollection(resultElement);
        if (!string.IsNullOrWhiteSpace(fromWeb3Nfts))
            return fromWeb3Nfts;
        if (string.IsNullOrWhiteSpace(rawResponseBody))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(rawResponseBody);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return null;
            foreach (var key in hashKeys)
            {
                var v = GetStringProperty(root, key);
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }
            fromWeb3Nfts = GetHashFromWeb3NFTsCollection(root);
            if (!string.IsNullOrWhiteSpace(fromWeb3Nfts))
                return fromWeb3Nfts;
            if (TryGetProperty(root, "Result", out var resultProp))
                fromWeb3Nfts = GetHashFromWeb3NFTsCollection(resultProp);
            if (!string.IsNullOrWhiteSpace(fromWeb3Nfts))
                return fromWeb3Nfts;
        }
        catch
        {
            /* ignore parse errors */
        }
        return null;
    }

    /// <summary>Extract MintTransactionHash from first Web3NFT in Web3NFTs array (WEB4 mint response shape).</summary>
    private static string? GetHashFromWeb3NFTsCollection(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (!TryGetProperty(element, "Web3NFTs", out var web3NftsProp) && !TryGetProperty(element, "web3NFTs", out web3NftsProp))
            return null;
        if (web3NftsProp.ValueKind != JsonValueKind.Array)
            return null;
        var i = 0;
        foreach (var item in web3NftsProp.EnumerateArray())
        {
            if (i++ > 0) break;
            var hash = GetStringProperty(item, "MintTransactionHash")
                ?? GetStringProperty(item, "MintHash")
                ?? GetStringProperty(item, "mintTransactionHash")
                ?? GetStringProperty(item, "mintHash");
            if (!string.IsNullOrWhiteSpace(hash))
                return hash;
        }
        return null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return null;
    }

    private void StartWorkers()
    {
        StartAddItemWorker();
        StartUseItemWorker();
        StartQuestObjectiveWorker();
        // Generic and dedicated workers (AuthSession, Profile, Inventory, Quests) are started lazily when first used.
    }

    private void StopWorkers()
    {
        StopAddItemWorker();
        StopUseItemWorker();
        StopQuestObjectiveWorker();
        StopGenericBackgroundWorker();
        StopDedicatedWorkers();
    }

    private void StopDedicatedWorkers()
    {
        StopAuthSessionWorker();
        StopProfileWorker();
        StopInventoryWorker();
        StopQuestsWorker();
    }

    private void StartGenericBackgroundWorker()
    {
        lock (_genericBackgroundLock)
        {
            if (_genericBackgroundWorker is { IsCompleted: false })
                return;
            _genericBackgroundCts = new CancellationTokenSource();
            _genericBackgroundWorker = Task.Run(() => ProcessGenericBackgroundJobsAsync(_genericBackgroundCts.Token));
        }
    }

    private void StopGenericBackgroundWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_genericBackgroundLock)
        {
            cts = _genericBackgroundCts;
            worker = _genericBackgroundWorker;
            _genericBackgroundCts = null;
            _genericBackgroundWorker = null;
        }
        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _genericBackgroundSignal.Release();
                worker?.GetAwaiter().GetResult();
            }
            catch { }
            finally { cts.Dispose(); }
        }
        while (_genericBackgroundQueue.TryDequeue(out _)) { }
    }

    private void StartAuthSessionWorker()
    {
        lock (_authSessionLock)
        {
            if (_authSessionWorker is { IsCompleted: false }) return;
            _authSessionCts = new CancellationTokenSource();
            _authSessionWorker = Task.Run(() => ProcessDedicatedWorkerAsync(_authSessionQueue, _authSessionSignal, _authSessionCts.Token));
        }
    }

    private void StopAuthSessionWorker()
    {
        StopDedicatedWorker(_authSessionLock, ref _authSessionCts, ref _authSessionWorker, _authSessionSignal, _authSessionQueue);
    }

    private void StartProfileWorker()
    {
        lock (_profileLock)
        {
            if (_profileWorker is { IsCompleted: false }) return;
            _profileCts = new CancellationTokenSource();
            _profileWorker = Task.Run(() => ProcessDedicatedWorkerAsync(_profileQueue, _profileSignal, _profileCts.Token));
        }
    }

    private void StopProfileWorker()
    {
        StopDedicatedWorker(_profileLock, ref _profileCts, ref _profileWorker, _profileSignal, _profileQueue);
    }

    private void StartInventoryWorker()
    {
        lock (_inventoryLock)
        {
            if (_inventoryWorker is { IsCompleted: false }) return;
            _inventoryCts = new CancellationTokenSource();
            _inventoryWorker = Task.Run(() => ProcessDedicatedWorkerAsync(_inventoryQueue, _inventorySignal, _inventoryCts.Token));
        }
    }

    private void StopInventoryWorker()
    {
        StopDedicatedWorker(_inventoryLock, ref _inventoryCts, ref _inventoryWorker, _inventorySignal, _inventoryQueue);
    }

    private void StartQuestsWorker()
    {
        lock (_questsLock)
        {
            if (_questsWorker is { IsCompleted: false }) return;
            _questsCts = new CancellationTokenSource();
            _questsWorker = Task.Run(() => ProcessDedicatedWorkerAsync(_questsQueue, _questsSignal, _questsCts.Token));
        }
    }

    private void StopQuestsWorker()
    {
        /* Quest progress runs on this queue; StopDedicatedWorker used to cancel then discard remaining jobs,
         * so exit/reload could drop POST /quests/.../progress that had not run yet (or abort in-flight).
         * Give the worker time to drain, a short grace for the last HTTP, then run any leftovers synchronously. */
        const int spinMs = 10;
        var spinDeadline = DateTime.UtcNow.AddSeconds(12);
        var sawQueuedWork = !_questsQueue.IsEmpty;
        while (DateTime.UtcNow < spinDeadline && !_questsQueue.IsEmpty)
        {
            sawQueuedWork = true;
            try { _questsSignal.Release(1); }
            catch { /* Semaphore disposed or at capacity — stop spinning */ break; }
            Thread.Sleep(spinMs);
        }

        /* Brief grace so an in-flight progress POST can finish; longer if we were draining the queue. */
        Thread.Sleep(sawQueuedWork ? TimeSpan.FromSeconds(2) : TimeSpan.FromMilliseconds(500));

        CancellationTokenSource? c;
        Task? w;
        lock (_questsLock)
        {
            c = _questsCts;
            w = _questsWorker;
            _questsCts = null;
            _questsWorker = null;
        }

        if (c is not null)
        {
            try
            {
                c.Cancel();
                try { _questsSignal.Release(1); } catch { /* ignore */ }
                w?.GetAwaiter().GetResult();
            }
            catch { /* join */ }
            finally { c.Dispose(); }
        }

        var drainSw = Stopwatch.StartNew();
        while (drainSw.Elapsed < TimeSpan.FromSeconds(10) && _questsQueue.TryDequeue(out var job))
        {
            try
            {
                job(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                OGEngineExports.StarApiLogFileOnly($"[Quest] Shutdown drain job failed: {ex.Message}");
            }
        }

        if (!_questsQueue.IsEmpty)
        {
            var dropped = 0;
            while (_questsQueue.TryDequeue(out _)) dropped++;
            OGEngineExports.StarApiLogFileOnly($"[Quest] Shutdown: {dropped} queued job(s) not drained after budget — progress may not persist.");
        }
    }

    private static async Task ProcessDedicatedWorkerAsync(ConcurrentQueue<Func<CancellationToken, Task>> queue, SemaphoreSlim signal, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            while (queue.TryDequeue(out var job))
            {
                if (cancellationToken.IsCancellationRequested) break;
                try
                {
                    await job(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch { /* TCS already set */ }
            }
        }
    }

    private void StopDedicatedWorker(object lockObj, ref CancellationTokenSource? cts, ref Task? worker, SemaphoreSlim signal, ConcurrentQueue<Func<CancellationToken, Task>> queue)
    {
        CancellationTokenSource? c;
        Task? w;
        lock (lockObj)
        {
            c = cts; w = worker;
            cts = null; worker = null;
        }
        if (c is not null)
        {
            try
            {
                c.Cancel();
                signal.Release();
                w?.GetAwaiter().GetResult();
            }
            catch { }
            finally { c.Dispose(); }
        }
        while (queue.TryDequeue(out _)) { }
    }

    /// <summary>Run an operation on a dedicated worker (AuthSession, Profile, Inventory, Quests) so it doesn't block the generic worker or other domains.</summary>
    private Task<OASISResult<T>> RunOnWorkerAsync<T>(DedicatedWorker workerType, Func<CancellationToken, Task<OASISResult<T>>> operation, CancellationToken cancellationToken)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<T>("Client is not initialized.", StarApiResultCode.NotInitialized));
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<OASISResult<T>>(cancellationToken);

        var tcs = new TaskCompletionSource<OASISResult<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        var run = async (CancellationToken workerCt) =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(workerCt, cancellationToken);
            try
            {
                var result = await operation(linked.Token).ConfigureAwait(false);
                tcs.TrySetResult(result);
            }
            catch (OperationCanceledException) { tcs.TrySetCanceled(); }
            catch (Exception ex) { tcs.TrySetResult(Fail<T>(ex.Message, StarApiResultCode.Network, ex)); }
        };

        switch (workerType)
        {
            case DedicatedWorker.AuthSession:
                _authSessionQueue.Enqueue(run);
                _authSessionSignal.Release();
                StartAuthSessionWorker();
                break;
            case DedicatedWorker.Profile:
                _profileQueue.Enqueue(run);
                _profileSignal.Release();
                StartProfileWorker();
                break;
            case DedicatedWorker.Inventory:
                _inventoryQueue.Enqueue(run);
                _inventorySignal.Release();
                StartInventoryWorker();
                break;
            case DedicatedWorker.Quests:
                _questsQueue.Enqueue(run);
                _questsSignal.Release();
                StartQuestsWorker();
                break;
        }
        return tcs.Task;
    }

    private async Task ProcessGenericBackgroundJobsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _genericBackgroundSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            while (_genericBackgroundQueue.TryDequeue(out var job))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                try
                {
                    await job(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    /* Job already set result/exception on its TCS; continue to next job. */
                }
            }
        }
    }

    /// <summary>Run an async operation on the generic background worker so the caller's thread (e.g. UI/game) never blocks. Returns a Task that completes when the operation finishes.</summary>
    private Task<OASISResult<T>> RunOnBackgroundAsync<T>(Func<CancellationToken, Task<OASISResult<T>>> operation, CancellationToken cancellationToken)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<T>("Client is not initialized.", StarApiResultCode.NotInitialized));
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<OASISResult<T>>(cancellationToken);

        var tcs = new TaskCompletionSource<OASISResult<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        }

        var run = async (CancellationToken workerCt) =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(workerCt, cancellationToken);
            try
            {
                var result = await operation(linked.Token).ConfigureAwait(false);
                tcs.TrySetResult(result);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(Fail<T>(ex.Message, StarApiResultCode.Network, ex));
            }
        };

        _genericBackgroundQueue.Enqueue(run);
        _genericBackgroundSignal.Release();
        StartGenericBackgroundWorker();
        return tcs.Task;
    }

    private void StartAddItemWorker()
    {
        lock (_jobLock)
        {
            if (_jobWorker is { IsCompleted: false })
                return;

            _jobCts = new CancellationTokenSource();
            _jobWorker = Task.Run(() => ProcessAddItemJobsAsync(_jobCts.Token));
        }
    }

    private void StopAddItemWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_jobLock)
        {
            cts = _jobCts;
            worker = _jobWorker;
            _jobCts = null;
            _jobWorker = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _addItemSignal.Release();
                if (worker is not null)
                    worker.GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        while (_pendingAddItemJobs.TryDequeue(out var pending))
            pending.Completion?.TrySetResult(Fail<StarItem>("Add-item queue stopped.", StarApiResultCode.NotInitialized));
    }

    private void StartUseItemWorker()
    {
        lock (_jobLock)
        {
            if (_useItemJobWorker is not null && !_useItemJobWorker.IsCompleted)
                return;

            _useItemJobCts = new CancellationTokenSource();
            _useItemJobWorker = Task.Run(() => ProcessUseItemJobsAsync(_useItemJobCts.Token));
        }
    }

    private void StopUseItemWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_jobLock)
        {
            cts = _useItemJobCts;
            worker = _useItemJobWorker;
            _useItemJobCts = null;
            _useItemJobWorker = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _useItemSignal.Release();
                if (worker is not null)
                    worker.GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        while (_pendingUseItemJobs.TryDequeue(out var pending))
            pending.Completion?.TrySetResult(Fail<bool>("Use-item queue stopped.", StarApiResultCode.NotInitialized));
    }

    private void StartQuestObjectiveWorker()
    {
        lock (_jobLock)
        {
            if (_questObjectiveJobWorker is not null && !_questObjectiveJobWorker.IsCompleted)
                return;

            _questObjectiveJobCts = new CancellationTokenSource();
            _questObjectiveJobWorker = Task.Run(() => ProcessQuestObjectiveJobsAsync(_questObjectiveJobCts.Token));
        }
    }

    private void StopQuestObjectiveWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_jobLock)
        {
            cts = _questObjectiveJobCts;
            worker = _questObjectiveJobWorker;
            _questObjectiveJobCts = null;
            _questObjectiveJobWorker = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _questObjectiveSignal.Release();
                if (worker is not null)
                    worker.GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        while (_pendingQuestObjectiveJobs.TryDequeue(out var pending))
            pending.Completion.TrySetResult(Fail<bool>("Quest objective queue stopped.", StarApiResultCode.NotInitialized));
    }

    /// <summary>Background worker: flush local pending to API (one add_item per type), then invalidate cache. Games only call EnqueueAddItemJobOnly or EnqueuePickupWithMintJobOnly; this does the heavy lifting.</summary>
    private async Task ProcessAddItemJobsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _addItemSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            /* Flush pending XP (queued by ogengine_queue_add_xp or monster kill jobs). */
            var pendingXp = Interlocked.Exchange(ref _pendingXp, 0);
            if (pendingXp > 0)
            {
                var addXpResult = await AddXpAsync(pendingXp, cancellationToken).ConfigureAwait(false);
                if (addXpResult.IsError)
                    OGEngineExports.SetLastBackgroundError($"STAR: Add XP failed: {addXpResult.Message}");
            }

            /* Process monster kill jobs: add XP and optionally mint + add item. Flush XP immediately after so it shows up as soon as you kill. */
            while (_pendingMonsterKill.TryDequeue(out var monsterJob))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                OGEngineExports.StarApiLog($"Monster kill processing: {monsterJob.DisplayName} {monsterJob.Xp} XP doMint={monsterJob.DoMint}");
                Interlocked.Add(ref _pendingXp, monsterJob.Xp);
                if (!monsterJob.DoMint)
                    continue;
                var gameSource = string.IsNullOrWhiteSpace(monsterJob.GameSource) ? "ODOOM" : monsterJob.GameSource;
                var desc = $"Monster defeated in {gameSource}: {monsterJob.DisplayName}";
                OGEngineExports.StarApiLog($"Monster kill: minting NFT for {monsterJob.DisplayName}");
                var mintResult = await CreateMonsterNftAsync(monsterJob.EngineName, desc, gameSource, "{}", monsterJob.Provider, cancellationToken).ConfigureAwait(false);
                if (mintResult.IsError || string.IsNullOrWhiteSpace(mintResult.Result.NftId))
                {
                    OGEngineExports.StarApiLog($"Monster kill: NFT mint failed for '{monsterJob.DisplayName}': {mintResult.Message}");
                    OGEngineExports.SetLastBackgroundError($"STAR: Monster NFT mint failed for '{monsterJob.DisplayName}': {mintResult.Message}");
                    continue;
                }
                OGEngineExports.StarApiLog($"Monster kill: NFT minted for {monsterJob.DisplayName}, adding to inventory");
                /* Store item name with game source so OQUAKE and ODOOM kills are separate (e.g. "Dog (OQUAKE)" vs "Dog (ODOOM)"). Add [BOSS] for boss monsters only. */
                var baseName = monsterJob.IsBoss ? "[BOSS] " + monsterJob.DisplayName : monsterJob.DisplayName;
                var itemName = $"{baseName} ({gameSource})";
                Interlocked.Increment(ref _activeAddItemJobs);
                try
                {
                    var addResult = await AddItemCoreAsync(itemName, desc, gameSource, "Monster", mintResult.Result.NftId, 1, true, cancellationToken).ConfigureAwait(false);
                    if (addResult.IsError)
                        OGEngineExports.SetLastBackgroundError($"STAR: Add monster item failed for '{itemName}': {addResult.Message}");
                    else
                    {
                        lock (_lastMintLock)
                        {
                            _lastMintItemName = itemName;
                            _lastMintNftId = mintResult.Result.NftId;
                            _lastMintHash = mintResult.Result.Hash;
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _activeAddItemJobs);
                }
            }

            /* Flush XP from monster kills (and any other pending) so HUD updates as soon as you kill, not on next worker wake. */
            var monsterXp = Interlocked.Exchange(ref _pendingXp, 0);
            if (monsterXp > 0)
            {
                OGEngineExports.StarApiLog($"Monster kill: sending AddXpAsync({monsterXp}) to API");
                var addXpResult = await AddXpAsync(monsterXp, cancellationToken).ConfigureAwait(false);
                if (addXpResult.IsError)
                {
                    OGEngineExports.StarApiLog($"Monster kill: Add XP failed: {addXpResult.Message}");
                    OGEngineExports.SetLastBackgroundError($"STAR: Add XP failed: {addXpResult.Message}");
                }
                else
                    OGEngineExports.StarApiLog($"Monster kill: Add XP succeeded, new total={addXpResult.Result}");
            }

            // Process pickup-with-mint jobs first (mint then add_item; all in C# background).
            while (_pendingPickupWithMint.TryDequeue(out var pickupJob))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                string? nftId = null;
                if (pickupJob.DoMint)
                {
                    var mintResult = await MintInventoryItemNftAsync(
                        pickupJob.ItemName,
                        pickupJob.Description,
                        pickupJob.GameSource,
                        pickupJob.ItemType,
                        pickupJob.Provider,
                        pickupJob.SendToAddressAfterMinting,
                        cancellationToken).ConfigureAwait(false);
                    if (!mintResult.IsError && mintResult.Result.NftId is { } id)
                    {
                        nftId = id;
                        var hash = mintResult.Result.Hash;
                        lock (_lastMintLock)
                        {
                            _lastMintItemName = pickupJob.ItemName;
                            _lastMintNftId = id;
                            _lastMintHash = string.IsNullOrWhiteSpace(hash) ? null : hash;
                        }
                        /* So overlay shows [NFT] before add completes: set NftId on pending entry. */
                        lock (_localPendingLock)
                        {
                            if (_localPending.TryGetValue(pickupJob.ItemName, out var pending))
                                pending.NftId = id;
                        }
                    }
                    else if (mintResult.IsError)
                    {
                        OGEngineExports.StarApiLog($"Mint failed for '{pickupJob.ItemName}': {mintResult.Message}");
                        OGEngineExports.SetLastBackgroundError($"STAR: Mint failed for '{pickupJob.ItemName}': {mintResult.Message}");
                    }
                }
                Interlocked.Increment(ref _activeAddItemJobs);
                try
                {
                    var addResult = await AddItemCoreAsync(pickupJob.ItemName, pickupJob.Description, pickupJob.GameSource, pickupJob.ItemType, nftId, pickupJob.Quantity, true, cancellationToken).ConfigureAwait(false);
                    if (addResult.IsError)
                        OGEngineExports.SetLastBackgroundError($"STAR: Add item failed for '{pickupJob.ItemName}': {addResult.Message}");
                    else
                        DeductLocalPending(pickupJob.ItemName, pickupJob.Quantity);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeAddItemJobs);
                }
            }

            /* Do not invalidate cache here: AddItemCoreAsync already updates _cachedInventory when add succeeds. Invalidating caused a refetch that could return stale data (keys vanished in overlay). */

            Dictionary<string, LocalPendingEntry> snapshot;
            lock (_localPendingLock)
            {
                if (_localPending.Count == 0)
                    continue;
                snapshot = new Dictionary<string, LocalPendingEntry>(_localPending, StringComparer.OrdinalIgnoreCase);
                _localPending.Clear();
            }

            /* Ensure FlushAddItemJobsAsync does not return until all items are processed (avoids race where HasItemAsync runs with cache not yet updated). */
            var snapshotCount = snapshot.Count;
            Interlocked.Add(ref _activeAddItemJobs, snapshotCount);

            if (AddItemBatchWindow > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(AddItemBatchWindow, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    lock (_localPendingLock)
                    {
                        foreach (var kv in snapshot)
                            _localPending[kv.Key] = kv.Value;
                    }
                    break;
                }
            }

            foreach (var kv in snapshot)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    lock (_localPendingLock)
                    {
                        if (_localPending.TryGetValue(kv.Key, out var existing))
                            existing.Quantity += kv.Value.Quantity;
                        else
                            _localPending[kv.Key] = kv.Value;
                    }
                    Interlocked.Decrement(ref _activeAddItemJobs);
                    continue;
                }
                var entry = kv.Value;
                try
                {
                    var addResult = await AddItemCoreAsync(entry.Name, entry.Description, entry.GameSource, entry.ItemType, null, entry.Quantity, true, cancellationToken).ConfigureAwait(false);
                    if (addResult.IsError)
                        OGEngineExports.SetLastBackgroundError($"STAR: Add item failed for '{entry.Name}': {addResult.Message}");
                }
                finally
                {
                    Interlocked.Decrement(ref _activeAddItemJobs);
                }
            }

            /* Do not invalidate cache: AddItemCoreAsync already updated _cachedInventory for each added item. */
        }
    }

    private async Task ProcessUseItemJobsAsync(CancellationToken cancellationToken)
    {
        var batch = new List<PendingUseItemJob>(Math.Max(1, UseItemBatchSize));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _useItemSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            batch.Clear();
            while (_pendingUseItemJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, UseItemBatchSize))
                batch.Add(pending);

            if (batch.Count == 0)
                continue;

            if (UseItemBatchWindow > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(UseItemBatchWindow, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                while (_pendingUseItemJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, UseItemBatchSize))
                    batch.Add(pending);
            }

            foreach (var job in batch)
            {
                if (job.CancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    job.Completion?.TrySetResult(Fail<bool>("Queued use-item job was cancelled.", StarApiResultCode.Network));
                    continue;
                }

                Interlocked.Increment(ref _activeUseItemJobs);
                try
                {
                    var result = await UseItemCoreAsync(job.ItemName, job.Context, job.Quantity, job.CancellationToken).ConfigureAwait(false);
                    job.Completion?.TrySetResult(result);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeUseItemJobs);
                }
            }
        }
    }

    private async Task ProcessQuestObjectiveJobsAsync(CancellationToken cancellationToken)
    {
        var batch = new List<PendingQuestObjectiveJob>(Math.Max(1, QuestObjectiveBatchSize));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _questObjectiveSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            batch.Clear();
            while (_pendingQuestObjectiveJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, QuestObjectiveBatchSize))
                batch.Add(pending);

            if (batch.Count == 0)
                continue;

            if (QuestObjectiveBatchWindow > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(QuestObjectiveBatchWindow, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                while (_pendingQuestObjectiveJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, QuestObjectiveBatchSize))
                    batch.Add(pending);
            }

            foreach (var job in batch)
            {
                if (job.CancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    job.Completion.TrySetResult(Fail<bool>("Queued quest objective job was cancelled.", StarApiResultCode.Network));
                    continue;
                }

                Interlocked.Increment(ref _activeQuestObjectiveJobs);
                try
                {
                    var result = await CompleteQuestObjectiveCoreAsync(job.QuestId, job.ObjectiveId, job.GameSource, job.CancellationToken).ConfigureAwait(false);
                    job.Completion.TrySetResult(result);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeQuestObjectiveJobs);
                }
            }
        }
    }

    /// <summary>Recursively search JSON tree for an object with id == avatarId that has activeQuestId/activeObjectiveId (handles double-wrapped or alternate API shapes).</summary>
    private static void FindQuestIdsInTree(JsonElement root, Guid avatarId, out Guid? activeQuestId, out Guid? activeObjectiveId)
    {
        activeQuestId = null;
        activeObjectiveId = null;
        SearchNode(root, avatarId, ref activeQuestId, ref activeObjectiveId);
    }

    private static void SearchNode(JsonElement node, Guid avatarId, ref Guid? activeQuestId, ref Guid? activeObjectiveId)
    {
        if (activeQuestId.HasValue && activeObjectiveId.HasValue) return;
        if (node.ValueKind == JsonValueKind.Object)
        {
            var idStr = GetStringProperty(node, "Id") ?? GetStringProperty(node, "id");
            if (Guid.TryParse(idStr, out var id) && id == avatarId)
            {
                var q = GetStringProperty(node, "ActiveQuestId") ?? GetStringProperty(node, "activeQuestId");
                if (!string.IsNullOrWhiteSpace(q) && Guid.TryParse(q, out var qGuid)) activeQuestId = qGuid;
                var o = GetStringProperty(node, "ActiveObjectiveId") ?? GetStringProperty(node, "activeObjectiveId");
                if (!string.IsNullOrWhiteSpace(o) && Guid.TryParse(o, out var oGuid)) activeObjectiveId = oGuid;
            }
            foreach (var prop in node.EnumerateObject())
                SearchNode(prop.Value, avatarId, ref activeQuestId, ref activeObjectiveId);
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
                SearchNode(item, avatarId, ref activeQuestId, ref activeObjectiveId);
        }
    }

    private static StarAvatarProfile? ParseAvatarProfile(JsonElement element, string? rawResponseJson = null)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        Guid.TryParse(GetStringProperty(element, "Id") ?? GetStringProperty(element, "id"), out var id);
        var xp = GetIntProperty(element, "XP") ?? GetIntProperty(element, "xp")
            ?? GetIntProperty(element, "TotalXP") ?? GetIntProperty(element, "totalXp");
        Guid? activeQuestId = null;
        Guid? activeObjectiveId = null;
        string? questSource = null;
        string? objectiveSource = null;
        if (TryGetProperty(element, "AvatarDetail", out var detailEl) || TryGetProperty(element, "avatarDetail", out detailEl))
        {
            if (xp is null) xp = GetIntProperty(detailEl, "XP") ?? GetIntProperty(detailEl, "xp");
            var q = GetStringProperty(detailEl, "ActiveQuestId") ?? GetStringProperty(detailEl, "activeQuestId");
            if (!string.IsNullOrWhiteSpace(q) && Guid.TryParse(q, out var qGuid)) { activeQuestId = qGuid; questSource = "AvatarDetail"; }
            var o = GetStringProperty(detailEl, "ActiveObjectiveId") ?? GetStringProperty(detailEl, "activeObjectiveId");
            if (!string.IsNullOrWhiteSpace(o) && Guid.TryParse(o, out var oGuid)) { activeObjectiveId = oGuid; objectiveSource = "AvatarDetail"; }
        }
        if (xp is null && TryGetProperty(element, "avatarDetail", out var detailEl2))
            xp = GetIntProperty(detailEl2, "XP") ?? GetIntProperty(detailEl2, "xp");
        if (activeQuestId is null)
        {
            var q = GetStringProperty(element, "ActiveQuestId") ?? GetStringProperty(element, "activeQuestId");
            if (!string.IsNullOrWhiteSpace(q) && Guid.TryParse(q, out var qGuid)) { activeQuestId = qGuid; questSource = "root"; }
        }
        if (activeObjectiveId is null)
        {
            var o = GetStringProperty(element, "ActiveObjectiveId") ?? GetStringProperty(element, "activeObjectiveId");
            if (!string.IsNullOrWhiteSpace(o) && Guid.TryParse(o, out var oGuid)) { activeObjectiveId = oGuid; objectiveSource = "root"; }
        }
        if ((!activeQuestId.HasValue || !activeObjectiveId.HasValue) && !string.IsNullOrEmpty(rawResponseJson) && id != Guid.Empty)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawResponseJson);
                FindQuestIdsInTree(doc.RootElement, id, out var treeQuest, out var treeObjective);
                if (treeQuest.HasValue && !activeQuestId.HasValue) { activeQuestId = treeQuest; questSource = "tree"; }
                if (treeObjective.HasValue && !activeObjectiveId.HasValue) { activeObjectiveId = treeObjective; objectiveSource = "tree"; }
            }
            catch { /* ignore parse for fallback */ }
        }
        try { OGEngineExports.StarApiLogFileOnly($"[Avatar] ParseAvatarProfile: ActiveQuestId={activeQuestId} (from {questSource ?? "none"}) ActiveObjectiveId={activeObjectiveId} (from {objectiveSource ?? "none"})"); } catch { /* ignore */ }
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] LOAD (parsed from API) questId={activeQuestId} objectiveId={activeObjectiveId}"); } catch { /* ignore */ }
        long? karma = GetLongProperty(element, "Karma") ?? GetLongProperty(element, "karma")
            ?? GetLongProperty(element, "KarmaScore") ?? GetLongProperty(element, "karmaScore");
        if (karma is null && TryGetProperty(element, "AvatarDetail", out var karmaDetailEl))
            karma = GetLongProperty(karmaDetailEl, "Karma") ?? GetLongProperty(karmaDetailEl, "karma")
                 ?? GetLongProperty(karmaDetailEl, "KarmaScore") ?? GetLongProperty(karmaDetailEl, "karmaScore");

        return new StarAvatarProfile
        {
            Id = id,
            Username = GetStringProperty(element, "Username") ?? string.Empty,
            Email = GetStringProperty(element, "Email") ?? string.Empty,
            FirstName = GetStringProperty(element, "FirstName") ?? string.Empty,
            LastName = GetStringProperty(element, "LastName") ?? string.Empty,
            XP = xp ?? 0,
            Karma = karma ?? 0,
            ActiveQuestId = activeQuestId,
            ActiveObjectiveId = activeObjectiveId
        };
    }

    private static List<StarQuestInfo> ParseQuestInfos(JsonElement element, string parseSource)
    {
        element = UnwrapQuestListRoot(element);
        LogQuestJsonShapeFileOnly($"[Quest][Parse] source={parseSource} listRoot", element);

        var quests = new List<StarQuestInfo>();
        if (element.ValueKind != JsonValueKind.Array)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quest][Parse] source={parseSource} listRoot not an array (ValueKind={element.ValueKind}); returning 0 quests"); } catch { /* ignore */ }
            return quests;
        }

        var questRowIndex = 0;
        foreach (var questElement in element.EnumerateArray())
        {
            var rowIdx = questRowIndex++;
            if (questElement.ValueKind != JsonValueKind.Object)
                continue;

            try { LogQuestParseChunkedFileOnly($"[Quest][Parse] source={parseSource} rawQuestRow[{rowIdx}] json", questElement.GetRawText()); } catch { /* ignore */ }

            /* Only read from known objective property names (Objectives, objectives, QuestObjectives, questObjectives at root/MetaData/MapMetaData) so we never bind SubQuests or PrerequisiteQuestIds. */
            var objectives = GetObjectivesFromQuestElement(questElement);
            /* Fallback: API may use "Quests" array for embedded objectives when items look like objectives (Description, no Name). */
            if (objectives.Count == 0 && (TryGetProperty(questElement, "Quests", out var qArr) || TryGetProperty(questElement, "Quest", out qArr)) && qArr.ValueKind == JsonValueKind.Array)
            {
                var first = qArr.EnumerateArray().FirstOrDefault();
                var hasName = !string.IsNullOrEmpty(GetStringProperty(first, "Name") ?? GetStringProperty(first, "name"));
                if (first.ValueKind == JsonValueKind.Object && !hasName &&
                    (GetStringProperty(first, "Description") ?? GetStringProperty(first, "description") ?? GetStringProperty(first, "Objective") ?? GetStringProperty(first, "objective")) != null)
                {
                    var idx = 0;
                    foreach (var sub in qArr.EnumerateArray())
                    {
                        if (sub.ValueKind != JsonValueKind.Object) continue;
                        ParseObjectiveStringsFromJsonObject(sub, out var title, out var desc);
                        var qLg = GetStringProperty(sub, "LinkedGeoHotSpotId") ?? GetStringProperty(sub, "linkedGeoHotSpotId");
                        var qHo = GetStringProperty(sub, "ExternalHandoffUri") ?? GetStringProperty(sub, "externalHandoffUri");
                        objectives.Add(new StarQuestObjective
                        {
                            Id = GetStringProperty(sub, "Id") ?? GetStringProperty(sub, "id") ?? string.Empty,
                            Title = title,
                            Description = desc,
                            GameSource = GetStringProperty(sub, "GameSource") ?? GetStringProperty(sub, "gameSource") ?? string.Empty,
                            Order = GetIntProperty(sub, "Order") ?? idx,
                            IsCompleted = GetBoolProperty(sub, "IsCompleted") || GetBoolProperty(sub, "isCompleted"),
                            LinkedGeoHotSpotId = string.IsNullOrWhiteSpace(qLg) ? null : qLg.Trim(),
                            ExternalHandoffUri = string.IsNullOrWhiteSpace(qHo) ? null : qHo.Trim(),
                            Dictionaries = ParseObjectiveDictionaries(sub)
                        });
                        idx++;
                    }
                }
            }

            // PrerequisiteQuestIds may be top-level (API serializes Quest after MapMetaData) or under MetaData; support PascalCase and camelCase
            var prereqIds = GetStringListFromElement(questElement, "MetaData", "PrerequisiteQuestIds");
            if (prereqIds.Count == 0)
                prereqIds = GetStringListFromElement(questElement, "metaData", "prerequisiteQuestIds");
            if (prereqIds.Count == 0 && (TryGetProperty(questElement, "PrerequisiteQuestIds", out var prereqArr) || TryGetProperty(questElement, "prerequisiteQuestIds", out prereqArr)) && prereqArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prereqArr.EnumerateArray())
                {
                    var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText()?.Trim('"');
                    if (!string.IsNullOrEmpty(s))
                        prereqIds.Add(s);
                }
            }
            var parentQuestId = GetStringProperty(questElement, "ParentQuestId") ?? GetStringProperty(questElement, "parentQuestId");
            if (string.IsNullOrWhiteSpace(parentQuestId) && (TryGetProperty(questElement, "ParentQuestId", out var parentEl) || TryGetProperty(questElement, "parentQuestId", out parentEl)) && parentEl.ValueKind == JsonValueKind.String)
                parentQuestId = parentEl.GetString();
            if (string.IsNullOrWhiteSpace(parentQuestId) && (TryGetProperty(questElement, "MetaData", out var metaForParent) || TryGetProperty(questElement, "metaData", out metaForParent)) && metaForParent.ValueKind == JsonValueKind.Object)
                parentQuestId = GetStringProperty(metaForParent, "ParentQuestId") ?? GetStringProperty(metaForParent, "parentQuestId") ?? string.Empty;

            var parentId = GetStringProperty(questElement, "Id") ?? string.Empty;
            var order = GetIntProperty(questElement, "Order") ?? GetIntProperty(questElement, "order") ?? 0;
            var gameSource = GetStringProperty(questElement, "GameSource") ?? GetStringProperty(questElement, "gameSource") ?? string.Empty;
            var requirements = new List<string>();
            if (TryGetProperty(questElement, "Requirements", out var reqEl) || TryGetProperty(questElement, "requirements", out reqEl))
            { if (reqEl.ValueKind == JsonValueKind.Array) foreach (var item in reqEl.EnumerateArray()) { var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText()?.Trim('"'); if (!string.IsNullOrEmpty(s)) requirements.Add(s); } }
            var rewardKarma = GetLongProperty(questElement, "RewardKarma") ?? GetLongProperty(questElement, "rewardKarma") ?? 0L;
            var rewardXP = GetLongProperty(questElement, "RewardXP") ?? GetLongProperty(questElement, "rewardXP") ?? 0L;
            var completionNotes = GetStringProperty(questElement, "CompletionNotes") ?? GetStringProperty(questElement, "completionNotes");
            var parentMissionId = GetStringProperty(questElement, "ParentMissionId") ?? GetStringProperty(questElement, "parentMissionId") ?? string.Empty;
            quests.Add(new StarQuestInfo
            {
                Id = parentId,
                Name = GetStringProperty(questElement, "Name") ?? string.Empty,
                Description = GetStringProperty(questElement, "Description") ?? string.Empty,
                Status = GetStringProperty(questElement, "Status") ?? string.Empty,
                Order = order,
                GameSource = gameSource,
                Requirements = requirements,
                RewardKarma = rewardKarma,
                RewardXP = rewardXP,
                CompletionNotes = completionNotes,
                ParentMissionId = parentMissionId,
                ParentQuestId = (parentQuestId ?? string.Empty).Trim(),
                Objectives = objectives,
                PrerequisiteQuestIds = prereqIds,
                LinkedGeoHotSpotId = ReadLinkedGeoHotSpotIdFromQuestJson(questElement),
                ExternalHandoffUri = ReadExternalHandoffUriFromQuestJson(questElement),
                Dictionaries = ParseObjectiveDictionaries(questElement)
            });

            /* Flatten nested sub-quests: SubQuests or Quest/Quests array of full quest objects (have Id + Name) so right-panel subquest list is populated. */
            if (string.IsNullOrEmpty(parentId)) continue;
            IEnumerable<JsonElement>? childElements = null;
            if (TryGetProperty(questElement, "SubQuests", out var subQuestsEl) && subQuestsEl.ValueKind == JsonValueKind.Array)
                childElements = subQuestsEl.EnumerateArray();
            else if (TryGetProperty(questElement, "Quests", out var questsArr) && questsArr.ValueKind == JsonValueKind.Array)
            {
                var first = questsArr.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Object && !string.IsNullOrEmpty(GetStringProperty(first, "Name") ?? GetStringProperty(first, "name")))
                    childElements = questsArr.EnumerateArray();
            }
            else if (TryGetProperty(questElement, "Quest", out var singleQuest) && singleQuest.ValueKind == JsonValueKind.Object)
                childElements = new[] { singleQuest };

            if (childElements != null)
            {
                foreach (var childEl in childElements)
                {
                    if (childEl.ValueKind != JsonValueKind.Object) continue;
                    try { LogQuestParseChunkedFileOnly($"[Quest][Parse] source={parseSource} rawSubQuestRow parentId={parentId} json", childEl.GetRawText()); } catch { /* ignore */ }
                    var childId = GetStringProperty(childEl, "Id") ?? GetStringProperty(childEl, "id");
                    if (string.IsNullOrEmpty(childId)) continue;
                    var childObj = new List<StarQuestObjective>();
                    if (TryGetProperty(childEl, "Objectives", out var coEl) || TryGetProperty(childEl, "objectives", out coEl))
                        childObj = ParseObjectivesFromElement(coEl);
                    if (childObj.Count == 0 && (TryGetProperty(childEl, "MetaData", out var cMeta) || TryGetProperty(childEl, "metaData", out cMeta)) && cMeta.ValueKind == JsonValueKind.Object
                        && (TryGetProperty(cMeta, "Objectives", out var cMetaObj) || TryGetProperty(cMeta, "objectives", out cMetaObj)))
                        childObj = ParseObjectivesFromElement(cMetaObj);
                    var childPrereqIds = GetStringListFromElement(childEl, "MetaData", "PrerequisiteQuestIds");
                    if (childPrereqIds.Count == 0)
                        childPrereqIds = GetStringListFromElement(childEl, "metaData", "prerequisiteQuestIds");
                    var childOrder = GetIntProperty(childEl, "Order") ?? GetIntProperty(childEl, "order") ?? 0;
                    var childGameSource = GetStringProperty(childEl, "GameSource") ?? GetStringProperty(childEl, "gameSource") ?? string.Empty;
                    var childReqs = new List<string>();
                    if (TryGetProperty(childEl, "Requirements", out var creq) || TryGetProperty(childEl, "requirements", out creq))
                    { if (creq.ValueKind == JsonValueKind.Array) foreach (var item in creq.EnumerateArray()) { var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText()?.Trim('"'); if (!string.IsNullOrEmpty(s)) childReqs.Add(s); } }
                    var childRewardKarma = GetLongProperty(childEl, "RewardKarma") ?? 0L;
                    var childRewardXP = GetLongProperty(childEl, "RewardXP") ?? 0L;
                    var childNotes = GetStringProperty(childEl, "CompletionNotes") ?? GetStringProperty(childEl, "completionNotes");
                    var childMissionId = GetStringProperty(childEl, "ParentMissionId") ?? string.Empty;
                    quests.Add(new StarQuestInfo
                    {
                        Id = childId,
                        Name = GetStringProperty(childEl, "Name") ?? GetStringProperty(childEl, "name") ?? string.Empty,
                        Description = GetStringProperty(childEl, "Description") ?? GetStringProperty(childEl, "description") ?? string.Empty,
                        Status = GetStringProperty(childEl, "Status") ?? GetStringProperty(childEl, "status") ?? string.Empty,
                        Order = childOrder,
                        GameSource = childGameSource,
                        Requirements = childReqs,
                        RewardKarma = childRewardKarma,
                        RewardXP = childRewardXP,
                        CompletionNotes = childNotes,
                        ParentMissionId = childMissionId,
                        ParentQuestId = parentId,
                        Objectives = childObj,
                        PrerequisiteQuestIds = childPrereqIds,
                        LinkedGeoHotSpotId = ReadLinkedGeoHotSpotIdFromQuestJson(childEl),
                        ExternalHandoffUri = ReadExternalHandoffUriFromQuestJson(childEl),
                        Dictionaries = ParseObjectiveDictionaries(childEl)
                    });
                }
            }
        }

        return quests;
    }

    private static string? ReadLinkedGeoHotSpotIdFromQuestJson(JsonElement element)
    {
        var s = GetStringProperty(element, "LinkedGeoHotSpotId") ?? GetStringProperty(element, "linkedGeoHotSpotId");
        if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
        if ((TryGetProperty(element, "MetaData", out var meta) || TryGetProperty(element, "metaData", out meta)) && meta.ValueKind == JsonValueKind.Object)
        {
            s = GetStringProperty(meta, "LinkedGeoHotSpotId") ?? GetStringProperty(meta, "linkedGeoHotSpotId");
            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
        }
        return null;
    }

    private static string? ReadExternalHandoffUriFromQuestJson(JsonElement element)
    {
        var s = GetStringProperty(element, "ExternalHandoffUri") ?? GetStringProperty(element, "externalHandoffUri");
        if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
        if ((TryGetProperty(element, "MetaData", out var meta) || TryGetProperty(element, "metaData", out meta)) && meta.ValueKind == JsonValueKind.Object)
        {
            s = GetStringProperty(meta, "ExternalHandoffUri") ?? GetStringProperty(meta, "externalHandoffUri");
            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
        }
        return null;
    }

    private static StarQuestInfo? ParseSingleQuestInfo(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        /* Only read from known objective property names so we never bind SubQuests or PrerequisiteQuestIds. */
        var objectives = GetObjectivesFromQuestElement(element);
        /* Fallback: single-quest response may have "Quests" array of objective-like items. */
        if (objectives.Count == 0 && TryGetProperty(element, "Quests", out var questsElement) && questsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var sub in questsElement.EnumerateArray())
            {
                if (sub.ValueKind != JsonValueKind.Object) continue;
                ParseObjectiveStringsFromJsonObject(sub, out var title, out var desc);
                if (string.IsNullOrEmpty(desc)) continue; /* Skip items that look like full quests (no Description/Objective). */
                var subLg = GetStringProperty(sub, "LinkedGeoHotSpotId") ?? GetStringProperty(sub, "linkedGeoHotSpotId");
                var subHo = GetStringProperty(sub, "ExternalHandoffUri") ?? GetStringProperty(sub, "externalHandoffUri");
                objectives.Add(new StarQuestObjective
                {
                    Id = GetStringProperty(sub, "Id") ?? GetStringProperty(sub, "id") ?? string.Empty,
                    Title = title,
                    Description = desc,
                    GameSource = GetStringProperty(sub, "GameSource") ?? GetStringProperty(sub, "gameSource") ?? string.Empty,
                    Order = GetIntProperty(sub, "Order") ?? GetIntProperty(sub, "order") ?? 0,
                    IsCompleted = GetBoolProperty(sub, "IsCompleted") || GetBoolProperty(sub, "isCompleted"),
                    LinkedGeoHotSpotId = string.IsNullOrWhiteSpace(subLg) ? null : subLg.Trim(),
                    ExternalHandoffUri = string.IsNullOrWhiteSpace(subHo) ? null : subHo.Trim(),
                    Dictionaries = ParseObjectiveDictionaries(sub)
                });
            }
        }

        var parentQuestId = GetStringProperty(element, "ParentQuestId") ?? GetStringProperty(element, "parentQuestId");
        if (string.IsNullOrWhiteSpace(parentQuestId) && (TryGetProperty(element, "MetaData", out var metaForParent) || TryGetProperty(element, "metaData", out metaForParent)) && metaForParent.ValueKind == JsonValueKind.Object)
            parentQuestId = GetStringProperty(metaForParent, "ParentQuestId") ?? GetStringProperty(metaForParent, "parentQuestId");
        var prereqIds = GetStringListFromElement(element, "MetaData", "PrerequisiteQuestIds");
        if (prereqIds.Count == 0) prereqIds = GetStringListFromElement(element, "metaData", "prerequisiteQuestIds");
        if (prereqIds.Count == 0 && (TryGetProperty(element, "PrerequisiteQuestIds", out var prereqArr) || TryGetProperty(element, "prerequisiteQuestIds", out prereqArr)) && prereqArr.ValueKind == JsonValueKind.Array)
        { foreach (var item in prereqArr.EnumerateArray()) { var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText()?.Trim('"'); if (!string.IsNullOrEmpty(s)) prereqIds.Add(s); } }
        var requirements = new List<string>();
        if (TryGetProperty(element, "Requirements", out var reqEl) || TryGetProperty(element, "requirements", out reqEl))
        { if (reqEl.ValueKind == JsonValueKind.Array) foreach (var item in reqEl.EnumerateArray()) { var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText()?.Trim('"'); if (!string.IsNullOrEmpty(s)) requirements.Add(s); } }
        return new StarQuestInfo
        {
            Id = GetStringProperty(element, "Id") ?? string.Empty,
            Name = GetStringProperty(element, "Name") ?? string.Empty,
            Description = GetStringProperty(element, "Description") ?? string.Empty,
            Status = GetStringProperty(element, "Status") ?? string.Empty,
            Order = GetIntProperty(element, "Order") ?? GetIntProperty(element, "order") ?? 0,
            GameSource = GetStringProperty(element, "GameSource") ?? GetStringProperty(element, "gameSource") ?? string.Empty,
            Requirements = requirements,
            RewardKarma = GetLongProperty(element, "RewardKarma") ?? GetLongProperty(element, "rewardKarma") ?? 0L,
            RewardXP = GetLongProperty(element, "RewardXP") ?? GetLongProperty(element, "rewardXP") ?? 0L,
            CompletionNotes = GetStringProperty(element, "CompletionNotes") ?? GetStringProperty(element, "completionNotes"),
            ParentMissionId = GetStringProperty(element, "ParentMissionId") ?? GetStringProperty(element, "parentMissionId") ?? string.Empty,
            ParentQuestId = (parentQuestId ?? string.Empty).Trim(),
            Objectives = objectives,
            PrerequisiteQuestIds = prereqIds,
            LinkedGeoHotSpotId = ReadLinkedGeoHotSpotIdFromQuestJson(element),
            ExternalHandoffUri = ReadExternalHandoffUriFromQuestJson(element),
            Dictionaries = ParseObjectiveDictionaries(element)
        };
    }

    private static List<StarNftInfo> ParseNftInfos(JsonElement element)
    {
        var nfts = new List<StarNftInfo>();
        if (element.ValueKind != JsonValueKind.Array)
            return nfts;

        foreach (var nft in element.EnumerateArray())
        {
            if (nft.ValueKind != JsonValueKind.Object)
                continue;

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (TryGetProperty(nft, "MetaData", out var metadataElement) && metadataElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in metadataElement.EnumerateObject())
                    metadata[property.Name] = property.Value.ToString();
            }

            nfts.Add(new StarNftInfo
            {
                Id = GetStringProperty(nft, "Id") ?? string.Empty,
                Name = GetStringProperty(nft, "Name") ?? string.Empty,
                Description = GetStringProperty(nft, "Description") ?? string.Empty,
                Type = GetStringProperty(nft, "Type") ?? string.Empty,
                MetaData = metadata
            });
        }

        return nfts;
    }

    private bool IsInitialized()
    {
        lock (_stateLock)
            return _initialized;
    }

    private OASISResult<T> Success<T>(T value, StarApiResultCode code, string message)
    {
        return new OASISResult<T>
        {
            Result = value,
            IsError = false,
            Message = message,
            ErrorCode = ((int)code).ToString()
        };
    }

    private OASISResult<T> Fail<T>(string message, StarApiResultCode code, Exception? exception = null)
    {
        lock (_stateLock)
            _lastError = message;

        var result = new OASISResult<T>
        {
            IsError = true,
            Message = message,
            ErrorCode = ((int)code).ToString()
        };

        if (exception is not null)
            result.Exception = exception;

        return result;
    }

    private OASISResult<T> FailAndCallback<T>(string message, StarApiResultCode code, Exception? exception = null)
    {
        var result = Fail<T>(message, code, exception);
        InvokeCallback(code);
        return result;
    }

    private StarApiResultCode ParseCode(string? errorCode, StarApiResultCode fallback)
    {
        if (int.TryParse(errorCode, out var parsed) && Enum.IsDefined(typeof(StarApiResultCode), parsed))
            return (StarApiResultCode)parsed;

        return fallback;
    }

    private void InvokeCallback(StarApiResultCode code)
    {
        StarApiCallback? callback;
        object? userData;

        lock (_stateLock)
        {
            callback = _callback;
            userData = _callbackUserData;
        }

        callback?.Invoke(code, userData);
    }

}
