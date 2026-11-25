using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OrbitDBOASIS;

/// <summary>Key-value operations against the OrbitDB storage service (see StorageService/storage-service.mjs).</summary>
internal sealed class OrbitDbServiceKeyValueBackend : IKeyValueBackend
{
    private readonly HttpClient _http;

    public OrbitDbServiceKeyValueBackend(string serviceUrl, string serviceToken)
    {
        if (!Uri.TryCreate(serviceUrl?.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            throw new ArgumentException("An OrbitDB storage service URL (http or https) is required.", nameof(serviceUrl));
        if (string.IsNullOrWhiteSpace(serviceToken))
            throw new ArgumentException("The OrbitDB storage service token is required.", nameof(serviceToken));
        _http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(60) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
    }

    private static string KeyPath(string key) => "kv?key=" + Uri.EscapeDataString(key);

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("health", cancellationToken);
        await EnsureSuccessAsync(response, "report its health");
        using var health = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!health.RootElement.TryGetProperty("address", out var address) || address.GetString()?.StartsWith("/orbitdb/", StringComparison.Ordinal) != true)
            throw new InvalidDataException("The OrbitDB storage service did not report an OrbitDB database address.");
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var response = await _http.GetAsync(KeyPath(key), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(response, $"read '{key}'");
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        using var response = await _http.PutAsync(KeyPath(key), new StringContent(value, Encoding.UTF8, "text/plain"), cancellationToken);
        await EnsureSuccessAsync(response, $"write '{key}'");
        await RequireEntryHashAsync(response, key, allowNone: false, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var response = await _http.DeleteAsync(KeyPath(key), cancellationToken);
        await EnsureSuccessAsync(response, $"delete '{key}'");
        await RequireEntryHashAsync(response, key, allowNone: true, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        using var response = await _http.GetAsync("keys?prefix=" + Uri.EscapeDataString(prefix), cancellationToken);
        await EnsureSuccessAsync(response, "list keys");
        return JsonSerializer.Deserialize<List<string>>(await response.Content.ReadAsStringAsync(cancellationToken))
            ?? throw new InvalidDataException("The OrbitDB storage service returned no key list.");
    }

    // A null hash is only valid for deleting a key that did not exist (no oplog entry is written).
    private static async Task RequireEntryHashAsync(HttpResponseMessage response, string key, bool allowNone, CancellationToken cancellationToken)
    {
        using var ack = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!ack.RootElement.TryGetProperty("hash", out var hash))
            throw new IOException($"The OrbitDB storage service did not acknowledge '{key}'.");
        if (hash.ValueKind == JsonValueKind.Null && allowNone) return;
        if (hash.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(hash.GetString()))
            throw new IOException($"The OrbitDB storage service returned no oplog entry for '{key}'.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        throw new IOException($"The OrbitDB storage service failed to {operation}: {(int)response.StatusCode} {body[..Math.Min(body.Length, 300)]}");
    }
}
