using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
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

    internal sealed class NetlifyBlobsBackend : IKeyValueBackend, IDisposable
    {
        private const string SignedUrlAccept = "application/json;type=signed-url";
        private readonly HttpClient _api;
        private readonly HttpClient _signed;
        private readonly string _storePath;
        private readonly Uri _signedOrigin;

        public NetlifyBlobsBackend(string siteId, string accessToken, string storeName, string apiUrl, HttpMessageHandler handler,
            Uri signedOrigin = null)
        {
            if (string.IsNullOrWhiteSpace(siteId)) throw new ArgumentException("A Netlify site id is required.", nameof(siteId));
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("A Netlify access token is required.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(storeName)) throw new ArgumentException("A blob store name is required.", nameof(storeName));

            _storePath = $"api/v1/blobs/{Uri.EscapeDataString(siteId)}/{Uri.EscapeDataString("site:" + storeName)}";
            _signedOrigin = signedOrigin;
            _api = CreateClient(handler);
            _api.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
            _api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            // Signed URLs carry their own authorisation and must not receive the API token.
            _signed = CreateClient(handler);
        }

        private HttpClient CreateClient(HttpMessageHandler handler)
        {
            if (handler != null) return new HttpClient(handler, disposeHandler: false);
            if (_signedOrigin == null) return new HttpClient();
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(transport);
        }

        private string BlobPath(string key) => $"{_storePath}/{string.Join("/", key.Split('/').Select(Uri.EscapeDataString))}";

        private async Task<string> GetSignedUrlAsync(HttpMethod method, string key, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(method, BlobPath(key));
            request.Headers.Accept.ParseAdd(SignedUrlAccept);
            var completion = _signedOrigin == null ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;
            using var response = await _api.SendAsync(request, completion, ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (_signedOrigin != null) response.EnsureSuccessStatusCode();
            else await EnsureSuccessAsync(response, $"sign {method}", key);
            using var doc = JsonDocument.Parse(_signedOrigin == null ? await response.Content.ReadAsStringAsync(ct)
                : await ReadBoundedTextAsync(response, 8192, ct));
            var url = doc.RootElement.GetProperty("url").GetString();
            if (_signedOrigin != null && (string.IsNullOrWhiteSpace(url) ||
                doc.RootElement.EnumerateObject().Count(property => property.NameEquals("url")) != 1))
                throw new InvalidDataException("Netlify returned an invalid signed URL envelope.");
            return url;
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_signedOrigin != null && (!key.StartsWith("holon/", StringComparison.Ordinal) ||
                !Guid.TryParseExact(key.AsSpan(6), "N", out var id) || id == Guid.Empty || key != "holon/" + id.ToString("N")))
                throw new InvalidOperationException("Netlify CQL reads only canonical holon keys.");
            var url = await GetSignedUrlAsync(HttpMethod.Get, key, cancellationToken);
            if (url == null) return null;
            if (_signedOrigin != null && (url.Length > 8192 || !Uri.TryCreate(url, UriKind.Absolute, out var signed) ||
                signed.UserInfo.Length != 0 || signed.Fragment.Length != 0 || signed.Query.Length == 0 ||
                signed.GetLeftPart(UriPartial.Authority) != _signedOrigin.GetLeftPart(UriPartial.Authority)))
                throw new InvalidDataException("Netlify returned a signed URL outside the approved object origin.");
            var completion = _signedOrigin == null ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;
            using var response = await _signed.GetAsync(url, completion, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (_signedOrigin != null)
            {
                response.EnsureSuccessStatusCode();
                return await ReadBoundedTextAsync(response, 1024 * 1024, cancellationToken);
            }
            await EnsureSuccessAsync(response, "get", key);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            RequireStorageMode();
            var url = await GetSignedUrlAsync(HttpMethod.Put, key, cancellationToken)
                      ?? throw new HttpRequestException($"Netlify Blobs did not return an upload URL for '{key}'.");
            using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(value));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var response = await _signed.PutAsync(url, content, cancellationToken);
            await EnsureSuccessAsync(response, "put", key);
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            RequireStorageMode();
            using var response = await _api.DeleteAsync(BlobPath(key), cancellationToken);
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
            RequireStorageMode();
            using var response = await _api.GetAsync($"{_storePath}?prefix={Uri.EscapeDataString("__oasis_verify__")}", cancellationToken);
            await EnsureSuccessAsync(response, "open store", _storePath);
        }

        private void RequireStorageMode()
        {
            if (_signedOrigin != null) throw new NotSupportedException("The enrolled Netlify profile supports only exact reads.");
        }

        private static async Task<string> ReadBoundedTextAsync(HttpResponseMessage response, int maximumBytes, CancellationToken token)
        {
            if (response.Content.Headers.ContentLength > maximumBytes) throw new InvalidDataException("Netlify response exceeds its byte budget.");
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var body = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(buffer, token)) != 0)
            {
                if (body.Length + count > maximumBytes) throw new InvalidDataException("Netlify response exceeds its byte budget.");
                body.Write(buffer, 0, count);
            }
            return new UTF8Encoding(false, true).GetString(body.GetBuffer(), 0, (int)body.Length);
        }

        public void Dispose()
        {
            _api.Dispose();
            _signed.Dispose();
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, string key)
        {
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Netlify Blobs {operation} '{key}' failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
        }
    }
}
