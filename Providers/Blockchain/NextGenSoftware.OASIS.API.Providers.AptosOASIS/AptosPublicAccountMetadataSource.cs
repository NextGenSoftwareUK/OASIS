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

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS;

/// <summary>Explicit per-query-version public Aptos accounts; never signed storage.</summary>
public sealed class AptosPublicAccountMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "AptosAccountAddress";
    private readonly AccountsBackend accounts;
    private bool disposed;

    public const byte PublicChainId = 1;

    public AptosPublicAccountMetadataSource(HttpClient client, Uri endpoint,
        IEnumerable<string> accounts, Guid sourceOwner, bool ownsClient = false)
        : this(new AccountsBackend(client, endpoint, accounts, sourceOwner, ownsClient)) { }

    private AptosPublicAccountMetadataSource(AccountsBackend backend) : base(backend, backend.Prefix)
    {
        accounts = backend;
        ProviderName = PType.AptosOASIS.ToString();
        ProviderDescription = "Read-only per-query-version public Aptos accounts";
        ProviderType = new EnumValue<PType>(PType.AptosOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 66, 8);
    }

    public PType QueryProviderType => PType.AptosOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Aptos storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static AptosPublicAccountMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Aptos public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        Unique(root);
        var fields = new[] { "schema", "restUrl", "chainId", "consistency", "accounts", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").GetInt32() != 1 || root.GetProperty("chainId").GetByte() != PublicChainId ||
            root.GetProperty("consistency").GetString() != "PerQueryLedgerVersion" ||
            root.GetProperty("accounts").ValueKind != JsonValueKind.Array ||
            root.GetProperty("accounts").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous version-selected Aptos configuration required; history/signing fields forbidden.");
        var accounts = root.GetProperty("accounts").EnumerateArray().Select(row => row.GetString()).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new AptosPublicAccountMetadataSource(client,
                new Uri(root.GetProperty("restUrl").GetString(), UriKind.Absolute),
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Aptos account source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Aptos source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Aptos query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Aptos account query failed; no partial rows.", error); }
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
        if (value == null || !Regex.IsMatch(value, "\\A0x[0-9a-f]{64}\\z"))
            throw new ArgumentException("Canonical Aptos 32-byte lowercase hex required.");
        return value;
    }

    private static ulong UInt64(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A(?:0|[1-9][0-9]{0,19})\\z") ||
            !ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out ulong result))
            throw new InvalidDataException("Canonical UInt64 decimal string required.");
        return result;
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
        private readonly Uri endpoint;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "aptos-public-accounts:mainnet:1";
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "aptos-public-metadata/";

        public AccountsBackend(HttpClient client, Uri endpoint, IEnumerable<string> accounts, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (endpoint == null || endpoint.Scheme != Uri.UriSchemeHttps || endpoint.AbsolutePath != "/v1/" ||
                !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
                throw new ArgumentException("Explicit anonymous HTTPS Aptos /v1/ endpoint required.");
            if (client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Anonymous source and nonempty projection owner required.");
            var catalogue = accounts?.Take(9).Select(ValidateAddress).ToArray() ?? throw new ArgumentNullException(nameof(accounts));
            if (catalogue.Length is < 1 or > 8 || catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length)
                throw new ArgumentException("One to eight unique native account identities required.");
            this.client = client; this.endpoint = endpoint; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(catalogue);
            records = catalogue.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.AptosOASIS, SourceName, key).ToString("N"), key => key);
        }

        private sealed record NativeResponse(JsonElement Data, ulong Current, ulong Oldest);

        private async Task<NativeResponse> GetNativeAsync(string path, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint, path));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            string Header(string name)
            {
                if (!response.Headers.TryGetValues(name, out var values))
                    throw new InvalidDataException("Required Aptos response header missing.");
                var entries = values.Take(2).ToArray();
                if (entries.Length != 1) throw new InvalidDataException("Duplicate Aptos response header.");
                return entries[0];
            }
            if (Header("X-APTOS-CHAIN-ID") != "1") throw new InvalidDataException("Explicit Mainnet chain ID required.");
            ulong current = UInt64(Header("X-APTOS-LEDGER-VERSION"));
            ulong oldest = UInt64(Header("X-APTOS-LEDGER-OLDEST-VERSION"));
            if (oldest > current) throw new InvalidDataException("Invalid retained version range.");
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
            return new(document.RootElement.Clone(), current, oldest);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            // One explicit ledger version is selected for this whole source query.
            // A pruned/failed request rejects the page; it never retries unversioned.
            var ledger = await GetNativeAsync("", token);
            ulong selected = UInt64(ledger.Data.GetProperty("ledger_version").GetString());
            if (ledger.Data.GetProperty("chain_id").GetByte() != PublicChainId || selected != ledger.Current ||
                UInt64(ledger.Data.GetProperty("oldest_ledger_version").GetString()) != ledger.Oldest)
                throw new InvalidDataException("Ledger body/header network or version differs.");
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var response = await GetNativeAsync("accounts/" + key + "?ledger_version=" + selected.ToString(CultureInfo.InvariantCulture), token);
                var native = response.Data;
                if (selected < response.Oldest || selected > response.Current || native.ValueKind != JsonValueKind.Object ||
                    native.EnumerateObject().Count() != 2 || !native.TryGetProperty("authentication_key", out var authentication) ||
                    !native.TryGetProperty("sequence_number", out var sequence))
                    throw new InvalidDataException("Version unavailable or exact account response differs.");
                string authenticationKey = ValidateAddress(authentication.GetString());
                string sequenceNumber = UInt64(sequence.GetString()).ToString(CultureInfo.InvariantCulture);
                var fields = new Dictionary<string, object> {
                    ["name"] = "Aptos account " + key,
                    ["description"] = "API-reported account at one source-query ledger version; no independent state proof.",
                    [AddressMetadata] = key, ["AptosLedgerVersion"] = selected.ToString(CultureInfo.InvariantCulture),
                    ["AptosSequenceNumber"] = sequenceNumber, ["AptosAuthenticationKey"] = authenticationKey,
                    ["AptosChainId"] = "1", ["AptosConsistency"] = "PerQueryLedgerVersion"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.AptosOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.AptosOASIS] = key; rows.Add(row);
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
            => Task.FromException(new NotSupportedException("Public Aptos account metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Aptos account metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
