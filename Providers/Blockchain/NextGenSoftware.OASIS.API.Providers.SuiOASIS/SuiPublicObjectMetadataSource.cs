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

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

/// <summary>Explicit checkpoint-scoped public Sui objects; never signed storage.</summary>
public sealed class SuiPublicObjectMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "SuiObjectId";
    private readonly ObjectsBackend objects;
    private bool disposed;

    public const string PublicGenesis = "4btiuiMPvEENsttpZC7CZ53DruC3MAgfznDbASZ7DR6S";
    public const string PublicEndpoint = "https://graphql.mainnet.sui.io/graphql";

    public SuiPublicObjectMetadataSource(HttpClient client, Uri endpoint,
        IEnumerable<string> objects, Guid sourceOwner, bool ownsClient = false)
        : this(new ObjectsBackend(client, endpoint, objects, sourceOwner, ownsClient)) { }

    private SuiPublicObjectMetadataSource(ObjectsBackend backend) : base(backend, backend.Prefix)
    {
        objects = backend;
        ProviderName = PType.SuiOASIS.ToString();
        ProviderDescription = "Read-only checkpoint-scoped public Sui objects";
        ProviderType = new EnumValue<PType>(PType.SuiOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 66, 8);
    }

    public PType QueryProviderType => PType.SuiOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Sui storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static SuiPublicObjectMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Sui public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        Unique(root);
        var fields = new[] { "schema", "graphqlUrl", "genesisDigest", "consistency", "objects", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").GetInt32() != 1 || root.GetProperty("genesisDigest").GetString() != PublicGenesis ||
            root.GetProperty("consistency").GetString() != "PerQueryCheckpoint" ||
            root.GetProperty("objects").ValueKind != JsonValueKind.Array ||
            root.GetProperty("objects").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous checkpoint-scoped Sui configuration required; history/signing fields forbidden.");
        var objects = root.GetProperty("objects").EnumerateArray().Select(row => row.GetString()).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new SuiPublicObjectMetadataSource(client,
                new Uri(root.GetProperty("graphqlUrl").GetString(), UriKind.Absolute),
                objects, root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await objects.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Sui object source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Sui source is required.");
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
            if (keys.Any(key => !objects.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured object catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await objects.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Sui query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Sui object query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; objects.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public static string ValidateAddress(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A0x[0-9a-f]{64}\\z"))
            throw new ArgumentException("Canonical Sui object address required.");
        return value;
    }

    public static string ValidateDigestEncoding(string value)
    {
        const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        if (value == null || value.Length is < 32 or > 44)
            throw new InvalidDataException("Canonical Base58 digest required.");
        BigInteger number = BigInteger.Zero;
        foreach (char character in value)
        {
            int digit = alphabet.IndexOf(character);
            if (digit < 0) throw new InvalidDataException("Invalid Base58 digest character.");
            number = number * 58 + digit;
        }
        int zeros = value.TakeWhile(character => character == '1').Count();
        int width = number.IsZero ? 0 : number.ToByteArray(isUnsigned: true, isBigEndian: true).Length;
        if (zeros + width != 32) throw new InvalidDataException("32-byte digest required.");
        return value;
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

    private sealed class ObjectsBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Uri endpoint;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "sui-public-graphql:" + PublicGenesis;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "sui-public-metadata/";

        public ObjectsBackend(HttpClient client, Uri endpoint, IEnumerable<string> objects, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            // This explicit public read profile stays outside the wallet registry.
            if (endpoint == null || !endpoint.IsAbsoluteUri || endpoint.AbsoluteUri != PublicEndpoint)
                throw new ArgumentException("Explicit official Sui Mainnet GraphQL endpoint required.");
            if (client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Anonymous source and nonempty projection owner required.");
            var catalogue = objects?.Take(9).Select(ValidateAddress).ToArray() ?? throw new ArgumentNullException(nameof(objects));
            if (catalogue.Length is < 1 or > 8 || catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length)
                throw new ArgumentException("One to eight unique native object identities required.");
            this.client = client; this.endpoint = endpoint; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(catalogue);
            records = catalogue.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.SuiOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> GetNativeAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var variables = keys.Select((key, index) => (key, index)).ToDictionary(pair => "k" + pair.index, pair => pair.key);
            string parameters = string.Join(",", variables.Keys.Select(key => "$" + key + ":SuiAddress!"));
            string fields = string.Join(" ", Enumerable.Range(0, keys.Count).Select(index =>
                "r" + index + ":object(address:$k" + index + "){address version digest}"));
            string query = "query(" + parameters + "){chainIdentifier genesis:checkpoint(sequenceNumber:0){digest} view:checkpoint{sequenceNumber digest query{" + fields + "}}}";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) {
                Content = new StringContent(JsonSerializer.Serialize(new { query, variables }), Encoding.UTF8, "application/json") };
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
            if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("errors", out _))
                throw new InvalidDataException("Complete GraphQL response without errors required.");
            token.ThrowIfCancellationRequested();
            return root.GetProperty("data").Clone();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            // All aliases live under this query's explicitly selected checkpoint.
            // Digest encoding checks do not independently verify BCS or consensus.
            var data = await GetNativeAsync(keys, token);
            if (ValidateDigestEncoding(data.GetProperty("chainIdentifier").GetString()) != PublicGenesis ||
                ValidateDigestEncoding(data.GetProperty("genesis").GetProperty("digest").GetString()) != PublicGenesis)
                throw new InvalidDataException("Configured API-reported Mainnet genesis differs.");
            var view = data.GetProperty("view");
            string checkpoint = UInt53(view.GetProperty("sequenceNumber"));
            string checkpointDigest = ValidateDigestEncoding(view.GetProperty("digest").GetString());
            var selected = view.GetProperty("query");
            var aliases = Enumerable.Range(0, keys.Count).Select(index => "r" + index).ToHashSet(StringComparer.Ordinal);
            if (selected.ValueKind != JsonValueKind.Object || selected.EnumerateObject().Count() != keys.Count ||
                selected.EnumerateObject().Any(property => !aliases.Contains(property.Name)))
                throw new InvalidDataException("Complete exact selection aliases required.");
            var rows = new List<IHolon>();
            for (int index = 0; index < keys.Count; index++)
            {
                string key = keys[index];
                var native = selected.GetProperty("r" + index);
                if (native.ValueKind != JsonValueKind.Object || ValidateAddress(native.GetProperty("address").GetString()) != key)
                    throw new InvalidDataException("Selected object missing or identity differs.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "Sui object " + key,
                    ["description"] = "API-reported checkpoint-scoped object metadata; no independent state/consensus proof.",
                    [AddressMetadata] = key, ["SuiObjectVersion"] = UInt53(native.GetProperty("version")),
                    ["SuiObjectDigest"] = ValidateDigestEncoding(native.GetProperty("digest").GetString()),
                    ["SuiCheckpointSequence"] = checkpoint, ["SuiCheckpointDigest"] = checkpointDigest,
                    ["SuiGenesisDigest"] = PublicGenesis, ["SuiConsistency"] = "PerQueryCheckpoint"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.SuiOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.SuiOASIS] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var rows = await ReadAsync(Addresses, cancellationToken);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured objects are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var objectId)) return null;
            var rows = await ReadAsync(new[] { objectId }, cancellationToken);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Sui object metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Sui object metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
