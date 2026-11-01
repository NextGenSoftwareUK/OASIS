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

namespace NextGenSoftware.OASIS.API.Providers.XRPLOASIS;

/// <summary>Explicit public Xrpl header representation; never signed storage.</summary>
public sealed class XrplPublicHeaderMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "XRPLLedgerHash";
    private readonly HeadersBackend headers;
    private bool disposed;

    public XrplPublicHeaderMetadataSource(HttpClient client, Uri rest, int networkId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new HeadersBackend(client, rest, networkId, addresses, sourceOwner, ownsClient)) { }

    private XrplPublicHeaderMetadataSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        headers = backend;
        ProviderName = PType.XRPLOASIS.ToString();
        ProviderDescription = "Read-only hash-pinned public Xrpl headers";
        ProviderType = new EnumValue<PType>(PType.XRPLOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 64, 8);
    }

    public PType QueryProviderType => PType.XRPLOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Xrpl storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static XrplPublicHeaderMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Xrpl public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "rpcUrl", "networkId", "ledgerHashes", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("rpcUrl").ValueKind != JsonValueKind.String || root.GetProperty("networkId").ValueKind != JsonValueKind.Number ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("ledgerHashes").ValueKind != JsonValueKind.Array ||
            root.GetProperty("ledgerHashes").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Xrpl source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new XrplPublicHeaderMetadataSource(client, new Uri(root.GetProperty("rpcUrl").GetString(), UriKind.Absolute),
                root.GetProperty("networkId").GetInt32(), root.GetProperty("ledgerHashes").EnumerateArray().Select(value => value.GetString()),
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Xrpl header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Xrpl source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Xrpl query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Xrpl header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; headers.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private static byte[] HashBytes(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A[0-9A-F]{64}\\z"))
            throw new InvalidDataException("Canonical uppercase XRPL hash required.");
        return Convert.FromHexString(value);
    }

    private static void Address(string value) => HashBytes(value);

    private static ulong Unsigned(JsonElement value, int width, bool decimalString = false)
    {
        string text = value.ValueKind == JsonValueKind.Number ? value.GetRawText()
            : decimalString && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (text == null || !Regex.IsMatch(text, "\\A(?:0|[1-9][0-9]*)\\z") ||
            !ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ulong number) ||
            (width < 8 && number >= (1UL << (width * 8))))
            throw new InvalidDataException("Bounded native unsigned header field required.");
        return number;
    }

    private static JsonElement VerifyHeader(JsonElement result, string expectedHash)
    {
        Address(expectedHash);
        var header = result.GetProperty("ledger");
        if (result.GetProperty("validated").ValueKind != JsonValueKind.True ||
            header.GetProperty("closed").ValueKind != JsonValueKind.True ||
            result.GetProperty("ledger_hash").GetString() != expectedHash ||
            header.GetProperty("ledger_hash").GetString() != expectedHash ||
            Unsigned(result.GetProperty("ledger_index"), 4, true) != Unsigned(header.GetProperty("ledger_index"), 4, true))
            throw new InvalidDataException("Closed validated header identity/sequence required.");
        using var bytes = new MemoryStream();
        bytes.Write(new byte[] { 0x4c, 0x57, 0x52, 0 });
        void Integer(string field, int width, bool text = false)
        {
            var buffer = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(buffer, Unsigned(header.GetProperty(field), width, text));
            bytes.Write(buffer, 8 - width, width);
        }
        // XRPLF LedgerHeader.cpp: fixed-width big-endian serialization, SHA512Half.
        Integer("ledger_index", 4, true);
        if (header.GetProperty("total_coins").ValueKind != JsonValueKind.String)
            throw new InvalidDataException("Native total_coins decimal string required.");
        Integer("total_coins", 8, true);
        foreach (string field in new[] { "parent_hash", "transaction_hash", "account_hash" })
            bytes.Write(HashBytes(header.GetProperty(field).GetString()));
        Integer("parent_close_time", 4);
        Integer("close_time", 4);
        Integer("close_time_resolution", 1);
        Integer("close_flags", 1);
        string observed = Convert.ToHexString(SHA512.HashData(bytes.ToArray()).AsSpan(0, 32));
        if (observed != expectedHash) throw new InvalidDataException("Native SHA512Half header identity differs.");
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
        private const string SourceName = "xrpl-public-headers:network:0";
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "xrpl-public-metadata/";

        public HeadersBackend(HttpClient client, Uri rpc, int networkId, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (rpc == null || rpc.Scheme != Uri.UriSchemeHttps || rpc.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(rpc.UserInfo) || !string.IsNullOrEmpty(rpc.Query) || !string.IsNullOrEmpty(rpc.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty || networkId != 0)
                throw new ArgumentException("Explicit anonymous HTTPS Xrpl source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique block hashes required.");
            foreach (var key in keys) Address(key);
            this.client = client; this.rpc = rpc; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.XRPLOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> RpcAsync(string method, object parameters, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, rpc) {
                Content = new StringContent(JsonSerializer.Serialize(new { method, @params = new[] { parameters } }), Encoding.UTF8, "application/json") };
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
            Unique(document.RootElement);
            var root = document.RootElement;
            var result = root.GetProperty("result");
            if (root.TryGetProperty("error", out _) || result.TryGetProperty("error", out _) ||
                result.GetProperty("status").GetString() != "success")
                throw new InvalidDataException("XRPL RPC error; no partial rows.");
            token.ThrowIfCancellationRequested();
            return result.Clone();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var info = (await RpcAsync("server_info", new { }, token)).GetProperty("info");
            if (info.GetProperty("network_id").ValueKind != JsonValueKind.Number || info.GetProperty("network_id").GetInt32() != 0)
                throw new InvalidDataException("Explicit API-reported XRPL Mainnet network0 required.");
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var result = await RpcAsync("ledger", new { ledger_hash = key, transactions = false, expand = false }, token);
                var header = VerifyHeader(result, key);
                var fields = new Dictionary<string, object> {
                    ["name"] = "XRPL ledger " + Unsigned(header.GetProperty("ledger_index"), 4, true).ToString(CultureInfo.InvariantCulture),
                    ["description"] = "Public XRPL ledger header; SHA512Half checked, validation and Mainnet reported by API.",
                    [AddressMetadata] = key, ["XRPLNetworkId"] = "0",
                    ["XRPLLedgerIndex"] = Unsigned(header.GetProperty("ledger_index"), 4, true).ToString(CultureInfo.InvariantCulture),
                    ["XRPLParentHash"] = header.GetProperty("parent_hash").GetString(),
                    ["XRPLAccountHash"] = header.GetProperty("account_hash").GetString(),
                    ["XRPLTransactionHash"] = header.GetProperty("transaction_hash").GetString(),
                    ["XRPLTotalDrops"] = Unsigned(header.GetProperty("total_coins"), 8, true).ToString(CultureInfo.InvariantCulture),
                    ["XRPLCloseTime"] = Unsigned(header.GetProperty("close_time"), 4).ToString(CultureInfo.InvariantCulture)
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.XRPLOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.XRPLOASIS] = key; rows.Add(row);
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
            => Task.FromException(new NotSupportedException("Public Xrpl header metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Xrpl header metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
