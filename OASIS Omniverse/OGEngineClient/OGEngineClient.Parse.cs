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
    private InventoryItemResponse? ParseInventoryItemResponse(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        // API may return item wrapped in Holon/Item/Data (e.g. new items). Unwrap so we parse same shape as POST response.
        if (TryGetProperty(element, "Holon", out var inner) && inner.ValueKind == JsonValueKind.Object)
            element = inner;
        else if (TryGetProperty(element, "Item", out inner) && inner.ValueKind == JsonValueKind.Object)
            element = inner;
        else if (TryGetProperty(element, "Data", out inner) && inner.ValueKind == JsonValueKind.Object)
            element = inner;

        var idValue = GetStringProperty(element, "Id") ?? GetStringProperty(element, "id");
        Guid.TryParse(idValue, out var parsedGuid);

        Dictionary<string, JsonElement>? metadata = null;
        if (TryGetProperty(element, "MetaData", out var metaElement) && metaElement.ValueKind == JsonValueKind.Object)
            metadata = CloneMetaData(metaElement);
        else if (TryGetProperty(element, "Metadata", out metaElement) && metaElement.ValueKind == JsonValueKind.Object)
            metadata = CloneMetaData(metaElement);

        var name = GetStringProperty(element, "Name") ?? GetStringProperty(element, "name");
        var description = GetStringProperty(element, "Description") ?? GetStringProperty(element, "description");
        var gameSource = GetStringProperty(element, "GameSource") ?? GetStringProperty(element, "gameSource");
        var itemType = GetStringProperty(element, "ItemType") ?? GetStringProperty(element, "itemType");
        int quantity = 1;
        if (TryGetProperty(element, "Quantity", out var qtyEl))
        {
            if (qtyEl.ValueKind == JsonValueKind.Number && qtyEl.TryGetInt32(out var q))
                quantity = q;
            else if (qtyEl.ValueKind == JsonValueKind.String && int.TryParse(qtyEl.GetString(), out var qs))
                quantity = qs;
        }
        if (metadata != null)
        {
            if (string.IsNullOrWhiteSpace(name)) name = ExtractMeta(metadata, "Name", string.Empty) ?? ExtractMeta(metadata, "name", string.Empty) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemType)) itemType = ExtractMeta(metadata, "ItemType", string.Empty) ?? ExtractMeta(metadata, "itemType", string.Empty) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gameSource)) gameSource = ExtractMeta(metadata, "GameSource", string.Empty) ?? ExtractMeta(metadata, "gameSource", string.Empty) ?? string.Empty;
            if (quantity <= 1)
            {
                var qtyStr = ExtractMeta(metadata, "Quantity", string.Empty) ?? ExtractMeta(metadata, "quantity", string.Empty);
                if (!string.IsNullOrWhiteSpace(qtyStr) && int.TryParse(qtyStr, out var qm) && qm > 0)
                    quantity = qm;
            }
        }
        if (quantity < 1) quantity = 1;
        if (string.IsNullOrWhiteSpace(name) && parsedGuid == Guid.Empty)
            return null;

        if (string.IsNullOrWhiteSpace(gameSource))
        {
            var extractedGs = TryExtractGameSourceFromDescription(description);
            if (!string.IsNullOrWhiteSpace(extractedGs))
                gameSource = extractedGs;
        }

        /* NftId: from root (API may use PascalCase or camelCase) or from MetaData so [NFT] prefix persists after reload / in Quake. */
        var nftId = GetStringProperty(element, "NftId") ?? GetStringProperty(element, "nftId") ?? GetStringProperty(element, "NFTId") ?? GetStringProperty(element, "OASISNFTId")
            ?? (metadata != null ? ExtractMeta(metadata, "NFTId", string.Empty) : null)
            ?? (metadata != null ? ExtractMeta(metadata, "OASISNFTId", string.Empty) : null);
        if (string.IsNullOrWhiteSpace(nftId)) nftId = null;

        return new InventoryItemResponse
        {
            Id = parsedGuid,
            Name = name,
            Description = description,
            GameSource = gameSource,
            ItemType = itemType,
            MetaData = metadata,
            Quantity = quantity,
            NftId = nftId
        };
    }

    private static Dictionary<string, JsonElement> CloneMetaData(JsonElement metaElement)
    {
        var metadata = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in metaElement.EnumerateObject())
            metadata[property.Name] = property.Value.Clone();
        return metadata;
    }

    private static AvatarAuthResponse? ParseAvatarAuthResponse(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var idText = GetStringProperty(element, "Id")
            ?? GetStringProperty(element, "AvatarId")
            ?? FindStringRecursive(element, "Id")
            ?? FindStringRecursive(element, "AvatarId");
        Guid.TryParse(idText, out var id);
        var jwt = GetStringProperty(element, "JwtToken") ?? FindStringRecursive(element, "JwtToken")
            ?? GetStringProperty(element, "Token") ?? FindStringRecursive(element, "Token")
            ?? GetStringProperty(element, "accessToken") ?? FindStringRecursive(element, "accessToken")
            ?? GetStringProperty(element, "access_token") ?? FindStringRecursive(element, "access_token")
            ?? GetStringProperty(element, "jwt") ?? FindStringRecursive(element, "jwt");
        var refresh = GetStringProperty(element, "RefreshToken") ?? FindStringRecursive(element, "RefreshToken");

        if (id != Guid.Empty || !string.IsNullOrWhiteSpace(jwt) || !string.IsNullOrWhiteSpace(refresh))
        {
            return new AvatarAuthResponse
            {
                Id = id,
                JwtToken = jwt,
                RefreshToken = refresh
            };
        }

        if (TryGetProperty(element, "Result", out var nested) && nested.ValueKind == JsonValueKind.Object)
            return ParseAvatarAuthResponse(nested);

        return new AvatarAuthResponse
        {
            Id = id,
            JwtToken = jwt,
            RefreshToken = refresh
        };
    }

    private static string? FindStringRecursive(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                        return property.Value.GetString();

                    var nestedDirect = FindStringRecursive(property.Value, propertyName);
                    if (!string.IsNullOrWhiteSpace(nestedDirect))
                        return nestedDirect;
                }
                else
                {
                    var nested = FindStringRecursive(property.Value, propertyName);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindStringRecursive(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return null;
    }

    private static AvatarInfo? ParseAvatarInfo(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        Guid.TryParse(GetStringProperty(element, "Id"), out var id);
        return new AvatarInfo { Id = id };
    }

    private static string? ParseIdAsString(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return GetStringProperty(element, "Id")
                ?? GetStringProperty(element, "OASISNFTId")
                ?? GetStringProperty(element, "STARNETHolonId")
                ?? GetStringProperty(element, "Hash");
        }

        if (element.ValueKind == JsonValueKind.String)
            return element.GetString();

        return null;
    }

    private static bool TryExtractTopLevelResultId(string? json, out string? id)
    {
        id = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            if (!TryGetProperty(doc.RootElement, "Result", out var resultElement) &&
                !TryGetProperty(doc.RootElement, "result", out resultElement))
            {
                return false;
            }

            var parsedId = ParseIdAsString(resultElement);
            if (string.IsNullOrWhiteSpace(parsedId))
                return false;

            id = parsedId;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Guid ExtractAvatarIdFromJwt(string? jwtToken)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
            return Guid.Empty;

        var parts = jwtToken.Split('.');
        if (parts.Length < 2)
            return Guid.Empty;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return Guid.Empty;

            var id = GetStringProperty(doc.RootElement, "id") ?? GetStringProperty(doc.RootElement, "Id");
            return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
        }
        catch
        {
            return Guid.Empty;
        }
    }

    private string ExtractMeta(Dictionary<string, JsonElement>? metadata, string key, string fallback)
    {
        if (metadata is not null && metadata.TryGetValue(key, out var value))
        {
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? fallback;

            return value.ToString();
        }

        return fallback;
    }

    private static string? GetStringProperty(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var prop))
            return null;

        return prop.ValueKind switch
        {
            JsonValueKind.String => prop.GetString(),
            JsonValueKind.Number => prop.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => prop.GetRawText()
        };
    }

    /// <summary>Get a list of strings from element, e.g. element.MetaData.PrerequisiteQuestIds (array of string).</summary>
    private static List<string> GetStringListFromElement(JsonElement element, string parentKey, string arrayKey)
    {
        var list = new List<string>();
        if (!TryGetProperty(element, parentKey, out var parent) || parent.ValueKind != JsonValueKind.Object)
            return list;
        if (!TryGetProperty(parent, arrayKey, out var arr))
            return list;
        if (arr.ValueKind != JsonValueKind.Array)
            return list;
        foreach (var item in arr.EnumerateArray())
        {
            var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText()?.Trim('"');
            if (!string.IsNullOrEmpty(s))
                list.Add(s);
        }
        return list;
    }

    /// <summary>Log once per session which JSON key supplied objectives (objectives vs children). File-only so we can see why "objectives" is sometimes empty in the API response.</summary>
    private static void LogObjectivesSourceOnce(string path, int count)
    {
        if (string.IsNullOrEmpty(path) || count <= 0) return;
        try
        {
            var key = $"{path}:{count}";
            if (!_objectivesSourceLogged.Add(key)) return;
            var expected = path.IndexOf("objectives", StringComparison.OrdinalIgnoreCase) >= 0
                ? " (backend is serializing Quest.Objectives correctly)"
                : " (API sent empty 'objectives'; data came from 'children' – backend PromoteQuestMetaDataToProperties or serialization may not be populating objectives)";
            OGEngineExports.StarApiLogFileOnly($"[Quests] Objectives source: path={path} count={count}{expected}");
        }
        catch { /* ignore */ }
    }
    private static readonly HashSet<string> _objectivesSourceLogged = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Get objectives from a quest element. Path in API response: Result[i].objectives or Result[i].children (each quest in the array).
    /// We try: Objectives, objectives, QuestObjectives, questObjectives, Children, children (root then MetaData/MapMetaData) and use the first that yields a non-empty list.
    /// Backend (QuestManager.PromoteQuestMetaDataToProperties) should populate Quest.Objectives from MetaData so "objectives" is in the JSON; if the API sends empty "objectives" and data in "children", we use children.</summary>
    private static List<StarQuestObjective> GetObjectivesFromQuestElement(JsonElement questElement)
    {
        if (questElement.ValueKind != JsonValueKind.Object) return new List<StarQuestObjective>();

        /* Try each known key and use the first that yields a non-empty list. If API returns both "objectives": [] and "children": [...], we must not stop at the empty objectives. */
        static bool TryKnownKeys(JsonElement parent, out List<StarQuestObjective> list, out string? usedKey)
        {
            list = new List<StarQuestObjective>();
            usedKey = null;
            var keys = new[] { "Objectives", "objectives", "QuestObjectives", "questObjectives", "Children", "children" };
            foreach (var key in keys)
            {
                if (!TryGetProperty(parent, key, out var el)) continue;
                var parsed = ParseObjectivesFromElement(el);
                if (parsed.Count > 0)
                {
                    list = parsed;
                    usedKey = key;
                    return true;
                }
            }
            return false;
        }

        if (TryKnownKeys(questElement, out var fromRoot, out var keyUsed))
        {
            LogObjectivesSourceOnce(keyUsed, fromRoot.Count);
            return fromRoot;
        }
        if ((TryGetProperty(questElement, "MetaData", out var meta) || TryGetProperty(questElement, "metaData", out meta)) && meta.ValueKind == JsonValueKind.Object)
        {
            if (TryKnownKeys(meta, out var fromMeta, out keyUsed))
            {
                LogObjectivesSourceOnce("MetaData." + keyUsed, fromMeta.Count);
                return fromMeta;
            }
            if ((TryGetProperty(meta, "MapMetaData", out var mapMeta) || TryGetProperty(meta, "mapMetaData", out mapMeta)) && mapMeta.ValueKind == JsonValueKind.Object)
                if (TryKnownKeys(mapMeta, out var fromMap, out keyUsed))
                {
                    LogObjectivesSourceOnce("MetaData.MapMetaData." + keyUsed, fromMap.Count);
                    return fromMap;
                }
        }

        /* Safe fallback: only keys that contain "objective" (case-insensitive), so we never bind SubQuests/PrerequisiteQuestIds. Handles provider/API key variants. */
        static List<StarQuestObjective> TryKeysContainingObjective(JsonElement parent)
        {
            foreach (var prop in parent.EnumerateObject())
            {
                if (!prop.Name.Contains("objective", StringComparison.OrdinalIgnoreCase)) continue;
                var list = ParseObjectivesFromElement(prop.Value);
                if (list.Count > 0) return list;
            }
            return new List<StarQuestObjective>();
        }
        var fromScan = TryKeysContainingObjective(questElement);
        if (fromScan.Count > 0) return fromScan;
        if ((TryGetProperty(questElement, "MetaData", out var meta2) || TryGetProperty(questElement, "metaData", out meta2)) && meta2.ValueKind == JsonValueKind.Object)
        {
            fromScan = TryKeysContainingObjective(meta2);
            if (fromScan.Count > 0) return fromScan;
            if ((TryGetProperty(meta2, "MapMetaData", out var mapMeta2) || TryGetProperty(meta2, "mapMetaData", out mapMeta2)) && mapMeta2.ValueKind == JsonValueKind.Object)
            {
                fromScan = TryKeysContainingObjective(mapMeta2);
                if (fromScan.Count > 0) return fromScan;
            }
        }

        return new List<StarQuestObjective>();
    }

    /// <summary>Parse a single game-keyed dictionary from JSON. Values may be arrays of strings (preferred), a single string/number, or the whole property may be a JSON string (ONODE <see cref="CustomOASISPropertyAttribute.StoreAsJsonString"/>).</summary>
    private static Dictionary<string, List<string>> ParseStringListDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (element.ValueKind == JsonValueKind.String)
        {
            var raw = element.GetString();
            if (string.IsNullOrWhiteSpace(raw)) return dict;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                return ParseStringListDictionary(doc.RootElement);
            }
            catch
            {
                return dict;
            }
        }
        if (element.ValueKind != JsonValueKind.Object)
            return dict;
        foreach (var prop in element.EnumerateObject())
        {
            var list = new List<string>();
            var v = prop.Value;
            if (v.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in v.EnumerateArray())
                {
                    var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText()?.Trim('"');
                    if (!string.IsNullOrEmpty(s)) list.Add(s!);
                }
            }
            else if (v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString();
                if (!string.IsNullOrEmpty(s)) list.Add(s);
            }
            else if (v.ValueKind == JsonValueKind.Number)
            {
                list.Add(v.GetRawText());
            }
            else if (v.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                list.Add(v.GetBoolean() ? "1" : "0");
            }
            if (list.Count > 0) dict[prop.Name] = list;
        }
        return dict;
    }

    /// <summary>Parse Objective requirement/progress dictionaries from a JSON object (backend Objective / IQuestObjectiveDictionaries). Tries root first, then common nested wrappers.</summary>
    private static StarQuestObjectiveDictionaries? ParseObjectiveDictionaries(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        var direct = ParseObjectiveDictionariesBody(element);
        if (direct != null) return direct;
        foreach (var wrap in new[] { "Dictionaries", "ObjectiveDictionaries", "QuestObjectiveDictionaries", "QuestObjectiveDictionary", "objectiveDictionaries", "questObjectiveDictionaries", "MetaData", "metaData" })
        {
            if (!TryGetProperty(element, wrap, out var nested) || nested.ValueKind != JsonValueKind.Object)
                continue;
            var inner = ParseObjectiveDictionariesBody(nested);
            if (inner != null) return inner;
            if ((TryGetProperty(nested, "MapMetaData", out var mapMeta) || TryGetProperty(nested, "mapMetaData", out mapMeta)) && mapMeta.ValueKind == JsonValueKind.Object)
            {
                inner = ParseObjectiveDictionariesBody(mapMeta);
                if (inner != null) return inner;
            }
        }
        return null;
    }

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
