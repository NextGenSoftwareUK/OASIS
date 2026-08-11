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
    private async Task<OASISResult<string>> EnsureAvatarIdAsync(CancellationToken cancellationToken)
    {
        lock (_stateLock)
        {
            if (!string.IsNullOrWhiteSpace(_avatarId))
                return Success(_avatarId!, StarApiResultCode.Success, "Avatar ID already available.");
        }

        if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
            return Fail<string>(missingWeb4, StarApiResultCode.InvalidParam);

        var response = await SendRawWithRetryAsync(HttpMethod.Get, $"{web4Base}{Web4GetLoggedInAvatarWithXpPath}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            return new OASISResult<string>
            {
                IsError = true,
                Message = response.Message,
                ErrorCode = response.ErrorCode,
                Exception = response.Exception
            };
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return Fail<string>(parseErrorMessage, parseErrorCode);

        var avatar = ParseAvatarInfo(resultElement);
        if (avatar is null || avatar.Id == Guid.Empty)
            return Fail<string>("Could not resolve current avatar ID.", StarApiResultCode.ApiError);

        lock (_stateLock)
            _avatarId = avatar.Id.ToString();

        return Success(_avatarId!, StarApiResultCode.Success, "Resolved current avatar ID.");
    }

    private static string BuildJson(Action<Utf8JsonWriter> writeAction)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writeAction(writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private bool ParseEnvelopeOrPayload(string? body, out JsonElement result, out StarApiResultCode errorCode, out string errorMessage)
    {
        result = default;
        errorCode = StarApiResultCode.ApiError;
        errorMessage = "Response body was empty.";

        if (string.IsNullOrWhiteSpace(body))
        {
            result = default;
            errorCode = StarApiResultCode.Success;
            errorMessage = string.Empty;
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(body, DeepJsonDocumentOptions);
            var current = doc.RootElement.Clone();
            var depth = 0;

            while (depth < 4 && current.ValueKind == JsonValueKind.Object)
            {
                depth++;

                var isError = GetBoolProperty(current, "IsError");
                var message = GetStringProperty(current, "Message");
                var codeText = GetStringProperty(current, "ErrorCode");
                var parsedCode = ParseCode(codeText, StarApiResultCode.ApiError);

                if (isError)
                {
                    errorCode = parsedCode;
                    errorMessage = string.IsNullOrWhiteSpace(message) ? "API returned an error." : message!;
                    result = current.Clone();
                    return false;
                }

                if (TryGetProperty(current, "Result", out var nested))
                {
                    if (nested.ValueKind == JsonValueKind.Object &&
                        (TryGetProperty(nested, "Result", out _) || TryGetProperty(nested, "IsError", out _)))
                    {
                        current = nested.Clone();
                        continue;
                    }

                    /* OASISHttpResponseMessage shape: outer unwraps to an OASISResult object with isError/message but no further Result to descend into. */
                    if (nested.ValueKind == JsonValueKind.Object && GetBoolProperty(nested, "IsError"))
                    {
                        var msg = GetStringProperty(nested, "Message");
                        errorCode = ParseCode(GetStringProperty(nested, "ErrorCode"), StarApiResultCode.ApiError);
                        errorMessage = string.IsNullOrWhiteSpace(msg) ? "API returned an error." : msg!;
                        result = nested.Clone();
                        return false;
                    }

                    result = nested.Clone();
                    errorCode = StarApiResultCode.Success;
                    errorMessage = string.Empty;
                    return true;
                }

                break;
            }

            result = current.Clone();
            errorCode = StarApiResultCode.Success;
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorCode = StarApiResultCode.ApiError;
            errorMessage = $"Invalid JSON response: {ex.Message}";
            return false;
        }
    }

    private List<StarItem> ParseInventoryItems(JsonElement element)
    {
        var items = new List<StarItem>();
        var arraysToMerge = new List<JsonElement>();

        if (element.ValueKind == JsonValueKind.Array)
            arraysToMerge.Add(element);
        else if (element.ValueKind == JsonValueKind.Object)
        {
            // API may return payload as Result/result (array or object with array inside). Merge all arrays so ammo/armor/items appear.
            var arrayPropertyNames = new[] { "Result", "Results", "Items", "Inventory", "Data", "Holons", "InventoryItems", "value" };
            foreach (var name in arrayPropertyNames)
            {
                if (TryGetProperty(element, name, out var prop) && prop.ValueKind == JsonValueKind.Array)
                    arraysToMerge.Add(prop);
            }
        }

        foreach (var arrayElement in arraysToMerge)
        {
            foreach (var itemElement in arrayElement.EnumerateArray())
            {
                var item = ParseInventoryItemResponse(itemElement);
                if (item is null)
                    continue;

                var nftId = !string.IsNullOrWhiteSpace(item.NftId) ? item.NftId
                    : ExtractMeta(item.MetaData, "NFTId", string.Empty) ?? ExtractMeta(item.MetaData, "OASISNFTId", string.Empty) ?? string.Empty;
                items.Add(new StarItem
                {
                    Id = item.Id,
                    Name = item.Name ?? string.Empty,
                    Description = item.Description ?? string.Empty,
                    GameSource = !string.IsNullOrWhiteSpace(item.GameSource) ? item.GameSource : "n/a",
                    ItemType = !string.IsNullOrWhiteSpace(item.ItemType) ? item.ItemType : "Miscellaneous",
                    NftId = nftId,
                    Quantity = item.Quantity
                });
            }
        }

        return items;
    }

    /// <summary>WEB4 inventory holons often omit GameSource; add-item stores <c>"{desc} | Source: ODOOM"</c> in Description.</summary>
    private static string? TryExtractGameSourceFromDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        var span = description.AsSpan();
        ReadOnlySpan<char> key = "Source:";
        var idx = span.LastIndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var tail = span[(idx + key.Length)..].TrimStart();
        if (tail.Length == 0) return null;
        var pipe = tail.IndexOf('|');
        if (pipe >= 0) tail = tail[..pipe].TrimEnd();
        return tail.Length > 0 ? tail.ToString() : null;
    }

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
}
