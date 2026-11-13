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

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

/// <summary>Explicit public NEAR account representation; never signed storage.</summary>
public sealed class NearPublicAccountMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "NearAccountId";
    private readonly AccountsBackend state;
    private bool disposed;

    public NearPublicAccountMetadataSource(HttpClient client, Uri rest, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new AccountsBackend(client, rest, chainId, addresses, sourceOwner, ownsClient)) { }

    private NearPublicAccountMetadataSource(AccountsBackend backend) : base(backend, backend.Prefix)
    {
        state = backend;
        ProviderName = PType.NEAROASIS.ToString();
        ProviderDescription = "Read-only height-pinned public NEAR accounts";
        ProviderType = new EnumValue<PType>(PType.NEAROASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 64, 8);
    }

    public PType QueryProviderType => PType.NEAROASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed NEAR storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static NearPublicAccountMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit NEAR public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "rpcUrl", "networkId", "accounts", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("rpcUrl").ValueKind != JsonValueKind.String || root.GetProperty("networkId").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("accounts").ValueKind != JsonValueKind.Array ||
            root.GetProperty("accounts").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous NEAR source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new NearPublicAccountMetadataSource(client, new Uri(root.GetProperty("rpcUrl").GetString(), UriKind.Absolute),
                root.GetProperty("networkId").GetString(), root.GetProperty("accounts").EnumerateArray().Select(value => value.GetString()),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await state.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public NEAR account source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public NEAR source is required.");
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
            if (keys.Any(key => !state.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured validator catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await state.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "NEAR query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public NEAR account query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; state.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private static void Address(string value)
    {
        if (value == null || value.Length is < 2 or > 64 ||
            !Regex.IsMatch(value, "\\A[a-z0-9]+(?:[._-][a-z0-9]+)*\\z"))
            throw new ArgumentException("Canonical NEAR account ID required.");
    }

    private static string Amount(JsonElement value)
    {
        string text = value.GetString();
        if (text == null || text.Length > 39 || !Regex.IsMatch(text, "\\A(?:0|[1-9][0-9]*)\\z") ||
            BigInteger.Parse(text, CultureInfo.InvariantCulture) > (BigInteger.One << 128) - 1)
            throw new InvalidDataException("Canonical UInt128 amount string required.");
        return text;
    }

    private static string Integer(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !Regex.IsMatch(value.GetRawText(), "\\A(?:0|[1-9][0-9]*)\\z"))
            throw new InvalidDataException("Canonical UInt64 native integer required.");
        return value.GetUInt64().ToString(CultureInfo.InvariantCulture);
    }

    private static string Hash(JsonElement value)
    {
        const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        string text = value.GetString();
        if (text == null || text.Length is < 32 or > 44) throw new InvalidDataException("NEAR32byte base58 hash required.");
        BigInteger number = BigInteger.Zero;
        foreach (char character in text)
        {
            int digit = alphabet.IndexOf(character);
            if (digit < 0) throw new InvalidDataException("Invalid base58 alphabet.");
            number = number * 58 + digit;
        }
        int zeroes = text.TakeWhile(character => character == '1').Count();
        int bytes = number.IsZero ? 0 : number.ToByteArray(isUnsigned: true, isBigEndian: true).Length;
        if (zeroes + bytes != 32) throw new InvalidDataException("Decoded NEAR hash width differs.");
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

    private sealed class AccountsBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Uri rpc;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "near-public-accounts:mainnet";
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "near-public-metadata/";

        public AccountsBackend(HttpClient client, Uri rpc, string chainId, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (rpc == null || rpc.Scheme != Uri.UriSchemeHttps || rpc.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(rpc.UserInfo) || !string.IsNullOrEmpty(rpc.Query) || !string.IsNullOrEmpty(rpc.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty || chainId != "mainnet")
                throw new ArgumentException("Explicit anonymous HTTPS NEAR mainnet source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique NEAR accounts required.");
            foreach (var key in keys) Address(key);
            this.client = client; this.rpc = rpc; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.NEAROASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> RpcAsync(string method, object parameters, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, rpc) {
                Content = System.Net.Http.Json.JsonContent.Create(new { jsonrpc = "2.0", id = 1, method, @params = parameters }) };
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
            Unique(document.RootElement); var root = document.RootElement;
            if (root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != 1 || root.TryGetProperty("error", out _))
                throw new InvalidDataException("RPC envelope identity/error differs.");
            token.ThrowIfCancellationRequested();
            return root.GetProperty("result").Clone();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var status = await RpcAsync("status", Array.Empty<object>(), token);
            if (status.GetProperty("chain_id").GetString() != "mainnet" || status.GetProperty("sync_info").GetProperty("syncing").GetBoolean())
                throw new InvalidDataException("Expected synced NEAR mainnet source.");
            string blockHash = null, blockHeight = null;
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                // Resolve final state once, then bind every remaining account to
                // that exact block. Never downgrade to optimistic/near-final/latest.
                var parameters = new Dictionary<string, object> { ["request_type"] = "view_account", ["account_id"] = key };
                if (blockHash == null) parameters["finality"] = "final";
                else parameters["block_id"] = blockHash;
                var account = await RpcAsync("query", parameters, token);
                string hash = Hash(account.GetProperty("block_hash"));
                string height = Integer(account.GetProperty("block_height"));
                if (blockHash == null) { blockHash = hash; blockHeight = height; }
                if (blockHash != hash || blockHeight != height) throw new InvalidDataException("Native account state block differs.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "NEAR account " + key,
                    ["description"] = "Public NEAR account state at one RPC-reported final block.",
                    [AddressMetadata] = key, ["NearNetworkId"] = "mainnet", ["NearBlockHash"] = blockHash,
                    ["NearBlockHeight"] = blockHeight, ["NearFinality"] = "final",
                    ["NearAmountYocto"] = Amount(account.GetProperty("amount")),
                    ["NearLockedYocto"] = Amount(account.GetProperty("locked")),
                    ["NearStorageUsageBytes"] = Integer(account.GetProperty("storage_usage")),
                    ["NearCodeHash"] = Hash(account.GetProperty("code_hash"))
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.NEAROASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.NEAROASIS] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var rows = await ReadAsync(Addresses, cancellationToken);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured accounts are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var account)) return null;
            var rows = await ReadAsync(new[] { account }, cancellationToken);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public NEAR account metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public NEAR account metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
