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

namespace NextGenSoftware.OASIS.API.Providers.EclipseOASIS;

/// <summary>
/// Finalized account metadata over a bounded configured Eclipse catalogue.
/// Never submits transactions or projects private/watchlist data.
/// </summary>
public sealed class EclipsePublicAccountMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "EclipseAccountAddress";
    private readonly AccountsBackend accountsBackend;
    private bool disposed;

    public EclipsePublicAccountMetadataSource(HttpClient client, Uri rest,
        IEnumerable<string> addresses, string genesisHash, Guid sourceOwner, bool ownsClient = false)
        : this(new AccountsBackend(client, rest, addresses, genesisHash, sourceOwner, ownsClient)) { }

    private EclipsePublicAccountMetadataSource(AccountsBackend backend) : base(backend, backend.Prefix)
    {
        accountsBackend = backend;
        ProviderName = PType.EclipseOASIS.ToString();
        ProviderDescription = "Read-only public Eclipse account observations";
        ProviderType = new EnumValue<PType>(PType.EclipseOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 44, 8);
    }

    public PType QueryProviderType => PType.EclipseOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// legacy transaction settings. Reject unknown fields rather than ignore keys.</summary>
    public static EclipsePublicAccountMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Eclipse public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "rpcUrl", "addresses", "genesisHash", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("rpcUrl").ValueKind != JsonValueKind.String ||
            root.GetProperty("genesisHash").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("addresses").ValueKind != JsonValueKind.Array ||
            root.GetProperty("addresses").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Eclipse source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new EclipsePublicAccountMetadataSource(client, new Uri(root.GetProperty("rpcUrl").GetString(), UriKind.Absolute),
                root.GetProperty("addresses").EnumerateArray().Select(value => value.GetString()),
                root.GetProperty("genesisHash").GetString(), root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await accountsBackend.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Eclipse account source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Eclipse account source is required.");
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
            if (keys.Any(key => !accountsBackend.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured account catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await accountsBackend.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Eclipse query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Eclipse name query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; accountsBackend.Dispose();
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



    private sealed class AccountsBackend : IKeyValueBackend, IDisposable
    {
        private readonly string sourceName;
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly string genesis;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "eclipse-public-metadata/";

        public AccountsBackend(HttpClient client, Uri api, IEnumerable<string> addresses, string genesisHash, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/" || api.AbsoluteUri.Length > 256 ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie") ||
                client.DefaultRequestHeaders.Contains("x-api-key") || owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous HTTPS Eclipse source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique addresses required.");
            foreach (string key in keys) EclipsePublicAccountReader.ValidateKey(key);
            EclipsePublicAccountReader.ValidateKey(genesisHash);
            genesis = genesisHash;
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            // Include the RPC endpoint in provenance and projected identity.
            // Same-owner addresses on another configured RPC endpoint cannot collide.
            sourceName = "eclipse-public-accounts:" + api.AbsoluteUri + ":" + genesis;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.EclipseOASIS, sourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var snapshot = await new EclipsePublicAccountReader(client, api).ReadAsync(keys, genesis, token);
            var rows = new List<IHolon>();
            foreach (var observed in snapshot.Accounts)
            {
                var fields = new Dictionary<string, object>
                {
                    [AddressMetadata] = observed.Address,
                    ["EclipseGenesisHash"] = snapshot.GenesisHash,
                    ["EclipseSlot"] = snapshot.Slot,
                    ["EclipseCommitment"] = "finalized",
                    ["EclipseAccountOwner"] = observed.Owner,
                    ["EclipseBalanceUnits"] = observed.BalanceUnits,
                    ["EclipseBalanceCurrency"] = "ETH",
                    ["EclipseBalanceDecimals"] = "9",
                    ["EclipseRentEpoch"] = observed.RentEpoch,
                    ["EclipseAccountSpace"] = observed.Space,
                    ["EclipseExecutable"] = observed.Executable ? "true" : "false",
                    ["EclipseObservationPolicy"] = "node-reported-finalized-account-metadata",
                    ["EclipseConsensusVerified"] = "false",
                    ["name"] = "Eclipse account " + observed.Address,
                    ["description"] = "Public finalized Eclipse account metadata; account data excluded."
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.EclipseOASIS,
                    new NativeRecord(sourceName, observed.Address, fields), NativeDataMapping.ForSource(sourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true;
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.EclipseOASIS] = observed.Address;
                rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return Array.AsReadOnly(rows.ToArray());
        }
        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured addresses are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out var name)) return null;
            var rows = await ReadAsync(new[] { name }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Eclipse name data is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Eclipse name data is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
