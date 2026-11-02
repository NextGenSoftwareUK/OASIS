using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
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

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;

/// <summary>Explicit public ThreeFold node-summary projection; never owned database or native storage.</summary>
public sealed class ThreeFoldPublicNodeMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "ThreeFoldNodeId";
    private readonly NodesBackend nodes;
    private bool disposed;

    public ThreeFoldPublicNodeMetadataSource(HttpClient client, Uri rest,
        IEnumerable<string> keys, Guid sourceOwner, bool ownsClient = false, TimeProvider timeProvider = null)
        : this(new NodesBackend(client, rest, keys, sourceOwner, ownsClient, timeProvider)) { }

    private ThreeFoldPublicNodeMetadataSource(NodesBackend backend) : base(backend, backend.Prefix)
    {
        nodes = backend;
        ProviderName = PType.ThreeFoldOASIS.ToString();
        ProviderDescription = "Read-only public ThreeFold node metadata";
        ProviderType = new EnumValue<PType>(PType.ThreeFoldOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 10, 8);
    }

    public PType QueryProviderType => PType.ThreeFoldOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    // Reject mutations before accessing native data, including unknown IDs.
    private static OASISResult<T> ReadOnly<T>() => new()
    {
        IsError = true, ErrorCount = 1, ErrorCode = "CQL_READ_ONLY_SOURCE",
        Message = "ThreeFold public nodes cannot be saved or deleted."
    };

    public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon,
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
        bool continueOnError = true, bool saveChildrenOnProvider = false)
        => Task.FromResult(ReadOnly<IHolon>());

    public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        => Task.FromResult(ReadOnly<IEnumerable<IHolon>>());

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        => Task.FromResult(ReadOnly<IHolon>());

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        => Task.FromResult(ReadOnly<IHolon>());

    /// <summary>Separate host query-profile configuration, never the existing
    /// owned database/ADO settings. Reject unknown fields rather than ignore keys.</summary>
    public static ThreeFoldPublicNodeMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit public ThreeFold source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 5 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "nodeIds", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("nodeIds").ValueKind != JsonValueKind.Array || root.GetProperty("nodeIds").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous ThreeFold node source schema required.");
        var keys = root.GetProperty("nodeIds").EnumerateArray().Select(item => item.GetString()).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new ThreeFoldPublicNodeMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                keys, root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await nodes.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public ThreeFold node-summary source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public ThreeFold source is required.");
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
            if (keys.Any(key => !nodes.Keys.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select node IDs from the configured catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await nodes.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "ThreeFold query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public ThreeFold node-summary query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; nodes.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!seen.Add(property.Name)) throw new InvalidDataException("Duplicate native/configuration property.");
                Unique(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var entry in value.EnumerateArray()) Unique(entry);
    }

    private sealed class RpcRateGate
    {
        private readonly SemaphoreSlim mutex = new(1, 1);
        private readonly TimeProvider clock;
        private long? previous;

        public RpcRateGate(TimeProvider clock) => this.clock = clock;

        public async Task EnterAsync(CancellationToken token)
        {
            await mutex.WaitAsync(token);
            try
            {
                if (previous.HasValue)
                {
                    var remaining = TimeSpan.FromSeconds(1) - clock.GetElapsedTime(previous.Value);
                    if (remaining > TimeSpan.Zero)
                        await Task.Delay(remaining, clock, token);
                }
                token.ThrowIfCancellationRequested();
                previous = clock.GetTimestamp();
            }
            catch { mutex.Release(); throw; }
        }

        public void Exit() => mutex.Release();
    }

    private sealed class NodesBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly RpcRateGate rate;
        private static readonly RpcRateGate PublicRate = new(TimeProvider.System);
        private readonly Dictionary<string, string> records;
        private const string SourceName = "threefold-mainnet-grid-proxy:https://gridproxy.grid.tf/";
        public IReadOnlyList<string> Keys { get; }
        public string Prefix => "threefold-public-nodes/";

        public NodesBackend(HttpClient client, Uri api, IEnumerable<string> keys,
            Guid owner, bool ownsClient, TimeProvider clock)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.AbsoluteUri != "https://gridproxy.grid.tf/" ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous mainnet Grid Proxy root required.");
            var selected = keys?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(keys));
            if (selected.Length is < 1 or > 8 || selected.Any(key => !IsNodeId(key)) ||
                selected.Distinct(StringComparer.Ordinal).Count() != selected.Length)
                throw new ArgumentException("One to eight unique canonical nonzero UInt32 node IDs required.");
            this.client = client; this.owner = owner; this.ownsClient = ownsClient;
            Keys = Array.AsReadOnly(selected);
            rate = clock == null ? PublicRate : new RpcRateGate(clock);
            records = selected.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.ThreeFoldOASIS, SourceName, key).ToString("N"), key => key);
        }

        private static bool IsNodeId(string key) => key != null && key.Length <= 10 &&
            uint.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out uint value) && value != 0 &&
            value.ToString(CultureInfo.InvariantCulture) == key;

        private async Task<IHolon> ReadNodeAsync(string key, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            token = deadline.Token;
            await rate.EnterAsync(token);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://gridproxy.grid.tf/nodes/" + key);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(token);
                using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
                while ((count = await stream.ReadAsync(buffer.AsMemory(), token)) != 0)
                {
                    if (body.Length + count > 262144) throw new InvalidDataException("Native response exceeds256KiB.");
                    body.Write(buffer, 0, count);
                }
                using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
                var root = document.RootElement; Unique(root);
                if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _) || root.GetProperty("nodeId").GetUInt32().ToString(CultureInfo.InvariantCulture) != key)
                    throw new InvalidDataException("Configured node identity differs.");
                string farm = root.GetProperty("farmId").GetUInt32().ToString(CultureInfo.InvariantCulture);
                string version = root.GetProperty("gridVersion").GetUInt32().ToString(CultureInfo.InvariantCulture);
                string country = root.GetProperty("country").GetString();
                string city = root.GetProperty("city").GetString();
                if (country == null || city == null || country.Length > 256 || city.Length > 256)
                    throw new InvalidDataException("Bounded native geographic strings required.");
                var fields = new Dictionary<string, object>
                {
                    ["name"] = "ThreeFold node " + key,
                    ["description"] = "Public grid node metadata: " + city + ", " + country + ".",
                    [AddressMetadata] = key, ["ThreeFoldFarmId"] = farm, ["ThreeFoldGridVersion"] = version,
                    ["ThreeFoldCountry"] = country, ["ThreeFoldCity"] = city,
                    ["ThreeFoldObservationPolicy"] = "operator-reported-public-grid-node",
                    ["ThreeFoldChainStateVerified"] = "false"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ThreeFoldOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result; row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true;
                row.Children = Array.Empty<IHolon>(); row.ProviderUniqueStorageKey[PType.ThreeFoldOASIS] = key;
                token.ThrowIfCancellationRequested(); return row;
            }
            finally { rate.Exit(); }
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            // IN is bounded point composition, not an invented native list filter.
            // No rows are published if any selected native read fails.
            var rows = new List<IHolon>();
            foreach (string key in keys) rows.Add(await ReadNodeAsync(key, token));
            token.ThrowIfCancellationRequested(); return rows.AsReadOnly();
        }
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var rows = await ReadAsync(Keys, cancellationToken);
            if (rows.Count != Keys.Count) throw new InvalidDataException("Configured node selection incomplete.");
        }
        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var native)) return null;
            return StoredRecordJson.Serialize<IHolon>((await ReadAsync(new[] { native }, cancellationToken))[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public grid metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public grid metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
