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

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;

/// <summary>
/// Anonymous native EOSIO/Vaulta account metadata. Reads latest state separately for
/// each selected account; this profile never signs or claims a common-block snapshot.
/// </summary>
public sealed class EosioPublicAccountMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string MainnetChainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906";
    public const string AddressMetadata = "EOSIOAccountName";
    private readonly AccountsBackend accounts;
    private bool disposed;

    public EosioPublicAccountMetadataSource(HttpClient client, Uri rest, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new AccountsBackend(client, rest, chainId, addresses, sourceOwner, ownsClient)) { }

    private EosioPublicAccountMetadataSource(AccountsBackend backend) : base(backend, backend.Prefix)
    {
        accounts = backend;
        ProviderName = PType.EOSIOOASIS.ToString();
        ProviderDescription = "Read-only public EOSIO account metadata; latest state per response";
        ProviderType = new EnumValue<PType>(PType.EOSIOOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 13, 8);
    }

    public PType QueryProviderType => PType.EOSIOOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed EOSIO storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static EosioPublicAccountMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit EOSIO public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "chainId", "accounts", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("apiUrl").ValueKind != JsonValueKind.String || root.GetProperty("chainId").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("accounts").ValueKind != JsonValueKind.Array ||
            root.GetProperty("accounts").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous EOSIO source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new EosioPublicAccountMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                root.GetProperty("chainId").GetString(), root.GetProperty("accounts").EnumerateArray().Select(value => value.GetString()),
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public EOSIO account source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public EOSIO source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "EOSIO query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public EOSIO account query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; accounts.Dispose();
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


    // Native names encode twelve 5-bit characters and an optional final 4-bit
    // character. Reject aliases with trailing dots so one account has one identity.
    private static void Account(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\A[a-z1-5.]{1,12}(?:[a-j1-5.])?\\z") || value.EndsWith('.'))
            throw new ArgumentException("Canonical native EOSIO account name required.");
    }

    private static string Integer(JsonElement value, bool signed = false)
    {
        if (value.ValueKind != JsonValueKind.Number ||
            !Regex.IsMatch(value.GetRawText(), signed ? "\\A-?(?:0|[1-9][0-9]*)\\z" : "\\A(?:0|[1-9][0-9]*)\\z") ||
            (signed ? !long.TryParse(value.GetRawText(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)
                    : !ulong.TryParse(value.GetRawText(), NumberStyles.None, CultureInfo.InvariantCulture, out _)))
            throw new InvalidDataException("Exact native 64-bit integer required.");
        return value.GetRawText();
    }

    private sealed class AccountsBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "eosio-public-accounts:mainnet";
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "eosio-public-metadata/";

        public AccountsBackend(HttpClient client, Uri api, string chainId, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty || chainId != MainnetChainId)
                throw new ArgumentException("Explicit anonymous HTTPS EOSIO Mainnet source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique native account names required.");
            foreach (string key in keys) Account(key);
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.EOSIOOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> ReadNativeAsync(string method, object parameters, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(api, "v1/chain/" + method)) {
                Content = new StringContent(JsonSerializer.Serialize(parameters), Encoding.UTF8, "application/json") };
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
            if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _))
                throw new InvalidDataException("Native EOSIO error; no partial rows.");
            token.ThrowIfCancellationRequested();
            return root.Clone();
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var info = await ReadNativeAsync("get_info", new { }, token);
            if (info.GetProperty("chain_id").GetString() != MainnetChainId)
                throw new InvalidDataException("Native EOSIO Mainnet identity differs.");
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var native = await ReadNativeAsync("get_account", new { account_name = key }, token);
                if (native.GetProperty("account_name").GetString() != key)
                    throw new InvalidDataException("Native account identity differs.");
                string head = Integer(native.GetProperty("head_block_num"));
                if (head == "0") throw new InvalidDataException("Positive native account head required.");
                string created = native.GetProperty("created").GetString();
                if (!DateTime.TryParseExact(created, "yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _)) throw new InvalidDataException("Native creation timestamp required.");
                var fields = new Dictionary<string, object> {
                    ["name"] = "EOSIO account " + key,
                    ["description"] = "Public native EOSIO/Vaulta account metadata; latest state per response, no shared snapshot.",
                    [AddressMetadata] = key, ["EOSIOChainId"] = MainnetChainId,
                    ["EOSIOHeadBlockNum"] = head, ["EOSIOCreated"] = created,
                    ["EOSIORamQuota"] = Integer(native.GetProperty("ram_quota"), signed: true)
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.EOSIOOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.EOSIOOASIS] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured accounts are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out var account)) return null;
            var rows = await ReadAsync(new[] { account }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public EOSIO account metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public EOSIO account metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
