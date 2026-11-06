using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests")]

namespace NextGenSoftware.OASIS.API.Providers.FastlyOASIS
{
    /// <summary>
    /// Stores OASIS avatars and holons in a Fastly KV Store through the Fastly API
    /// (https://api.fastly.com/resources/stores/kv/{store_id}/keys/{key}), so Compute@Edge services can read them at the edge.
    /// </summary>
    public class FastlyOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        public FastlyOASIS(string apiToken, string storeId) : this(new FastlyKvBackend(apiToken, storeId, null))
        {
        }

        internal FastlyOASIS(FastlyKvBackend backend) : base(backend)
        {
            ProviderName = "FastlyOASIS";
            ProviderDescription = "Fastly KV Store provider: OASIS avatars and holons served from Fastly's edge.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FastlyOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }
    }

    internal sealed class FastlyKvBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _storeId;
        private readonly bool _queryOnly;

        public FastlyKvBackend(string apiToken, string storeId, HttpMessageHandler handler, bool queryOnly = false)
        {
            if (string.IsNullOrWhiteSpace(apiToken)) throw new ArgumentException("A Fastly API token is required.", nameof(apiToken));
            if (string.IsNullOrWhiteSpace(storeId)) throw new ArgumentException("A Fastly KV store id is required.", nameof(storeId));
            _storeId = storeId;
            _queryOnly = queryOnly;
            _http = CreateClient(handler, queryOnly);
            _http.BaseAddress = new Uri("https://api.fastly.com/");
            _http.DefaultRequestHeaders.Add("Fastly-Key", apiToken);
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        private static HttpClient CreateClient(HttpMessageHandler handler, bool queryOnly)
        {
            if (handler != null) return new HttpClient(handler, disposeHandler: false);
            if (!queryOnly) return new HttpClient();
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(transport);
        }

        private string ItemPath(string key) => $"resources/stores/kv/{Uri.EscapeDataString(_storeId)}/keys/{Uri.EscapeDataString(key)}";

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            const string holonPrefix = "oasis/holon/";
            if (_queryOnly && (!key.StartsWith(holonPrefix, StringComparison.Ordinal) ||
                !Guid.TryParseExact(key.AsSpan(holonPrefix.Length), "N", out var id) ||
                id == Guid.Empty || key != holonPrefix + id.ToString("N")))
                throw new InvalidOperationException("Fastly CQL reads only canonical holon keys.");
            var completion = _queryOnly ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead;
            using var response = await _http.GetAsync(ItemPath(key), completion, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (_queryOnly)
            {
                response.EnsureSuccessStatusCode();
                const int maximumBytes = 1024 * 1024;
                if (response.Content.Headers.ContentLength > maximumBytes)
                    throw new InvalidDataException("Fastly KV response exceeds its byte budget.");
                await response.Content.LoadIntoBufferAsync(maximumBytes, cancellationToken);
                return new UTF8Encoding(false, true).GetString(await response.Content.ReadAsByteArrayAsync(cancellationToken));
            }
            await EnsureSuccessAsync(response, "get", key);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            RequireStorageMode();
            using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(value));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            using var response = await _http.PutAsync(ItemPath(key), content, cancellationToken);
            await EnsureSuccessAsync(response, "put", key);
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            RequireStorageMode();
            using var response = await _http.DeleteAsync(ItemPath(key), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return;
            await EnsureSuccessAsync(response, "delete", key);
        }

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            RequireStorageMode();
            var keys = new List<string>();
            string cursor = null;
            do
            {
                var url = $"resources/stores/kv/{Uri.EscapeDataString(_storeId)}/keys?limit=1000&prefix={Uri.EscapeDataString(prefix)}"
                          + (cursor == null ? string.Empty : $"&cursor={Uri.EscapeDataString(cursor)}");
                using var response = await _http.GetAsync(url, cancellationToken);
                await EnsureSuccessAsync(response, "list", prefix);
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                if (doc.RootElement.TryGetProperty("data", out var data))
                    foreach (var k in data.EnumerateArray()) keys.Add(k.GetString());
                cursor = doc.RootElement.TryGetProperty("meta", out var meta) && meta.TryGetProperty("next_cursor", out var next) && next.ValueKind == JsonValueKind.String
                    ? next.GetString() : null;
            } while (!string.IsNullOrEmpty(cursor));
            return keys;
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            RequireStorageMode();
            using var response = await _http.GetAsync($"resources/stores/kv/{Uri.EscapeDataString(_storeId)}", cancellationToken);
            await EnsureSuccessAsync(response, "open store", _storeId);
        }

        private void RequireStorageMode()
        {
            if (_queryOnly) throw new NotSupportedException("The enrolled Fastly profile supports only exact reads.");
        }

        public void Dispose() => _http.Dispose();

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, string key)
        {
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Fastly KV {operation} '{key}' failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
        }
    }
}
