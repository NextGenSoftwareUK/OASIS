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

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS;

/// <summary>Explicit public Cardano header representation; never signed storage.</summary>
public sealed class CardanoPublicHeaderMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "CardanoBlockHash";
    private readonly HeadersBackend headers;
    private bool disposed;

    public CardanoPublicHeaderMetadataSource(HttpClient client, Uri rest,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new HeadersBackend(client, rest, addresses, sourceOwner, ownsClient)) { }

    private CardanoPublicHeaderMetadataSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        headers = backend;
        ProviderName = PType.CardanoOASIS.ToString();
        ProviderDescription = "Read-only explicit historical indexed Cardano headers";
        ProviderType = new EnumValue<PType>(PType.CardanoOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 64, 8);
    }

    public PType QueryProviderType => PType.CardanoOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Cardano storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static CardanoPublicHeaderMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Cardano public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "restUrl", "networkMagic", "blockHashes", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("restUrl").ValueKind != JsonValueKind.String || root.GetProperty("networkMagic").ValueKind != JsonValueKind.String ||
            root.GetProperty("networkMagic").GetString() != NetworkMagic ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("blockHashes").ValueKind != JsonValueKind.Array ||
            root.GetProperty("blockHashes").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Cardano source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new CardanoPublicHeaderMetadataSource(client, new Uri(root.GetProperty("restUrl").GetString(), UriKind.Absolute),
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Cardano header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Cardano source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Cardano query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Cardano header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; headers.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public const string PublicEndpoint = "https://api.koios.rest/api/v1/";
    public const string NetworkMagic = "764824073";
    // Explicit demo profile maturity threshold; not an independent finality proof.
    public const ulong MinimumReportedConfirmations = 2160;

    private static string Hash(string value)
    {
        if (value == null || !Regex.IsMatch(value, @"\A[0-9a-f]{64}\z"))
            throw new InvalidDataException("Canonical lowercase32byte Cardano block hash required.");
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

    private static string Lovelace(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null) return null;
        string text = value.GetString();
        if (text == null || !Regex.IsMatch(text, @"\A(?:0|[1-9][0-9]{0,255})\z"))
            throw new InvalidDataException("Exact bounded decimal-integer lovelace string or null required.");
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
        private const string SourceName = "cardano-public-headers:" + NetworkMagic;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "cardano-public-metadata/";

        public HeadersBackend(HttpClient client, Uri rpc, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (rpc == null || rpc.AbsoluteUri != PublicEndpoint ||
                !string.IsNullOrEmpty(rpc.UserInfo) || !string.IsNullOrEmpty(rpc.Query) || !string.IsNullOrEmpty(rpc.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous HTTPS Cardano source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique block hashes required.");
            foreach (var key in keys) Address(key);
            this.client = client; this.rpc = rpc; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.CardanoOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> JsonAsync(string route, object parameters, CancellationToken token)
        {
            using var request = new HttpRequestMessage(parameters == null ? HttpMethod.Get : HttpMethod.Post, new Uri(rpc, route));
            if (parameters != null) request.Content = System.Net.Http.Json.JsonContent.Create(parameters);
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
            var genesis = await JsonAsync("genesis", null, token);
            if (genesis.ValueKind != JsonValueKind.Array || genesis.GetArrayLength() != 1 ||
                genesis[0].GetProperty("networkmagic").GetString() != NetworkMagic ||
                genesis[0].GetProperty("networkid").GetString() != "Mainnet")
                throw new InvalidDataException("Native Cardano Mainnet identity differs.");

            var native = await JsonAsync("block_info", new { _block_hashes = keys }, token);
            if (native.ValueKind != JsonValueKind.Array || native.GetArrayLength() != keys.Count)
                throw new InvalidDataException("Complete selected header set required.");
            var indexed = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var header in native.EnumerateArray())
            {
                string hash = Hash(header.GetProperty("hash").GetString());
                if (!keys.Contains(hash, StringComparer.Ordinal) || !indexed.TryAdd(hash, header))
                    throw new InvalidDataException("Duplicate or unrequested native header.");
            }

            // Koios response ordering is not request ordering. Validate every selected
            // row before returning any page, then restore the captured key order.
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var header = indexed[key];
                string confirmations = Integer(header.GetProperty("num_confirmations"));
                if (ulong.Parse(confirmations, CultureInfo.InvariantCulture) < MinimumReportedConfirmations)
                    throw new InvalidDataException("Selected indexed history does not meet the profile confirmation threshold.");
                string number = Integer(header.GetProperty("block_height"));
                var fields = new Dictionary<string, object> {
                    ["name"] = "Cardano block " + number,
                    ["description"] = "Public historical Cardano header; confirmations reported by Koios, not independently verified.",
                    [AddressMetadata] = key, ["CardanoNetworkMagic"] = NetworkMagic,
                    ["CardanoBlockHeight"] = number,
                    ["CardanoParentHash"] = Hash(header.GetProperty("parent_hash").GetString()),
                    ["CardanoEpoch"] = Integer(header.GetProperty("epoch_no")),
                    ["CardanoAbsoluteSlot"] = Integer(header.GetProperty("abs_slot")),
                    ["CardanoEpochSlot"] = Integer(header.GetProperty("epoch_slot")),
                    ["CardanoBlockSize"] = Integer(header.GetProperty("block_size")),
                    ["CardanoTimestamp"] = Integer(header.GetProperty("block_time")),
                    ["CardanoTransactionCount"] = Integer(header.GetProperty("tx_count")),
                    ["CardanoReportedConfirmations"] = confirmations
                };
                // Absent, null and zero stay distinct; do not manufacture a balance.
                if (header.TryGetProperty("total_output", out var output)) fields["CardanoTotalOutputLovelace"] = Lovelace(output);
                if (header.TryGetProperty("total_fees", out var fees)) fields["CardanoTotalFeesLovelace"] = Lovelace(fees);
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.CardanoOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.CardanoOASIS] = key; rows.Add(row);
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
            => Task.FromException(new NotSupportedException("Public Cardano header metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Cardano header metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
