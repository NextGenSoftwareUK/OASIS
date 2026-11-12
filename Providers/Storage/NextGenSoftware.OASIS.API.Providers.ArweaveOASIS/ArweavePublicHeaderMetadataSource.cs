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

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS;

/// <summary>Explicit public Arweave header representation; never signed storage.</summary>
public sealed class ArweavePublicHeaderMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "ArweaveBlockHash";
    private readonly HeadersBackend headers;
    private bool disposed;

    public ArweavePublicHeaderMetadataSource(HttpClient client, Uri rest,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new HeadersBackend(client, rest, addresses, sourceOwner, ownsClient)) { }

    private ArweavePublicHeaderMetadataSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        headers = backend;
        ProviderName = PType.ArweaveOASIS.ToString();
        ProviderDescription = "Read-only explicit historical Arweave gateway headers";
        ProviderType = new EnumValue<PType>(PType.ArweaveOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 64, 8);
    }

    public PType QueryProviderType => PType.ArweaveOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Arweave storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static ArweavePublicHeaderMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Arweave public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "restUrl", "network", "blockHashes", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("restUrl").ValueKind != JsonValueKind.String || root.GetProperty("network").ValueKind != JsonValueKind.String ||
            root.GetProperty("network").GetString() != Network ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("blockHashes").ValueKind != JsonValueKind.Array ||
            root.GetProperty("blockHashes").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Arweave source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new ArweavePublicHeaderMetadataSource(client, new Uri(root.GetProperty("restUrl").GetString(), UriKind.Absolute),
                root.GetProperty("blockHashes").EnumerateArray().Select(value => value.GetString()),
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Arweave header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Arweave source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Arweave query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Arweave header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; headers.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public const string PublicEndpoint = "https://arweave.net/";
    public const string Network = "arweave.N.1";
    // This profile deliberately supports the documented historical wire era only.
    public const ulong MinimumHeight = 269510;
    public const ulong MaximumHeightExclusive = 422250;

    private static string Hash(string value)
    {
        if (value == null || !Regex.IsMatch(value, @"\A[A-Za-z0-9_-]{64}\z"))
            throw new InvalidDataException("Canonical unpadded48byte Arweave block ID required.");
        var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/'));
        if (bytes.Length != 48 || Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_') != value)
            throw new InvalidDataException("Noncanonical Arweave block ID.");
        return value;
    }

    private static void Address(string value) => Hash(value);

    private static string Integer(JsonElement value)
    {
        string text = value.GetRawText();
        if (value.ValueKind != JsonValueKind.Number || !Regex.IsMatch(text, @"\A(?:0|[1-9][0-9]{0,19})\z") ||
            !ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            throw new InvalidDataException("Exact nonnegative UInt64 header integer required.");
        return text;
    }

    private static string DecimalInteger(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("Native difficulty must be a decimal string.");
        string text = value.GetString();
        if (text == null || !Regex.IsMatch(text, @"\A(?:0|[1-9][0-9]{0,255})\z"))
            throw new InvalidDataException("Bounded canonical decimal-integer string required.");
        return text;
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
        private const string SourceName = "arweave-public-headers:" + Network;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "arweave-public-metadata/";

        public HeadersBackend(HttpClient client, Uri rpc, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (rpc == null || rpc.AbsoluteUri != PublicEndpoint ||
                !string.IsNullOrEmpty(rpc.UserInfo) || !string.IsNullOrEmpty(rpc.Query) || !string.IsNullOrEmpty(rpc.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous HTTPS Arweave source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique block hashes required.");
            foreach (var key in keys) Address(key);
            this.client = client; this.rpc = rpc; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.ArweaveOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> JsonAsync(string route, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(rpc, route));
            request.Headers.TryAddWithoutValidation("X-Block-Format", "2");
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
            var info = await JsonAsync("info", token);
            if (info.ValueKind != JsonValueKind.Object || info.GetProperty("network").GetString() != Network)
                throw new InvalidDataException("Native Arweave network identity differs.");

            // Fetch only explicit selected keys. Validate the complete set before any
            // paging, including a zero-length page; no chain scan or alternate gateway.
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var header = await JsonAsync("block/hash/" + key, token);
                if (header.ValueKind != JsonValueKind.Object || Hash(header.GetProperty("indep_hash").GetString()) != key)
                    throw new InvalidDataException("Native header does not match the requested block ID.");
                string number = Integer(header.GetProperty("height"));
                ulong height = ulong.Parse(number, CultureInfo.InvariantCulture);
                if (height < MinimumHeight || height >= MaximumHeightExclusive)
                    throw new InvalidDataException("Header is outside the admitted historical wire era.");
                var transactions = header.GetProperty("txs");
                if (transactions.ValueKind != JsonValueKind.Array)
                    throw new InvalidDataException("Native transaction ID array required.");
                var transactionIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var transaction in transactions.EnumerateArray())
                {
                    string id = transaction.GetString();
                    if (id == null || !Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{43}\z") ||
                        !transactionIds.Add(id))
                        throw new InvalidDataException("Unique32byte transaction IDs required.");
                    byte[] decoded = Convert.FromBase64String(id.Replace('-', '+').Replace('_', '/') + "=");
                    if (decoded.Length != 32 || Convert.ToBase64String(decoded).TrimEnd('=').Replace('+', '-').Replace('/', '_') != id)
                        throw new InvalidDataException("Canonical transaction IDs required.");
                }
                var fields = new Dictionary<string, object> {
                    ["name"] = "Arweave block " + number,
                    ["description"] = "Public historical Arweave gateway header; consensus not independently verified.",
                    [AddressMetadata] = key, ["ArweaveNetwork"] = Network,
                    ["ArweaveBlockHeight"] = number,
                    ["ArweaveParentHash"] = Hash(header.GetProperty("previous_block").GetString()),
                    ["ArweaveTimestamp"] = Integer(header.GetProperty("timestamp")),
                    ["ArweaveLastRetarget"] = Integer(header.GetProperty("last_retarget")),
                    ["ArweaveRewardPool"] = Integer(header.GetProperty("reward_pool")),
                    ["ArweaveWeaveSize"] = Integer(header.GetProperty("weave_size")),
                    ["ArweaveBlockSize"] = Integer(header.GetProperty("block_size")),
                    ["ArweaveDifficulty"] = DecimalInteger(header.GetProperty("diff")),
                    ["ArweaveCumulativeDifficulty"] = DecimalInteger(header.GetProperty("cumulative_diff")),
                    ["ArweaveTransactionCount"] = transactionIds.Count.ToString(CultureInfo.InvariantCulture)
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ArweaveOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.ArweaveOASIS] = key; rows.Add(row);
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
            => Task.FromException(new NotSupportedException("Public Arweave header metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Arweave header metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
