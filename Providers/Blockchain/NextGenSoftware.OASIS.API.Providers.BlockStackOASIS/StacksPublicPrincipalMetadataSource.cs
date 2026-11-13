extern alias StacksCrypto;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using Sha512tDigest = StacksCrypto::Org.BouncyCastle.Crypto.Digests.Sha512tDigest;
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

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;

/// <summary>
/// Anonymous mainnet principal-state observations over an explicit bounded catalogue.
/// Each selection reads one explicit node tip; no signing, submission or discovery scans.
/// </summary>
public sealed class StacksPublicPrincipalMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "StacksPrincipal";
    private readonly AccountsBackend accounts;
    private bool disposed;

    public StacksPublicPrincipalMetadataSource(HttpClient client, Uri rest,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new AccountsBackend(client, rest, addresses, sourceOwner, ownsClient)) { }

    private StacksPublicPrincipalMetadataSource(AccountsBackend backend) : base(backend, backend.Prefix)
    {
        accounts = backend;
        ProviderName = PType.BlockStackOASIS.ToString();
        ProviderDescription = "Public Stacks explicit-tip principal-state observations";
        ProviderType = new EnumValue<PType>(PType.BlockStackOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 128, 8);
    }

    public PType QueryProviderType => PType.BlockStackOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// legacy Gaia/authenticated storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static StacksPublicPrincipalMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Stacks public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "principals", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("apiUrl").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("principals").ValueKind != JsonValueKind.Array ||
            root.GetProperty("principals").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Stacks source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new StacksPublicPrincipalMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                root.GetProperty("principals").EnumerateArray().Select(value => value.GetString()),
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Stacks principal-state source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Stacks source is required.");
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
                return Failure("CQL_INVALID_SOURCE_KEY", "Select principals from the configured principal catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await accounts.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Stacks query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Stacks principal-state query failed; no partial rows.", error); }
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



    private static void Principal(string value)
    {
        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        if (value == null || value.Length > 128) throw new ArgumentException("Bounded mainnet principal required.");
        var parts = value.Split('.');
        if (parts.Length > 2 || (parts.Length == 2 && !Regex.IsMatch(parts[1], @"\A[A-Za-z][A-Za-z0-9_-]{0,39}\z")))
            throw new ArgumentException("Canonical contract principal required.");
        string address = parts[0];
        if (address.Length is < 3 or > 41 || address[0] != 'S' || address[1] is not ('P' or 'M'))
            throw new ArgumentException("Mainnet C32 address required.");
        BigInteger encoded = BigInteger.Zero;
        foreach (char character in address[2..])
        {
            int digit = alphabet.IndexOf(character);
            if (digit < 0) throw new ArgumentException("Canonical uppercase C32 address required.");
            encoded = encoded * 32 + digit;
        }
        int leadingZeroBytes = address[2..].TakeWhile(c => c == '0').Count();
        byte[] decoded = encoded.IsZero ? Array.Empty<byte>() : encoded.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (leadingZeroBytes + decoded.Length != 24) throw new ArgumentException("C32 payload length differs.");
        var payload = new byte[24];
        decoded.CopyTo(payload, leadingZeroBytes);
        var checksumInput = new byte[21];
        checksumInput[0] = (byte)alphabet.IndexOf(address[1]);
        Array.Copy(payload, 0, checksumInput, 1, 20);
        var checksum = SHA256.HashData(SHA256.HashData(checksumInput));
        if (!checksum.AsSpan(0, 4).SequenceEqual(payload.AsSpan(20, 4)))
            throw new ArgumentException("C32 checksum differs.");
    }

    private static string Hash(JsonElement value, int bytes)
    {
        if (value.ValueKind != JsonValueKind.String ||
            !Regex.IsMatch(value.GetString(), @"\A[0-9a-f]{" + (bytes * 2) + @"}\z"))
            throw new InvalidDataException("Canonical unprefixed native hash required.");
        return value.GetString();
    }

    private static string Unsigned64(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !Regex.IsMatch(value.GetRawText(), @"\A(?:0|[1-9][0-9]*)\z") ||
            !ulong.TryParse(value.GetRawText(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            throw new InvalidDataException("Exact native uint64 required.");
        return parsed.ToString(CultureInfo.InvariantCulture);
    }

    private static string Amount(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String || !Regex.IsMatch(value.GetString(), @"\A0x[0-9a-f]{32}\z"))
            throw new InvalidDataException("Fixed-width native uint128 required.");
        return UInt128.Parse(value.GetString().AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)
            .ToString(CultureInfo.InvariantCulture);
    }

    private sealed class AccountsBackend : IKeyValueBackend, IDisposable
    {
        private const string SourceName = "stacks-public-principal-state";
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "stacks-public-metadata/";

        public AccountsBackend(HttpClient client, Uri api, IEnumerable<string> principals, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie") ||
                owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous HTTPS mainnet RPC source required.");
            var keys = principals?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(principals));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique mainnet principals required.");
            foreach (string key in keys) Principal(key);
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.BlockStackOASIS, SourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            long totalBytes = 0;

            async Task<JsonDocument> Read(string route)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(api, route));
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
                while ((count = await stream.ReadAsync(buffer.AsMemory(), deadline.Token)) != 0)
                {
                    totalBytes += count;
                    if (body.Length + count > 65536 || totalBytes > 262144)
                        throw new InvalidDataException("Native response byte budget exceeded.");
                    body.Write(buffer, 0, count);
                }
                var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 8 });
                try { Unique(document.RootElement); return document; }
                catch { document.Dispose(); throw; }
            }

            using var infoDocument = await Read("v2/info");
            var info = infoDocument.RootElement;
            if (info.ValueKind != JsonValueKind.Object || info.GetProperty("network_id").ValueKind != JsonValueKind.Number ||
                info.GetProperty("network_id").GetRawText() != "1" || info.GetProperty("is_fully_synced").ValueKind != JsonValueKind.True)
                throw new InvalidDataException("Synchronized Stacks mainnet network1 required.");
            string height = Unsigned64(info.GetProperty("stacks_tip_height"));
            string blockHash = Hash(info.GetProperty("stacks_tip"), 32);
            string consensusHash = Hash(info.GetProperty("stacks_tip_consensus_hash"), 20);
            string genesisHash = Hash(info.GetProperty("genesis_chainstate_hash"), 32);
            // Reference StacksBlockId::new hashes block || consensus using
            // SHA-512/256. Truncating plain SHA-512 would select another ID.
            byte[] input = Convert.FromHexString(blockHash + consensusHash);
            var digest = new Sha512tDigest(256);
            digest.BlockUpdate(input, 0, input.Length);
            var blockIdBytes = new byte[32];
            digest.DoFinal(blockIdBytes, 0);
            string blockId = Convert.ToHexString(blockIdBytes).ToLowerInvariant();

            var rows = new List<IHolon>();
            foreach (string principal in keys)
            {
                // Exactly64lowercase hex is mandatory. Malformed tip selectors
                // can silently choose the node's current state instead.
                using var document = await Read("v2/accounts/" + Uri.EscapeDataString(principal) + "?proof=0&tip=" + blockId);
                var root = document.RootElement;
                var expected = new[] { "balance", "locked", "unlock_height", "nonce" };
                if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != expected.Length ||
                    root.EnumerateObject().Any(field => !expected.Contains(field.Name, StringComparer.Ordinal)))
                    throw new InvalidDataException("Complete proof0 principal state required.");
                string balance = Amount(root.GetProperty("balance"));
                var fields = new Dictionary<string, object> {
                    [AddressMetadata] = principal, ["StacksBalanceMicroStx"] = balance,
                    ["StacksLockedMicroStx"] = Amount(root.GetProperty("locked")),
                    ["StacksNonce"] = Unsigned64(root.GetProperty("nonce")),
                    ["StacksUnlockHeight"] = Unsigned64(root.GetProperty("unlock_height")),
                    ["StacksBlockId"] = blockId, ["StacksBlockHash"] = blockHash,
                    ["StacksConsensusHash"] = consensusHash, ["StacksTipHeight"] = height,
                    ["StacksGenesisChainstateHash"] = genesisHash,
                    ["StacksObservationPolicy"] = "node-reported-explicit-tip",
                    ["name"] = principal,
                    ["description"] = "Stacks principal at height " + height + "; spendable " + balance + " micro-STX"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.BlockStackOASIS,
                    new NativeRecord(SourceName, principal, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.BlockStackOASIS] = principal;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return Array.AsReadOnly(rows.ToArray());
        }

        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured principals are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out var did)) return null;
            var rows = await ReadAsync(new[] { did }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Stacks principal-state metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Stacks principal-state metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
