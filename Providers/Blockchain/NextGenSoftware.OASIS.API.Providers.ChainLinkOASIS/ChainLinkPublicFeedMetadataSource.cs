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

namespace NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;

/// <summary>
/// Anonymous ChainLink AggregatorV3 feed reads on Ethereum mainnet. Each query
/// resolves an RPC-reported finalized block and binds every call to its hash.
/// This profile cannot sign transactions or become canonical storage authority.
/// </summary>
public sealed class ChainLinkPublicFeedMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public sealed record Feed(string Address, string Name, int Decimals);

    public const string MainnetChainId = "0x1";
    public const string AddressMetadata = "ChainLinkFeedAddress";
    private readonly FeedsBackend feeds;
    private bool disposed;

    public ChainLinkPublicFeedMetadataSource(HttpClient client, Uri rest, string chainId,
        IEnumerable<Feed> definitions, Guid sourceOwner, bool ownsClient = false)
        : this(new FeedsBackend(client, rest, chainId, definitions, sourceOwner, ownsClient)) { }

    private ChainLinkPublicFeedMetadataSource(FeedsBackend backend) : base(backend, backend.Prefix)
    {
        feeds = backend;
        ProviderName = PType.ChainLinkOASIS.ToString();
        ProviderDescription = "Read-only ChainLink feed metadata at RPC-reported finalized Ethereum blocks";
        ProviderType = new EnumValue<PType>(PType.ChainLinkOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 42, 8);
    }

    public PType QueryProviderType => PType.ChainLinkOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed ChainLink storage settings. Reject unknown fields rather than ignore keys.</summary>
    private static Feed ParseFeed(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 3 ||
            value.EnumerateObject().Any(p => p.Name is not ("address" or "name" or "decimals")))
            throw new ArgumentException("Exact anonymous feed descriptor required.");
        return new Feed(value.GetProperty("address").GetString(), value.GetProperty("name").GetString(),
            value.GetProperty("decimals").GetInt32());
    }

    public static ChainLinkPublicFeedMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit ChainLink public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "rpcUrl", "chainId", "feeds", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("rpcUrl").ValueKind != JsonValueKind.String || root.GetProperty("chainId").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("feeds").ValueKind != JsonValueKind.Array ||
            root.GetProperty("feeds").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous ChainLink source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new ChainLinkPublicFeedMetadataSource(client, new Uri(root.GetProperty("rpcUrl").GetString(), UriKind.Absolute),
                root.GetProperty("chainId").GetString(), root.GetProperty("feeds").EnumerateArray().Select(ParseFeed),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await feeds.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public ChainLink feed source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public ChainLink source is required.");
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
            if (keys.Any(key => !feeds.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured feed catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await feeds.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "ChainLink query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public ChainLink feed query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; feeds.Dispose();
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



    private static string WordHex(string encoded, int words)
    {
        if (encoded == null || !Regex.IsMatch(encoded, "\\A0x[0-9a-f]{" + (words * 64) + "}\\z"))
            throw new InvalidDataException("Exact canonical ABI words required.");
        return encoded.Substring(2);
    }

    private static BigInteger Unsigned(string word)
        => BigInteger.Parse("0" + word, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);

    private static BigInteger Quantity(JsonElement value)
    {
        string text = value.GetString();
        if (text == null || !Regex.IsMatch(text, "\\A0x(?:0|[1-9a-f][0-9a-f]{0,63})\\z"))
            throw new InvalidDataException("Canonical UInt256 quantity required.");
        return Unsigned(text.Substring(2));
    }

    private static string Description(JsonElement result)
    {
        string value = result.GetString();
        if (value == null || value.Length < 130 || value.Length > 386 ||
            !Regex.IsMatch(value, "\\A0x[0-9a-f]+\\z") || (value.Length - 2) % 64 != 0)
            throw new InvalidDataException("Bounded canonical ABI string required.");
        string hex = value.Substring(2);
        BigInteger count = Unsigned(hex.Substring(64, 64));
        if (Unsigned(hex.Substring(0, 64)) != 32 || count < 1 || count > 128 ||
            hex.Length != 128 + (((int)count + 31) / 32 * 64))
            throw new InvalidDataException("Complete ABI string with canonical offset required.");
        byte[] bytes = Convert.FromHexString(hex.Substring(128));
        if (bytes.Skip((int)count).Any(b => b != 0)) throw new InvalidDataException("ABI string padding differs.");
        return new UTF8Encoding(false, true).GetString(bytes, 0, (int)count);
    }

    private sealed class FeedsBackend : IKeyValueBackend, IDisposable
    {
        private const string Genesis = "0xd4e56740f876aef8c010b86a40d5f56745a118d0906a34e69aec8c0db1cb8fa3";
        private const string SourceName = "chainlink-public-feeds:ethereum:1";
        private readonly HttpClient client;
        private readonly Uri rpc;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, Feed> definitions;
        private readonly Dictionary<string, string> records;
        private long nextId;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "chainlink-public-metadata/";

        public FeedsBackend(HttpClient client, Uri rpc, string chainId, IEnumerable<Feed> feeds, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (rpc == null || rpc.Scheme != Uri.UriSchemeHttps || rpc.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(rpc.UserInfo) || !string.IsNullOrEmpty(rpc.Query) || !string.IsNullOrEmpty(rpc.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty || chainId != MainnetChainId)
                throw new ArgumentException("Explicit anonymous HTTPS Ethereum mainnet feed source required.");
            var rows = feeds?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(feeds));
            if (rows.Length is < 1 or > 8 || rows.Any(f => f == null || f.Address == null ||
                    !Regex.IsMatch(f.Address, "\\A0x[0-9a-f]{40}\\z") || f.Address == "0x" + new string('0', 40) ||
                    string.IsNullOrEmpty(f.Name) || f.Name.Length > 128 || f.Name.Any(c => c < 32 || c > 126) ||
                    f.Decimals is < 0 or > 255) || rows.Select(f => f.Address).Distinct(StringComparer.Ordinal).Count() != rows.Length)
                throw new ArgumentException("One to eight unique canonical feed descriptors required.");
            this.client = client; this.rpc = rpc; this.owner = owner; this.ownsClient = ownsClient;
            definitions = rows.ToDictionary(f => f.Address, StringComparer.Ordinal);
            Addresses = Array.AsReadOnly(rows.Select(f => f.Address).ToArray());
            records = rows.ToDictionary(f => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.ChainLinkOASIS, SourceName, f.Address).ToString("N"), f => f.Address);
        }

        private async Task<JsonElement> RpcAsync(string method, object[] parameters, CancellationToken token)
        {
            long id = Interlocked.Increment(ref nextId);
            using var request = new HttpRequestMessage(HttpMethod.Post, rpc) {
                Content = new StringContent(JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters }), Encoding.UTF8, "application/json") };
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
            var root = document.RootElement;
            Unique(root);
            if (root.GetProperty("jsonrpc").GetString() != "2.0" ||
                root.GetProperty("id").ValueKind != JsonValueKind.Number || root.GetProperty("id").GetInt64() != id ||
                root.TryGetProperty("error", out _) || !root.TryGetProperty("result", out var result) ||
                result.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                throw new InvalidDataException("Native RPC envelope/result rejected.");
            token.ThrowIfCancellationRequested();
            return result.Clone();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            if ((await RpcAsync("eth_chainId", Array.Empty<object>(), token)).GetString() != MainnetChainId)
                throw new InvalidDataException("Ethereum mainnet chain identity differs.");
            var genesis = await RpcAsync("eth_getBlockByNumber", new object[] { "0x0", false }, token);
            if (genesis.GetProperty("number").GetString() != "0x0" || genesis.GetProperty("hash").GetString() != Genesis)
                throw new InvalidDataException("Ethereum mainnet genesis differs.");
            var header = await RpcAsync("eth_getBlockByNumber", new object[] { "finalized", false }, token);
            string hash = header.GetProperty("hash").GetString();
            if (hash == null || !Regex.IsMatch(hash, "\\A0x[0-9a-f]{64}\\z"))
                throw new InvalidDataException("Canonical finalized block hash required.");
            string blockNumber = Quantity(header.GetProperty("number")).ToString(CultureInfo.InvariantCulture);
            BigInteger blockTime = Quantity(header.GetProperty("timestamp"));
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                Feed feed = definitions[key];
                async Task<JsonElement> Call(string selector) => await RpcAsync("eth_call", new object[] {
                    new { to = key, data = selector }, new { blockHash = hash, requireCanonical = true } }, token);

                string name = Description(await Call("0x7284e416"));
                if (name != feed.Name) throw new InvalidDataException("Native feed description differs from explicit catalogue.");
                BigInteger decimals = Unsigned(WordHex((await Call("0x313ce567")).GetString(), 1));
                if (decimals != feed.Decimals) throw new InvalidDataException("Native feed decimals differ from explicit catalogue.");
                string round = WordHex((await Call("0xfeaf968c")).GetString(), 5);
                BigInteger roundId = Unsigned(round.Substring(0, 64));
                BigInteger answer = BigInteger.Parse(round.Substring(64, 64), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                BigInteger started = Unsigned(round.Substring(128, 64));
                BigInteger updated = Unsigned(round.Substring(192, 64));
                BigInteger answered = Unsigned(round.Substring(256, 64));
                if (roundId <= 0 || roundId >= BigInteger.Pow(2, 80) || answered >= BigInteger.Pow(2, 80) ||
                    started <= 0 || updated < started || updated > blockTime)
                    throw new InvalidDataException("Native round identity/width/timestamp invalid.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "ChainLink " + name,
                    ["description"] = "Public feed observation at an RPC-reported finalized Ethereum block; no current-price guarantee.",
                    [AddressMetadata] = key, ["ChainLinkFeedName"] = name, ["ChainLinkChainId"] = MainnetChainId,
                    ["ChainLinkDecimals"] = feed.Decimals.ToString(CultureInfo.InvariantCulture),
                    ["ChainLinkRoundId"] = roundId.ToString(CultureInfo.InvariantCulture),
                    ["ChainLinkAnswer"] = answer.ToString(CultureInfo.InvariantCulture),
                    ["ChainLinkStartedAt"] = started.ToString(CultureInfo.InvariantCulture),
                    ["ChainLinkUpdatedAt"] = updated.ToString(CultureInfo.InvariantCulture),
                    ["ChainLinkAnsweredInRound"] = answered.ToString(CultureInfo.InvariantCulture),
                    ["ChainLinkBlockHash"] = hash, ["ChainLinkBlockNumber"] = blockNumber,
                    ["ChainLinkBlockTimestamp"] = blockTime.ToString(CultureInfo.InvariantCulture),
                    ["ChainLinkCommitment"] = "rpc-reported-finalized"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ChainLinkOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.ChainLinkOASIS] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured feeds are missing.");
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
            => Task.FromException(new NotSupportedException("Public ChainLink feed metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public ChainLink feed metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
