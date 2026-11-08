#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Buffers.Binary;
using Nethereum.ABI;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.HolonymOASIS;

/// <summary>
/// Current Human ID attestation index records through exact native IDs.
/// Index observations do not independently verify a person, proof or native consensus.
/// </summary>
public sealed class HolonymAttestationQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "human-id/sign/optimism/schema-1";
    public const string SchemaId = "onchain_evm_10_0x1";
    public const string Attester = "0xb1f50c6c34c72346b1229e5c80587d0d659556fd";
    public static IReadOnlyList<string> ReviewedIds { get; } = Array.AsReadOnly(new[] {
        "onchain_evm_10_0x3248d", "onchain_evm_10_0x3248c", "onchain_evm_10_0x3248a" });
    private readonly RegistryBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    public HolonymAttestationQuerySource(Guid owner, IEnumerable<string> attestations)
        : this(owner, attestations, null) { }

    public static HolonymAttestationQuerySource ForReviewedTeachingCatalogue(Guid owner) =>
        new(owner, ReviewedIds);

    internal HolonymAttestationQuerySource(Guid owner, IEnumerable<string> attestations, HttpMessageHandler? handler)
        : this(new RegistryBackend(owner, attestations, handler)) { }

    private HolonymAttestationQuerySource(RegistryBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Human ID production Sign attestation observations";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3,
            admittedRecordIds: backend.RecordIds);
    }

    public PType QueryProviderType => PType.HolonymOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static Guid LogicalId(Guid owner, string attestationId)
    {
        if (owner == Guid.Empty || !IsNativeId(attestationId))
            throw new ArgumentException("A source owner and canonical Optimism native attestation ID are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.HolonymOASIS, Source,
            attestationId);
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(backend.Probe, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose() { IsProviderActivated = false; backend.Dispose(); }

    private static bool IsNativeId(string? value) => value != null && Regex.IsMatch(value,
        "^onchain_evm_10_0x[1-9a-f][0-9a-f]{0,63}$", RegexOptions.CultureInvariant);

    private sealed class RegistryBackend : IKeyValueBackend, IDisposable
    {
        private static readonly Uri Endpoint = new("https://mainnet-rpc.sign.global/api/index/attestations/");
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> attestations;
        private readonly HttpClient client;
        public Guid Probe { get; }
        public IReadOnlySet<Guid> RecordIds => attestations.Keys.ToHashSet();

        public RegistryBackend(Guid owner, IEnumerable<string> configured, HttpMessageHandler? handler)
        {
            if (configured is null) throw new ArgumentNullException(nameof(configured));
            var selected = configured.Take(9).ToArray();
            if (selected.Length is < 1 or > 8 || selected.Distinct(StringComparer.Ordinal).Count() != selected.Length)
                throw new ArgumentException("One to eight distinct explicit native attestation IDs are required.");
            attestations = selected.ToDictionary(id => LogicalId(owner, id));
            this.owner = owner; Probe = LogicalId(owner, selected[0]);
            if (handler is null)
            {
                var guarded = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                guarded.AllowAutoRedirect = false; guarded.UseProxy = false; guarded.UseCookies = false;
                handler = guarded;
            }
            client = new HttpClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!attestations.TryGetValue(id, out var key))
                throw new InvalidDataException("The ID is outside the configured Human ID catalogue.");
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(Endpoint, key));
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("The native index must return JSON.");
            await response.Content.LoadIntoBufferAsync(16384, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 24 });
            UniqueFields(document.RootElement);
            var envelope = document.RootElement;
            if (!envelope.GetProperty("success").GetBoolean() || envelope.GetProperty("statusCode").GetInt32() != 200)
                throw new InvalidDataException("The index did not acknowledge the exact native record.");
            var row = envelope.GetProperty("data"); var schema = row.GetProperty("schema");
            if (row.GetProperty("id").GetString() != key || row.GetProperty("attestationId").GetString() != key[15..] ||
                row.GetProperty("fullSchemaId").GetString() != SchemaId || row.GetProperty("schemaId").GetString() != "0x1" ||
                row.GetProperty("attester").GetString()?.ToLowerInvariant() != Attester ||
                schema.GetProperty("id").GetString() != SchemaId || schema.GetProperty("schemaId").GetString() != "0x1")
                throw new InvalidDataException("Native attestation identity/schema/attester differs.");
            foreach (var value in new[] { row, schema })
                if (value.GetProperty("mode").GetString() != "onchain" || value.GetProperty("chainType").GetString() != "evm" ||
                    value.GetProperty("chainId").GetString() != "10" || value.GetProperty("dataLocation").GetString() != "onchain")
                    throw new InvalidDataException("The record differs from the admitted Optimism on-chain index scope.");
            var types = schema.GetProperty("data");
            if (types.ValueKind != JsonValueKind.Array || types.GetArrayLength() != 2 ||
                types[0].GetProperty("name").GetString() != "circuitId" || types[0].GetProperty("type").GetString() != "string" ||
                types[1].GetProperty("name").GetString() != "publicValues" || types[1].GetProperty("type").GetString() != "uint256[]")
                throw new InvalidDataException("The native payload schema differs.");
            var hex = row.GetProperty("data").GetString();
            if (hex is null || hex.Length > 8194 || !Regex.IsMatch(hex, "^0x(?:[0-9a-f]{2})+$"))
                throw new InvalidDataException("A bounded canonical hexadecimal ABI payload is required.");
            var bytes = Convert.FromHexString(hex[2..]); ValidateFrame(bytes);
            var payload = new FunctionCallDecoder().DecodeFunctionOutput<AttestationPayload>(hex);
            if (payload.PublicValues.Count is < 1 or > 16 || payload.PublicValues.Any(value => value < 0) ||
                !new ABIEncode().GetABIEncoded(new ABIValue("string", payload.CircuitId),
                    new ABIValue("uint256[]", payload.PublicValues.ToArray())).SequenceEqual(bytes))
                throw new InvalidDataException("The SDK ABI round trip differs from the exact native payload.");
            var revoked = row.GetProperty("revoked").GetBoolean();
            var revokedAt = row.GetProperty("revokeTimestamp");
            if (revoked && revokedAt.ValueKind == JsonValueKind.Null)
                throw new InvalidDataException("A reported revocation requires its native timestamp.");
            var transaction = row.GetProperty("transactionHash").GetString();
            if (transaction is null || !Regex.IsMatch(transaction, "^0x[0-9a-f]{64}$"))
                throw new InvalidDataException("The native transaction identity differs.");
            var fields = new Dictionary<string, object>
            {
                ["name"] = "Human ID attestation " + key,
                ["HolonymNativeId"] = key, ["HolonymSchemaId"] = SchemaId,
                ["HolonymChainId"] = "10", ["HolonymAttester"] = row.GetProperty("attester").GetString()!,
                ["HolonymCircuitIdRaw"] = payload.CircuitId,
                ["HolonymPublicValues"] = payload.PublicValues.Select(value => value.ToString(CultureInfo.InvariantCulture)).ToArray(),
                ["HolonymDataHex"] = hex,
                ["HolonymTransactionHash"] = transaction,
                ["HolonymAttestTimestamp"] = Decimal(row.GetProperty("attestTimestamp")),
                ["HolonymValidUntil"] = Decimal(row.GetProperty("validUntil")),
                ["HolonymRevoked"] = revoked,
                ["HolonymRevokeTimestamp"] = revokedAt.ValueKind == JsonValueKind.Null ? null! : Decimal(revokedAt),
                ["HolonymRecipients"] = row.GetProperty("recipients").EnumerateArray().Select(value => value.GetString() ?? throw new InvalidDataException("A native recipient string is required.")).ToArray()
            };
            // syncAt/lastSyncAt/extra are index maintenance fields, not the
            // attestation. Never turn moving cursors into snapshot contents.
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.HolonymOASIS,
                new NativeRecord(Source, key, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var holon = mapped.Result; holon.CreatedByAvatarId = owner; holon.IsPublic = true;
            holon.IsActive = true; holon.Children = Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey[PType.HolonymOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(holon);
        }

        private static string Decimal(JsonElement element)
        {
            var value = element.GetString();
            if (value is null || value.Length > 78 || !BigInteger.TryParse(value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var number) || number < 0 || number >= (BigInteger.One << 256) ||
                number.ToString(CultureInfo.InvariantCulture) != value)
                throw new InvalidDataException("A canonical unsigned native decimal string is required.");
            return value;
        }

        private static void ValidateFrame(byte[] data)
        {
            int Word(int offset)
            {
                if (offset < 0 || offset + 32 > data.Length || data.AsSpan(offset, 28).ContainsAnyExcept((byte)0))
                    throw new InvalidDataException("ABI allocation bounds differ.");
                var value = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 28, 4));
                if (value > 8192) throw new InvalidDataException("ABI allocation exceeds the record budget.");
                return (int)value;
            }
            if (data.Length % 32 != 0 || Word(0) != 64) throw new InvalidDataException("Canonical dynamic ABI header required.");
            var length = Word(64); var array = Word(32); var end = 96 + length;
            if (length is < 1 or > 256 || array != ((end + 31) / 32) * 32 || array > data.Length ||
                data.AsSpan(end, array - end).ContainsAnyExcept((byte)0))
                throw new InvalidDataException("ABI string framing differs.");
            var count = Word(array);
            if (count is < 1 or > 16 || array + 32 + count * 32 != data.Length)
                throw new InvalidDataException("ABI array framing differs.");
        }

        private static void UniqueFields(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate native JSON field.");
                    UniqueFields(property.Value);
                }
            }
            else if (value.ValueKind == JsonValueKind.Array)
                foreach (var item in value.EnumerateArray()) UniqueFields(item);
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(new NotSupportedException("Use exact attestation CQL."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Attestation scans unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Attestation observations are read-only."));
        public void Dispose() => client.Dispose();
    }

    [FunctionOutput]
    public sealed class AttestationPayload
    {
        [Parameter("string", "circuitId", 1)] public string CircuitId { get; set; } = "";
        [Parameter("uint256[]", "publicValues", 2)] public List<BigInteger> PublicValues { get; set; } = new();
    }
}
