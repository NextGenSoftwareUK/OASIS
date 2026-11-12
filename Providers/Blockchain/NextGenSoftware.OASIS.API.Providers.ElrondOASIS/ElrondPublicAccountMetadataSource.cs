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
using NextGenSoftware.OASIS.API.Core.Helpers.Crypto;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS;

/// <summary>Explicit latest-indexed public MultiversX accounts; never signed storage.</summary>
public sealed class ElrondPublicAccountMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "ElrondAccountAddress";
    private readonly AccountsBackend addresses;
    private bool disposed;

    public const string PublicChainId = "1";
    public const string PublicEndpoint = "https://api.multiversx.com/";

    public ElrondPublicAccountMetadataSource(HttpClient client, Uri endpoint,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new AccountsBackend(client, endpoint, addresses, sourceOwner, ownsClient)) { }

    private ElrondPublicAccountMetadataSource(AccountsBackend backend) : base(backend, backend.Prefix)
    {
        addresses = backend;
        ProviderName = PType.ElrondOASIS.ToString();
        ProviderDescription = "Read-only latest-indexed public MultiversX accounts";
        ProviderType = new EnumValue<PType>(PType.ElrondOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 62, 8);
    }

    public PType QueryProviderType => PType.ElrondOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed MultiversX storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static ElrondPublicAccountMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit MultiversX public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        Unique(root);
        var fields = new[] { "schema", "apiUrl", "chainId", "consistency", "addresses", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").GetInt32() != 1 || root.GetProperty("chainId").GetString() != PublicChainId ||
            root.GetProperty("consistency").GetString() != "LatestIndexed" ||
            root.GetProperty("addresses").ValueKind != JsonValueKind.Array ||
            root.GetProperty("addresses").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous latest-indexed MultiversX configuration required; history/signing fields forbidden.");
        var addresses = root.GetProperty("addresses").EnumerateArray().Select(row => row.GetString()).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new ElrondPublicAccountMetadataSource(client,
                new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                addresses, root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await addresses.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public MultiversX account source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public MultiversX source is required.");
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
            if (keys.Any(key => !addresses.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured account catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await addresses.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "MultiversX query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public MultiversX account query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; addresses.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public static string ValidateAddress(string value)
    {
        if (value == null || value.Length != 62 || value != value.ToLowerInvariant())
            throw new ArgumentException("Canonical lowercase 32-byte erd address required.");
        var decoded = Bech32.Decode(value);
        if (decoded.Hrp != "erd" || decoded.Data.Length != 32 || Bech32.Encode("erd", decoded.Data) != value)
            throw new ArgumentException("Canonical erd address checksum/payload required.");
        return value;
    }

    private static string Balance(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String ||
            !Regex.IsMatch(value.GetString(), "\\A(?:0|[1-9][0-9]{0,4095})\\z"))
            throw new InvalidDataException("Bounded exact decimal balance string required.");
        return value.GetString();
    }

    private static string UInt53(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetUInt64(out ulong number) || number > 9007199254740991UL)
            throw new InvalidDataException("Exact native UInt53 integer required.");
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

    private sealed class AccountsBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Uri endpoint;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "elrond-public-api:" + PublicChainId;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "elrond-public-metadata/";

        public AccountsBackend(HttpClient client, Uri endpoint, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            // This explicit public read profile stays outside the wallet registry.
            if (endpoint == null || !endpoint.IsAbsoluteUri || endpoint.AbsoluteUri != PublicEndpoint)
                throw new ArgumentException("Explicit official MultiversX Mainnet API endpoint required.");
            if (client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Anonymous source and nonempty projection owner required.");
            var catalogue = addresses?.Take(9).Select(ValidateAddress).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (catalogue.Length is < 1 or > 8 || catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length)
                throw new ArgumentException("One to eight unique native account identities required.");
            this.client = client; this.endpoint = endpoint; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(catalogue);
            records = catalogue.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.ElrondOASIS, SourceName, key).ToString("N"), key => key);
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
                if (body.Length + count > 65536) throw new InvalidDataException("Native response exceeds64KiB.");
                body.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            Unique(document.RootElement);
            token.ThrowIfCancellationRequested();
            return document.RootElement.Clone();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var network = await GetNativeAsync("dapp/config", token);
            if (network.GetProperty("id").GetString() != "mainnet" ||
                network.GetProperty("chainId").GetString() != PublicChainId ||
                network.GetProperty("egldDenomination").GetString() != "18")
                throw new InvalidDataException("API-reported Mainnet identity/denomination differs.");
            string path = "accounts?addresses=" + Uri.EscapeDataString(string.Join(",", keys)) +
                "&from=0&size=" + keys.Count.ToString(CultureInfo.InvariantCulture) + "&fields=address%2Cbalance%2Cnonce%2Cshard";
            var selected = await GetNativeAsync(path, token);
            if (selected.ValueKind != JsonValueKind.Array || selected.GetArrayLength() != keys.Count)
                throw new InvalidDataException("Complete exact native account selection required.");
            var byKey = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var account in selected.EnumerateArray())
            {
                string key = ValidateAddress(account.GetProperty("address").GetString());
                if (!keys.Contains(key, StringComparer.Ordinal) || !byKey.TryAdd(key, account))
                    throw new InvalidDataException("Missing, duplicate or unexpected native account identity.");
            }
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var native = byKey[key];
                var shardValue = native.GetProperty("shard");
                if (shardValue.ValueKind != JsonValueKind.Number || !shardValue.TryGetUInt32(out uint shard) ||
                    shard is not (0 or 1 or 2 or uint.MaxValue))
                    throw new InvalidDataException("Mainnet shard/metachain identity required.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "MultiversX account " + key,
                    ["description"] = "API-reported latest indexed account; no pinned state or consensus proof.",
                    [AddressMetadata] = key, ["ElrondBalanceBaseUnits"] = Balance(native.GetProperty("balance")),
                    ["ElrondNonce"] = UInt53(native.GetProperty("nonce")),
                    ["ElrondShard"] = shard.ToString(CultureInfo.InvariantCulture),
                    ["ElrondChainId"] = PublicChainId, ["ElrondDenomination"] = "18",
                    ["ElrondConsistency"] = "LatestIndexed"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ElrondOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.ElrondOASIS] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var rows = await ReadAsync(Addresses, cancellationToken);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured addresses are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var accountId)) return null;
            var rows = await ReadAsync(new[] { accountId }, cancellationToken);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public MultiversX account metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public MultiversX account metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
