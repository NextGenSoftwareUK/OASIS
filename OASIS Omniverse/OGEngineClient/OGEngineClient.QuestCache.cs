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
    /// <summary>Replace the quest in _cachedQuestList with a copy that has the given objectives, so the next TryGetQuestObjectivesCache lookup returns them. Caller must hold _questsCacheLock. Increments _questObjectivesCacheVersion so UI can re-read.</summary>
    private void MergeObjectivesIntoCachedQuest(string questId, List<StarQuestObjective> objectives)
    {
        if (_cachedQuestList == null || objectives == null || objectives.Count == 0) return;
        for (var i = 0; i < _cachedQuestList.Count; i++)
        {
            var q = _cachedQuestList[i];
            if (!string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase)) continue;
            var updated = new StarQuestInfo
            {
                Id = q.Id,
                Name = q.Name,
                Description = q.Description,
                Status = q.Status,
                Order = q.Order,
                GameSource = q.GameSource ?? string.Empty,
                Requirements = q.Requirements ?? new List<string>(),
                RewardKarma = q.RewardKarma,
                RewardXP = q.RewardXP,
                CompletionNotes = q.CompletionNotes,
                ParentMissionId = q.ParentMissionId ?? string.Empty,
                ParentQuestId = q.ParentQuestId ?? string.Empty,
                Objectives = objectives,
                PrerequisiteQuestIds = q.PrerequisiteQuestIds ?? new List<string>(),
                LinkedGeoHotSpotId = q.LinkedGeoHotSpotId,
                ExternalHandoffUri = q.ExternalHandoffUri,
                Dictionaries = q.Dictionaries
            };
            _cachedQuestList[i] = updated;
            _questObjectivesCacheVersion++;
            OGEngineExports.StarApiLogFileOnly($"[Quests] Merged {objectives.Count} objectives into cached quest {questId}; cache version now {_questObjectivesCacheVersion}. UI should re-call get_quest_objectives_string to refresh.");
            break;
        }
    }

    /// <summary>Objectives cache version; increments when on-demand fetch merges objectives. UI polls this each frame and re-calls get_quest_objectives_string when it changes so the list refreshes.</summary>
    internal int GetQuestObjectivesCacheVersion()
    {
        lock (_questsCacheLock) { return _questObjectivesCacheVersion; }
    }

    /// <summary>Fetch a single quest by id and return its Objectives (for on-demand fill when all-for-avatar had 0).</summary>
    private async Task<List<StarQuestObjective>?> FetchSingleQuestObjectivesAsync(string questId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrEmpty(_baseApiUrl)) return null;
        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/quests/{questId}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError || string.IsNullOrWhiteSpace(response.Result)) return null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out _, out _);
        if (!parseResult || resultElement.ValueKind != JsonValueKind.Object) return null;
        var src = $"GET.api.quests/{questId.Trim()}";
        LogQuestParseChunkedFileOnly($"[Quest][Parse] source={src} full HTTP body", response.Result);
        LogQuestJsonShapeFileOnly($"[Quest][Parse] source={src} envelope object", resultElement);
        var quest = ParseSingleQuestInfo(resultElement);
        LogParsedSingleQuestModelAudit(src, quest);
        return quest?.Objectives;
    }

    /// <summary>Get serialized sub-quests (child quests with ParentQuestId set) for a parent quest for right panel. Objectives are on Quest.Objectives, not in this list.</summary>
    internal bool TryGetQuestSubQuestsCache(string? parentQuestId, out string? cached)
    {
        cached = null;
        if (string.IsNullOrWhiteSpace(parentQuestId)) { cached = string.Empty; return true; }
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _questsCacheString == null) { EnsureQuestsCacheInBackground(); return false; }
            var id = parentQuestId.Trim();
            var sub = _cachedQuestList.Where(q => string.Equals(q.ParentQuestId, id, StringComparison.OrdinalIgnoreCase)).ToList();
            if (_questsFilterLastLogSubQuests != (id, sub.Count))
            {
                _questsFilterLastLogSubQuests = (id, sub.Count);
            }
            cached = sub.Count == 0 ? string.Empty : SerializeQuestsForGame(sub);
            return true;
        }
    }

    /// <summary>Prefer runtime client game, then objective/quest source, then last progress POST game key.</summary>
    private static string? ResolvePreferredGameKeyForQuestUi(string? clientGs, string? objectiveGameSource, string? questGameSource, string? lastProgressGs)
    {
        foreach (var c in new[] { clientGs, objectiveGameSource, questGameSource, lastProgressGs })
        {
            if (!string.IsNullOrWhiteSpace(c)) return c.Trim();
        }
        return null;
    }

    /// <summary>Requirement payloads often interleave labels (e.g. monster names) with counts; use the first positive integer in the list as the required tally.</summary>
    private static int GetFirstPositiveIntFromStringList(List<string>? list)
    {
        if (list == null) return 0;
        foreach (var s in list)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0)
                return n;
        }
        return 0;
    }

    /// <summary>Progress lists usually store the tally in the first parseable non-negative integer.</summary>
    private static int GetFirstNonNegativeIntFromStringList(List<string>? list)
    {
        if (list == null) return 0;
        foreach (var s in list)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 0)
                return n;
        }
        return 0;
    }

    /// <summary>When API/DB only has ONODE-style <c>Objective</c> text (no parsed dictionaries), map phrases to HUD lines so ODOOM shows Killed 0/N not "Kill N in …".</summary>
    private static void AppendLegacyObjectiveDescriptionProgressLines(string? desc, List<string> outLines)
    {
        if (string.IsNullOrWhiteSpace(desc)) return;
        var t = desc.Trim();
        var parts = t.Split(new[] { " and " }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        foreach (var p in parts)
            TryAppendLegacyObjectivePhrase(p, outLines);
    }

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

}
