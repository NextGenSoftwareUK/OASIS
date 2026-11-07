using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
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

namespace NextGenSoftware.OASIS.API.Providers.RitualOASIS;

/// <summary>
/// Public Ritual Chain headers over an explicit height catalogue.
/// </summary>
public sealed class RitualPublicBlockSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string HeightMetadata = "RitualBlockHeight";
    private readonly HeadersBackend headers;
    private bool disposed;

    public RitualPublicBlockSource(IEnumerable<long> heights, Guid sourceOwner)
        : this(new HeadersBackend(heights, sourceOwner)) { }

    private RitualPublicBlockSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        headers = backend;
        ProviderName = PType.RitualOASIS.ToString();
        ProviderDescription = "Public Ritual Chain block headers";
        ProviderType = new EnumValue<PType>(PType.RitualOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(HeightMetadata, 19, 8);
    }

    public PType QueryProviderType => PType.RitualOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    public static RitualPublicBlockSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 2048)
            throw new ArgumentException("Bounded explicit Ritual public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 3 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(new[] { "heights", "schema", "sourceOwner" }) ||
            root.GetProperty("schema").GetInt32() != 1)
            throw new ArgumentException("Exact Ritual public source schema required.");

        var heights = root.GetProperty("heights");
        if (heights.ValueKind != JsonValueKind.Array || heights.GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("One to eight Ritual heights are required.");
        return new RitualPublicBlockSource(heights.EnumerateArray().Select(height => height.GetInt64()),
            root.GetProperty("sourceOwner").GetGuid());
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await headers.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Ritual header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Ritual source is required.");
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
            if (keys.Any(key => !headers.HeightKeys.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select block heights from the configured catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await headers.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Ritual query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Ritual header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; headers.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private sealed class HeadersBackend : IKeyValueBackend, IDisposable
    {
        private const string SourceName = "ritual-chain-blocks";
        private readonly RitualPublicBlockReader client;
        private readonly Dictionary<string, long> catalogue;
        private readonly Guid owner;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> HeightKeys { get; }
        public string Prefix => "ritual-public-blocks/";

        public HeadersBackend(IEnumerable<long> heights, Guid owner)
        {
            ArgumentNullException.ThrowIfNull(heights);
            var captured = heights.Take(9).ToArray();
            if (owner == Guid.Empty || captured.Length is < 1 or > 8 ||
                captured.Any(height => height <= 0) || captured.Distinct().Count() != captured.Length)
                throw new ArgumentException("An explicit owner and one to eight distinct positive heights are required.");
            catalogue = captured.ToDictionary(height => height.ToString(CultureInfo.InvariantCulture));
            this.owner = owner;
            HeightKeys = Array.AsReadOnly(catalogue.Keys.ToArray());
            records = HeightKeys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.RitualOASIS, SourceName, key).ToString("N"), key => key);
            client = new RitualPublicBlockReader();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var blocks = await client.ReadAsync(keys.Select(id => catalogue[id]).ToArray(), deadline.Token);
            var rows = new List<IHolon>(keys.Count);
            foreach (var header in blocks)
            {
                var id = header.Height.ToString(CultureInfo.InvariantCulture);
                var fields = new Dictionary<string, object>
                {
                    [HeightMetadata] = id,
                    ["RitualChainId"] = "784",
                    ["RitualBlockTimestamp"] = header.Timestamp,
                    ["RitualBlockHash"] = header.BlockHash,
                    ["name"] = id,
                    ["description"] = "Ritual Chain public block"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.RitualOASIS,
                    new NativeRecord(SourceName, id, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner;
                row.IsPublic = true;
                row.IsActive = true;
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.RitualOASIS] = id;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken token) => await ReadAsync(HeightKeys, token);

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
            => Task.FromException(new NotSupportedException("Public Ritual metadata is read-only."));

        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Ritual metadata is read-only."));

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
