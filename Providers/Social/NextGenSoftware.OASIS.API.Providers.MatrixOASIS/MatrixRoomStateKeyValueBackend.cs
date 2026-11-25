using System;
using System.Collections.Generic;
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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MatrixOASIS;

internal sealed class MatrixCredentials
{
    public string AccessToken { get; private init; }
    public string Username { get; private init; }
    public string Password { get; private init; }

    public static MatrixCredentials FromToken(string accessToken) => string.IsNullOrWhiteSpace(accessToken)
        ? throw new ArgumentException("A Matrix access token is required.", nameof(accessToken))
        : new MatrixCredentials { AccessToken = accessToken };

    public static MatrixCredentials FromPassword(string username, string password) =>
        string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)
            ? throw new ArgumentException("A Matrix username and password are required.")
            : new MatrixCredentials { Username = username, Password = password };
}

/// <summary>
/// Key-value storage as Matrix room state: key = state key of an <c>org.oasis.record</c> event, value = its <c>v</c>
/// field. Empty content is the delete tombstone. Rate-limited requests (429 <c>M_LIMIT_EXCEEDED</c>) are retried after
/// the server's <c>retry_after_ms</c>, as the Client-Server specification requires, a bounded number of times.
/// </summary>
internal sealed class MatrixRoomStateKeyValueBackend : IKeyValueBackend
{
    private const string EventType = "org.oasis.record";
    private const string ValueField = "v";
    // The Matrix event size limit is 65536 bytes of canonical JSON including the envelope; keep headroom for it.
    private const int MaxValueBytes = 60 * 1024;
    private const int MaxRateLimitRetries = 8;
    private readonly HttpClient _http;
    private readonly MatrixCredentials _credentials;
    private readonly string _roomPath;
    private readonly SemaphoreSlim _login = new(1, 1);
    private string _accessToken;

    public MatrixRoomStateKeyValueBackend(string homeserver, MatrixCredentials credentials, string roomId)
    {
        if (!Uri.TryCreate(homeserver?.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            throw new ArgumentException("A Matrix homeserver URL (http or https) is required.", nameof(homeserver));
        if (string.IsNullOrWhiteSpace(roomId) || !roomId.StartsWith('!') || !roomId.Contains(':'))
            throw new ArgumentException("A Matrix room ID (!opaque:server) is required.", nameof(roomId));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _accessToken = credentials.AccessToken;
        _roomPath = "_matrix/client/v3/rooms/" + Uri.EscapeDataString(roomId) + "/state";
        _http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private string RecordPath(string key) => $"{_roomPath}/{EventType}/{Uri.EscapeDataString(key)}";

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, "_matrix/client/v3/account/whoami"), cancellationToken);
        await EnsureSuccessAsync(response, "verify the storage account");
        await ListKeysAsync(string.Empty, cancellationToken);
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, RecordPath(key)), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound && await ErrorCodeAsync(response) == "M_NOT_FOUND") return null;
        await EnsureSuccessAsync(response, $"read '{key}'");
        using var content = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return Value(content.RootElement, key);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, _roomPath), cancellationToken);
        await EnsureSuccessAsync(response, "list the room state");
        using var state = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (state.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("The Matrix room state is not an event array.");
        var keys = new List<string>();
        foreach (var evt in state.RootElement.EnumerateArray())
        {
            if (!evt.TryGetProperty("type", out var type) || type.GetString() != EventType) continue;
            var key = evt.GetProperty("state_key").GetString();
            if (key == null || !key.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (Value(evt.GetProperty("content"), key) != null) keys.Add(key);
        }
        return keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }

    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        if (Encoding.UTF8.GetByteCount(value) > MaxValueBytes)
            throw new InvalidDataException($"The record for '{key}' exceeds the {MaxValueBytes}-byte Matrix state event limit.");
        return SetStateAsync(key, new JsonObject { [ValueField] = value }, cancellationToken);
    }

    // Matrix state cannot be erased; empty content is the removed-state convention. Deleting a missing key succeeds.
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (await GetAsync(key, cancellationToken) == null) return;
        await SetStateAsync(key, new JsonObject(), cancellationToken);
    }

    private async Task SetStateAsync(string key, JsonObject content, CancellationToken cancellationToken)
    {
        var body = content.ToJsonString();
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Put, RecordPath(key))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        }, cancellationToken);
        await EnsureSuccessAsync(response, $"write '{key}'");
        using var ack = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!ack.RootElement.TryGetProperty("event_id", out var eventId) || string.IsNullOrEmpty(eventId.GetString()))
            throw new IOException($"The Matrix homeserver did not return an event ID for the write of '{key}'.");
    }

    private static string Value(JsonElement content, string key)
    {
        if (content.ValueKind != JsonValueKind.Object) throw new InvalidDataException($"Matrix state for '{key}' is not an object.");
        if (!content.TryGetProperty(ValueField, out var value))
        {
            if (content.EnumerateObject().Any()) throw new InvalidDataException($"Matrix state for '{key}' has unexpected fields.");
            return null; // empty content: removed state
        }
        return value.ValueKind == JsonValueKind.String ? value.GetString()
            : throw new InvalidDataException($"Matrix state for '{key}' holds a non-string record value.");
    }

    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> request, CancellationToken cancellationToken)
    {
        await EnsureAccessTokenAsync(cancellationToken);
        for (var attempt = 0; ; attempt++)
        {
            using var message = request();
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            var response = await _http.SendAsync(message, cancellationToken);
            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= MaxRateLimitRetries) return response;
            var delay = await RetryAfterAsync(response);
            response.Dispose();
            await Task.Delay(delay, cancellationToken);
        }
    }

    private async Task EnsureAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken != null) return;
        await _login.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken != null) return;
            var body = new JsonObject
            {
                ["type"] = "m.login.password",
                ["identifier"] = new JsonObject { ["type"] = "m.id.user", ["user"] = _credentials.Username },
                ["password"] = _credentials.Password
            }.ToJsonString();
            using var response = await _http.PostAsync("_matrix/client/v3/login", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
            await EnsureSuccessAsync(response, "log in");
            using var login = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            _accessToken = login.RootElement.GetProperty("access_token").GetString()
                ?? throw new IOException("The Matrix login response has no access token.");
        }
        finally { _login.Release(); }
    }

    private static async Task<TimeSpan> RetryAfterAsync(HttpResponseMessage response)
    {
        try
        {
            using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (error.RootElement.TryGetProperty("retry_after_ms", out var ms) && ms.TryGetInt64(out var value) && value >= 0)
                return TimeSpan.FromMilliseconds(Math.Min(value, 30_000));
        }
        catch (JsonException) { }
        return response.Headers.RetryAfter?.Delta is { } delta ? TimeSpan.FromTicks(Math.Min(delta.Ticks, TimeSpan.FromSeconds(30).Ticks)) : TimeSpan.FromSeconds(1);
    }

    private static async Task<string> ErrorCodeAsync(HttpResponseMessage response)
    {
        try
        {
            using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return error.RootElement.TryGetProperty("errcode", out var code) ? code.GetString() : null;
        }
        catch (JsonException) { return null; }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        throw new IOException($"The Matrix homeserver failed to {operation}: {(int)response.StatusCode} {body[..Math.Min(body.Length, 300)]}");
    }
}
