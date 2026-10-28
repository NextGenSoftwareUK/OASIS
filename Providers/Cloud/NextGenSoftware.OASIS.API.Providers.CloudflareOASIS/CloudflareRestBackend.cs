using System;
using System.Collections.Generic;
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

namespace NextGenSoftware.OASIS.API.Providers.CloudflareOASIS;

/// <summary>Workers KV's actual unconditional, eventually consistent REST protocol.
/// No synthetic ETag, compare-and-swap, read-your-writes cache or retry loop.</summary>
internal sealed class CloudflareRestBackend : IKeyValueBackend
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _token;
    private readonly Lazy<Task> _admission;

    public CloudflareRestBackend(string account, string space, string token, HttpClient http)
    {
        Validate(account, space, token); ArgumentNullException.ThrowIfNull(http);
        _http = http; _token = token;
        _baseUrl = $"https://api.cloudflare.com/client/v4/accounts/{account}/storage/kv/namespaces/{space}";
        _admission = new Lazy<Task>(VerifyLegacyLayoutAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }
    internal static void Validate(string account, string space, string token)
    {
        foreach (var segment in new[] { account, space })
            if (string.IsNullOrWhiteSpace(segment) || segment.Length > 32 || segment.Any(character => !char.IsAsciiLetterOrDigit(character)))
                throw new ArgumentException("Cloudflare account and namespace IDs must be nonempty alphanumeric path segments up to32 characters.");
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        _ = new AuthenticationHeaderValue("Bearer", token);
    }
    private static string Key(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!key.StartsWith("oasis/private/v1/", StringComparison.Ordinal) || Utf8.GetByteCount(key) > 512 || key.Any(char.IsWhiteSpace))
            throw new ArgumentException("Cloudflare key must be in the private namespace and fit its512-byte native limit.", nameof(key));
        return key;
    }
    private HttpRequestMessage Request(HttpMethod method, string relative)
    {
        var request = new HttpRequestMessage(method, _baseUrl + relative);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return request;
    }
    private async Task<JsonDocument> EnvelopeAsync(HttpRequestMessage request, CancellationToken token)
    {
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            if ((int)response.StatusCode is >= 400 and < 500)
                throw new CloudflareCommandRejectedException("Cloudflare rejected the command (HTTP " + (int)response.StatusCode + ").");
            throw new HttpRequestException("Cloudflare did not return an authoritative HTTP200 acknowledgement.", null, response.StatusCode);
        }
        var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
        try
        {
            var root = document.RootElement; RequireObject(root);
            if (!root.TryGetProperty("success", out var success) || success.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new InvalidDataException("Cloudflare omitted its explicit success flag.");
            if (success.ValueKind == JsonValueKind.False) throw new CloudflareCommandRejectedException("Cloudflare rejected the native command.");
            if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Array || errors.GetArrayLength() != 0)
                throw new InvalidDataException("Cloudflare success response contains missing or contradictory errors.");
            return document;
        }
        catch { document.Dispose(); throw; }
    }
    private static void RequireObject(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Cloudflare response requires an object.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            if (!names.Add(property.Name)) throw new InvalidDataException("Cloudflare response contains duplicate fields.");
    }
    private async Task AdmitAsync(CancellationToken token)
    { token.ThrowIfCancellationRequested(); await _admission.Value.WaitAsync(token).ConfigureAwait(false); }
    private async Task VerifyLegacyLayoutAsync()
    {
        foreach (var prefix in new[] { "avatar:", "avatar_detail:", "holon:", "index:" })
            if ((await ListNativeAsync(prefix, CancellationToken.None).ConfigureAwait(false)).Count != 0)
                throw new InvalidOperationException("Legacy Cloudflare records require reviewed migration before private-layout access; no data was changed.");
    }
    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        key = Key(key); await AdmitAsync(cancellationToken).ConfigureAwait(false);
        using var request = Request(HttpMethod.Get, "/values/" + Uri.EscapeDataString(key));
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        // Native value404 after authenticated namespace admission is absence; other statuses remain errors.
        if (response.StatusCode == HttpStatusCode.NotFound) return null!;
        if (response.StatusCode != HttpStatusCode.OK)
        {
            if ((int)response.StatusCode is >= 400 and < 500)
                throw new CloudflareCommandRejectedException("Cloudflare value read failed (HTTP " + (int)response.StatusCode + ").");
            throw new HttpRequestException("Cloudflare value read failed.", null, response.StatusCode);
        }
        return Utf8.GetString(await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
    }
    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        key = Key(key); ArgumentNullException.ThrowIfNull(value);
        var bytes = Utf8.GetBytes(value);
        if (bytes.Length > 25 * 1024 * 1024) throw new ArgumentException("Cloudflare value exceeds25MiB.", nameof(value));
        await AdmitAsync(cancellationToken).ConfigureAwait(false);
        using var request = Request(HttpMethod.Put, "/values/" + Uri.EscapeDataString(key));
        request.Content = new ByteArrayContent(bytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        await MutateAsync(request, key, cancellationToken).ConfigureAwait(false);
    }
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        key = Key(key); await AdmitAsync(cancellationToken).ConfigureAwait(false);
        using var request = Request(HttpMethod.Delete, "/values/" + Uri.EscapeDataString(key));
        await MutateAsync(request, key, cancellationToken).ConfigureAwait(false);
    }
    private async Task MutateAsync(HttpRequestMessage request, string key, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try { using var acknowledged = await EnvelopeAsync(request, token).ConfigureAwait(false); }
        catch (CloudflareCommandRejectedException) { throw; }
        catch (Exception cause) { throw new CloudflareMutationReconciliationException(key, request.Method.Method, cause); }
    }
    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    { prefix = Key(prefix); await AdmitAsync(cancellationToken).ConfigureAwait(false); return await ListNativeAsync(prefix, cancellationToken).ConfigureAwait(false); }
    private async Task<IReadOnlyList<string>> ListNativeAsync(string prefix, CancellationToken token)
    {
        string? cursor = null;
        var cursors = new HashSet<string>(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        do
        {
            using var request = Request(HttpMethod.Get, "/keys?limit=1000&prefix=" + Uri.EscapeDataString(prefix)
                + (cursor == null ? "" : "&cursor=" + Uri.EscapeDataString(cursor)));
            using var document = await EnvelopeAsync(request, token).ConfigureAwait(false);
            var root = document.RootElement;
            if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Cloudflare key listing omitted its result array.");
            foreach (var item in result.EnumerateArray())
            {
                RequireObject(item);
                if (!item.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String || !name.GetString()!.StartsWith(prefix, StringComparison.Ordinal))
                    throw new InvalidDataException("Cloudflare key listing contains an out-of-scope or malformed name.");
                keys.Add(name.GetString()!);
            }
            cursor = null;
            if (root.TryGetProperty("result_info", out var info))
            {
                RequireObject(info);
                if (info.TryGetProperty("count", out var count) && (count.ValueKind != JsonValueKind.Number || !count.TryGetInt32(out var number) || number != result.GetArrayLength()))
                    throw new InvalidDataException("Cloudflare page count contradicts its keys.");
                if (info.TryGetProperty("cursor", out var next))
                {
                    if (next.ValueKind != JsonValueKind.String) throw new InvalidDataException("Cloudflare cursor must be an opaque string.");
                    if (next.GetString()!.Length != 0) cursor = next.GetString();
                }
            }
            // REST completion is absence of a continuation cursor, NOT an empty page.
            if (cursor != null && !cursors.Add(cursor)) throw new InvalidDataException("Cloudflare continuation cursor repeated.");
        } while (cursor != null);
        return keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }
    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await AdmitAsync(cancellationToken).ConfigureAwait(false);
        _ = await ListNativeAsync("oasis/private/v1/", cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class CloudflareCommandRejectedException(string message) : Exception(message);
internal sealed class CloudflareMutationReconciliationException(string key, string operation, Exception cause)
    : Exception("Cloudflare mutation outcome is uncertain; reconcile the original key before replay.", cause), INativeMutationReconciliationRequired
{
    public string Key { get; } = key;
    public string Operation { get; } = operation;
    public string OriginalTransactionId => string.Empty;
}
