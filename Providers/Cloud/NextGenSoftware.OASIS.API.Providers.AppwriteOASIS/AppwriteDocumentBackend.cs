#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AppwriteOASIS
{
    /// <summary>
    /// One Appwrite document per OASIS key. Appwrite document IDs are limited to 36 characters, so the ID is a
    /// SHA-256 digest of the exact key and the key itself is stored (and verified on every read) in the "key" attribute.
    /// </summary>
    internal sealed class AppwriteDocumentBackend : IKeyValueBackend
    {
        internal const int MaxKeyLength = 255;
        private const int PageSize = 100;
        private readonly HttpClient _http;
        private readonly string _project;
        private readonly string _apiKey;
        private readonly string _collectionUrl;

        public AppwriteDocumentBackend(string endpoint, string projectId, string apiKey, string databaseId, string collectionId, HttpClient http)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException("Appwrite requires an absolute http(s) API endpoint.", nameof(endpoint));
            ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
            ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _project = projectId;
            _apiKey = apiKey;
            _collectionUrl = $"{endpoint.TrimEnd('/')}/databases/{Uri.EscapeDataString(databaseId)}/collections/{Uri.EscapeDataString(collectionId)}";
        }

        internal static string DocumentId(string key)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            if (key.Length > MaxKeyLength) throw new ArgumentException($"Appwrite keys are limited to {MaxKeyLength} characters.", nameof(key));
            return "k" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..35].ToLowerInvariant();
        }

        private HttpRequestMessage Request(HttpMethod method, string url, object? body = null)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Appwrite-Project", _project);
            request.Headers.Add("X-Appwrite-Key", _apiKey);
            if (body != null) request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            return request;
        }

        private static async Task<HttpRequestException> RejectedAsync(HttpResponseMessage response, string operation, CancellationToken ct)
        {
            string type = "";
            try
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                if (json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String)
                    type = t.GetString()!;
            }
            catch (JsonException) { type = "non-JSON error body"; }
            return new HttpRequestException($"Appwrite {operation} failed with HTTP {(int)response.StatusCode} ({type}).", null, response.StatusCode);
        }

        private static async Task<bool> IsDocumentNotFoundAsync(HttpResponseMessage response, CancellationToken ct)
        {
            if (response.StatusCode != HttpStatusCode.NotFound) return false;
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            // Only an explicit missing document is absence; a missing database or collection is a configuration error.
            return json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String && t.GetString() == "document_not_found";
        }

        private static string RequireString(JsonElement document, string property)
        {
            if (document.ValueKind != JsonValueKind.Object || !document.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
                throw new InvalidDataException($"Appwrite document is missing its '{property}' string.");
            return value.GetString()!;
        }

        private static void RequireIdentity(JsonElement document, string id, string key)
        {
            if (RequireString(document, "$id") != id || RequireString(document, "key") != key)
                throw new InvalidDataException("Appwrite document identity does not match the requested key.");
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var id = DocumentId(key);
            using var request = Request(HttpMethod.Get, $"{_collectionUrl}/documents/{id}");
            using var response = await _http.SendAsync(request, cancellationToken);
            if (await IsDocumentNotFoundAsync(response, cancellationToken)) return null!;
            if (response.StatusCode != HttpStatusCode.OK) throw await RejectedAsync(response, "read", cancellationToken);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            RequireIdentity(json.RootElement, id, key);
            return RequireString(json.RootElement, "value");
        }

        public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(value);
            var id = DocumentId(key);
            // PUT is Appwrite's native upsert; explicit empty document permissions leave access to the collection rules.
            using var request = Request(HttpMethod.Put, $"{_collectionUrl}/documents/{id}",
                new { data = new { key, value }, permissions = Array.Empty<string>() });
            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
                throw await RejectedAsync(response, "write", cancellationToken);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            RequireIdentity(json.RootElement, id, key);
            if (RequireString(json.RootElement, "value") != value)
                throw new InvalidDataException("Appwrite acknowledged a different value than was written.");
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            using var request = Request(HttpMethod.Delete, $"{_collectionUrl}/documents/{DocumentId(key)}");
            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NoContent || await IsDocumentNotFoundAsync(response, cancellationToken)) return;
            throw await RejectedAsync(response, "delete", cancellationToken);
        }

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            var keys = new List<string>();
            string? cursor = null;
            while (true)
            {
                var queries = new List<object>
                {
                    new { method = "startsWith", attribute = "key", values = new[] { prefix } },
                    new { method = "select", values = new[] { "key", "$id" } },
                    new { method = "orderAsc", attribute = "$id" },
                    new { method = "limit", values = new[] { PageSize } }
                };
                if (cursor != null) queries.Add(new { method = "cursorAfter", values = new[] { cursor } });
                var query = string.Join("&", queries.Select(q => "queries[]=" + Uri.EscapeDataString(JsonSerializer.Serialize(q))));
                using var request = Request(HttpMethod.Get, $"{_collectionUrl}/documents?{query}");
                using var response = await _http.SendAsync(request, cancellationToken);
                if (response.StatusCode != HttpStatusCode.OK) throw await RejectedAsync(response, "list", cancellationToken);
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                if (json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty("documents", out var documents) || documents.ValueKind != JsonValueKind.Array)
                    throw new InvalidDataException("Appwrite list response is missing its documents array.");
                var count = 0;
                foreach (var document in documents.EnumerateArray())
                {
                    count++;
                    var key = RequireString(document, "key");
                    cursor = RequireString(document, "$id");
                    if (cursor != DocumentId(key)) throw new InvalidDataException("Appwrite listed a document whose ID does not match its key.");
                    // Appwrite prefix matching follows the collation (case-insensitive); keys are case-sensitive.
                    if (key.StartsWith(prefix, StringComparison.Ordinal)) keys.Add(key);
                }
                if (count > PageSize) throw new InvalidDataException("Appwrite returned more documents than the requested page size.");
                if (count < PageSize) return keys;
            }
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            using var request = Request(HttpMethod.Get, $"{_collectionUrl}/attributes");
            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK) throw await RejectedAsync(response, "collection verification", cancellationToken);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!json.RootElement.TryGetProperty("attributes", out var attributes) || attributes.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Appwrite attribute listing is malformed.");
            var available = attributes.EnumerateArray()
                .Where(a => RequireString(a, "status") == "available" && RequireString(a, "type") == "string")
                .Select(a => RequireString(a, "key")).ToHashSet(StringComparer.Ordinal);
            if (!available.Contains("key") || !available.Contains("value"))
                throw new InvalidOperationException("The Appwrite collection requires available string attributes 'key' and 'value'.");
        }
    }
}
