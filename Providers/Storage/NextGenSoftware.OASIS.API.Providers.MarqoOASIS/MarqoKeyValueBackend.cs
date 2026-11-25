using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MarqoOASIS;

/// <summary>
/// Key-value storage on one Marqo index (no embedding model; records are not vectorised). Each record is a document
/// {_id: sha256(key), k: key, v: value, p: every '/'-terminated prefix of the key, t: "oasis"}. Prefix listing is a
/// lexical search for the constant term filtered on p, paged to the end. Writes are acknowledged only when Marqo reports
/// the document item as written.
/// </summary>
internal sealed class MarqoKeyValueBackend : IKeyValueBackend, IDisposable
{
    private const int PageSize = 1000;
    private const int MaximumOffset = 10_000;
    private readonly HttpClient _http;
    private readonly string _index;

    public MarqoKeyValueBackend(string baseUrl, string index)
    {
        _http = new HttpClient { BaseAddress = new Uri((baseUrl ?? throw new ArgumentNullException(nameof(baseUrl))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
        _index = string.IsNullOrWhiteSpace(index) ? throw new ArgumentException("A Marqo index name is required.", nameof(index)) : index;
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        using var existing = await _http.GetAsync($"indexes/{_index}/stats", cancellationToken);
        if (existing.StatusCode == HttpStatusCode.NotFound)
        {
            var settings = new JsonObject
            {
                ["type"] = "unstructured",
                ["model"] = "no_model",
                ["modelProperties"] = new JsonObject { ["type"] = "no_model", ["dimensions"] = 2 }
            };
            using var created = await _http.PostAsync($"indexes/{_index}", Json(settings), cancellationToken);
            var body = await created.Content.ReadAsStringAsync(cancellationToken);
            if (!created.IsSuccessStatusCode && !body.Contains("index_already_exists", StringComparison.Ordinal))
                throw new HttpRequestException($"Marqo could not create index {_index} ({(int)created.StatusCode}): {body}");
        }
        else
        {
            existing.EnsureSuccessStatusCode();
        }
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var response = await _http.GetAsync($"indexes/{_index}/documents/{DocumentId(key)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Marqo get returned {(int)response.StatusCode}: {text}");
        using var doc = JsonDocument.Parse(text);
        if (!string.Equals(doc.RootElement.GetProperty("k").GetString(), key, StringComparison.Ordinal))
            throw new InvalidDataException("The Marqo document belongs to a different key (id collision).");
        return doc.RootElement.GetProperty("v").GetString();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        var prefixes = new JsonArray();
        for (var i = key.IndexOf('/'); i >= 0; i = key.IndexOf('/', i + 1)) prefixes.Add(key[..(i + 1)]);
        var request = new JsonObject
        {
            ["documents"] = new JsonArray(new JsonObject
            {
                ["_id"] = DocumentId(key), ["k"] = key, ["v"] = value, ["p"] = prefixes, ["t"] = "oasis"
            }),
            ["tensorFields"] = new JsonArray()
        };
        using var response = await _http.PostAsync($"indexes/{_index}/documents", Json(request), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Marqo write returned {(int)response.StatusCode}: {text}");
        using var receipt = JsonDocument.Parse(text);
        var items = receipt.RootElement.GetProperty("items");
        if (receipt.RootElement.GetProperty("errors").GetBoolean() || items.GetArrayLength() != 1 || items[0].GetProperty("status").GetInt32() / 100 != 2)
            throw new IOException($"Marqo did not acknowledge the write: {text}");
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var response = await _http.PostAsync($"indexes/{_index}/documents/delete-batch", Json(new JsonArray(DocumentId(key))), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Marqo delete returned {(int)response.StatusCode}: {text}");
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        prefix ??= string.Empty;
        if (prefix.Length > 0 && !prefix.EndsWith('/'))
            throw new ArgumentException("Marqo prefix listing supports '/'-terminated prefixes only.", nameof(prefix));
        var keys = new List<string>();
        for (var offset = 0; ; offset += PageSize)
        {
            if (offset >= MaximumOffset) throw new IOException($"Marqo prefix listing exceeds {MaximumOffset} keys.");
            var search = new JsonObject
            {
                ["q"] = "oasis", ["searchMethod"] = "LEXICAL", ["limit"] = PageSize, ["offset"] = offset,
                ["searchableAttributes"] = new JsonArray("t"), ["attributesToRetrieve"] = new JsonArray("k")
            };
            if (prefix.Length > 0) search["filter"] = $"p:({Escape(prefix)})";
            using var response = await _http.PostAsync($"indexes/{_index}/search", Json(search), cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Marqo search returned {(int)response.StatusCode}: {text}");
            using var doc = JsonDocument.Parse(text);
            var hits = doc.RootElement.GetProperty("hits").EnumerateArray().Select(h => h.GetProperty("k").GetString()!).ToArray();
            keys.AddRange(hits);
            if (hits.Length < PageSize) break;
        }
        return keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).Distinct().OrderBy(k => k, StringComparer.Ordinal).ToArray();
    }

    internal static string DocumentId(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();

    /// <summary>Marqo filter DSL: every non-alphanumeric character in a value is backslash-escaped.</summary>
    internal static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length * 2);
        foreach (var c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c)) builder.Append('\\');
            builder.Append(c);
        }
        return builder.ToString();
    }

    private static StringContent Json(JsonNode node) => new(node.ToJsonString(), Encoding.UTF8, "application/json");

    public void Dispose() => _http.Dispose();
}
