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
    /* ── Map entity list ──────────────────────────────────────────────────── */

    /// <summary>Fetch the cross-game entity list for a map from STAR API (GET /api/maps/{gameId}/{mapName}/entities). Returns raw JSON array.</summary>
    public async Task<OASISResult<string>> GetMapEntitiesAsync(string gameId, string mapName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseApiUrl))
            return Fail<string>("STAR API base URL is not set.", StarApiResultCode.NotInitialized);
        var gId = Uri.EscapeDataString(gameId);
        var mName = Uri.EscapeDataString(mapName);
        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/maps/{gId}/{mName}/entities", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            OGEngineExports.StarApiLogFileOnly($"[MapEntities] GetMapEntities error: gameId={gameId} mapName={mapName} {response.Message}");
            return Fail<string>(response.Message ?? "GetMapEntities failed.", StarApiResultCode.ApiError);
        }
        OGEngineExports.StarApiLogFileOnly($"[MapEntities] GetMapEntities: OK gameId={gameId} mapName={mapName}");
        return Success(response.Result ?? "[]", StarApiResultCode.Success, "Map entities retrieved.");
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

    /// <summary>Try to refresh JWT using refresh token so play is not interrupted when token expires. Uses OASIS refresh-token endpoint (cookie or body). Tries _oasisBaseUrl first, then _baseApiUrl for STAR API–only setups.</summary>
    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        await _tokenRefreshSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            /* Another caller may have refreshed while we waited on the semaphore. */
            string? jwtAfterWait;
            lock (_stateLock) { jwtAfterWait = _jwtToken; }
            if (!string.IsNullOrWhiteSpace(jwtAfterWait))
            {
                var exp = GetJwtExpirationUtc(jwtAfterWait);
                if (exp.HasValue && exp.Value > DateTime.UtcNow.AddSeconds(30))
                {
                    OGEngineExports.StarApiLogFileOnly("[Auth] Token refresh skipped: JWT already valid (another caller refreshed).");
                    return true;
                }
            }

            string? refreshToken;
            string oasisBase;
            string starBase;
            lock (_stateLock)
            {
                refreshToken = _refreshToken;
                oasisBase = _oasisBaseUrl ?? string.Empty;
                starBase = _baseApiUrl ?? string.Empty;
            }
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                OGEngineExports.StarApiLogFileOnly("[Auth] Token refresh skipped: no refresh token (save session after beam-in to persist it).");
                return false;
            }
            /* Prefer OASIS (Web4) URL; fall back to STAR API (Web5) so refresh works when only _baseApiUrl is set. */
            var baseUrl = !string.IsNullOrWhiteSpace(oasisBase) ? oasisBase.TrimEnd('/') : !string.IsNullOrWhiteSpace(starBase) ? starBase.TrimEnd('/') : null;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                OGEngineExports.StarApiLogFileOnly("[Auth] Token refresh skipped: no OASIS or STAR API base URL set.");
                return false;
            }
            if (_httpClient is null)
            {
                OGEngineExports.StarApiLogFileOnly("[Auth] Token refresh skipped: HTTP client is null.");
                return false;
            }

            try
            {
                var url = $"{baseUrl}/api/avatar/refresh-token";
                var usedOasis = !string.IsNullOrWhiteSpace(oasisBase);
                OGEngineExports.StarApiLogFileOnly($"[Auth] Token refresh: POST {(usedOasis ? "OASIS" : "STAR API")} url={url}");
                /* Do not send Authorization header: ONODE JwtMiddleware validates the JWT on every request. If we send the expired JWT, middleware returns 401 before the refresh-token controller runs. */
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                /* WEB4/ONODE reads refreshToken from cookie for browsers; HttpClient often does not send cookies the same way — send JSON body (ONODE RefreshTokenRequest) as primary. */
                var refreshBody = BuildJson(w =>
                {
                    w.WriteStartObject();
                    w.WriteString("refreshToken", refreshToken);
                    w.WriteEndObject();
                });
                request.Content = new StringContent(refreshBody, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                var responseBody = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : string.Empty;

                if (!response.IsSuccessStatusCode)
                {
                    OGEngineExports.StarApiLogFileOnly($"[Auth] Token refresh failed: HTTP {(int)response.StatusCode} url={url} responseBody={responseBody}");
                    OGEngineExports.StarApiLog($"[Auth] Token refresh failed: HTTP {(int)response.StatusCode} (full body in ogengine.log). Deploy ONODE with POST /api/avatar/refresh-token accepting JSON {{\"refreshToken\":\"...\"}} if renew always fails.");
                    return false;
                }

                var parseResult = ParseEnvelopeOrPayload(responseBody, out var resultElement, out _, out var parseErrMsg);
                if (!parseResult)
                {
                    var msg = string.IsNullOrWhiteSpace(parseErrMsg) ? "API returned error envelope." : parseErrMsg;
                    OGEngineExports.StarApiLogFileOnly($"[Auth] Token refresh parse failed: {msg} url={url} responseBody={responseBody}");
                    OGEngineExports.StarApiLog($"[Auth] Token refresh failed: {msg} (full body in ogengine.log)");
                    return false;
                }
                if (resultElement.ValueKind == JsonValueKind.Object && GetBoolProperty(resultElement, "IsError"))
                {
                    var msg = GetStringProperty(resultElement, "Message");
                    var em = string.IsNullOrWhiteSpace(msg) ? "API returned an error." : msg!;
                    OGEngineExports.StarApiLogFileOnly($"[Auth] Token refresh OASISResult IsError: {em} url={url} responseBody={responseBody}");
                    OGEngineExports.StarApiLog($"[Auth] Token refresh failed: {em} (details in ogengine.log)");
                    return false;
                }
                AvatarAuthResponse? auth = ParseAvatarAuthResponse(resultElement);
                if (auth is null || string.IsNullOrWhiteSpace(auth.JwtToken))
                {
                    try
                    {
                        using var rawDoc = JsonDocument.Parse(responseBody);
                        var rawJwt = FindStringRecursive(rawDoc.RootElement, "JwtToken") ?? FindStringRecursive(rawDoc.RootElement, "Token")
                            ?? FindStringRecursive(rawDoc.RootElement, "accessToken") ?? FindStringRecursive(rawDoc.RootElement, "access_token")
                            ?? FindStringRecursive(rawDoc.RootElement, "jwt");
                        var rawRefresh = FindStringRecursive(rawDoc.RootElement, "RefreshToken");
                        if (!string.IsNullOrWhiteSpace(rawJwt))
                            auth = new AvatarAuthResponse { JwtToken = rawJwt, RefreshToken = rawRefresh };
                    }
                    catch { /* ignore */ }
                }
                if (auth is null || string.IsNullOrWhiteSpace(auth.JwtToken))
                {
                    OGEngineExports.StarApiLogFileOnly($"[Auth] Token refresh: could not parse JwtToken from envelope. url={url} responseBody={responseBody}");
                    OGEngineExports.StarApiLog("[Auth] Token refresh failed: no JwtToken in response (full JSON in ogengine.log).");
                    return false;
                }

                lock (_stateLock)
                {
                    _jwtToken = auth.JwtToken;
                    if (!string.IsNullOrWhiteSpace(auth.RefreshToken))
                        _refreshToken = auth.RefreshToken;
                    if (_httpClient is not null)
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
                }
                OGEngineExports.StarApiLog("[Auth] JWT refreshed successfully.");
                ScheduleBackgroundTokenRefresh();
                return true;
            }
            catch (Exception ex)
            {
                OGEngineExports.StarApiLog($"[Auth] Token refresh exception: {ex.Message}");
                return false;
            }
        }
        finally
        {
            _tokenRefreshSemaphore.Release();
        }
    }

    private static readonly object _tokenRefreshScheduledLock = new();
    private static bool _tokenRefreshScheduled;

    /// <summary>Schedule a single background refresh shortly before JWT expiry so play is not interrupted.</summary>
    private void ScheduleBackgroundTokenRefresh()
    {
        lock (_tokenRefreshScheduledLock)
        {
            if (_tokenRefreshScheduled)
                return;
            _tokenRefreshScheduled = true;
        }
        _ = RunOnBackgroundAsync<bool>(async ct =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(10), ct).ConfigureAwait(false);
                for (int i = 0; i < 30; i++)
                {
                    if (ct.IsCancellationRequested) return Success(true, StarApiResultCode.Success, "Cancelled");
                    string? jwt;
                    lock (_stateLock) { jwt = _jwtToken; }
                    if (string.IsNullOrWhiteSpace(jwt)) return Success(true, StarApiResultCode.Success, "No token");
                    var exp = GetJwtExpirationUtc(jwt);
                    if (exp.HasValue && exp.Value > DateTime.UtcNow && (exp.Value - DateTime.UtcNow).TotalMinutes < 5)
                    {
                        var refreshed = await TryRefreshTokenAsync(ct).ConfigureAwait(false);
                        if (refreshed) OGEngineExports.StarApiLog("[Auth] Background token refresh completed.");
                        return Success(true, StarApiResultCode.Success, "Refreshed or skipped");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(20), ct).ConfigureAwait(false);
                }
                return Success(true, StarApiResultCode.Success, "Done");
            }
            finally
            {
                lock (_tokenRefreshScheduledLock) { _tokenRefreshScheduled = false; }
            }
        }, default);
    }

    private static DateTime? GetJwtExpirationUtc(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            var decoded = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(decoded);
            if (doc.RootElement.TryGetProperty("exp", out var expProp) && expProp.ValueKind == JsonValueKind.Number)
            {
                if (expProp.TryGetInt64(out var unix))
                    return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private async Task<OASISResult<string>> SendRawAsync(HttpMethod method, string url, string? bodyJson, CancellationToken cancellationToken)
    {
        if (_httpClient is null)
            return Fail<string>("HTTP client is not initialized.", StarApiResultCode.NotInitialized);

        lock (_stateLock)
        {
            if (_sessionExpiredCleared && string.IsNullOrEmpty(_jwtToken))
                return Fail<string>("Session expired. Please beam in again.", StarApiResultCode.ApiError);
        }

        try
        {
            var result = await SendRawAsyncCore(method, url, bodyJson, cancellationToken).ConfigureAwait(false);
            // On 401, try refresh once and retry the request (minimal JWT timeout fix).
            if (result.IsError && result.Message != null && result.Message.Contains("401", StringComparison.Ordinal))
            {
                var refreshed = await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                if (refreshed)
                    result = await SendRawAsyncCore(method, url, bodyJson, cancellationToken).ConfigureAwait(false);
                else
                {
                    /* Concurrent refresh may have succeeded on another worker; do not clear a good session. */
                    string? jwtCheck;
                    lock (_stateLock) { jwtCheck = _jwtToken; }
                    var exp = string.IsNullOrWhiteSpace(jwtCheck) ? null : GetJwtExpirationUtc(jwtCheck);
                    if (exp.HasValue && exp.Value > DateTime.UtcNow.AddSeconds(15))
                    {
                        OGEngineExports.StarApiLogFileOnly("[Auth] 401 retry: refresh returned false but JWT is valid (concurrent refresh); retrying request once.");
                        result = await SendRawAsyncCore(method, url, bodyJson, cancellationToken).ConfigureAwait(false);
                    }
                    if (result.IsError)
                    {
                        ClearSessionToken();
                        OGEngineExports.StarApiLog("[Auth] JWT expired and refresh failed or no refresh token; session cleared. Please beam in again.");
                    }
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException != null && !string.IsNullOrWhiteSpace(ex.InnerException.Message)
                ? $"Network call failed: {ex.Message} ({ex.InnerException.Message})"
                : $"Network call failed: {ex.Message}";
            return Fail<string>(msg, StarApiResultCode.Network, ex);
        }
    }

    /// <summary>Send request with bounded retries on transient network errors (audit: retry with backoff).</summary>
    private async Task<OASISResult<string>> SendRawWithRetryAsync(HttpMethod method, string url, string? bodyJson, CancellationToken cancellationToken)
    {
        OASISResult<string> last = Fail<string>("No attempt.", StarApiResultCode.Network);
        for (var attempt = 0; attempt < HttpRetryMaxAttempts; attempt++)
        {
            if (attempt > 0)
            {
                var delayMs = attempt <= HttpRetryDelayMs.Length ? HttpRetryDelayMs[attempt - 1] : HttpRetryDelayMs[^1];
                try
                {
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return last;
                }
                OGEngineExports.StarApiLogFileOnly($"[HTTP] Retry attempt {attempt + 1}/{HttpRetryMaxAttempts} after {delayMs}ms: {method.Method} {url}");
            }
            last = await SendRawAsync(method, url, bodyJson, cancellationToken).ConfigureAwait(false);
            if (!last.IsError)
                return last;
            var code = ParseCode(last.ErrorCode, StarApiResultCode.ApiError);
            if (code != StarApiResultCode.Network)
                return last; /* Don't retry auth or API errors. */
        }
        return last;
    }

    /// <summary>WEB5 on Linux may return HTTP 406 with a valid OASIS envelope (isError false, result array/object). Root-level check avoids relying on full envelope unroll for multi-MB bodies.</summary>
    private static bool Is406ResponseWithOasisSuccessResult(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        try
        {
            using var doc = JsonDocument.Parse(body, DeepJsonDocumentOptions);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            if (GetBoolProperty(root, "IsError")) return false;
            if (!TryGetProperty(root, "Result", out var res)) return false;
            return res.ValueKind == JsonValueKind.Array || res.ValueKind == JsonValueKind.Object;
        }
        catch (Exception ex)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[HTTP] 406 success-check: parse failed {ex.GetType().Name}: {ex.Message}"); } catch { /* ignore */ }
            return false;
        }
    }

    private async Task<OASISResult<string>> SendRawAsyncCore(HttpMethod method, string url, string? bodyJson, CancellationToken cancellationToken)
    {
        if (_httpClient is null)
            return Fail<string>("HTTP client is not initialized.", StarApiResultCode.NotInitialized);

        using var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(bodyJson))
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        lock (_stateLock)
        {
            if (!string.IsNullOrWhiteSpace(_avatarId))
                request.Headers.TryAddWithoutValidation("X-Avatar-Id", _avatarId);

            var bearerToken = _jwtToken;
            if (string.IsNullOrWhiteSpace(bearerToken) && _httpClient.DefaultRequestHeaders.Authorization?.Scheme == "Bearer")
                bearerToken = _httpClient.DefaultRequestHeaders.Authorization.Parameter;
            if (!string.IsNullOrWhiteSpace(bearerToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        var responseBody = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : string.Empty;

        if (!response.IsSuccessStatusCode)
        {
            // WEB5 on Linux sometimes returns 406 with a full success JSON body. Never attach response bodies to errors or StarApiLog — multi-MB strings crash native logging.
            if ((int)response.StatusCode == 406 && Is406ResponseWithOasisSuccessResult(responseBody))
            {
                OGEngineExports.StarApiLogFileOnly($"[HTTP] 406 {method.Method} treated as success (OASIS success JSON): {url}");
                return Success(responseBody ?? string.Empty, StarApiResultCode.Success, "Request completed (HTTP 406 with success JSON).");
            }

            var path = url;
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Segments?.Length > 0)
                    path = string.Concat(u.Segments);
            }
            catch { /* use full url if parse fails */ }
            OGEngineExports.StarApiLog($"[HTTP] {(int)response.StatusCode} {method.Method} {path}");
            OGEngineExports.StarApiLogFileOnly($"[HTTP] {(int)response.StatusCode} {method.Method} {path} url={url} bodyLen={responseBody.Length}");
            var failureMessage = $"HTTP {(int)response.StatusCode} ({response.StatusCode}) calling {url}.";
            return Fail<string>(failureMessage, StarApiResultCode.ApiError);
        }

        return Success(responseBody ?? string.Empty, StarApiResultCode.Success, "Request completed successfully.");
    }

}
