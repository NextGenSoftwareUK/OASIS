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

}
