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

namespace NextGenSoftware.OASIS.API.Providers.EigenLayerOASIS;

/// <summary>
/// Public EigenLayer mainnet operators over an explicit address catalogue.
/// </summary>
public sealed class EigenLayerPublicOperatorSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "EigenLayerOperatorAddress";
    private readonly OperatorsBackend operators;
    private bool disposed;

    public EigenLayerPublicOperatorSource(IEnumerable<string> addresses, Guid sourceOwner)
        : this(new OperatorsBackend(addresses, sourceOwner)) { }

    private EigenLayerPublicOperatorSource(OperatorsBackend backend) : base(backend, backend.Prefix)
    {
        operators = backend;
        ProviderName = PType.EigenLayerOASIS.ToString();
        ProviderDescription = "Public EigenLayer mainnet block operators";
        ProviderType = new EnumValue<PType>(PType.EigenLayerOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 42, 8);
    }

    public PType QueryProviderType => PType.EigenLayerOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    public static EigenLayerPublicOperatorSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 2048)
            throw new ArgumentException("Bounded explicit EigenLayer public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 3 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(new[] { "addresses", "schema", "sourceOwner" }) ||
            root.GetProperty("schema").GetInt32() != 1)
            throw new ArgumentException("Exact EigenLayer public source schema required.");

        var addresses = root.GetProperty("addresses");
        if (addresses.ValueKind != JsonValueKind.Array || addresses.GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("One to eight EigenLayer addresses are required.");
        return new EigenLayerPublicOperatorSource(addresses.EnumerateArray().Select(address => address.GetString()),
            root.GetProperty("sourceOwner").GetGuid());
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await operators.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public EigenLayer operator source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public EigenLayer source is required.");
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
            if (keys.Any(key => !operators.AddressKeys.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select operator addresses from the configured catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await operators.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "EigenLayer query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public EigenLayer operator query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; operators.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private sealed class OperatorsBackend : IKeyValueBackend, IDisposable
    {
        private const string SourceName = "eigenlayer-mainnet-operators";
        private readonly EigenLayerPublicOperatorReader client;
        private readonly Guid owner;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> AddressKeys { get; }
        public string Prefix => "eigenlayer-public-operators/";

        public OperatorsBackend(IEnumerable<string> addresses, Guid owner)
        {
            ArgumentNullException.ThrowIfNull(addresses);
            var captured = addresses.Take(9).ToArray();
            if (owner == Guid.Empty || captured.Length is < 1 or > 8 ||
                captured.Any(address => address == null || !System.Text.RegularExpressions.Regex.IsMatch(address, @"\A0x[0-9a-f]{40}\z")) || captured.Distinct().Count() != captured.Length)
                throw new ArgumentException("An explicit owner and one to eight distinct lowercase Ethereum addresses are required.");
            this.owner = owner;
            AddressKeys = Array.AsReadOnly(captured);
            records = AddressKeys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.EigenLayerOASIS, SourceName, key).ToString("N"), key => key);
            client = new EigenLayerPublicOperatorReader();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var snapshot = await client.ReadAsync(keys, deadline.Token);
            var rows = new List<IHolon>(keys.Count);
            foreach (var observation in snapshot.Records)
            {
                var id = observation.Address;
                var fields = new Dictionary<string, object>
                {
                    [AddressMetadata] = id,
                    ["EigenLayerIsOperator"] = observation.IsOperator,
                    ["EigenLayerFinalizedBlockNumber"] = snapshot.BlockNumber,
                    ["EigenLayerFinalizedBlockHash"] = snapshot.BlockHash,
                    ["name"] = id,
                    ["description"] = "EigenLayer Ethereum operator registration"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.EigenLayerOASIS,
                    new NativeRecord(SourceName, id, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner;
                row.IsPublic = true;
                row.IsActive = true;
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.EigenLayerOASIS] = id;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken token) => await ReadAsync(AddressKeys, token);

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
            => Task.FromException(new NotSupportedException("Public EigenLayer metadata is read-only."));

        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public EigenLayer metadata is read-only."));

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
