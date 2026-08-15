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
}
