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
}
