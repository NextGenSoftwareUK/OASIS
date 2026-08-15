using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class QuestManager
    {
        /// <summary>
        /// Returns the CrossGameEventsOnActivate of the first (lowest Order) incomplete objective on a quest.
        /// Call this immediately after StartQuestAsync to fire the opening events for the quest's first objective
        /// (intro audio, narration, spawn escort NPC, etc.).
        /// Returns an empty list when the quest has no objectives or the first objective has no activate events.
        /// </summary>
        public async Task<OASISResult<List<CrossGameEvent>>> GetFirstObjectiveActivationEventsAsync(Guid avatarId, Guid questId)
        {
            var result = new OASISResult<List<CrossGameEvent>> { Result = new List<CrossGameEvent>() };
            try
            {
                var questResult = await LoadAsync(avatarId, questId);
                if (questResult.IsError || questResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error in GetFirstObjectiveActivationEventsAsync: quest not found. Reason: {questResult.Message}");
                    return result;
                }
                var first = questResult.Result.Objectives?
                    .Where(o => !o.IsCompleted)
                    .OrderBy(o => o.Order).ThenBy(o => o.Id)
                    .FirstOrDefault();
                if (first?.CrossGameEventsOnActivate?.Count > 0)
                    result.Result = first.CrossGameEventsOnActivate;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetFirstObjectiveActivationEventsAsync: {ex.Message}");
            }
            return result;
        }

        /// <summary>Incomplete objectives in apply order: <paramref name="activeObjectiveId"/> first if present on the quest, then by <see cref="Objective.Order"/> and Id. Same ordering intent as STAR client <c>MergeQuestProgressIntoLocalCache</c>.</summary>
        private static IEnumerable<Objective> OrderIncompleteObjectivesForProgress(IList<Objective>? objectives, Guid? activeObjectiveId)
        {
            if (objectives == null || objectives.Count == 0) yield break;
            var incomplete = objectives.Where(o => !o.IsCompleted).ToList();
            Objective? activeFirst = null;
            if (activeObjectiveId.HasValue && activeObjectiveId.Value != Guid.Empty)
                activeFirst = incomplete.FirstOrDefault(o => o.Id == activeObjectiveId.Value);
            if (activeFirst != null)
            {
                yield return activeFirst;
                incomplete.Remove(activeFirst);
            }
            foreach (var o in incomplete.OrderBy(x => x.Order).ThenBy(x => x.Id))
                yield return o;
        }

        /// <summary>
        /// Applies in-game progress (kills, pickups, XP, level time) to the quest's incomplete objectives.
        /// Updates progress dictionaries; completes objectives when thresholds are met; completes the quest when all objectives are done.
        /// Objectives are processed in <see cref="OrderIncompleteObjectivesForProgress"/> order (active objective first when <see cref="QuestProgressDelta.ActiveObjectiveId"/> is set); every incomplete row receives the delta bundle and <see cref="ApplyDeltaToObjective"/> only applies matching Need* fields (same idea as the STAR client merge).
        /// </summary>
        public async Task<OASISResult<QuestProgressApplyResult>> ApplyQuestProgressAsync(Guid avatarId, Guid questId, string gameSource, QuestProgressDelta delta)
        {
            OASISResult<QuestProgressApplyResult> result = new OASISResult<QuestProgressApplyResult> { Result = new QuestProgressApplyResult() };
            string errorMessage = "Error occurred in QuestManager.ApplyQuestProgressAsync. Reason:";
            try
            {
                if (string.IsNullOrWhiteSpace(gameSource))
                    gameSource = "ODOOM";
                var gs = gameSource.Trim();
                var questResult = await LoadAsync(avatarId, questId);
                if (questResult.IsError || questResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Quest not found. Reason: {questResult.Message}");
                    return result;
                }
                var quest = questResult.Result;
                if (quest.Status != QuestStatus.InProgress && quest.Status != QuestStatus.NotStarted)
                {
                    result.Result.Message = "Quest not in progress; no progress applied.";
                    result.Result.ObjectivesCompleted = 0;
                    result.Result.QuestCompleted = false;
                    result.Result.PercentComplete = ComputeQuestPercent(quest);
                    return result;
                }
                if (quest.Status == QuestStatus.NotStarted)
                {
                    quest.Status = QuestStatus.InProgress;
                    if (quest.StartedOn == DateTime.MinValue)
                        quest.StartedOn = DateTime.UtcNow;
                    quest.StartedBy = avatarId;
                }
                if (quest.Objectives == null)
                    quest.Objectives = new List<Objective>();
                int completedThisRound = 0;
                var eventsToDispatch = new List<CrossGameEvent>();
                var itemsToGrant = new List<Guid>();
                /* Match STAR client MergeQuestProgressIntoLocalCache: process every incomplete objective (active first when
                   ActiveObjectiveId is set). Each objective only absorbs deltas that match its Need* rows — e.g. kills on
                   the kill objective, health pickups on the health objective. Applying to active only dropped health/armor/etc.
                   when the profile pointed at a different incomplete row than the one carrying NeedToCollectHealth. */
                Guid? orderByActive = delta.ActiveObjectiveId.HasValue && delta.ActiveObjectiveId.Value != Guid.Empty
                    ? delta.ActiveObjectiveId
                    : null;
                IEnumerable<Objective> progressTargets = OrderIncompleteObjectivesForProgress(quest.Objectives, orderByActive);
                foreach (var objective in progressTargets)
                {
                    ApplyDeltaToObjective(objective, gs, delta);
                    objective.InvalidateObjectiveString();
                    if (IsObjectiveRequirementsMet(objective, gs))
                    {
                        objective.IsCompleted = true;
                        objective.CompletedAt = DateTime.UtcNow;
                        objective.CompletedBy = avatarId;
                        completedThisRound++;
                        // Collect on-complete cross-game events and inventory rewards
                        if (objective.CrossGameEventsOnComplete?.Count > 0)
                            eventsToDispatch.AddRange(objective.CrossGameEventsOnComplete);
                        if (objective.RewardInventoryItemIds?.Count > 0)
                            itemsToGrant.AddRange(objective.RewardInventoryItemIds);
                    }
                }
                // If any objectives completed this round, the next one in Order is now active — fire its OnActivate events
                if (completedThisRound > 0)
                {
                    var nextActive = quest.Objectives
                        .Where(o => !o.IsCompleted)
                        .OrderBy(o => o.Order).ThenBy(o => o.Id)
                        .FirstOrDefault();
                    if (nextActive?.CrossGameEventsOnActivate?.Count > 0)
                        eventsToDispatch.AddRange(nextActive.CrossGameEventsOnActivate);
                }
                var allDone = quest.Objectives.Count > 0 && quest.Objectives.All(x => x.IsCompleted);
                if (allDone)
                {
                    quest.Status = QuestStatus.Completed;
                    quest.CompletedOn = DateTime.UtcNow;
                    quest.CompletedBy = avatarId;
                    if (quest.MetaData == null) quest.MetaData = new Dictionary<string, object>();
                    quest.MetaData["Status"] = quest.Status.ToString();
                    result.Result.QuestCompleted = true;
                    // Quest-level inventory rewards granted when all objectives are done
                    if (quest.RewardInventoryItemIds?.Count > 0)
                        itemsToGrant.AddRange(quest.RewardInventoryItemIds);
                }
                else
                {
                    if (quest.MetaData == null) quest.MetaData = new Dictionary<string, object>();
                    quest.MetaData["Status"] = QuestStatus.InProgress.ToString();
                }
                var pct = ComputeQuestPercent(quest);
                quest.ProgressPercent = pct;
                if (quest.MetaData != null)
                    quest.MetaData["ProgressPercent"] = pct.ToString(System.Globalization.CultureInfo.InvariantCulture);
                foreach (var obj in quest.Objectives)
                    obj.ProgressPercent = obj.IsCompleted ? 100 : ObjectiveApproximatePercent(obj);
                var updateResult = await UpdateAsync(avatarId, quest);
                if (updateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to save. Reason: {updateResult.Message}");
                    return result;
                }
                result.Result.ObjectivesCompleted = completedThisRound;
                result.Result.PercentComplete = pct;
                result.Result.CrossGameEventsToDispatch = eventsToDispatch;
                result.Result.InventoryItemsToGrant = itemsToGrant;
                result.Result.Message = allDone ? "Quest completed." : $"Progress updated ({pct}% complete).";
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}");
            }
            return result;
        }

        /// <summary>Clears every embedded objective’s progress dictionaries and quest-level progress; does not change Need* requirements.
        /// Resets completion flags and 0% approx; if the quest was Completed, sets status back to InProgress.</summary>
        public async Task<OASISResult<Quest>> ResetObjectiveProgressAsync(Guid avatarId, Guid questId)
        {
            OASISResult<Quest> result = new OASISResult<Quest>();
            const string errorMessage = "Error occurred in QuestManager.ResetObjectiveProgressAsync. Reason:";
            try
            {
                var questResult = await LoadAsync(avatarId, questId);
                if (questResult.IsError || questResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Quest not found. Reason: {questResult.Message}");
                    return result;
                }
                var quest = questResult.Result;
                if (quest.Objectives != null)
                {
                    foreach (var o in quest.Objectives)
                        o.ResetProgressDictionariesOnly();
                }
                quest.ResetQuestLevelProgressDictionariesOnly();
                if (quest.Status == QuestStatus.Completed)
                {
                    quest.Status = QuestStatus.InProgress;
                    quest.CompletedOn = DateTime.MinValue;
                    quest.CompletedBy = Guid.Empty;
                }
                if (quest.MetaData == null)
                    quest.MetaData = new Dictionary<string, object>();
                quest.MetaData["Status"] = quest.Status.ToString();
                var pct = ComputeQuestPercent(quest);
                quest.ProgressPercent = pct;
                quest.MetaData["ProgressPercent"] = pct.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (quest.Objectives != null)
                {
                    foreach (var obj in quest.Objectives)
                        obj.ProgressPercent = obj.IsCompleted ? 100 : ObjectiveApproximatePercent(obj);
                }
                var updateResult = await UpdateAsync(avatarId, quest);
                if (updateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to save. Reason: {updateResult.Message}");
                    return result;
                }
                result.Result = quest;
                result.Message = "Objective progress reset to 0%; requirement (Need*) dictionaries unchanged.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}");
            }
            return result;
        }

        private static int ComputeQuestPercent(Quest quest)
        {
            if (quest.Objectives == null || quest.Objectives.Count == 0)
                return quest.Status == QuestStatus.Completed ? 100 : 0;
            int sum = 0;
            foreach (var o in quest.Objectives)
                sum += o.IsCompleted ? 100 : ObjectiveApproximatePercent(o);
            return sum / quest.Objectives.Count;
        }

        /// <summary>Rough completion 0–99 for one objective from requirement vs progress dicts.</summary>
        private static int ObjectiveApproximatePercent(Objective o)
        {
            var scores = new List<int>();
            void AddPair(IDictionary<string, IList<string>> need, IDictionary<string, IList<string>> prog, string gameKey)
            {
                if (need == null || prog == null || !need.TryGetValue(gameKey, out var nlist) || nlist == null || nlist.Count == 0)
                    return;
                if (!int.TryParse(nlist[0], out var needN) || needN <= 0)
                    return;
                var cur = GetDictInt(prog, gameKey);
                scores.Add((int)System.Math.Min(99, 100 * cur / needN));
            }
            foreach (var kv in o.NeedToKillMonsters ?? new Dictionary<string, IList<string>>())
                AddPair(o.NeedToKillMonsters, o.MonstersKilled, kv.Key);
            // Per-type kill contribution: score each classname:count requirement independently
            foreach (var kv in o.NeedToKillMonstersByType ?? new Dictionary<string, IList<string>>())
            {
                var reqs = kv.Value;
                if (reqs == null) continue;
                var killedByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (o.MonstersKilledByType != null && o.MonstersKilledByType.TryGetValue(kv.Key, out var kList) && kList != null)
                {
                    foreach (var e in kList) { var p = e.Split(':'); if (p.Length == 2 && int.TryParse(p[1], out var k)) killedByType[p[0]] = k; }
                }
                foreach (var req in reqs)
                {
                    var rp = req.Split(':');
                    if (rp.Length != 2 || !int.TryParse(rp[1], out var needed) || needed <= 0) continue;
                    killedByType.TryGetValue(rp[0], out var got);
                    scores.Add((int)System.Math.Min(99, 100 * got / needed));
                }
            }
            foreach (var kv in o.NeedToEarnXP ?? new Dictionary<string, IList<string>>())
                AddPair(o.NeedToEarnXP, o.XPEarnt, kv.Key);
            foreach (var kv in o.NeedToCollectItems ?? new Dictionary<string, IList<string>>())
                AddPair(o.NeedToCollectItems, o.ItemsCollected, kv.Key);
            foreach (var kv in o.NeedToCollectArmor ?? new Dictionary<string, IList<string>>())
                AddPair(o.NeedToCollectArmor, o.ArmorCollected, kv.Key);
            foreach (var kv in o.NeedToCollectHealth ?? new Dictionary<string, IList<string>>())
                AddPair(o.NeedToCollectHealth, o.HealthCollected, kv.Key);
            foreach (var kv in o.NeedToCollectWeapons ?? new Dictionary<string, IList<string>>())
                AddPair(o.NeedToCollectWeapons, o.WeaponsCollected, kv.Key);
            foreach (var kv in o.NeedToCollectPowerups ?? new Dictionary<string, IList<string>>())
                AddPair(o.NeedToCollectPowerups, o.PowerupsCollected, kv.Key);
            foreach (var kv in o.NeedToCollectAmmo ?? new Dictionary<string, IList<string>>())
                AddPair(o.NeedToCollectAmmo, o.AmmoCollected, kv.Key);
            if (scores.Count == 0)
                return 0;
            return scores.Sum() / scores.Count;
        }

        /// <summary>Resolve which game key in a need dict matches the incoming <paramref name="gs"/> (ODOOM vs Doom, OQUAKE vs Quake).</summary>
        private static string? ResolveDictGameKey(IDictionary<string, IList<string>>? need, string gs)
        {
            if (need == null || need.Count == 0) return null;
            if (string.IsNullOrWhiteSpace(gs)) gs = "ODOOM";
            if (need.ContainsKey(gs)) return gs;
            foreach (var k in need.Keys)
            {
                if (GameKeysAliasForProgress(k, gs)) return k;
            }
            return null;
        }

        private static bool GameKeysAliasForProgress(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            static string Norm(string s) =>
                (s ?? "").Replace(" ", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
            var na = Norm(a);
            var nb = Norm(b);
            if (na.Equals(nb, StringComparison.OrdinalIgnoreCase)) return true;
            var aDoom = na.Equals("DOOM", StringComparison.OrdinalIgnoreCase) || na.Equals("ODOOM", StringComparison.OrdinalIgnoreCase);
            var bDoom = nb.Equals("DOOM", StringComparison.OrdinalIgnoreCase) || nb.Equals("ODOOM", StringComparison.OrdinalIgnoreCase);
            if (aDoom && bDoom) return true;
            var aQ = na.Equals("QUAKE", StringComparison.OrdinalIgnoreCase) || na.Equals("OQUAKE", StringComparison.OrdinalIgnoreCase);
            var bQ = nb.Equals("QUAKE", StringComparison.OrdinalIgnoreCase) || nb.Equals("OQUAKE", StringComparison.OrdinalIgnoreCase);
            return aQ && bQ;
        }

        private static void ApplyDeltaToObjective(Objective o, string gs, QuestProgressDelta d)
        {
            var mk = ResolveDictGameKey(o.NeedToKillMonsters, gs);
            if (d.MonstersKilledDelta != 0 && mk != null)
                AddDictInt(o.MonstersKilled, mk, d.MonstersKilledDelta);
            // Per-type kill: update MonstersKilledByType when MonsterKilledClassname is specified and required by this objective
            if (!string.IsNullOrWhiteSpace(d.MonsterKilledClassname))
            {
                var btKey = ResolveDictGameKey(o.NeedToKillMonstersByType, gs);
                if (btKey != null && o.NeedToKillMonstersByType.TryGetValue(btKey, out var reqs) && reqs != null)
                {
                    var classname = d.MonsterKilledClassname.Trim();
                    var isRequired = reqs.Any(r => { var p = r.Split(':'); return p.Length == 2 && string.Equals(p[0], classname, StringComparison.OrdinalIgnoreCase); });
                    if (isRequired)
                        AddOrIncrementTypedKill(o.MonstersKilledByType, btKey, classname);
                }
            }
            var xpk = ResolveDictGameKey(o.NeedToEarnXP, gs);
            if (d.XpEarnedDelta != 0 && xpk != null)
                AddDictInt(o.XPEarnt, xpk, d.XpEarnedDelta);
            var kk = ResolveDictGameKey(o.NeedToCollectKeys, gs);
            if (d.KeysCollectedDelta != 0 && kk != null)
                AddDictInt(o.KeysCollected, kk, d.KeysCollectedDelta);
            var ak = ResolveDictGameKey(o.NeedToCollectArmor, gs);
            if (d.ArmorCollectedDelta != 0 && ak != null)
                AddDictInt(o.ArmorCollected, ak, d.ArmorCollectedDelta);
            var hk = ResolveDictGameKey(o.NeedToCollectHealth, gs);
            if (d.HealthCollectedDelta != 0 && hk != null)
                AddDictInt(o.HealthCollected, hk, d.HealthCollectedDelta);
            var wk = ResolveDictGameKey(o.NeedToCollectWeapons, gs);
            if (d.WeaponsCollectedDelta != 0 && wk != null)
                AddDictInt(o.WeaponsCollected, wk, d.WeaponsCollectedDelta);
            var pk = ResolveDictGameKey(o.NeedToCollectPowerups, gs);
            if (d.PowerupsCollectedDelta != 0 && pk != null)
                AddDictInt(o.PowerupsCollected, pk, d.PowerupsCollectedDelta);
            var amk = ResolveDictGameKey(o.NeedToCollectAmmo, gs);
            if (d.AmmoCollectedDelta != 0 && amk != null)
                AddDictInt(o.AmmoCollected, amk, d.AmmoCollectedDelta);
            var itk = ResolveDictGameKey(o.NeedToCollectItems, gs);
            if (!string.IsNullOrWhiteSpace(d.ItemCollectedName) && itk != null)
            {
                var name = d.ItemCollectedName.Trim();
                var reqs = o.NeedToCollectItems![itk];
                var matched = reqs.Any(r => string.Equals(r, name, StringComparison.OrdinalIgnoreCase));
                if (matched || (reqs.Count > 0 && int.TryParse(reqs[0], out _)))
                    AddDictInt(o.ItemsCollected, itk, 1);
            }
            else if (d.GenericItemPickup != 0 && itk != null)
                AddDictInt(o.ItemsCollected, itk, d.GenericItemPickup);
            if (d.LevelTimeSeconds.HasValue)
            {
                SetDictInt(o.TimeTaken, gs, d.LevelTimeSeconds.Value);
                if (o.TimeStarted != null && !o.TimeStarted.ContainsKey(gs))
                    o.TimeStarted[gs] = new List<string> { DateTime.UtcNow.AddSeconds(-d.LevelTimeSeconds.Value).ToString("O") };
            }
        }

        /// <summary>True if this objective has at least one Need* row resolvable for <paramref name="gs"/> (avoids vacuous completion when all OkNeed branches are "no requirement").</summary>
        private static bool ObjectiveHasAnyRequirementForGame(Objective o, string gs)
        {
            if (string.IsNullOrWhiteSpace(gs)) gs = "ODOOM";
            if (o.NeedToKillMonsters != null && o.NeedToKillMonsters.Count > 0 && ResolveDictGameKey(o.NeedToKillMonsters, gs) != null) return true;
            if (o.NeedToKillMonstersByType != null && o.NeedToKillMonstersByType.Count > 0 && ResolveDictGameKey(o.NeedToKillMonstersByType, gs) != null) return true;
            if (o.NeedToEarnXP != null && o.NeedToEarnXP.Count > 0 && ResolveDictGameKey(o.NeedToEarnXP, gs) != null) return true;
            if (o.NeedToCollectKeys != null && o.NeedToCollectKeys.Count > 0 && ResolveDictGameKey(o.NeedToCollectKeys, gs) != null) return true;
            if (o.NeedToCollectArmor != null && o.NeedToCollectArmor.Count > 0 && ResolveDictGameKey(o.NeedToCollectArmor, gs) != null) return true;
            if (o.NeedToCollectHealth != null && o.NeedToCollectHealth.Count > 0 && ResolveDictGameKey(o.NeedToCollectHealth, gs) != null) return true;
            if (o.NeedToCollectWeapons != null && o.NeedToCollectWeapons.Count > 0 && ResolveDictGameKey(o.NeedToCollectWeapons, gs) != null) return true;
            if (o.NeedToCollectPowerups != null && o.NeedToCollectPowerups.Count > 0 && ResolveDictGameKey(o.NeedToCollectPowerups, gs) != null) return true;
            if (o.NeedToCollectAmmo != null && o.NeedToCollectAmmo.Count > 0 && ResolveDictGameKey(o.NeedToCollectAmmo, gs) != null) return true;
            if (o.NeedToCollectItems != null && o.NeedToCollectItems.Count > 0 && ResolveDictGameKey(o.NeedToCollectItems, gs) != null) return true;
            return false;
        }

        private static bool IsObjectiveRequirementsMet(Objective o, string gs)
        {
            if (!ObjectiveHasAnyRequirementForGame(o, gs))
                return false;
            bool OkNeed(IDictionary<string, IList<string>> need, IDictionary<string, IList<string>> prog)
            {
                if (need == null || need.Count == 0) return true;
                var key = ResolveDictGameKey(need, gs);
                /* Need dict exists but no row for this gameSource — not satisfied (was wrongly treated as met). */
                if (key == null) return false;
                if (!need.TryGetValue(key, out var nlist) || nlist == null || nlist.Count == 0) return true;
                if (!int.TryParse(nlist[0], out var needN) || needN <= 0) return true;
                return GetDictInt(prog, key) >= needN;
            }
            bool OkItems()
            {
                if (o.NeedToCollectItems == null) return true;
                var key = ResolveDictGameKey(o.NeedToCollectItems, gs);
                if (key == null) return false;
                if (!o.NeedToCollectItems.TryGetValue(key, out var items) || items == null || items.Count == 0)
                    return true;
                if (int.TryParse(items[0], out var needCount) && needCount > 0)
                    return GetDictInt(o.ItemsCollected, key) >= needCount;
                return GetDictInt(o.ItemsCollected, key) >= items.Count;
            }
            if (!OkNeed(o.NeedToKillMonsters, o.MonstersKilled)) return false;
            // Per-type kill check: every classname:count requirement must be met
            bool OkKillsByType()
            {
                if (o.NeedToKillMonstersByType == null || o.NeedToKillMonstersByType.Count == 0) return true;
                var key = ResolveDictGameKey(o.NeedToKillMonstersByType, gs);
                if (key == null) return false;
                if (!o.NeedToKillMonstersByType.TryGetValue(key, out var reqs) || reqs == null || reqs.Count == 0) return true;
                var killed = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (o.MonstersKilledByType != null && o.MonstersKilledByType.TryGetValue(key, out var kList) && kList != null)
                {
                    foreach (var e in kList) { var p = e.Split(':'); if (p.Length == 2 && int.TryParse(p[1], out var k)) killed[p[0]] = k; }
                }
                foreach (var req in reqs)
                {
                    var p = req.Split(':');
                    if (p.Length != 2 || !int.TryParse(p[1], out var needed) || needed <= 0) continue;
                    killed.TryGetValue(p[0], out var got);
                    if (got < needed) return false;
                }
                return true;
            }
            if (!OkKillsByType()) return false;
            if (!OkNeed(o.NeedToEarnXP, o.XPEarnt)) return false;
            if (!OkNeed(o.NeedToCollectKeys, o.KeysCollected)) return false;
            if (!OkNeed(o.NeedToCollectArmor, o.ArmorCollected)) return false;
            if (!OkNeed(o.NeedToCollectHealth, o.HealthCollected)) return false;
            if (!OkNeed(o.NeedToCollectWeapons, o.WeaponsCollected)) return false;
            if (!OkNeed(o.NeedToCollectPowerups, o.PowerupsCollected)) return false;
            if (!OkNeed(o.NeedToCollectAmmo, o.AmmoCollected)) return false;
            if (o.NeedToCollectItems != null && o.NeedToCollectItems.Count > 0 && ResolveDictGameKey(o.NeedToCollectItems, gs) != null && !OkItems()) return false;
            return true;
        }

        private static int GetDictInt(IDictionary<string, IList<string>> d, string key)
        {
            if (d == null || !d.TryGetValue(key, out var list) || list == null || list.Count == 0) return 0;
            return int.TryParse(list[0], out var n) ? n : 0;
        }

        private static void AddDictInt(IDictionary<string, IList<string>> d, string key, int delta)
        {
            if (d == null || delta == 0) return;
            if (!d.TryGetValue(key, out var list) || list == null)
            {
                d[key] = new List<string> { delta.ToString() };
                return;
            }
            var cur = int.TryParse(list[0], out var n) ? n : 0;
            list[0] = (cur + delta).ToString();
        }

        private static void SetDictInt(IDictionary<string, IList<string>> d, string key, int value)
        {
            if (d == null) return;
            d[key] = new List<string> { value.ToString() };
        }

        /// <summary>Increment the kill count for a specific monster classname in a MonstersKilledByType dictionary entry (format "classname:count").</summary>
        private static void AddOrIncrementTypedKill(IDictionary<string, IList<string>> dict, string gameKey, string classname)
        {
            if (dict == null) return;
            if (!dict.TryGetValue(gameKey, out var list) || list == null)
                dict[gameKey] = list = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i].Split(':');
                if (p.Length == 2 && string.Equals(p[0], classname, StringComparison.OrdinalIgnoreCase))
                {
                    var cur = int.TryParse(p[1], out var n) ? n : 0;
                    list[i] = $"{p[0]}:{cur + 1}";
                    return;
                }
            }
            list.Add($"{classname}:1");
        }

        /// <summary>
        /// Completes a quest for the specified avatar
        /// </summary>
        /// <param name="avatarId">The avatar completing the quest</param>
        /// <param name="questId">The quest to complete</param>
        /// <param name="completionNotes">Optional completion notes</param>
        /// <returns>Success status</returns>
    }
}
