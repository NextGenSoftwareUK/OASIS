using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
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

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

/// <summary>Explicit ledger-pinned public Radix resource definitions; never signed storage.</summary>
public sealed class RadixPublicResourceMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "RadixResourceAddress";
    private readonly ResourcesBackend addresses;
    private bool disposed;

    public const int PublicNetworkId = 1;
    public const string PublicEndpoint = "https://mainnet.radixdlt.com/";

    public RadixPublicResourceMetadataSource(HttpClient client, Uri endpoint,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new ResourcesBackend(client, endpoint, addresses, sourceOwner, ownsClient)) { }

    private RadixPublicResourceMetadataSource(ResourcesBackend backend) : base(backend, backend.Prefix)
    {
        addresses = backend;
        ProviderName = PType.RadixOASIS.ToString();
        ProviderDescription = "Read-only ledger-pinned public Radix resource definitions";
        ProviderType = new EnumValue<PType>(PType.RadixOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 67, 8);
    }

    public PType QueryProviderType => PType.RadixOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Radix storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static RadixPublicResourceMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Radix public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        Unique(root);
        var fields = new[] { "schema", "gatewayUrl", "networkId", "consistency", "resources", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").GetInt32() != 1 || root.GetProperty("networkId").GetInt32() != PublicNetworkId ||
            root.GetProperty("consistency").GetString() != "PerQueryLedgerState" ||
            root.GetProperty("resources").ValueKind != JsonValueKind.Array ||
            root.GetProperty("resources").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous ledger-pinned Radix configuration required; history/signing fields forbidden.");
        var addresses = root.GetProperty("resources").EnumerateArray().Select(row => row.GetString()).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new RadixPublicResourceMetadataSource(client,
                new Uri(root.GetProperty("gatewayUrl").GetString(), UriKind.Absolute),
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Radix resource source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Radix source is required.");
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
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured resource catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await addresses.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Radix query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Radix resource query failed; no partial rows.", error); }
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
        ResourceType(value);
        return value;
    }

    private static string ResourceType(string value)
    {
        const string hrp = "resource_rdx";
        const string alphabet = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        if (value == null || value.Length != 67 || !value.StartsWith(hrp + "1", StringComparison.Ordinal))
            throw new ArgumentException("Canonical Mainnet resource address required.");
        var words = value.Substring(hrp.Length + 1).Select(character => alphabet.IndexOf(character)).ToArray();
        if (words.Any(word => word < 0)) throw new ArgumentException("Invalid resource Bech32m alphabet.");
        uint check = 1;
        uint[] generators = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
        foreach (int word in hrp.Select(c => c >> 5).Concat(new[] { 0 }).Concat(hrp.Select(c => c & 31)).Concat(words))
        {
            uint top = check >> 25;
            check = ((check & 0x1ffffff) << 5) ^ (uint)word;
            for (int i = 0; i < 5; i++) if (((top >> i) & 1) != 0) check ^= generators[i];
        }
        if (check != 0x2bc830a3) throw new ArgumentException("Invalid resource Bech32m checksum.");
        // A Radix NodeId is30bytes:48five-bit payload words, no padding.
        int nodeType = (words[0] << 3) | (words[1] >> 2);
        return nodeType switch { 93 => "FungibleResource", 154 => "NonFungibleResource",
            _ => throw new ArgumentException("Resource NodeId entity type required.") };
    }

    public static string ValidateSupply(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A(?:0|[1-9][0-9]{0,39})(?:\\.[0-9]{1,18})?\\z"))
            throw new InvalidDataException("Canonical nonnegative Radix Decimal string required.");
        var pieces = value.Split('.');
        int scale = pieces.Length == 1 ? 0 : pieces[1].Length;
        var unscaled = BigInteger.Parse(string.Concat(pieces), CultureInfo.InvariantCulture) * BigInteger.Pow(10, 18 - scale);
        if (unscaled > (BigInteger.One << 191) - 1)
            throw new InvalidDataException("Radix signed192bit Decimal range exceeded.");
        return value;
    }

    private static string Integer(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out long number) || number < 0)
            throw new InvalidDataException("Exact nonnegative Gateway Int64 required.");
        return number.ToString(CultureInfo.InvariantCulture);
    }

    private static (long Version, string Epoch, string Round, string Timestamp) Ledger(JsonElement value)
    {
        if (value.GetProperty("network").GetString() != "mainnet")
            throw new InvalidDataException("Mainnet ledger context required.");
        long version = long.Parse(Integer(value.GetProperty("state_version")), CultureInfo.InvariantCulture);
        if (version == 0) throw new InvalidDataException("Positive ledger state version required.");
        string timestamp = value.GetProperty("proposer_round_timestamp").GetString();
        if (timestamp == null || !Regex.IsMatch(timestamp, "\\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\\.[0-9]{1,9})?Z\\z") ||
            !DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _))
            throw new InvalidDataException("Valid UTC ledger timestamp required.");
        return (version, Integer(value.GetProperty("epoch")), Integer(value.GetProperty("round")), timestamp);
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

    private sealed class ResourcesBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Uri endpoint;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "radix-public-resources:mainnet";
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "radix-public-metadata/";

        public ResourcesBackend(HttpClient client, Uri endpoint, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            // This explicit public read profile stays outside the wallet registry.
            if (endpoint == null || !endpoint.IsAbsoluteUri || endpoint.AbsoluteUri != PublicEndpoint)
                throw new ArgumentException("Explicit official Radix Mainnet API endpoint required.");
            if (client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Anonymous source and nonempty projection owner required.");
            var catalogue = addresses?.Take(9).Select(ValidateAddress).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (catalogue.Length is < 1 or > 8 || catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length)
                throw new ArgumentException("One to eight unique native resource identities required.");
            this.client = client; this.endpoint = endpoint; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(catalogue);
            records = catalogue.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.RadixOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> GetNativeAsync(string path, object payload, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, path)) {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = await stream.ReadAsync(buffer.AsMemory(), token)) != 0)
            {
                if (body.Length + count > 262144) throw new InvalidDataException("Native response exceeds256KiB.");
                body.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            Unique(document.RootElement);
            token.ThrowIfCancellationRequested();
            return document.RootElement.Clone();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var network = await GetNativeAsync("status/network-configuration", new { }, token);
            if (network.GetProperty("network_name").GetString() != "mainnet" || network.GetProperty("network_id").GetInt32() != PublicNetworkId)
                throw new InvalidDataException("API-reported Mainnet identity differs.");
            var status = await GetNativeAsync("status/gateway-status", new { }, token);
            var context = Ledger(status.GetProperty("ledger_state"));
            string[] labels = { "name", "symbol", "description" };
            var page = await GetNativeAsync("state/entity/details", new { addresses = keys,
                at_ledger_state = new { state_version = context.Version }, opt_ins = new { explicit_metadata = labels } }, token);
            if (Ledger(page.GetProperty("ledger_state")) != context)
                throw new InvalidDataException("Requested complete ledger context was not acknowledged; no unpinned retry.");
            var selected = page.GetProperty("items");
            if (selected.ValueKind != JsonValueKind.Array || selected.GetArrayLength() != keys.Count)
                throw new InvalidDataException("Complete native resource selection required.");
            var byKey = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var resource in selected.EnumerateArray())
            {
                string key = ValidateAddress(resource.GetProperty("address").GetString());
                if (!keys.Contains(key, StringComparer.Ordinal) || !byKey.TryAdd(key, resource))
                    throw new InvalidDataException("Missing, duplicate or unexpected resource identity.");
            }
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var native = byKey[key]; var details = native.GetProperty("details");
                string kind = details.GetProperty("type").GetString();
                if (kind != ResourceType(key)) throw new InvalidDataException("Address and native resource types differ.");
                var supply = details.GetProperty("total_supply");
                if (supply.ValueKind != JsonValueKind.String) throw new InvalidDataException("Native decimal string required.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "Radix resource " + key,
                    ["description"] = "API-reported resource at an acknowledged ledger state; no independent consensus proof.",
                    [AddressMetadata] = key, ["RadixResourceType"] = kind,
                    ["RadixTotalSupply"] = ValidateSupply(supply.GetString()),
                    ["RadixLedgerStateVersion"] = context.Version.ToString(CultureInfo.InvariantCulture),
                    ["RadixEpoch"] = context.Epoch, ["RadixRound"] = context.Round,
                    ["RadixLedgerTimestamp"] = context.Timestamp, ["RadixNetwork"] = "mainnet",
                    ["RadixConsistency"] = "PerQueryLedgerState"
                };
                var metadata = native.GetProperty("explicit_metadata");
                var entries = metadata.GetProperty("items");
                if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() > 3 ||
                    Integer(metadata.GetProperty("total_count")) != entries.GetArrayLength().ToString(CultureInfo.InvariantCulture) ||
                    metadata.TryGetProperty("next_cursor", out var cursor) && cursor.ValueKind != JsonValueKind.Null)
                    throw new InvalidDataException("Complete explicitly selected metadata page required.");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var entry in entries.EnumerateArray())
                {
                    string label = entry.GetProperty("key").GetString();
                    if (!labels.Contains(label, StringComparer.Ordinal) || !seen.Add(label))
                        throw new InvalidDataException("Unexpected or duplicate selected metadata key.");
                    var typed = entry.GetProperty("value").GetProperty("typed");
                    if (typed.GetProperty("type").GetString() != "String" || typed.GetProperty("value").ValueKind != JsonValueKind.String)
                        throw new InvalidDataException("Selected native label must be a string.");
                    string text = typed.GetProperty("value").GetString();
                    if (text.Length > 4096) throw new InvalidDataException("Selected label exceeds text budget.");
                    string updated = Integer(entry.GetProperty("last_updated_at_state_version"));
                    if (long.Parse(updated, CultureInfo.InvariantCulture) > context.Version)
                        throw new InvalidDataException("Metadata is newer than the acknowledged ledger view.");
                    string field = label switch { "name" => "RadixResourceName", "symbol" => "RadixResourceSymbol", _ => "RadixResourceDescription" };
                    fields[field] = text; fields[field + "UpdatedAtStateVersion"] = updated;
                }
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.RadixOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.RadixOASIS] = key; rows.Add(row);
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
            if (!records.TryGetValue(key, out var resourceId)) return null;
            var rows = await ReadAsync(new[] { resourceId }, cancellationToken);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Radix resource metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Radix resource metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
