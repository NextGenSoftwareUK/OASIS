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

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

/// <summary>Explicit public Cosmos Hub validator representation; never signed storage.</summary>
public sealed class CosmosPublicValidatorMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "CosmosValidatorAddress";
    private readonly ValidatorsBackend validators;
    private bool disposed;

    public CosmosPublicValidatorMetadataSource(HttpClient client, Uri rest, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new ValidatorsBackend(client, rest, chainId, addresses, sourceOwner, ownsClient)) { }

    private CosmosPublicValidatorMetadataSource(ValidatorsBackend backend) : base(backend, backend.Prefix)
    {
        validators = backend;
        ProviderName = PType.CosmosBlockChainOASIS.ToString();
        ProviderDescription = "Read-only height-pinned public Cosmos Hub validators";
        ProviderType = new EnumValue<PType>(PType.CosmosBlockChainOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 52, 8);
    }

    public PType QueryProviderType => PType.CosmosBlockChainOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Cosmos storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static CosmosPublicValidatorMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Cosmos public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "restUrl", "chainId", "addresses", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("restUrl").ValueKind != JsonValueKind.String || root.GetProperty("chainId").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("addresses").ValueKind != JsonValueKind.Array ||
            root.GetProperty("addresses").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Cosmos source schema required; signer/billing fields forbidden.");
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new CosmosPublicValidatorMetadataSource(client, new Uri(root.GetProperty("restUrl").GetString(), UriKind.Absolute),
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
            await validators.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Cosmos account source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Cosmos source is required.");
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
            if (keys.Any(key => !validators.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured validator catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await validators.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Cosmos query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Cosmos account query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; validators.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private static void Address(string value)
    {
        const string alphabet = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        const string prefix = "cosmosvaloper";
        if (value == null || value.Length != 52 || !value.StartsWith(prefix + "1", StringComparison.Ordinal))
            throw new ArgumentException("Canonical Cosmos Hub validator address required.");
        uint checksum = 1;
        void Step(int part)
        {
            uint top = checksum >> 25;
            checksum = ((checksum & 0x1ffffff) << 5) ^ (uint)part;
            uint[] generators = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
            for (int i = 0; i < 5; i++) if (((top >> i) & 1) != 0) checksum ^= generators[i];
        }
        foreach (char c in prefix) Step(c >> 5);
        Step(0);
        foreach (char c in prefix) Step(c & 31);
        foreach (char c in value.Substring(prefix.Length + 1))
        {
            int digit = alphabet.IndexOf(c);
            if (digit < 0) throw new ArgumentException("Invalid Bech32 alphabet.");
            Step(digit);
        }
        if (checksum != 1) throw new ArgumentException("Invalid validator Bech32 checksum.");
    }

    private static string Unsigned(string text)
    {
        if (text == null || text.Length > 78 || !Regex.IsMatch(text, "\\A(?:0|[1-9][0-9]*)\\z") ||
            BigInteger.Parse(text, CultureInfo.InvariantCulture) > (BigInteger.One << 256) - 1)
            throw new InvalidDataException("Canonical UInt256 decimal string required.");
        return text;
    }

    private static string Hash(JsonElement value)
    {
        string text = value.GetString();
        byte[] decoded = Convert.FromBase64String(text);
        if (decoded.Length != 32 || Convert.ToBase64String(decoded) != text)
            throw new InvalidDataException("Canonical base64 32-byte hash required.");
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

    private sealed class ValidatorsBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Uri rest;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "cosmos-public-validators:cosmoshub-4";
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "cosmos-public-metadata/";

        public ValidatorsBackend(HttpClient client, Uri rest, string chainId, IEnumerable<string> addresses, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (rest == null || rest.Scheme != Uri.UriSchemeHttps || rest.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(rest.UserInfo) || !string.IsNullOrEmpty(rest.Query) || !string.IsNullOrEmpty(rest.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty || chainId != "cosmoshub-4")
                throw new ArgumentException("Explicit anonymous HTTPS Cosmos Hub source required.");
            var keys = addresses?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(addresses));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique validators required.");
            foreach (var key in keys) Address(key);
            this.client = client; this.rest = rest; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.CosmosBlockChainOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<(JsonElement Body, string Height)> GetAsync(string path, string height, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(rest, path));
            request.Headers.Accept.ParseAdd("application/json");
            if (height != null) request.Headers.Add("x-cosmos-block-height", height);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            var heights = response.Headers.TryGetValues("x-cosmos-block-height", out var values) ? values.ToArray() : Array.Empty<string>();
            if (heights.Length != 1 || (height != null && heights[0] != height))
                throw new InvalidDataException("Native state height was not acknowledged exactly.");
            Unsigned(heights[0]);
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = await stream.ReadAsync(buffer.AsMemory(), token)) != 0)
            {
                if (body.Length + count > 262144) throw new InvalidDataException("Native response exceeds 256 KiB.");
                body.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            Unique(document.RootElement);
            token.ThrowIfCancellationRequested();
            return (document.RootElement.Clone(), heights[0]);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var latest = await GetAsync("/cosmos/base/tendermint/v1beta1/blocks/latest", null, token);
            var header = latest.Body.GetProperty("block").GetProperty("header");
            if (header.GetProperty("chain_id").GetString() != "cosmoshub-4")
                throw new InvalidDataException("Unexpected native chain identity.");
            string blockHeight = Unsigned(header.GetProperty("height").GetString());
            var blockNumber = BigInteger.Parse(blockHeight, CultureInfo.InvariantCulture);
            if (blockNumber < 1 || (latest.Height != blockHeight && latest.Height != (blockNumber - 1).ToString(CultureInfo.InvariantCulture)))
                throw new InvalidDataException("Unexpected application query height.");
            string blockHash = Hash(latest.Body.GetProperty("block_id").GetProperty("hash"));
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var result = await GetAsync("/cosmos/staking/v1beta1/validators/" + key, latest.Height, token);
                var validator = result.Body.GetProperty("validator");
                if (validator.GetProperty("operator_address").GetString() != key)
                    throw new InvalidDataException("Validator identity differs from selection.");
                string status = validator.GetProperty("status").GetString();
                if (status is not ("BOND_STATUS_BONDED" or "BOND_STATUS_UNBONDED" or "BOND_STATUS_UNBONDING"))
                    throw new InvalidDataException("Unexpected validator status.");
                string moniker = validator.GetProperty("description").GetProperty("moniker").GetString();
                if (moniker == null || moniker.Length > 256) throw new InvalidDataException("Bounded validator name required.");
                var fields = new Dictionary<string, object> {
                    ["name"] = moniker, ["description"] = "Public Cosmos Hub validator metadata; server-reported pinned state.",
                    [AddressMetadata] = key, ["CosmosChainId"] = "cosmoshub-4", ["CosmosBlockHash"] = blockHash,
                    ["CosmosBlockNumber"] = blockHeight, ["CosmosStateHeight"] = latest.Height,
                    ["CosmosValidatorTokens"] = Unsigned(validator.GetProperty("tokens").GetString()),
                    ["CosmosValidatorStatus"] = status, ["CosmosValidatorJailed"] = validator.GetProperty("jailed").GetBoolean()
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.CosmosBlockChainOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.CosmosBlockChainOASIS] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            var rows = await ReadAsync(Addresses, cancellationToken);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured validators are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var address)) return null;
            var rows = await ReadAsync(new[] { address }, cancellationToken);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Cosmos validator metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Cosmos validator metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
