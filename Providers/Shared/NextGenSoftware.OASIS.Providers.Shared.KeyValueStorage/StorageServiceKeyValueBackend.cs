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

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>
/// Key-value operations against a repository storage service: a co-located process that owns a backend which has no
/// network API of its own (OrbitDB, Scuttlebutt). Contract, served on loopback with a bearer token:
/// GET health → {"address":...}; GET kv?key=K → 200 value / 404; PUT kv?key=K → {"hash":entry};
/// DELETE kv?key=K → {"hash":entry|null} (null only when the key did not exist); GET keys?prefix=P → [keys].
/// </summary>
public sealed class StorageServiceKeyValueBackend : IKeyValueBackend
{
    private readonly HttpClient _http;
    private readonly string _service;
    private readonly string _addressPrefix;

    /// <param name="service">Name used in errors, e.g. "OrbitDB storage service".</param>
    /// <param name="addressPrefix">Required prefix of the address the service reports from /health.</param>
    public StorageServiceKeyValueBackend(string serviceUrl, string serviceToken, string service, string addressPrefix)
        : this(serviceUrl, serviceToken, service, addressPrefix, new HttpClientHandler())
    {
    }

    /// <param name="transport">The HTTP transport to reach the service through, e.g. a Tor SOCKS5 proxy handler.</param>
    public StorageServiceKeyValueBackend(string serviceUrl, string serviceToken, string service, string addressPrefix, HttpMessageHandler transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _service = string.IsNullOrWhiteSpace(service) ? throw new ArgumentException("A service name is required.", nameof(service)) : service;
        _addressPrefix = string.IsNullOrEmpty(addressPrefix) ? throw new ArgumentException("An address prefix is required.", nameof(addressPrefix)) : addressPrefix;
        if (!Uri.TryCreate(serviceUrl?.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            throw new ArgumentException($"A {service} URL (http or https) is required.", nameof(serviceUrl));
        if (string.IsNullOrWhiteSpace(serviceToken))
            throw new ArgumentException($"The {service} token is required.", nameof(serviceToken));
        _http = new HttpClient(transport) { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(60) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
    }

    private static string KeyPath(string key) => "kv?key=" + Uri.EscapeDataString(key);

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("health", cancellationToken);
        await EnsureSuccessAsync(response, "report its health");
        using var health = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!health.RootElement.TryGetProperty("address", out var address) || address.GetString()?.StartsWith(_addressPrefix, StringComparison.Ordinal) != true)
            throw new InvalidDataException($"The {_service} did not report a database address.");
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
            ?? throw new InvalidDataException($"The {_service} returned no key list.");
    }

    // A null hash is only valid for deleting a key that did not exist (no log entry is written).
    private async Task RequireEntryHashAsync(HttpResponseMessage response, string key, bool allowNone, CancellationToken cancellationToken)
    {
        using var ack = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!ack.RootElement.TryGetProperty("hash", out var hash))
            throw new IOException($"The {_service} did not acknowledge '{key}'.");
        if (hash.ValueKind == JsonValueKind.Null && allowNone) return;
        if (hash.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(hash.GetString()))
            throw new IOException($"The {_service} returned no log entry for '{key}'.");
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        throw new IOException($"The {_service} failed to {operation}: {(int)response.StatusCode} {body[..Math.Min(body.Length, 300)]}");
    }
}
