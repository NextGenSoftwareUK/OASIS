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
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests")]

namespace NextGenSoftware.OASIS.API.Providers.VercelKVOASIS
{
    /// <summary>
    /// Stores OASIS avatars and holons in Vercel KV. Vercel KV is Upstash Redis, reached over the Upstash REST API with the
    /// KV_REST_API_URL and KV_REST_API_TOKEN values from the Vercel project.
    /// </summary>
    public class VercelKVOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        public VercelKVOASIS(string restApiUrl, string restApiToken)
            : this(new UpstashRestBackend(restApiUrl, restApiToken, null))
        {
        }

        internal VercelKVOASIS(UpstashRestBackend backend) : base(backend)
        {
            ProviderName = "VercelKVOASIS";
            ProviderDescription = "Vercel KV provider: OASIS avatars and holons in Vercel KV (Upstash Redis REST).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.VercelKVOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }
    }

    internal sealed class UpstashRestBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient _http;
        private readonly bool _queryOnly;

        public UpstashRestBackend(string restApiUrl, string restApiToken, HttpMessageHandler handler, bool queryOnly = false)
        {
            if (string.IsNullOrWhiteSpace(restApiUrl)) throw new ArgumentException("KV_REST_API_URL is required.", nameof(restApiUrl));
            if (string.IsNullOrWhiteSpace(restApiToken)) throw new ArgumentException("KV_REST_API_TOKEN is required.", nameof(restApiToken));
            _queryOnly = queryOnly;
            if (queryOnly && handler == null)
            {
                var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                transport.UseCookies = false;
                transport.UseProxy = false;
                _http = new HttpClient(transport);
            }
            else
                _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(restApiUrl.TrimEnd('/') + "/");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", restApiToken);
        }

        private async Task<JsonElement> CommandAsync(CancellationToken ct, params string[] command)
        {
            if (_queryOnly && (command.Length != 2 || command[0] != "GET" ||
                !command[1].StartsWith("oasis/holon/", StringComparison.Ordinal) ||
                !Guid.TryParseExact(command[1].AsSpan(12), "N", out var id) || command[1] != $"oasis/holon/{id:N}"))
                throw new InvalidOperationException("The migrated Vercel KV query source permits only exact Holon GET commands.");

            using var request = new HttpRequestMessage(HttpMethod.Post, _http.BaseAddress)
            {
                Content = new StringContent(JsonSerializer.Serialize(command), Encoding.UTF8, "application/json")
            };
            using var response = await _http.SendAsync(request, _queryOnly
                ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, ct);
            if (_queryOnly)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new HttpRequestException("Upstash exact read failed.", null, response.StatusCode);
                await response.Content.LoadIntoBufferAsync(1024 * 1024, ct);
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (_queryOnly && (doc.RootElement.ValueKind != JsonValueKind.Object ||
                doc.RootElement.EnumerateObject().Count() != 1 ||
                !doc.RootElement.TryGetProperty("result", out var result) ||
                result.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)))
                throw new HttpRequestException("Upstash returned an invalid exact read result.");
            if (doc.RootElement.TryGetProperty("error", out var error))
                throw new HttpRequestException($"Vercel KV {command[0]} failed: {error.GetString()}");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Vercel KV {command[0]} failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
            return doc.RootElement.GetProperty("result").Clone();
        }

        public void Dispose() => _http.Dispose();

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var result = await CommandAsync(cancellationToken, "GET", key);
            return result.ValueKind == JsonValueKind.Null ? null : result.GetString();
        }

        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken, "SET", key, value);

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken, "DEL", key);

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var pattern = EscapeGlob(prefix) + "*";
            var keys = new List<string>();
            var cursor = "0";
            do
            {
                var result = await CommandAsync(cancellationToken, "SCAN", cursor, "MATCH", pattern, "COUNT", "1000");
                cursor = result[0].ValueKind == JsonValueKind.Number ? result[0].GetRawText() : result[0].GetString();
                foreach (var key in result[1].EnumerateArray()) keys.Add(key.GetString());
            } while (cursor != "0");

            // SCAN may return a key more than once across iterations.
            return new List<string>(new HashSet<string>(keys));
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var pong = await CommandAsync(cancellationToken, "PING");
            if (pong.GetString() != "PONG") throw new HttpRequestException($"Vercel KV PING returned '{pong}'.");
        }

        private static string EscapeGlob(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (c is '*' or '?' or '[' or ']' or '\\') sb.Append('\\');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
