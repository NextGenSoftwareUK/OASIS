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
using System.Buffers.Binary;
using System.Security.Cryptography;
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

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

/// <summary>Explicit public Bitcoin header representation; never signed storage.</summary>
public sealed class BitcoinPublicHeaderMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "BitcoinBlockHash";
    private readonly HeadersBackend headers;
    private bool disposed;

    public BitcoinPublicHeaderMetadataSource(HttpClient client, Uri rest, string genesisHash,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new HeadersBackend(client, rest, genesisHash, addresses, sourceOwner, ownsClient)) { }

    private BitcoinPublicHeaderMetadataSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        headers = backend;
        ProviderName = PType.BitcoinOASIS.ToString();
        ProviderDescription = "Read-only hash-pinned public Bitcoin headers";
        ProviderType = new EnumValue<PType>(PType.BitcoinOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 64, 8);
    }

    public PType QueryProviderType => PType.BitcoinOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Bitcoin storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static BitcoinPublicHeaderMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Bitcoin public source required.");
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
            throw new ArgumentException("Exact anonymous Bitcoin source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new BitcoinPublicHeaderMetadataSource(client, new Uri(root.GetProperty("rpcUrl").GetString(), UriKind.Absolute),
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Bitcoin header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Bitcoin source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Bitcoin query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Bitcoin header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; headers.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public const string Genesis = "000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f";

    private static void Address(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A[0-9a-f]{64}\\z"))
            throw new InvalidDataException("Canonical lowercase Bitcoin hash required.");
    }

    private static byte[] VerifyHeader(string text, string expectedHash)
    {
        Address(expectedHash);
        if (text == null || !Regex.IsMatch(text, "\\A[0-9a-f]{160}\\z"))
            throw new InvalidDataException("Exactly80canonical header bytes required.");
        var header = Convert.FromHexString(text);
        var digest = SHA256.HashData(SHA256.HashData(header));
        if (Convert.ToHexString(digest.Reverse().ToArray()).ToLowerInvariant() != expectedHash)
            throw new InvalidDataException("Native double-SHA256 header identity differs.");
        uint bits = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(72));
        int exponent = (int)(bits >> 24);
        uint coefficient = bits & 0x7fffff;
        BigInteger target = exponent <= 3 ? coefficient >> (8 * (3 - exponent))
            : (BigInteger)coefficient << (8 * (exponent - 3));
        BigInteger maximum = (BigInteger)0xffff << (8 * (0x1d - 3));
        if ((bits & 0x800000) != 0 || target <= 0 || target > maximum ||
            new BigInteger(digest, isUnsigned: true, isBigEndian: false) > target)
            throw new InvalidDataException("Invalid Bitcoin proof-of-work target or work.");
        return header;
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
        private const string SourceName = "bitcoin-public-headers:" + Genesis;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "bitcoin-public-metadata/";

        public HeadersBackend(HttpClient client, Uri rpc, string genesisHash, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (rpc == null || rpc.Scheme != Uri.UriSchemeHttps || rpc.AbsolutePath != "/api/" ||
                !string.IsNullOrEmpty(rpc.UserInfo) || !string.IsNullOrEmpty(rpc.Query) || !string.IsNullOrEmpty(rpc.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty || genesisHash != Genesis)
                throw new ArgumentException("Explicit anonymous HTTPS Bitcoin source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique block hashes required.");
            foreach (var key in keys) Address(key);
            this.client = client; this.rpc = rpc; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.BitcoinOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<string> GetAsyncNative(string path, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(rpc, path));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var body = new MemoryStream();
            var buffer = new byte[1024];
            int count;
            while ((count = await stream.ReadAsync(buffer.AsMemory(), token)) != 0)
            {
                if (body.Length + count > 4096) throw new InvalidDataException("Native response exceeds4KiB.");
                body.Write(buffer, 0, count);
            }
            token.ThrowIfCancellationRequested();
            return Encoding.UTF8.GetString(body.ToArray());
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            if (await GetAsyncNative("block-height/0", token) != Genesis)
                throw new InvalidDataException("Native Bitcoin genesis differs.");
            VerifyHeader(await GetAsyncNative("block/" + Genesis + "/header", token), Genesis);
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var header = VerifyHeader(await GetAsyncNative("block/" + key + "/header", token), key);
                using var status = JsonDocument.Parse(await GetAsyncNative("block/" + key + "/status", token),
                    new JsonDocumentOptions { MaxDepth = 4 });
                Unique(status.RootElement);
                if (!status.RootElement.TryGetProperty("in_best_chain", out var best) || best.ValueKind != JsonValueKind.True)
                    throw new InvalidDataException("Selected header is not API-reported best-chain history.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "Bitcoin block " + key,
                    ["description"] = "Public Bitcoin header; hash and declared proof of work verified; best chain reported by API.",
                    [AddressMetadata] = key, ["BitcoinGenesisHash"] = Genesis,
                    ["BitcoinParentHash"] = Convert.ToHexString(header.AsSpan(4,32).ToArray().Reverse().ToArray()).ToLowerInvariant(),
                    ["BitcoinMerkleRoot"] = Convert.ToHexString(header.AsSpan(36,32).ToArray().Reverse().ToArray()).ToLowerInvariant(),
                    ["BitcoinTimestamp"] = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(68)).ToString(CultureInfo.InvariantCulture),
                    ["BitcoinBits"] = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(72)).ToString("x8", CultureInfo.InvariantCulture)
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.BitcoinOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.BitcoinOASIS] = key;
                rows.Add(row);
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
            => Task.FromException(new NotSupportedException("Public Bitcoin header metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Bitcoin header metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
