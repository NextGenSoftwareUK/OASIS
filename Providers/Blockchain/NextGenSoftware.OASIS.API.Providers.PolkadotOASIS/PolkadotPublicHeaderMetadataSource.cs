extern alias BouncyCastleCryptography;
using Blake2bDigest = BouncyCastleCryptography::Org.BouncyCastle.Crypto.Digests.Blake2bDigest;
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

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

/// <summary>Explicit public Polkadot header representation; never signed storage.</summary>
public sealed class PolkadotPublicHeaderMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "PolkadotBlockHash";
    private readonly HeadersBackend headers;
    private bool disposed;

    public PolkadotPublicHeaderMetadataSource(HttpClient client, Uri rest, string genesisHash,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new HeadersBackend(client, rest, genesisHash, addresses, sourceOwner, ownsClient)) { }

    private PolkadotPublicHeaderMetadataSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        headers = backend;
        ProviderName = PType.PolkadotOASIS.ToString();
        ProviderDescription = "Read-only height-pinned public Polkadot headers";
        ProviderType = new EnumValue<PType>(PType.PolkadotOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 66, 8);
    }

    public PType QueryProviderType => PType.PolkadotOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Polkadot storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static PolkadotPublicHeaderMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Polkadot public source required.");
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
            throw new ArgumentException("Exact anonymous Polkadot source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new PolkadotPublicHeaderMetadataSource(client, new Uri(root.GetProperty("rpcUrl").GetString(), UriKind.Absolute),
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Polkadot account source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Polkadot source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Polkadot query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Polkadot account query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; headers.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public const string Genesis = "0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3";

    private static byte[] HashBytes(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A0x[0-9a-f]{64}\\z"))
            throw new InvalidDataException("Canonical lowercase32byte hash required.");
        return Convert.FromHexString(value.Substring(2));
    }

    private static void Address(string value) => HashBytes(value);

    private static uint Number(string text)
    {
        if (text == null || !Regex.IsMatch(text, "\\A0x(?:0|[1-9a-f][0-9a-f]{0,7})\\z"))
            throw new InvalidDataException("Canonical UInt32 header number required.");
        return uint.Parse(text.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
    }

    private static byte[] Compact(uint value)
    {
        if (value < 64) return new[] { (byte)(value << 2) };
        if (value < 16384) return new[] { (byte)((value << 2) | 1), (byte)(value >> 6) };
        if (value < 1u << 30)
        {
            uint encoded = (value << 2) | 2;
            return new[] { (byte)encoded, (byte)(encoded >> 8), (byte)(encoded >> 16), (byte)(encoded >> 24) };
        }
        return new[] { (byte)3, (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
    }

    private static uint VerifyHeader(JsonElement header, string expectedHash)
    {
        string[] fields = { "parentHash", "number", "stateRoot", "extrinsicsRoot", "digest" };
        if (header.ValueKind != JsonValueKind.Object || header.EnumerateObject().Count() != fields.Length ||
            header.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)))
            throw new InvalidDataException("Exact native header schema required.");
        using var encoded = new MemoryStream();
        encoded.Write(HashBytes(header.GetProperty("parentHash").GetString()));
        uint number = Number(header.GetProperty("number").GetString());
        encoded.Write(Compact(number));
        encoded.Write(HashBytes(header.GetProperty("stateRoot").GetString()));
        encoded.Write(HashBytes(header.GetProperty("extrinsicsRoot").GetString()));
        var digest = header.GetProperty("digest");
        if (digest.ValueKind != JsonValueKind.Object || digest.EnumerateObject().Count() != 1 ||
            !digest.TryGetProperty("logs", out var logs) || logs.ValueKind != JsonValueKind.Array || logs.GetArrayLength() > 64)
            throw new InvalidDataException("Bounded exact digest schema required.");
        encoded.Write(Compact((uint)logs.GetArrayLength()));
        foreach (var log in logs.EnumerateArray())
        {
            string text = log.GetString();
            if (text == null || text.Length > 131074 || !Regex.IsMatch(text, "\\A0x(?:[0-9a-f]{2})+\\z"))
                throw new InvalidDataException("Canonical bounded SCALE digest bytes required.");
            encoded.Write(Convert.FromHexString(text.Substring(2)));
        }
        var bytes = encoded.ToArray(); var hasher = new Blake2bDigest(256); var hash = new byte[32];
        hasher.BlockUpdate(bytes, 0, bytes.Length); hasher.DoFinal(hash, 0);
        if (!hash.SequenceEqual(HashBytes(expectedHash))) throw new InvalidDataException("Native header content hash differs.");
        return number;
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
        private const string SourceName = "polkadot-public-headers:" + Genesis;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "polkadot-public-metadata/";

        public HeadersBackend(HttpClient client, Uri rpc, string genesisHash, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (rpc == null || rpc.Scheme != Uri.UriSchemeHttps || rpc.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(rpc.UserInfo) || !string.IsNullOrEmpty(rpc.Query) || !string.IsNullOrEmpty(rpc.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty || genesisHash != Genesis)
                throw new ArgumentException("Explicit anonymous HTTPS Polkadot source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique block hashes required.");
            foreach (var key in keys) Address(key);
            this.client = client; this.rpc = rpc; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.PolkadotOASIS, SourceName, key).ToString("N"), key => key);
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
            if ((await RpcAsync("chain_getBlockHash", new object[] { 0 }, token)).GetString() != Genesis)
                throw new InvalidDataException("Native Polkadot genesis differs.");
            string finalized = (await RpcAsync("chain_getFinalizedHead", Array.Empty<object>(), token)).GetString();
            var finalHeader = await RpcAsync("chain_getHeader", new object[] { finalized }, token);
            uint finalNumber = VerifyHeader(finalHeader, finalized);
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var header = await RpcAsync("chain_getHeader", new object[] { key }, token);
                uint number = VerifyHeader(header, key);
                if (number > finalNumber || (await RpcAsync("chain_getBlockHash", new object[] { number }, token)).GetString() != key)
                    throw new InvalidDataException("Selected header is not RPC-reported canonical finalized history.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "Polkadot block " + number.ToString(CultureInfo.InvariantCulture),
                    ["description"] = "Public Polkadot header; content hash verified, finality reported by RPC.",
                    [AddressMetadata] = key, ["PolkadotGenesisHash"] = Genesis,
                    ["PolkadotBlockNumber"] = number.ToString(CultureInfo.InvariantCulture),
                    ["PolkadotParentHash"] = header.GetProperty("parentHash").GetString(),
                    ["PolkadotStateRoot"] = header.GetProperty("stateRoot").GetString(),
                    ["PolkadotExtrinsicsRoot"] = header.GetProperty("extrinsicsRoot").GetString(),
                    ["PolkadotReportedFinalizedHash"] = finalized,
                    ["PolkadotReportedFinalizedNumber"] = finalNumber.ToString(CultureInfo.InvariantCulture)
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.PolkadotOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.PolkadotOASIS] = key; rows.Add(row);
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
            => Task.FromException(new NotSupportedException("Public Polkadot header metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Polkadot header metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
