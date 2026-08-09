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

    private sealed class AvatarAuthResponse
    {
        public Guid Id { get; set; }
        public string? JwtToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    private sealed class AvatarInfo
    {
        public Guid Id { get; set; }
    }

    private sealed class InventoryItemResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, JsonElement>? MetaData { get; set; }
        public int Quantity { get; set; } = 1;
        /// <summary>From API / InventoryItem holon.</summary>
        public string? GameSource { get; set; }
        /// <summary>From API / InventoryItem holon.</summary>
        public string? ItemType { get; set; }
        /// <summary>NFT ID when item is linked to NFTHolon (from MetaData or root). Persists so [NFT] prefix shows in Quake/Doom after reload.</summary>
        public string? NftId { get; set; }
    }

    /// <summary>One row per item type: accumulated delta until flushed to API. Used by GetInventory merge and background flush.</summary>
    private sealed class LocalPendingEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string GameSource { get; set; } = string.Empty;
        public string ItemType { get; set; } = "KeyItem";
        public int Quantity { get; set; }
        /// <summary>Set when mint completes (pickup-with-mint) so merge shows [NFT] prefix in Quake/Doom overlay.</summary>
        public string? NftId { get; set; }
    }
}
