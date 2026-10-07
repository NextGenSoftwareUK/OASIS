using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests")]

namespace NextGenSoftware.OASIS.API.Providers.NetlifyBlobsOASIS
{
    /// <summary>
    /// Stores OASIS avatars and holons in a Netlify Blobs site store via the Netlify API, using the same protocol as
    /// the official @netlify/blobs client: reads and writes obtain a signed URL first, deletes and listings are direct.
    /// </summary>
    public class NetlifyBlobsOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        public NetlifyBlobsOASIS(string siteId, string accessToken, string storeName = "oasis", string apiUrl = "https://api.netlify.com")
            : this(new NetlifyBlobsBackend(siteId, accessToken, storeName, apiUrl, null))
        {
        }

        internal NetlifyBlobsOASIS(NetlifyBlobsBackend backend) : base(backend, keyPrefix: string.Empty)
        {
            ProviderName = "NetlifyBlobsOASIS";
            ProviderDescription = "Netlify Blobs provider: OASIS avatars and holons in a Netlify site blob store.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.NetlifyBlobsOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }
    }

    internal sealed class NetlifyBlobsBackend : IKeyValueBackend
    {
        private const string SignedUrlAccept = "application/json;type=signed-url";
        private readonly HttpClient _api;
        private readonly HttpClient _signed;
        private readonly string _storePath;

        public NetlifyBlobsBackend(string siteId, string accessToken, string storeName, string apiUrl, HttpMessageHandler handler)
        {
            if (string.IsNullOrWhiteSpace(siteId)) throw new ArgumentException("A Netlify site id is required.", nameof(siteId));
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("A Netlify access token is required.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(storeName)) throw new ArgumentException("A blob store name is required.", nameof(storeName));

            _storePath = $"api/v1/blobs/{Uri.EscapeDataString(siteId)}/{Uri.EscapeDataString("site:" + storeName)}";
            _api = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _api.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
            _api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            // Signed URLs carry their own authorisation and must not receive the API token.
            _signed = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        }

        private string BlobPath(string key) => $"{_storePath}/{string.Join("/", key.Split('/').Select(Uri.EscapeDataString))}";

        private async Task<string> GetSignedUrlAsync(HttpMethod method, string key, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(method, BlobPath(key));
            request.Headers.Accept.ParseAdd(SignedUrlAccept);
            using var response = await _api.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            await EnsureSuccessAsync(response, $"sign {method}", key);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("url").GetString();
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var url = await GetSignedUrlAsync(HttpMethod.Get, key, cancellationToken);
            if (url == null) return null;
            using var response = await _signed.GetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            await EnsureSuccessAsync(response, "get", key);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            var url = await GetSignedUrlAsync(HttpMethod.Put, key, cancellationToken)
                      ?? throw new HttpRequestException($"Netlify Blobs did not return an upload URL for '{key}'.");
            using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(value));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var response = await _signed.PutAsync(url, content, cancellationToken);
            await EnsureSuccessAsync(response, "put", key);
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            using var response = await _api.DeleteAsync(BlobPath(key), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return;
            await EnsureSuccessAsync(response, "delete", key);
        }

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var keys = new List<string>();
            string cursor = null;
            do
            {
                var url = $"{_storePath}?prefix={Uri.EscapeDataString(prefix)}" + (cursor == null ? string.Empty : $"&cursor={Uri.EscapeDataString(cursor)}");
                using var response = await _api.GetAsync(url, cancellationToken);
                await EnsureSuccessAsync(response, "list", prefix);
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                if (doc.RootElement.TryGetProperty("blobs", out var blobs))
                    foreach (var blob in blobs.EnumerateArray()) keys.Add(blob.GetProperty("key").GetString());
                cursor = doc.RootElement.TryGetProperty("next_cursor", out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;
            } while (!string.IsNullOrEmpty(cursor));
            return keys;
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _api.GetAsync($"{_storePath}?prefix={Uri.EscapeDataString("__oasis_verify__")}", cancellationToken);
            await EnsureSuccessAsync(response, "open store", _storePath);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, string key)
        {
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Netlify Blobs {operation} '{key}' failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
        }
    }
}
