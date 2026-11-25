using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS;

/// <summary>An XRPC error returned by a PDS: HTTP status plus the lexicon error name and message.</summary>
public sealed class AtProtoXrpcException : Exception
{
    public AtProtoXrpcException(string method, HttpStatusCode status, string error, string message)
        : base($"{method} failed ({(int)status} {error}): {message}")
    { Method = method; Status = status; Error = error; }

    public string Method { get; }
    public HttpStatusCode Status { get; }
    public string Error { get; }
}

/// <summary>
/// Authenticated XRPC client for one account on one PDS.
/// Sessions come from <c>com.atproto.server.createSession</c>. When the PDS answers <c>ExpiredToken</c> the access
/// token is renewed with <c>com.atproto.server.refreshSession</c>; when the refresh token itself is expired or
/// revoked a new session is created from the account credentials (the documented re-login path). A 429 is retried
/// only after the server's <c>RateLimit-Reset</c> time, at most <see cref="MaxRateLimitRetries"/> times and never
/// longer than <see cref="MaxRateLimitWait"/> per wait; anything else is surfaced as <see cref="AtProtoXrpcException"/>.
/// </summary>
internal sealed class AtProtoSessionClient : IDisposable
{
    public const int MaxRateLimitRetries = 3;
    public static readonly TimeSpan MaxRateLimitWait = TimeSpan.FromSeconds(90);

    private readonly HttpClient _http;
    private readonly Uri _pds;
    private readonly string _identifier;
    private readonly string _password;
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private Session? _session;

    private sealed record Session(string Did, string Handle, string AccessJwt, string RefreshJwt);

    public AtProtoSessionClient(Uri pds, string identifier, string password, HttpMessageHandler? handler = null)
    {
        if (pds == null || (pds.Scheme != Uri.UriSchemeHttps && pds.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("An http(s) PDS URL is required.", nameof(pds));
        if (string.IsNullOrWhiteSpace(identifier)) throw new ArgumentException("A PDS account identifier is required.", nameof(identifier));
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("A PDS account or app password is required.", nameof(password));
        _pds = new Uri(pds.AbsoluteUri.TrimEnd('/') + "/");
        _identifier = identifier;
        _password = password;
        _http = handler == null ? new HttpClient() : new HttpClient(handler);
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public Uri Pds => _pds;

    /// <summary>The DID of the logged-in account (creates the session on first use).</summary>
    public async Task<string> GetDidAsync(CancellationToken cancellationToken)
        => (await CurrentSessionAsync(cancellationToken)).Did;

    public async Task<string> GetHandleAsync(CancellationToken cancellationToken)
        => (await CurrentSessionAsync(cancellationToken)).Handle;

    public Task<JsonElement> QueryAsync(string nsid, string query, CancellationToken cancellationToken)
        => SendAuthenticatedAsync(nsid, () => new HttpRequestMessage(HttpMethod.Get, new Uri(_pds, $"xrpc/{nsid}?{query}")), cancellationToken);

    public Task<JsonElement> ProcedureAsync(string nsid, JsonObject body, CancellationToken cancellationToken)
    {
        var json = body.ToJsonString();
        return SendAuthenticatedAsync(nsid, () => new HttpRequestMessage(HttpMethod.Post, new Uri(_pds, $"xrpc/{nsid}"))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }, cancellationToken);
    }

    /// <summary>Forgets the session; the next call logs in again.</summary>
    public async Task ResetAsync()
    {
        await _sessionLock.WaitAsync();
        try { _session = null; }
        finally { _sessionLock.Release(); }
    }

    private async Task<JsonElement> SendAuthenticatedAsync(string nsid, Func<HttpRequestMessage> request, CancellationToken cancellationToken)
    {
        var session = await CurrentSessionAsync(cancellationToken);
        try
        {
            return await SendAsync(nsid, request, session.AccessJwt, cancellationToken);
        }
        catch (AtProtoXrpcException expired) when (expired.Error == "ExpiredToken")
        {
            session = await RenewAsync(session, cancellationToken);
            return await SendAsync(nsid, request, session.AccessJwt, cancellationToken);
        }
    }

    private async Task<Session> CurrentSessionAsync(CancellationToken cancellationToken)
    {
        var current = _session;
        if (current != null) return current;
        await _sessionLock.WaitAsync(cancellationToken);
        try { return _session ??= await CreateSessionAsync(cancellationToken); }
        finally { _sessionLock.Release(); }
    }

    private async Task<Session> RenewAsync(Session expired, CancellationToken cancellationToken)
    {
        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            if (_session != null && !ReferenceEquals(_session, expired)) return _session; // another caller already renewed it
            try
            {
                var refreshed = await SendAsync("com.atproto.server.refreshSession",
                    () => new HttpRequestMessage(HttpMethod.Post, new Uri(_pds, "xrpc/com.atproto.server.refreshSession")),
                    expired.RefreshJwt, cancellationToken);
                _session = ParseSession(refreshed);
            }
            catch (AtProtoXrpcException refresh) when (refresh.Error is "ExpiredToken" or "InvalidToken")
            {
                _session = await CreateSessionAsync(cancellationToken);
            }
            if (_session.Did != expired.Did)
                throw new InvalidDataException("The PDS renewed the session for a different account DID.");
            return _session;
        }
        finally { _sessionLock.Release(); }
    }

    private async Task<Session> CreateSessionAsync(CancellationToken cancellationToken)
    {
        var body = new JsonObject { ["identifier"] = _identifier, ["password"] = _password }.ToJsonString();
        var created = await SendAsync("com.atproto.server.createSession",
            () => new HttpRequestMessage(HttpMethod.Post, new Uri(_pds, "xrpc/com.atproto.server.createSession"))
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            }, null, cancellationToken);
        return ParseSession(created);
    }

