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
}
