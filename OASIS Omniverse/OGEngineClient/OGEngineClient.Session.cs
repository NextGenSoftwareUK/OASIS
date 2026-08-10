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
    private static void TryAppendLegacyObjectivePhrase(string part, List<string> lines)
    {
        part = part.Trim().TrimEnd('.', ')', ']', '…');
        if (part.Length == 0) return;
        const RegexOptions Rx = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        static string TrimGameToken(string s) => s.Trim().TrimEnd('.', ')', ']', '…', ',');

        /* "Kill … in <game>" with arbitrary middle text (commas, monster names). Avoids missing rows when API text is not exactly "Kill N monsters in X". */
        var m = Regex.Match(part, @"^Kill\s+(\d+)\s*(?s:.+?)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var k) && k > 0)
        {
            lines.Add($"Killed 0/{k} monsters in {TrimGameToken(m.Groups[2].Value)}");
            return;
        }

        /* "Kill N" with optional trailing "in Game" anywhere (some payloads omit "monsters"). */
        var mKill = Regex.Match(part, @"^Kill\s+(\d+)\b", Rx);
        if (mKill.Success && int.TryParse(mKill.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var kk) && kk > 0)
        {
            var mIn = Regex.Match(part, @"\bin\s+(\S+)\s*$", Rx);
            if (mIn.Success)
                lines.Add($"Killed 0/{kk} monsters in {TrimGameToken(mIn.Groups[1].Value)}");
            else
                lines.Add($"Killed 0/{kk} monsters");
            return;
        }

        m = Regex.Match(part, @"^Collect\s+keys?:\s*(\d+)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ck) && ck > 0)
        {
            lines.Add($"Collected 0/{ck} keys in {m.Groups[2].Value}");
            return;
        }

        m = Regex.Match(part, @"^Collect\s+health:\s*(\d+)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ch) && ch > 0)
        {
            lines.Add($"Collected 0/{ch} health in {m.Groups[2].Value}");
            return;
        }

        m = Regex.Match(part, @"^Collect\s+armor:\s*(\d+)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ca) && ca > 0)
        {
            lines.Add($"Collected 0/{ca} armor in {m.Groups[2].Value}");
            return;
        }

        m = Regex.Match(part, @"^Collect\s+ammo:\s*(\d+)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var am) && am > 0)
        {
            lines.Add($"Collected 0/{am} ammo in {m.Groups[2].Value}");
            return;
        }

        m = Regex.Match(part, @"^Collect\s+weapons?:\s*(\d+)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cw) && cw > 0)
        {
            lines.Add($"Collected 0/{cw} weapons in {m.Groups[2].Value}");
            return;
        }

        m = Regex.Match(part, @"^Collect\s+powerups?:\s*(\d+)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cp) && cp > 0)
        {
            lines.Add($"Collected 0/{cp} powerups in {m.Groups[2].Value}");
            return;
        }

        m = Regex.Match(part, @"^Collect\s+items?:\s*(\d+)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ci) && ci > 0)
        {
            lines.Add($"Collected 0/{ci} items in {m.Groups[2].Value}");
            return;
        }

        m = Regex.Match(part, @"^Earn\s+(\d+)\s+XP\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var xp) && xp > 0)
        {
            lines.Add($"Earned 0/{xp} XP in {m.Groups[2].Value}");
            return;
        }

        m = Regex.Match(part, @"^Complete\s+level:\s*(\d+)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cl) && cl > 0)
        {
            lines.Add($"Completed 0/{cl} levels in {m.Groups[2].Value}");
            return;
        }

        m = Regex.Match(part, @"^Use\s+weapons?:\s*(\d+)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uw) && uw > 0)
        {
            lines.Add($"Used 0/{uw} weapons in {m.Groups[2].Value}");
            return;
        }

        m = Regex.Match(part, @"^Use\s+powerups?:\s*(\d+)\s+in\s+(\S+)$", Rx);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var up) && up > 0)
        {
            lines.Add($"Used 0/{up} powerups in {m.Groups[2].Value}");
            return;
        }
    }

    /// <summary>Objective title for quest lists and detail popup. Always prefer authored Title.</summary>
    private static string GetObjectiveRawTitle(StarQuestObjective o, StarQuestInfo? quest)
    {
        return (o.Title ?? string.Empty).Trim();
    }

    /// <summary>Objective body text for detail popup left pane. Always prefer authored Description.</summary>
    private static string GetObjectiveRawDescription(StarQuestObjective o, string titleFallback)
    {
        return (o.Description ?? string.Empty).Trim();
    }

    /// <summary>Format Need* + progress dicts into HUD lines (current/required). When <paramref name="restrictToGameKey"/> is set, only the row matching that game key (aliases e.g. DOOM/ODOOM) is emitted and the trailing " in Game" suffix is omitted.</summary>
    private static void FormatRequirementProgressLines(StarQuestObjectiveDictionaries? dicts, List<string> outLines, string? restrictToGameKey = null)
    {
        if (dicts == null) return;
        var filterKey = string.IsNullOrWhiteSpace(restrictToGameKey) ? null : restrictToGameKey.Trim();

        static int ParseProgressForGame(Dictionary<string, List<string>>? d, string game)
        {
            if (d == null || !d.TryGetValue(game, out var list) || list == null || list.Count == 0) return 0;
            return GetFirstNonNegativeIntFromStringList(list);
        }

        void AddKeyedProgressLine(Dictionary<string, List<string>>? need, Dictionary<string, List<string>>? progress, string verb, string nounPlural)
        {
            if (need == null || need.Count == 0) return;
            if (filterKey != null)
            {
                var mk = ResolveMergeGameKey(need, filterKey);
                if (mk == null || !need.TryGetValue(mk, out var reqList)) return;
                int required = GetFirstPositiveIntFromStringList(reqList);
                int current = ParseProgressForGame(progress, mk);
                if (required <= 0) return;
                outLines.Add($"{verb} {current}/{required} {nounPlural}");
                return;
            }
            foreach (var kv in need)
            {
                var game = kv.Key;
                var reqList = kv.Value;
                int required = GetFirstPositiveIntFromStringList(reqList);
                int current = ParseProgressForGame(progress, game);
                if (required <= 0) continue;
                outLines.Add($"{verb} {current}/{required} {nounPlural} in {game}");
            }
        }

        if (dicts.NeedToKillMonsters is { Count: > 0 })
        {
            if (filterKey != null)
            {
                var mk = ResolveMergeGameKey(dicts.NeedToKillMonsters, filterKey);
                if (mk != null && dicts.NeedToKillMonsters.TryGetValue(mk, out var reqList))
                {
                    int required = GetFirstPositiveIntFromStringList(reqList);
                    int current = ParseProgressForGame(dicts.MonstersKilled, mk);
                    if (required > 0)
                        outLines.Add($"Killed {current}/{required} monsters");
                }
            }
            else
            {
                foreach (var kv in dicts.NeedToKillMonsters)
                {
                    var game = kv.Key;
                    var reqList = kv.Value;
                    int required = GetFirstPositiveIntFromStringList(reqList);
                    int current = ParseProgressForGame(dicts.MonstersKilled, game);
                    if (required <= 0) continue;
                    outLines.Add($"Killed {current}/{required} monsters in {game}");
                }
            }
        }

        AddKeyedProgressLine(dicts.NeedToCollectArmor, dicts.ArmorCollected, "Collected", "armor");
        AddKeyedProgressLine(dicts.NeedToCollectAmmo, dicts.AmmoCollected, "Collected", "ammo");
        AddKeyedProgressLine(dicts.NeedToCollectHealth, dicts.HealthCollected, "Collected", "health");
        AddKeyedProgressLine(dicts.NeedToCollectWeapons, dicts.WeaponsCollected, "Collected", "weapons");
        AddKeyedProgressLine(dicts.NeedToCollectPowerups, dicts.PowerupsCollected, "Collected", "powerups");
        AddKeyedProgressLine(dicts.NeedToCollectItems, dicts.ItemsCollected, "Collected", "items");
        AddKeyedProgressLine(dicts.NeedToCollectKeys, dicts.KeysCollected, "Collected", "keys");
        AddKeyedProgressLine(dicts.NeedToCompleteLevel, dicts.LevelsCompleted, "Completed", "levels");
        AddKeyedProgressLine(dicts.NeedToEarnKarma, dicts.KarmaEarnt, "Earned", "karma");
        AddKeyedProgressLine(dicts.NeedToEarnXP, dicts.XPEarnt, "Earned", "XP");
        AddKeyedProgressLine(dicts.NeedToGoToGeoHotSpots, dicts.GeoHotSpotsArrived, "Visited", "hot spots");
        AddKeyedProgressLine(dicts.NeedToUseWeapons, dicts.WeaponsCollected, "Used", "weapons");
        AddKeyedProgressLine(dicts.NeedToUsePowerups, dicts.PowerupsCollected, "Used", "powerups");
    }

    /// <summary>Objective label for embedded O-lines in <see cref="SerializeQuestsForGame"/>: API goal text only. Progress (Killed X/Y) lives in tracker + requirements CVar, not in the name column.</summary>
    private static string FormatObjectiveLineForGameList(StarQuestObjective o, StarQuestInfo? quest) =>
        EscapeForQuestLine(GetObjectiveRawTitle(o, quest));

    /// <summary>ProgressSummary for selected objective in the objectives popup lower pane.</summary>
    internal bool TryGetQuestObjectiveRequirementsForGame(string? questId, string? objectiveId, out string result)
    {
        result = string.Empty;
        if (string.IsNullOrWhiteSpace(questId)) return true;
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _questsCacheString == null) { EnsureQuestsCacheInBackground(); return false; }
            var quest = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, questId!.Trim(), StringComparison.OrdinalIgnoreCase));
            if (quest == null) return true;
            if (quest.Objectives == null || quest.Objectives.Count == 0) return true;
            StarQuestObjective? objective = null;
            if (!string.IsNullOrWhiteSpace(objectiveId))
                objective = quest.Objectives.FirstOrDefault(o => string.Equals(o.Id, objectiveId, StringComparison.OrdinalIgnoreCase));
            objective ??= quest.Objectives.OrderBy(o => o.Order).FirstOrDefault();
            result = objective?.ProgressSummary ?? string.Empty;
            return true;
        }
    }

    /// <summary>One display row per objective for HUD tracker: objective ProgressSummary.</summary>
    internal bool TryGetQuestTrackerObjectivesProgress(string? questId, out string linesResult, out int activeObjectiveIndex)
    {
        linesResult = string.Empty;
        activeObjectiveIndex = 0;
        if (string.IsNullOrWhiteSpace(questId)) return true;
        Guid? cachedObjId = null;
        lock (_stateLock)
        {
            cachedObjId = _cachedActiveObjectiveId;
        }
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _questsCacheString == null) { EnsureQuestsCacheInBackground(); return false; }
            var quest = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, questId!.Trim(), StringComparison.OrdinalIgnoreCase));
            if (quest == null || quest.Objectives == null || quest.Objectives.Count == 0) return true;
            var sb = new StringBuilder();
            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                var o = quest.Objectives[i];
                sb.Append(EscapeForQuestLine(o.ProgressSummary ?? string.Empty)).Append("\n");
            }
            linesResult = sb.ToString().TrimEnd();
            activeObjectiveIndex = 0;
            /* Prefer persisted active objective (user choice) when we have it; else use first incomplete (quest progress). */
            if (cachedObjId.HasValue && quest.Objectives != null)
            {
                var idStr = cachedObjId.Value.ToString("D");
                for (var i = 0; i < quest.Objectives.Count; i++)
                {
                    if (string.Equals(quest.Objectives[i].Id, idStr, StringComparison.OrdinalIgnoreCase))
                    {
                        activeObjectiveIndex = i;
                        return true;
                    }
                }
            }
            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                if (!quest.Objectives[i].IsCompleted)
                {
                    activeObjectiveIndex = i;
                    break;
                }
            }
            return true;
        }
    }

    /// <summary>Objective status token for game tab rows: completed, else <c>NotStarted</c> when parent quest is not started, else <c>InProgress</c>.</summary>
    private static string SerializeObjectiveStatusToken(StarQuestObjective o, StarQuestInfo? quest)
    {
        if (o.IsCompleted) return "Completed";
        var qs = quest?.Status?.Trim() ?? string.Empty;
        if (qs.Equals("NotStarted", StringComparison.OrdinalIgnoreCase) || qs.Equals("Not Started", StringComparison.OrdinalIgnoreCase))
            return "NotStarted";
        return "InProgress";
    }

    /// <summary>Serialize a quest's Objectives collection as Q-lines (id, name, desc, status, pct) for the game UI. Name/desc are API goal text; progress is not duplicated here (tracker + odoom_quest_detail_requirements).</summary>
    private static string SerializeObjectivesAsQuestLines(StarQuestInfo quest)
    {
        if (quest.Objectives == null || quest.Objectives.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        for (var i = 0; i < quest.Objectives.Count; i++)
        {
            var o = quest.Objectives[i];
            var oid = string.IsNullOrEmpty(o.Id) ? $"obj_{i}" : o.Id;
            var titleRaw = GetObjectiveRawTitle(o, quest);
            var descRaw = GetObjectiveRawDescription(o, titleRaw);
            var name = EscapeForQuestLine(titleRaw);
            var desc = EscapeForQuestLine(descRaw);
            var status = SerializeObjectiveStatusToken(o, quest);
            var pct = o.IsCompleted ? 100 : 0;
            sb.Append("Q\t").Append(oid).Append("\t").Append(name).Append("\t").Append(desc).Append("\t").Append(status).Append("\t").Append(pct).Append("\n");
        }
        return sb.ToString();
    }

    /// <summary>Get serialized prerequisite quests (id, name, desc) for right panel. Returns null if cache not ready.</summary>
    internal bool TryGetQuestPrereqsCache(string? questId, out string? cached)
    {
        cached = null;
        if (string.IsNullOrWhiteSpace(questId)) { cached = string.Empty; return true; }
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _questsCacheString == null) { EnsureQuestsCacheInBackground(); return false; }
            var qid = questId.Trim();
            var quest = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, qid, StringComparison.OrdinalIgnoreCase));
            if (quest?.PrerequisiteQuestIds == null || quest.PrerequisiteQuestIds.Count == 0)
            {
                if (_questsFilterLastLogPrereqs != (qid, 0))
                {
                    _questsFilterLastLogPrereqs = (qid, 0);
                }
                cached = string.Empty;
                return true;
            }
            var set = new HashSet<string>(quest.PrerequisiteQuestIds, StringComparer.OrdinalIgnoreCase);
            var prereqs = _cachedQuestList.Where(q => set.Contains(q.Id)).ToList();
            if (_questsFilterLastLogPrereqs != (qid, prereqs.Count))
            {
                _questsFilterLastLogPrereqs = (qid, prereqs.Count);
            }
            cached = prereqs.Count == 0 ? string.Empty : SerializeQuestsForGame(prereqs);
            return true;
        }
    }

    public async Task<OASISResult<StarItem>> AddItemAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", string? nftId = null, int quantity = 1, bool stack = true, CancellationToken cancellationToken = default)
    {
        return await AddItemCoreAsync(itemName, description, gameSource, itemType, nftId, quantity, stack, cancellationToken).ConfigureAwait(false);
    }

    public Task<OASISResult<StarItem>> QueueAddItemAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", string? nftId = null, int quantity = 1, bool stack = true, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<StarItem>("Client is not initialized.", StarApiResultCode.NotInitialized));

        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(gameSource))
            return Task.FromResult(FailAndCallback<StarItem>("Item name and game source are required.", StarApiResultCode.InvalidParam));

        EnqueueAddItemJobOnly(itemName, description, gameSource, itemType, nftId, quantity, stack);
        return Task.FromResult(Success(new StarItem { Id = Guid.Empty, Name = itemName, Description = description ?? string.Empty, GameSource = gameSource, ItemType = string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType, Quantity = quantity < 1 ? 1 : quantity }, StarApiResultCode.Success, "Queued for sync."));
    }

    public async Task<OASISResult<List<StarItem>>> QueueAddItemsAsync(IEnumerable<StarItem> items, string defaultGameSource = "Unknown", CancellationToken cancellationToken = default)
    {
        if (items is null)
            return FailAndCallback<List<StarItem>>("Items collection cannot be null.", StarApiResultCode.InvalidParam);

        var tasks = new List<Task<OASISResult<StarItem>>>();
        foreach (var item in items)
        {
            if (item is null)
                continue;

            var source = string.IsNullOrWhiteSpace(item.GameSource) ? defaultGameSource : item.GameSource;
            tasks.Add(QueueAddItemAsync(item.Name, item.Description, source, item.ItemType, string.IsNullOrWhiteSpace(item.NftId) ? null : item.NftId, item.Quantity, true, cancellationToken));
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var successful = new List<StarItem>();
        var errorMessages = new List<string>();

        foreach (var result in results)
        {
            if (!result.IsError && result.Result is not null)
                successful.Add(result.Result);
            else if (!string.IsNullOrWhiteSpace(result.Message))
                errorMessages.Add(result.Message);
        }

        if (errorMessages.Count > 0)
        {
            var failure = FailAndCallback<List<StarItem>>($"One or more queued item jobs failed: {string.Join(" | ", errorMessages)}", StarApiResultCode.ApiError);
            failure.Result = successful;
            return failure;
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(successful, StarApiResultCode.Success, $"Queued add-item jobs completed for {successful.Count} item(s).");
    }

    /// <summary>Storage name for add-item: include game suffix so ODOOM and OQUAKE pickups (armor, weapons, etc.) stack per-game, not merged.</summary>
    private static string ItemNameWithGameSource(string itemName, string gameSource)
    {
        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(gameSource)) return itemName;
        var g = gameSource.Trim();
        if (g.Equals("Quake", StringComparison.OrdinalIgnoreCase)) g = "OQUAKE";
        if (!g.Equals("ODOOM", StringComparison.OrdinalIgnoreCase) && !g.Equals("OQUAKE", StringComparison.OrdinalIgnoreCase))
            return itemName;
        if (itemName.Contains(" (ODOOM)") || itemName.Contains(" (OQUAKE)"))
            return itemName;
        return $"{itemName} ({g})";
    }

    /// <summary>Add pickup to local delta (one row per type). Used by native C: game calls this on pickup; GetInventory returns API + pending; worker flushes to API in background. No per-call HTTP.</summary>
    public void EnqueueAddItemJobOnly(string itemName, string description, string gameSource, string itemType = "KeyItem", string? nftId = null, int quantity = 1, bool stack = true)
    {
        if (!IsInitialized() || string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(gameSource))
            return;
        var storageName = ItemNameWithGameSource(itemName, gameSource);
        var qty = quantity < 1 ? 1 : quantity;
        var type = string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType;
        lock (_localPendingLock)
        {
            if (_localPending.TryGetValue(storageName, out var existing))
            {
                existing.Quantity += qty;
            }
            else
            {
                _localPending[storageName] = new LocalPendingEntry
                {
                    Name = storageName,
                    Description = description ?? string.Empty,
                    GameSource = gameSource,
                    ItemType = type,
                    Quantity = qty
                };
            }
        }
        _addItemSignal.Release();
        var keysDelta = (type != null && (type.IndexOf("Key", StringComparison.OrdinalIgnoreCase) >= 0 || type.IndexOf("key", StringComparison.OrdinalIgnoreCase) >= 0)) ? 1 : 0;
        EnqueueQuestProgressFromGame(gameSource, 0, 0, itemName, keysDelta, 1, null, itemType);
    }

    /// <summary>Queue XP to add to the beamed-in avatar (e.g. on monster kill). Flushed with add-item worker. Amount 0 allowed (temp) for refresh / get newTotal from server.</summary>
    public void EnqueueAddXpJobOnly(int amount)
    {
        if (!IsInitialized()) return;
        if (amount < 0) return;
        Interlocked.Add(ref _pendingXp, amount);
        _addItemSignal.Release();
    }

    /// <summary>Send pending XP to the API (POST add-xp). Returns new total on success. Used by background worker and flush. amount 0 is allowed: no-op add but response newTotal updates cache (same code path as monster kill).</summary>
    public async Task<OASISResult<int>> AddXpAsync(int amount, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<int>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (amount < 0)
            return FailAndCallback<int>("XP amount must be non-negative.", StarApiResultCode.InvalidParam);
        string? avatarId;
        lock (_stateLock)
            avatarId = _avatarId;
        if (string.IsNullOrWhiteSpace(avatarId))
            return FailAndCallback<int>("Avatar ID not set. Beam in first.", StarApiResultCode.NotInitialized);

        if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
            return FailAndCallback<int>(missingWeb4, StarApiResultCode.InvalidParam);

        var url = $"{web4Base}/api/avatar/add-xp";
        try
        {
            var payload = BuildJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteNumber("amount", amount);
                writer.WriteEndObject();
            });
            if (amount == 0)
                OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] POST url={url}");
            var response = await SendRawAsync(HttpMethod.Post, url, payload, cancellationToken).ConfigureAwait(false);
            var rawPreview = response.Result != null && response.Result.Length > 0
                ? (response.Result.Length <= 400 ? response.Result : response.Result[..400] + "...")
                : "(null or empty)";
            if (amount == 0)
                OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] response IsError={response.IsError} body={rawPreview}");
            if (response.IsError)
            {
                if (amount == 0)
                    OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] failed: {response.Message}");
                return FailAndCallback<int>(response.Message ?? "Add XP failed.", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
            }

            var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
            if (!parseResult)
            {
                if (amount == 0)
                    OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] parse failed: {parseErrorMessage}");
                return FailAndCallback<int>(parseErrorMessage, parseErrorCode);
            }

            var newTotal = GetIntProperty(resultElement, "newTotal") ?? GetIntProperty(resultElement, "NewTotal")
                ?? GetIntProperty(resultElement, "xp") ?? GetIntProperty(resultElement, "XP");
            if (amount == 0)
                OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] parsed newTotal={newTotal?.ToString() ?? "null"}");
            if (newTotal.HasValue && newTotal.Value >= 0)
            {
                Volatile.Write(ref _cachedAvatarXp, newTotal.Value);
                if (amount == 0)
                    OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] cache updated to {newTotal.Value}");
                InvokeCallback(StarApiResultCode.Success);
                return Success(newTotal.Value, StarApiResultCode.Success, amount == 0 ? "XP refreshed." : "XP added.");
            }
            /* No newTotal in response: assume current + amount (skip when amount is 0). */
            if (amount == 0)
            {
                OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] no newTotal in response; cache stays at {Volatile.Read(ref _cachedAvatarXp)}");
                InvokeCallback(StarApiResultCode.Success);
                return Success(Volatile.Read(ref _cachedAvatarXp), StarApiResultCode.Success, "XP refresh (no newTotal in response).");
            }
            var updated = Volatile.Read(ref _cachedAvatarXp) + amount;
            Volatile.Write(ref _cachedAvatarXp, updated);
            InvokeCallback(StarApiResultCode.Success);
            return Success(updated, StarApiResultCode.Success, "XP added.");
        }
        catch (Exception ex)
        {
            if (amount == 0)
                OGEngineExports.StarApiLog($"[XP refresh add-xp(0)] exception: {ex.Message}");
            return FailAndCallback<int>($"Add XP failed: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    /// <summary>Last known avatar XP (from get-current-avatar or add-xp). For ogengine_get_avatar_xp.</summary>
    public int GetCachedAvatarXp() => Volatile.Read(ref _cachedAvatarXp);
    public long GetCachedAvatarKarma() => Volatile.Read(ref _cachedAvatarKarma);

    /// <summary>Last active quest ID from avatar detail (restored after beam-in).</summary>
    public Guid? GetCachedActiveQuestId()
    {
        lock (_stateLock) return _cachedActiveQuestId;
    }
    /// <summary>Last active objective ID from avatar detail (restored after beam-in).</summary>
    public Guid? GetCachedActiveObjectiveId()
    {
        lock (_stateLock) return _cachedActiveObjectiveId;
    }

    /// <summary>Resolve quest and objective names from cache for logging (save/load debug). Returns (null, null) if cache not ready or ids not found.</summary>
    private (string? questName, string? objectiveName) TryGetQuestAndObjectiveNamesFromCache(Guid? questId, Guid? objectiveId)
    {
        if (!questId.HasValue) return (null, null);
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return (null, null);
            var idStr = questId.Value.ToString();
            var q = _cachedQuestList.FirstOrDefault(x => string.Equals(x.Id, idStr, StringComparison.OrdinalIgnoreCase));
            if (q == null) return (null, null);
            string? objName = null;
            if (objectiveId.HasValue && q.Objectives != null)
            {
                var oidStr = objectiveId.Value.ToString();
                var o = q.Objectives.FirstOrDefault(o => string.Equals(o.Id, oidStr, StringComparison.OrdinalIgnoreCase));
                objName = o?.Title ?? o?.Description;
            }
            return (q.Name, objName);
        }
    }

    /// <summary>File log: default one totals line; per-quest lines when <c>STAR_VERBOSE_QUEST_LIST=1</c>. Caller must hold <see cref="_questsCacheLock"/>.</summary>
    private void LogTopLevelQuestPctSnapshotUnderQuestLock(string reason, Guid? activeQuestId)
    {
        if (_cachedQuestList is null || _cachedQuestList.Count == 0)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quests][PctSnapshot] reason={reason} cache=empty activeQuest={(activeQuestId.HasValue ? activeQuestId.Value.ToString("D") : "(null)")}"); } catch { /* ignore */ }
            return;
        }
        var top = _cachedQuestList
            .Where(q => string.IsNullOrWhiteSpace(q.ParentQuestId) || q.ParentQuestId == Guid.Empty.ToString())
            .OrderBy(q => q.Id ?? string.Empty, StringComparer.Ordinal)
            .ToList();
        var totalObj = 0;
        var doneObj = 0;
        foreach (var q in top)
        {
            totalObj += q.Objectives?.Count ?? 0;
            doneObj += q.Objectives?.Count(o => o.IsCompleted) ?? 0;
        }
        try
        {
            OGEngineExports.StarApiLogFileOnly($"[Quests][PctSnapshot] reason={reason} questsInCache={_cachedQuestList.Count} topLevel={top.Count} objectivesCompleted={doneObj}/{totalObj} activeQuest={activeQuestId?.ToString("D") ?? "(null)"}");
        }
        catch { /* ignore */ }
        if (!VerboseQuestListLogsEnabled) return;
        var sb = new StringBuilder(512 + top.Count * 120);
        sb.Append("[Quests][PctSnapshot][verbose] reason=").Append(reason)
            .Append(" topLevel=").Append(top.Count)
            .Append(" activeQuest=").Append(activeQuestId?.ToString("D") ?? "(null)");
        var n = 0;
        foreach (var q in top)
        {
            if (n++ >= 32)
            {
                sb.Append(" | ...(truncated)");
                break;
            }
            var oc = q.Objectives?.Count ?? 0;
            var done = q.Objectives?.Count(o => o.IsCompleted) ?? 0;
            var pct = oc > 0 ? done * 100 / oc : 0;
            var mark = activeQuestId.HasValue && activeQuestId.Value != Guid.Empty && string.Equals(q.Id, activeQuestId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase) ? "*" : "";
            sb.Append(" | ").Append(mark).Append(q.Id).Append(':').Append(EscapeForQuestLine(q.Name ?? ""))
                .Append(" st=").Append(q.Status ?? "")
                .Append(" obj=").Append(done).Append('/').Append(oc)
                .Append(" pct=").Append(pct);
        }
        try { OGEngineExports.StarApiLogFileOnly(sb.ToString()); } catch { /* ignore */ }
    }

    /// <summary>Log top-level quest pct from an in-memory list (e.g. incoming GET payload that will not be applied).</summary>
    private void LogTopLevelQuestPctSnapshotFromList(string reason, List<StarQuestInfo>? list, Guid? activeQuestId)
    {
        if (list is null || list.Count == 0)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quests][PctSnapshot] reason={reason} list=empty activeQuest={(activeQuestId.HasValue ? activeQuestId.Value.ToString("D") : "(null)")}"); } catch { /* ignore */ }
            return;
        }
        var top = list
            .Where(q => string.IsNullOrWhiteSpace(q.ParentQuestId) || q.ParentQuestId == Guid.Empty.ToString())
            .OrderBy(q => q.Id ?? string.Empty, StringComparer.Ordinal)
            .ToList();
        var totalObj = 0;
        var doneObj = 0;
        foreach (var q in top)
        {
            totalObj += q.Objectives?.Count ?? 0;
            doneObj += q.Objectives?.Count(o => o.IsCompleted) ?? 0;
        }
        try
        {
            OGEngineExports.StarApiLogFileOnly($"[Quests][PctSnapshot] reason={reason} questsInList={list.Count} topLevel={top.Count} objectivesCompleted={doneObj}/{totalObj} activeQuest={activeQuestId?.ToString("D") ?? "(null)"}");
        }
        catch { /* ignore */ }
        if (!VerboseQuestListLogsEnabled) return;
        var sb = new StringBuilder(512 + top.Count * 120);
        sb.Append("[Quests][PctSnapshot][verbose] reason=").Append(reason)
            .Append(" topLevel=").Append(top.Count)
            .Append(" activeQuest=").Append(activeQuestId?.ToString("D") ?? "(null)");
        var n = 0;
        foreach (var q in top)
        {
            if (n++ >= 32)
            {
                sb.Append(" | ...(truncated)");
                break;
            }
            var oc = q.Objectives?.Count ?? 0;
            var done = q.Objectives?.Count(o => o.IsCompleted) ?? 0;
            var pct = oc > 0 ? done * 100 / oc : 0;
            var mark = activeQuestId.HasValue && activeQuestId.Value != Guid.Empty && string.Equals(q.Id, activeQuestId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase) ? "*" : "";
            sb.Append(" | ").Append(mark).Append(q.Id).Append(':').Append(EscapeForQuestLine(q.Name ?? ""))
                .Append(" st=").Append(q.Status ?? "")
                .Append(" obj=").Append(done).Append('/').Append(oc)
                .Append(" pct=").Append(pct);
        }
        try { OGEngineExports.StarApiLogFileOnly(sb.ToString()); } catch { /* ignore */ }
    }

    /// <summary>Native: quest list popup opened (1) or closed (0). While open, progress and quest-cache replacement from refresh are ignored.</summary>
    public void NotifyQuestPopupOpenChanged(int isOpen)
    {
        var newVal = isOpen != 0 ? 1 : 0;
        Interlocked.Exchange(ref _questUiPopupOpen, newVal);
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] quest popup open={newVal}"); } catch { /* ignore */ }
    }

    /// <summary>File log: default one-line summary; full objective/tracker dump when <c>STAR_VERBOSE_QUEST_LIST=1</c>. Search <c>[Quest][ActiveSnapshot]</c>.</summary>
    private void LogActiveQuestSnapshot(string reason)
    {
        Guid? qid;
        Guid? oid;
        lock (_stateLock)
        {
            qid = _cachedActiveQuestId;
            oid = _cachedActiveObjectiveId;
        }

        if (!VerboseQuestListLogsEnabled)
        {
            string? qn = null;
            var objN = 0;
            if (qid.HasValue && qid.Value != Guid.Empty)
            {
                lock (_questsCacheLock)
                {
                    if (_cachedQuestList != null)
                    {
                        var qs = qid.Value.ToString("D");
                        var quest = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, qs, StringComparison.OrdinalIgnoreCase));
                        if (quest != null)
                        {
                            qn = quest.Name;
                            objN = quest.Objectives?.Count ?? 0;
                        }
                    }
                }
            }
            try
            {
                OGEngineExports.StarApiLogFileOnly($"[Quest][ActiveSnapshot] reason={reason} activeQuest={qid?.ToString("D") ?? "(null)"} activeObjective={oid?.ToString("D") ?? "(null)"} questName={qn ?? "(n/a)"} objectiveCount={objN}");
            }
            catch { /* ignore */ }
            return;
        }

        var sb = new StringBuilder(1024);
        sb.Append("[Quest][ActiveSnapshot] reason=").Append(reason)
            .Append(" | cachedActiveQuestId=").Append(qid?.ToString("D") ?? "(null)")
            .Append(" | cachedActiveObjectiveId=").Append(oid?.ToString("D") ?? "(null)")
            .AppendLine();

        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _cachedQuestList.Count == 0)
            {
                sb.Append("  questListCache: empty (no quests loaded yet)").AppendLine();
            }
            else if (!qid.HasValue || qid.Value == Guid.Empty)
            {
                sb.Append("  questListCache: ").Append(_cachedQuestList.Count).Append(" quests | no active quest id in client cache").AppendLine();
            }
            else
            {
                var qs = qid.Value.ToString("D");
                var quest = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, qs, StringComparison.OrdinalIgnoreCase));
                if (quest == null)
                {
                    sb.Append("  activeQuestId NOT FOUND in questListCache: ").Append(qs).AppendLine();
                    sb.Append("  hint: refresh quest cache or check id spelling vs API").AppendLine();
                }
                else
                {
                    sb.Append("  resolvedQuest: Id=").Append(quest.Id)
                        .Append(" Name=").Append(quest.Name ?? "")
                        .Append(" Status=").Append(quest.Status ?? "")
                        .Append(" GameSource=").Append(quest.GameSource ?? "")
                        .AppendLine();
                    if (quest.Objectives == null || quest.Objectives.Count == 0)
                    {
                        sb.Append("  objectives: (none on this quest in cache)").AppendLine();
                    }
                    else
                    {
                        for (var i = 0; i < quest.Objectives.Count; i++)
                        {
                            var o = quest.Objectives[i];
                            var isMarked = oid.HasValue && string.Equals(o.Id, oid.Value.ToString("D"), StringComparison.OrdinalIgnoreCase);
                            var listLine = FormatObjectiveLineForGameList(o, quest);
                            sb.Append("  objective[").Append(i).Append("] Id=").Append(o.Id)
                                .Append(" Order=").Append(o.Order)
                                .Append(" IsCompleted=").Append(o.IsCompleted)
                                .Append(" GameSource=").Append(o.GameSource ?? "")
                                .Append(isMarked ? " **matches cachedActiveObjectiveId**" : "")
                                .AppendLine();
                            sb.Append("             Title=").Append(o.Title ?? "").AppendLine();
                            sb.Append("             Description=").Append(o.Description ?? "").AppendLine();
                            sb.Append("             listLine(Q/O tab text)=").Append(listLine).AppendLine();
                        }
                    }
                }
            }
        }

        var qidStr = qid?.ToString("D");
        if (!string.IsNullOrWhiteSpace(qidStr))
        {
            if (TryGetQuestTrackerObjectivesProgress(qidStr, out var trk, out var hudIdx))
            {
                sb.Append("  hudTrackerObjectiveIndex (green / first incomplete): ").Append(hudIdx).AppendLine();
                if (string.IsNullOrWhiteSpace(trk))
                    sb.Append("  hudTrackerRows: (empty — no per-objective lines from Need/Progress dicts)").AppendLine();
                else
                {
                    sb.Append("  hudTrackerRows (same as odoom_quest_tracker_objectives, one row per objective):").AppendLine();
                    var rows = trk.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    for (var r = 0; r < rows.Length; r++)
                        sb.Append("    [").Append(r).Append("] ").Append(rows[r]).AppendLine();
                }
            }
            else
                sb.Append("  hudTrackerRows: (TryGetQuestTrackerObjectivesProgress false — quest cache not ready)").AppendLine();
        }

        try { OGEngineExports.StarApiLogFileOnly(sb.ToString()); } catch { /* ignore */ }
        try { OGEngineExports.StarApiLogFileOnly($"[Quest][ActiveSnapshot] reason={reason} (verbose block logged above)"); } catch { /* ignore */ }
    }

    private static readonly bool VerboseQuestParseLogsEnabled =
        string.Equals(Environment.GetEnvironmentVariable("STAR_VERBOSE_QUEST_PARSE"), "1", StringComparison.OrdinalIgnoreCase);

    /// <summary>Per-quest pct lines in file log, full <see cref="LogActiveQuestSnapshot"/>, and console merge-detail. Default off so STAR debug stays readable; set <c>STAR_VERBOSE_QUEST_LIST=1</c> for deep quest list logging.</summary>
    private static readonly bool VerboseQuestListLogsEnabled =
        string.Equals(Environment.GetEnvironmentVariable("STAR_VERBOSE_QUEST_LIST"), "1", StringComparison.OrdinalIgnoreCase);

    /// <summary>File-only: logs large strings in fixed-size chunks (search <c>[Quest][Parse]</c> in ogengine.log).</summary>
    private static void LogQuestParseChunkedFileOnly(string linePrefix, string? text)
    {
        if (!VerboseQuestParseLogsEnabled) return;
        try
        {
            if (string.IsNullOrEmpty(text))
            {
                OGEngineExports.StarApiLogFileOnly($"{linePrefix} (empty)");
                return;
            }

            const int maxChunk = 3200;
            if (text.Length <= maxChunk)
            {
                OGEngineExports.StarApiLogFileOnly($"{linePrefix} len={text.Length} {text}");
                return;
            }

            OGEngineExports.StarApiLogFileOnly($"{linePrefix} len={text.Length} chunkSize={maxChunk}");
            for (var i = 0; i < text.Length; i += maxChunk)
            {
                var take = Math.Min(maxChunk, text.Length - i);
                OGEngineExports.StarApiLogFileOnly($"{linePrefix} offset={i} len={take} {text.AsSpan(i, take).ToString()}");
            }
        }
        catch (Exception ex)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[Quest][Parse] LogQuestParseChunkedFileOnly error: {ex.Message}"); } catch { /* ignore */ }
        }
    }

    private static void LogQuestJsonShapeFileOnly(string prefix, JsonElement el)
    {
        if (!VerboseQuestParseLogsEnabled) return;
        try
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Array:
                    OGEngineExports.StarApiLogFileOnly($"{prefix} ValueKind=Array Length={el.GetArrayLength()}");
                    break;
                case JsonValueKind.Object:
                    var names = string.Join(", ", el.EnumerateObject().Select(p => p.Name));
                    OGEngineExports.StarApiLogFileOnly($"{prefix} ValueKind=Object PropertyNames=[{names}]");
                    break;
                default:
                    OGEngineExports.StarApiLogFileOnly($"{prefix} ValueKind={el.ValueKind}");
                    break;
            }
        }
        catch (Exception ex)
        {
            try { OGEngineExports.StarApiLogFileOnly($"{prefix} shapeLogError={ex.Message}"); } catch { /* ignore */ }
        }
    }

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
