using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.DenoDeployOASIS.Datapath;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests")]

namespace NextGenSoftware.OASIS.API.Providers.DenoDeployOASIS
{
    /// <summary>
    /// Stores OASIS avatars and holons in a Deno KV database on Deno Deploy using the KV Connect protocol
    /// (https://github.com/denoland/denokv/blob/main/proto/kv-connect.md): a metadata exchange returns the data
    /// endpoints and a short-lived token, then reads and writes are protobuf SnapshotRead / AtomicWrite requests.
    /// </summary>
    public class DenoDeployOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        /// <param name="accessToken">A Deno Deploy access token with access to the database.</param>
        /// <param name="databaseId">The Deno KV database id (shown in the Deno Deploy dashboard).</param>
        public DenoDeployOASIS(string accessToken, string databaseId)
            : this(new DenoKvConnectBackend($"https://api.deno.com/databases/{RequireId(databaseId)}/connect", accessToken, null))
        {
        }

        internal DenoDeployOASIS(DenoKvConnectBackend backend) : base(backend)
        {
            ProviderName = "DenoDeployOASIS";
            ProviderDescription = "Deno KV provider: OASIS avatars and holons in a Deno Deploy KV database (KV Connect).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.DenoDeployOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static string RequireId(string databaseId)
            => string.IsNullOrWhiteSpace(databaseId) ? throw new ArgumentException("A Deno KV database id is required.", nameof(databaseId)) : Uri.EscapeDataString(databaseId);
    }

    internal sealed class DenoKvConnectBackend : IKeyValueBackend, IDisposable
    {
        private const int PageSize = 500;
        private static readonly int[] SupportedVersions = { 1, 2, 3 };
        private static readonly int[] ReadOnlyVersions = { 2, 3 };
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);
        private const int MaximumValueBytes = 64 * 1024;

        private readonly string _connectUrl;
        private readonly string _accessToken;
        private readonly HttpClient _http;
        private readonly Uri _reviewedEndpoint;
        private readonly Guid _reviewedDatabaseId;
        private readonly SemaphoreSlim _metadataLock = new(1, 1);
        private Metadata _metadata;

