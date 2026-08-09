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

}
