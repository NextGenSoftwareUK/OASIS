using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STARAPI.Client;

public enum StarApiResultCode
{
    Success = 0,
    InitFailed = -1,
    NotInitialized = -2,
    Network = -3,
    InvalidParam = -4,
    ApiError = -5
}

public sealed class StarApiConfig
{
    public string BaseUrl { get; init; } = string.Empty;
    public string? ApiKey { get; init; }
    public string? AvatarId { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
}

public sealed class StarItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string GameSource { get; init; } = "Unknown";
    public string ItemType { get; init; } = "Miscellaneous";
}

public sealed class StarAvatarProfile
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}

public sealed class StarQuestObjective
{
    public string Description { get; init; } = string.Empty;
    public string GameSource { get; init; } = string.Empty;
    public string ItemRequired { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
}

public sealed class StarQuestInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<StarQuestObjective> Objectives { get; init; } = new();
}

public sealed class StarNftInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Dictionary<string, string> MetaData { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public delegate void StarApiCallback(StarApiResultCode result, object? userData);

public sealed class StarApiClient : IDisposable
{
    private readonly object _stateLock = new();

    private HttpClient? _httpClient;
    private bool _initialized;
    private string _baseApiUrl = string.Empty;
    private string _oasisBaseUrl = string.Empty;
    private string? _jwtToken;
    private string? _refreshToken;
    private string? _avatarId;
    private string _lastError = string.Empty;
    private StarApiCallback? _callback;
    private object? _callbackUserData;
    private readonly ConcurrentQueue<PendingAddItemJob> _pendingAddItemJobs = new();
    private readonly SemaphoreSlim _addItemSignal = new(0);
    private readonly object _jobLock = new();
    private CancellationTokenSource? _jobCts;
    private Task? _jobWorker;

    public int AddItemBatchSize { get; set; } = 32;
    public TimeSpan AddItemBatchWindow { get; set; } = TimeSpan.FromMilliseconds(75);

    public OASISResult<bool> Init(StarApiConfig config)
    {
        if (config is null || string.IsNullOrWhiteSpace(config.BaseUrl))
            return Fail<bool>("Invalid configuration.", StarApiResultCode.InvalidParam);

        if (!Uri.TryCreate(config.BaseUrl.TrimEnd('/'), UriKind.Absolute, out var baseUri))
            return Fail<bool>("BaseUrl must be a valid absolute URL.", StarApiResultCode.InvalidParam);

        var timeout = config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 30;
        var normalizedBaseUrl = baseUri.ToString().TrimEnd('/');
        var oasisBaseUrl = normalizedBaseUrl;
        var apiIndex = oasisBaseUrl.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
        if (apiIndex >= 0)
            oasisBaseUrl = oasisBaseUrl[..apiIndex];

        lock (_stateLock)
        {
            _httpClient?.Dispose();
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(normalizedBaseUrl + "/"),
                Timeout = TimeSpan.FromSeconds(timeout)
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _baseApiUrl = normalizedBaseUrl;
            _oasisBaseUrl = oasisBaseUrl;
            _avatarId = string.IsNullOrWhiteSpace(config.AvatarId) ? null : config.AvatarId;
            _jwtToken = null;
            _refreshToken = null;
            _lastError = string.Empty;
            _initialized = true;

            if (!string.IsNullOrWhiteSpace(config.ApiKey))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        }

        StartAddItemWorker();

        return Success(true, StarApiResultCode.Success, "STAR API client initialized successfully.");
    }

    public async Task<OASISResult<bool>> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return FailAndCallback<bool>("Username and password are required.", StarApiResultCode.InvalidParam);

        try
        {
            var payload = BuildJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("username", username);
                writer.WriteString("password", password);
                writer.WriteEndObject();
            });

            var response = await SendRawAsync(HttpMethod.Post, $"{_oasisBaseUrl}/api/avatar/authenticate", payload, cancellationToken).ConfigureAwait(false);
            if (response.IsError)
                return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

            var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
            if (!parseResult)
                return FailAndCallback<bool>(parseErrorMessage, parseErrorCode);

            var auth = ParseAvatarAuthResponse(resultElement);
            if (auth is null)
                return FailAndCallback<bool>("Authentication response did not include avatar data.", StarApiResultCode.ApiError);

            lock (_stateLock)
            {
                _jwtToken = auth.JwtToken;
                _refreshToken = auth.RefreshToken;
                _avatarId = auth.Id == Guid.Empty ? _avatarId : auth.Id.ToString();

                if (!string.IsNullOrWhiteSpace(_jwtToken) && _httpClient is not null)
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
            }