        public DenoKvConnectBackend(string connectUrl, string accessToken, HttpMessageHandler handler,
            Uri reviewedEndpoint = null, Guid reviewedDatabaseId = default)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("A Deno Deploy access token is required.", nameof(accessToken));
            _connectUrl = connectUrl;
            _accessToken = accessToken;
            _reviewedEndpoint = reviewedEndpoint;
            _reviewedDatabaseId = reviewedDatabaseId;
            if (reviewedEndpoint != null && handler == null)
            {
                var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                transport.AllowAutoRedirect = false;
                transport.UseProxy = false;
                transport.UseCookies = false;
                _http = new HttpClient(transport);
            }
            else
                _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("NextGenSoftware-OASIS-DenoDeployOASIS/2.0");
        }

        private sealed record Metadata(int Version, string DatabaseId, string Endpoint, string Token, DateTimeOffset ExpiresAt);

        private bool QueryOnly => _reviewedEndpoint != null;

        public void Dispose()
        {
            _http.Dispose();
            _metadataLock.Dispose();
        }

        private Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            _http.SendAsync(request, QueryOnly ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, ct);

        private static async Task RequireBoundedResponseAsync(HttpResponseMessage response, string mediaType,
            int maximumBytes, CancellationToken ct)
        {
            if (response.StatusCode != HttpStatusCode.OK ||
                !string.Equals(response.Content.Headers.ContentType?.MediaType, mediaType, StringComparison.OrdinalIgnoreCase))
                throw new HttpRequestException($"Deno KV returned an unexpected response ({(int)response.StatusCode}).");
            await response.Content.LoadIntoBufferAsync(maximumBytes, ct);
        }

        #region Key encoding (denokv tuple encoding of a single string part)

        internal static byte[] EncodeKey(string key)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            var output = new List<byte>(bytes.Length + 2) { 0x02 };
            foreach (var b in bytes)
            {
                output.Add(b);
                if (b == 0) output.Add(0xFF);
            }
            output.Add(0x00);
            return output.ToArray();
        }

        internal static string DecodeKey(ByteString encoded)
        {
            var span = encoded.Span;
            if (span.Length < 2 || span[0] != 0x02 || span[^1] != 0x00)
                throw new FormatException("Deno KV key is not a single string key part.");
            var raw = new List<byte>(span.Length);
            for (var i = 1; i < span.Length - 1; i++)
            {
                raw.Add(span[i]);
                if (span[i] == 0 && i + 1 < span.Length - 1 && span[i + 1] == 0xFF) i++;
            }
            return Encoding.UTF8.GetString(raw.ToArray());
        }

        // Every encoded key whose string starts with the prefix sorts between these two bounds.
        private static (byte[] Start, byte[] End) PrefixRange(string prefix)
        {
            var start = EncodeKey(prefix);
            var open = start[..^1];
            return (open, open.Append((byte)0xFF).ToArray());
        }

        #endregion

        #region KV Connect transport

        private async Task<Metadata> GetMetadataAsync(CancellationToken ct, bool forceRefresh = false)
        {
            var current = _metadata;
            if (!forceRefresh && current != null && current.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30)) return current;

            await _metadataLock.WaitAsync(ct);
            try
            {
                if (!forceRefresh && _metadata != null && _metadata.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30)) return _metadata;

                var versions = QueryOnly ? ReadOnlyVersions : SupportedVersions;
                using var request = new HttpRequestMessage(HttpMethod.Post, _connectUrl)
                {
                    Content = JsonContent.Create(new { supportedVersions = versions })
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                using var response = await SendAsync(request, ct);
                if (QueryOnly)
                    await RequireBoundedResponseAsync(response, "application/json", 16 * 1024, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Deno KV metadata exchange failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var version = root.GetProperty("version").GetInt32();
                if (!versions.Contains(version)) throw new HttpRequestException($"Deno KV negotiated unsupported protocol version {version}.");
                if (QueryOnly)
                {
                    _metadata = ReadReviewedMetadata(root, version);
                    return _metadata;
                }
                var endpoint = root.GetProperty("endpoints").EnumerateArray()
                    .FirstOrDefault(e => e.GetProperty("consistency").GetString() == "strong");
                if (endpoint.ValueKind == JsonValueKind.Undefined) throw new HttpRequestException("Deno KV returned no strongly consistent endpoint.");

                _metadata = new Metadata(version, root.GetProperty("uuid").GetString(), endpoint.GetProperty("url").GetString().TrimEnd('/'),
                    root.GetProperty("token").GetString(), root.GetProperty("expiresAt").GetDateTimeOffset());
                return _metadata;
            }
            finally { _metadataLock.Release(); }
        }

        private Metadata ReadReviewedMetadata(JsonElement root, int version)
        {
            var databaseId = root.GetProperty("uuid").GetString();
            var token = root.GetProperty("token").GetString();
            var expiresAt = root.GetProperty("expiresAt").GetDateTimeOffset();
            if (!Guid.TryParseExact(databaseId, "D", out var parsedId) || parsedId != _reviewedDatabaseId ||
                !DenoDeployQuerySource.IsValidToken(token) || expiresAt <= DateTimeOffset.UtcNow)
                throw new HttpRequestException("Deno KV returned invalid credentials or a different database identity.");

            foreach (var endpoint in root.GetProperty("endpoints").EnumerateArray())
            {
                if (endpoint.GetProperty("consistency").GetString() == "strong" &&
                    Uri.TryCreate(new Uri(_connectUrl), endpoint.GetProperty("url").GetString(), out var resolved) &&
                    string.Equals(resolved.AbsoluteUri.TrimEnd('/'), _reviewedEndpoint.AbsoluteUri.TrimEnd('/'), StringComparison.Ordinal))
                    return new Metadata(version, databaseId, _reviewedEndpoint.AbsoluteUri.TrimEnd('/'), token, expiresAt);
            }
            throw new HttpRequestException("Deno KV did not return the reviewed strongly consistent endpoint.");
        }

        private async Task<(TOut Output, int Version)> CallAsync<TOut>(string method, IMessage input, MessageParser<TOut> parser, CancellationToken ct) where TOut : IMessage<TOut>
        {
            for (var attempt = 0; ; attempt++)
            {
                var meta = await GetMetadataAsync(ct, forceRefresh: attempt > 0);
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{meta.Endpoint}/{method}") { Content = new ByteArrayContent(input.ToByteArray()) };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", meta.Token);
                if (meta.Version == 1) request.Headers.Add("x-transaction-domain-id", meta.DatabaseId);
                else
                {
                    request.Headers.Add("x-denokv-version", meta.Version.ToString());
                    request.Headers.Add("x-denokv-database-id", meta.DatabaseId);
                }

                using var response = await SendAsync(request, ct);
                if (QueryOnly)
                    await RequireBoundedResponseAsync(response, "application/x-protobuf", MaximumValueBytes + 1024, ct);
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0) continue; // token expired early: re-exchange once
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Deno KV {method} failed: {(int)response.StatusCode} {response.ReasonPhrase} {await response.Content.ReadAsStringAsync(ct)}");
                return (parser.ParseFrom(await response.Content.ReadAsByteArrayAsync(ct)), meta.Version);
            }
        }

        private async Task<IReadOnlyList<KvEntry>> ReadRangeAsync(byte[] start, byte[] end, int limit, CancellationToken ct)
        {
            var read = new SnapshotRead();
            read.Ranges.Add(new ReadRange { Start = ByteString.CopyFrom(start), End = ByteString.CopyFrom(end), Limit = limit });
            var (output, version) = await CallAsync("snapshot_read", read, SnapshotReadOutput.Parser, ct);
            var unavailable = QueryOnly
                ? !output.ReadIsStronglyConsistent || (version == 3 ? output.Status != SnapshotReadStatus.SrSuccess : output.ReadDisabled)
                : output.ReadDisabled || output.Status == SnapshotReadStatus.SrReadDisabled;
            if (unavailable)
                throw new HttpRequestException("Deno KV region cannot serve reads right now.");
            if (output.Ranges.Count != 1) throw new HttpRequestException("Deno KV returned an unexpected number of read ranges.");
            return output.Ranges[0].Values;
        }

        private async Task WriteAsync(Mutation mutation, CancellationToken ct)
        {
            RequireStorageMode();
            var write = new AtomicWrite();
            write.Mutations.Add(mutation);
            var (output, _) = await CallAsync("atomic_write", write, AtomicWriteOutput.Parser, ct);
            if (output.Status != AtomicWriteStatus.AwSuccess)
                throw new HttpRequestException($"Deno KV atomic write failed with status {output.Status}.");
        }

        #endregion

        private void RequireStorageMode()
        {
            if (QueryOnly)
                throw new InvalidOperationException("The reviewed Deno KV query source permits only exact reads.");
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            using var deadline = QueryOnly ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : null;
            deadline?.CancelAfter(TimeSpan.FromSeconds(10));
            var encoded = EncodeKey(key);
            var values = await ReadRangeAsync(encoded, encoded.Append((byte)0x00).ToArray(), 1, deadline?.Token ?? cancellationToken);
            if (QueryOnly && (values.Count > 1 || (values.Count == 1 && !values[0].Key.Span.SequenceEqual(encoded))))
                throw new HttpRequestException("Deno KV returned records outside the requested exact key.");
            var entry = values.FirstOrDefault(v => v.Key.Span.SequenceEqual(encoded));
            if (entry == null) return null;
            if (entry.Encoding != ValueEncoding.VeBytes)
                throw new FormatException($"Deno KV value for '{key}' uses {entry.Encoding}; OASIS stores values as raw bytes.");
            if (!QueryOnly) return entry.Value.ToStringUtf8();
            if (entry.Value.Length > MaximumValueBytes)
                throw new HttpRequestException("Deno KV exceeded the value size limit.");
            return StrictUtf8.GetString(entry.Value.Span);
        }

        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => WriteAsync(new Mutation
            {
                Key = ByteString.CopyFrom(EncodeKey(key)),
                MutationType = MutationType.MSet,
                Value = new KvValue { Data = ByteString.CopyFromUtf8(value), Encoding = ValueEncoding.VeBytes }
            }, cancellationToken);

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => WriteAsync(new Mutation { Key = ByteString.CopyFrom(EncodeKey(key)), MutationType = MutationType.MDelete }, cancellationToken);

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            RequireStorageMode();
            var (start, end) = PrefixRange(prefix);
            var keys = new List<string>();
            while (true)
            {
                var page = await ReadRangeAsync(start, end, PageSize, cancellationToken);
                keys.AddRange(page.Select(e => DecodeKey(e.Key)));
                if (page.Count < PageSize) return keys;
                start = page[^1].Key.ToByteArray().Append((byte)0x00).ToArray();
            }
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            RequireStorageMode();
            await GetMetadataAsync(cancellationToken, forceRefresh: true);
            await ListKeysAsync("__oasis_verify__", cancellationToken);
        }
    }
}
