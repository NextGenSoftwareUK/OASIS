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
    public OASISResult<bool> Init(OGEngineConfig config)
    {
        OGEngineExports.StarApiLog("\n********** GAME LOAD **********");
        if (config is null)
            return Fail<bool>("Invalid configuration.", StarApiResultCode.InvalidParam);
        if (config.Transport == OGEngineTransport.Native)
            return Fail<bool>(
                "Native STAR transport is not implemented in OGEngineClient (NativeAOT). Use star_transport \"remote\" with WEB5/WEB4 URLs, or load a ogengine build that embeds OASIS HyperDrive and OASISBootLoader.",
                StarApiResultCode.InitFailed);
        var web5BaseUrl = config.Web5StarApiBaseUrl;
        if (string.IsNullOrWhiteSpace(web5BaseUrl))
            return Fail<bool>("Invalid configuration.", StarApiResultCode.InvalidParam);

        if (!Uri.TryCreate(web5BaseUrl.TrimEnd('/'), UriKind.Absolute, out var baseUri))
            return Fail<bool>("Web5StarApiBaseUrl must be a valid absolute URL.", StarApiResultCode.InvalidParam);

        var timeout = config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 60;
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
            // Linux Kestrel can return 406 if Accept is only application/json; */* satisfies negotiation (JSON still preferred).
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));

            _baseApiUrl = normalizedBaseUrl;
            _oasisBaseUrl = oasisBaseUrl;
            _avatarId = string.IsNullOrWhiteSpace(config.AvatarId) ? null : config.AvatarId;
            _jwtToken = string.IsNullOrWhiteSpace(config.ApiKey) ? null : config.ApiKey;
            _refreshToken = null;
            _lastError = string.Empty;
            _initialized = true;
            _cachedInventory = null;
            _inventoryFetchTask = null;
            _sessionExpiredCleared = false;

            if (!string.IsNullOrWhiteSpace(config.ApiKey))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

            _questProgressCacheRefresh = config.QuestProgressCacheRefresh;
            _questClientGameSource = string.IsNullOrWhiteSpace(config.ClientGameSource) ? null : config.ClientGameSource.Trim();
        }

        StartWorkers();

        return Success(true, StarApiResultCode.Success, "WEB5 STAR API client initialized successfully.");
    }

    /// <summary>Switch quest progress follow-up: local merge vs full server refresh. Thread-safe.</summary>
    public void SetQuestProgressCacheRefreshMode(QuestProgressCacheRefreshMode mode)
    {
        lock (_stateLock) { _questProgressCacheRefresh = mode; }
    }

    public async Task<OASISResult<bool>> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        await AuthenticateSingleFlight.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
        OGEngineExports.StarApiLogFileOnly("[Auth] AuthenticateAsync called");
        if (!IsInitialized())
        {
            OGEngineExports.StarApiLogFileOnly("[Auth] AuthenticateAsync failed: client not initialized");
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        }

        string oasisUrl;
        lock (_stateLock) { oasisUrl = _oasisBaseUrl ?? string.Empty; }
        if (string.IsNullOrWhiteSpace(oasisUrl))
        {
            OGEngineExports.StarApiLogFileOnly("[Auth] AuthenticateAsync failed: OASIS base URL not set");
            return FailAndCallback<bool>("WEB4 OASIS API base URL is not set. Set OASIS_WEB4_API_BASE_URL or Web4OasisApiBaseUrl (e.g. http://localhost:5555).", StarApiResultCode.InvalidParam);
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            OGEngineExports.StarApiLogFileOnly("[Auth] AuthenticateAsync failed: username or password empty");
            return FailAndCallback<bool>("Username and password are required.", StarApiResultCode.InvalidParam);
        }

        OGEngineExports.StarApiLogFileOnly("\n********** SESSION (BEAM IN) START **********");
        // Allow this login request through: clear "session expired" short-circuit so user can beam in again after 401.
        lock (_stateLock) { _sessionExpiredCleared = false; }

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
            {
                OGEngineExports.StarApiLogFileOnly($"[Auth] AuthenticateAsync API error: {response.Message}");
                return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
            }

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
                    ?? FindStringRecursive(rawDoc.RootElement, "accessToken") ?? FindStringRecursive(rawDoc.RootElement, "access_token")
                    ?? FindStringRecursive(rawDoc.RootElement, "jwt");
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

            string? loggedAvatarId;
            lock (_stateLock)
            {
                if (!string.IsNullOrWhiteSpace(auth.JwtToken))
                    _jwtToken = auth.JwtToken;
                _refreshToken = auth.RefreshToken;
                _avatarId = auth.Id == Guid.Empty ? _avatarId : auth.Id.ToString();
                _loggedInUsername = string.IsNullOrWhiteSpace(username) ? _loggedInUsername : username.Trim();
                _sessionExpiredCleared = false;
                loggedAvatarId = _avatarId;

                if (!string.IsNullOrWhiteSpace(_jwtToken) && _httpClient is not null)
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
            }

            OGEngineExports.StarApiLogFileOnly($"[Auth] Success. BaseApiUrl={(_baseApiUrl != null && _baseApiUrl.Length > 0 ? "set" : "empty")}, OasisBaseUrl={(_oasisBaseUrl != null && _oasisBaseUrl.Length > 0 ? "set" : "empty")}, AvatarId={(!string.IsNullOrEmpty(loggedAvatarId) ? "set" : "empty")}, JWT from auth={(string.IsNullOrEmpty(_jwtToken) ? "no" : "yes (length=" + _jwtToken.Length + ")")}");

            /* Game (Doom/Quake) calls ogengine_refresh_avatar_profile() in its auth-done handler. Do NOT invoke callback here so Quake only runs "profile loaded" when that refresh completes (cache has XP/quest). */
            InvalidateQuestCache(); /* so next quest popup open will GET /api/quests with auth */
            RequestQuestCacheRefreshInBackground(); /* warm quest list once after login (no per-progress refetch) */

            var result = Success(true, StarApiResultCode.Success, "Authentication successful.");
            return result;
        }
        catch (Exception ex)
        {
            OGEngineExports.StarApiLogFileOnly($"[Auth] AuthenticateAsync exception: {ex.Message}");
            return FailAndCallback<bool>($"Authentication failed: {ex.Message}", StarApiResultCode.Network, ex);
        }
        }
        finally
        {
            AuthenticateSingleFlight.Release();
        }
    }

    /// <summary>Run authentication on the background worker so the calling thread does not block. Await the returned task for the result.</summary>
    public Task<OASISResult<bool>> QueueAuthenticateAsync(string username, string password, CancellationToken cancellationToken = default) =>
        RunOnWorkerAsync(DedicatedWorker.AuthSession, ct => AuthenticateAsync(username, password, ct), cancellationToken);

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

        /* Game calls ogengine_refresh_avatar_profile() after beam-in. Do NOT invoke callback so "profile loaded" only runs when that refresh completes. */

        return Success(true, StarApiResultCode.Success, "API key authentication configured.");
    }

    /// <summary>Set avatar ID for subsequent API calls (e.g. after SSO when C++ has avatar_id from auth result). Does not change JWT.</summary>
    public OASISResult<bool> SetAvatarId(string avatarId)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        lock (_stateLock)
            _avatarId = string.IsNullOrWhiteSpace(avatarId) ? null : avatarId;

        /* Do NOT invoke callback; "profile loaded" should only run when ogengine_refresh_avatar_profile completes. */
        return Success(true, StarApiResultCode.Success, "Avatar ID set.");
    }

    /// <summary>Set JWT from persisted session (e.g. oasisstar.json). Avatar ID is extracted from the JWT. Call RestoreSessionAsync to validate and load profile.</summary>
    public OASISResult<bool> SetSavedSession(string jwt)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (string.IsNullOrWhiteSpace(jwt))
            return FailAndCallback<bool>("JWT is required for session restore.", StarApiResultCode.InvalidParam);

        var avatarId = ExtractAvatarIdFromJwt(jwt);
        lock (_stateLock)
        {
            _jwtToken = jwt.Trim();
            _avatarId = avatarId != Guid.Empty ? avatarId.ToString() : _avatarId;
            _sessionExpiredCleared = false;
            if (_httpClient is not null)
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
        }
        OGEngineExports.StarApiLogFileOnly("[Auth] SetSavedSession: JWT set, avatar id from token");
        return Success(true, StarApiResultCode.Success, "Saved session set.");
    }

    /// <summary>Set refresh token from persisted session (e.g. oasisstar.json). Call after SetSavedSession when restoring so 401 can trigger token refresh. Optional.</summary>
    public void SetRefreshToken(string? refreshToken)
    {
        lock (_stateLock)
            _refreshToken = string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken.Trim();
    }

    /// <summary>Current refresh token (for saving to oasisstar.json). Empty if none. Caller should not log.</summary>
    public string? GetCurrentRefreshToken()
    {
        lock (_stateLock) return _refreshToken;
    }

    /// <summary>True if session was cleared due to 401 (JWT expired and refresh failed or no refresh token). Games should clear jwt_token/refresh_token in oasisstar.json when saving so the next launch does not try to restore a dead session.</summary>
    public bool IsSessionExpired()
    {
        lock (_stateLock) return _sessionExpiredCleared;
    }

    /// <summary>Clear in-memory session (JWT, refresh token, avatar id) and Authorization header after 401 when refresh fails. Stops sending expired token and avoids request spam until user re-logs in.</summary>
    private void ClearSessionToken()
    {
        lock (_stateLock)
        {
            _jwtToken = null;
            _refreshToken = null;
            _avatarId = null;
            _sessionExpiredCleared = true;
            if (_httpClient is not null)
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
        }
        InvalidateInventoryCache();
        OGEngineExports.StarApiLogFileOnly("[Auth] Session cleared (expired JWT, refresh failed or no refresh token).");
    }

    /// <summary>Validate current JWT by calling GET avatar/current; on success update cache and invoke callback so game can treat as beamed in. Run on background (e.g. QueueRestoreSessionAsync). Proactively refreshes JWT if expired or expiring within 60s so restore succeeds without waiting for 401.</summary>
    public async Task<OASISResult<bool>> RestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        if (OGEngineExports.GetStarDebug())
            OGEngineExports.StarApiLog("\n********** SESSION RESTORE START **********");
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        string? jwt;
        lock (_stateLock) { jwt = _jwtToken; }
        if (string.IsNullOrWhiteSpace(jwt))
        {
            OGEngineExports.StarApiLogFileOnly("[Auth] RestoreSession: no JWT set");
            return FailAndCallback<bool>("No saved session (JWT) to restore.", StarApiResultCode.InvalidParam);
        }

        /* Proactively refresh if JWT is expired or expiring within 60s so we don't send an expired token and get 401. */
        var exp = GetJwtExpirationUtc(jwt);
        if (exp.HasValue && exp.Value <= DateTime.UtcNow.AddSeconds(60))
        {
            OGEngineExports.StarApiLogFileOnly("[Auth] RestoreSession: JWT expired or expiring soon, refreshing before GET");
            var refreshed = await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);
            if (refreshed)
                OGEngineExports.StarApiLogFileOnly("[Auth] RestoreSession: JWT refreshed, proceeding with GET");
        }

        OGEngineExports.StarApiLogFileOnly("[Auth] RestoreSession: GET avatar/current to validate saved session");
        var result = await GetCurrentAvatarAsync(cancellationToken, invokeCallback: true).ConfigureAwait(false);
        if (result.IsError)
        {
            OGEngineExports.StarApiLogFileOnly($"[Auth] RestoreSession failed: {result.Message}");
            var errCode = ParseCode(result.ErrorCode, StarApiResultCode.ApiError);
            OGEngineExports.InvokeOperationCallback(errCode, OGEngineExports.StarApiOpProfileLoaded);
            return FailAndCallback<bool>(result.Message ?? "Session restore failed.", errCode);
        }
        /* Invoke the same "profile loaded" operation callback that refresh_avatar_profile uses, so the game runs beamed-in logic: tracker, XP, quest cache, etc. */
        OGEngineExports.InvokeOperationCallback(StarApiResultCode.Success, OGEngineExports.StarApiOpProfileLoaded);
        OGEngineExports.StarApiLogFileOnly("[Auth] RestoreSession: success, profile loaded (operation callback invoked)");
        return Success(true, StarApiResultCode.Success, "Session restored.");
    }

    public Task<OASISResult<bool>> QueueRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        var task = RunOnWorkerAsync(DedicatedWorker.AuthSession, ct => RestoreSessionAsync(ct), cancellationToken);
        lock (_stateLock)
        {
            _restoreSessionInFlight = task;
        }
        _ = task.ContinueWith(
            static (t, state) =>
            {
                var self = (OGEngineClient)state!;
                lock (self._stateLock)
                {
                    if (ReferenceEquals(self._restoreSessionInFlight, t))
                        self._restoreSessionInFlight = null;
                }
            },
            this,
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
        return task;
    }

    /// <summary>Username of the currently logged-in avatar (for persistence to oasisstar.json). Empty if not logged in.</summary>
    public string? GetCurrentUsername()
    {
        lock (_stateLock) return _loggedInUsername;
    }

    /// <summary>Current JWT (for persistence to oasisstar.json). Empty if not logged in. Caller should not log or display.</summary>
    public string? GetCurrentJwt()
    {
        lock (_stateLock) return _jwtToken;
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

    public async Task<OASISResult<StarAvatarProfile>> GetCurrentAvatarAsync(CancellationToken cancellationToken = default, bool invokeCallback = true)
    {
        if (!IsInitialized())
            return invokeCallback ? FailAndCallback<StarAvatarProfile>("Client is not initialized.", StarApiResultCode.NotInitialized) : Fail<StarAvatarProfile>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
            return invokeCallback ? FailAndCallback<StarAvatarProfile>(missingWeb4, StarApiResultCode.InvalidParam) : Fail<StarAvatarProfile>(missingWeb4, StarApiResultCode.InvalidParam);

        var url = $"{web4Base}{Web4GetLoggedInAvatarWithXpPath}";
        OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 get-logged-in-avatar-with-xp url={url}");
        var response = await SendRawWithRetryAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            /* Do NOT return Success with a stub profile when GET fails: game would get "profile loaded" but cache has no XP/quest (causes 0 XP in Quake). Always return Fail so callback is not invoked with Success. */
            OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 avatar profile FAILED: IsError=True Message={response.Message ?? "null"} (returning Fail, not stub)");
            return invokeCallback ? FailAndCallback<StarAvatarProfile>(response.Message ?? "Request failed.", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception) : Fail<StarAvatarProfile>(response.Message ?? "Request failed.", ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var len = response.Result?.Length ?? 0;
        if (OGEngineExports.GetStarDebug())
        {
            var responsePreview = len > 0
                ? (len <= 500 ? response.Result! : response.Result!.Substring(0, 500) + "...")
                : "(empty)";
            OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 avatar profile response OK len={len} preview={responsePreview}");
        }
        else
            OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 avatar profile response OK len={len}");

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 avatar profile parse failed: {parseErrorMessage}");
            return invokeCallback ? FailAndCallback<StarAvatarProfile>(parseErrorMessage, parseErrorCode) : Fail<StarAvatarProfile>(parseErrorMessage, parseErrorCode);
        }

        var avatar = ParseAvatarProfile(resultElement, response.Result);
        if (avatar is null || avatar.Id == Guid.Empty)
        {
            OGEngineExports.StarApiLogFileOnly("[Avatar] GET WEB4 avatar profile parse failed: no avatar in response");
            return invokeCallback ? FailAndCallback<StarAvatarProfile>("Could not parse current avatar profile.", StarApiResultCode.ApiError) : Fail<StarAvatarProfile>("Could not parse current avatar profile.", StarApiResultCode.ApiError);
        }

        lock (_stateLock)
        {
            _avatarId = avatar.Id.ToString();
            _cachedAvatarXp = avatar.XP;
            Volatile.Write(ref _cachedAvatarKarma, avatar.Karma);
            /* If user saved a quest/objective after this GET was started, do not let stale response overwrite their choice (fixes "wrong quest" on load). */
            if (!_questTrackerSavedSinceLastGet)
            {
                _cachedActiveQuestId = avatar.ActiveQuestId;
                _cachedActiveObjectiveId = avatar.ActiveObjectiveId;
            }
            else
            {
                _questTrackerSavedSinceLastGet = false;
                try { OGEngineExports.StarApiLogFileOnly($"[Quest] GET WEB4 avatar profile: ignoring quest/objective in response (user saved since GET started; keeping cache)"); } catch { /* ignore */ }
            }
        }

        lock (_stateLock) { if (!string.IsNullOrWhiteSpace(avatar.Username)) _loggedInUsername = avatar.Username; }
        Guid? loadQuestId;
        Guid? loadObjectiveId;
        lock (_stateLock) { loadQuestId = _cachedActiveQuestId; loadObjectiveId = _cachedActiveObjectiveId; }
        OGEngineExports.StarApiLogFileOnly($"[Avatar] GET WEB4 avatar profile OK: XP={avatar.XP} ActiveQuestId={loadQuestId} ActiveObjectiveId={loadObjectiveId} (cache updated)");
        var (loadQuestName, loadObjName) = TryGetQuestAndObjectiveNamesFromCache(loadQuestId, loadObjectiveId);
        try { OGEngineExports.StarApiLogFileOnly($"[Quest] LOAD questId={loadQuestId} objectiveId={loadObjectiveId} questName={loadQuestName ?? "(not in cache)"} objectiveName={loadObjName ?? "(not in cache)"}"); } catch { /* ignore */ }
        LogActiveQuestSnapshot("after_web4_avatar_profile_loaded");
        if (OGEngineExports.GetStarDebug())
        {
            try
            {
                if (loadQuestId.HasValue || loadObjectiveId.HasValue)
                    OGEngineExports.StarApiLog($"[Avatar] Profile loaded: quest={loadQuestId} objective={loadObjectiveId} (tracker can restore)");
                else
                    OGEngineExports.StarApiLog("[Avatar] Profile loaded: no ActiveQuestId/ActiveObjectiveId (tracker will stay clear)");
            }
            catch { /* ignore */ }
        }
        if (invokeCallback) InvokeCallback(StarApiResultCode.Success);
        return Success(avatar, StarApiResultCode.Success, "Current avatar loaded.");
    }

    /// <summary>Run get-current-avatar on the profile worker so the calling thread does not block.</summary>
    public Task<OASISResult<StarAvatarProfile>> QueueGetCurrentAvatarAsync(CancellationToken cancellationToken = default) =>
        RunOnWorkerAsync(DedicatedWorker.Profile, ct => GetCurrentAvatarAsync(ct), cancellationToken);

    public OASISResult<bool> Cleanup()
    {
        StopWorkers();

        lock (_stateLock)
        {
            _restoreSessionInFlight = null;
            _httpClient?.Dispose();
            _httpClient = null;
            _initialized = false;
            _jwtToken = null;
            _refreshToken = null;
            _avatarId = null;
            _lastError = string.Empty;
            _loggedInUsername = null;
            _cachedActiveQuestId = null;
            _cachedActiveObjectiveId = null;
            _questTrackerSavedSinceLastGet = false;
            Volatile.Write(ref _cachedAvatarXp, 0);
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

    /// <summary>Run has-item on the inventory worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueHasItemAsync(string itemName, CancellationToken cancellationToken = default) =>
        RunOnWorkerAsync(DedicatedWorker.Inventory, ct => HasItemAsync(itemName, ct), cancellationToken);

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

    /// <summary>Run get-inventory on the inventory worker so the calling thread does not block.</summary>
    public Task<OASISResult<List<StarItem>>> QueueGetInventoryAsync(CancellationToken cancellationToken = default) =>
        RunOnWorkerAsync(DedicatedWorker.Inventory, ct => GetInventoryAsync(ct), cancellationToken);

    /// <summary>Return current inventory from cache only (merged with pending). No network. Returns null if cache not populated yet.</summary>
    public List<StarItem>? TryGetCachedInventory()
    {
        lock (_inventoryCacheLock)
        {
            if (_cachedInventory is null)
                return null;
            return MergeLocalPendingIntoInventory(_cachedInventory);
        }
    }

    /// <summary>Request inventory fetch in background. When done, operation_callback is invoked with StarApiOpGetInventory. Non-blocking.</summary>
    public void RequestInventoryInBackground()
    {
        if (!IsInitialized())
        {
            // Defer callback so the export returns immediately; avoids blocking/hang when not beamed in (no re-entrant C# from native callback).
            _ = Task.Run(() => OGEngineExports.InvokeOperationCallback(StarApiResultCode.NotInitialized, OGEngineExports.StarApiOpGetInventory));
            return;
        }
        _ = QueueGetInventoryAsync().ContinueWith((Task<OASISResult<List<StarItem>>> task) =>
        {
            var result = task.IsCompletedSuccessfully ? task.Result : new OASISResult<List<StarItem>> { IsError = true, Message = task.Exception?.Message ?? "Inventory fetch failed." };
            var code = result.IsError ? ParseCode(result.ErrorCode, StarApiResultCode.ApiError) : StarApiResultCode.Success;
            OGEngineExports.InvokeOperationCallback(code, OGEngineExports.StarApiOpGetInventory);
        }, TaskContinuationOptions.None);
    }

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
            if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
                return FailAndCallback<List<StarItem>>(missingWeb4, StarApiResultCode.InvalidParam);

            var response = await SendRawWithRetryAsync(HttpMethod.Get, $"{web4Base}/api/avatar/inventory", null, CancellationToken.None).ConfigureAwait(false);
            if (response.IsError)
            {
                return FailAndCallback<List<StarItem>>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
            }

            var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
            if (!parseResult)
            {
                return FailAndCallback<List<StarItem>>(parseErrorMessage, parseErrorCode);
            }

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

    /// <summary>Clear all client caches (e.g. inventory, quests). Next GetInventory/HasItem/GetQuests will hit the API.</summary>
    public void ClearCache()
    {
        InvalidateInventoryCache();
        InvalidateQuestCache();
    }

    /// <summary>Clear the local quest cache. Next ogengine_get_quests_string will trigger a background refresh. Call after completing objectives if you want the popup to show fresh data.</summary>
    public void InvalidateQuestCache()
    {
        lock (_questsCacheLock)
        {
            _questsCacheString = null;
            _cachedQuestList = null;
            _questsFilterLastLogTop = (0, 0);
            _questsFilterLastLogObjectives = ("", -1);
            _questsFilterLastLogSubQuests = ("", -1);
            _questsFilterLastLogPrereqs = ("", -1);
            _questObjectivesHydrating.Clear();
        }
    }

    /// <summary>End of a quest-worker fetch: clear in-flight flag, run a coalesced refresh if one was requested while busy.</summary>
    private void ReleaseQuestRefreshInProgressSlot(bool invokeQuestsCacheRefreshedCallback)
    {
        bool pending;
        lock (_questsCacheLock)
        {
            _questsRefreshInProgress = false;
            pending = _questsRefreshPending;
            _questsRefreshPending = false;
        }
        if (invokeQuestsCacheRefreshedCallback)
            OGEngineExports.StarApiLogFileOnly("[Quests] Cache refresh complete (native callback suppressed; UI reads cache by polling).");
        if (pending)
            RequestQuestCacheRefreshInBackground(forceRefetch: true);
    }

    /// <summary>Start a background refresh of the quest cache without clearing it. When the fetch completes, the cache is updated. Use when opening the quest popup so the UI shows the previous list immediately and updates when the callback returns.</summary>
    /// <param name="forceRefetch">When false, skips scheduling a network fetch if both structured and string caches are already populated (avoids GET all-for-avatar/game after every profile refresh while playing). Popup / <c>ogengine_refresh_quest_cache_in_background</c> should pass true. After <see cref="InvalidateQuestCache"/>, caches are null so a fetch still runs.</param>
    public void RequestQuestCacheRefreshInBackground(bool forceRefetch = true)
    {
        lock (_questsCacheLock)
        {
            if (_questsRefreshInProgress)
            {
                _questsRefreshPending = true;
                return;
            }
            if (!forceRefetch && _cachedQuestList != null && _questsCacheString != null)
                return;
            _questsRefreshInProgress = true;
        }
        _ = RunOnWorkerAsync(DedicatedWorker.Quests, async ct =>
        {
            var cacheUpdatedOk = false;
            try
            {
                var result = await GetAllQuestsForAvatarAsync(ct).ConfigureAwait(false);
                if (result.IsError)
                {
                    OGEngineExports.StarApiLog("[Quests] Refresh failed (all-for-avatar).");
                    OGEngineExports.StarApiLogFileOnly($"[Quests] Refresh failed: {result.Message ?? "unknown"}");
                    return FailAndCallback<bool>("Quest refresh failed.", StarApiResultCode.Network);
                }
                if (result.Result is null || result.Result.Count == 0)
                {
                    OGEngineExports.StarApiLogFileOnly("[Quests] Refresh OK (0 quests)");
                }
                else
                {
                    var list = result.Result;
                    int withObjectives = list.Count(q => q.Objectives != null && q.Objectives.Count > 0);
                    OGEngineExports.StarApiLogFileOnly($"[Quests] Cache refreshed: {list.Count} quests, {withObjectives} with objectives");
                }
                var serialized = result.Result is null || result.Result.Count == 0
                    ? string.Empty
                    : SerializeQuestsForGame(result.Result);
                Guid? activeForSnap;
                lock (_stateLock) { activeForSnap = _cachedActiveQuestId; }
                if (Volatile.Read(ref _questUiPopupOpen) != 0)
                {
                    LogTopLevelQuestPctSnapshotFromList("GET_all_for_avatar_DISCARDED_incoming_snapshot", result.Result, activeForSnap);
                    try { OGEngineExports.StarApiLogFileOnly("[Quests] GET all-for-avatar DISCARDED (quest popup open — cache unchanged)"); } catch { /* ignore */ }
                    return Success(true, StarApiResultCode.Success, "Quests refresh discarded (popup open).");
                }
                lock (_questsCacheLock)
                {
                    LogTopLevelQuestPctSnapshotUnderQuestLock("GET_all_for_avatar_before_assign", activeForSnap);
                    _questsCacheString = serialized;
                    _cachedQuestList = result.Result;
                    _questsFilterLastLogTop = (0, 0);
                    _questsFilterLastLogObjectives = ("", -1);
                    _questsFilterLastLogSubQuests = ("", -1);
                    _questsFilterLastLogPrereqs = ("", -1);
                    LogTopLevelQuestPctSnapshotUnderQuestLock("GET_all_for_avatar_after_assign", activeForSnap);
                }
                cacheUpdatedOk = true;
                return Success(true, StarApiResultCode.Success, "Quests cache refreshed.");
            }
            catch (Exception ex)
            {
                OGEngineExports.StarApiLogFileOnly($"[Quests] Refresh exception: {ex.Message}");
                return FailAndCallback<bool>("Quest refresh failed.", StarApiResultCode.Network);
            }
            finally
            {
                ReleaseQuestRefreshInProgressSlot(cacheUpdatedOk);
            }
        }, default);
    }

    /// <summary>Filter cached quest list to top-level only (no ParentQuestId or empty). Returns empty list if cache not ready.</summary>
    public List<StarQuestInfo> GetTopLevelQuestsFromCache()
    {
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return new List<StarQuestInfo>();
            return _cachedQuestList.Where(q => string.IsNullOrWhiteSpace(q.ParentQuestId) || q.ParentQuestId == Guid.Empty.ToString()).ToList();
        }
    }

    /// <summary>Get objectives for a parent quest from the quest's Objectives collection (Quest.Objectives). Returns one StarQuestInfo per objective so callers get a list; objectives are no longer separate child quests.</summary>
    public List<StarQuestInfo> GetQuestObjectivesFromCache(string parentQuestId)
    {
        if (string.IsNullOrWhiteSpace(parentQuestId)) return new List<StarQuestInfo>();
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return new List<StarQuestInfo>();
            var id = parentQuestId.Trim();
            var parent = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, id, StringComparison.OrdinalIgnoreCase));
            if (parent?.Objectives == null || parent.Objectives.Count == 0) return new List<StarQuestInfo>();
            var list = new List<StarQuestInfo>();
            for (var i = 0; i < parent.Objectives.Count; i++)
            {
                var o = parent.Objectives[i];
                var objTitle = GetObjectiveRawTitle(o, parent);
                var objBody = GetObjectiveRawDescription(o, objTitle);
                list.Add(new StarQuestInfo
                {
                    Id = string.IsNullOrEmpty(o.Id) ? $"obj_{i}" : o.Id,
                    Name = objTitle,
                    Description = objBody,
                    Status = o.IsCompleted ? "Completed" : "InProgress",
                    Order = o.Order,
                    GameSource = o.GameSource ?? string.Empty,
                    Objectives = new List<StarQuestObjective>(),
                    ParentQuestId = id,
                    LinkedGeoHotSpotId = o.LinkedGeoHotSpotId,
                    ExternalHandoffUri = o.ExternalHandoffUri,
                    Dictionaries = o.Dictionaries
                });
            }
            return list;
        }
    }

    /// <summary>Filter cached quest list to sub-quests (child quests with ParentQuestId set). Sub-quests are full nested quests; objectives are on Quest.Objectives.</summary>
    public List<StarQuestInfo> GetQuestSubQuestsFromCache(string parentQuestId)
    {
        if (string.IsNullOrWhiteSpace(parentQuestId)) return new List<StarQuestInfo>();
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return new List<StarQuestInfo>();
            var id = parentQuestId.Trim();
            return _cachedQuestList.Where(q => string.Equals(q.ParentQuestId, id, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    /// <summary>Resolve prerequisite quest IDs for the given quest to full StarQuestInfo from cache. Returns empty list if cache not ready or quest not found.</summary>
    public List<StarQuestInfo> GetQuestPrereqsFromCache(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId)) return new List<StarQuestInfo>();
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return new List<StarQuestInfo>();
            var quest = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, questId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (quest?.PrerequisiteQuestIds == null || quest.PrerequisiteQuestIds.Count == 0) return new List<StarQuestInfo>();
            var set = new HashSet<string>(quest.PrerequisiteQuestIds, StringComparer.OrdinalIgnoreCase);
            return _cachedQuestList.Where(q => set.Contains(q.Id)).ToList();
        }
    }

    /// <summary>Ensure quest cache is populated in the background. Called from ogengine_get_quests_string when cache is empty so the game thread never blocks.</summary>
    private void EnsureQuestsCacheInBackground()
    {
        lock (_questsCacheLock)
        {
            if (_questsCacheString != null || _questsRefreshInProgress)
                return;
            _questsRefreshInProgress = true;
        }
        OGEngineExports.StarApiLogFileOnly("[Quests] EnsureQuestsCacheInBackground started (fetching all-for-avatar)");
        _ = RunOnWorkerAsync(DedicatedWorker.Quests, async ct =>
        {
            var invokeUi = false;
            try
            {
                var result = await GetAllQuestsForAvatarAsync(ct).ConfigureAwait(false);
                string serialized;
                if (result.IsError)
                {
                    serialized = "Error: Error loading quests. Check console or ogengine.log for details.";
                    OGEngineExports.StarApiLog("[Quests] Load failed (all-for-avatar). See [HTTP] line above or ogengine.log.");
                    OGEngineExports.StarApiLogFileOnly($"[Quests] Load failed detail: {result.Message ?? "unknown"}");
                }
                else if (result.Result is null || result.Result.Count == 0)
                {
                    serialized = string.Empty;
                    OGEngineExports.StarApiLog("[Quests] OK (0 quests)");
                    invokeUi = true;
                }
                else
                {
                    serialized = SerializeQuestsForGame(result.Result);
                    var list = result.Result;
                    int withObjectives = list.Count(q => q.Objectives != null && q.Objectives.Count > 0);
                    OGEngineExports.StarApiLog($"[Quests] Cache updated: {list.Count} quests, {withObjectives} with objectives");
                    invokeUi = true;
                }
                Guid? activeEnsureSnap;
                lock (_stateLock) { activeEnsureSnap = _cachedActiveQuestId; }
                lock (_questsCacheLock)
                {
                    if (!result.IsError && result.Result is { Count: > 0 })
                        LogTopLevelQuestPctSnapshotUnderQuestLock("EnsureQuests_first_load_before_assign", activeEnsureSnap);
                    _questsCacheString = serialized;
                    _cachedQuestList = result.Result;
                    _questsFilterLastLogTop = (0, 0);
                    _questsFilterLastLogObjectives = ("", -1);
                    _questsFilterLastLogSubQuests = ("", -1);
                    _questsFilterLastLogPrereqs = ("", -1);
                    if (!result.IsError && result.Result is { Count: > 0 })
                        LogTopLevelQuestPctSnapshotUnderQuestLock("EnsureQuests_first_load_after_assign", activeEnsureSnap);
                }
                if (invokeUi && !result.IsError)
                    LogActiveQuestSnapshot("after_quest_list_cache_updated");
                return Success(true, StarApiResultCode.Success, "Quests cached.");
            }
            catch (Exception ex)
            {
                var serialized = "Error: Error loading quests. Check console or ogengine.log for details.";
                OGEngineExports.StarApiLog($"[Quests] Exception: {ex.Message}");
                lock (_questsCacheLock)
                {
                    _questsCacheString = serialized;
                    _cachedQuestList = null;
                    _questsFilterLastLogTop = (0, 0);
                    _questsFilterLastLogObjectives = ("", -1);
                    _questsFilterLastLogSubQuests = ("", -1);
                    _questsFilterLastLogPrereqs = ("", -1);
                }
                return FailAndCallback<bool>("Quest refresh failed.", StarApiResultCode.Network);
            }
            finally
            {
                ReleaseQuestRefreshInProgressSlot(invokeUi);
            }
        }, default);
    }

    /// <summary>Get current quest cache for native ogengine_get_quests_string. Returns cached string if available; otherwise starts background refresh and returns null (caller shows "Loading..."). Never blocks.</summary>
    internal bool TryGetQuestsCache(out string? cached)
    {
        lock (_questsCacheLock)
        {
            if (_questsCacheString != null)
            {
                cached = _questsCacheString;
                return true;
            }
        }
        EnsureQuestsCacheInBackground();
        cached = null;
        return false;
    }

    /// <summary>Get display name for the current tracked quest (ActiveQuestId) from cache. Returns null if cache not ready or quest not in list.</summary>
    internal string? TryGetTrackerQuestNameFromCache()
    {
        var questId = GetCachedActiveQuestId();
        if (!questId.HasValue || questId.Value == Guid.Empty) return null;
        var idStr = questId.Value.ToString();
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null) return null;
            var q = _cachedQuestList.FirstOrDefault(q => string.Equals(q.Id, idStr, StringComparison.OrdinalIgnoreCase));
            return q?.Name;
        }
    }

    /// <summary>Get serialized top-level-only quest list for left panel. Filters from cache; returns null if cache not ready.</summary>
    internal bool TryGetTopLevelQuestsCache(out string? cached)
    {
        cached = null;
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _questsCacheString == null) { EnsureQuestsCacheInBackground(); return false; }
            /* Stable order by Id so the same quest always has the same index across reloads and cache refreshes (fixes popup "1 above" drift). */
            var top = _cachedQuestList
                .Where(q => string.IsNullOrWhiteSpace(q.ParentQuestId) || q.ParentQuestId == Guid.Empty.ToString())
                .OrderBy(q => q.Id ?? string.Empty, StringComparer.Ordinal)
                .ToList();
            var total = _cachedQuestList.Count;
            if (_questsFilterLastLogTop != (total, top.Count))
            {
                _questsFilterLastLogTop = (total, top.Count);
            }
            cached = top.Count == 0 ? string.Empty : SerializeQuestsForGame(top);
            return true;
        }
    }

    /// <summary>Get serialized objectives for a parent quest. We do NOT cache a single "right panel" list: every call is filtered by the requested parentQuestId.
    /// Data path: Game passes the selected quest id → TryGetQuestObjectivesCache(id) → find that quest in _cachedQuestList by Id → return that quest's Objectives only.
    /// If the main cache has 0 objectives for that quest, we start an on-demand fetch and merge the result into _cachedQuestList so the next call (next frame) returns them.</summary>
    internal bool TryGetQuestObjectivesCache(string? parentQuestId, out string? cached)
    {
        cached = null;
        if (string.IsNullOrWhiteSpace(parentQuestId)) { cached = string.Empty; return true; }
        var id = parentQuestId.Trim();
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _questsCacheString == null) { EnsureQuestsCacheInBackground(); return false; }
            // 1) Find the requested quest in the cache by Id (not by index – selection change always uses the new id). On-demand fetch merges objectives into this list.
            var parent = _cachedQuestList.Where(q => string.Equals(q.Id, id, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(q => q.Objectives?.Count ?? 0)
                .FirstOrDefault();
            if (parent != null && parent.Objectives != null && parent.Objectives.Count > 0)
            {
                cached = SerializeObjectivesAsQuestLines(parent);
                return true;
            }
            if (parent != null)
            {
                cached = SerializeObjectivesAsQuestLines(parent);
                return true;
            }
            cached = string.Empty;
            return true;
        }
    }

    /// <summary>Replace the quest in _cachedQuestList with a copy that has the given objectives, so the next TryGetQuestObjectivesCache lookup returns them. Caller must hold _questsCacheLock. Increments _questObjectivesCacheVersion so UI can re-read.</summary>
    private void MergeObjectivesIntoCachedQuest(string questId, List<StarQuestObjective> objectives)
    {
        if (_cachedQuestList == null || objectives == null || objectives.Count == 0) return;
        for (var i = 0; i < _cachedQuestList.Count; i++)
        {
            var q = _cachedQuestList[i];
            if (!string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase)) continue;
            var updated = new StarQuestInfo
            {
                Id = q.Id,
                Name = q.Name,
                Description = q.Description,
                Status = q.Status,
                Order = q.Order,
                GameSource = q.GameSource ?? string.Empty,
                Requirements = q.Requirements ?? new List<string>(),
                RewardKarma = q.RewardKarma,
                RewardXP = q.RewardXP,
                CompletionNotes = q.CompletionNotes,
                ParentMissionId = q.ParentMissionId ?? string.Empty,
                ParentQuestId = q.ParentQuestId ?? string.Empty,
                Objectives = objectives,
                PrerequisiteQuestIds = q.PrerequisiteQuestIds ?? new List<string>(),
                LinkedGeoHotSpotId = q.LinkedGeoHotSpotId,
                ExternalHandoffUri = q.ExternalHandoffUri,
                Dictionaries = q.Dictionaries
            };
            _cachedQuestList[i] = updated;
            _questObjectivesCacheVersion++;
            OGEngineExports.StarApiLogFileOnly($"[Quests] Merged {objectives.Count} objectives into cached quest {questId}; cache version now {_questObjectivesCacheVersion}. UI should re-call get_quest_objectives_string to refresh.");
            break;
        }
    }

    /// <summary>Objectives cache version; increments when on-demand fetch merges objectives. UI polls this each frame and re-calls get_quest_objectives_string when it changes so the list refreshes.</summary>
    internal int GetQuestObjectivesCacheVersion()
    {
        lock (_questsCacheLock) { return _questObjectivesCacheVersion; }
    }

    /// <summary>Fetch a single quest by id and return its Objectives (for on-demand fill when all-for-avatar had 0).</summary>
    private async Task<List<StarQuestObjective>?> FetchSingleQuestObjectivesAsync(string questId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrEmpty(_baseApiUrl)) return null;
        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/quests/{questId}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError || string.IsNullOrWhiteSpace(response.Result)) return null;
        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out _, out _);
        if (!parseResult || resultElement.ValueKind != JsonValueKind.Object) return null;
        var src = $"GET.api.quests/{questId.Trim()}";
        LogQuestParseChunkedFileOnly($"[Quest][Parse] source={src} full HTTP body", response.Result);
        LogQuestJsonShapeFileOnly($"[Quest][Parse] source={src} envelope object", resultElement);
        var quest = ParseSingleQuestInfo(resultElement);
        LogParsedSingleQuestModelAudit(src, quest);
        return quest?.Objectives;
    }

    /// <summary>Get serialized sub-quests (child quests with ParentQuestId set) for a parent quest for right panel. Objectives are on Quest.Objectives, not in this list.</summary>
    internal bool TryGetQuestSubQuestsCache(string? parentQuestId, out string? cached)
    {
        cached = null;
        if (string.IsNullOrWhiteSpace(parentQuestId)) { cached = string.Empty; return true; }
        lock (_questsCacheLock)
        {
            if (_cachedQuestList == null || _questsCacheString == null) { EnsureQuestsCacheInBackground(); return false; }
            var id = parentQuestId.Trim();
            var sub = _cachedQuestList.Where(q => string.Equals(q.ParentQuestId, id, StringComparison.OrdinalIgnoreCase)).ToList();
            if (_questsFilterLastLogSubQuests != (id, sub.Count))
            {
                _questsFilterLastLogSubQuests = (id, sub.Count);
            }
            cached = sub.Count == 0 ? string.Empty : SerializeQuestsForGame(sub);
            return true;
        }
    }

    /// <summary>Prefer runtime client game, then objective/quest source, then last progress POST game key.</summary>
    private static string? ResolvePreferredGameKeyForQuestUi(string? clientGs, string? objectiveGameSource, string? questGameSource, string? lastProgressGs)
    {
        foreach (var c in new[] { clientGs, objectiveGameSource, questGameSource, lastProgressGs })
        {
            if (!string.IsNullOrWhiteSpace(c)) return c.Trim();
        }
        return null;
    }

    /// <summary>Requirement payloads often interleave labels (e.g. monster names) with counts; use the first positive integer in the list as the required tally.</summary>
    private static int GetFirstPositiveIntFromStringList(List<string>? list)
    {
        if (list == null) return 0;
        foreach (var s in list)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0)
                return n;
        }
        return 0;
    }

    /// <summary>Progress lists usually store the tally in the first parseable non-negative integer.</summary>
    private static int GetFirstNonNegativeIntFromStringList(List<string>? list)
    {
        if (list == null) return 0;
        foreach (var s in list)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 0)
                return n;
        }
        return 0;
    }

    /// <summary>When API/DB only has ONODE-style <c>Objective</c> text (no parsed dictionaries), map phrases to HUD lines so ODOOM shows Killed 0/N not "Kill N in …".</summary>
    private static void AppendLegacyObjectiveDescriptionProgressLines(string? desc, List<string> outLines)
    {
        if (string.IsNullOrWhiteSpace(desc)) return;
        var t = desc.Trim();
        var parts = t.Split(new[] { " and " }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        foreach (var p in parts)
            TryAppendLegacyObjectivePhrase(p, outLines);
    }

}
