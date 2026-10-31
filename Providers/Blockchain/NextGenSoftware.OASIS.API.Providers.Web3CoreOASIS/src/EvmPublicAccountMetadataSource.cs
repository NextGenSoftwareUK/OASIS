using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

/// <summary>Explicit native public-account metadata profile. No wallet, signing,
/// contract execution, transaction-body download, history scan or source mutations.</summary>
public abstract class EvmPublicAccountMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    protected sealed record Network(PType Provider, string Name, string ChainId, string GenesisHash, string AddressMetadata);
    private readonly AccountsBackend accounts;
    private bool disposed;

    protected EvmPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient, Network network)
        : this(new AccountsBackend(client, rpc, chainId, addresses, sourceOwner, ownsClient, network)) { }

    private EvmPublicAccountMetadataSource(AccountsBackend accounts) : base(accounts, accounts.Prefix)
    {
        this.accounts = accounts;
        var network = accounts.Identity;
        ProviderName = network.Provider.ToString();
        ProviderDescription = $"Read-only finalized public {network.Name} balances; no wallet or transaction-body download";
        ProviderType = new EnumValue<PType>(network.Provider);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(network.AddressMetadata, 42, 8);
    }

    public PType QueryProviderType => accounts.Identity.Provider;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed EVM storage settings. Reject unknown fields rather than ignore keys.</summary>
    protected static T CreateFromJson<T>(string json, Func<HttpClient, Uri, string, IEnumerable<string>, Guid, bool, T> create)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit EVM public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "rpcUrl", "chainId", "addresses", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("rpcUrl").ValueKind != JsonValueKind.String || root.GetProperty("chainId").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("addresses").ValueKind != JsonValueKind.Array ||
            root.GetProperty("addresses").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous EVM source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return create(client, new Uri(root.GetProperty("rpcUrl").GetString(), UriKind.Absolute),
                root.GetProperty("chainId").GetString(), root.GetProperty("addresses").EnumerateArray().Select(value => value.GetString()),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await accounts.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public EVM account source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public EVM source is required.");
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
            if (keys.Any(key => !accounts.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured public catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await accounts.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "EVM query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public EVM account query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; accounts.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private static void Address(string value)
    {
        if (value == null || value.Length != 42 || !Regex.IsMatch(value, "^0x[0-9a-f]{40}$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Canonical lowercase 20-byte EVM address required.");
    }

    private static string Quantity(string value)
    {
        if (value == null || value.Contains("\n") || !Regex.IsMatch(value, "^0x(?:0|[1-9a-f][0-9a-f]{0,63})$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("Canonical UInt256 quantity required.");
        return BigInteger.Parse("0" + value.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)
            .ToString(CultureInfo.InvariantCulture);
    }

    private static string Hash(JsonElement value)
    {
        var text = value.GetString();
        if (text == null || text.Length != 66 || !Regex.IsMatch(text, "^0x[0-9a-f]{64}$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("Canonical 32-byte EVM block hash required.");
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
        private readonly string chainId, sourceName;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Addresses { get; }
        public Network Identity { get; }
        public string Prefix => Identity.Name.ToLowerInvariant() + "-public-metadata/";

        public AccountsBackend(HttpClient client, Uri rpc, string chainId, IEnumerable<string> addresses, Guid owner, bool ownsClient, Network network)
        {
            Identity = network;
            ArgumentNullException.ThrowIfNull(client);
            if (rpc == null || rpc.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(rpc.UserInfo) ||
                !string.IsNullOrEmpty(rpc.Query) || !string.IsNullOrEmpty(rpc.Fragment) || rpc.AbsolutePath != "/" ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Explicit credential-free HTTPS RPC and source identity required.");
            if (chainId != network.ChainId) throw new ArgumentException("The explicit source chain ID differs from the provider profile.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique public accounts required.");
            foreach (var key in keys) Address(key);
            this.client = client; this.rpc = rpc; this.chainId = chainId; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            sourceName = network.Name.ToLowerInvariant() + "-public-accounts:" + chainId;
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, Identity.Provider, sourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> RpcAsync(string method, object[] parameters, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            using var request = new HttpRequestMessage(HttpMethod.Post, rpc) {
                Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, method, @params = parameters }) };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = await stream.ReadAsync(buffer.AsMemory(), token)) != 0)
            {
                if (body.Length + count > 262144) throw new InvalidDataException("Native metadata response exceeds 256KiB.");
                body.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            Unique(document.RootElement);
            var root = document.RootElement;
            if (root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != 1 ||
                root.TryGetProperty("error", out _)) throw new InvalidDataException("Native RPC envelope failed validation.");
            token.ThrowIfCancellationRequested();
            return root.GetProperty("result").Clone();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            // Bind the network before selecting account state. A matching chain ID
            // alone is insufficient to distinguish a misconfigured chain.
            var identity = await RpcAsync("eth_chainId", Array.Empty<object>(), token);
            if (identity.GetString() != chainId) throw new InvalidDataException("Native chain ID differs.");
            var genesis = await RpcAsync("eth_getBlockByNumber", new object[] { "0x0", false }, token);
            if (Hash(genesis.GetProperty("hash")) != Identity.GenesisHash ||
                genesis.GetProperty("number").GetString() != "0x0")
                throw new InvalidDataException("Native EVM genesis differs.");
            var finalized = await RpcAsync("eth_getBlockByNumber", new object[] { "finalized", false }, token);
            var hash = Hash(finalized.GetProperty("hash"));
            var number = Quantity(finalized.GetProperty("number").GetString());
            var rows = new List<IHolon>();
            foreach (var key in keys)
            {
                // Every selected address is read from the same finalized canonical
                // block. Unknown/unsupported block semantics fail, never fall back.
                var balance = await RpcAsync("eth_getBalance", new object[] {
                    key, new { blockHash = hash, requireCanonical = true } }, token);
                var fields = new Dictionary<string, object> {
                    ["name"] = Identity.Name + " address " + key,
                    ["description"] = "Public finalized " + Identity.Name + " address balance; no wallet or transactions.",
                    [Identity.AddressMetadata] = key, [Identity.Name + "ChainId"] = chainId,
                    [Identity.Name + "BlockHash"] = hash, [Identity.Name + "BlockNumber"] = number,
                    [Identity.Name + "Commitment"] = "finalized", [Identity.Name + "BalanceWei"] = Quantity(balance.GetString())
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, Identity.Provider,
                    new NativeRecord(sourceName, key, fields), NativeDataMapping.ForSource(sourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[Identity.Provider] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var rows = await ReadAsync(Addresses, cancellationToken);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured probe accounts are missing.");
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var address)) return null;
            var rows = await ReadAsync(new[] { address }, cancellationToken);
            return rows.Count == 0 ? null : StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public EVM account metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public EVM account metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
