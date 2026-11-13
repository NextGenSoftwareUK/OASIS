#nullable enable
using System;
using System.Collections.Generic;
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

namespace NextGenSoftware.OASIS.API.Providers.TheGraphOASIS;

/// <summary>Reviewed public Subgraph Studio deployment status.</summary>
public sealed class TheGraphStudioStatusQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "the-graph-studio-index-status";
    private const string Probe = "adexto-arbitrum";
    private sealed record Deployment(string StudioPath, string Cid, string Network);
    private static readonly Dictionary<string, Deployment> Reviewed = new(StringComparer.Ordinal)
    {
        [Probe] = new("1757874/adexto-arbitrum/v1.0.1", "QmYpYjRUayUUBmiJpciHa4zgE91fonUVhdEg2gaTehvMo9", "arbitrum-one"),
        ["adexto-base"] = new("1757874/adexto-base/v1.0.1", "QmT1W8wYCgRoTSBaPrw8wtZJhv1avLxiQkNRXmvtqGCj9m", "base"),
        ["klerosboard-gnosis"] = new("66145/klerosboard-gnosis/version/latest", "QmYaqJVD9qHhjiqrsywQqcQ2TimGRzfqQA8nBew1ZRvL2z", "gnosis")
    };

    private readonly StatusBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public TheGraphStudioStatusQuerySource(Guid owner) : this(owner, handler: null) { }

    internal TheGraphStudioStatusQuerySource(Guid owner, HttpMessageHandler? handler)
        : this(new StatusBackend(owner, handler), owner) { }

    private TheGraphStudioStatusQuerySource(StatusBackend backend, Guid owner) : base(backend, "graph-studio-status:")
    {
        this.backend = backend;
        probeId = LogicalId(owner, Probe);
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed public Subgraph Studio indexing status";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Network);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3);
    }

    public static Guid LogicalId(Guid owner, string key)
    {
        if (owner == Guid.Empty || key is null || !Reviewed.ContainsKey(key))
            throw new ArgumentException("A teaching owner and reviewed Subgraph Studio deployment are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.TheGraphOASIS, Source, key);
    }

    public PType QueryProviderType => PType.TheGraphOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(probeId, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose()
    {
        IsProviderActivated = false;
        backend.Dispose();
    }

    private sealed class StatusBackend : IKeyValueBackend, IDisposable
    {
        private const string Route = "https://api.studio.thegraph.com/query/";
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> deployments;
        private readonly HttpClient client;

        public StatusBackend(Guid owner, HttpMessageHandler? handler)
        {
            this.owner = owner;
            deployments = Reviewed.Keys.ToDictionary(key => LogicalId(owner, key));
            if (handler is null)
            {
                var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                transport.UseCookies = false;
                transport.UseProxy = false;
                handler = transport;
            }
            client = new HttpClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!deployments.TryGetValue(id, out var key))
                throw new InvalidDataException("The selected ID is outside the reviewed Subgraph Studio deployment catalogue.");

                        var deployment = Reviewed[key];
            using var request = new HttpRequestMessage(HttpMethod.Post, Route + deployment.StudioPath)
            {
                Content = JsonContent.Create(new
                {
                    query = "query Status { _meta { deployment hasIndexingErrors block { number hash timestamp } } }"
                })
            };
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("Subgraph Studio must return a JSON query response.");

            await response.Content.LoadIntoBufferAsync(32 * 1024, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 16 });
            var root = RequireObject(document.RootElement);
            if (root.TryGetProperty("errors", out _))
                throw new InvalidDataException("Subgraph Studio returned a GraphQL error.");
            var meta = RequireObject(RequireObject(root.GetProperty("data")).GetProperty("_meta"));
            var block = RequireObject(meta.GetProperty("block"));
            var hash = block.GetProperty("hash").GetString();
            var height = block.GetProperty("number").GetInt32();
            var timestamp = block.GetProperty("timestamp").GetInt64();
            if (meta.GetProperty("deployment").GetString() != deployment.Cid ||
                meta.GetProperty("hasIndexingErrors").GetBoolean() || height <= 0 || timestamp <= 0 ||
                hash is null || hash.Length != 66 || !hash.StartsWith("0x", StringComparison.Ordinal) ||
                hash.Skip(2).Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
                throw new InvalidDataException("Subgraph Studio did not return the reviewed deployment and healthy block.");

            var record = new NativeRecord(Source, key, new Dictionary<string, object>
            {
                ["name"] = key,
                ["GraphDeployment"] = deployment.Cid,
                ["GraphNetwork"] = deployment.Network,
                ["GraphIndexedBlockNumber"] = height,
                ["GraphIndexedBlockHash"] = hash,
                ["GraphIndexedBlockTimestamp"] = timestamp
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.TheGraphOASIS, record, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.TheGraphOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static JsonElement RequireObject(JsonElement value)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Any(property => !names.Add(property.Name)))
                throw new InvalidDataException("Subgraph Studio must return unique-field objects.");
            return value;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Subgraph Studio deployment CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Subgraph Studio deployment scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Subgraph Studio exposes exact reviewed deployment status reads."));
        public void Dispose() => client.Dispose();
    }
}
