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
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS;

public sealed record ZcashPublicBlockReference(int Height, string Hash);

/// <summary>Explicit public Zcash block-summary projection; never shielded-wallet or signed storage.</summary>
public sealed class ZcashPublicBlockMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "ZcashBlockHash";
    private readonly SummariesBackend summaries;
    private bool disposed;

    public ZcashPublicBlockMetadataSource(HttpClient client, Uri rest, string genesisHash,
        IEnumerable<ZcashPublicBlockReference> entries, Guid sourceOwner, bool ownsClient = false, TimeProvider timeProvider = null)
        : this(new SummariesBackend(client, rest, genesisHash, entries, sourceOwner, ownsClient, timeProvider)) { }

    private ZcashPublicBlockMetadataSource(SummariesBackend backend) : base(backend, backend.Prefix)
    {
        summaries = backend;
        ProviderName = PType.ZcashOASIS.ToString();
        ProviderDescription = "Read-only hash-pinned public Zcash block summaries";
        ProviderType = new EnumValue<PType>(PType.ZcashOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 64, 8);
    }

    public PType QueryProviderType => PType.ZcashOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    // Reject mutations before accessing native data, including unknown IDs.
    private static OASISResult<T> ReadOnly<T>() => new()
    {
        IsError = true, ErrorCount = 1, ErrorCode = "CQL_READ_ONLY_SOURCE",
        Message = "Zcash public summaries cannot be saved or deleted."
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
    /// RPC wallet/bridge settings. Reject unknown fields rather than ignore keys.</summary>
    public static ZcashPublicBlockMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit public Zcash source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 5 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "genesisHash", "blocks", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("blocks").ValueKind != JsonValueKind.Array || root.GetProperty("blocks").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Zcash source schema required; wallet/key fields forbidden.");
        var entries = root.GetProperty("blocks").EnumerateArray().Select(item =>
        {
            if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 2 ||
                !item.TryGetProperty("height", out var height) || height.ValueKind != JsonValueKind.Number ||
                !item.TryGetProperty("hash", out var hash) || hash.ValueKind != JsonValueKind.String)
                throw new ArgumentException("Exact height/hash mappings required.");
            return new ZcashPublicBlockReference(height.GetInt32(), hash.GetString());
        }).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new ZcashPublicBlockMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                root.GetProperty("genesisHash").GetString(), entries, root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await summaries.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Zcash block-summary source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Zcash source is required.");
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
            if (keys.Any(key => !summaries.Hashes.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select block hashes from the configured catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await summaries.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Zcash query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Zcash block-summary query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; summaries.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public const string Genesis = "00040fe8ec8471911baa1db1266ea15dd06b4a8a5c453883c000b031973dce08";

    private static void Hash(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A[0-9a-f]{64}\\z"))
            throw new InvalidDataException("Canonical native32byte block hash required.");
    }

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

    private sealed class SummariesBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly RpcRateGate rate;
        private static readonly RpcRateGate PublicRate = new(TimeProvider.System);
        private readonly Dictionary<string, ZcashPublicBlockReference> entries;
        private readonly Dictionary<string, string> records;
        private readonly string sourceName;
        public IReadOnlyList<string> Hashes { get; }
        public string Prefix => "zcash-public-metadata/";

        public SummariesBackend(HttpClient client, Uri api, string genesis,
            IEnumerable<ZcashPublicBlockReference> blocks, Guid owner, bool ownsClient, TimeProvider clock)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty || genesis != Genesis)
                throw new ArgumentException("Explicit anonymous HTTPS Zcash mainnet source required.");
            var selected = blocks?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(blocks));
            if (selected.Length is < 1 or > 8 || selected.Any(row => row == null || row.Height < 0) ||
                selected.Select(row => row.Height).Distinct().Count() != selected.Length ||
                selected.Select(row => row.Hash).Distinct(StringComparer.Ordinal).Count() != selected.Length)
                throw new ArgumentException("One to eight unique height/hash mappings required.");
            foreach (var row in selected) Hash(row.Hash);
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            entries = selected.ToDictionary(row => row.Hash, StringComparer.Ordinal);
            Hashes = Array.AsReadOnly(selected.Select(row => row.Hash).ToArray());
            rate = clock == null ? PublicRate : new RpcRateGate(clock);
            sourceName = "zecblock-public-summaries:" + api.AbsoluteUri + ":" + Genesis;
            records = selected.ToDictionary(row => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.ZcashOASIS, sourceName, row.Hash).ToString("N"), row => row.Hash);
        }

        private sealed record Summary(int Height, string Hash, uint Timestamp, string Freshness);

        private async Task<Summary> ReadSummaryAsync(int height, string expectedHash, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            token = deadline.Token;
            await rate.EnterAsync(token);
            try
            {
                string number = height.ToString(CultureInfo.InvariantCulture);
                var uri = new Uri(api, "v1/blocks?min_height=" + number + "&max_height=" + number + "&limit=1&order=oldest");
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
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
                if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                    !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array || data.GetArrayLength() != 1 ||
                    !root.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object ||
                    meta.GetProperty("network").GetString() != "mainnet")
                    throw new InvalidDataException("Exact successful mainnet summary envelope required.");
                var units = meta.GetProperty("units");
                if (units.GetProperty("authoritativeMonetary").GetString() != "zatoshi" ||
                    units.GetProperty("authoritativeEncoding").GetString() != "decimal-string" ||
                    units.GetProperty("zatoshiPerZec").GetString() != "100000000")
                    throw new InvalidDataException("Native v1unit contract differs.");
                string freshness = meta.GetProperty("freshness").GetProperty("status").GetString();
                if (freshness is not ("fresh" or "stale" or "unknown"))
                    throw new InvalidDataException("Available observation with explicit freshness required.");
                var item = data[0];
                foreach (string field in new[] { "transactions", "raw", "private_notes", "shielded_balances" })
                    if (item.TryGetProperty(field, out _)) throw new InvalidDataException("Unexpected body/private payload.");
                int returnedHeight = item.GetProperty("height").GetInt32();
                string hash = item.GetProperty("hash").GetString(); Hash(hash);
                if (returnedHeight != height || hash != expectedHash)
                    throw new InvalidDataException("Configured native block identity differs.");
                uint timestamp = item.GetProperty("timestamp").GetUInt32();
                token.ThrowIfCancellationRequested();
                return new Summary(returnedHeight, hash, timestamp, freshness);
            }
            finally { rate.Exit(); }
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            // Network identity precedes selected reads. Heights locate the pinned hashes;
            // an indexer may not silently substitute a different canonical block.
            await ReadSummaryAsync(0, Genesis, token);
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var reference = entries[key];
                var summary = await ReadSummaryAsync(reference.Height, reference.Hash, token);
                var fields = new Dictionary<string, object>
                {
                    ["name"] = "Zcash block " + summary.Height.ToString(CultureInfo.InvariantCulture),
                    ["description"] = "Public Zcash block summary; indexer reported, no independent proof verification.",
                    [AddressMetadata] = summary.Hash, ["ZcashGenesisHash"] = Genesis,
                    ["ZcashBlockHeight"] = summary.Height.ToString(CultureInfo.InvariantCulture),
                    ["ZcashBlockTimestamp"] = summary.Timestamp.ToString(CultureInfo.InvariantCulture),
                    ["ZcashFreshness"] = summary.Freshness,
                    ["ZcashObservationPolicy"] = "indexer-reported-public-block-summary",
                    ["ZcashProofVerified"] = "false"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ZcashOASIS,
                    new NativeRecord(sourceName, key, fields), NativeDataMapping.ForSource(sourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.ZcashOASIS] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var rows = await ReadAsync(Hashes, cancellationToken);
            if (rows.Count != Hashes.Count) throw new InvalidDataException("Configured summaries are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var hash)) return null;
            var rows = await ReadAsync(new[] { hash }, cancellationToken);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Zcash block summaries are read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Zcash block summaries are read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
