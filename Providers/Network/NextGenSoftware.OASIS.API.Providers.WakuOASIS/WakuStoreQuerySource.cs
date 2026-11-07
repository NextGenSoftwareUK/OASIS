#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.WakuOASIS;

/// <summary>Exact public metadata from an isolated Waku Store v3.</summary>
public sealed class WakuStoreQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "waku-store-v3/oasis-cql/1/store-fixture/proto";
    private const string PubsubTopic = "/waku/2/rs/123/1";
    private const string ContentTopic = "/oasis-cql/1/store-fixture/proto";
    private readonly StoreBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly IReadOnlyList<Guid> probeIds;

    public WakuStoreQuerySource(Guid owner, IReadOnlyList<string> reviewedHashes)
        : this(owner, reviewedHashes, handler: null) { }

    internal WakuStoreQuerySource(Guid owner, IReadOnlyList<string> reviewedHashes, HttpMessageHandler? handler,
        bool verifyCatalogue = false)
        : this(new StoreBackend(owner, reviewedHashes, handler), verifyCatalogue) { }

    private WakuStoreQuerySource(StoreBackend backend, bool verifyCatalogue) : base(backend, "waku-store:")
    {
        this.backend = backend;
        probeIds = verifyCatalogue ? backend.ReviewedIds : new[] { backend.ReviewedIds[0] };
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed public Waku Store messages";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Network);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3);
    }

    public static Guid LogicalId(Guid owner, string hash)
    {
        if (owner == Guid.Empty || hash is null || hash.Length != 66 ||
            !hash.StartsWith("0x", StringComparison.Ordinal) ||
            hash.Skip(2).Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ArgumentException("A teaching owner and canonical Waku message hash are required.");

        return NativeRecordHolonMapper.StableHolonId(owner, PType.WakuOASIS, Source, hash);
    }

    public PType QueryProviderType => PType.WakuOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        foreach (var probe in probeIds)
        {
            var result = await query.VerifyProbeAsync(probe, cancellationToken);
            if (result.IsError || !result.Result)
                return result;
        }

        IsProviderActivated = true;
        return new OASISResult<bool>(true);
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose()
    {
        IsProviderActivated = false;
        backend.Dispose();
    }

    private sealed class StoreBackend : IKeyValueBackend, IDisposable
    {
        private const string Route = "http://127.0.0.1:28645/store/v3/messages?includeData=true&pageSize=4&hashes=";
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> hashes;
        private readonly HttpClient client;

        public StoreBackend(Guid owner, IReadOnlyList<string> reviewedHashes, HttpMessageHandler? handler)
        {
            ArgumentNullException.ThrowIfNull(reviewedHashes);
            if (reviewedHashes.Count is < 1 or > 4)
                throw new ArgumentException("Select one to four reviewed Waku message hashes.", nameof(reviewedHashes));

            this.owner = owner;
            hashes = reviewedHashes.ToDictionary(hash => LogicalId(owner, hash));
            ReviewedIds = reviewedHashes.Select(hash => LogicalId(owner, hash)).ToArray();
            client = new HttpClient(handler ?? new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseProxy = false,
                UseCookies = false
            });
        }

        public IReadOnlyList<Guid> ReviewedIds { get; }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!hashes.TryGetValue(id, out var hash))
                throw new InvalidDataException("The selected ID is outside the reviewed Waku catalogue.");

            using var request = new HttpRequestMessage(HttpMethod.Get, Route + hash);
            request.Headers.Accept.ParseAdd("application/json");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("Waku Store must return a JSON query response.");

            await response.Content.LoadIntoBufferAsync(64 * 1024, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 12 });
            var root = RequireObject(document.RootElement);
            var messages = root.GetProperty("messages");
            if (root.GetProperty("statusCode").GetInt32() != 200 ||
                root.TryGetProperty("paginationCursor", out _) || messages.GetArrayLength() > 1)
                throw new InvalidDataException("Waku Store must return one complete exact hash selection.");

            if (messages.GetArrayLength() == 0) return null!;

            var entry = RequireObject(messages[0]);
            var message = RequireObject(entry.GetProperty("message"));
            var timestamp = message.GetProperty("timestamp").GetInt64();
            if (entry.GetProperty("messageHash").GetString() != hash ||
                entry.GetProperty("pubsubTopic").GetString() != PubsubTopic ||
                message.GetProperty("contentTopic").GetString() != ContentTopic ||
                timestamp <= 0 || message.GetProperty("ephemeral").GetBoolean())
                throw new InvalidDataException("Waku Store returned a message outside the reviewed identity or topic.");

            var payload = message.GetProperty("payload").GetBytesFromBase64();
            var metadata = message.TryGetProperty("meta", out var meta) ? meta.GetBytesFromBase64() : Array.Empty<byte>();
            using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            digest.AppendData(Encoding.UTF8.GetBytes(PubsubTopic));
            digest.AppendData(payload);
            digest.AppendData(Encoding.UTF8.GetBytes(ContentTopic));
            digest.AppendData(metadata);
            var timestampBytes = new byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(timestampBytes, timestamp);
            digest.AppendData(timestampBytes);
            if (!digest.GetHashAndReset().AsSpan().SequenceEqual(Convert.FromHexString(hash.AsSpan(2))))
                throw new InvalidDataException("Waku Store message content does not match the requested hash.");

            var record = new NativeRecord(Source, hash, new Dictionary<string, object>
            {
                ["name"] = hash,
                ["WakuMessageHash"] = hash,
                ["WakuPubsubTopic"] = PubsubTopic,
                ["WakuContentTopic"] = ContentTopic,
                ["WakuTimestampNanoseconds"] = timestamp,
                ["WakuPayloadBytes"] = payload.Length
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.WakuOASIS, record, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.Message);

            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.WakuOASIS] = hash;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static JsonElement RequireObject(JsonElement value)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Any(property => !names.Add(property.Name)))
                throw new InvalidDataException("Waku Store must return a unique-field JSON object.");
            return value;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Waku Store CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Waku Store scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Waku Store exposes reviewed exact hash reads."));
        public void Dispose() => client.Dispose();
    }
}