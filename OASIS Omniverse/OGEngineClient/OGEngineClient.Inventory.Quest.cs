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

    public async Task<OASISResult<StarQuestInfo?>> CreateCrossGameQuestAsync(string questName, string description, List<StarQuestObjective> objectives, string? questLinkedGeoHotSpotId = null, string? questExternalHandoffUri = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarQuestInfo?>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questName) || string.IsNullOrWhiteSpace(description) || objectives is null || objectives.Count == 0)
            return FailAndCallback<StarQuestInfo?>("Quest name, description and at least one objective are required.", StarApiResultCode.InvalidParam);
        foreach (var o in objectives)
        {
            if (string.IsNullOrWhiteSpace(o.Title) || string.IsNullOrWhiteSpace(o.Description))
                return FailAndCallback<StarQuestInfo?>("Each objective requires Title and Description.", StarApiResultCode.InvalidParam);
            if (!ObjectiveHasAuthoringRequirements(o))
                return FailAndCallback<StarQuestInfo?>("Each objective requires at least one Need* dictionary definition, a valid LinkedGeoHotSpotId, or ExternalHandoffUri.", StarApiResultCode.InvalidParam);
        }

        var avatarIdResult = await EnsureAvatarIdAsync(cancellationToken).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
            return FailAndCallback<StarQuestInfo?>(avatarIdResult.Message ?? "Could not resolve avatar ID.", ParseCode(avatarIdResult.ErrorCode, StarApiResultCode.ApiError), avatarIdResult.Exception);

        var games = objectives
            .Select(o => string.IsNullOrWhiteSpace(o.GameSource) ? "Unknown" : o.GameSource)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Name", questName);
            writer.WriteString("Description", description);
            writer.WriteNumber("HolonSubType", 8); /* HolonType.Quest */
            writer.WriteString("SourceFolderPath", string.Empty);
            if (!string.IsNullOrWhiteSpace(questLinkedGeoHotSpotId) && Guid.TryParse(questLinkedGeoHotSpotId.Trim(), out var questGh))
                writer.WriteString("LinkedGeoHotSpotId", questGh.ToString("D"));
            if (!string.IsNullOrWhiteSpace(questExternalHandoffUri))
                writer.WriteString("ExternalHandoffUri", questExternalHandoffUri.Trim());
            writer.WritePropertyName("CreateOptions");
            writer.WriteNullValue();
            writer.WritePropertyName("MetaData");
            writer.WriteStartObject();
            writer.WriteBoolean("CrossGameQuest", true);
            writer.WriteString("QuestType", "CrossGame");
            writer.WritePropertyName("Games");
            writer.WriteStartArray();
            foreach (var game in games)
                writer.WriteStringValue(game);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WritePropertyName("Objectives");
            writer.WriteStartArray();
            for (var i = 0; i < objectives.Count; i++)
            {
                var o = objectives[i];
                writer.WriteStartObject();
                writer.WriteString("Title", o.Title ?? string.Empty);
                writer.WriteString("Description", o.Description ?? string.Empty);
                writer.WriteString("GameSource", o.GameSource ?? string.Empty);
                writer.WriteNumber("Order", o.Order >= 0 ? o.Order : i);
                writer.WriteBoolean("IsCompleted", o.IsCompleted);
                if (o.CompletedAt.HasValue) writer.WriteString("CompletedAt", o.CompletedAt.Value.ToString("O"));
                if (!string.IsNullOrEmpty(o.CompletedBy)) writer.WriteString("CompletedBy", o.CompletedBy);
                if (!string.IsNullOrWhiteSpace(o.LinkedGeoHotSpotId) && Guid.TryParse(o.LinkedGeoHotSpotId.Trim(), out var objGh))
                    writer.WriteString("LinkedGeoHotSpotId", objGh.ToString("D"));
                if (!string.IsNullOrWhiteSpace(o.ExternalHandoffUri))
                    writer.WriteString("ExternalHandoffUri", o.ExternalHandoffUri.Trim());
                if (o.Dictionaries != null)
                {
                    writer.WritePropertyName("Dictionaries");
                    writer.WriteStartObject();
                    WriteObjectiveDictionaries(writer, o.Dictionaries);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/create", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarQuestInfo?>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        LogQuestParseChunkedFileOnly("[Quest][Parse] source=POST.api.quests/create full HTTP body", response.Result);
        StarQuestInfo? created = null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (parseResult && resultElement.ValueKind == JsonValueKind.Object)
        {
            LogQuestJsonShapeFileOnly("[Quest][Parse] source=POST.api.quests/create envelope", resultElement);
            created = ParseSingleQuestInfo(resultElement);
            LogParsedSingleQuestModelAudit("POST.api.quests/create", created);
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(created, StarApiResultCode.Success, "Cross-game quest created successfully.");
    }

    private static bool HasAtLeastOneNeedDefinition(StarQuestObjectiveDictionaries? dictionaries)
    {
        if (dictionaries == null) return false;
        return dictionaries.NeedToCollectArmor?.Count > 0 ||
               dictionaries.NeedToCollectAmmo?.Count > 0 ||
               dictionaries.NeedToCollectHealth?.Count > 0 ||
               dictionaries.NeedToCollectWeapons?.Count > 0 ||
               dictionaries.NeedToCollectPowerups?.Count > 0 ||
               dictionaries.NeedToCollectItems?.Count > 0 ||
               dictionaries.NeedToCollectKeys?.Count > 0 ||
               dictionaries.NeedToKillMonsters?.Count > 0 ||
               dictionaries.NeedToCompleteInMins?.Count > 0 ||
               dictionaries.NeedToEarnKarma?.Count > 0 ||
               dictionaries.NeedToEarnXP?.Count > 0 ||
               dictionaries.NeedToGoToGeoHotSpots?.Count > 0 ||
               dictionaries.NeedToCompleteLevel?.Count > 0 ||
               dictionaries.NeedToUseWeapons?.Count > 0 ||
               dictionaries.NeedToUsePowerups?.Count > 0 ||
               dictionaries.NeedToVisitLocations?.Count > 0 ||
               dictionaries.NeedToSurviveMins?.Count > 0;
    }

    /// <summary>True when an objective is valid for create/add: at least one Need* row, a parseable linked GeoHotSpot id, or a non-empty handoff URI (matches STAR WebAPI rules).</summary>
    private static bool ObjectiveHasAuthoringRequirements(StarQuestObjective? o)
    {
        if (o == null) return false;
        if (HasAtLeastOneNeedDefinition(o.Dictionaries)) return true;
        if (!string.IsNullOrWhiteSpace(o.LinkedGeoHotSpotId) && Guid.TryParse(o.LinkedGeoHotSpotId.Trim(), out _)) return true;
        if (!string.IsNullOrWhiteSpace(o.ExternalHandoffUri)) return true;
        return false;
    }

    /// <summary>Run create-cross-game-quest on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarQuestInfo?>> QueueCreateCrossGameQuestAsync(string questName, string description, List<StarQuestObjective> objectives, string? questLinkedGeoHotSpotId = null, string? questExternalHandoffUri = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => CreateCrossGameQuestAsync(questName, description, objectives, questLinkedGeoHotSpotId, questExternalHandoffUri, ct), cancellationToken);

    /// <summary>Adds an objective to an existing quest (Title, Description, explicit Dictionaries with at least one Need*).</summary>
    public async Task<OASISResult<StarQuestInfo?>> AddQuestObjectiveAsync(string questId, string title, string description, string? gameSource = null, int order = -1, StarQuestObjectiveDictionaries? dictionaries = null, string? linkedGeoHotSpotId = null, string? externalHandoffUri = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarQuestInfo?>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId))
            return FailAndCallback<StarQuestInfo?>("Quest ID is required.", StarApiResultCode.InvalidParam);

        if (string.IsNullOrWhiteSpace(title))
            return FailAndCallback<StarQuestInfo?>("Objective title is required.", StarApiResultCode.InvalidParam);

        if (string.IsNullOrWhiteSpace(description))
            return FailAndCallback<StarQuestInfo?>("Description is required.", StarApiResultCode.InvalidParam);

        var probe = new StarQuestObjective { Dictionaries = dictionaries, LinkedGeoHotSpotId = linkedGeoHotSpotId, ExternalHandoffUri = externalHandoffUri };
        if (!ObjectiveHasAuthoringRequirements(probe))
            return FailAndCallback<StarQuestInfo?>("At least one Need* dictionary definition, a valid LinkedGeoHotSpotId, or ExternalHandoffUri is required.", StarApiResultCode.InvalidParam);

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Title", title);
            writer.WriteString("Description", description);
            writer.WriteString("GameSource", gameSource ?? string.Empty);
            writer.WriteNumber("Order", order);
            if (!string.IsNullOrWhiteSpace(linkedGeoHotSpotId) && Guid.TryParse(linkedGeoHotSpotId.Trim(), out var gh))
                writer.WriteString("LinkedGeoHotSpotId", gh.ToString("D"));
            if (!string.IsNullOrWhiteSpace(externalHandoffUri))
                writer.WriteString("ExternalHandoffUri", externalHandoffUri.Trim());
            if (dictionaries != null)
            {
                writer.WritePropertyName("Dictionaries");
                writer.WriteStartObject();
                WriteObjectiveDictionaries(writer, dictionaries);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/{questId}/objectives", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarQuestInfo?>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var objSrc = $"POST.api.quests/{questId.Trim()}/objectives";
        LogQuestParseChunkedFileOnly($"[Quest][Parse] source={objSrc} full HTTP body", response.Result);
        StarQuestInfo? created = null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (parseResult && resultElement.ValueKind == JsonValueKind.Object)
        {
            LogQuestJsonShapeFileOnly($"[Quest][Parse] source={objSrc} envelope", resultElement);
            created = ParseSingleQuestInfo(resultElement);
            LogParsedSingleQuestModelAudit(objSrc, created);
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(created, StarApiResultCode.Success, "Quest objective added successfully.");
    }

    /// <summary>Run add-quest-objective on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarQuestInfo?>> QueueAddQuestObjectiveAsync(string questId, string title, string description, string? gameSource = null, int order = -1, StarQuestObjectiveDictionaries? dictionaries = null, string? linkedGeoHotSpotId = null, string? externalHandoffUri = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => AddQuestObjectiveAsync(questId, title, description, gameSource, order, dictionaries, linkedGeoHotSpotId, externalHandoffUri, ct), cancellationToken);

    /// <summary>Loads a GeoHotSpot by id from STAR WebAPI (<c>GET /api/GeoHotSpots/{id}</c>). Deserializes <c>audioData</c>/<c>videoData</c> when the API returns base64-encoded bytes.</summary>
    public async Task<OASISResult<StarGeoHotSpotDetails?>> GetGeoHotSpotAsync(string geoHotSpotId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarGeoHotSpotDetails?>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(geoHotSpotId) || !Guid.TryParse(geoHotSpotId.Trim(), out var ghId))
            return FailAndCallback<StarGeoHotSpotDetails?>("A valid GeoHotSpot id (GUID) is required.", StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/GeoHotSpots/{ghId:D}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarGeoHotSpotDetails?>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<StarGeoHotSpotDetails?>(parseErrorMessage, parseErrorCode);

        if (resultElement.ValueKind != JsonValueKind.Object)
            return FailAndCallback<StarGeoHotSpotDetails?>("GeoHotSpot response was not a JSON object.", StarApiResultCode.ApiError);

        StarGeoHotSpotDetails? details;
        try
        {
            var serializerOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, MaxDepth = 1024 };
            details = JsonSerializer.Deserialize<StarGeoHotSpotDetails>(resultElement.GetRawText(), serializerOpts);
        }
        catch (Exception ex)
        {
            return FailAndCallback<StarGeoHotSpotDetails?>($"Could not parse GeoHotSpot JSON: {ex.Message}", StarApiResultCode.ApiError, ex);
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(details, StarApiResultCode.Success, "GeoHotSpot loaded.");
    }

    /// <summary>Run <see cref="GetGeoHotSpotAsync"/> on the background worker.</summary>
    public Task<OASISResult<StarGeoHotSpotDetails?>> QueueGetGeoHotSpotAsync(string geoHotSpotId, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => GetGeoHotSpotAsync(geoHotSpotId, ct), cancellationToken);

    /// <summary>Removes an objective from a quest.</summary>
    public async Task<OASISResult<bool>> RemoveQuestObjectiveAsync(string questId, string objectiveId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId))
            return FailAndCallback<bool>("Quest ID and objective ID are required.", StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Delete, $"{_baseApiUrl}/api/quests/{questId}/objectives/{objectiveId}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Quest objective removed successfully.");
    }

    /// <summary>Run remove-quest-objective on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueRemoveQuestObjectiveAsync(string questId, string objectiveId, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => RemoveQuestObjectiveAsync(questId, objectiveId, ct), cancellationToken);

    /// <summary>Adds a sub-quest (full child quest) to an existing quest. Use for nested quests; use AddQuestObjectiveAsync for checklist objectives (Quest.Objectives).</summary>
    public async Task<OASISResult<StarQuestInfo?>> AddSubQuestAsync(string questId, string description, string? name = null, string? gameSource = null, int order = -1, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarQuestInfo?>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId))
            return FailAndCallback<StarQuestInfo?>("Quest ID is required.", StarApiResultCode.InvalidParam);

        if (string.IsNullOrWhiteSpace(description))
            return FailAndCallback<StarQuestInfo?>("Description is required.", StarApiResultCode.InvalidParam);

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Name", name ?? string.Empty);
            writer.WriteString("Description", description);
            writer.WriteString("GameSource", gameSource ?? string.Empty);
            writer.WriteNumber("Order", order);
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/{questId}/subquests", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarQuestInfo?>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var subSrc = $"POST.api.quests/{questId.Trim()}/subquests";
        LogQuestParseChunkedFileOnly($"[Quest][Parse] source={subSrc} full HTTP body", response.Result);
        StarQuestInfo? created = null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (parseResult && resultElement.ValueKind == JsonValueKind.Object)
        {
            LogQuestJsonShapeFileOnly($"[Quest][Parse] source={subSrc} envelope", resultElement);
            created = ParseSingleQuestInfo(resultElement);
            LogParsedSingleQuestModelAudit(subSrc, created);
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(created, StarApiResultCode.Success, "Sub-quest added successfully.");
    }

    /// <summary>Run add-sub-quest on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarQuestInfo?>> QueueAddSubQuestAsync(string questId, string description, string? name = null, string? gameSource = null, int order = -1, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => AddSubQuestAsync(questId, description, name, gameSource, order, ct), cancellationToken);

    /// <summary>Removes a sub-quest (child quest) from a quest.</summary>
    public async Task<OASISResult<bool>> RemoveSubQuestAsync(string parentQuestId, string subQuestId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(parentQuestId) || string.IsNullOrWhiteSpace(subQuestId))
            return FailAndCallback<bool>("Parent quest ID and sub-quest ID are required.", StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Delete, $"{_baseApiUrl}/api/quests/{parentQuestId}/subquests/{subQuestId}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Sub-quest removed successfully.");
    }

    /// <summary>Run remove-sub-quest on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueRemoveSubQuestAsync(string parentQuestId, string subQuestId, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => RemoveSubQuestAsync(parentQuestId, subQuestId, ct), cancellationToken);

    /// <summary>Sets prerequisite quest IDs on a quest (MetaData.PrerequisiteQuestIds). Loads the quest via GET, merges metaData, then PUTs. Use for seed data so the UI can show prerequisite chains.</summary>
    public async Task<OASISResult<bool>> SetQuestPrerequisitesAsync(string questId, IReadOnlyList<string> prerequisiteQuestIds, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (string.IsNullOrWhiteSpace(questId))
            return FailAndCallback<bool>("Quest ID is required.", StarApiResultCode.InvalidParam);

        var getResponse = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/quests/{questId}", null, cancellationToken).ConfigureAwait(false);
        if (getResponse.IsError)
            return FailAndCallback<bool>(getResponse.Message ?? "GET quest failed", ParseCode(getResponse.ErrorCode, StarApiResultCode.ApiError), getResponse.Exception);

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(getResponse.Result ?? "{}");
        }
        catch (Exception ex)
        {
            return FailAndCallback<bool>($"Failed to parse quest response: {ex.Message}", StarApiResultCode.ApiError, ex);
        }

        var quest = root?["result"] ?? root?["Result"];
        if (quest is not JsonObject questObj)
            return FailAndCallback<bool>("Quest response did not contain a result object.", StarApiResultCode.ApiError);

        var metaData = questObj["metaData"] ?? questObj["MetaData"];
        if (metaData is not JsonObject metaObj)
        {
            metaObj = new JsonObject();
            questObj["metaData"] = metaObj;
        }
        var arr = new JsonArray(prerequisiteQuestIds.Select(s => (JsonNode?)s).ToArray());
        metaObj["PrerequisiteQuestIds"] = arr;

        var putBody = questObj.ToJsonString();
        var putResponse = await SendRawAsync(HttpMethod.Put, $"{_baseApiUrl}/api/quests/{questId}", putBody, cancellationToken).ConfigureAwait(false);
        if (putResponse.IsError)
            return FailAndCallback<bool>(putResponse.Message ?? "PUT quest failed", ParseCode(putResponse.ErrorCode, StarApiResultCode.ApiError), putResponse.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Quest prerequisites set.");
    }

    /// <summary>
    /// Gets all quests for the current avatar (no status filter).
    /// Use this for the quest popup and filter by status (Not Started, In Progress, Completed) in the client with checkboxes.
    /// </summary>
    public async Task<OASISResult<List<StarQuestInfo>>> GetAllQuestsForAvatarAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarQuestInfo>>("Client is not initialized.", StarApiResultCode.NotInitialized);

        var avatarIdResult = await EnsureAvatarIdAsync(cancellationToken).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
            return FailAndCallback<List<StarQuestInfo>>(avatarIdResult.Message ?? "Could not resolve avatar ID.", ParseCode(avatarIdResult.ErrorCode, StarApiResultCode.ApiError), avatarIdResult.Exception);

        var url = $"{_baseApiUrl}/api/quests/all-for-avatar/game";
        if (string.IsNullOrEmpty(_baseApiUrl))
            return FailAndCallback<List<StarQuestInfo>>("STAR API base URL not set.", StarApiResultCode.NotInitialized);

        OGEngineExports.StarApiLogFileOnly($"[Quests] GET all-for-avatar/game (AvatarId={GetCachedAvatarId() ?? "(none)"}) (BaseApiUrl)");

        var response = await SendRawWithRetryAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            OGEngineExports.StarApiLogFileOnly("[Quests] GET all-for-avatar/game failed (error).");
            OGEngineExports.StarApiLogFileOnly($"[Quests] GET all-for-avatar/game failed: {response.Message ?? "Request failed"}");
            return FailAndCallback<List<StarQuestInfo>>(response.Message ?? "Request failed", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            OGEngineExports.StarApiLogFileOnly($"[Quests] GET all-for-avatar/game parse failed: {parseErrorMessage ?? "Parse error"}");
            return FailAndCallback<List<StarQuestInfo>>(parseErrorMessage ?? "Parse error", parseErrorCode);
        }

        LogQuestJsonShapeFileOnly("[Quest][Parse] source=all-for-avatar/game envelope before unwrap", resultElement);
        LogQuestParseChunkedFileOnly("[Quest][Parse] source=all-for-avatar/game full HTTP body (exact API/DB payload)", response.Result);
        var quests = ParseQuestInfos(resultElement, "all-for-avatar/game") ?? new List<StarQuestInfo>();
        LogParsedQuestListModelAudit("all-for-avatar/game", quests);
        int totalObjectives = quests.Sum(q => q.Objectives?.Count ?? 0);
        OGEngineExports.StarApiLogFileOnly($"[Quests] GET all-for-avatar/game success: {quests.Count} quests, {totalObjectives} objectives");
        var idSummary = quests.Count > 0 ? string.Join(", ", quests.Take(12).Select(q => q.Id ?? "(null)")) + (quests.Count > 12 ? "..." : "") : "(none)";
        OGEngineExports.StarApiLogFileOnly($"[Quests] all-for-avatar/game Response IsError=False Message=(ok) Parsed: Count={quests.Count} totalObjectives={totalObjectives} Ids={idSummary}");
        OGEngineExports.StarApiLogFileOnly($"[Quests] all-for-avatar/game parsed: {quests.Count} quests, {totalObjectives} objectives");
        // Update in-memory cache so GetQuestObjectivesFromCache / TryGetQuestObjectivesCache (and game detail panel) see this data without waiting for background refresh.
        UpdateQuestsCache(quests);
        InvokeCallback(StarApiResultCode.Success);
        return Success(quests, StarApiResultCode.Success, $"Loaded {quests.Count} quest(s) for avatar.");
    }

    /// <summary>Write a quest list into the in-memory cache so native/game cache readers (get_quests_string, get_quest_objectives_string, etc.) see it. Used after GetAllQuestsForAvatarAsync and by the background refresh.</summary>
    private void UpdateQuestsCache(List<StarQuestInfo> list)
    {
        if (list == null) return;
        lock (_questsCacheLock)
        {
            _questsCacheString = list.Count == 0 ? string.Empty : SerializeQuestsForGame(list);
            _cachedQuestList = list;
            _questsFilterLastLogTop = (0, 0);
            _questsFilterLastLogObjectives = ("", -1);
            _questsFilterLastLogSubQuests = ("", -1);
            _questsFilterLastLogPrereqs = ("", -1);
        }
    }

    /// <summary>Update a single quest's status in the cached list and re-serialize so the UI sees the change immediately without a full refetch. Call after start-quest API success.</summary>
    private void UpdateQuestStatusInCache(string questId, string newStatus)
    {
        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(newStatus)) return;
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return;
            for (var i = 0; i < _cachedQuestList.Count; i++)
            {
                var q = _cachedQuestList[i];
                if (!string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase)) continue;
                var updated = new StarQuestInfo
                {
                    Id = q.Id,
                    Name = q.Name,
                    Description = q.Description,
                    Status = newStatus,
                    Order = q.Order,
                    GameSource = q.GameSource ?? string.Empty,
                    Requirements = q.Requirements ?? new List<string>(),
                    RewardKarma = q.RewardKarma,
                    RewardXP = q.RewardXP,
                    CompletionNotes = q.CompletionNotes,
                    ParentMissionId = q.ParentMissionId ?? string.Empty,
                    ParentQuestId = q.ParentQuestId ?? string.Empty,
                    Objectives = q.Objectives ?? new List<StarQuestObjective>(),
                    PrerequisiteQuestIds = q.PrerequisiteQuestIds ?? new List<string>(),
                    LinkedGeoHotSpotId = q.LinkedGeoHotSpotId,
                    ExternalHandoffUri = q.ExternalHandoffUri,
                    Dictionaries = q.Dictionaries
                };
                _cachedQuestList[i] = updated;
                _questsCacheString = _cachedQuestList.Count == 0 ? string.Empty : SerializeQuestsForGame(_cachedQuestList);
                _questsFilterLastLogTop = (0, 0);
                _questsFilterLastLogObjectives = ("", -1);
                _questsFilterLastLogSubQuests = ("", -1);
                _questsFilterLastLogPrereqs = ("", -1);
                OGEngineExports.StarApiLog($"[Quests] Updated cached quest {questId} status to {newStatus}; UI will refresh from cache.");
                return;
            }
            OGEngineExports.StarApiLogFileOnly($"[Quests] UpdateQuestStatusInCache: quest id not in local cache ({questId}); status not patched in-memory (server may still have updated).");
        }
    }

    //TODO: Use Enum for status, try to use enums instead of strings generally.
    public async Task<OASISResult<List<StarQuestInfo>>> GetQuestsByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarQuestInfo>>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (string.IsNullOrWhiteSpace(status))
            return FailAndCallback<List<StarQuestInfo>>("Quest status is required (e.g. InProgress, NotStarted, Completed).", StarApiResultCode.InvalidParam);

        var avatarIdResult = await EnsureAvatarIdAsync(cancellationToken).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
            return FailAndCallback<List<StarQuestInfo>>(avatarIdResult.Message ?? "Could not resolve avatar ID.", ParseCode(avatarIdResult.ErrorCode, StarApiResultCode.ApiError), avatarIdResult.Exception);

        var url = $"{_baseApiUrl}/api/quests/by-status/{Uri.EscapeDataString(status.Trim())}/game";
        if (string.IsNullOrEmpty(_baseApiUrl))
            return FailAndCallback<List<StarQuestInfo>>("STAR API base URL not set.", StarApiResultCode.NotInitialized);

        var avatarIdForLog = GetCachedAvatarId() ?? "(none)";
        OGEngineExports.StarApiLog($"[Quests] Client AvatarId={avatarIdForLog} (compare with seed output and API log)");
        OGEngineExports.StarApiLog($"[Quests] GET {url}");

        var response = await SendRawAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);

        OGEngineExports.StarApiLog($"[Quests] Response IsError={response.IsError} Message={response.Message ?? "(ok)"}");
        if (response.IsError)
            OGEngineExports.StarApiLog($"[Quests] Error: {response.Message ?? "Request failed"}");
        else
            OGEngineExports.StarApiLog("[Quests] OK");

        if (response.IsError)
            return FailAndCallback<List<StarQuestInfo>>(response.Message ?? "Request failed", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<List<StarQuestInfo>>(parseErrorMessage ?? "Parse error", parseErrorCode);

        var statusTag = $"by-status/{Uri.EscapeDataString(status.Trim())}/game";
        LogQuestJsonShapeFileOnly($"[Quest][Parse] source={statusTag} envelope before unwrap", resultElement);
        LogQuestParseChunkedFileOnly($"[Quest][Parse] source={statusTag} full HTTP body (exact API/DB payload)", response.Result);
        var quests = ParseQuestInfos(resultElement, statusTag) ?? new List<StarQuestInfo>();
        LogParsedQuestListModelAudit(statusTag, quests);
        var idSummary = quests.Count > 0 ? string.Join(", ", quests.Take(12).Select(q => q.Id ?? "(null)")) + (quests.Count > 12 ? "..." : "") : "(none)";
        OGEngineExports.StarApiLogFileOnly($"[Quests] by-status parsed: Count={quests.Count} Ids={idSummary}");
        if (quests.Count > 0)
            OGEngineExports.StarApiLog($"[Quests] OK ({quests.Count} quests) Ids={string.Join(", ", quests.Select(q => q.Id ?? "(null)"))}");
        else
            OGEngineExports.StarApiLog("[Quests] OK (0 quests)");
        InvokeCallback(StarApiResultCode.Success);
        return Success(quests, StarApiResultCode.Success, $"Loaded {quests.Count} quest(s) (status={status}).");
    }

    public Task<OASISResult<List<StarQuestInfo>>> GetActiveQuestsAsync(CancellationToken cancellationToken = default) =>
        GetQuestsByStatusAsync("InProgress", cancellationToken);

    /// <summary>Run get-active-quests on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<List<StarQuestInfo>>> QueueGetActiveQuestsAsync(CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => GetActiveQuestsAsync(ct), cancellationToken);

    /// <summary>Serialize quests to a string for game UI: each quest block is "Q\tid\tname\tdesc\tstatus\tpct\n" then "O\tid\tdesc\tdone\n" per objective (sub-quests), then "P\tid1\tid2\n" (prereqs), then "---\n". Tabs/newlines in text are replaced with space. pct = completed objectives / total * 100.</summary>
    public static string SerializeQuestsForGame(List<StarQuestInfo>? quests)
    {
        if (quests is null || quests.Count == 0)
            return string.Empty;
        var sb = new StringBuilder();
        foreach (var q in quests)
        {
            var name = EscapeForQuestLine(q.Name);
            var desc = EscapeForQuestLine(q.Description);
            var status = QuestStatusToGameString(q.Status);
            var objCount = q.Objectives?.Count ?? 0;
            var completed = q.Objectives?.Count(o => o.IsCompleted) ?? 0;
            var pct = objCount > 0 ? (completed * 100 / objCount) : 0;
            /* Never show Completed in the list unless every embedded objective is completed (API MetaData/Status can be wrong after partial progress). */
            if (objCount > 0 && q.Objectives != null)
            {
                var allObjDone = q.Objectives.All(o => o.IsCompleted);
                if (allObjDone)
                    status = "Completed";
                else if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                    status = "InProgress";
            }

            sb.Append("Q\t").Append(q.Id).Append("\t").Append(name).Append("\t").Append(desc).Append("\t").Append(status).Append("\t").Append(pct).Append("\n");
            if (q.Objectives != null)
            {
                for (var i = 0; i < q.Objectives.Count; i++)
                {
                    var o = q.Objectives[i];
                    var oid = string.IsNullOrEmpty(o.Id) ? $"obj_{i}" : o.Id;
                    sb.Append("O\t").Append(oid).Append("\t").Append(FormatObjectiveLineForGameList(o, q)).Append("\t").Append(o.IsCompleted ? "1" : "0").Append("\n");
                }
            }
            if (q.PrerequisiteQuestIds != null && q.PrerequisiteQuestIds.Count > 0)
                sb.Append("P\t").AppendJoin("\t", q.PrerequisiteQuestIds).Append("\n");
            sb.Append("\n---\n");
        }
        return sb.ToString();
    }

    /// <summary>Map API status (enum number "0"/"1"/"2" or name) to game string: NotStarted, InProgress, Completed.</summary>
    private static string QuestStatusToGameString(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "InProgress";
        var t = s.Trim();
        if (t == "0" || string.Equals(t, "NotStarted", StringComparison.OrdinalIgnoreCase)) return "NotStarted";
        if (t == "1" || string.Equals(t, "InProgress", StringComparison.OrdinalIgnoreCase)) return "InProgress";
        if (t == "2" || string.Equals(t, "Completed", StringComparison.OrdinalIgnoreCase)) return "Completed";
        return NormalizeQuestStatus(t);
    }

    /// <summary>Normalize status for game parsing: "Not Started" -> "NotStarted", "In Progress" -> "InProgress", "Completed" unchanged.</summary>
    private static string NormalizeQuestStatus(string s)
    {
        if (string.IsNullOrEmpty(s)) return "InProgress";
        return EscapeForQuestLine(s).Replace(" ", "");
    }
}
