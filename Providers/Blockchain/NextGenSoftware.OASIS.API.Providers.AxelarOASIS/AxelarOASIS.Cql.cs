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
using NextGenSoftware.OASIS.Common;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.AxelarOASIS;

/// <summary>
/// Public Axelar mainnet headers over an explicit height catalogue.
/// </summary>
public partial class AxelarOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string HeightMetadata = "AxelarBlockHeight";
    private readonly PublicHeaders? publicHeaders;
    private bool disposed;

    private AxelarOASIS(PublicHeaders headers) : this()
    {
        publicHeaders = headers;
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(HeightMetadata, 19, 8);
    }

    public PType QueryProviderType => PType.AxelarOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; } = new(CosmicQueryFeatures.None);

    public static AxelarOASIS FromPublicQueryConfiguration(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 2048)
            throw new ArgumentException("Bounded explicit Axelar public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 3 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(new[] { "heights", "schema", "sourceOwner" }) ||
            root.GetProperty("schema").GetInt32() != 1)
            throw new ArgumentException("Exact Axelar public source schema required.");

        var heights = root.GetProperty("heights");
        if (heights.ValueKind != JsonValueKind.Array || heights.GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("One to eight Axelar heights are required.");
        return new AxelarOASIS(new PublicHeaders(heights.EnumerateArray().Select(height => height.GetInt64()),
            root.GetProperty("sourceOwner").GetGuid()));
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        if (publicHeaders == null)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_SOURCE_UNAVAILABLE",
                Message = "Configure the public block catalogue before activating Axelar queries." };
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await publicHeaders.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Axelar header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (publicHeaders == null)
                return Failure("CQL_SOURCE_UNAVAILABLE", "Configure the public block catalogue before querying Axelar.");
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Axelar source is required.");
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
            if (keys.Any(key => !publicHeaders.HeightKeys.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select block heights from the configured catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await publicHeaders.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Axelar query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Axelar header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        IsProviderActivated = false;
        publicHeaders?.Dispose();
        _http.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private sealed class PublicHeaders : IDisposable
    {
        private const string SourceName = "axelar-mainnet-headers";
        private readonly AxelarPublicBlockClient client;
        private readonly Dictionary<string, long> catalogue;
        private readonly Guid owner;
        public IReadOnlyList<string> HeightKeys { get; }

        public PublicHeaders(IEnumerable<long> heights, Guid owner)
        {
            ArgumentNullException.ThrowIfNull(heights);
            var captured = heights.Take(9).ToArray();
            if (owner == Guid.Empty || captured.Length is < 1 or > 8 ||
                captured.Any(height => height <= 0) || captured.Distinct().Count() != captured.Length)
                throw new ArgumentException("An explicit owner and one to eight distinct positive heights are required.");
            catalogue = captured.ToDictionary(height => height.ToString(CultureInfo.InvariantCulture));
            this.owner = owner;
            HeightKeys = Array.AsReadOnly(catalogue.Keys.ToArray());
            client = new AxelarPublicBlockClient();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>(keys.Count);
            foreach (var id in keys)
            {
                var header = await client.ReadAsync(catalogue[id], deadline.Token);
                var fields = new Dictionary<string, object>
                {
                    [HeightMetadata] = id,
                    ["AxelarChainId"] = "axelar-dojo-1",
                    ["AxelarBlockTimestamp"] = header.Timestamp,
                    ["AxelarBlockHash"] = header.BlockHash,
                    ["name"] = id,
                    ["description"] = "Axelar mainnet public block header"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.AxelarOASIS,
                    new NativeRecord(SourceName, id, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner;
                row.IsPublic = true;
                row.IsActive = true;
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.AxelarOASIS] = id;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken token) => await ReadAsync(HeightKeys, token);

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
