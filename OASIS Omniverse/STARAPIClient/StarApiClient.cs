using System.Diagnostics;
using System.IO;
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
    /// <summary>Stack size. When adding with stack=true, API increments this if item exists; otherwise new item gets this quantity. Default 1.</summary>
    public int Quantity { get; init; } = 1;
}

public sealed class StarAvatarProfile
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    /// <summary>Experience points (from avatar detail). Updated by get-current-avatar and add-xp responses.</summary>
    public int XP { get; init; }
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

    /// <summary>Pickup delta array: one entry per item type (name -> pending qty to add). Games call QueueAddItem; we merge here and return API + pending in GetInventory. Worker flushes to API in background.</summary>
    private readonly Dictionary<string, LocalPendingEntry> _localPending = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _localPendingLock = new();

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
    private readonly ConcurrentQueue<PendingPickupWithMintJob> _pendingPickupWithMint = new();
    private readonly ConcurrentQueue<PendingMonsterKillJob> _pendingMonsterKill = new();
    private readonly ConcurrentQueue<PendingUseItemJob> _pendingUseItemJobs = new();
    private readonly ConcurrentQueue<PendingQuestObjectiveJob> _pendingQuestObjectiveJobs = new();
    private readonly SemaphoreSlim _addItemSignal = new(0);
    private readonly SemaphoreSlim _useItemSignal = new(0);
    private readonly SemaphoreSlim _questObjectiveSignal = new(0);
    private readonly object _jobLock = new();
    private int _activeAddItemJobs;
    private readonly object _lastMintLock = new();
    private string? _lastMintItemName;
    private string? _lastMintNftId;
    private string? _lastMintHash;
    private int _activeUseItemJobs;
    private int _activeQuestObjectiveJobs;
    /// <summary>Last known avatar XP (from get-current-avatar or add-xp response). Used by star_api_get_avatar_xp.</summary>
    private int _cachedAvatarXp;
    /// <summary>Pending XP to add (queued by star_api_queue_add_xp). Flushed with add-item worker.</summary>
    private int _pendingXp;
    private CancellationTokenSource? _jobCts;
    private Task? _jobWorker;
    private CancellationTokenSource? _useItemJobCts;
    private Task? _useItemJobWorker;
    private CancellationTokenSource? _questObjectiveJobCts;
    private Task? _questObjectiveJobWorker;
    /// <summary>Generic background queue for any async API call so UI/game thread never blocks. One worker processes jobs in order.</summary>
    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _genericBackgroundQueue = new();
    private readonly SemaphoreSlim _genericBackgroundSignal = new(0);
    private CancellationTokenSource? _genericBackgroundCts;
    private Task? _genericBackgroundWorker;
    private readonly object _genericBackgroundLock = new();

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
        // NFT minting and avatar auth use WEB4 OASIS API only; do not fall back to WEB5 URL.
        var oasisBaseUrl = FirstNonEmpty(
            config.Web4OasisApiBaseUrl,
            Environment.GetEnvironmentVariable("OASIS_WEB4_API_BASE_URL"))?.TrimEnd('/') ?? string.Empty;
        var apiIndex = oasisBaseUrl.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
        if (apiIndex >= 0)
            oasisBaseUrl = oasisBaseUrl[..apiIndex];
        // When WEB5 is localhost:5556, default WEB4 to localhost:5555 so mint/auth work without extra config.
        if (string.IsNullOrWhiteSpace(oasisBaseUrl) && (normalizedBaseUrl.Contains(":5556", StringComparison.Ordinal) || normalizedBaseUrl.Contains("localhost:5556", StringComparison.OrdinalIgnoreCase)))
            oasisBaseUrl = normalizedBaseUrl.Contains("https://", StringComparison.OrdinalIgnoreCase) ? "https://localhost:5555" : "http://localhost:5555";

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

        string oasisUrl;
        lock (_stateLock) { oasisUrl = _oasisBaseUrl ?? string.Empty; }
        if (string.IsNullOrWhiteSpace(oasisUrl))
            return FailAndCallback<bool>("WEB4 OASIS API base URL is not set. Set OASIS_WEB4_API_BASE_URL or Web4OasisApiBaseUrl (e.g. http://localhost:5555).", StarApiResultCode.InvalidParam);

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

            /* Call refresh XP so we get both GET avatar/current and add-xp(0) results in console. */
            RefreshAvatarXp();

            var result = Success(true, StarApiResultCode.Success, "Authentication successful.");
            InvokeCallback(StarApiResultCode.Success);
            return result;
        }
        catch (Exception ex)
        {
            return FailAndCallback<bool>($"Authentication failed: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    /// <summary>Run authentication on the background worker so the calling thread does not block. Await the returned task for the result.</summary>
    public Task<OASISResult<bool>> QueueAuthenticateAsync(string username, string password, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => AuthenticateAsync(username, password, ct), cancellationToken);

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

        /* Call refresh XP so we get both GET avatar/current and add-xp(0) results in console. */
        RefreshAvatarXp();

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
        {
            _avatarId = avatar.Id.ToString();
            _cachedAvatarXp = avatar.XP;
        }

        InvokeCallback(StarApiResultCode.Success);
        return Success(avatar, StarApiResultCode.Success, "Current avatar loaded.");
    }

    /// <summary>Run get-current-avatar on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarAvatarProfile>> QueueGetCurrentAvatarAsync(CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => GetCurrentAvatarAsync(ct), cancellationToken);

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

        static string NormalizeKeyName(string s) =>
            string.IsNullOrWhiteSpace(s) ? string.Empty : s.Replace('_', ' ').Trim();

        var matches = (string a, string b) =>
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            var na = NormalizeKeyName(a);
            var nb = NormalizeKeyName(b);
            return string.Equals(na, nb, StringComparison.OrdinalIgnoreCase);
        };

        // Fuzzy match for keycards: e.g. "Red Keycard" matches any item whose name contains "red" and "key"
        static bool FuzzyKeycardMatch(string itemNameQuery, string inventoryName)
        {
            if (string.IsNullOrWhiteSpace(inventoryName)) return false;
            var n = NormalizeKeyName(inventoryName);
            var q = NormalizeKeyName(itemNameQuery);
            var ni = n.ToLowerInvariant();
            var qi = q.ToLowerInvariant();
            if (qi.Contains("red") && (qi.Contains("key") || qi.Contains("keycard")))
                return ni.Contains("red") && (ni.Contains("key") || ni.Contains("keycard"));
            if (qi.Contains("blue") && (qi.Contains("key") || qi.Contains("keycard")))
                return ni.Contains("blue") && (ni.Contains("key") || ni.Contains("keycard"));
            if (qi.Contains("yellow") && (qi.Contains("key") || qi.Contains("keycard")))
                return ni.Contains("yellow") && (ni.Contains("key") || ni.Contains("keycard"));
            if (qi.Contains("skull") && qi.Contains("key"))
                return ni.Contains("skull") && ni.Contains("key");
            if (qi.Contains("gold") && qi.Contains("key"))
                return ni.Contains("gold") && (ni.Contains("key") || ni.Contains("keycard"));
            if (qi.Contains("silver") && qi.Contains("key"))
                return ni.Contains("silver") && (ni.Contains("key") || ni.Contains("keycard"));
            return false;
        }

        bool hasItem(IEnumerable<StarItem> items) =>
            items.Any(x => matches(x.Name, itemName) || matches(x.Description, itemName) || FuzzyKeycardMatch(itemName, x.Name) || FuzzyKeycardMatch(itemName, x.Description));

        lock (_inventoryCacheLock)
        {
            if (_cachedInventory is not null)
            {
                var merged = MergeLocalPendingIntoInventory(_cachedInventory);
                var hasItemResult = hasItem(merged);
                InvokeCallback(StarApiResultCode.Success);
                return Success(hasItemResult, StarApiResultCode.Success, hasItemResult ? "Item found in inventory (cached)." : "Item not found in inventory.");
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

        var found = hasItem(inventory.Result!);

        InvokeCallback(StarApiResultCode.Success);
        return Success(found, StarApiResultCode.Success, found ? "Item found in inventory." : "Item not found in inventory.");
    }

    /// <summary>Run has-item on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueHasItemAsync(string itemName, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => HasItemAsync(itemName, ct), cancellationToken);

    /// <summary>Get avatar inventory. Returns cache (or fetches) then merges with local pickup deltas so one row per type = API qty + pending. Single-flight fetch when cache is null.</summary>
    public async Task<OASISResult<List<StarItem>>> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarItem>>("Client is not initialized.", StarApiResultCode.NotInitialized);

        Task<OASISResult<List<StarItem>>>? task;
        lock (_inventoryCacheLock)
        {
            if (_cachedInventory is not null)
            {
                var merged = MergeLocalPendingIntoInventory(_cachedInventory);
                InvokeCallback(StarApiResultCode.Success);
                return Success(merged, StarApiResultCode.Success, $"Loaded {merged.Count} item(s) (cached + pending).");
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
            {
                var fetched = result.Result;
                /* Don't replace a non-empty cache with an empty fetch: avoids keys/items vanishing when a refetch (e.g. after sync) returns empty due to timing or API. */
                if (fetched.Count == 0 && _cachedInventory is not null && _cachedInventory.Count > 0)
                {
                    var merged = MergeLocalPendingIntoInventory(_cachedInventory);
                    return Success(merged, StarApiResultCode.Success, $"Loaded {merged.Count} item(s) (cached + pending, kept prior cache).");
                }
                _cachedInventory = new List<StarItem>(fetched);
            }
        }
        if (result.Result is not null)
        {
            var merged = MergeLocalPendingIntoInventory(result.Result);
            return Success(merged, StarApiResultCode.Success, result.Message ?? $"Loaded {merged.Count} item(s).");
        }
        return result;
    }

    /// <summary>Run get-inventory on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<List<StarItem>>> QueueGetInventoryAsync(CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => GetInventoryAsync(ct), cancellationToken);

    /// <summary>Merge API list with local pending: one row per type, qty = API qty + pending for that name. Types only in pending get a new row.</summary>
    private List<StarItem> MergeLocalPendingIntoInventory(List<StarItem> apiList)
    {
        Dictionary<string, LocalPendingEntry> snapshot;
        lock (_localPendingLock)
        {
            snapshot = new Dictionary<string, LocalPendingEntry>(_localPending, StringComparer.OrdinalIgnoreCase);
        }
        if (snapshot.Count == 0)
            return new List<StarItem>(apiList);

        var nameToPending = snapshot;
        var merged = new List<StarItem>(apiList.Count + nameToPending.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in apiList)
        {
            seen.Add(item.Name);
            var extra = nameToPending.TryGetValue(item.Name, out var pe) ? pe.Quantity : 0;
            merged.Add(new StarItem
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                GameSource = item.GameSource,
                ItemType = item.ItemType,
                NftId = item.NftId,
                Quantity = Math.Max(1, item.Quantity + extra)
            });
        }
        foreach (var kv in nameToPending)
        {
            if (seen.Contains(kv.Key))
                continue;
            merged.Add(new StarItem
            {
                Id = Guid.Empty,
                Name = kv.Value.Name,
                Description = kv.Value.Description,
                GameSource = kv.Value.GameSource,
                ItemType = kv.Value.ItemType,
                NftId = kv.Value.NftId ?? string.Empty,
                Quantity = Math.Max(1, kv.Value.Quantity)
            });
        }
        return merged;
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

    /// <summary>Clear all client caches (e.g. inventory). Next GetInventory/HasItem will hit the API.</summary>
    public void ClearCache()
    {
        InvalidateInventoryCache();
    }

    public async Task<OASISResult<StarItem>> AddItemAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", string? nftId = null, int quantity = 1, bool stack = true, CancellationToken cancellationToken = default)
    {
        return await AddItemCoreAsync(itemName, description, gameSource, itemType, nftId, quantity, stack, cancellationToken).ConfigureAwait(false);
    }

    public Task<OASISResult<StarItem>> QueueAddItemAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", string? nftId = null, int quantity = 1, bool stack = true, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<StarItem>("Client is not initialized.", StarApiResultCode.NotInitialized));

        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(gameSource))
            return Task.FromResult(FailAndCallback<StarItem>("Item name and game source are required.", StarApiResultCode.InvalidParam));

        EnqueueAddItemJobOnly(itemName, description, gameSource, itemType, nftId, quantity, stack);
        return Task.FromResult(Success(new StarItem { Id = Guid.Empty, Name = itemName, Description = description ?? string.Empty, GameSource = gameSource, ItemType = string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType, Quantity = quantity < 1 ? 1 : quantity }, StarApiResultCode.Success, "Queued for sync."));
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
            tasks.Add(QueueAddItemAsync(item.Name, item.Description, source, item.ItemType, string.IsNullOrWhiteSpace(item.NftId) ? null : item.NftId, item.Quantity, true, cancellationToken));
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

    /// <summary>Add pickup to local delta (one row per type). Used by native C: game calls this on pickup; GetInventory returns API + pending; worker flushes to API in background. No per-call HTTP.</summary>
    public void EnqueueAddItemJobOnly(string itemName, string description, string gameSource, string itemType = "KeyItem", string? nftId = null, int quantity = 1, bool stack = true)
    {
        if (!IsInitialized() || string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(gameSource))
            return;
        var qty = quantity < 1 ? 1 : quantity;
        var type = string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType;
        lock (_localPendingLock)
        {
            if (_localPending.TryGetValue(itemName, out var existing))
            {
                existing.Quantity += qty;
            }
            else
            {
                _localPending[itemName] = new LocalPendingEntry
                {
                    Name = itemName,
                    Description = description ?? string.Empty,
                    GameSource = gameSource,
                    ItemType = type,
                    Quantity = qty
                };
            }
        }
        _addItemSignal.Release();
    }

    /// <summary>Queue XP to add to the beamed-in avatar (e.g. on monster kill). Flushed with add-item worker. Amount 0 allowed (temp) for refresh / get newTotal from server.</summary>
    public void EnqueueAddXpJobOnly(int amount)
    {
        if (!IsInitialized()) return;
        if (amount < 0) return;
        Interlocked.Add(ref _pendingXp, amount);
        _addItemSignal.Release();
    }

    /// <summary>Send pending XP to the API (POST add-xp). Returns new total on success. Used by background worker and flush. amount 0 is allowed: no-op add but response newTotal updates cache (same code path as monster kill).</summary>
    public async Task<OASISResult<int>> AddXpAsync(int amount, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<int>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (amount < 0)
            return FailAndCallback<int>("XP amount must be non-negative.", StarApiResultCode.InvalidParam);
        string? avatarId;
        lock (_stateLock)
            avatarId = _avatarId;
        if (string.IsNullOrWhiteSpace(avatarId))
            return FailAndCallback<int>("Avatar ID not set. Beam in first.", StarApiResultCode.NotInitialized);

        var url = $"{_baseApiUrl}/api/avatar/add-xp";
        try
        {
            var payload = BuildJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteNumber("amount", amount);
                writer.WriteEndObject();
            });
            if (amount == 0)
                StarApiExports.StarApiLog($"[XP refresh add-xp(0)] POST url={url}");
            var response = await SendRawAsync(HttpMethod.Post, url, payload, cancellationToken).ConfigureAwait(false);
            var rawPreview = response.Result != null && response.Result.Length > 0
                ? (response.Result.Length <= 400 ? response.Result : response.Result[..400] + "...")
                : "(null or empty)";
            if (amount == 0)
                StarApiExports.StarApiLog($"[XP refresh add-xp(0)] response IsError={response.IsError} body={rawPreview}");
            if (response.IsError)
            {
                if (amount == 0)
                    StarApiExports.StarApiLog($"[XP refresh add-xp(0)] failed: {response.Message}");
                return FailAndCallback<int>(response.Message ?? "Add XP failed.", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
            }

            var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
            if (!parseResult)
            {
                if (amount == 0)
                    StarApiExports.StarApiLog($"[XP refresh add-xp(0)] parse failed: {parseErrorMessage}");
                return FailAndCallback<int>(parseErrorMessage, parseErrorCode);
            }

            var newTotal = GetIntProperty(resultElement, "newTotal") ?? GetIntProperty(resultElement, "NewTotal")
                ?? GetIntProperty(resultElement, "xp") ?? GetIntProperty(resultElement, "XP");
            if (amount == 0)
                StarApiExports.StarApiLog($"[XP refresh add-xp(0)] parsed newTotal={newTotal?.ToString() ?? "null"}");
            if (newTotal.HasValue && newTotal.Value >= 0)
            {
                Volatile.Write(ref _cachedAvatarXp, newTotal.Value);
                if (amount == 0)
                    StarApiExports.StarApiLog($"[XP refresh add-xp(0)] cache updated to {newTotal.Value}");
                InvokeCallback(StarApiResultCode.Success);
                return Success(newTotal.Value, StarApiResultCode.Success, amount == 0 ? "XP refreshed." : "XP added.");
            }
            /* No newTotal in response: assume current + amount (skip when amount is 0). */
            if (amount == 0)
            {
                StarApiExports.StarApiLog($"[XP refresh add-xp(0)] no newTotal in response; cache stays at {Volatile.Read(ref _cachedAvatarXp)}");
                InvokeCallback(StarApiResultCode.Success);
                return Success(Volatile.Read(ref _cachedAvatarXp), StarApiResultCode.Success, "XP refresh (no newTotal in response).");
            }
            var updated = Volatile.Read(ref _cachedAvatarXp) + amount;
            Volatile.Write(ref _cachedAvatarXp, updated);
            InvokeCallback(StarApiResultCode.Success);
            return Success(updated, StarApiResultCode.Success, "XP added.");
        }
        catch (Exception ex)
        {
            if (amount == 0)
                StarApiExports.StarApiLog($"[XP refresh add-xp(0)] exception: {ex.Message}");
            return FailAndCallback<int>($"Add XP failed: {ex.Message}", StarApiResultCode.Network, ex);
        }
    }

    /// <summary>Last known avatar XP (from get-current-avatar or add-xp). For star_api_get_avatar_xp.</summary>
    public int GetCachedAvatarXp() => Volatile.Read(ref _cachedAvatarXp);

    /// <summary>Refresh XP cache from API after beam-in. Calls both GET /api/avatar/current and add-xp(0); logs both results to console so you can see which works. Cache is updated from whichever succeeds.</summary>
    public void RefreshAvatarXp()
    {
        if (!IsInitialized())
        {
            StarApiExports.StarApiLog("[XP refresh] skipped (not initialized)");
            return;
        }
        StarApiExports.StarApiLog("[XP refresh] calling refresh XP endpoint (GET /api/avatar/current) and add-xp(0); results below.");
        _ = RunOnBackgroundAsync<StarAvatarProfile>(async ct =>
        {
            StarApiExports.StarApiLog("[XP refresh] GET /api/avatar/current (refresh XP endpoint) ...");
            var result = await GetCurrentAvatarAsync(ct).ConfigureAwait(false);
            if (result.IsError)
                StarApiExports.StarApiLog($"[XP refresh GET avatar/current] failed: {result.Message}");
            else
                StarApiExports.StarApiLog($"[XP refresh GET avatar/current] OK XP={result.Result?.XP ?? 0} (cache updated)");
            return result;
        }, CancellationToken.None);
        _ = RunOnBackgroundAsync<int>(async ct =>
        {
            StarApiExports.StarApiLog("[XP refresh] POST add-xp(0) ...");
            var result = await AddXpAsync(0, ct).ConfigureAwait(false);
            if (result.IsError)
                StarApiExports.StarApiLog($"[XP refresh add-xp(0)] failed: {result.Message}");
            else
                StarApiExports.StarApiLog($"[XP refresh add-xp(0)] OK newTotal={result.Result} (cache updated)");
            return result;
        }, CancellationToken.None);
    }

    /// <summary>Load avatar XP from API and update cache. Blocks until the request completes. Uses add-xp(0) so same code path as monster kill.</summary>
    public void LoadAvatarXpBlocking()
    {
        if (!IsInitialized()) return;
        _ = AddXpAsync(0, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>Consume the last mint result (item name, NFT ID, hash) from background pickup-with-mint. Returns true if a result was available and copies into the provided buffers; clears the stored result. Call from game each frame/pump to show mint results in console.</summary>
    public bool ConsumeLastMintResult(out string? itemName, out string? nftId, out string? hash)
    {
        lock (_lastMintLock)
        {
            itemName = _lastMintItemName;
            nftId = _lastMintNftId;
            hash = _lastMintHash;
            _lastMintItemName = _lastMintNftId = _lastMintHash = null;
        }
        return itemName is not null || nftId is not null;
    }

    /// <summary>Queue a pickup that may include mint (all work in background worker). Game calls this instead of mint+queue_add when do_mint is true; C# client mints then adds in ProcessAddItemJobsAsync.</summary>
    public void EnqueuePickupWithMintJobOnly(string itemName, string description, string gameSource, string itemType = "KeyItem", bool doMint = false, string? provider = null, string? sendToAddressAfterMinting = null, int quantity = 1)
    {
        if (!IsInitialized())
        {
            StarApiExports.SetLastBackgroundError("STAR: Pickup not queued (client not initialized).");
            return;
        }
        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(gameSource))
        {
            StarApiExports.SetLastBackgroundError("STAR: Pickup not queued (item name or game source empty).");
            return;
        }
        var type = string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType;
        var qty = quantity < 1 ? 1 : quantity;
        _pendingPickupWithMint.Enqueue(new PendingPickupWithMintJob(itemName, description ?? string.Empty, gameSource, type, doMint, provider, sendToAddressAfterMinting, qty));
        /* Show in overlay immediately: merge in GetInventoryAsync uses _localPending, so add here; worker will deduct when add completes. */
        lock (_localPendingLock)
        {
            if (_localPending.TryGetValue(itemName, out var existing))
                existing.Quantity += qty;
            else
                _localPending[itemName] = new LocalPendingEntry { Name = itemName, Description = description ?? string.Empty, GameSource = gameSource, ItemType = type, Quantity = qty };
        }
        _addItemSignal.Release();
    }

    /// <summary>Queue a monster kill (XP + optional mint + add to inventory). All work runs on the add-item background worker; never blocks.</summary>
    public void EnqueueMonsterKillJobOnly(string engineName, string displayName, int xp, bool isBoss, bool doMint, string? provider, string? gameSource = null)
    {
        if (!IsInitialized())
        {
            StarApiExports.StarApiLog($"Monster kill NOT queued (client not initialized): {displayName} {xp} XP");
            return;
        }
        if (string.IsNullOrWhiteSpace(engineName) || string.IsNullOrWhiteSpace(displayName))
        {
            StarApiExports.StarApiLog($"Monster kill NOT queued (empty name): engine='{engineName}' display='{displayName}'");
            return;
        }
        var xpVal = xp < 0 ? 0 : xp;
        var gs = string.IsNullOrWhiteSpace(gameSource) ? "ODOOM" : gameSource;
        StarApiExports.StarApiLog($"Monster kill queued: {displayName} ({engineName}) {xpVal} XP doMint={doMint} gameSource={gs}");
        /* Optimistic XP update so HUD shows new XP immediately without waiting for background worker. */
        if (xpVal > 0)
            Volatile.Write(ref _cachedAvatarXp, Volatile.Read(ref _cachedAvatarXp) + xpVal);
        StartAddItemWorker();
        _pendingMonsterKill.Enqueue(new PendingMonsterKillJob(engineName, displayName, xpVal, isBoss, doMint, provider ?? "SolanaOASIS", gs));
        _addItemSignal.Release();
    }

    public async Task<OASISResult<bool>> FlushAddItemJobsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        while ((GetLocalPendingCount() > 0 || Volatile.Read(ref _activeAddItemJobs) > 0) && !cancellationToken.IsCancellationRequested)
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
            return FailAndCallback<bool>("Flush add-item jobs was cancelled.", StarApiResultCode.Network);

        return Success(true, StarApiResultCode.Success, "Add-item queue flushed.");
    }

    private int GetLocalPendingCount()
    {
        lock (_localPendingLock)
            return _localPending.Count;
    }

    /// <summary>Subtract quantity from _localPending for itemName (after pickup-with-mint add succeeds so we don't double-count in merge).</summary>
    private void DeductLocalPending(string itemName, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0) return;
        lock (_localPendingLock)
        {
            if (!_localPending.TryGetValue(itemName, out var entry)) return;
            entry.Quantity -= quantity;
            if (entry.Quantity <= 0)
                _localPending.Remove(itemName);
        }
    }

    private async Task<OASISResult<StarItem>> AddItemCoreAsync(string itemName, string description, string gameSource, string itemType = "KeyItem", string? nftId = null, int quantity = 1, bool stack = true, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
        {
            StarApiExports.StarApiLog("AddItemCoreAsync: not initialized");
            return FailAndCallback<StarItem>("Client is not initialized.", StarApiResultCode.NotInitialized);
        }

        string? avatarId;
        lock (_stateLock)
            avatarId = _avatarId;
        if (string.IsNullOrWhiteSpace(avatarId))
        {
            StarApiExports.StarApiLog("AddItemCoreAsync: Avatar ID not set (beam-in required)");
            return FailAndCallback<StarItem>("Avatar ID is not set. Complete beam-in (authenticate) first; add_item requires avatar context.", StarApiResultCode.NotInitialized);
        }

        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(gameSource))
        {
            StarApiExports.StarApiLog("AddItemCoreAsync: missing required param");
            return FailAndCallback<StarItem>("Item name, description, and game source are required.", StarApiResultCode.InvalidParam);
        }

        var url = $"{_baseApiUrl}/api/avatar/inventory";
        StarApiExports.StarApiLog($"AddItemCoreAsync: sending POST to {url} name='{itemName}' avatarId={avatarId}");

        try
        {
            var payload = BuildJson(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("Name", itemName);
                writer.WriteString("Description", $"{description} | Source: {gameSource}");
                writer.WriteNumber("HolonType", 11);
                writer.WriteNumber("Quantity", quantity < 1 ? 1 : quantity);
                writer.WriteBoolean("Stack", stack);
                writer.WriteString("GameSource", gameSource);
                writer.WriteString("ItemType", string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType);
                if (!string.IsNullOrWhiteSpace(nftId))
                {
                    writer.WriteString("NftId", nftId);
                    writer.WritePropertyName("MetaData");
                    writer.WriteStartObject();
                    writer.WriteString("NFTId", nftId);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            });

            var response = await SendRawAsync(HttpMethod.Post, url, payload, cancellationToken).ConfigureAwait(false);
            if (response.IsError)
            {
                StarApiExports.StarApiLog($"AddItemCoreAsync: response IsError=true message='{response.Message}'");
                return FailAndCallback<StarItem>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
            }
            StarApiExports.StarApiLog($"AddItemCoreAsync: POST succeeded, parsing response");

            var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
            if (!parseResult)
                return FailAndCallback<StarItem>(parseErrorMessage, parseErrorCode);

            var item = ParseInventoryItemResponse(resultElement);
            if (item is null)
                return FailAndCallback<StarItem>("API did not return the created inventory item.", StarApiResultCode.ApiError);

            /* Use NftId from response when API returns it; otherwise use the nftId we sent so [NFT] prefix shows on first display (Doom/Quake) even if API does not echo it yet. */
            var itemNftId = !string.IsNullOrWhiteSpace(item.NftId) ? item.NftId
                : ExtractMeta(item.MetaData, "NFTId", string.Empty) ?? ExtractMeta(item.MetaData, "OASISNFTId", string.Empty)
                ?? (!string.IsNullOrWhiteSpace(nftId) ? nftId : string.Empty);
            var mapped = new StarItem
            {
                Id = item.Id,
                Name = item.Name ?? itemName,
                Description = item.Description ?? description,
                GameSource = !string.IsNullOrWhiteSpace(item.GameSource) ? item.GameSource : gameSource,
                ItemType = !string.IsNullOrWhiteSpace(item.ItemType) ? item.ItemType : (string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType),
                NftId = itemNftId,
                Quantity = item.Quantity
            };

            lock (_inventoryCacheLock)
            {
                _cachedInventory ??= new List<StarItem>();
                if (stack)
                {
                    var idx = _cachedInventory.FindIndex(x => string.Equals(x.Name, itemName, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                        _cachedInventory[idx] = mapped;
                    else
                        _cachedInventory.Add(mapped);
                }
                else
                    _cachedInventory.Add(mapped);
            }

            StarApiExports.StarApiLog($"AddItemCoreAsync: item added id={mapped.Id} name='{mapped.Name}' quantity={mapped.Quantity}");
            InvokeCallback(StarApiResultCode.Success);
            return Success(mapped, StarApiResultCode.Success, "Item added successfully.");
        }
        catch (Exception ex)
        {
            StarApiExports.StarApiLog($"AddItemCoreAsync: exception {ex.GetType().Name} message='{ex.Message}'");
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

    /// <summary>Enqueue one use-item job without returning a completion task. Used by native C sync lib for batching.</summary>
    public void EnqueueUseItemJobOnly(string itemName, string? context = null)
    {
        if (!IsInitialized() || string.IsNullOrWhiteSpace(itemName))
            return;
        _pendingUseItemJobs.Enqueue(new PendingUseItemJob(itemName, context, CancellationToken.None, null));
        _useItemSignal.Release();
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

    /// <summary>Run start-quest on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueStartQuestAsync(string questId, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => StartQuestAsync(questId, ct), cancellationToken);

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

    /// <summary>Run complete-quest on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueCompleteQuestAsync(string questId, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => CompleteQuestAsync(questId, ct), cancellationToken);

    public async Task<OASISResult<StarQuestInfo?>> CreateCrossGameQuestAsync(string questName, string description, List<StarQuestObjective> objectives, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<StarQuestInfo?>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(questName) || string.IsNullOrWhiteSpace(description) || objectives is null || objectives.Count == 0)
            return FailAndCallback<StarQuestInfo?>("Quest name, description and at least one objective are required.", StarApiResultCode.InvalidParam);

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
                writer.WriteString("Description", o.Description ?? string.Empty);
                writer.WriteString("GameSource", o.GameSource ?? string.Empty);
                writer.WriteString("ItemRequired", o.ItemRequired ?? string.Empty);
                writer.WriteNumber("Order", i);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/create", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarQuestInfo?>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        StarQuestInfo? created = null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (parseResult && resultElement.ValueKind == JsonValueKind.Object)
            created = ParseSingleQuestInfo(resultElement);

        InvokeCallback(StarApiResultCode.Success);
        return Success(created, StarApiResultCode.Success, "Cross-game quest created successfully.");
    }

    /// <summary>Run create-cross-game-quest on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarQuestInfo?>> QueueCreateCrossGameQuestAsync(string questName, string description, List<StarQuestObjective> objectives, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => CreateCrossGameQuestAsync(questName, description, objectives, ct), cancellationToken);

    /// <summary>Adds an objective (sub-quest) to an existing quest. Returns the created objective with its Id.</summary>
    public async Task<OASISResult<StarQuestInfo?>> AddQuestObjectiveAsync(string questId, string description, string? name = null, string? gameSource = null, string? itemRequired = null, int order = -1, CancellationToken cancellationToken = default)
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
            writer.WriteString("ItemRequired", itemRequired ?? string.Empty);
            writer.WriteNumber("Order", order);
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/quests/{questId}/objectives", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<StarQuestInfo?>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        StarQuestInfo? created = null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (parseResult && resultElement.ValueKind == JsonValueKind.Object)
            created = ParseSingleQuestInfo(resultElement);

        InvokeCallback(StarApiResultCode.Success);
        return Success(created, StarApiResultCode.Success, "Quest objective added successfully.");
    }

    /// <summary>Run add-quest-objective on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarQuestInfo?>> QueueAddQuestObjectiveAsync(string questId, string description, string? name = null, string? gameSource = null, string? itemRequired = null, int order = -1, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => AddQuestObjectiveAsync(questId, description, name, gameSource, itemRequired, order, ct), cancellationToken);

    /// <summary>Removes an objective (sub-quest) from a quest.</summary>
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

    /// <summary>Run get-active-quests on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<List<StarQuestInfo>>> QueueGetActiveQuestsAsync(CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => GetActiveQuestsAsync(ct), cancellationToken);

    /// <summary>Mint an NFT for a monster kill (any monster, including bosses) via WEB4 OASIS API. Returns NFT ID and optional tx hash. provider: same as nft_provider in oasisstar.json (e.g. SolanaOASIS); null/empty = SolanaOASIS. SPL used when provider is SolanaOASIS, else ERC1155.</summary>
    public async Task<OASISResult<(string NftId, string? Hash)>> CreateMonsterNftAsync(string monsterName, string? description, string? gameSource, string? monsterStatsJson, string? provider = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<(string NftId, string? Hash)>("Client is not initialized.", StarApiResultCode.NotInitialized);

        string oasisUrl;
        lock (_stateLock) { oasisUrl = _oasisBaseUrl ?? string.Empty; }
        if (string.IsNullOrWhiteSpace(oasisUrl))
            return FailAndCallback<(string NftId, string? Hash)>("WEB4 OASIS API base URL is not set. Set OASIS_WEB4_API_BASE_URL or Web4OasisApiBaseUrl (e.g. http://localhost:5555).", StarApiResultCode.InvalidParam);

        if (string.IsNullOrWhiteSpace(monsterName))
            return FailAndCallback<(string NftId, string? Hash)>("Monster name is required.", StarApiResultCode.InvalidParam);

        JsonElement monsterStatsElement;
        try
        {
            var statsJson = string.IsNullOrWhiteSpace(monsterStatsJson) ? "{}" : monsterStatsJson;
            using var statsDoc = JsonDocument.Parse(statsJson);
            monsterStatsElement = statsDoc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            return FailAndCallback<(string NftId, string? Hash)>($"monsterStatsJson is not valid JSON: {ex.Message}", StarApiResultCode.InvalidParam, ex);
        }

        string? sendToAvatarAfterMintingId = null;
        lock (_stateLock)
        {
            if (Guid.TryParse(_avatarId, out var avatarGuid) && avatarGuid != Guid.Empty)
                sendToAvatarAfterMintingId = avatarGuid.ToString();
        }

        var onChainProvider = string.IsNullOrWhiteSpace(provider) ? "SolanaOASIS" : provider;
        var nftStandardType = string.Equals(onChainProvider, "SolanaOASIS", StringComparison.OrdinalIgnoreCase) ? "SPL" : "ERC1155";

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("title", monsterName);
            writer.WriteString("description", string.IsNullOrWhiteSpace(description) ? "Monster from game" : description);
            writer.WriteString("symbol", "BOSS");
            writer.WriteString("image", "AQ==");
            writer.WriteString("imageUrl", "https://oasisweb4.one/images/star/default-boss.png");
            writer.WriteString("thumbnail", "AQ==");
            writer.WriteString("thumbnailUrl", "https://oasisweb4.one/images/star/default-boss-thumb.png");
            writer.WriteString("memoText", "Minted by WEB4 OASIS API");
            writer.WriteNumber("numberToMint", 1);
            writer.WriteBoolean("storeNFTMetaDataOnChain", false);
            writer.WriteString("offChainProvider", "MongoDBOASIS");
            writer.WriteString("onChainProvider", onChainProvider);
            writer.WriteString("nftOffChainMetaType", "ExternalJSONURL");
            writer.WriteString("JSONMetaDataURL", "https://oasisweb4.one/metadata/star/default-boss.json");
            writer.WriteString("nftStandardType", nftStandardType);
            if (!string.IsNullOrWhiteSpace(sendToAvatarAfterMintingId))
                writer.WriteString("sendToAvatarAfterMintingId", sendToAvatarAfterMintingId);
            writer.WritePropertyName("metaData");
            writer.WriteStartObject();
            writer.WriteString("GameSource", string.IsNullOrWhiteSpace(gameSource) ? "Unknown" : gameSource);
            writer.WritePropertyName("BossStats");
            monsterStatsElement.WriteTo(writer);
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
                return Success((warningMintId!, (string?)null), StarApiResultCode.Success, $"Boss NFT created with warnings: {response.Message}");
            }

            return FailAndCallback<(string NftId, string? Hash)>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            if (TryExtractTopLevelResultId(response.Result, out var warningMintId))
            {
                InvokeCallback(StarApiResultCode.Success);
                return Success((warningMintId!, (string?)null), StarApiResultCode.Success, $"Boss NFT created with warnings: {parseErrorMessage}");
            }

            return FailAndCallback<(string NftId, string? Hash)>(parseErrorMessage, parseErrorCode);
        }

        var nftId = ParseIdAsString(resultElement);
        if (string.IsNullOrWhiteSpace(nftId))
            return FailAndCallback<(string NftId, string? Hash)>("API did not return an NFT ID.", StarApiResultCode.ApiError);

        var hash = GetMintResponseHash(resultElement, response.Result);
        InvokeCallback(StarApiResultCode.Success);
        return Success((nftId, string.IsNullOrWhiteSpace(hash) ? null : hash), StarApiResultCode.Success, "Monster NFT created successfully.");
    }

    /// <summary>Run create-monster-NFT on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<(string NftId, string? Hash)>> QueueCreateMonsterNftAsync(string monsterName, string? description, string? gameSource, string? monsterStatsJson, string? provider = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => CreateMonsterNftAsync(monsterName, description, gameSource, monsterStatsJson, provider, ct), cancellationToken);

    /// <summary>Mint an NFT for an inventory item (creates NFTHolon on WEB4). Returns NFT ID and optional hash (tx/signature). Default provider: SolanaOASIS. Same as nft_provider in oasisstar.json. sendToAddressAfterMinting: optional wallet address to send the minted NFT to (from oasisstar.json SendToAddressAfterMinting).</summary>
    public async Task<OASISResult<(string NftId, string? Hash)>> MintInventoryItemNftAsync(string itemName, string? description, string gameSource, string itemType = "KeyItem", string? provider = null, string? sendToAddressAfterMinting = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<(string NftId, string? Hash)>("Client is not initialized.", StarApiResultCode.NotInitialized);

        string oasisUrl;
        lock (_stateLock) { oasisUrl = _oasisBaseUrl ?? string.Empty; }
        if (string.IsNullOrWhiteSpace(oasisUrl))
            return FailAndCallback<(string NftId, string? Hash)>("WEB4 OASIS API base URL is not set. Set OASIS_WEB4_API_BASE_URL or Web4OasisApiBaseUrl (e.g. http://localhost:5555).", StarApiResultCode.InvalidParam);

        if (string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<(string NftId, string? Hash)>("Item name is required.", StarApiResultCode.InvalidParam);

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
            writer.WriteString("memoText", "Minted by WEB4 OASIS API (inventory item)");
            writer.WriteNumber("numberToMint", 1);
            writer.WriteBoolean("storeNFTMetaDataOnChain", false);
            writer.WriteString("offChainProvider", "MongoDBOASIS");
            writer.WriteString("onChainProvider", onChainProvider);
            writer.WriteString("nftOffChainMetaType", "ExternalJSONURL");
            writer.WriteString("JSONMetaDataURL", "https://oasisweb4.one/metadata/star/default-item.json");
            writer.WriteString("nftStandardType", string.Equals(onChainProvider, "SolanaOASIS", StringComparison.OrdinalIgnoreCase) ? "SPL" : "ERC1155");
            if (!string.IsNullOrWhiteSpace(sendToAvatarAfterMintingId))
                writer.WriteString("sendToAvatarAfterMintingId", sendToAvatarAfterMintingId);
            if (!string.IsNullOrWhiteSpace(sendToAddressAfterMinting))
                writer.WriteString("sendToAddressAfterMinting", sendToAddressAfterMinting);
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
                return Success((warningMintId!, (string?)null), StarApiResultCode.Success, $"Inventory item NFT created with warnings: {response.Message}");
            return FailAndCallback<(string NftId, string? Hash)>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            if (TryExtractTopLevelResultId(response.Result, out var warningMintId))
                return Success((warningMintId!, (string?)null), StarApiResultCode.Success, $"Inventory item NFT created with warnings: {parseErrorMessage}");
            return FailAndCallback<(string NftId, string? Hash)>(parseErrorMessage, parseErrorCode);
        }

        var nftId = ParseIdAsString(resultElement);
        if (string.IsNullOrWhiteSpace(nftId))
            return FailAndCallback<(string NftId, string? Hash)>("API did not return an NFT ID.", StarApiResultCode.ApiError);

        var hash = GetMintResponseHash(resultElement, response.Result);

        InvokeCallback(StarApiResultCode.Success);
        return Success((nftId, string.IsNullOrWhiteSpace(hash) ? null : hash), StarApiResultCode.Success, "Inventory item NFT minted successfully.");
    }

    /// <summary>Run mint-inventory-item-NFT on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<(string NftId, string? Hash)>> QueueMintInventoryItemNftAsync(string itemName, string? description, string gameSource, string itemType = "KeyItem", string? provider = null, string? sendToAddressAfterMinting = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => MintInventoryItemNftAsync(itemName, description, gameSource, itemType, provider, sendToAddressAfterMinting, ct), cancellationToken);

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

    /// <summary>Run deploy-boss-NFT on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueDeployBossNftAsync(string nftId, string targetGame, string? location = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => DeployBossNftAsync(nftId, targetGame, location, ct), cancellationToken);

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

    /// <summary>Run get-NFT-collection on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<List<StarNftInfo>>> QueueGetNftCollectionAsync(CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => GetNftCollectionAsync(ct), cancellationToken);

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

    /// <summary>Run send-item-to-avatar on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueSendItemToAvatarAsync(string targetUsernameOrAvatarId, string itemName, int quantity = 1, Guid? itemId = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => SendItemToAvatarAsync(targetUsernameOrAvatarId, itemName, quantity, itemId, ct), cancellationToken);

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

    /// <summary>Run send-item-to-clan on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueSendItemToClanAsync(string clanNameOrTargetUsername, string itemName, int quantity = 1, Guid? itemId = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => SendItemToClanAsync(clanNameOrTargetUsername, itemName, quantity, itemId, ct), cancellationToken);

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

    private static int? GetIntProperty(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n))
            return n;
        var text = GetStringProperty(element, name);
        return int.TryParse(text, out var parsed) ? parsed : null;
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

    private void StartWorkers()
    {
        StartAddItemWorker();
        StartUseItemWorker();
        StartQuestObjectiveWorker();
        // Generic background worker is started lazily when the first Queue* (non–add-item/use-item/quest-objective) method is used. Do not start here so games that only use direct/blocking APIs (e.g. star_api_get_inventory) do not get an extra worker thread that can contribute to freezes or thread-pool pressure.
    }

    private void StopWorkers()
    {
        StopAddItemWorker();
        StopUseItemWorker();
        StopQuestObjectiveWorker();
        StopGenericBackgroundWorker();
    }

    private void StartGenericBackgroundWorker()
    {
        lock (_genericBackgroundLock)
        {
            if (_genericBackgroundWorker is { IsCompleted: false })
                return;
            _genericBackgroundCts = new CancellationTokenSource();
            _genericBackgroundWorker = Task.Run(() => ProcessGenericBackgroundJobsAsync(_genericBackgroundCts.Token));
        }
    }

    private void StopGenericBackgroundWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_genericBackgroundLock)
        {
            cts = _genericBackgroundCts;
            worker = _genericBackgroundWorker;
            _genericBackgroundCts = null;
            _genericBackgroundWorker = null;
        }
        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _genericBackgroundSignal.Release();
                worker?.GetAwaiter().GetResult();
            }
            catch { }
            finally { cts.Dispose(); }
        }
        while (_genericBackgroundQueue.TryDequeue(out _)) { }
    }

    private async Task ProcessGenericBackgroundJobsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _genericBackgroundSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            while (_genericBackgroundQueue.TryDequeue(out var job))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                try
                {
                    await job(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    /* Job already set result/exception on its TCS; continue to next job. */
                }
            }
        }
    }

    /// <summary>Run an async operation on the generic background worker so the caller's thread (e.g. UI/game) never blocks. Returns a Task that completes when the operation finishes.</summary>
    private Task<OASISResult<T>> RunOnBackgroundAsync<T>(Func<CancellationToken, Task<OASISResult<T>>> operation, CancellationToken cancellationToken)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<T>("Client is not initialized.", StarApiResultCode.NotInitialized));
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<OASISResult<T>>(cancellationToken);

        var tcs = new TaskCompletionSource<OASISResult<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        }

        var run = async (CancellationToken workerCt) =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(workerCt, cancellationToken);
            try
            {
                var result = await operation(linked.Token).ConfigureAwait(false);
                tcs.TrySetResult(result);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(Fail<T>(ex.Message, StarApiResultCode.Network, ex));
            }
        };

        _genericBackgroundQueue.Enqueue(run);
        _genericBackgroundSignal.Release();
        StartGenericBackgroundWorker();
        return tcs.Task;
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
                if (worker is not null)
                    worker.GetAwaiter().GetResult();
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
            pending.Completion?.TrySetResult(Fail<StarItem>("Add-item queue stopped.", StarApiResultCode.NotInitialized));
    }

    private void StartUseItemWorker()
    {
        lock (_jobLock)
        {
            if (_useItemJobWorker is not null && !_useItemJobWorker.IsCompleted)
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
                if (worker is not null)
                    worker.GetAwaiter().GetResult();
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
            pending.Completion?.TrySetResult(Fail<bool>("Use-item queue stopped.", StarApiResultCode.NotInitialized));
    }

    private void StartQuestObjectiveWorker()
    {
        lock (_jobLock)
        {
            if (_questObjectiveJobWorker is not null && !_questObjectiveJobWorker.IsCompleted)
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
                if (worker is not null)
                    worker.GetAwaiter().GetResult();
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

    /// <summary>Background worker: flush local pending to API (one add_item per type), then invalidate cache. Games only call EnqueueAddItemJobOnly or EnqueuePickupWithMintJobOnly; this does the heavy lifting.</summary>
    private async Task ProcessAddItemJobsAsync(CancellationToken cancellationToken)
    {
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

            /* Flush pending XP (queued by star_api_queue_add_xp or monster kill jobs). */
            var pendingXp = Interlocked.Exchange(ref _pendingXp, 0);
            if (pendingXp > 0)
            {
                var addXpResult = await AddXpAsync(pendingXp, cancellationToken).ConfigureAwait(false);
                if (addXpResult.IsError)
                    StarApiExports.SetLastBackgroundError($"STAR: Add XP failed: {addXpResult.Message}");
            }

            /* Process monster kill jobs: add XP and optionally mint + add item. Flush XP immediately after so it shows up as soon as you kill. */
            while (_pendingMonsterKill.TryDequeue(out var monsterJob))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                StarApiExports.StarApiLog($"Monster kill processing: {monsterJob.DisplayName} {monsterJob.Xp} XP doMint={monsterJob.DoMint}");
                Interlocked.Add(ref _pendingXp, monsterJob.Xp);
                if (!monsterJob.DoMint)
                    continue;
                var gameSource = string.IsNullOrWhiteSpace(monsterJob.GameSource) ? "ODOOM" : monsterJob.GameSource;
                var desc = $"Monster defeated in {gameSource}: {monsterJob.DisplayName}";
                StarApiExports.StarApiLog($"Monster kill: minting NFT for {monsterJob.DisplayName}");
                var mintResult = await CreateMonsterNftAsync(monsterJob.EngineName, desc, gameSource, "{}", monsterJob.Provider, cancellationToken).ConfigureAwait(false);
                if (mintResult.IsError || string.IsNullOrWhiteSpace(mintResult.Result.NftId))
                {
                    StarApiExports.StarApiLog($"Monster kill: NFT mint failed for '{monsterJob.DisplayName}': {mintResult.Message}");
                    StarApiExports.SetLastBackgroundError($"STAR: Monster NFT mint failed for '{monsterJob.DisplayName}': {mintResult.Message}");
                    continue;
                }
                StarApiExports.StarApiLog($"Monster kill: NFT minted for {monsterJob.DisplayName}, adding to inventory");
                /* Store item name without [NFT] prefix (popup adds it). Add [BOSS] for boss monsters only. */
                var itemName = monsterJob.IsBoss ? "[BOSS] " + monsterJob.DisplayName : monsterJob.DisplayName;
                Interlocked.Increment(ref _activeAddItemJobs);
                try
                {
                    var addResult = await AddItemCoreAsync(itemName, desc, gameSource, "Monster", mintResult.Result.NftId, 1, true, cancellationToken).ConfigureAwait(false);
                    if (addResult.IsError)
                        StarApiExports.SetLastBackgroundError($"STAR: Add monster item failed for '{itemName}': {addResult.Message}");
                    else
                    {
                        lock (_lastMintLock)
                        {
                            _lastMintItemName = itemName;
                            _lastMintNftId = mintResult.Result.NftId;
                            _lastMintHash = mintResult.Result.Hash;
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _activeAddItemJobs);
                }
            }

            /* Flush XP from monster kills (and any other pending) so HUD updates as soon as you kill, not on next worker wake. */
            var monsterXp = Interlocked.Exchange(ref _pendingXp, 0);
            if (monsterXp > 0)
            {
                StarApiExports.StarApiLog($"Monster kill: sending AddXpAsync({monsterXp}) to API");
                var addXpResult = await AddXpAsync(monsterXp, cancellationToken).ConfigureAwait(false);
                if (addXpResult.IsError)
                {
                    StarApiExports.StarApiLog($"Monster kill: Add XP failed: {addXpResult.Message}");
                    StarApiExports.SetLastBackgroundError($"STAR: Add XP failed: {addXpResult.Message}");
                }
                else
                    StarApiExports.StarApiLog($"Monster kill: Add XP succeeded, new total={addXpResult.Result}");
            }

            // Process pickup-with-mint jobs first (mint then add_item; all in C# background).
            while (_pendingPickupWithMint.TryDequeue(out var pickupJob))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                string? nftId = null;
                if (pickupJob.DoMint)
                {
                    var mintResult = await MintInventoryItemNftAsync(
                        pickupJob.ItemName,
                        pickupJob.Description,
                        pickupJob.GameSource,
                        pickupJob.ItemType,
                        pickupJob.Provider,
                        pickupJob.SendToAddressAfterMinting,
                        cancellationToken).ConfigureAwait(false);
                    if (!mintResult.IsError && mintResult.Result.NftId is { } id)
                    {
                        nftId = id;
                        var hash = mintResult.Result.Hash;
                        lock (_lastMintLock)
                        {
                            _lastMintItemName = pickupJob.ItemName;
                            _lastMintNftId = id;
                            _lastMintHash = string.IsNullOrWhiteSpace(hash) ? null : hash;
                        }
                        /* So overlay shows [NFT] before add completes: set NftId on pending entry. */
                        lock (_localPendingLock)
                        {
                            if (_localPending.TryGetValue(pickupJob.ItemName, out var pending))
                                pending.NftId = id;
                        }
                    }
                    else if (mintResult.IsError)
                    {
                        StarApiExports.StarApiLog($"Mint failed for '{pickupJob.ItemName}': {mintResult.Message}");
                        StarApiExports.SetLastBackgroundError($"STAR: Mint failed for '{pickupJob.ItemName}': {mintResult.Message}");
                    }
                }
                Interlocked.Increment(ref _activeAddItemJobs);
                try
                {
                    var addResult = await AddItemCoreAsync(pickupJob.ItemName, pickupJob.Description, pickupJob.GameSource, pickupJob.ItemType, nftId, pickupJob.Quantity, true, cancellationToken).ConfigureAwait(false);
                    if (addResult.IsError)
                        StarApiExports.SetLastBackgroundError($"STAR: Add item failed for '{pickupJob.ItemName}': {addResult.Message}");
                    else
                        DeductLocalPending(pickupJob.ItemName, pickupJob.Quantity);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeAddItemJobs);
                }
            }

            /* Do not invalidate cache here: AddItemCoreAsync already updates _cachedInventory when add succeeds. Invalidating caused a refetch that could return stale data (keys vanished in overlay). */

            Dictionary<string, LocalPendingEntry> snapshot;
            lock (_localPendingLock)
            {
                if (_localPending.Count == 0)
                    continue;
                snapshot = new Dictionary<string, LocalPendingEntry>(_localPending, StringComparer.OrdinalIgnoreCase);
                _localPending.Clear();
            }

            /* Ensure FlushAddItemJobsAsync does not return until all items are processed (avoids race where HasItemAsync runs with cache not yet updated). */
            var snapshotCount = snapshot.Count;
            Interlocked.Add(ref _activeAddItemJobs, snapshotCount);

            if (AddItemBatchWindow > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(AddItemBatchWindow, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    lock (_localPendingLock)
                    {
                        foreach (var kv in snapshot)
                            _localPending[kv.Key] = kv.Value;
                    }
                    break;
                }
            }

            foreach (var kv in snapshot)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    lock (_localPendingLock)
                    {
                        if (_localPending.TryGetValue(kv.Key, out var existing))
                            existing.Quantity += kv.Value.Quantity;
                        else
                            _localPending[kv.Key] = kv.Value;
                    }
                    Interlocked.Decrement(ref _activeAddItemJobs);
                    continue;
                }
                var entry = kv.Value;
                try
                {
                    var addResult = await AddItemCoreAsync(entry.Name, entry.Description, entry.GameSource, entry.ItemType, null, entry.Quantity, true, cancellationToken).ConfigureAwait(false);
                    if (addResult.IsError)
                        StarApiExports.SetLastBackgroundError($"STAR: Add item failed for '{entry.Name}': {addResult.Message}");
                }
                finally
                {
                    Interlocked.Decrement(ref _activeAddItemJobs);
                }
            }

            /* Do not invalidate cache: AddItemCoreAsync already updated _cachedInventory for each added item. */
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
                    job.Completion?.TrySetResult(Fail<bool>("Queued use-item job was cancelled.", StarApiResultCode.Network));
                    continue;
                }

                Interlocked.Increment(ref _activeUseItemJobs);
                try
                {
                    var result = await UseItemCoreAsync(job.ItemName, job.Context, job.CancellationToken).ConfigureAwait(false);
                    job.Completion?.TrySetResult(result);
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
        var xp = GetIntProperty(element, "XP") ?? GetIntProperty(element, "xp")
            ?? GetIntProperty(element, "TotalXP") ?? GetIntProperty(element, "totalXp");
        if (xp is null && TryGetProperty(element, "AvatarDetail", out var detailEl))
            xp = GetIntProperty(detailEl, "XP") ?? GetIntProperty(detailEl, "xp");
        if (xp is null && TryGetProperty(element, "avatarDetail", out var detailEl2))
            xp = GetIntProperty(detailEl2, "XP") ?? GetIntProperty(detailEl2, "xp");
        return new StarAvatarProfile
        {
            Id = id,
            Username = GetStringProperty(element, "Username") ?? string.Empty,
            Email = GetStringProperty(element, "Email") ?? string.Empty,
            FirstName = GetStringProperty(element, "FirstName") ?? string.Empty,
            LastName = GetStringProperty(element, "LastName") ?? string.Empty,
            XP = xp ?? 0
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

    private static StarQuestInfo? ParseSingleQuestInfo(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var objectives = new List<StarQuestObjective>();
        if (TryGetProperty(element, "Objectives", out var objElement) && objElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var objective in objElement.EnumerateArray())
            {
                if (objective.ValueKind != JsonValueKind.Object) continue;
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
        else if (TryGetProperty(element, "Quests", out var questsElement) && questsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var sub in questsElement.EnumerateArray())
            {
                if (sub.ValueKind != JsonValueKind.Object) continue;
                objectives.Add(new StarQuestObjective
                {
                    Id = GetStringProperty(sub, "Id") ?? string.Empty,
                    Description = GetStringProperty(sub, "Description") ?? string.Empty,
                    GameSource = GetStringProperty(sub, "GameSource") ?? string.Empty,
                    ItemRequired = GetStringProperty(sub, "ItemRequired") ?? string.Empty,
                    IsCompleted = GetBoolProperty(sub, "IsCompleted")
                });
            }
        }

        return new StarQuestInfo
        {
            Id = GetStringProperty(element, "Id") ?? string.Empty,
            Name = GetStringProperty(element, "Name") ?? string.Empty,
            Description = GetStringProperty(element, "Description") ?? string.Empty,
            Status = GetStringProperty(element, "Status") ?? string.Empty,
            Objectives = objectives
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
        public int Quantity { get; set; } = 1;
        /// <summary>From API / InventoryItem holon.</summary>
        public string? GameSource { get; set; }
        /// <summary>From API / InventoryItem holon.</summary>
        public string? ItemType { get; set; }
        /// <summary>NFT ID when item is linked to NFTHolon (from MetaData or root). Persists so [NFT] prefix shows in Quake/Doom after reload.</summary>
        public string? NftId { get; set; }
    }

    /// <summary>One row per item type: accumulated delta until flushed to API. Used by GetInventory merge and background flush.</summary>
    private sealed class LocalPendingEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string GameSource { get; set; } = string.Empty;
        public string ItemType { get; set; } = "KeyItem";
        public int Quantity { get; set; }
        /// <summary>Set when mint completes (pickup-with-mint) so merge shows [NFT] prefix in Quake/Doom overlay.</summary>
        public string? NftId { get; set; }
    }

    private sealed class PendingPickupWithMintJob
    {
        public PendingPickupWithMintJob(string itemName, string description, string gameSource, string itemType, bool doMint, string? provider, string? sendToAddressAfterMinting, int quantity)
        {
            ItemName = itemName;
            Description = description;
            GameSource = gameSource;
            ItemType = itemType;
            DoMint = doMint;
            Provider = provider;
            SendToAddressAfterMinting = sendToAddressAfterMinting;
            Quantity = quantity < 1 ? 1 : quantity;
        }

        public string ItemName { get; }
        public string Description { get; }
        public string GameSource { get; }
        public string ItemType { get; }
        public bool DoMint { get; }
        public string? Provider { get; }
        public string? SendToAddressAfterMinting { get; }
        public int Quantity { get; }
    }

    private sealed class PendingMonsterKillJob
    {
        public PendingMonsterKillJob(string engineName, string displayName, int xp, bool isBoss, bool doMint, string? provider, string gameSource)
        {
            EngineName = engineName;
            DisplayName = displayName;
            Xp = xp;
            IsBoss = isBoss;
            DoMint = doMint;
            Provider = provider ?? "SolanaOASIS";
            GameSource = gameSource ?? "ODOOM";
        }

        public string EngineName { get; }
        public string DisplayName { get; }
        public int Xp { get; }
        public bool IsBoss { get; }
        public bool DoMint { get; }
        public string Provider { get; }
        public string GameSource { get; }
    }

    private sealed class PendingAddItemJob
    {
        public PendingAddItemJob(string itemName, string description, string gameSource, string itemType, string? nftId, int quantity, bool stack, CancellationToken cancellationToken, TaskCompletionSource<OASISResult<StarItem>>? completion)
        {
            ItemName = itemName;
            Description = description;
            GameSource = gameSource;
            ItemType = string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType;
            NftId = string.IsNullOrWhiteSpace(nftId) ? null : nftId;
            Quantity = quantity < 1 ? 1 : quantity;
            Stack = stack;
            CancellationToken = cancellationToken;
            Completion = completion;
        }

        public string ItemName { get; }
        public string Description { get; }
        public string GameSource { get; }
        public string ItemType { get; }
        public string? NftId { get; }
        public int Quantity { get; }
        public bool Stack { get; }
        public CancellationToken CancellationToken { get; }
        public TaskCompletionSource<OASISResult<StarItem>>? Completion { get; }
    }

    private sealed class PendingUseItemJob
    {
        public PendingUseItemJob(string itemName, string? context, CancellationToken cancellationToken, TaskCompletionSource<OASISResult<bool>>? completion)
        {
            ItemName = itemName;
            Context = context;
            CancellationToken = cancellationToken;
            Completion = completion;
        }

        public string ItemName { get; }
        public string? Context { get; }
        public CancellationToken CancellationToken { get; }
        public TaskCompletionSource<OASISResult<bool>>? Completion { get; }
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
    public int quantity;
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
    private static readonly object BackgroundErrorLock = new();
    private static string? _lastBackgroundError;
    private static readonly ConcurrentQueue<string> _consoleLogQueue = new();
    private const int MaxConsoleLogMessages = 64;
    private static StarApiClient? _client;
    private static byte* _lastError;
    private static delegate* unmanaged[Cdecl]<int, void*, void> _callback;
    private static void* _callbackUserData;

    /// <summary>Set the last background error (mint/add_item failure or pickup not queued). Consumed by star_api_consume_last_background_error for game console display.</summary>
    public static void SetLastBackgroundError(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        lock (BackgroundErrorLock)
            _lastBackgroundError = message;
    }

    /// <summary>Return and clear the last background error. Used by star_api_consume_last_background_error.</summary>
    public static string? TryConsumeLastBackgroundError()
    {
        lock (BackgroundErrorLock)
        {
            var msg = _lastBackgroundError;
            _lastBackgroundError = null;
            return msg;
        }
    }

    /// <summary>Enqueue a message for the game console (consumed by star_api_consume_console_log).</summary>
    public static void EnqueueConsoleLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        while (_consoleLogQueue.Count >= MaxConsoleLogMessages && _consoleLogQueue.TryDequeue(out _)) { }
        _consoleLogQueue.Enqueue(message);
    }

    /// <summary>Dequeue one console log message for the game to display. Used by star_api_consume_console_log.</summary>
    public static string? TryConsumeConsoleLog()
    {
        return _consoleLogQueue.TryDequeue(out var msg) ? msg : null;
    }

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
                dst->quantity = src.Quantity;
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

    [UnmanagedCallersOnly(EntryPoint = "star_api_invalidate_inventory_cache", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiInvalidateInventoryCache()
    {
        var client = GetClient();
        client?.InvalidateInventoryCache();
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_clear_cache", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiClearCache()
    {
        var client = GetClient();
        client?.ClearCache();
    }

    /// <summary>Add item to avatar inventory. quantity: amount to add; stack: 1 = increment if exists, 0 = error if exists.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_add_item", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiAddItem(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, sbyte* nftId, int quantity, int stack)
    {
        var client = GetClient();
        if (client is null)
        {
            StarApiLog("star_api_add_item: client is null");
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);
        }

        var name = PtrToString(itemName) ?? string.Empty;
        var desc = PtrToString(description) ?? string.Empty;
        var source = PtrToString(gameSource) ?? string.Empty;
        var type = PtrToString(itemType) ?? "KeyItem";
        var nftIdStr = PtrToString(nftId);
        var nftIdOpt = string.IsNullOrWhiteSpace(nftIdStr) ? null : nftIdStr;
        var qty = quantity < 1 ? 1 : quantity;
        var doStack = stack != 0;

        StarApiLog($"star_api_add_item: name='{name}' quantity={qty} stack={doStack} (calling AddItemAsync on thread pool)");

        var result = Task.Run(() => client.AddItemAsync(name, desc, source, type, nftIdOpt, qty, doStack).GetAwaiter().GetResult()).GetAwaiter().GetResult();

        var code = FinalizeResult(result);
        StarApiLog($"star_api_add_item: result IsError={result.IsError} code={(int)code} message={result.Message ?? "(ok)"}");
        return (int)code;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_add_item", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueAddItem(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, sbyte* nftId, int quantity, int stack)
    {
        var client = GetClient();
        if (client is null)
            return;
        var nftIdStr = PtrToString(nftId);
        var qty = quantity < 1 ? 1 : quantity;
        var doStack = stack != 0;
        client.EnqueueAddItemJobOnly(
            PtrToString(itemName) ?? string.Empty,
            PtrToString(description) ?? string.Empty,
            PtrToString(gameSource) ?? string.Empty,
            PtrToString(itemType) ?? "KeyItem",
            string.IsNullOrWhiteSpace(nftIdStr) ? null : nftIdStr,
            qty,
            doStack);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_add_xp", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueAddXp(int amount)
    {
        var client = GetClient();
        if (client is null) return;
        if (amount < 0) return;
        client.EnqueueAddXpJobOnly(amount);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_monster_kill", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueMonsterKill(sbyte* engineName, sbyte* displayName, int xp, int isBoss, int doMint, sbyte* provider, sbyte* gameSource)
    {
        var client = GetClient();
        if (client is null) return;
        client.EnqueueMonsterKillJobOnly(
            PtrToString(engineName) ?? string.Empty,
            PtrToString(displayName) ?? string.Empty,
            xp,
            isBoss != 0,
            doMint != 0,
            PtrToString(provider),
            PtrToString(gameSource));
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_get_avatar_xp", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiGetAvatarXp(int* xpOut)
    {
        var client = GetClient();
        if (client is null) return 0;
        if (xpOut is not null)
            *xpOut = client.GetCachedAvatarXp();
        return 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_refresh_avatar_xp", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiRefreshAvatarXp()
    {
        var client = GetClient();
        if (client is null) return;
        client.RefreshAvatarXp();
    }

    /// <summary>Block until avatar XP is loaded from API and cache is updated. Call in auth-done callback before setting "beamed in" so HUD shows correct XP immediately.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_refresh_avatar_xp_blocking", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiRefreshAvatarXpBlocking()
    {
        var client = GetClient();
        if (client is null) return;
        client.LoadAvatarXpBlocking();
    }

    /// <summary>Queue pickup with optional mint; C# client does mint (if do_mint) then add_item in background. Same pattern as queue_add_item.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_pickup_with_mint", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueuePickupWithMint(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, int doMint, sbyte* provider, sbyte* sendToAddressAfterMinting, int quantity)
    {
        var client = GetClient();
        if (client is null)
        {
            SetLastBackgroundError("STAR: Pickup not queued (client not initialized).");
            return;
        }
        var qty = quantity < 1 ? 1 : quantity;
        client.EnqueuePickupWithMintJobOnly(
            PtrToString(itemName) ?? string.Empty,
            PtrToString(description) ?? string.Empty,
            PtrToString(gameSource) ?? string.Empty,
            PtrToString(itemType) ?? "KeyItem",
            doMint != 0,
            PtrToString(provider),
            PtrToString(sendToAddressAfterMinting),
            qty);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_flush_add_item_jobs", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiFlushAddItemJobs()
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);
        var result = client.FlushAddItemJobsAsync(CancellationToken.None).GetAwaiter().GetResult();
        return (int)FinalizeResult(result);
    }

    /// <summary>Mint an NFT for an inventory item via WEB4 OASIS API (NFTHolon). Returns NFT ID to pass to star_api_add_item as nft_id. Optional hash_out for tx hash/signature. provider defaults to SolanaOASIS. Note: mint is currently synchronous (blocking); add_item is queued and flushed async.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_mint_inventory_nft", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiMintInventoryNft(sbyte* itemName, sbyte* description, sbyte* gameSource, sbyte* itemType, sbyte* provider, sbyte* nftIdOut, sbyte* hashOut, sbyte* sendToAddressAfterMinting)
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
            PtrToString(provider),
            PtrToString(sendToAddressAfterMinting)).GetAwaiter().GetResult();

        if (result.IsError)
        {
            SetError(result.Message ?? "Mint failed.");
            InvokeCallback(ExtractCode(result));
            return (int)ExtractCode(result);
        }

        var (nftId, hash) = result.Result;
        WriteUtf8ToOutput(nftId ?? string.Empty, nftIdOut, 128);
        if (hashOut is not null)
            WriteUtf8ToOutput(hash ?? string.Empty, hashOut, 128);
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

    [UnmanagedCallersOnly(EntryPoint = "star_api_queue_use_item", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarApiQueueUseItem(sbyte* itemName, sbyte* context)
    {
        var client = GetClient();
        if (client is null)
            return;
        client.EnqueueUseItemJobOnly(PtrToString(itemName) ?? string.Empty, PtrToString(context));
    }

    [UnmanagedCallersOnly(EntryPoint = "star_api_flush_use_item_jobs", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiFlushUseItemJobs()
    {
        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);
        var result = client.FlushUseItemJobsAsync(CancellationToken.None).GetAwaiter().GetResult();
        return (int)FinalizeResult(result);
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

    [UnmanagedCallersOnly(EntryPoint = "star_api_create_monster_nft", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiCreateMonsterNft(sbyte* monsterName, sbyte* description, sbyte* gameSource, sbyte* monsterStats, sbyte* provider, sbyte* nftIdOut)
    {
        if (nftIdOut is null)
            return (int)SetErrorAndReturn("nftIdOut buffer must not be null.", StarApiResultCode.InvalidParam);

        var client = GetClient();
        if (client is null)
            return (int)SetErrorAndReturn("Client is not initialized.", StarApiResultCode.NotInitialized);

        var result = client.CreateMonsterNftAsync(
            PtrToString(monsterName) ?? string.Empty,
            PtrToString(description),
            PtrToString(gameSource),
            PtrToString(monsterStats),
            PtrToString(provider)).GetAwaiter().GetResult();

        var code = FinalizeResult(result);
        if (code == StarApiResultCode.Success && !string.IsNullOrWhiteSpace(result.Result.NftId))
            WriteUtf8ToOutput(result.Result.NftId, nftIdOut, 64);
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

    /// <summary>Consume last mint result (from background pickup-with-mint). Returns 1 if result was available and written to buffers, 0 otherwise. Buffers are null-terminated. Use from game pump/frame to show NFT ID and hash in console.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_consume_last_mint_result", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiConsumeLastMintResult(sbyte* itemNameOut, nuint itemNameSize, sbyte* nftIdOut, nuint nftIdSize, sbyte* hashOut, nuint hashSize)
    {
        var client = GetClient();
        if (client is null || itemNameOut is null || nftIdOut is null || hashOut is null)
            return 0;
        if (!client.ConsumeLastMintResult(out var itemName, out var nftId, out var hash))
            return 0;
        var isize = (int)Math.Min(itemNameSize, int.MaxValue);
        var nsize = (int)Math.Min(nftIdSize, int.MaxValue);
        var hsize = (int)Math.Min(hashSize, int.MaxValue);
        if (isize > 0) WriteUtf8ToOutput(itemName ?? string.Empty, itemNameOut, isize);
        if (nsize > 0) WriteUtf8ToOutput(nftId ?? string.Empty, nftIdOut, nsize);
        if (hsize > 0) WriteUtf8ToOutput(hash ?? string.Empty, hashOut, hsize);
        return 1;
    }

    /// <summary>Consume last background error (mint/add_item failure or pickup not queued). Writes message to buf (null-terminated). Returns 1 if an error was available, 0 otherwise. Call from game pump to show errors in console.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_consume_last_background_error", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiConsumeLastBackgroundError(sbyte* buf, nuint size)
    {
        var msg = TryConsumeLastBackgroundError();
        if (msg is null || buf == null || size == 0) return 0;
        var len = (int)Math.Min(size, int.MaxValue);
        WriteUtf8ToOutput(msg, buf, len);
        return 1;
    }

    /// <summary>Consume one STAR log message for the game console. Returns 1 if a message was copied to buf, 0 otherwise. Call from game pump each frame to show STAR logs in console.</summary>
    [UnmanagedCallersOnly(EntryPoint = "star_api_consume_console_log", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarApiConsumeConsoleLog(sbyte* buf, nuint size)
    {
        var msg = TryConsumeConsoleLog();
        if (msg is null || buf == null || size == 0) return 0;
        var len = (int)Math.Min(size, int.MaxValue);
        WriteUtf8ToOutput(msg, buf, len);
        return 1;
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

    private static readonly object LogLock = new();
    internal static void StarApiLog(string message)
    {
        var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] {message}";
        Trace.WriteLine(line);
        try
        {
            var dir = Environment.CurrentDirectory;
            if (string.IsNullOrEmpty(dir)) dir = AppContext.BaseDirectory ?? ".";
            var path = Path.Combine(dir, "star_api.log");
            lock (LogLock)
                File.AppendAllText(path, line + Environment.NewLine);
        }
        catch { /* ignore file write errors */ }
        EnqueueConsoleLog(message);
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

