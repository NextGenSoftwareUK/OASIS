#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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

namespace NextGenSoftware.OASIS.API.Providers.SubqueryOASIS;

/// <summary>
/// Exact configured block matches from the SubQuery V1 Arbitrum dictionary.
/// A match describes indexed Transfer-topic activity, not a complete log,
/// independently verified block header, finality proof or writable native holon.
/// </summary>
public sealed class SubqueryDictionaryBlockQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "subquery/arbitrum/transfer-block";
    public const string ReportedGenesis = "0x7ee576b35482195fc49205cec9af72ce14f003b9ae69f6ba0faef4514be8b442";
    public const string TransferTopic = "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef";
    private readonly DictionaryBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    public SubqueryDictionaryBlockQuerySource(Guid owner, IEnumerable<long> heights)
        : this(owner, heights, null) { }

    public static SubqueryDictionaryBlockQuerySource ForReviewedTeachingCatalogue(Guid owner) =>
        new(owner, new long[] { 400000000, 400000002, 400000003 });

    internal SubqueryDictionaryBlockQuerySource(Guid owner, IEnumerable<long> heights, HttpMessageHandler? handler)
        : this(new DictionaryBackend(owner, heights, handler)) { }

    private SubqueryDictionaryBlockQuerySource(DictionaryBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Configured public SubQuery V1 dictionary block matches";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Network);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3);
    }

    public PType QueryProviderType => PType.SubqueryOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static Guid LogicalId(Guid owner, long height)
    {
        if (owner == Guid.Empty || height <= 0 || height == long.MaxValue)
            throw new ArgumentException("A source owner and bounded positive block height are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.SubqueryOASIS, Source,
            height.ToString(CultureInfo.InvariantCulture));
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(backend.Probe, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose() { IsProviderActivated = false; backend.Dispose(); }

    private sealed class DictionaryBackend : IKeyValueBackend, IDisposable
    {
        private static readonly Uri Endpoint = new("https://dict-tyk.subquery.network/query/arbitrum");
        private readonly Guid owner;
        private readonly Dictionary<Guid, long> heights;
        private readonly HttpClient client;
        public Guid Probe { get; }

        public DictionaryBackend(Guid owner, IEnumerable<long> configured, HttpMessageHandler? handler)
        {
            if (configured is null) throw new ArgumentNullException(nameof(configured));
            var selected = configured.Take(9).ToArray();
            if (selected.Length is < 1 or > 8 || selected.Distinct().Count() != selected.Length)
                throw new ArgumentException("One to eight distinct explicit dictionary heights are required.");
            heights = selected.ToDictionary(height => LogicalId(owner, height));
            this.owner = owner;
            Probe = LogicalId(owner, selected[0]);
            if (handler is null)
            {
                var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                transport.AllowAutoRedirect = false; transport.UseCookies = false; transport.UseProxy = false;
                handler = transport;
            }
            client = new HttpClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!heights.TryGetValue(id, out var height))
                throw new InvalidDataException("The ID is outside the configured dictionary catalogue.");
            var key = height.ToString(CultureInfo.InvariantCulture);
            var end = checked(height + 1).ToString(CultureInfo.InvariantCulture);
            // The pinned V1 SDK uses quoted decimal range boundaries and the
            // BLOCK_HEIGHT ordering/distinct contract. Only validated integers
            // are interpolated; no caller-supplied query text or URL is accepted.
            var text = "query Match($topic:String!){ _metadata { lastProcessedHeight genesisHash startHeight } " +
                "evmLogs(filter:{or:[{topics0:{equalTo:$topic}}],blockHeight:{greaterThanOrEqualTo:\"" + key +
                "\",lessThan:\"" + end + "\"}},orderBy:BLOCK_HEIGHT_ASC,first:1,distinct:[BLOCK_HEIGHT]){nodes{blockHeight}} }";
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(new { query = text, variables = new { topic = TransferTopic } })
            };
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("The dictionary must return a JSON response.");
            await response.Content.LoadIntoBufferAsync(32768, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 16 });
            var root = Object(document.RootElement);
            if (root.TryGetProperty("errors", out _)) throw new InvalidDataException("Dictionary GraphQL errors are not partial results.");
            var data = Object(root.GetProperty("data"));
            var metadata = Object(data.GetProperty("_metadata"));
            if (metadata.GetProperty("genesisHash").GetString() != ReportedGenesis ||
                !metadata.GetProperty("lastProcessedHeight").TryGetInt64(out var indexed) || indexed < height ||
                !metadata.GetProperty("startHeight").TryGetInt64(out var start) || start < 1 || start > height)
                throw new InvalidDataException("Dictionary reported identity/index coverage differs.");
            var nodes = Object(data.GetProperty("evmLogs")).GetProperty("nodes");
            if (nodes.ValueKind != JsonValueKind.Array || nodes.GetArrayLength() > 1)
                throw new InvalidDataException("The exact one-height query exceeded its bound.");
            if (nodes.GetArrayLength() == 0) return null!;
            if (Object(nodes[0]).GetProperty("blockHeight").GetString() != key)
                throw new InvalidDataException("Dictionary match differs from the selected height.");
            var record = new NativeRecord(Source, key, new Dictionary<string, object>
            {
                ["name"] = "SubQuery Transfer-topic block " + key,
                ["SubqueryBlockHeight"] = key,
                ["SubqueryDictionary"] = Endpoint.AbsoluteUri,
                ["SubqueryTopic0"] = TransferTopic,
                // Index coverage is a read boundary, not a changing attribute
                // of this historical match.
                ["SubqueryReportedGenesisHash"] = ReportedGenesis
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.SubqueryOASIS, record, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.SubqueryOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static JsonElement Object(JsonElement value)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Any(property => !names.Add(property.Name)))
                throw new InvalidDataException("Dictionary objects must have unique fields.");
            return value;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact dictionary CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Dictionary scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("SubQuery dictionary matches are read-only."));
        public void Dispose() => client.Dispose();
    }
}
