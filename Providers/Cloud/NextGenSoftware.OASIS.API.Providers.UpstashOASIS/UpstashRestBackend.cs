using NextGenSoftware.OASIS.API.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.UpstashOASIS;

/// <summary>Native REST only. Serialized commands forward the opaque session sync token;
/// this is read-your-writes within this client, not cross-client linearizability.</summary>
internal sealed class UpstashRestBackend : IConditionalKeyValueBackend
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly Uri _origin;
    private readonly string _token;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _session = new(1, 1);
    private readonly Lazy<Task> _admission;
    private string? _syncToken;

    public UpstashRestBackend(string origin, string token, HttpClient http)
    {
        Validate(origin, token); ArgumentNullException.ThrowIfNull(http);
        _origin = new Uri(origin); _token = token; _http = http;
        _admission = new Lazy<Task>(VerifyLegacyLayoutAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }
    internal static void Validate(string origin, string token)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.AbsolutePath != "/" ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new ArgumentException("Upstash requires a root HTTPS REST origin.", nameof(origin));
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        _ = new AuthenticationHeaderValue("Bearer", token);
    }
    private static string Key(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!key.StartsWith("oasis/private/v1/", StringComparison.Ordinal))
            throw new ArgumentException("Upstash key is outside the private storage namespace.", nameof(key));
        return key;
    }
    private async Task<T> ExecuteAsync<T>(object[] command, bool mutation, Func<JsonElement, T> parse, CancellationToken token)
    {
        await _session.WaitAsync(token).ConfigureAwait(false);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _origin);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            if (_syncToken != null) request.Headers.Add("upstash-sync-token", _syncToken);
            request.Content = new StringContent(JsonSerializer.Serialize(command), Utf8, "application/json");
            token.ThrowIfCancellationRequested();
            try
            {
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                        throw new UpstashCommandRejectedException("Upstash rejected the command (HTTP " + (int)response.StatusCode + ").");
                    throw new InvalidDataException("Upstash did not return an authoritative HTTP 200 response.");
                }
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token).ConfigureAwait(false));
                var root = json.RootElement;
                if (root.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Upstash response must be an object.");
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in root.EnumerateObject()) if (!names.Add(property.Name)) throw new InvalidDataException("Upstash response contains duplicate fields.");
                if (root.TryGetProperty("error", out _))
                {
                    if (root.TryGetProperty("result", out _)) throw new InvalidDataException("Upstash returned contradictory result/error fields.");
                    throw new UpstashCommandRejectedException("Upstash rejected the native command.");
                }
                if (!root.TryGetProperty("result", out var result)) throw new InvalidDataException("Upstash omitted the native result.");
                if (response.Headers.TryGetValues("upstash-sync-token", out var tokens))
                {
                    var values = tokens.ToArray();
                    if (values.Length != 1 || string.IsNullOrWhiteSpace(values[0])) throw new InvalidDataException("Upstash sync token is malformed.");
                    _syncToken = values[0];
                }
                else if (mutation) throw new InvalidDataException("Upstash mutation omitted its causal sync token.");
                return parse(result);
            }
            catch (StorageLifecycleConflictException) { throw; }
            catch (UpstashCommandRejectedException) { throw; }
            catch (Exception cause) when (mutation)
            { throw new UpstashMutationReconciliationException((string)command[(string)command[0] == "EVAL" ? 3 : 1], (string)command[0], cause); }
        }
        finally { _session.Release(); }
    }
    private async Task VerifyLegacyLayoutAsync()
    {
        foreach (var prefix in new[] { "avatar:", "avatars:", "detail:", "details:", "holon:", "holons:" })
            if ((await ScanAsync(prefix, CancellationToken.None).ConfigureAwait(false)).Count != 0)
                throw new InvalidOperationException("Legacy Upstash records require reviewed migration before private-layout access. No data was changed.");
    }
    private async Task AdmitAsync(CancellationToken token)
    { token.ThrowIfCancellationRequested(); await _admission.Value.WaitAsync(token).ConfigureAwait(false); }
    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        => (await ReadObservedAsync(key, cancellationToken).ConfigureAwait(false))?.Value!;
    public async Task<KeyValueObservation?> ReadObservedAsync(string key, CancellationToken cancellationToken = default)
    {
        key = Key(key); await AdmitAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteAsync(new object[] { "GET", key }, false, result => result.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => new KeyValueObservation(result.GetString()!, RedisStateComparison.Observe(key, Utf8.GetBytes(result.GetString()!))),
            _ => throw new InvalidDataException("Upstash GET returned neither a string nor explicit null.")
        }, cancellationToken).ConfigureAwait(false);
    }
    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => SetAsync(key, value, false, cancellationToken);
    public Task CreateOnlyAsync(string key, string value, CancellationToken cancellationToken = default) => SetAsync(key, value, true, cancellationToken);
    private async Task SetAsync(string key, string value, bool create, CancellationToken token)
    {
        key = Key(key); ArgumentNullException.ThrowIfNull(value); _ = Utf8.GetBytes(value);
        await AdmitAsync(token).ConfigureAwait(false);
        await ExecuteAsync(create ? new object[] { "SET", key, value, "NX" } : new object[] { "SET", key, value }, true, result =>
        {
            if (create && result.ValueKind == JsonValueKind.Null) throw new StorageLifecycleConflictException("Upstash create-only key already exists.");
            if (result.ValueKind != JsonValueKind.String || result.GetString() != "OK") throw new InvalidDataException("Upstash SET acknowledgement is invalid.");
            return true;
        }, token).ConfigureAwait(false);
    }
    public Task ReplaceUnchangedAsync(string key, string value, string revision, CancellationToken cancellationToken = default)
        => ConditionalAsync(key, value, revision, false, cancellationToken);
    public Task DeleteUnchangedAsync(string key, string revision, CancellationToken cancellationToken = default)
        => ConditionalAsync(key, null, revision, true, cancellationToken);
    private async Task ConditionalAsync(string key, string? value, string revision, bool delete, CancellationToken token)
    {
        key = Key(key); var expected = Utf8.GetString(RedisStateComparison.Expected(key, revision));
        if (!delete) { ArgumentNullException.ThrowIfNull(value); _ = Utf8.GetBytes(value); }
        await AdmitAsync(token).ConfigureAwait(false);
        var command = delete
            ? new object[] { "EVAL", RedisStateComparison.DeleteScript, 1, key, expected }
            : new object[] { "EVAL", RedisStateComparison.ReplaceScript, 1, key, expected, value! };
        await ExecuteAsync(command, true, result =>
        {
            var count = Integer(result);
            if (count == 0) throw new StorageLifecycleConflictException("Upstash observed state changed or disappeared.");
            if (count != 1) throw new InvalidDataException("Upstash conditional acknowledgement is invalid.");
            return true;
        }, token).ConfigureAwait(false);
    }
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        key = Key(key); await AdmitAsync(cancellationToken).ConfigureAwait(false);
        await ExecuteAsync(new object[] { "DEL", key }, true, result =>
        {
            var count = Integer(result);
            if (count is not (0 or 1)) throw new InvalidDataException("Upstash DEL acknowledgement is invalid.");
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }
    private static long Integer(JsonElement result)
        => result.ValueKind == JsonValueKind.Number && result.TryGetInt64(out var number)
            ? number : throw new InvalidDataException("Upstash omitted an integer acknowledgement.");
    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    { prefix = Key(prefix); await AdmitAsync(cancellationToken).ConfigureAwait(false); return await ScanAsync(prefix, cancellationToken).ConfigureAwait(false); }
    private async Task<IReadOnlyList<string>> ScanAsync(string prefix, CancellationToken token)
    {
        var cursor = "0"; var seen = new HashSet<string>(StringComparer.Ordinal); var keys = new HashSet<string>(StringComparer.Ordinal);
        do
        {
            if (!seen.Add(cursor)) throw new InvalidDataException("Upstash SCAN cursor repeated before completion.");
            cursor = await ExecuteAsync(new object[] { "SCAN", cursor, "MATCH", RedisStateComparison.PrefixPattern(prefix), "COUNT", 256 }, false, result =>
            {
                if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() != 2 ||
                    result[0].ValueKind != JsonValueKind.String || result[1].ValueKind != JsonValueKind.Array)
                    throw new InvalidDataException("Upstash SCAN page is malformed.");
                var next = result[0].GetString()!;
                if (!ulong.TryParse(next, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed.ToString(CultureInfo.InvariantCulture) != next)
                    throw new InvalidDataException("Upstash SCAN cursor is malformed.");
                foreach (var item in result[1].EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String || !item.GetString()!.StartsWith(prefix, StringComparison.Ordinal))
                        throw new InvalidDataException("Upstash SCAN returned an out-of-scope key.");
                    keys.Add(item.GetString()!); // Native SCAN can repeat keys; not a record snapshot.
                }
                return next;
            }, token).ConfigureAwait(false);
        } while (cursor != "0");
        return keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }
    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await AdmitAsync(cancellationToken).ConfigureAwait(false);
        await ExecuteAsync(new object[] { "PING" }, false, result =>
        {
            if (result.ValueKind != JsonValueKind.String || result.GetString() != "PONG") throw new InvalidDataException("Upstash PING acknowledgement is invalid.");
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class UpstashCommandRejectedException(string message) : Exception(message);
internal sealed class UpstashMutationReconciliationException(string key, string operation, Exception cause)
    : Exception("Upstash mutation outcome is uncertain; reconcile the original key before replay.", cause), INativeMutationReconciliationRequired
{
    public string Key { get; } = key;
    public string Operation { get; } = operation;
    public string OriginalTransactionId => string.Empty;
}
