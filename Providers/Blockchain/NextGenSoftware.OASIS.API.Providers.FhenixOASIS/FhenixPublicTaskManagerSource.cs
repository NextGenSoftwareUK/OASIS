using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.FhenixOASIS;

/// <summary>
/// Public Fhenix CoFHE deployments over an explicit chain ID catalogue.
/// </summary>
public sealed class FhenixPublicTaskManagerSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string ChainIdMetadata = "FhenixChainId";
    private readonly TaskManagersBackend deployments;
    private bool disposed;

    public FhenixPublicTaskManagerSource(IEnumerable<string> chainIds, Guid sourceOwner)
        : this(new TaskManagersBackend(chainIds, sourceOwner)) { }

    private FhenixPublicTaskManagerSource(TaskManagersBackend backend) : base(backend, backend.Prefix)
    {
        deployments = backend;
        ProviderName = PType.FhenixOASIS.ToString();
        ProviderDescription = "Public Fhenix mainnet block deployments";
        ProviderType = new EnumValue<PType>(PType.FhenixOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(ChainIdMetadata, 8, 3);
    }

    public PType QueryProviderType => PType.FhenixOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    public static FhenixPublicTaskManagerSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 2048)
            throw new ArgumentException("Bounded explicit Fhenix public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 3 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(new[] { "chainIds", "schema", "sourceOwner" }) ||
            root.GetProperty("schema").GetInt32() != 1)
            throw new ArgumentException("Exact Fhenix public source schema required.");

        var chainIds = root.GetProperty("chainIds");
        if (chainIds.ValueKind != JsonValueKind.Array || chainIds.GetArrayLength() is < 1 or > 3)
            throw new ArgumentException("One to three Fhenix chain IDs are required.");
        return new FhenixPublicTaskManagerSource(chainIds.EnumerateArray().Select(chainId => chainId.GetString()),
            root.GetProperty("sourceOwner").GetGuid());
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await deployments.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Fhenix Task Manager source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Fhenix source is required.");
            if (maximumResults is < 1 or > 10000) return Failure("CQL_QUERY_LIMIT", "A bounded result budget is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Failure(admitted.ErrorCode, admitted.Message);
            IReadOnlyList<string> keys;
            if (QueryCapabilities.StringKeySetLookup.IsKeySetQuery(plan.Query))
            {
                var selected = QueryCapabilities.StringKeySetLookup.SelectKeys(plan.Query);
                if (selected.IsError) return Failure(selected.ErrorCode, selected.Message);
                keys = selected.Result;
            }
            else
            {
                var selected = QueryCapabilities.StringPointLookup.SelectKey(plan.Query);
                if (selected.IsError) return Failure(selected.ErrorCode, selected.Message);
                keys = new[] { selected.Result };
            }
            if (keys.Any(key => !deployments.ChainIds.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select CoFHE chain IDs from the configured catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await deployments.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Fhenix query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Fhenix Task Manager query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; deployments.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private sealed class TaskManagersBackend : IKeyValueBackend, IDisposable
    {
        private const string SourceName = "fhenix-cofhe-task-managers";
        private readonly FhenixPublicTaskManagerReader client;
        private readonly Guid owner;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> ChainIds { get; }
        public string Prefix => "fhenix-public-task-managers/";

        public TaskManagersBackend(IEnumerable<string> chainIds, Guid owner)
        {
            ArgumentNullException.ThrowIfNull(chainIds);
            var captured = chainIds.Take(4).ToArray();
            if (owner == Guid.Empty || captured.Length is < 1 or > 3 ||
                captured.Any(chainId => chainId is not ("1" or "11155111" or "84532")) || captured.Distinct().Count() != captured.Length)
                throw new ArgumentException("An explicit owner and one to three distinct qualified CoFHE chain IDs are required.");
            this.owner = owner;
            ChainIds = Array.AsReadOnly(captured);
            records = ChainIds.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.FhenixOASIS, SourceName, key).ToString("N"), key => key);
            client = new FhenixPublicTaskManagerReader();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>(keys.Count);
            foreach (var id in keys)
            {
                var observation = await client.ReadAsync(id, deadline.Token);
                var fields = new Dictionary<string, object>
                {
                    [ChainIdMetadata] = id,
                    ["FhenixTaskManagerAddress"] = observation.TaskManager,
                    ["FhenixFinalizedBlockNumber"] = observation.BlockNumber,
                    ["FhenixFinalizedBlockHash"] = observation.BlockHash,
                    ["name"] = id,
                    ["description"] = "Fhenix CoFHE Task Manager deployment"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.FhenixOASIS,
                    new NativeRecord(SourceName, id, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner;
                row.IsPublic = true;
                row.IsActive = true;
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.FhenixOASIS] = id;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken token) => await ReadAsync(ChainIds, token);

        public async Task<string> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out var id)) return null;
            var rows = await ReadAsync(new[] { id }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }

        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }

        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Fhenix metadata is read-only."));

        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Fhenix metadata is read-only."));

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
