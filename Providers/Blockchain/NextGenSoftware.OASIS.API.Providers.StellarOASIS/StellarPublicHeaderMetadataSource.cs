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

namespace NextGenSoftware.OASIS.API.Providers.StellarOASIS;

/// <summary>Explicit public Stellar header representation; never signed storage.</summary>
public sealed class StellarPublicHeaderMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "StellarLedgerSequence";
    private readonly HeadersBackend headers;
    private bool disposed;

    public const string PublicNetwork = "Public Global Stellar Network ; September 2015";
    public sealed record LedgerIdentity(uint Sequence, string Hash);

    public StellarPublicHeaderMetadataSource(HttpClient client, Uri endpoint, string networkPassphrase,
        IEnumerable<LedgerIdentity> ledgers, Guid sourceOwner, bool ownsClient = false)
        : this(new HeadersBackend(client, endpoint, networkPassphrase, ledgers, sourceOwner, ownsClient)) { }

    private StellarPublicHeaderMetadataSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        headers = backend;
        ProviderName = PType.StellarOASIS.ToString();
        ProviderDescription = "Read-only hash-pinned public Stellar headers";
        ProviderType = new EnumValue<PType>(PType.StellarOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 10, 8);
    }

    public PType QueryProviderType => PType.StellarOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Stellar storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static StellarPublicHeaderMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Stellar public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        Unique(root);
        var fields = new[] { "schema", "horizonUrl", "networkPassphrase", "ledgers", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("horizonUrl").ValueKind != JsonValueKind.String || root.GetProperty("networkPassphrase").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("ledgers").ValueKind != JsonValueKind.Array ||
            root.GetProperty("ledgers").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Stellar source schema required; signer fields forbidden.");
        var ledgers = new List<LedgerIdentity>();
        foreach (var row in root.GetProperty("ledgers").EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 2 ||
                !row.TryGetProperty("sequence", out var sequence) || !row.TryGetProperty("hash", out var hash) ||
                sequence.ValueKind != JsonValueKind.Number || !sequence.TryGetUInt32(out uint number) || hash.ValueKind != JsonValueKind.String)
                throw new ArgumentException("Exact native sequence/hash catalogue entry required.");
            ledgers.Add(new(number, hash.GetString()));
        }
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new StellarPublicHeaderMetadataSource(client, new Uri(root.GetProperty("horizonUrl").GetString(), UriKind.Absolute),
                root.GetProperty("networkPassphrase").GetString(), ledgers, root.GetProperty("sourceOwner").GetGuid(), true);
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Stellar header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Stellar source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Stellar query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Stellar header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; headers.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private static uint Sequence(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A[1-9][0-9]{0,9}\\z") ||
            !uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out uint number))
            throw new InvalidDataException("Canonical positive UInt32 ledger sequence required.");
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
        private readonly Uri endpoint;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> hashes;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "stellar-public-headers:" + PublicNetwork;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "stellar-public-metadata/";

        public HeadersBackend(HttpClient client, Uri endpoint, string network, IEnumerable<LedgerIdentity> ledgers, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (endpoint == null || endpoint.Scheme != Uri.UriSchemeHttps || endpoint.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty || network != PublicNetwork)
                throw new ArgumentException("Explicit anonymous HTTPS Stellar public-network source required.");
            var catalogue = ledgers?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(ledgers));
            if (catalogue.Length is < 1 or > 8 || catalogue.Any(row => row == null || row.Sequence == 0 || row.Hash == null ||
                    !Regex.IsMatch(row.Hash, "\\A[0-9a-f]{64}\\z")) || catalogue.Select(row => row.Sequence).Distinct().Count() != catalogue.Length)
                throw new ArgumentException("One to eight unique native sequence/hash identities required.");
            hashes = catalogue.ToDictionary(row => row.Sequence.ToString(CultureInfo.InvariantCulture), row => row.Hash, StringComparer.Ordinal);
            this.client = client; this.endpoint = endpoint; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(hashes.Keys.ToArray());
            records = hashes.Keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.StellarOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> GetNativeAsync(string path, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint, path));
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
            token.ThrowIfCancellationRequested();
            return document.RootElement.Clone();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            if ((await GetNativeAsync("", token)).GetProperty("network_passphrase").GetString() != PublicNetwork)
                throw new InvalidDataException("Explicit Horizon public-network passphrase required.");
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                uint sequence = Sequence(key);
                var native = await GetNativeAsync("ledgers/" + key, token);
                if (native.GetProperty("hash").GetString() != hashes[key] || native.GetProperty("sequence").GetUInt32() != sequence)
                    throw new InvalidDataException("Configured/API ledger identity differs.");
                var header = StellarLedgerHeaderDecoder.Decode(native.GetProperty("header_xdr").GetString(), hashes[key], sequence);
                if (native.GetProperty("prev_hash").GetString() != header.ParentHash || native.GetProperty("protocol_version").GetUInt32() != header.ProtocolVersion)
                    throw new InvalidDataException("API parent/protocol differs from complete native header.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "Stellar ledger " + key,
                    ["description"] = "Public Stellar header; complete XDR/hash verified, network reported by Horizon.",
                    [AddressMetadata] = key, ["StellarLedgerHash"] = header.Hash, ["StellarParentHash"] = header.ParentHash,
                    ["StellarProtocolVersion"] = header.ProtocolVersion.ToString(CultureInfo.InvariantCulture),
                    ["StellarCloseTime"] = header.CloseTime.ToString(CultureInfo.InvariantCulture),
                    ["StellarTotalStroops"] = header.TotalStroops.ToString(CultureInfo.InvariantCulture),
                    ["StellarFeePoolStroops"] = header.FeePool.ToString(CultureInfo.InvariantCulture),
                    ["StellarIdPool"] = header.IdPool.ToString(CultureInfo.InvariantCulture),
                    ["StellarNetworkPassphrase"] = PublicNetwork
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.StellarOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.StellarOASIS] = key; rows.Add(row);
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
            if (!records.TryGetValue(key, out var sequence)) return null;
            var rows = await ReadAsync(new[] { sequence }, cancellationToken);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Stellar header metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Stellar header metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
