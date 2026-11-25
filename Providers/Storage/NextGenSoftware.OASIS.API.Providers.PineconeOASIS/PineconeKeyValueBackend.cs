using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PineconeOASIS;

/// <summary>
/// Key-value storage on one Pinecone dense index of dimension 1, inside the <c>oasis</c> namespace.
/// Put upserts {id=key, values=[1], metadata={v:value}} and requires upsertedCount 1; Get fetches by id;
/// Delete deletes by id; ListKeys follows the serverless <c>/vectors/list</c> id-prefix listing to the end.
/// </summary>
public sealed class PineconeKeyValueBackend : IKeyValueBackend
{
    public const string Namespace = "oasis";
    public const string ApiVersion = "2026-07";
    /// <summary>Pinecone vector ids are limited to 512 bytes.</summary>
    public const int MaxIdBytes = 512;
    /// <summary>Pinecone metadata is limited to 40 KB per vector.</summary>
    public const int MaxMetadataBytes = 40960;
    private const int MaxResponseBytes = 4 * 1024 * 1024;
    private const int MaxStatsBytes = 262144;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    private readonly HttpClient _http;
    private readonly string _host;
    private readonly string _apiKey;

    public PineconeKeyValueBackend(HttpClient http, string indexHost, string apiKey)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        if (!Uri.TryCreate(indexHost?.TrimEnd('/'), UriKind.Absolute, out var host) || host.Scheme is not ("http" or "https")
            || host.UserInfo != "" || host.Query != "" || host.Fragment != "")
            throw new ArgumentException("An explicit HTTP(S) Pinecone index host is required.", nameof(indexHost));
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("A Pinecone API key is required.", nameof(apiKey));
        _host = host.AbsoluteUri.TrimEnd('/');
        _apiKey = apiKey;
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        using var document = await SendAsync(HttpMethod.Get,
            $"/vectors/fetch?namespace={Namespace}&ids={Uri.EscapeDataString(key)}", null, MaxResponseBytes, cancellationToken);
        var root = document.RootElement;
        RequireNamespace(root);
        var vectors = root.GetProperty("vectors");
        if (vectors.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Pinecone fetch returned no vector map.");
        JsonElement? record = null;
        foreach (var entry in vectors.EnumerateObject())
        {
            if (entry.Name != key || record != null) throw new InvalidDataException("Pinecone fetch returned an unrequested vector.");
            record = entry.Value;
        }
        if (record == null) return null;
        var row = record.Value;
        if (row.GetProperty("id").GetString() != key) throw new InvalidDataException("Pinecone vector identity differs from its key.");
        var values = row.GetProperty("values");
        if (values.ValueKind != JsonValueKind.Array || values.GetArrayLength() != 1 || !values[0].TryGetDouble(out var value) || value != 1)
            throw new InvalidDataException("Pinecone vector differs from the storage contract.");
        var stored = row.GetProperty("metadata").GetProperty("v");
        if (stored.ValueKind != JsonValueKind.String) throw new InvalidDataException("Pinecone stored value must be a string.");
        return stored.GetString();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);
        var metadata = new Dictionary<string, string> { ["v"] = value };
        if (JsonSerializer.SerializeToUtf8Bytes(metadata).Length > MaxMetadataBytes)
            throw new InvalidDataException($"The OASIS record exceeds Pinecone's {MaxMetadataBytes}-byte metadata limit per vector.");
        using var document = await SendAsync(HttpMethod.Post, "/vectors/upsert",
            new { vectors = new[] { new { id = key, values = new[] { 1.0f }, metadata } }, @namespace = Namespace },
            MaxResponseBytes, cancellationToken);
        if (!document.RootElement.TryGetProperty("upsertedCount", out var count) || count.ValueKind != JsonValueKind.Number
            || !count.TryGetInt32(out var upserted) || upserted != 1)
            throw new InvalidDataException("Pinecone must acknowledge exactly one upserted vector.");
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        // Pinecone acknowledges deleting an absent id with 200, which is the backend contract.
        using var _ = await SendAsync(HttpMethod.Post, "/vectors/delete",
            new { ids = new[] { key }, @namespace = Namespace }, MaxResponseBytes, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        var keys = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        string token = null;
        do
        {
            var path = $"/vectors/list?namespace={Namespace}&limit=100";
            if (prefix.Length != 0) path += "&prefix=" + Uri.EscapeDataString(prefix);
            if (token != null) path += "&paginationToken=" + Uri.EscapeDataString(token);
            using var document = await SendAsync(HttpMethod.Get, path, null, MaxResponseBytes, cancellationToken);
            var root = document.RootElement;
            RequireNamespace(root);
            var vectors = root.GetProperty("vectors");
            if (vectors.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Pinecone list returned no vector array.");
            foreach (var vector in vectors.EnumerateArray())
            {
                var id = vector.GetProperty("id").GetString();
                if (id == null || !id.StartsWith(prefix, StringComparison.Ordinal))
                    throw new InvalidDataException("Pinecone list returned an id outside the requested prefix.");
                if (seen.Add(id)) keys.Add(id);
            }
            token = null;
            if (root.TryGetProperty("pagination", out var pagination) && pagination.ValueKind == JsonValueKind.Object
                && pagination.TryGetProperty("next", out var next) && next.ValueKind == JsonValueKind.String)
            {
                token = next.GetString();
                if (string.IsNullOrEmpty(token) || !tokens.Add(token))
                    throw new InvalidDataException("Pinecone list pagination did not advance.");
            }
        } while (token != null);
        return keys;
    }

    /// <summary>Authenticated statistics round trip proving the index is one-dimensional and dense. Never creates an index.</summary>
    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        using var document = await SendAsync(HttpMethod.Post, "/describe_index_stats", new { }, MaxStatsBytes, cancellationToken);
        PineconeIndexValidation.Validate(document.RootElement);
    }

    private static void ValidateKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length == 0) throw new ArgumentException("A Pinecone vector id must not be empty.", nameof(key));
        foreach (var c in key)
            if (c < 0x21 || c > 0x7e) throw new ArgumentException("Pinecone vector ids must be printable ASCII.", nameof(key));
        if (key.Length > MaxIdBytes)
            throw new ArgumentException($"The OASIS key exceeds Pinecone's {MaxIdBytes}-byte vector id limit.", nameof(key));
    }

    private static void RequireNamespace(JsonElement root)
    {
        if (!root.TryGetProperty("namespace", out var ns) || ns.GetString() != Namespace)
            throw new InvalidDataException("Pinecone answered for a different namespace.");
    }

    private async Task<JsonDocument> SendAsync(HttpMethod method, string path, object body, int maxBytes,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(RequestTimeout);
        using var request = new HttpRequestMessage(method, _host + path);
        request.Headers.Add("Api-Key", _apiKey);
        request.Headers.Add("X-Pinecone-API-Version", ApiVersion);
        request.Headers.Accept.ParseAdd("application/json");
        if (body != null) request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException($"Pinecone {path.Split('?')[0]} returned {(int)response.StatusCode} {response.ReasonPhrase}.");
        using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(chunk, deadline.Token)) != 0)
        {
            if (buffer.Length + count > maxBytes) throw new InvalidDataException($"Pinecone response exceeds {maxBytes} bytes.");
            buffer.Write(chunk, 0, count);
        }
        // Delete answers with an empty JSON body; every other endpoint returns an object validated by its caller.
        return JsonDocument.Parse(buffer.Length == 0 ? "{}"u8.ToArray() : buffer.ToArray());
    }
}
