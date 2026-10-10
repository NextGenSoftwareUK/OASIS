using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests")]

namespace NextGenSoftware.OASIS.API.Providers.DaprOASIS
{
    /// <summary>
    /// Stores OASIS avatars and holons in a Dapr state store (any Dapr state component: Redis, Cosmos DB, Postgres, ...)
    /// through the Dapr sidecar using the official Dapr .NET SDK.
    /// </summary>
    public class DaprOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        /// <param name="storeName">Name of the Dapr state store component.</param>
        /// <param name="daprGrpcEndpoint">Sidecar gRPC endpoint; null uses DAPR_GRPC_ENDPOINT / DAPR_GRPC_PORT.</param>
        /// <param name="daprApiToken">Sidecar API token if the sidecar requires one; null uses DAPR_API_TOKEN.</param>
        public DaprOASIS(string storeName = "statestore", string daprGrpcEndpoint = null, string daprApiToken = null)
            : this(new DaprStateBackend(new DaprClientStateApi(BuildClient(daprGrpcEndpoint, daprApiToken)), storeName))
        {
        }

        internal DaprOASIS(DaprStateBackend backend) : base(backend)
        {
            ProviderName = "DaprOASIS";
            ProviderDescription = "Dapr provider: OASIS avatars and holons in any Dapr state store component.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.DaprOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static DaprClient BuildClient(string grpcEndpoint, string apiToken)
        {
            var builder = new DaprClientBuilder();
            if (!string.IsNullOrWhiteSpace(grpcEndpoint)) builder.UseGrpcEndpoint(grpcEndpoint);
            if (!string.IsNullOrWhiteSpace(apiToken)) builder.UseDaprApiToken(apiToken);
            return builder.Build();
        }
    }

    /// <summary>The Dapr state operations the backend needs; implemented over DaprClient.</summary>
    internal interface IDaprStateApi
    {
        Task<(string Value, string ETag)> GetAsync(string store, string key, CancellationToken ct);
        Task SaveAsync(string store, string key, string value, CancellationToken ct);
        Task<bool> TrySaveAsync(string store, string key, string value, string etag, CancellationToken ct);
        Task DeleteAsync(string store, string key, CancellationToken ct);
        Task<bool> HealthyAsync(CancellationToken ct);
    }

    internal sealed class DaprClientStateApi : IDaprStateApi
    {
        private static readonly StateOptions Strong = new() { Consistency = ConsistencyMode.Strong, Concurrency = ConcurrencyMode.FirstWrite };
        private readonly DaprClient _client;

        public DaprClientStateApi(DaprClient client) => _client = client;

        public async Task<(string Value, string ETag)> GetAsync(string store, string key, CancellationToken ct)
        {
            var (value, etag) = await _client.GetStateAndETagAsync<string>(store, key, ConsistencyMode.Strong, cancellationToken: ct);
            return (value, etag);
        }

        public Task SaveAsync(string store, string key, string value, CancellationToken ct)
            => _client.SaveStateAsync(store, key, value, new StateOptions { Consistency = ConsistencyMode.Strong }, cancellationToken: ct);

        // An empty etag means "only if the key does not exist yet" under first-write concurrency.
        public Task<bool> TrySaveAsync(string store, string key, string value, string etag, CancellationToken ct)
            => _client.TrySaveStateAsync(store, key, value, etag ?? string.Empty, Strong, cancellationToken: ct);

        public Task DeleteAsync(string store, string key, CancellationToken ct)
            => _client.DeleteStateAsync(store, key, new StateOptions { Consistency = ConsistencyMode.Strong }, cancellationToken: ct);

        public Task<bool> HealthyAsync(CancellationToken ct) => _client.CheckHealthAsync(ct);
    }

    /// <summary>
    /// Dapr state stores cannot list keys, so each "directory" (key up to its last '/') keeps an index document of
    /// its keys, updated with ETag first-write concurrency and retried on conflict.
    /// </summary>
    internal sealed class DaprStateBackend : IKeyValueBackend
    {
        private const string IndexPrefix = "__oasis_index__/";
        private const int MaxConflictRetries = 20;
        private readonly IDaprStateApi _api;
        private readonly string _store;

        public DaprStateBackend(IDaprStateApi api, string store)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            if (string.IsNullOrWhiteSpace(store)) throw new ArgumentException("A Dapr state store name is required.", nameof(store));
            _store = store;
        }

        private static string Directory(string key) => key[..(key.LastIndexOf('/') + 1)];
        private static string IndexKey(string directory) => IndexPrefix + directory;

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
            => (await _api.GetAsync(_store, key, cancellationToken)).Value;

        public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            await _api.SaveAsync(_store, key, value, cancellationToken);
            await UpdateIndexAsync(Directory(key), keys => keys.Add(key), cancellationToken);
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            await _api.DeleteAsync(_store, key, cancellationToken);
            await UpdateIndexAsync(Directory(key), keys => keys.Remove(key), cancellationToken);
        }

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            if (!prefix.EndsWith("/", StringComparison.Ordinal))
                throw new NotSupportedException("Dapr key listing is by directory; prefixes must end with '/'.");
            var (json, _) = await _api.GetAsync(_store, IndexKey(prefix), cancellationToken);
            return json == null ? Array.Empty<string>() : JsonSerializer.Deserialize<List<string>>(json);
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            if (!await _api.HealthyAsync(cancellationToken))
                throw new InvalidOperationException("The Dapr sidecar is not healthy.");
            await _api.GetAsync(_store, IndexKey("__oasis_verify__/"), cancellationToken);
        }

        private async Task UpdateIndexAsync(string directory, Func<SortedSet<string>, bool> change, CancellationToken ct)
        {
            for (var attempt = 0; attempt < MaxConflictRetries; attempt++)
            {
                var (json, etag) = await _api.GetAsync(_store, IndexKey(directory), ct);
                var keys = json == null ? new SortedSet<string>(StringComparer.Ordinal) : new SortedSet<string>(JsonSerializer.Deserialize<List<string>>(json), StringComparer.Ordinal);
                if (!change(keys)) return;
                if (await _api.TrySaveAsync(_store, IndexKey(directory), JsonSerializer.Serialize(keys.ToList()), json == null ? null : etag, ct)) return;
            }
            throw new DaprException($"Could not update the Dapr key index for '{directory}' after {MaxConflictRetries} concurrent attempts.");
        }
    }
}
