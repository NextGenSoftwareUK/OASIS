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
    // WEB5 STAR API base URI (for STAR gameplay/inventory/quest endpoints).
    public string Web5StarApiBaseUrl { get; init; } = string.Empty;
    // WEB4 OASIS API base URI (for avatar auth and NFT mint endpoints). Optional.
    public string? Web4OasisApiBaseUrl { get; init; }
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
    /// <summary>NFT ID from MetaData when item is linked to an NFTHolon (minted). Empty when not an NFT item.</summary>
    public string NftId { get; init; } = string.Empty;
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
    public string Id { get; init; } = string.Empty;
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
    private readonly object _inventoryCacheLock = new();

    /// <summary>Local cache of last loaded inventory. GetInventory/HasItem/UseItem use this first and only hit the API when cache is empty or item not found (for has_item).</summary>
    private List<StarItem>? _cachedInventory;
    /// <summary>Single-flight fetch: when cache is null, only one HTTP get_inventory runs; other callers wait on this task.</summary>
    private Task<OASISResult<List<StarItem>>>? _inventoryFetchTask;

    private HttpClient? _httpClient;
    private bool _initialized;
    // WEB5 STAR API base URI.
    private string _baseApiUrl = string.Empty;
    // WEB4 OASIS API base URI.
    private string _oasisBaseUrl = string.Empty;
    private string? _jwtToken;
    private string? _refreshToken;
    private string? _avatarId;
    private string _lastError = string.Empty;
    private StarApiCallback? _callback;
    private object? _callbackUserData;
    private readonly ConcurrentQueue<PendingAddItemJob> _pendingAddItemJobs = new();
    private readonly ConcurrentQueue<PendingUseItemJob> _pendingUseItemJobs = new();
    private readonly ConcurrentQueue<PendingQuestObjectiveJob> _pendingQuestObjectiveJobs = new();
    private readonly SemaphoreSlim _addItemSignal = new(0);
    private readonly SemaphoreSlim _useItemSignal = new(0);
    private readonly SemaphoreSlim _questObjectiveSignal = new(0);
    private readonly object _jobLock = new();
    private int _activeAddItemJobs;
    private int _activeUseItemJobs;
    private int _activeQuestObjectiveJobs;
    private CancellationTokenSource? _jobCts;
    private Task? _jobWorker;
    private CancellationTokenSource? _useItemJobCts;
    private Task? _useItemJobWorker;
    private CancellationTokenSource? _questObjectiveJobCts;
    private Task? _questObjectiveJobWorker;

    public int AddItemBatchSize { get; set; } = 32;
    public TimeSpan AddItemBatchWindow { get; set; } = TimeSpan.FromMilliseconds(75);
    public int UseItemBatchSize { get; set; } = 32;
    public TimeSpan UseItemBatchWindow { get; set; } = TimeSpan.FromMilliseconds(50);
    public int QuestObjectiveBatchSize { get; set; } = 32;
    public TimeSpan QuestObjectiveBatchWindow { get; set; } = TimeSpan.FromMilliseconds(50);

    public OASISResult<bool> Init(StarApiConfig config)
    {
        var web5BaseUrl = config?.Web5StarApiBaseUrl;
        if (config is null || string.IsNullOrWhiteSpace(web5BaseUrl))
            return Fail<bool>("Invalid configuration.", StarApiResultCode.InvalidParam);

        if (!Uri.TryCreate(web5BaseUrl.TrimEnd('/'), UriKind.Absolute, out var baseUri))
            return Fail<bool>("Web5StarApiBaseUrl must be a valid absolute URL.", StarApiResultCode.InvalidParam);

        var timeout = config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 30;
        var normalizedBaseUrl = baseUri.ToString().TrimEnd('/');
        var oasisBaseUrl = FirstNonEmpty(
            config.Web4OasisApiBaseUrl,
            Environment.GetEnvironmentVariable("OASIS_WEB4_API_BASE_URL"),
            normalizedBaseUrl)!.TrimEnd('/');
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
            _jwtToken = string.IsNullOrWhiteSpace(config.ApiKey) ? null : config.ApiKey;
            _refreshToken = null;
            _lastError = string.Empty;
            _initialized = true;
            _cachedInventory = null;
            _inventoryFetchTask = null;

            if (!string.IsNullOrWhiteSpace(config.ApiKey))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        }

        StartWorkers();

        return Success(true, StarApiResultCode.Success, "WEB5 STAR API client initialized successfully.");
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

            // Some WEB4 OASIS API payloads wrap auth properties multiple levels deep.
            // Parse directly from raw JSON as a fallback to ensure JWT/avatar id are captured.
            try
            {
                using var rawDoc = JsonDocument.Parse(response.Result);
                var rawJwt = FindStringRecursive(rawDoc.RootElement, "JwtToken") ?? FindStringRecursive(rawDoc.RootElement, "Token")
                    ?? FindStringRecursive(rawDoc.RootElement, "accessToken") ?? FindStringRecursive(rawDoc.RootElement, "access_token");
                var rawRefresh = FindStringRecursive(rawDoc.RootElement, "RefreshToken");
                var rawId = FindStringRecursive(rawDoc.RootElement, "Id") ?? FindStringRecursive(rawDoc.RootElement, "AvatarId");

                if (string.IsNullOrWhiteSpace(auth.JwtToken) && !string.IsNullOrWhiteSpace(rawJwt))
                    auth.JwtToken = rawJwt;
                if (string.IsNullOrWhiteSpace(auth.RefreshToken) && !string.IsNullOrWhiteSpace(rawRefresh))
                    auth.RefreshToken = rawRefresh;
                if (auth.Id == Guid.Empty && Guid.TryParse(rawId, out var parsedRawId))
                    auth.Id = parsedRawId;
            }
            catch
            {
                // Keep parsed envelope values if raw parsing fails.
            }

            // Keep local WEB5 STAR API session state in sync after WEB4 OASIS authentication.
            // Some local controllers resolve avatar context from their own auth flow.
            try
            {
                // Ensure WEB5 STAR API runtime is ignited before using manager-backed routes.
                var starStatusResponse = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/star/status", null, cancellationToken).ConfigureAwait(false);
                if (!starStatusResponse.IsError)
                {
                    var needsIgnite = true;
                    try
                    {
                        using var statusDoc = JsonDocument.Parse(starStatusResponse.Result);
                        if (statusDoc.RootElement.ValueKind == JsonValueKind.Object &&
                            statusDoc.RootElement.TryGetProperty("isIgnited", out var ignitedProp) &&
                            ignitedProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        {
                            needsIgnite = !ignitedProp.GetBoolean();
                        }
                    }
                    catch
                    {
                        needsIgnite = true;
                    }

                    if (needsIgnite)
                    {
                        var ignitePayload = BuildJson(writer =>
                        {
                            writer.WriteStartObject();
                            writer.WriteString("userName", username);
                            writer.WriteString("password", password);
                            writer.WriteEndObject();
                        });

                        _ = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/star/ignite", ignitePayload, cancellationToken).ConfigureAwait(false);
                    }
                }

                var web5AuthResponse = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/avatar/authenticate", payload, cancellationToken).ConfigureAwait(false);
                if (!web5AuthResponse.IsError)
                {
                    var web5Parsed = ParseEnvelopeOrPayload(web5AuthResponse.Result, out var web5ResultElement, out _, out _);
                    if (web5Parsed)
                    {
                        var web5Auth = ParseAvatarAuthResponse(web5ResultElement);
                        if (web5Auth is not null)
                        {
                            if (string.IsNullOrWhiteSpace(auth.JwtToken) && !string.IsNullOrWhiteSpace(web5Auth.JwtToken))
                                auth.JwtToken = web5Auth.JwtToken;
                            if (string.IsNullOrWhiteSpace(auth.RefreshToken) && !string.IsNullOrWhiteSpace(web5Auth.RefreshToken))
                                auth.RefreshToken = web5Auth.RefreshToken;
                            if (auth.Id == Guid.Empty && web5Auth.Id != Guid.Empty)
                                auth.Id = web5Auth.Id;
                        }
                    }
                }
            }
            catch
            {
                // Best effort only: WEB4 auth remains the source of truth.
            }

            if (auth.Id == Guid.Empty)
            {
                var jwtAvatarId = ExtractAvatarIdFromJwt(auth.JwtToken);
                if (jwtAvatarId != Guid.Empty)
                    auth.Id = jwtAvatarId;
            }

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

    /// <summary>Set avatar ID for subsequent API calls (e.g. after SSO when C++ has avatar_id from auth result). Does not change JWT.</summary>
    public OASISResult<bool> SetAvatarId(string avatarId)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        lock (_stateLock)
            _avatarId = string.IsNullOrWhiteSpace(avatarId) ? null : avatarId;

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Avatar ID set.");
    }

    public OASISResult<bool> SetWeb4OasisApiBaseUrl(string web4OasisApiBaseUrl)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(web4OasisApiBaseUrl) || !Uri.TryCreate(web4OasisApiBaseUrl.TrimEnd('/'), UriKind.Absolute, out var uri))
            return FailAndCallback<bool>("A valid OASIS WEB4 API base URL is required.", StarApiResultCode.InvalidParam);

        var normalized = uri.ToString().TrimEnd('/');
        var apiIndex = normalized.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
        if (apiIndex >= 0)
            normalized = normalized[..apiIndex];

        lock (_stateLock)
            _oasisBaseUrl = normalized;

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "WEB4 OASIS API base URL updated.");
    }

    public OASISResult<bool> SetWeb5StarApiBaseUrl(string web5StarApiBaseUrl)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(web5StarApiBaseUrl) || !Uri.TryCreate(web5StarApiBaseUrl.TrimEnd('/'), UriKind.Absolute, out var uri))
            return FailAndCallback<bool>("A valid WEB5 STAR API base URL is required.", StarApiResultCode.InvalidParam);

        var normalized = uri.ToString().TrimEnd('/');
        lock (_stateLock)
        {
            _baseApiUrl = normalized;
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "WEB5 STAR API base URL updated.");
    }

    public async Task<OASISResult<StarAvatarProfile>> GetCurrentAvatarAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarAvatarProfile>("Client is not initialized.", StarApiResultCode.NotInitialized);

        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/avatar/current", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            lock (_stateLock)
            {
                if (Guid.TryParse(_avatarId, out var cachedId) && cachedId != Guid.Empty)
                {
                    InvokeCallback(StarApiResultCode.Success);
                    return Success(new StarAvatarProfile { Id = cachedId }, StarApiResultCode.Success, "Avatar resolved from authenticated session.");
                }
            }

            return FailAndCallback<StarAvatarProfile>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

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
        StopWorkers();

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

        return Success(true, StarApiResultCode.Success, "WEB5 STAR API client cleaned up.");
    }

    /// <summary>Check if the avatar has an item by name. Uses local cache first; only hits the API when cache is null (e.g. first load).</summary>
    public async Task<OASISResult<bool>> HasItemAsync(string itemName, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<bool>("Item name is required.", StarApiResultCode.InvalidParam);

        lock (_inventoryCacheLock)
        {
            if (_cachedInventory is not null)
            {
                var hasItem = _cachedInventory.Any(x =>
                    string.Equals(x.Name, itemName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Description, itemName, StringComparison.OrdinalIgnoreCase));
                InvokeCallback(StarApiResultCode.Success);
                return Success(hasItem, StarApiResultCode.Success, hasItem ? "Item found in inventory (cached)." : "Item not found in inventory.");
            }
        }

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

        var found = inventory.Result!.Any(x =>
            string.Equals(x.Name, itemName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Description, itemName, StringComparison.OrdinalIgnoreCase));

        InvokeCallback(StarApiResultCode.Success);
        return Success(found, StarApiResultCode.Success, found ? "Item found in inventory." : "Item not found in inventory.");
    }

    /// <summary>Get avatar inventory. Returns the local cache when available; only one HTTP fetch runs when cache is null (single-flight).</summary>
    public async Task<OASISResult<List<StarItem>>> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarItem>>("Client is not initialized.", StarApiResultCode.NotInitialized);

        Task<OASISResult<List<StarItem>>>? task;
        lock (_inventoryCacheLock)
        {
            if (_cachedInventory is not null)
            {
                var copy = new List<StarItem>(_cachedInventory);
                InvokeCallback(StarApiResultCode.Success);
                return Success(copy, StarApiResultCode.Success, $"Loaded {copy.Count} item(s) (cached).");
            }
            if (_inventoryFetchTask is null)
                _inventoryFetchTask = FetchInventoryOnceAsync();
            task = _inventoryFetchTask;
        }

        var result = await task.ConfigureAwait(false);
        lock (_inventoryCacheLock)
        {
            _inventoryFetchTask = null;
            if (result.Result is not null)
                _cachedInventory = new List<StarItem>(result.Result);
        }
        return result;
    }

    private async Task<OASISResult<List<StarItem>>> FetchInventoryOnceAsync()
    {
        var avatarIdResult = await EnsureAvatarIdAsync(CancellationToken.None).ConfigureAwait(false);
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
            var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/avatar/inventory", null, CancellationToken.None).ConfigureAwait(false);
            if (response.IsError && ParseCode(response.ErrorCode, StarApiResultCode.ApiError) == StarApiResultCode.Network)
            {
                await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
                response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/avatar/inventory", null, CancellationToken.None).ConfigureAwait(false);
            }
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

    /// <summary>Clear the local inventory cache. Next GetInventory/HasItem will hit the API. Call after external inventory changes if needed.</summary>
    public void InvalidateInventoryCache()
    {
        lock (_inventoryCacheLock)
        {
            _cachedInventory = null;
            _inventoryFetchTask = null;
        }
    }

    public async Task<OASISResult<StarItem>> AddItemAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", string? nftId = null, CancellationToken cancellationToken = default)
    {
        return await AddItemCoreAsync(itemName, description, gameSource, itemType, nftId, cancellationToken).ConfigureAwait(false);
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

        while ((!_pendingAddItemJobs.IsEmpty || Volatile.Read(ref _activeAddItemJobs) > 0) && !cancellationToken.IsCancellationRequested)
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
            return FailAndCallback<bool>("Flush add-item jobs was cancelled.", StarApiResultCode.Network);

        return Success(true, StarApiResultCode.Success, "Add-item queue flushed.");
    }

    private async Task<OASISResult<StarItem>> AddItemCoreAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", string? nftId = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarItem>("Client is not initialized.", StarApiResultCode.NotInitialized);

        string? avatarId;
        lock (_stateLock)
            avatarId = _avatarId;
        if (string.IsNullOrWhiteSpace(avatarId))
            return FailAndCallback<StarItem>("Avatar ID is not set. Complete beam-in (authenticate) first; add_item requires avatar context.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(gameSource))
            return FailAndCallback<StarItem>("Item name, description, and game source are required.", StarApiResultCode.InvalidParam);

        try
        {
            var payload = BuildJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("Name", itemName);
                writer.WriteString("Description", $"{description} | Source: {gameSource}");
                writer.WriteNumber("HolonType", 11);
                writer.WritePropertyName("MetaData");
                writer.WriteStartObject();
                writer.WriteString("GameSource", gameSource);
                writer.WriteString("ItemType", string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType);
                writer.WriteBoolean("CrossGameItem", true);
                writer.WriteString("CollectedAt", DateTime.UtcNow.ToString("O"));
                if (!string.IsNullOrWhiteSpace(nftId))
                    writer.WriteString("NFTId", nftId);
                writer.WriteEndObject();
                writer.WriteEndObject();
            });

            var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/avatar/inventory", payload, cancellationToken).ConfigureAwait(false);
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
                ItemType = ExtractMeta(item.MetaData, "ItemType", string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType),
                NftId = ExtractMeta(item.MetaData, "NFTId", string.Empty) ?? ExtractMeta(item.MetaData, "OASISNFTId", string.Empty) ?? string.Empty
            };

            lock (_inventoryCacheLock)
            {
                _cachedInventory ??= new List<StarItem>();
                _cachedInventory.Add(mapped);
            }

            InvokeCallback(StarApiResultCode.Success);
            return Success(mapped, StarApiResultCode.Success, "Item added successfully.");
        }
        catch (Exception ex)
        {
            return FailAndCallback<StarItem>($"Failed to add item: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    private void RemoveFromInventoryCache(string itemName, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0) return;
        lock (_inventoryCacheLock)
        {
            if (_cachedInventory is null || _cachedInventory.Count == 0) return;
            var removed = 0;
            _cachedInventory.RemoveAll(x =>
            {
                if (removed >= quantity) return false;
                var match = string.Equals(x.Name, itemName, StringComparison.OrdinalIgnoreCase);
                if (match) removed++;
                return match;
            });
        }
    }

    /// <summary>Record use of an item in a context (e.g. door). For optimization, prefer deciding access from the already-loaded inventory (local cache) and only call this when you need to record use or when cache is unavailable.</summary>
    public async Task<OASISResult<bool>> UseItemAsync(string itemName, string? context = null, CancellationToken cancellationToken = default)
    {
        return await UseItemCoreAsync(itemName, context, cancellationToken).ConfigureAwait(false);
    }

    public Task<OASISResult<bool>> QueueUseItemAsync(string itemName, string? context = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized));

        if (string.IsNullOrWhiteSpace(itemName))
            return Task.FromResult(FailAndCallback<bool>("Item name is required.", StarApiResultCode.InvalidParam));

        var tcs = new TaskCompletionSource<OASISResult<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingUseItemJobs.Enqueue(new PendingUseItemJob(itemName, context, cancellationToken, tcs));
        _useItemSignal.Release();
        return tcs.Task;
    }

    public async Task<OASISResult<bool>> FlushUseItemJobsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        while ((!_pendingUseItemJobs.IsEmpty || Volatile.Read(ref _activeUseItemJobs) > 0) && !cancellationToken.IsCancellationRequested)
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
            return FailAndCallback<bool>("Flush use-item jobs was cancelled.", StarApiResultCode.Network);

        return Success(true, StarApiResultCode.Success, "Use-item queue flushed.");
    }

    private async Task<OASISResult<bool>> UseItemCoreAsync(string itemName, string? context = null, CancellationToken cancellationToken = default)
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

            var response = await SendRawAsync(HttpMethod.Delete, $"{_baseApiUrl}/api/avatar/inventory/{item.Id}", payload, cancellationToken).ConfigureAwait(false);
            if (response.IsError)
                return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

            RemoveFromInventoryCache(itemName, 1);
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
        return await CompleteQuestObjectiveCoreAsync(questId, objectiveId, gameSource, cancellationToken).ConfigureAwait(false);
    }

    public Task<OASISResult<bool>> QueueCompleteQuestObjectiveAsync(string questId, string objectiveId, string? gameSource = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized));

        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId))
            return Task.FromResult(FailAndCallback<bool>("Quest ID and objective ID are required.", StarApiResultCode.InvalidParam));

        var tcs = new TaskCompletionSource<OASISResult<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingQuestObjectiveJobs.Enqueue(new PendingQuestObjectiveJob(questId, objectiveId, gameSource, cancellationToken, tcs));
        _questObjectiveSignal.Release();
        return tcs.Task;
    }

    public async Task<OASISResult<bool>> FlushQuestObjectiveJobsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        while ((!_pendingQuestObjectiveJobs.IsEmpty || Volatile.Read(ref _activeQuestObjectiveJobs) > 0) && !cancellationToken.IsCancellationRequested)
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
            return FailAndCallback<bool>("Flush quest objective jobs was cancelled.", StarApiResultCode.Network);

        return Success(true, StarApiResultCode.Success, "Quest objective queue flushed.");
    }

    private async Task<OASISResult<bool>> CompleteQuestObjectiveCoreAsync(string questId, string objectiveId, string? gameSource = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId))
            return FailAndCallback<bool>("Quest ID and objective ID are required.", StarApiResultCode.InvalidParam);

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("gameSource", string.IsNullOrWhiteSpace(gameSource) ? "Unknown" : gameSource);
            writer.WriteString("completionNotes", $"Completed objective {objectiveId} at {DateTime.UtcNow:O}");
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/{questId}/objectives/{objectiveId}/complete", payload, cancellationToken).ConfigureAwait(false);
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
            writer.WriteNumber("HolonSubType", 7);
            writer.WriteString("SourceFolderPath", string.Empty);
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
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/create", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Cross-game quest created successfully.");
    }

    public async Task<OASISResult<List<StarQuestInfo>>> GetActiveQuestsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarQuestInfo>>("Client is not initialized.", StarApiResultCode.NotInitialized);

        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/quests/by-status/InProgress", null, cancellationToken).ConfigureAwait(false);
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

        string? sendToAvatarAfterMintingId = null;
        lock (_stateLock)
        {
            if (Guid.TryParse(_avatarId, out var avatarGuid) && avatarGuid != Guid.Empty)
                sendToAvatarAfterMintingId = avatarGuid.ToString();
        }

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("title", bossName);
            writer.WriteString("description", string.IsNullOrWhiteSpace(description) ? "Boss from game" : description);
            writer.WriteString("symbol", "BOSS");
            writer.WriteString("image", "AQ==");
            writer.WriteString("imageUrl", "https://oasisweb4.one/images/star/default-boss.png");
            writer.WriteString("thumbnail", "AQ==");
            writer.WriteString("thumbnailUrl", "https://oasisweb4.one/images/star/default-boss-thumb.png");
            writer.WriteString("memoText", "Minted by WEB5 STAR API Client");
            writer.WriteNumber("numberToMint", 1);
            writer.WriteBoolean("storeNFTMetaDataOnChain", false);
            writer.WriteString("offChainProvider", "MongoDBOASIS");
            writer.WriteString("onChainProvider", "ArbitrumOASIS");
            writer.WriteString("nftOffChainMetaType", "ExternalJSONURL");
            writer.WriteString("JSONMetaDataURL", "https://oasisweb4.one/metadata/star/default-boss.json");
            writer.WriteString("nftStandardType", "ERC1155");
            if (!string.IsNullOrWhiteSpace(sendToAvatarAfterMintingId))
                writer.WriteString("sendToAvatarAfterMintingId", sendToAvatarAfterMintingId);
            writer.WritePropertyName("metaData");
            writer.WriteStartObject();
            writer.WriteString("GameSource", string.IsNullOrWhiteSpace(gameSource) ? "Unknown" : gameSource);
            writer.WritePropertyName("BossStats");
            bossStatsElement.WriteTo(writer);
            writer.WriteString("DefeatedAt", DateTime.UtcNow.ToString("O"));
            writer.WriteBoolean("Deployable", true);
            writer.WriteEndObject();
            writer.WriteBoolean("waitTillNFTMinted", false);
            writer.WriteNumber("waitForNFTToMintInSeconds", 10);
            writer.WriteNumber("attemptToMintEveryXSeconds", 1);
            writer.WriteBoolean("waitTillNFTSent", false);
            writer.WriteNumber("waitForNFTToSendInSeconds", 30);
            writer.WriteNumber("attemptToSendEveryXSeconds", 1);
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_oasisBaseUrl}/api/nft/mint-nft", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            if (TryExtractTopLevelResultId(response.Result, out var warningMintId))
            {
                InvokeCallback(StarApiResultCode.Success);
                return Success(warningMintId!, StarApiResultCode.Success, $"Boss NFT created with warnings: {response.Message}");
            }

            return FailAndCallback<string>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            if (TryExtractTopLevelResultId(response.Result, out var warningMintId))
            {
                InvokeCallback(StarApiResultCode.Success);
                return Success(warningMintId!, StarApiResultCode.Success, $"Boss NFT created with warnings: {parseErrorMessage}");
            }

            return FailAndCallback<string>(parseErrorMessage, parseErrorCode);
        }

        var nftId = ParseIdAsString(resultElement);
        if (string.IsNullOrWhiteSpace(nftId))
            return FailAndCallback<string>("API did not return an NFT ID.", StarApiResultCode.ApiError);

        InvokeCallback(StarApiResultCode.Success);
        return Success(nftId, StarApiResultCode.Success, "Boss NFT created successfully.");
    }

    /// <summary>Mint an NFT for an inventory item (creates NFTHolon on WEB4). Returns NFT ID to store in InventoryItem MetaData.NFTId. Default provider: SolanaOASIS.</summary>
    public async Task<OASISResult<string>> MintInventoryItemNftAsync(string itemName, string? description, string gameSource, string itemType = "KeyItem", string? provider = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<string>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<string>("Item name is required.", StarApiResultCode.InvalidParam);

        var onChainProvider = string.IsNullOrWhiteSpace(provider) ? "SolanaOASIS" : provider;
        string? sendToAvatarAfterMintingId = null;
        lock (_stateLock)
        {
            if (Guid.TryParse(_avatarId, out var avatarGuid) && avatarGuid != Guid.Empty)
                sendToAvatarAfterMintingId = avatarGuid.ToString();
        }

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("title", itemName);
            writer.WriteString("description", string.IsNullOrWhiteSpace(description) ? $"Inventory item: {itemName}" : description);
            writer.WriteString("symbol", "STARITEM");
            writer.WriteString("image", "AQ==");
            writer.WriteString("imageUrl", "https://oasisweb4.one/images/star/default-item.png");
            writer.WriteString("thumbnail", "AQ==");
            writer.WriteString("thumbnailUrl", "https://oasisweb4.one/images/star/default-item-thumb.png");
            writer.WriteString("memoText", "Minted by STAR API (inventory item)");
            writer.WriteNumber("numberToMint", 1);
            writer.WriteBoolean("storeNFTMetaDataOnChain", false);
            writer.WriteString("offChainProvider", "MongoDBOASIS");
            writer.WriteString("onChainProvider", onChainProvider);
            writer.WriteString("nftOffChainMetaType", "ExternalJSONURL");
            writer.WriteString("JSONMetaDataURL", "https://oasisweb4.one/metadata/star/default-item.json");
            writer.WriteString("nftStandardType", "ERC1155");
            if (!string.IsNullOrWhiteSpace(sendToAvatarAfterMintingId))
                writer.WriteString("sendToAvatarAfterMintingId", sendToAvatarAfterMintingId);
            writer.WritePropertyName("metaData");
            writer.WriteStartObject();
            writer.WriteString("GameSource", string.IsNullOrWhiteSpace(gameSource) ? "Unknown" : gameSource);
            writer.WriteString("ItemType", string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType);
            writer.WriteString("ItemName", itemName);
            writer.WriteString("MintedAt", DateTime.UtcNow.ToString("O"));
            writer.WriteEndObject();
            writer.WriteBoolean("waitTillNFTMinted", false);
            writer.WriteNumber("waitForNFTToMintInSeconds", 10);
            writer.WriteBoolean("waitTillNFTSent", false);
            writer.WriteNumber("waitForNFTToSendInSeconds", 30);
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_oasisBaseUrl}/api/nft/mint-nft", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            if (TryExtractTopLevelResultId(response.Result, out var warningMintId))
                return Success(warningMintId!, StarApiResultCode.Success, $"Inventory item NFT created with warnings: {response.Message}");
            return FailAndCallback<string>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            if (TryExtractTopLevelResultId(response.Result, out var warningMintId))
                return Success(warningMintId!, StarApiResultCode.Success, $"Inventory item NFT created with warnings: {parseErrorMessage}");
            return FailAndCallback<string>(parseErrorMessage, parseErrorCode);
        }

        var nftId = ParseIdAsString(resultElement);
        if (string.IsNullOrWhiteSpace(nftId))
            return FailAndCallback<string>("API did not return an NFT ID.", StarApiResultCode.ApiError);

        InvokeCallback(StarApiResultCode.Success);
        return Success(nftId, StarApiResultCode.Success, "Inventory item NFT minted successfully.");
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

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/nfts/{nftId}/activate", payload, cancellationToken).ConfigureAwait(false);
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

        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/nfts/load-all-for-avatar", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<List<StarNftInfo>>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<List<StarNftInfo>>(parseErrorMessage, parseErrorCode);

        var nfts = ParseNftInfos(resultElement);
        InvokeCallback(StarApiResultCode.Success);
        return Success(nfts, StarApiResultCode.Success, $"Loaded {nfts.Count} NFT(s).");
    }

    /// <summary>Sends an item from the current avatar's inventory to another avatar. Target is username or avatar Id. Optionally pass itemId (Guid) to send that specific item. Works for all items (STAR and local).</summary>
    public async Task<OASISResult<bool>> SendItemToAvatarAsync(string targetUsernameOrAvatarId, string itemName, int quantity = 1, Guid? itemId = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (string.IsNullOrWhiteSpace(targetUsernameOrAvatarId) || string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<bool>("Target and item name are required.", StarApiResultCode.InvalidParam);
        if (quantity < 1) quantity = 1;

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Target", targetUsernameOrAvatarId.Trim());
            writer.WriteString("ItemName", itemName.Trim());
            if (itemId.HasValue && itemId.Value != Guid.Empty)
                writer.WriteString("ItemId", itemId.Value.ToString());
            writer.WriteNumber("Quantity", quantity);
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/avatar/inventory/send-to-avatar", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out _, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<bool>(parseErrorMessage, parseErrorCode);

        RemoveFromInventoryCache(itemName, quantity);
        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Item sent to avatar.");
    }

    /// <summary>Sends an item from the current avatar's inventory to a clan. Target is clan name (or username). Optionally pass itemId (Guid) to send that specific item. Works for all items (STAR and local).</summary>
    public async Task<OASISResult<bool>> SendItemToClanAsync(string clanNameOrTargetUsername, string itemName, int quantity = 1, Guid? itemId = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (string.IsNullOrWhiteSpace(clanNameOrTargetUsername) || string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<bool>("Clan name (or target) and item name are required.", StarApiResultCode.InvalidParam);
        if (quantity < 1) quantity = 1;

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Target", clanNameOrTargetUsername.Trim());
            writer.WriteString("ItemName", itemName.Trim());
            if (itemId.HasValue && itemId.Value != Guid.Empty)
                writer.WriteString("ItemId", itemId.Value.ToString());
            writer.WriteNumber("Quantity", quantity);
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/avatar/inventory/send-to-clan", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            var msg = response.Message ?? string.Empty;
            if (msg.IndexOf("avatar", StringComparison.OrdinalIgnoreCase) >= 0)
                msg = "Clan not found.";
            return FailAndCallback<bool>(msg, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out _, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            var msg = parseErrorMessage ?? string.Empty;
            if (msg.IndexOf("avatar", StringComparison.OrdinalIgnoreCase) >= 0)
                msg = "Clan not found.";
            return FailAndCallback<bool>(msg, parseErrorCode);
        }

        RemoveFromInventoryCache(itemName, quantity);
        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Item sent to clan.");
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
            
            lock (_stateLock)
            {
                if (!string.IsNullOrWhiteSpace(_avatarId))
                    request.Headers.TryAddWithoutValidation("X-Avatar-Id", _avatarId);

                /* Always set Authorization on the request so it is sent (some gateways return 405 when missing). */
                var bearerToken = _jwtToken;
                if (string.IsNullOrWhiteSpace(bearerToken) && _httpClient.DefaultRequestHeaders.Authorization?.Scheme == "Bearer")
                    bearerToken = _httpClient.DefaultRequestHeaders.Authorization.Parameter;
                if (!string.IsNullOrWhiteSpace(bearerToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            /* Read as bytes then decode to avoid "Error copying to stream" when connection is closed mid-transfer. */
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var responseBody = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : string.Empty;

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
            var msg = ex.InnerException != null && !string.IsNullOrWhiteSpace(ex.InnerException.Message)
                ? $"Network call failed: {ex.Message} ({ex.InnerException.Message})"
                : $"Network call failed: {ex.Message}";
            return Fail<string>(msg, StarApiResultCode.Network, ex);
        }
    }

    private async Task<OASISResult<string>> EnsureAvatarIdAsync(CancellationToken cancellationToken)
    {
        lock (_stateLock)
        {
            if (!string.IsNullOrWhiteSpace(_avatarId))
                return Success(_avatarId!, StarApiResultCode.Success, "Avatar ID already available.");
        }

        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/avatar/current", null, cancellationToken).ConfigureAwait(false);
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
                ItemType = ExtractMeta(item.MetaData, "ItemType", "Miscellaneous"),
                NftId = ExtractMeta(item.MetaData, "NFTId", string.Empty) ?? ExtractMeta(item.MetaData, "OASISNFTId", string.Empty) ?? string.Empty
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

        var idText = GetStringProperty(element, "Id")
            ?? GetStringProperty(element, "AvatarId")
            ?? FindStringRecursive(element, "Id")
            ?? FindStringRecursive(element, "AvatarId");
        Guid.TryParse(idText, out var id);
        var jwt = GetStringProperty(element, "JwtToken") ?? FindStringRecursive(element, "JwtToken")
            ?? GetStringProperty(element, "Token") ?? FindStringRecursive(element, "Token")
            ?? GetStringProperty(element, "accessToken") ?? FindStringRecursive(element, "accessToken")
            ?? GetStringProperty(element, "access_token") ?? FindStringRecursive(element, "access_token");
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

    private static string? FirstNonEmpty(params string?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return null;
    }

    private void StartWorkers()
    {
        StartAddItemWorker();
        StartUseItemWorker();
        StartQuestObjectiveWorker();
    }

    private void StopWorkers()
    {
        StopAddItemWorker();
        StopUseItemWorker();
        StopQuestObjectiveWorker();
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

    private void StartUseItemWorker()
    {
        lock (_jobLock)
        {
            if (_useItemJobWorker is { IsCompleted: false })
                return;

            _useItemJobCts = new CancellationTokenSource();
            _useItemJobWorker = Task.Run(() => ProcessUseItemJobsAsync(_useItemJobCts.Token));
        }
    }

    private void StopUseItemWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_jobLock)
        {
            cts = _useItemJobCts;
            worker = _useItemJobWorker;
            _useItemJobCts = null;
            _useItemJobWorker = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _useItemSignal.Release();
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

        while (_pendingUseItemJobs.TryDequeue(out var pending))
            pending.Completion.TrySetResult(Fail<bool>("Use-item queue stopped.", StarApiResultCode.NotInitialized));
    }

    private void StartQuestObjectiveWorker()
    {
        lock (_jobLock)
        {
            if (_questObjectiveJobWorker is { IsCompleted: false })
                return;

            _questObjectiveJobCts = new CancellationTokenSource();
            _questObjectiveJobWorker = Task.Run(() => ProcessQuestObjectiveJobsAsync(_questObjectiveJobCts.Token));
        }
    }

    private void StopQuestObjectiveWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_jobLock)
        {
            cts = _questObjectiveJobCts;
            worker = _questObjectiveJobWorker;
            _questObjectiveJobCts = null;
            _questObjectiveJobWorker = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _questObjectiveSignal.Release();
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

        while (_pendingQuestObjectiveJobs.TryDequeue(out var pending))
            pending.Completion.TrySetResult(Fail<bool>("Quest objective queue stopped.", StarApiResultCode.NotInitialized));
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

                Interlocked.Increment(ref _activeAddItemJobs);
                try
                {
                    var result = await AddItemCoreAsync(job.ItemName, job.Description, job.GameSource, job.ItemType, null, job.CancellationToken).ConfigureAwait(false);
                    job.Completion.TrySetResult(result);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeAddItemJobs);
                }
            }
        }
    }

    private async Task ProcessUseItemJobsAsync(CancellationToken cancellationToken)
    {
        var batch = new List<PendingUseItemJob>(Math.Max(1, UseItemBatchSize));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _useItemSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            batch.Clear();
            while (_pendingUseItemJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, UseItemBatchSize))
                batch.Add(pending);

            if (batch.Count == 0)
                continue;

            if (UseItemBatchWindow > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(UseItemBatchWindow, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                while (_pendingUseItemJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, UseItemBatchSize))
                    batch.Add(pending);
            }

            foreach (var job in batch)
            {
                if (job.CancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    job.Completion.TrySetResult(Fail<bool>("Queued use-item job was cancelled.", StarApiResultCode.Network));
                    continue;
                }

                Interlocked.Increment(ref _activeUseItemJobs);
                try
                {
                    var result = await UseItemCoreAsync(job.ItemName, job.Context, job.CancellationToken).ConfigureAwait(false);
                    job.Completion.TrySetResult(result);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeUseItemJobs);
                }
            }
        }
    }

    private async Task ProcessQuestObjectiveJobsAsync(CancellationToken cancellationToken)
    {
        var batch = new List<PendingQuestObjectiveJob>(Math.Max(1, QuestObjectiveBatchSize));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _questObjectiveSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            batch.Clear();
            while (_pendingQuestObjectiveJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, QuestObjectiveBatchSize))
                batch.Add(pending);

            if (batch.Count == 0)
                continue;

            if (QuestObjectiveBatchWindow > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(QuestObjectiveBatchWindow, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                while (_pendingQuestObjectiveJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, QuestObjectiveBatchSize))
                    batch.Add(pending);
            }

            foreach (var job in batch)
            {
                if (job.CancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    job.Completion.TrySetResult(Fail<bool>("Queued quest objective job was cancelled.", StarApiResultCode.Network));
                    continue;
                }

                Interlocked.Increment(ref _activeQuestObjectiveJobs);
                try
                {
                    var result = await CompleteQuestObjectiveCoreAsync(job.QuestId, job.ObjectiveId, job.GameSource, job.CancellationToken).ConfigureAwait(false);
                    job.Completion.TrySetResult(result);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeQuestObjectiveJobs);
                }
            }
        }
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
                        Id = GetStringProperty(objective, "Id") ?? string.Empty,
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

    private sealed class PendingUseItemJob
    {
        public PendingUseItemJob(string itemName, string? context, CancellationToken cancellationToken, TaskCompletionSource<OASISResult<bool>> completion)
        {
            ItemName = itemName;
            Context = context;
            CancellationToken = cancellationToken;
            Completion = completion;
        }

        public string ItemName { get; }
        public string? Context { get; }
        public CancellationToken CancellationToken { get; }
        public TaskCompletionSource<OASISResult<bool>> Completion { get; }
    }

    private sealed class PendingQuestObjectiveJob
    {
        public PendingQuestObjectiveJob(string questId, string objectiveId, string? gameSource, CancellationToken cancellationToken, TaskCompletionSource<OASISResult<bool>> completion)
        {
            QuestId = questId;
            ObjectiveId = objectiveId;
            GameSource = gameSource;
            CancellationToken = cancellationToken;
            Completion = completion;
        }

        public string QuestId { get; }
        public string ObjectiveId { get; }
        public string? GameSource { get; }
        public CancellationToken CancellationToken { get; }
        public TaskCompletionSource<OASISResult<bool>> Completion { get; }
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
    public fixed byte nft_id[128];
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
            Web5StarApiBaseUrl = PtrToString(config->base_url) ?? string.Empty,
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

    /// <summary>Native export for star_api_has_item. Prefer checking already-loaded inventory (local cache) for optimization; use this as last resort.</summary>
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
                WriteFixedUtf8(src.NftId ?? string.Empty, dst->nft_id, 128);
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
    public static int StarApiAddItem(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, sbyte* nftId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var nftIdStr = PtrToString(nftId);
        var result = client.AddItemAsync(
            PtrToString(itemName) ?? string.Empty,
            PtrToString(description) ?? string.Empty,
            PtrToString(gameSource) ?? string.Empty,
            PtrToString(itemType) ?? "KeyItem",
            string.IsNullOrWhiteSpace(nftIdStr) ? null : nftIdStr).GetAwaiter().GetResult();

        return (int)FinalizeResult(result);
    }

    /// <summary>Mint an NFT for an inventory item (WEB4 NFTHolon). Returns NFT ID to pass to star_api_add_item as nft_id. provider defaults to SolanaOASIS.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_mint_inventory_nft", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiMintInventoryNft(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, sbyte* provider, sbyte* nftIdOut)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (nftIdOut is null)
            return (int)SetErrorAndReturn("nftIdOut buffer must not be null.", StarApiResultCode.InvalidParam);

        var result = client.MintInventoryItemNftAsync(
            PtrToString(itemName) ?? string.Empty,
            PtrToString(description),
            PtrToString(gameSource) ?? string.Empty,
            PtrToString(itemType) ?? "KeyItem",
            PtrToString(provider)).GetAwaiter().GetResult();

        if (result.IsError)
        {
            SetError(result.Message ?? "Mint failed.");
            InvokeCallback(ExtractCode(result));
            return (int)ExtractCode(result);
        }

        WriteUtf8ToOutput(result.Result ?? string.Empty, nftIdOut, 128);
        InvokeCallback(StarApiResultCode.Success);
        return (int)StarApiResultCode.Success;
    }

    /// <summary>Native export for star_api_use_item. Prefer deciding access from already-loaded inventory (local cache); use this when recording use or when cache unavailable.</summary>
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

    /// <summary>Send item to avatar. Uses the client's HTTP timeout (no extra cancellation).</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_send_item_to_avatar", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSendItemToAvatar(sbyte* targetUsernameOrAvatarId, sbyte* itemName, int quantity, sbyte* itemId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var idStr = PtrToString(itemId);
        Guid? guid = Guid.TryParse(idStr ?? string.Empty, out var g) && g != Guid.Empty ? g : null;
        var result = client.SendItemToAvatarAsync(
            PtrToString(targetUsernameOrAvatarId) ?? string.Empty,
            PtrToString(itemName) ?? string.Empty,
            quantity < 1 ? 1 : quantity,
            guid,
            CancellationToken.None).GetAwaiter().GetResult();
        return (int)FinalizeResult(result);
    }

    /// <summary>Send item to clan. Uses the client's HTTP timeout (no extra cancellation).</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_send_item_to_clan", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSendItemToClan(sbyte* clanNameOrTarget, sbyte* itemName, int quantity, sbyte* itemId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var idStr = PtrToString(itemId);
        Guid? guid = Guid.TryParse(idStr ?? string.Empty, out var g) && g != Guid.Empty ? g : null;
        var result = client.SendItemToClanAsync(
            PtrToString(clanNameOrTarget) ?? string.Empty,
            PtrToString(itemName) ?? string.Empty,
            quantity < 1 ? 1 : quantity,
            guid,
            CancellationToken.None).GetAwaiter().GetResult();

        if (result.IsError && !string.IsNullOrEmpty(result.Message) && result.Message.IndexOf("avatar", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            SetError("Clan not found.");
            var code = ExtractCode(result);
            InvokeCallback(code);
            return (int)code;
        }
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

    [UnmanagedCallersOnly(EntryPoint = "star_api_set_oasis_base_url", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSetOasisBaseUrl(sbyte* oasisBaseUrl)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var result = client.SetWeb4OasisApiBaseUrl(PtrToString(oasisBaseUrl) ?? string.Empty);
        return (int)FinalizeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_get_avatar_id", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetAvatarId(sbyte* avatarIdOut, nuint avatarIdSize)
    {
        if (avatarIdOut is null || avatarIdSize == 0)
            return (int)SetErrorAndReturn("avatarIdOut must not be null and size must be > 0.", StarApiResultCode.InvalidParam);

        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        // Get avatar_id by calling GetCurrentAvatarAsync which will ensure it's set
        var result = client.GetCurrentAvatarAsync().GetAwaiter().GetResult();
        if (result.IsError || result.Result is null)
        {
            return (int)SetErrorAndReturn(result.Message ?? "Failed to get avatar ID. Authenticate first.", ExtractCode(result));
        }

        var avatarId = result.Result.Id.ToString();
        if (string.IsNullOrWhiteSpace(avatarId))
            return (int)SetErrorAndReturn("Avatar ID not available. Authenticate first.", StarApiResultCode.NotInitialized);

        var bytes = Encoding.UTF8.GetBytes(avatarId);
        var copySize = Math.Min((int)avatarIdSize - 1, bytes.Length);
        if (copySize > 0)
        {
            Marshal.Copy(bytes, 0, (nint)avatarIdOut, copySize);
            avatarIdOut[copySize] = 0;
        }
        else
        {
            avatarIdOut[0] = 0;
        }

        SetError(string.Empty);
        InvokeCallback(StarApiResultCode.Success);
        return (int)StarApiResultCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_set_avatar_id", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiSetAvatarId(sbyte* avatarId)
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var result = client.SetAvatarId(PtrToString(avatarId) ?? string.Empty);
        return (int)FinalizeResult(result);
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