            var result = Success(true, StarApiResultCode.Success, "Authentication successful.");
            InvokeCallback(StarApiResultCode.Success);
            return result;
        }
        catch (Exception ex)
        {
            return FailAndCallback<bool>($"Authentication failed: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    public OASISResult<bool> SetApiKey(string apiKey, string avatarId)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(avatarId))
            return FailAndCallback<bool>("API key and avatar ID are required.", StarApiResultCode.InvalidParam);

        lock (_stateLock)
        {
            _avatarId = avatarId;
            _jwtToken = apiKey;
            if (_httpClient is not null)
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "API key authentication configured.");
    }

    public async Task<OASISResult<StarAvatarProfile>> GetCurrentAvatarAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarAvatarProfile>("Client is not initialized.", StarApiResultCode.NotInitialized);

        var response = await SendRawAsync(HttpMethod.Get, $"{_oasisBaseUrl}/api/avatar/current", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarAvatarProfile>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<StarAvatarProfile>(parseErrorMessage, parseErrorCode);

        var avatar = ParseAvatarProfile(resultElement);
        if (avatar is null || avatar.Id == Guid.Empty)
            return FailAndCallback<StarAvatarProfile>("Could not parse current avatar profile.", StarApiResultCode.ApiError);

        lock (_stateLock)
            _avatarId = avatar.Id.ToString();

        InvokeCallback(StarApiResultCode.Success);
        return Success(avatar, StarApiResultCode.Success, "Current avatar loaded.");
    }

    public OASISResult<bool> Cleanup()
    {
        StopAddItemWorker();

        lock (_stateLock)
        {
            _httpClient?.Dispose();
            _httpClient = null;
            _initialized = false;
            _jwtToken = null;
            _refreshToken = null;
            _avatarId = null;
            _lastError = string.Empty;
        }

        return Success(true, StarApiResultCode.Success, "STAR API client cleaned up.");
    }

    public async Task<OASISResult<bool>> HasItemAsync(string itemName, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<bool>("Item name is required.", StarApiResultCode.InvalidParam);

        var inventory = await GetInventoryAsync(cancellationToken).ConfigureAwait(false);
        if (inventory.IsError)
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = inventory.Message,
                ErrorCode = inventory.ErrorCode,
                Exception = inventory.Exception
            };
        }

        var hasItem = inventory.Result!.Any(x =>
            string.Equals(x.Name, itemName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Description, itemName, StringComparison.OrdinalIgnoreCase));

        InvokeCallback(StarApiResultCode.Success);
        return Success(hasItem, StarApiResultCode.Success, hasItem ? "Item found in inventory." : "Item not found in inventory.");
    }

    public async Task<OASISResult<List<StarItem>>> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarItem>>("Client is not initialized.", StarApiResultCode.NotInitialized);

        var avatarIdResult = await EnsureAvatarIdAsync(cancellationToken).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
        {
            return new OASISResult<List<StarItem>>
            {
                IsError = true,
                Message = avatarIdResult.Message,
                ErrorCode = avatarIdResult.ErrorCode,
                Exception = avatarIdResult.Exception
            };
        }

        try
        {
            var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/inventoryitems/user/{avatarIdResult.Result}", null, cancellationToken).ConfigureAwait(false);
            if (response.IsError)
                return FailAndCallback<List<StarItem>>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

            var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
            if (!parseResult)
                return FailAndCallback<List<StarItem>>(parseErrorMessage, parseErrorCode);

            var mapped = ParseInventoryItems(resultElement);

            InvokeCallback(StarApiResultCode.Success);
            return Success(mapped, StarApiResultCode.Success, $"Loaded {mapped.Count} item(s).");
        }
        catch (Exception ex)
        {
            return FailAndCallback<List<StarItem>>($"Failed to load inventory: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    public async Task<OASISResult<StarItem>> AddItemAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", CancellationToken cancellationToken = default)
    {
        return await AddItemCoreAsync(itemName, description, gameSource, itemType, cancellationToken).ConfigureAwait(false);
    }

    public Task<OASISResult<StarItem>> QueueAddItemAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<StarItem>("Client is not initialized.", StarApiResultCode.NotInitialized));

        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(gameSource))
            return Task.FromResult(FailAndCallback<StarItem>("Item name, description, and game source are required.", StarApiResultCode.InvalidParam));

        var tcs = new TaskCompletionSource<OASISResult<StarItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingAddItemJobs.Enqueue(new PendingAddItemJob(itemName, description, gameSource, itemType, cancellationToken, tcs));
        _addItemSignal.Release();
        return tcs.Task;
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
            tasks.Add(QueueAddItemAsync(item.Name, item.Description, source, item.ItemType, cancellationToken));
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

    public async Task<OASISResult<bool>> FlushAddItemJobsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        while ((!_pendingAddItemJobs.IsEmpty || GetInFlightAddItemCount() > 0) && !cancellationToken.IsCancellationRequested)
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
            return FailAndCallback<bool>("Flush add-item jobs was cancelled.", StarApiResultCode.Network);

        return Success(true, StarApiResultCode.Success, "Add-item queue flushed.");
    }

    private async Task<OASISResult<StarItem>> AddItemCoreAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarItem>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(gameSource))
            return FailAndCallback<StarItem>("Item name, description, and game source are required.", StarApiResultCode.InvalidParam);

        try
        {
            var payload = BuildJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("Name", itemName);
                writer.WriteString("Description", $"{description} | Source: {gameSource}");
                writer.WriteString("HolonType", "InventoryItem");
                writer.WritePropertyName("MetaData");
                writer.WriteStartObject();
                writer.WriteString("GameSource", gameSource);
                writer.WriteString("ItemType", string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType);
                writer.WriteBoolean("CrossGameItem", true);
                writer.WriteString("CollectedAt", DateTime.UtcNow.ToString("O"));
                writer.WriteEndObject();
                writer.WriteEndObject();
            });

            var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/inventoryitems", payload, cancellationToken).ConfigureAwait(false);
            if (response.IsError)
                return FailAndCallback<StarItem>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

            var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
            if (!parseResult)
                return FailAndCallback<StarItem>(parseErrorMessage, parseErrorCode);

            var item = ParseInventoryItemResponse(resultElement);
            if (item is null)
                return FailAndCallback<StarItem>("API did not return the created inventory item.", StarApiResultCode.ApiError);

            var mapped = new StarItem
            {
                Id = item.Id,
                Name = item.Name ?? itemName,
                Description = item.Description ?? description,
                GameSource = ExtractMeta(item.MetaData, "GameSource", gameSource),
                ItemType = ExtractMeta(item.MetaData, "ItemType", string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType)
            };

            InvokeCallback(StarApiResultCode.Success);
            return Success(mapped, StarApiResultCode.Success, "Item added successfully.");
        }
        catch (Exception ex)
        {
            return FailAndCallback<StarItem>($"Failed to add item: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    public async Task<OASISResult<bool>> UseItemAsync(string itemName, string? context = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<bool>("Item name is required.", StarApiResultCode.InvalidParam);

        var inventory = await GetInventoryAsync(cancellationToken).ConfigureAwait(false);
        if (inventory.IsError)
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = inventory.Message,
                ErrorCode = inventory.ErrorCode,
                Exception = inventory.Exception
            };
        }

        var item = inventory.Result!.FirstOrDefault(i => string.Equals(i.Name, itemName, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            InvokeCallback(StarApiResultCode.Success);
            return Success(false, StarApiResultCode.Success, $"Item '{itemName}' is not in inventory.");
        }

        try
        {
            var payload = BuildJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("Context", string.IsNullOrWhiteSpace(context) ? "game_use" : context);
                writer.WriteString("UsedAt", DateTime.UtcNow.ToString("O"));
                writer.WriteEndObject();
            });

            var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/inventoryitems/{item.Id}/use", payload, cancellationToken).ConfigureAwait(false);
            if (response.IsError)
                return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

            InvokeCallback(StarApiResultCode.Success);
            return Success(true, StarApiResultCode.Success, "Item used successfully.");
        }
        catch (Exception ex)
        {
            return FailAndCallback<bool>($"Failed to use item: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    public async Task<OASISResult<bool>> StartQuestAsync(string questId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId))
            return FailAndCallback<bool>("Quest ID is required.", StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/{questId}/start", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Quest started successfully.");
    }

    public async Task<OASISResult<bool>> CompleteQuestObjectiveAsync(string questId, string objectiveId, string? gameSource = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId))
            return FailAndCallback<bool>("Quest ID and objective ID are required.", StarApiResultCode.InvalidParam);

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("objectiveId", objectiveId);
            writer.WriteBoolean("completed", true);
            writer.WriteString("completedAt", DateTime.UtcNow.ToString("O"));
            writer.WriteString("gameSource", string.IsNullOrWhiteSpace(gameSource) ? "Unknown" : gameSource);
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Put, $"{_baseApiUrl}/api/quests/{questId}/objectives/{objectiveId}", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Quest objective completed successfully.");
    }

    public async Task<OASISResult<bool>> CompleteQuestAsync(string questId, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId))
            return FailAndCallback<bool>("Quest ID is required.", StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/{questId}/complete", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Quest completed successfully.");
    }

    public async Task<OASISResult<bool>> CreateCrossGameQuestAsync(string questName, string description, List<StarQuestObjective> objectives, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questName) || string.IsNullOrWhiteSpace(description) || objectives is null || objectives.Count == 0)
            return FailAndCallback<bool>("Quest name, description and at least one objective are required.", StarApiResultCode.InvalidParam);

        var games = objectives
            .Select(o => string.IsNullOrWhiteSpace(o.GameSource) ? "Unknown" : o.GameSource)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Name", questName);
            writer.WriteString("Description", description);
            writer.WriteString("Type", "CrossGame");
            writer.WritePropertyName("Objectives");
            writer.WriteStartArray();
            foreach (var objective in objectives)
            {
                writer.WriteStartObject();
                writer.WriteString("Description", objective.Description);
                writer.WriteString("GameSource", objective.GameSource);
                writer.WriteString("ItemRequired", objective.ItemRequired);
                writer.WriteBoolean("IsCompleted", objective.IsCompleted);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("MetaData");
            writer.WriteStartObject();
            writer.WriteBoolean("CrossGameQuest", true);
            writer.WritePropertyName("Games");
            writer.WriteStartArray();
            foreach (var game in games)
                writer.WriteStringValue(game);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/missions", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Cross-game quest created successfully.");
    }

    public async Task<OASISResult<List<StarQuestInfo>>> GetActiveQuestsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarQuestInfo>>("Client is not initialized.", StarApiResultCode.NotInitialized);

        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/quests?status=Active", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<List<StarQuestInfo>>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<List<StarQuestInfo>>(parseErrorMessage, parseErrorCode);

        var quests = ParseQuestInfos(resultElement);
        InvokeCallback(StarApiResultCode.Success);
        return Success(quests, StarApiResultCode.Success, $"Loaded {quests.Count} active quest(s).");
    }

    public async Task<OASISResult<string>> CreateBossNftAsync(string bossName, string? description, string? gameSource, string? bossStatsJson, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<string>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(bossName))
            return FailAndCallback<string>("Boss name is required.", StarApiResultCode.InvalidParam);

        JsonElement bossStatsElement;
        try
        {
            var statsJson = string.IsNullOrWhiteSpace(bossStatsJson) ? "{}" : bossStatsJson;
            using var statsDoc = JsonDocument.Parse(statsJson);
            bossStatsElement = statsDoc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            return FailAndCallback<string>($"bossStatsJson is not valid JSON: {ex.Message}", StarApiResultCode.InvalidParam, ex);
        }

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Name", bossName);
            writer.WriteString("Description", string.IsNullOrWhiteSpace(description) ? "Boss from game" : description);
            writer.WriteString("Type", "Boss");
            writer.WritePropertyName("MetaData");
            writer.WriteStartObject();
            writer.WriteString("GameSource", string.IsNullOrWhiteSpace(gameSource) ? "Unknown" : gameSource);
            writer.WritePropertyName("BossStats");
            bossStatsElement.WriteTo(writer);
            writer.WriteString("DefeatedAt", DateTime.UtcNow.ToString("O"));
            writer.WriteBoolean("Deployable", true);
            writer.WriteEndObject();
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/nfts", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<string>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<string>(parseErrorMessage, parseErrorCode);

        var nftId = ParseIdAsString(resultElement);
        if (string.IsNullOrWhiteSpace(nftId))
            return FailAndCallback<string>("API did not return an NFT ID.", StarApiResultCode.ApiError);

        InvokeCallback(StarApiResultCode.Success);
        return Success(nftId, StarApiResultCode.Success, "Boss NFT created successfully.");
    }

    public async Task<OASISResult<bool>> DeployBossNftAsync(string nftId, string targetGame, string? location = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(nftId) || string.IsNullOrWhiteSpace(targetGame))
            return FailAndCallback<bool>("NFT ID and target game are required.", StarApiResultCode.InvalidParam);

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("nftId", nftId);
            writer.WriteString("targetGame", targetGame);
            writer.WriteString("location", string.IsNullOrWhiteSpace(location) ? "default" : location);
            writer.WriteString("deployedAt", DateTime.UtcNow.ToString("O"));
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/nfts/{nftId}/deploy", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Boss NFT deployed successfully.");
    }

    public async Task<OASISResult<List<StarNftInfo>>> GetNftCollectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarNftInfo>>("Client is not initialized.", StarApiResultCode.NotInitialized);

        var avatarIdResult = await EnsureAvatarIdAsync(cancellationToken).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
            return FailAndCallback<List<StarNftInfo>>(avatarIdResult.Message, ParseCode(avatarIdResult.ErrorCode, StarApiResultCode.ApiError), avatarIdResult.Exception);

        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/nfts/user/{avatarIdResult.Result}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<List<StarNftInfo>>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<List<StarNftInfo>>(parseErrorMessage, parseErrorCode);

        var nfts = ParseNftInfos(resultElement);
        InvokeCallback(StarApiResultCode.Success);
        return Success(nfts, StarApiResultCode.Success, $"Loaded {nfts.Count} NFT(s).");
    }

    public OASISResult<string> GetLastError()
    {
        lock (_stateLock)
            return Success(_lastError, StarApiResultCode.Success, "Last error retrieved.");
    }

    public OASISResult<bool> SetCallback(StarApiCallback? callback, object? userData = null)
    {
        lock (_stateLock)
        {
            _callback = callback;
            _callbackUserData = userData;
        }

        return Success(true, StarApiResultCode.Success, "Callback updated.");
    }

    public void Dispose()
    {
        Cleanup();
    }

    private async Task<OASISResult<string>> SendRawAsync(HttpMethod method, string url, string? bodyJson, CancellationToken cancellationToken)
    {
        if (_httpClient is null)
            return Fail<string>("HTTP client is not initialized.", StarApiResultCode.NotInitialized);

        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (!string.IsNullOrWhiteSpace(bodyJson))
                request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var failureMessage = $"HTTP {(int)response.StatusCode} ({response.StatusCode}) calling {url}.";
                if (!string.IsNullOrWhiteSpace(responseBody))
                    failureMessage += $" Body: {responseBody}";

                return Fail<string>(failureMessage, StarApiResultCode.ApiError);
            }

            return Success(responseBody ?? string.Empty, StarApiResultCode.Success, "Request completed successfully.");
        }
        catch (Exception ex)
        {
            return Fail<string>($"Network call failed: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    private async Task<OASISResult<string>> EnsureAvatarIdAsync(CancellationToken cancellationToken)
    {
        lock (_stateLock)
        {
            if (!string.IsNullOrWhiteSpace(_avatarId))
                return Success(_avatarId!, StarApiResultCode.Success, "Avatar ID already available.");
        }

        var response = await SendRawAsync(HttpMethod.Get, $"{_oasisBaseUrl}/api/avatar/current", null, cancellationToken).ConfigureAwait(false);
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
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                var isError = GetBoolProperty(root, "IsError");
                var message = GetStringProperty(root, "Message");
                var codeText = GetStringProperty(root, "ErrorCode");
                var parsedCode = ParseCode(codeText, StarApiResultCode.ApiError);

                var hasResult = TryGetProperty(root, "Result", out var resultElement);
                if (hasResult)
                    result = resultElement.Clone();
                else
                    result = root.Clone();

                if (isError)
                {
                    errorCode = parsedCode;
                    errorMessage = string.IsNullOrWhiteSpace(message) ? "API returned an error." : message!;
                    return false;
                }

                errorCode = StarApiResultCode.Success;
                errorMessage = string.Empty;
                return true;
            }

            result = root.Clone();
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
        if (element.ValueKind != JsonValueKind.Array)
            return items;

        foreach (var itemElement in element.EnumerateArray())
        {
            var item = ParseInventoryItemResponse(itemElement);
            if (item is null)
                continue;

            items.Add(new StarItem
            {
                Id = item.Id,
                Name = item.Name ?? string.Empty,
                Description = item.Description ?? string.Empty,
                GameSource = ExtractMeta(item.MetaData, "GameSource", "Unknown"),
                ItemType = ExtractMeta(item.MetaData, "ItemType", "Miscellaneous")
            });
        }

        return items;
    }

    private InventoryItemResponse? ParseInventoryItemResponse(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var idValue = GetStringProperty(element, "Id");
        Guid.TryParse(idValue, out var parsedGuid);

        Dictionary<string, JsonElement>? metadata = null;
        if (TryGetProperty(element, "MetaData", out var metaElement) && metaElement.ValueKind == JsonValueKind.Object)
        {
            metadata = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in metaElement.EnumerateObject())
                metadata[property.Name] = property.Value.Clone();
        }

        return new InventoryItemResponse
        {
            Id = parsedGuid,
            Name = GetStringProperty(element, "Name"),
            Description = GetStringProperty(element, "Description"),
            MetaData = metadata
        };
    }

    private static AvatarAuthResponse? ParseAvatarAuthResponse(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        Guid.TryParse(GetStringProperty(element, "Id"), out var id);
        return new AvatarAuthResponse
        {
            Id = id,
            JwtToken = GetStringProperty(element, "JwtToken"),
            RefreshToken = GetStringProperty(element, "RefreshToken")
        };
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
            return GetStringProperty(element, "Id");

        if (element.ValueKind == JsonValueKind.String)
            return element.GetString();

        return null;
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

    private void StartAddItemWorker()
    {
        lock (_jobLock)
        {
            if (_jobWorker is { IsCompleted: false })
                return;

            _jobCts = new CancellationTokenSource();
            _jobWorker = Task.Run(() => ProcessAddItemJobsAsync(_jobCts.Token));
        }
    }

    private void StopAddItemWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_jobLock)
        {
            cts = _jobCts;
            worker = _jobWorker;
            _jobCts = null;
            _jobWorker = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _addItemSignal.Release();
                worker?.GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        while (_pendingAddItemJobs.TryDequeue(out var pending))
            pending.Completion.TrySetResult(Fail<StarItem>("Add-item queue stopped.", StarApiResultCode.NotInitialized));
    }

    private async Task ProcessAddItemJobsAsync(CancellationToken cancellationToken)
    {
        var batch = new List<PendingAddItemJob>(Math.Max(1, AddItemBatchSize));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _addItemSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            batch.Clear();
            while (_pendingAddItemJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, AddItemBatchSize))
                batch.Add(pending);

            if (batch.Count == 0)
                continue;

            if (AddItemBatchWindow > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(AddItemBatchWindow, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                while (_pendingAddItemJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, AddItemBatchSize))
                    batch.Add(pending);
            }

            foreach (var job in batch)
            {
                if (job.CancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    job.Completion.TrySetResult(Fail<StarItem>("Queued add-item job was cancelled.", StarApiResultCode.Network));
                    continue;
                }

                var result = await AddItemCoreAsync(job.ItemName, job.Description, job.GameSource, job.ItemType, job.CancellationToken).ConfigureAwait(false);
                job.Completion.TrySetResult(result);
            }
        }
    }

    private int GetInFlightAddItemCount()
    {
        lock (_jobLock)
            return _jobWorker is { IsCompleted: false } ? 1 : 0;
    }

    private static StarAvatarProfile? ParseAvatarProfile(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        Guid.TryParse(GetStringProperty(element, "Id"), out var id);
        return new StarAvatarProfile
        {
            Id = id,
            Username = GetStringProperty(element, "Username") ?? string.Empty,
            Email = GetStringProperty(element, "Email") ?? string.Empty,
            FirstName = GetStringProperty(element, "FirstName") ?? string.Empty,
            LastName = GetStringProperty(element, "LastName") ?? string.Empty
        };
    }

    private static List<StarQuestInfo> ParseQuestInfos(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object && TryGetProperty(element, "Quests", out var questsElement))
            element = questsElement;

        var quests = new List<StarQuestInfo>();
        if (element.ValueKind != JsonValueKind.Array)
            return quests;

        foreach (var questElement in element.EnumerateArray())
        {
            if (questElement.ValueKind != JsonValueKind.Object)
                continue;

            var objectives = new List<StarQuestObjective>();
            if (TryGetProperty(questElement, "Objectives", out var objectiveElement) && objectiveElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var objective in objectiveElement.EnumerateArray())
                {
                    objectives.Add(new StarQuestObjective
                    {
                        Description = GetStringProperty(objective, "Description") ?? string.Empty,
                        GameSource = GetStringProperty(objective, "GameSource") ?? string.Empty,
                        ItemRequired = GetStringProperty(objective, "ItemRequired") ?? string.Empty,
                        IsCompleted = GetBoolProperty(objective, "IsCompleted")
                    });
                }
            }

            quests.Add(new StarQuestInfo
            {
                Id = GetStringProperty(questElement, "Id") ?? string.Empty,
                Name = GetStringProperty(questElement, "Name") ?? string.Empty,
                Description = GetStringProperty(questElement, "Description") ?? string.Empty,
                Status = GetStringProperty(questElement, "Status") ?? string.Empty,
                Objectives = objectives
            });
        }

        return quests;
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

    private sealed class AvatarAuthResponse
    {
        public Guid Id { get; set; }
        public string? JwtToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    private sealed class AvatarInfo
    {
        public Guid Id { get; set; }
    }

    private sealed class InventoryItemResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, JsonElement>? MetaData { get; set; }
    }

    private sealed class PendingAddItemJob
    {
        public PendingAddItemJob(string itemName, string description, string gameSource, string itemType, CancellationToken cancellationToken, TaskCompletionSource<OASISResult<StarItem>> completion)
        {
            ItemName = itemName;
            Description = description;
            GameSource = gameSource;
            ItemType = string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType;
            CancellationToken = cancellationToken;
            Completion = completion;
        }

        public string ItemName { get; }
        public string Description { get; }
        public string GameSource { get; }
        public string ItemType { get; }
        public CancellationToken CancellationToken { get; }
        public TaskCompletionSource<OASISResult<StarItem>> Completion { get; }
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct star_api_config_t
{
    public sbyte* base_url;
    public sbyte* api_key;
    public sbyte* avatar_id;
    public int timeout_seconds;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct star_item_t
{
    public fixed byte id[64];
    public fixed byte name[256];
    public fixed byte description[512];
    public fixed byte game_source[64];
    public fixed byte item_type[64];
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct star_item_list_t
{
    public star_item_t* items;
    public nuint count;
    public nuint capacity;
}

public static unsafe class StarApiExports
{
    private static readonly object Sync = new();
    private static readonly object NativeStateLock = new();
    private static StarApiClient? _client;
    private static byte* _lastError;
    private static delegate* unmanaged[Cdecl]<int, void*, void> _callback;
    private static void* _callbackUserData;

    static StarApiExports()
    {
        SetError(string.Empty);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_init", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiInit(star_api_config_t* config)
    {
        if (config is null || config->base_url is null)
            return (int)SetErrorAndReturn("Invalid configuration.", StarApiResultCode.InvalidParam);

        var managedConfig = new StarApiConfig
        {
            BaseUrl = PtrToString(config->base_url) ?? string.Empty,
            ApiKey = PtrToString(config->api_key),
            AvatarId = PtrToString(config->avatar_id),
            TimeoutSeconds = config->timeout_seconds
        };

        lock (Sync)
        {
            _client?.Dispose();
            _client = new StarApiClient();
        }

        var result = _client.Init(managedConfig);
        return (int)FinalizeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_authenticate", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiAuthenticate(sbyte* username, sbyte* password)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var user = PtrToString(username);
        var pass = PtrToString(password);
        var result = client.AuthenticateAsync(user ?? string.Empty, pass ?? string.Empty).GetAwaiter().GetResult();
        return (int)FinalizeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_cleanup", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiCleanup()
    {
        lock (Sync)
        {
            _client?.Dispose();
            _client = null;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_has_item", CallConvs = [typeof(CallConvCdecl)])]
    public static byte StarApiHasItem(sbyte* itemName)
    {
        var client = GetClient();
        if (client is null)
        {
            SetError("Client is not initialized.");
            InvokeCallback(StarApiResultCode.NotInitialized);
            return 0;
        }

        var result = client.HasItemAsync(PtrToString(itemName) ?? string.Empty).GetAwaiter().GetResult();
        var code = FinalizeResult(result);
        return code == StarApiResultCode.Success && result.Result ? (byte)1 : (byte)0;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_get_inventory", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetInventory(star_item_list_t** itemList)
    {
        if (itemList is null)
            return (int)SetErrorAndReturn("itemList must not be null.", StarApiResultCode.InvalidParam);

        *itemList = null;

        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var result = client.GetInventoryAsync().GetAwaiter().GetResult();
        var resultCode = ExtractCode(result);
        if (result.IsError || result.Result is null)
        {
            SetError(result.Message ?? "Failed to load inventory.");
            InvokeCallback(resultCode);
            return (int)resultCode;
        }

        var count = (nuint)result.Result.Count;
        var listPtr = (star_item_list_t*)NativeMemory.Alloc((nuint)1, (nuint)sizeof(star_item_list_t));
        if (listPtr is null)
            return (int)SetErrorAndReturn("Memory allocation failed for item list.", StarApiResultCode.InitFailed);

        listPtr->count = count;
        listPtr->capacity = count;
        listPtr->items = null;

        if (count > 0)
        {
            listPtr->items = (star_item_t*)NativeMemory.Alloc(count, (nuint)sizeof(star_item_t));
            if (listPtr->items is null)
            {
                NativeMemory.Free(listPtr);
                return (int)SetErrorAndReturn("Memory allocation failed for inventory items.", StarApiResultCode.InitFailed);
            }

            for (var i = 0; i < result.Result.Count; i++)
            {
                var src = result.Result[i];
                var dst = &listPtr->items[i];
                WriteFixedUtf8(src.Id.ToString(), dst->id, 64);
                WriteFixedUtf8(src.Name, dst->name, 256);
                WriteFixedUtf8(src.Description, dst->description, 512);
                WriteFixedUtf8(src.GameSource, dst->game_source, 64);
                WriteFixedUtf8(src.ItemType, dst->item_type, 64);
            }
        }

        *itemList = listPtr;
        SetError(string.Empty);
        InvokeCallback(StarApiResultCode.Success);
        return (int)StarApiResultCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_free_item_list", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiFreeItemList(star_item_list_t* itemList)
    {
        if (itemList is null)
            return;

        if (itemList->items is not null)
            NativeMemory.Free(itemList->items);

        NativeMemory.Free(itemList);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_add_item", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiAddItem(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var result = client.AddItemAsync(
            PtrToString(itemName) ?? string.Empty,
            PtrToString(description) ?? string.Empty,
            PtrToString(gameSource) ?? string.Empty,
            PtrToString(itemType) ?? "KeyItem").GetAwaiter().GetResult();

        return (int)FinalizeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_use_item", CallConvs = [typeof(CallConvCdecl)])]
    public static byte StarApiUseItem(sbyte* itemName, sbyte* context)
    {
        var client = GetClient();
        if (client is null)
        {
            SetError("Client is not initialized.");
            InvokeCallback(StarApiResultCode.NotInitialized);
            return 0;
        }

        var result = client.UseItemAsync(PtrToString(itemName) ?? string.Empty, PtrToString(context)).GetAwaiter().GetResult();
        var code = FinalizeResult(result);
        return code == StarApiResultCode.Success && result.Result ? (byte)1 : (byte)0;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_start_quest", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiStartQuest(sbyte* questId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var result = client.StartQuestAsync(PtrToString(questId) ?? string.Empty).GetAwaiter().GetResult();
        return (int)FinalizeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_complete_quest_objective", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiCompleteQuestObjective(sbyte* questId, sbyte* objectiveId, sbyte* gameSource)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var result = client.CompleteQuestObjectiveAsync(
            PtrToString(questId) ?? string.Empty,
            PtrToString(objectiveId) ?? string.Empty,
            PtrToString(gameSource)).GetAwaiter().GetResult();

        return (int)FinalizeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_complete_quest", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiCompleteQuest(sbyte* questId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var result = client.CompleteQuestAsync(PtrToString(questId) ?? string.Empty).GetAwaiter().GetResult();
        return (int)FinalizeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_create_boss_nft", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiCreateBossNft(sbyte* bossName, sbyte* description, sbyte* gameSource, sbyte* bossStats, sbyte* nftIdOut)
    {
        if (nftIdOut is null)
            return (int)SetErrorAndReturn("nftIdOut buffer must not be null.", StarApiResultCode.InvalidParam);

        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var result = client.CreateBossNftAsync(
            PtrToString(bossName) ?? string.Empty,
            PtrToString(description),
            PtrToString(gameSource),
            PtrToString(bossStats)).GetAwaiter().GetResult();

        var code = FinalizeResult(result);
        if (code == StarApiResultCode.Success && !string.IsNullOrWhiteSpace(result.Result))
            WriteUtf8ToOutput(result.Result!, nftIdOut, 64);
        else
            WriteUtf8ToOutput(string.Empty, nftIdOut, 64);

        return (int)code;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_deploy_boss_nft", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiDeployBossNft(sbyte* nftId, sbyte* targetGame, sbyte* location)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var result = client.DeployBossNftAsync(
            PtrToString(nftId) ?? string.Empty,
            PtrToString(targetGame) ?? string.Empty,
            PtrToString(location)).GetAwaiter().GetResult();

        return (int)FinalizeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_get_last_error", CallConvs = [typeof(CallConvCdecl)])]
    public static sbyte* StarApiGetLastError()
    {
        lock (NativeStateLock)
            return (sbyte*)_lastError;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_set_callback", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiSetCallback(delegate* unmanaged[Cdecl]<int, void*, void> callback, void* userData)
    {
        lock (NativeStateLock)
        {
            _callback = callback;
            _callbackUserData = userData;
        }
    }

    private static StarApiClient? GetClient()
    {
        lock (Sync)
            return _client;
    }

    private static StarApiResultCode FinalizeResult<T>(OASISResult<T> result)
    {
        var code = ExtractCode(result);
        if (result.IsError)
            SetError(result.Message ?? "Unknown error.");
        else
            SetError(string.Empty);

        InvokeCallback(code);
        return code;
    }

    private static StarApiResultCode ExtractCode<T>(OASISResult<T> result)
    {
        if (!result.IsError)
            return StarApiResultCode.Success;

        if (!string.IsNullOrWhiteSpace(result.ErrorCode) && int.TryParse(result.ErrorCode, out var code))
            return Enum.IsDefined(typeof(StarApiResultCode), code) ? (StarApiResultCode)code : StarApiResultCode.ApiError;

        return StarApiResultCode.ApiError;
    }

    private static StarApiResultCode SetErrorAndReturn(string message, StarApiResultCode code)
    {
        SetError(message);
        InvokeCallback(code);
        return code;
    }

    private static void SetError(string message)
    {
        var value = message ?? string.Empty;
        var bytes = Encoding.UTF8.GetBytes(value);
        var buffer = (byte*)NativeMemory.Alloc((nuint)bytes.Length + 1);
        if (buffer is null)
            return;

        new ReadOnlySpan<byte>(bytes).CopyTo(new Span<byte>(buffer, bytes.Length));
        buffer[bytes.Length] = 0;

        lock (NativeStateLock)
        {
            var previous = _lastError;
            _lastError = buffer;
            if (previous is not null)
                NativeMemory.Free(previous);
        }
    }

    private static void InvokeCallback(StarApiResultCode code)
    {
        delegate* unmanaged[Cdecl]<int, void*, void> callback;
        void* callbackUserData;

        lock (NativeStateLock)
        {
            callback = _callback;
            callbackUserData = _callbackUserData;
        }

        if (callback != null)
            callback((int)code, callbackUserData);
    }

    private static string? PtrToString(sbyte* ptr)
    {
        return ptr is null ? null : Marshal.PtrToStringUTF8((nint)ptr);
    }

    private static void WriteUtf8ToOutput(string value, sbyte* destination, int size)
    {
        if (destination is null || size <= 0)
            return;

        var buffer = new Span<byte>((byte*)destination, size);
        buffer.Clear();
        if (string.IsNullOrEmpty(value))
            return;

        Encoding.UTF8.GetBytes(value.AsSpan(), buffer[..(size - 1)]);
    }

    private static void WriteFixedUtf8(string value, byte* destination, int size)
    {
        var span = new Span<byte>(destination, size);
        span.Clear();
        if (string.IsNullOrEmpty(value))
            return;

        Encoding.UTF8.GetBytes(value.AsSpan(), span[..(size - 1)]);
    }
}

