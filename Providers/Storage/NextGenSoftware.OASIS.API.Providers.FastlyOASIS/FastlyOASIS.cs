using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
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

    internal sealed class FastlyKvBackend : IKeyValueBackend
    {
        private readonly HttpClient _http;
        private readonly string _storeId;

        public FastlyKvBackend(string apiToken, string storeId, HttpMessageHandler handler)
        {
            if (string.IsNullOrWhiteSpace(apiToken)) throw new ArgumentException("A Fastly API token is required.", nameof(apiToken));
            if (string.IsNullOrWhiteSpace(storeId)) throw new ArgumentException("A Fastly KV store id is required.", nameof(storeId));
            _storeId = storeId;
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri("https://api.fastly.com/");
            _http.DefaultRequestHeaders.Add("Fastly-Key", apiToken);
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        private string ItemPath(string key) => $"resources/stores/kv/{Uri.EscapeDataString(_storeId)}/keys/{Uri.EscapeDataString(key)}";

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync(ItemPath(key), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            await EnsureSuccessAsync(response, "get", key);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(value));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            using var response = await _http.PutAsync(ItemPath(key), content, cancellationToken);
            await EnsureSuccessAsync(response, "put", key);
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            using var response = await _http.DeleteAsync(ItemPath(key), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return;
            await EnsureSuccessAsync(response, "delete", key);
        }

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
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
            using var response = await _http.GetAsync($"resources/stores/kv/{Uri.EscapeDataString(_storeId)}", cancellationToken);
            await EnsureSuccessAsync(response, "open store", _storeId);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, string key)
        {
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Fastly KV {operation} '{key}' failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
        }
    }
}