    private static Session ParseSession(JsonElement json)
    {
        string Text(string name) => json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(value.GetString()) ? value.GetString()! : throw new InvalidDataException($"PDS session response has no '{name}'.");
        var did = Text("did");
        if (!did.StartsWith("did:", StringComparison.Ordinal)) throw new InvalidDataException("PDS session DID is invalid.");
        return new Session(did, Text("handle"), Text("accessJwt"), Text("refreshJwt"));
    }

    private async Task<JsonElement> SendAsync(string nsid, Func<HttpRequestMessage> build, string? bearer, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = build();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (bearer != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            using var response = await _http.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(text)) return default;
                using var ok = JsonDocument.Parse(text);
                return ok.RootElement.Clone();
            }
            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRateLimitRetries)
            {
                await Task.Delay(RateLimitWait(response, nsid), cancellationToken);
                continue;
            }
            var (error, message) = ParseError(text);
            throw new AtProtoXrpcException(nsid, response.StatusCode, error, message);
        }
    }

    /// <summary>Time until the server's RateLimit-Reset (epoch seconds); a reset beyond the bound is an error, not a long sleep.</summary>
    private static TimeSpan RateLimitWait(HttpResponseMessage response, string nsid)
    {
        if (!response.Headers.TryGetValues("RateLimit-Reset", out var values)
            || !long.TryParse(values.FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var reset))
            throw new AtProtoXrpcException(nsid, response.StatusCode, "RateLimitExceeded", "the PDS rate limited the call without a RateLimit-Reset header");
        var wait = DateTimeOffset.FromUnixTimeSeconds(reset) - DateTimeOffset.UtcNow;
        if (wait > MaxRateLimitWait)
            throw new AtProtoXrpcException(nsid, response.StatusCode, "RateLimitExceeded",
                $"the PDS rate limit resets at {DateTimeOffset.FromUnixTimeSeconds(reset):O}, beyond the {MaxRateLimitWait.TotalSeconds:0}s wait bound");
        return wait < TimeSpan.Zero ? TimeSpan.Zero : wait + TimeSpan.FromMilliseconds(250);
    }

    private static (string Error, string Message) ParseError(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            return (root.TryGetProperty("error", out var e) ? e.GetString() ?? "Unknown" : "Unknown",
                root.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty);
        }
        catch (JsonException)
        {
            return ("Unknown", text.Length > 300 ? text[..300] : text);
        }
    }

    public void Dispose()
    {
        _http.Dispose();
        _sessionLock.Dispose();
    }
}
