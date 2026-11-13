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

namespace NextGenSoftware.OASIS.API.Providers.BlockscoutOASIS;

/// <summary>
/// Anonymous explorer-address observations over a bounded configured catalogue.
/// Never submits transactions or projects private/watchlist data.
/// </summary>
public sealed class BlockscoutPublicAddressMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "BlockscoutAddress";
    private readonly AddressesBackend addressesBackend;
    private bool disposed;

    public BlockscoutPublicAddressMetadataSource(HttpClient client, Uri rest,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new AddressesBackend(client, rest, addresses, sourceOwner, ownsClient)) { }

    private BlockscoutPublicAddressMetadataSource(AddressesBackend backend) : base(backend, backend.Prefix)
    {
        addressesBackend = backend;
        ProviderName = PType.BlockscoutOASIS.ToString();
        ProviderDescription = "Read-only public Blockscout address observations";
        ProviderType = new EnumValue<PType>(PType.BlockscoutOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 42, 8);
    }

    public PType QueryProviderType => PType.BlockscoutOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// explorer API-key settings. Reject unknown fields rather than ignore keys.</summary>
    public static BlockscoutPublicAddressMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Blockscout public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "addresses", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("apiUrl").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("addresses").ValueKind != JsonValueKind.Array ||
            root.GetProperty("addresses").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Blockscout source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new BlockscoutPublicAddressMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                root.GetProperty("addresses").EnumerateArray().Select(value => value.GetString()),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await addressesBackend.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Blockscout address source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Blockscout address source is required.");
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
            if (keys.Any(key => !addressesBackend.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured address catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await addressesBackend.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Blockscout query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Blockscout address query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; addressesBackend.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

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



    private static void Address(string value)
    {
        if (value == null || !Regex.IsMatch(value, @"\A0x[0-9a-f]{40}\z"))
            throw new ArgumentException("Canonical lowercase20byte explorer address required.");
    }

    private static Dictionary<string, object> Decode(JsonElement root, string address)
    {
        string reported = root.GetProperty("hash").GetString();
        if (reported == null || !Regex.IsMatch(reported, @"\A0x[0-9a-fA-F]{40}\z") ||
            !string.Equals(reported, address, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Exact explorer address identity required.");
        bool isContract = root.GetProperty("is_contract").GetBoolean();
        var fields = new Dictionary<string, object>
        {
            [AddressMetadata] = address, ["BlockscoutReportedAddress"] = reported,
            ["BlockscoutIsContract"] = isContract ? "true" : "false",
            ["BlockscoutObservationPolicy"] = "explorer-reported-address-state",
            ["BlockscoutConsensusVerified"] = "false", ["name"] = address,
            ["description"] = $"Blockscout explorer address {address}"
        };
        // A valid but unindexed address can have null balance/update height.
        // Explicit absence is not a zero amount or proof of account existence.
        var balance = root.GetProperty("coin_balance");
        bool balanceObserved = balance.ValueKind != JsonValueKind.Null;
        fields["BlockscoutBalanceObserved"] = balanceObserved ? "true" : "false";
        if (balanceObserved)
        {
            string value = balance.GetString();
            if (value == null || !Regex.IsMatch(value, @"\A(?:0|[1-9][0-9]{0,77})\z"))
                throw new InvalidDataException("Bounded canonical native-unit balance text required.");
            fields["BlockscoutCoinBalance"] = value;
        }
        var block = root.GetProperty("block_number_balance_updated_at");
        bool blockObserved = block.ValueKind != JsonValueKind.Null;
        fields["BlockscoutBalanceBlockObserved"] = blockObserved ? "true" : "false";
        if (blockObserved)
        {
            ulong number = block.GetUInt64();
            string text = number.ToString(CultureInfo.InvariantCulture);
            if (block.GetRawText() != text) throw new InvalidDataException("Canonical integer update block required.");
            fields["BlockscoutBalanceUpdatedAtBlock"] = text;
        }
        var name = root.GetProperty("name");
        bool named = name.ValueKind != JsonValueKind.Null;
        fields["BlockscoutIndexedNameObserved"] = named ? "true" : "false";
        if (named)
        {
            string text = name.GetString();
            if (text == null || Encoding.UTF8.GetByteCount(text) > 256 || text.Any(char.IsControl))
                throw new InvalidDataException("Bounded indexed name text required.");
            fields["BlockscoutIndexedName"] = text;
        }
        // Deliberate allowlist: private_tags, watchlist_names, user metadata,
        // exchange rates and nested token records are never projected.
        return fields;
    }

    private sealed class AddressesBackend : IKeyValueBackend, IDisposable
    {
        private readonly string sourceName;
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "blockscout-public-metadata/";

        public AddressesBackend(HttpClient client, Uri api, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/api/v2/" || api.AbsoluteUri.Length > 256 ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie") ||
                client.DefaultRequestHeaders.Contains("x-api-key") || owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous HTTPS explorer source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique addresses required.");
            foreach (string key in keys) Address(key);
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            // Include the explorer instance in provenance and projected identity.
            // Same-owner addresses on another configured explorer cannot collide.
            sourceName = "blockscout-public-addresses:" + api.AbsoluteUri;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.BlockscoutOASIS, sourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var rows = new List<IHolon>();
            long totalBytes = 0;
            foreach (string address in keys)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(api, "addresses/" + address));
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
                while ((count = await stream.ReadAsync(buffer.AsMemory(), deadline.Token)) != 0)
                {
                    totalBytes += count;
                    if (body.Length + count > 1048576 || totalBytes > 4194304)
                        throw new InvalidDataException("Native response byte budget exceeded.");
                    body.Write(buffer, 0, count);
                }
                using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
                var root = document.RootElement;
                Unique(root);
                if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _) || root.TryGetProperty("message", out _))
                    throw new InvalidDataException("Complete successful explorer address response required.");
                var fields = Decode(root, address);
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.BlockscoutOASIS,
                    new NativeRecord(sourceName, address, fields), NativeDataMapping.ForSource(sourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.BlockscoutOASIS] = address;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return Array.AsReadOnly(rows.ToArray());
        }
        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured addresses are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out var address)) return null;
            var rows = await ReadAsync(new[] { address }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Blockscout address data is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Blockscout address data is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
