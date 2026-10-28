using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS;

/// <summary>Native transactional companion calls; record policy lives in the shared base.</summary>
internal sealed class ConvexRecordBackend : IConditionalKeyValueBackend
{
    private readonly Uri _origin;
    private readonly string _adminKey;
    private readonly HttpClient _http;
    private readonly object _readyLock = new();
    private Task? _ready;

    public ConvexRecordBackend(string origin, string adminKey, HttpClient http)
    {
        _origin = Validate(origin, adminKey);
        _adminKey = adminKey;
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    internal static Uri Validate(string origin, string adminKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminKey);
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.UserInfo != "" || uri.Query != "" || uri.Fragment != ""
            || uri.AbsolutePath != "/" || uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback))
            throw new ArgumentException("A root HTTPS deployment origin or explicit loopback HTTP origin is required.", nameof(origin));
        return uri;
    }

    private async Task<JsonElement> CallAsync(string endpoint, string operation, object args, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_origin, "api/" + endpoint));
        request.Headers.Authorization = new AuthenticationHeaderValue("Convex", _adminKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new { path = "oasis/records:" + operation, args, format = "json" }), Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("Convex function omitted its status envelope.");
        if (status.GetString() == "error")
        {
            if (root.TryGetProperty("errorData", out var data) && data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String && code.GetString() == "OASIS_STORAGE_CONFLICT")
                throw new StorageLifecycleConflictException("Convex native revision precondition failed.");
            throw new ConvexFunctionRejectedException();
        }
        if (status.GetString() != "success" || !root.TryGetProperty("value", out var value))
            throw new InvalidDataException("Convex function requires explicit success and value.");
        return value.Clone();
    }

    private Task ReadyAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        lock (_readyLock) return _ready ??= VerifyLayoutAsync(token);
    }
    private async Task VerifyLayoutAsync(CancellationToken token)
    {
        var value = await CallAsync("query", "verify", new { }, token);
        if (value.ValueKind != JsonValueKind.Object || Text(value, "protocol") != "oasis-private-records-v1")
            throw new InvalidDataException("Deploy the matching private-record companion; preserve existing data before changing layouts.");
    }
    public Task VerifyAsync(CancellationToken cancellationToken = default) => ReadyAsync(cancellationToken);

    private static string Text(JsonElement value, string property)
        => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out var field) && field.ValueKind == JsonValueKind.String
            ? field.GetString()! : throw new InvalidDataException("Convex function returned an invalid record field.");
    private static void Key(string key) => ArgumentException.ThrowIfNullOrWhiteSpace(key);

    public async Task<KeyValueObservation?> ReadObservedAsync(string key, CancellationToken cancellationToken = default)
    {
        Key(key); await ReadyAsync(cancellationToken);
        var value = await CallAsync("query", "get", new { key }, cancellationToken);
        if (value.ValueKind == JsonValueKind.Null) return null;
        var revision = Text(value, "revision");
        if (string.IsNullOrWhiteSpace(revision)) throw new InvalidDataException("Convex record omitted its native revision.");
        return new KeyValueObservation(Text(value, "value"), revision);
    }
    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        => (await ReadObservedAsync(key, cancellationToken))?.Value!;

    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => WriteAsync(key, value, "put", null, cancellationToken);
    public Task CreateOnlyAsync(string key, string value, CancellationToken cancellationToken = default)
        => WriteAsync(key, value, "create", null, cancellationToken);
    public Task ReplaceUnchangedAsync(string key, string value, string revision, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        return WriteAsync(key, value, "replace", revision, cancellationToken);
    }
    private async Task WriteAsync(string key, string value, string mode, string? revision, CancellationToken token)
    {
        Key(key); ArgumentNullException.ThrowIfNull(value); await ReadyAsync(token);
        token.ThrowIfCancellationRequested();
        try
        {
            object args = revision == null ? new { key, value, mode } : new { key, value, mode, revision };
            var ack = await CallAsync("mutation", "put", args, token);
            if (Text(ack, "key") != key || string.IsNullOrWhiteSpace(Text(ack, "revision")) || revision != null && Text(ack, "revision") == revision)
                throw new InvalidDataException("Convex write omitted a matching key and new native revision.");
        }
        catch (Exception error) when (!KnownRejection(error)) { throw new ConvexMutationReconciliationException(_origin, key, error); }
    }
    private static bool KnownRejection(Exception error) => error is StorageLifecycleConflictException or ConvexFunctionRejectedException
        || error is HttpRequestException { StatusCode: { } status } && (int)status >= 400 && (int)status < 500 && status != HttpStatusCode.RequestTimeout;

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => DeleteAsync(key, null, cancellationToken);
    public Task DeleteUnchangedAsync(string key, string revision, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        return DeleteAsync(key, revision, cancellationToken);
    }
    private async Task DeleteAsync(string key, string? revision, CancellationToken token)
    {
        Key(key); await ReadyAsync(token); token.ThrowIfCancellationRequested();
        try
        {
            object args = revision == null ? new { key } : new { key, revision };
            var ack = await CallAsync("mutation", "remove", args, token);
            if (Text(ack, "key") != key || !ack.TryGetProperty("deleted", out var deleted) || deleted.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                || revision != null && !deleted.GetBoolean())
                throw new InvalidDataException("Convex delete omitted its native acknowledgement.");
        }
        catch (Exception error) when (!KnownRejection(error)) { throw new ConvexMutationReconciliationException(_origin, key, error); }
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix); await ReadyAsync(cancellationToken);
        var keys = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal); var cursors = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;
        while (true)
        {
            var page = await CallAsync("query", "list", new { prefix, cursor }, cancellationToken);
            if (page.ValueKind != JsonValueKind.Object || !page.TryGetProperty("keys", out var items) || items.ValueKind != JsonValueKind.Array
                || !page.TryGetProperty("complete", out var complete) || complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                || !page.TryGetProperty("cursor", out var next)) throw new InvalidDataException("Convex listing omitted its explicit page state.");
            foreach (var item in items.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String || !item.GetString()!.StartsWith(prefix, StringComparison.Ordinal) || !seen.Add(item.GetString()!))
                    throw new InvalidDataException("Convex listing returned a duplicate or mismatched key.");
                keys.Add(item.GetString()!);
            }
            if (complete.GetBoolean())
            {
                if (next.ValueKind != JsonValueKind.Null) throw new InvalidDataException("Convex completed listing still has a cursor.");
                return keys;
            }
            if (next.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(next.GetString()) || !cursors.Add(next.GetString()!))
                throw new InvalidDataException("Convex incomplete listing omitted a progressing cursor.");
            cursor = next.GetString();
        }
    }
}

internal sealed class ConvexFunctionRejectedException() : Exception("Convex function execution was rejected.");
internal sealed class ConvexMutationReconciliationException(Uri origin, string key, Exception cause)
    : Exception("Convex mutation outcome is uncertain; reconcile the original record before replay.", cause), INativeMutationReconciliationRequired
{
    public string DeploymentOrigin { get; } = origin.AbsoluteUri;
    public string RecordKey { get; } = key;
    public string OriginalTransactionId => string.Empty;
}
