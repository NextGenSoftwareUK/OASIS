using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
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

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

/// <summary>Explicit public Starknet header representation; never signed storage.</summary>
public sealed class StarknetPublicHeaderMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "StarknetBlockHash";
    private readonly HeadersBackend headers;
    private bool disposed;

    public StarknetPublicHeaderMetadataSource(HttpClient client, Uri rest, string genesisHash,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new HeadersBackend(client, rest, genesisHash, addresses, sourceOwner, ownsClient)) { }

    private StarknetPublicHeaderMetadataSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        headers = backend;
        ProviderName = PType.StarknetOASIS.ToString();
        ProviderDescription = "Read-only explicit accepted public Starknet headers";
        ProviderType = new EnumValue<PType>(PType.StarknetOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 65, 8);
    }

    public PType QueryProviderType => PType.StarknetOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Starknet storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static StarknetPublicHeaderMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Starknet public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "rpcUrl", "genesisHash", "blockHashes", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("rpcUrl").ValueKind != JsonValueKind.String || root.GetProperty("genesisHash").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("blockHashes").ValueKind != JsonValueKind.Array ||
            root.GetProperty("blockHashes").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Starknet source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new StarknetPublicHeaderMetadataSource(client, new Uri(root.GetProperty("rpcUrl").GetString(), UriKind.Absolute),
                root.GetProperty("genesisHash").GetString(), root.GetProperty("blockHashes").EnumerateArray().Select(value => value.GetString()),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Starknet header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Starknet source is required.");
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
            if (keys.Any(key => !headers.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured header catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await headers.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Starknet query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Starknet header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; headers.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public const string PublicEndpoint = "https://starknet-rpc.publicnode.com/";
    public const string ChainId = "0x534e5f4d41494e";
    public const string RpcVersion = "0.10.3";
    public const string Genesis = "0x47c3637b57c2b079b93c61539950c17e868a28f46cdef28f88521067f21e943";
    private static readonly BigInteger FieldPrime = (BigInteger.One << 251) + 17 * (BigInteger.One << 192) + 1;

    // Preserve exact hexadecimal field elements: no floating-point, decimal or signed hex parsing.
    private static string Felt(string value)
    {
        if (value == null || !Regex.IsMatch(value, @"\A0x(?:0|[1-9a-f][0-9a-f]{0,62})\z") ||
            BigInteger.Parse("0" + value.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture) >= FieldPrime)
            throw new InvalidDataException("Canonical Starknet field element required.");
        return value;
    }

    private static void Address(string value) => Felt(value);

    private static string Integer(JsonElement value)
    {
        string text = value.GetRawText();
        if (value.ValueKind != JsonValueKind.Number || !Regex.IsMatch(text, @"\A(?:0|[1-9][0-9]{0,19})\z") ||
            !ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            throw new InvalidDataException("Exact nonnegative UInt64 header integer required.");
        return text;
    }

    private static string VerifyHeader(JsonElement header, string expectedHash)
    {
        if (header.ValueKind != JsonValueKind.Object ||
            header.GetProperty("status").GetString() != "ACCEPTED_ON_L1" ||
            Felt(header.GetProperty("block_hash").GetString()) != expectedHash)
            throw new InvalidDataException("Selected header identity or RPC-reported L1 acceptance differs.");
        Felt(header.GetProperty("parent_hash").GetString());
        Felt(header.GetProperty("new_root").GetString());
        Felt(header.GetProperty("sequencer_address").GetString());
        Integer(header.GetProperty("timestamp"));
        return Integer(header.GetProperty("block_number"));
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
        private readonly Uri rpc;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "starknet-public-headers:" + Genesis;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "starknet-public-metadata/";

        public HeadersBackend(HttpClient client, Uri rpc, string genesisHash, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (rpc == null || rpc.AbsoluteUri != PublicEndpoint ||
                !string.IsNullOrEmpty(rpc.UserInfo) || !string.IsNullOrEmpty(rpc.Query) || !string.IsNullOrEmpty(rpc.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty || genesisHash != Genesis)
                throw new ArgumentException("Explicit anonymous HTTPS Starknet source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique block hashes required.");
            foreach (var key in keys) Address(key);
            this.client = client; this.rpc = rpc; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.StarknetOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> RpcAsync(string method, object[] parameters, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, rpc) {
                Content = System.Net.Http.Json.JsonContent.Create(new { jsonrpc = "2.0", id = 1, method, @params = parameters }) };
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
            Unique(document.RootElement); var root = document.RootElement;
            if (root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != 1 || root.TryGetProperty("error", out _))
                throw new InvalidDataException("RPC envelope identity/error differs.");
            token.ThrowIfCancellationRequested();
            return root.GetProperty("result").Clone();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            if ((await RpcAsync("starknet_specVersion", Array.Empty<object>(), token)).GetString() != RpcVersion ||
                (await RpcAsync("starknet_chainId", Array.Empty<object>(), token)).GetString() != ChainId)
                throw new InvalidDataException("Native Starknet protocol or Mainnet chain differs.");
            var genesis = await RpcAsync("starknet_getBlockWithTxHashes", new object[] { new { block_number = 0 } }, token);
            if (VerifyHeader(genesis, Genesis) != "0")
                throw new InvalidDataException("Native Starknet genesis number differs.");
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var header = await RpcAsync("starknet_getBlockWithTxHashes", new object[] { new { block_hash = key } }, token);
                string number = VerifyHeader(header, key);
                var fields = new Dictionary<string, object> {
                    ["name"] = "Starknet block " + number,
                    ["description"] = "Public Starknet header; L1 acceptance reported by RPC, not independently verified.",
                    [AddressMetadata] = key, ["StarknetGenesisHash"] = Genesis,
                    ["StarknetChainId"] = ChainId, ["StarknetRpcVersion"] = RpcVersion,
                    ["StarknetBlockNumber"] = number,
                    ["StarknetParentHash"] = Felt(header.GetProperty("parent_hash").GetString()),
                    ["StarknetStateRoot"] = Felt(header.GetProperty("new_root").GetString()),
                    ["StarknetSequencerAddress"] = Felt(header.GetProperty("sequencer_address").GetString()),
                    ["StarknetTimestamp"] = Integer(header.GetProperty("timestamp")),
                    ["StarknetReportedStatus"] = "ACCEPTED_ON_L1"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.StarknetOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.StarknetOASIS] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var rows = await ReadAsync(Addresses, cancellationToken);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured headers are missing.");
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
            => Task.FromException(new NotSupportedException("Public Starknet header metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Starknet header metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
