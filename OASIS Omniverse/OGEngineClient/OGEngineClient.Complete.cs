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
                var clientGame = _questClientGameSource ?? requestGameSource;
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

}
