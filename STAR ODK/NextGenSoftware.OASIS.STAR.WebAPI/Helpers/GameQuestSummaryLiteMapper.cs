using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

internal static class GameQuestSummaryLiteMapper
{
    public static GameQuestSummaryLite ToLite(Quest q)
    {
        var objectives = new List<GameQuestObjectiveLite>();
        if (q.Objectives != null)
        {
            foreach (var o in q.Objectives)
            {
                var progress = o.ProgressSummary ?? string.Empty;
                objectives.Add(new GameQuestObjectiveLite
                {
                    Id = o.Id.ToString(),
                    Title = o.Title ?? string.Empty,
                    Order = o.Order,
                    IsCompleted = o.IsCompleted,
                    ProgressPercent = o.ProgressPercent,
                    Description = o.Description ?? string.Empty,
                    ProgressSummary = progress,
                    GameSource = InferObjectiveGameSource(o),
                    Dictionaries = BuildObjectiveDictionariesLite(o)
                });
            }
        }

        return new GameQuestSummaryLite
        {
            Id = q.Id.ToString(),
            Name = q.Name ?? string.Empty,
            Description = q.Description ?? string.Empty,
            Status = q.Status.ToString(),
            Order = q.Order,
            ProgressPercent = q.ProgressPercent,
            GameSource = q.GameSource ?? string.Empty,
            ParentQuestId = q.ParentQuestId == Guid.Empty ? string.Empty : q.ParentQuestId.ToString(),
            ParentMissionId = q.ParentMissionId == Guid.Empty ? string.Empty : q.ParentMissionId.ToString(),
            RewardKarma = q.RewardKarma,
            RewardXP = q.RewardXP,
            Requirements = q.Requirements != null ? new List<string>(q.Requirements) : new List<string>(),
            PrerequisiteQuestIds = q.PrerequisiteQuestIds != null ? new List<string>(q.PrerequisiteQuestIds) : new List<string>(),
            CompletionNotes = q.CompletionNotes,
            Objectives = objectives
        };
    }

    /// <summary>Best-effort game id (ODOOM, OQUAKE, …) from requirement dictionaries.</summary>
    private static string InferObjectiveGameSource(Objective o)
    {
        static string FirstGameKey(IDictionary<string, IList<string>>? d)
        {
            if (d == null) return string.Empty;
            foreach (var kv in d)
            {
                if (kv.Value is { Count: > 0 } && !string.IsNullOrWhiteSpace(kv.Key))
                    return kv.Key.Trim();
            }
            return string.Empty;
        }

        foreach (var d in new[]
                 {
                     o.NeedToCollectItems, o.NeedToKillMonsters, o.NeedToCollectKeys, o.NeedToCollectHealth,
                     o.NeedToCollectArmor, o.NeedToCollectAmmo, o.NeedToCollectWeapons, o.NeedToCollectPowerups,
                     o.NeedToCompleteLevel, o.NeedToEarnXP, o.NeedToEarnKarma, o.NeedToCompleteInMins,
                     o.NeedToGoToGeoHotSpots, o.NeedToUseWeapons, o.NeedToUsePowerups, o.NeedToVisitLocations,
                     o.NeedToSurviveMins
                 })
        {
            var k = FirstGameKey(d);
            if (k.Length > 0) return k;
        }

        return string.Empty;
    }

    private static Dictionary<string, List<string>>? ToJsonDict(IDictionary<string, IList<string>>? src)
    {
        if (src == null || src.Count == 0) return null;
        var d = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in src)
        {
            if (kv.Value == null || kv.Value.Count == 0) continue;
            d[kv.Key] = kv.Value.Select(x => x?.ToString() ?? string.Empty).ToList();
        }
        return d.Count > 0 ? d : null;
    }

    private static GameQuestObjectiveDictionariesLite? BuildObjectiveDictionariesLite(Objective o)
    {
        var d = new GameQuestObjectiveDictionariesLite
        {
            NeedToCollectArmor = ToJsonDict(o.NeedToCollectArmor),
            NeedToCollectAmmo = ToJsonDict(o.NeedToCollectAmmo),
            NeedToCollectHealth = ToJsonDict(o.NeedToCollectHealth),
            NeedToCollectWeapons = ToJsonDict(o.NeedToCollectWeapons),
            NeedToCollectPowerups = ToJsonDict(o.NeedToCollectPowerups),
            NeedToCollectItems = ToJsonDict(o.NeedToCollectItems),
            NeedToCollectKeys = ToJsonDict(o.NeedToCollectKeys),
            NeedToKillMonsters = ToJsonDict(o.NeedToKillMonsters),
            NeedToCompleteInMins = ToJsonDict(o.NeedToCompleteInMins),
            NeedToEarnKarma = ToJsonDict(o.NeedToEarnKarma),
            NeedToEarnXP = ToJsonDict(o.NeedToEarnXP),
            NeedToGoToGeoHotSpots = ToJsonDict(o.NeedToGoToGeoHotSpots),
            NeedToCompleteLevel = ToJsonDict(o.NeedToCompleteLevel),
            NeedToUseWeapons = ToJsonDict(o.NeedToUseWeapons),
            NeedToUsePowerups = ToJsonDict(o.NeedToUsePowerups),
            NeedToVisitLocations = ToJsonDict(o.NeedToVisitLocations),
            NeedToSurviveMins = ToJsonDict(o.NeedToSurviveMins),
            ArmorCollected = ToJsonDict(o.ArmorCollected),
            AmmoCollected = ToJsonDict(o.AmmoCollected),
            HealthCollected = ToJsonDict(o.HealthCollected),
            WeaponsCollected = ToJsonDict(o.WeaponsCollected),
            PowerupsCollected = ToJsonDict(o.PowerupsCollected),
            ItemsCollected = ToJsonDict(o.ItemsCollected),
            KeysCollected = ToJsonDict(o.KeysCollected),
            MonstersKilled = ToJsonDict(o.MonstersKilled),
            TimeStarted = ToJsonDict(o.TimeStarted),
            TimeEnded = ToJsonDict(o.TimeEnded),
            TimeTaken = ToJsonDict(o.TimeTaken),
            KarmaEarnt = ToJsonDict(o.KarmaEarnt),
            XPEarnt = ToJsonDict(o.XPEarnt),
            GeoHotSpotsArrived = ToJsonDict(o.GeoHotSpotsArrived),
            LevelsCompleted = ToJsonDict(o.LevelsCompleted)
        };
        foreach (var p in typeof(GameQuestObjectiveDictionariesLite).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetValue(d) != null) return d;
        }
        return null;
    }
}
