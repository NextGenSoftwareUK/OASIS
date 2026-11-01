extern alias AlgorandCrypto;
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

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS;

/// <summary>Explicit current-state public Algorand accounts; never signed storage.</summary>
public sealed class AlgorandPublicAccountMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "AlgorandAddress";
    private readonly AccountsBackend accounts;
    private bool disposed;

    public const string PublicNetwork = "mainnet-v1.0";
    public const string PublicGenesis = "wGHE2Pwdvd7S12BL5FaOP20EGYesN73ktiC1qzkkit8=";

    public AlgorandPublicAccountMetadataSource(HttpClient client, Uri algod, Uri indexer,
        IEnumerable<string> accounts, Guid sourceOwner, bool ownsClient = false)
        : this(new AccountsBackend(client, algod, indexer, accounts, sourceOwner, ownsClient)) { }

    private AlgorandPublicAccountMetadataSource(AccountsBackend backend) : base(backend, backend.Prefix)
    {
        accounts = backend;
        ProviderName = PType.AlgorandOASIS.ToString();
        ProviderDescription = "Read-only current-state public Algorand accounts";
        ProviderType = new EnumValue<PType>(PType.AlgorandOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 58, 8);
    }

    public PType QueryProviderType => PType.AlgorandOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Algorand storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static AlgorandPublicAccountMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Algorand public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        Unique(root);
        var fields = new[] { "schema", "algodUrl", "indexerUrl", "genesisId", "genesisHash", "consistency", "accounts", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("genesisId").GetString() != PublicNetwork ||
            root.GetProperty("genesisHash").GetString() != PublicGenesis ||
            root.GetProperty("consistency").GetString() != "Current" ||
            root.GetProperty("accounts").ValueKind != JsonValueKind.Array ||
            root.GetProperty("accounts").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous current-state Algorand configuration required; historical/signing fields forbidden.");
        var accounts = root.GetProperty("accounts").EnumerateArray().Select(row => row.GetString()).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new AlgorandPublicAccountMetadataSource(client,
                new Uri(root.GetProperty("algodUrl").GetString(), UriKind.Absolute),
                new Uri(root.GetProperty("indexerUrl").GetString(), UriKind.Absolute),
                accounts, root.GetProperty("sourceOwner").GetGuid(), true);
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Algorand account source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Algorand source is required.");
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
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured account catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await accounts.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Algorand query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Algorand account query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; accounts.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public static string ValidateAddress(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        if (value == null || !Regex.IsMatch(value, "\\A[A-Z2-7]{58}\\z"))
            throw new ArgumentException("Canonical Algorand address required.");
        var raw = new byte[36];
        uint bits = 0; int available = 0, offset = 0;
        foreach (char character in value)
        {
            bits = (bits << 5) | (uint)alphabet.IndexOf(character);
            available += 5;
            if (available >= 8)
            {
                available -= 8;
                raw[offset++] = (byte)(bits >> available);
            }
        }
        if (offset != 36 || (bits & 3) != 0)
            throw new ArgumentException("Canonical address padding required.");
        var digest = new AlgorandCrypto::Org.BouncyCastle.Crypto.Digests.Sha512tDigest(256);
        digest.BlockUpdate(raw, 0, 32);
        var checksum = new byte[32]; digest.DoFinal(checksum, 0);
        if (!raw.AsSpan(32, 4).SequenceEqual(checksum.AsSpan(28, 4)))
            throw new ArgumentException("Algorand address checksum differs.");
        return value;
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
        private readonly Uri algod, indexer;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "algorand-public-accounts:" + PublicGenesis;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "algorand-public-metadata/";

        public AccountsBackend(HttpClient client, Uri algod, Uri indexer, IEnumerable<string> accounts, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            foreach (var endpoint in new[] { algod, indexer })
                if (endpoint == null || endpoint.Scheme != Uri.UriSchemeHttps || endpoint.AbsolutePath != "/" ||
                    !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
                    throw new ArgumentException("Explicit anonymous HTTPS Algorand endpoints required.");
            if (client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Anonymous source and nonempty projection owner required.");
            var catalogue = accounts?.Take(9).Select(ValidateAddress).ToArray() ?? throw new ArgumentNullException(nameof(accounts));
            if (catalogue.Length is < 1 or > 8 || catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length)
                throw new ArgumentException("One to eight unique native account identities required.");
            this.client = client; this.algod = algod; this.indexer = indexer; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(catalogue);
            records = catalogue.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.AlgorandOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> GetNativeAsync(Uri endpoint, string path, CancellationToken token)
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
            var versions = await GetNativeAsync(algod, "versions", token);
            var genesis = await GetNativeAsync(indexer, "v2/blocks/0", token);
            if (versions.GetProperty("genesis_id").GetString() != PublicNetwork ||
                versions.GetProperty("genesis_hash_b64").GetString() != PublicGenesis ||
                genesis.GetProperty("genesis-id").GetString() != PublicNetwork ||
                genesis.GetProperty("genesis-hash").GetString() != PublicGenesis || genesis.GetProperty("round").GetUInt64() != 0)
                throw new InvalidDataException("Both endpoints must report the configured Mainnet genesis.");
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                // Current state is an explicit contract, not a failed rewind fallback.
                var response = await GetNativeAsync(indexer, "v2/accounts/" + key + "?exclude=all", token);
                var native = response.GetProperty("account");
                ulong observed = native.GetProperty("round").GetUInt64();
                if (ValidateAddress(native.GetProperty("address").GetString()) != key ||
                    response.GetProperty("current-round").GetUInt64() != observed ||
                    (native.TryGetProperty("deleted", out var deleted) && deleted.GetBoolean()))
                    throw new InvalidDataException("Account identity/round/visibility differs.");
                string status = native.GetProperty("status").GetString();
                if (status is not ("Online" or "Offline" or "NotParticipating"))
                    throw new InvalidDataException("Unknown participation status.");
                ulong amount = native.GetProperty("amount").GetUInt64();
                ulong without = native.GetProperty("amount-without-pending-rewards").GetUInt64();
                ulong pending = native.GetProperty("pending-rewards").GetUInt64();
                if ((BigInteger)without + pending != amount)
                    throw new InvalidDataException("Native reward accounting differs.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "Algorand account " + key,
                    ["description"] = "API-reported current Algorand account; independently observed round, no shared snapshot.",
                    [AddressMetadata] = key, ["AlgorandObservedRound"] = observed.ToString(CultureInfo.InvariantCulture),
                    ["AlgorandAmountMicroAlgos"] = amount.ToString(CultureInfo.InvariantCulture),
                    ["AlgorandAmountWithoutPendingRewards"] = without.ToString(CultureInfo.InvariantCulture),
                    ["AlgorandPendingRewards"] = pending.ToString(CultureInfo.InvariantCulture),
                    ["AlgorandRewards"] = native.GetProperty("rewards").GetUInt64().ToString(CultureInfo.InvariantCulture),
                    ["AlgorandMinBalance"] = native.GetProperty("min-balance").GetUInt64().ToString(CultureInfo.InvariantCulture),
                    ["AlgorandStatus"] = status, ["AlgorandGenesisHash"] = PublicGenesis,
                    ["AlgorandGenesisId"] = PublicNetwork, ["AlgorandConsistency"] = "Current"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.AlgorandOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.AlgorandOASIS] = key; rows.Add(row);
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
            => Task.FromException(new NotSupportedException("Public Algorand account metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Algorand account metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
