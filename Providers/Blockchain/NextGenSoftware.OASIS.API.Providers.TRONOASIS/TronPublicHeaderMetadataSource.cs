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

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>Explicit solidified public TRON headers; never signed storage.</summary>
public sealed class TronPublicHeaderMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "TronBlockHeight";
    private readonly HeadersBackend blockHeights;
    private bool disposed;

    public const string PublicGenesis = "00000000000000001ebf88508a03865c71d452e25f4d51194196a1d22b6653dc";
    public const string PublicEndpoint = "https://api.trongrid.io/walletsolidity/getblock";

    public TronPublicHeaderMetadataSource(HttpClient client, Uri endpoint,
        IEnumerable<string> blockHeights, Guid sourceOwner, bool ownsClient = false)
        : this(new HeadersBackend(client, endpoint, blockHeights, sourceOwner, ownsClient)) { }

    private TronPublicHeaderMetadataSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        blockHeights = backend;
        ProviderName = PType.TRONOASIS.ToString();
        ProviderDescription = "Read-only solidified public TRON headers";
        ProviderType = new EnumValue<PType>(PType.TRONOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 19, 8);
    }

    public PType QueryProviderType => PType.TRONOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Tron storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static TronPublicHeaderMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Tron public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        Unique(root);
        var fields = new[] { "schema", "rpcUrl", "genesisHash", "consistency", "blockHeights", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").GetInt32() != 1 || root.GetProperty("genesisHash").GetString() != PublicGenesis ||
            root.GetProperty("consistency").GetString() != "SolidityNodeSolidified" ||
            root.GetProperty("blockHeights").ValueKind != JsonValueKind.Array ||
            root.GetProperty("blockHeights").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous solidified TRON header configuration required; history/signing fields forbidden.");
        var blockHeights = root.GetProperty("blockHeights").EnumerateArray().Select(row => row.GetString()).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new TronPublicHeaderMetadataSource(client,
                new Uri(root.GetProperty("rpcUrl").GetString(), UriKind.Absolute),
                blockHeights, root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await blockHeights.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Tron header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Tron source is required.");
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
            if (keys.Any(key => !blockHeights.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured block catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await blockHeights.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Tron query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public TRON header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; blockHeights.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public static string ValidateAddress(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A[1-9][0-9]{0,18}\\z") ||
            !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            throw new ArgumentException("Canonical positive native int64 block height required.");
        return value;
    }

    private static string Hash32(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A[0-9a-f]{64}\\z"))
            throw new InvalidDataException("Canonical 32-byte native hash required.");
        return value;
    }

    private static string NonnegativeInt64(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out long number) || number < 0)
            throw new InvalidDataException("Exact nonnegative native Int64 required.");
        return number.ToString(CultureInfo.InvariantCulture);
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

    private sealed class HeadersBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Uri endpoint;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "tron-public-solidified:" + PublicGenesis;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "tron-public-metadata/";

        public HeadersBackend(HttpClient client, Uri endpoint, IEnumerable<string> blockHeights, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            // This explicit public read profile stays outside the wallet registry.
            if (endpoint == null || !endpoint.IsAbsoluteUri || endpoint.AbsoluteUri != PublicEndpoint)
                throw new ArgumentException("Explicit official TRON Mainnet SolidityNode endpoint required.");
            if (client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Anonymous source and nonempty projection owner required.");
            var catalogue = blockHeights?.Take(9).Select(ValidateAddress).ToArray() ?? throw new ArgumentNullException(nameof(blockHeights));
            if (catalogue.Length is < 1 or > 8 || catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length)
                throw new ArgumentException("One to eight unique native block heights required.");
            this.client = client; this.endpoint = endpoint; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(catalogue);
            records = catalogue.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.TRONOASIS, SourceName, key).ToString("N"), key => key);
        }

        private Task<JsonElement> GetNativeAsync(string height, CancellationToken token)
            => SendNativeAsync(endpoint, new { id_or_num = height, detail = false }, token);

        private async Task<JsonElement> SendNativeAsync(Uri uri, object payload, CancellationToken token)
        {
            // POST submits a header-only read, never a transaction or mutation.
            using var request = new HttpRequestMessage(HttpMethod.Post, uri) {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = await stream.ReadAsync(buffer.AsMemory(), token)) != 0)
            {
                if (body.Length + count > 65536) throw new InvalidDataException("Native header response exceeds64KiB.");
                body.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            Unique(document.RootElement);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("Error", out _) || root.TryGetProperty("transactions", out _))
                throw new InvalidDataException("Complete header-only native result required.");
            token.ThrowIfCancellationRequested();
            return root.Clone();
        }

        private async Task<IReadOnlyList<JsonElement>> ReadRangeAsync(string[] keys, CancellationToken token)
        {
            if (keys.Length == 1)
            {
                var point = await GetNativeAsync(keys[0], token);
                return new[] { point };
            }
            long first = long.Parse(keys[0], CultureInfo.InvariantCulture);
            long end = checked(long.Parse(keys[keys.Length - 1], CultureInfo.InvariantCulture) + 1);
            var page = await SendNativeAsync(new Uri("https://api.trongrid.io/walletsolidity/getblockbylimitnext"),
                new { startNum = first, endNum = end }, token);
            if (!page.TryGetProperty("block", out var blocks) || blocks.ValueKind != JsonValueKind.Array || blocks.GetArrayLength() != keys.Length)
                throw new InvalidDataException("Complete exact native range required.");
            var result = blocks.EnumerateArray().ToArray();
            for (int index = 0; index < result.Length; index++)
                if (NonnegativeInt64(result[index].GetProperty("block_header").GetProperty("raw_data").GetProperty("number")) != keys[index])
                    throw new InvalidDataException("Native range contains missing, duplicate or unexpected heights.");
            return result;
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var genesis = await GetNativeAsync("0", token);
            if (Hash32(genesis.GetProperty("blockID").GetString()) != PublicGenesis)
                throw new InvalidDataException("Configured API-reported TRON Mainnet genesis differs.");
            var genesisRaw = genesis.GetProperty("block_header").GetProperty("raw_data");
            if (genesisRaw.ValueKind != JsonValueKind.Object ||
                genesisRaw.TryGetProperty("number", out var genesisNumber) && NonnegativeInt64(genesisNumber) != "0")
                throw new InvalidDataException("Genesis header identity differs.");

            // Combine only adjacent requested heights into exact native ranges.
            // Sparse selections do not fetch intervening records. Int64.MaxValue
            // uses its point primitive because an exclusive range end cannot fit.
            // A failed primitive is never retried or replaced by another route.
            var ordered = keys.Select(key => long.Parse(key, CultureInfo.InvariantCulture)).OrderBy(value => value).ToArray();
            var ranges = new List<List<string>>();
            foreach (long height in ordered)
            {
                if (ranges.Count == 0 || height == long.MaxValue ||
                    long.Parse(ranges[ranges.Count - 1].Last(), CultureInfo.InvariantCulture) != height - 1)
                    ranges.Add(new List<string>());
                ranges[ranges.Count - 1].Add(height.ToString(CultureInfo.InvariantCulture));
            }
            using var gate = new SemaphoreSlim(3);
            var pages = await Task.WhenAll(ranges.Select(async range => {
                await gate.WaitAsync(token);
                try { return await ReadRangeAsync(range.ToArray(), token); }
                finally { gate.Release(); }
            }));
            var byHeight = pages.SelectMany(page => page).ToDictionary(
                row => NonnegativeInt64(row.GetProperty("block_header").GetProperty("raw_data").GetProperty("number")),
                row => row, StringComparer.Ordinal);
            var rows = new List<IHolon>();
            for (int index = 0; index < keys.Count; index++)
            {
                string key = keys[index];
                var native = byHeight[key];
                string blockId = Hash32(native.GetProperty("blockID").GetString());
                var raw = native.GetProperty("block_header").GetProperty("raw_data");
                if (NonnegativeInt64(raw.GetProperty("number")) != key ||
                    ulong.Parse(blockId.Substring(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) != key)
                    throw new InvalidDataException("Requested, header and block ID heights must agree.");
                string witness = raw.GetProperty("witness_address").GetString();
                if (witness == null || !Regex.IsMatch(witness, "\\A41[0-9a-f]{40}\\z"))
                    throw new InvalidDataException("Canonical TRON witness hex address required.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "TRON block " + key,
                    ["description"] = "API-reported solidified header; no independent signature or consensus proof.",
                    [AddressMetadata] = key, ["TronBlockId"] = blockId,
                    ["TronParentHash"] = Hash32(raw.GetProperty("parentHash").GetString()),
                    ["TronTransactionRoot"] = Hash32(raw.GetProperty("txTrieRoot").GetString()),
                    ["TronTimestampMilliseconds"] = NonnegativeInt64(raw.GetProperty("timestamp")),
                    ["TronWitnessAddress"] = witness, ["TronGenesisHash"] = PublicGenesis,
                    ["TronConsistency"] = "SolidityNodeSolidified"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.TRONOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.TRONOASIS] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var rows = await ReadAsync(Addresses, cancellationToken);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured blockHeights are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var blockHeight)) return null;
            var rows = await ReadAsync(new[] { blockHeight }, cancellationToken);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Tron header metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Tron header metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
