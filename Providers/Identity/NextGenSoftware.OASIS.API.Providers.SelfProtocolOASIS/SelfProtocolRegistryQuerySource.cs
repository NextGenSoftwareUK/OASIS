#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Security.Cryptography;
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

namespace NextGenSoftware.OASIS.API.Providers.SelfProtocolOASIS;

/// <summary>
/// Public registry roots at one reviewed, RPC-reported finalized Celo block.
/// Registry observations do not verify a person, proof, document or OFAC status.
/// </summary>
public sealed class SelfProtocolRegistryQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string GenesisHash = "0x19ea3339d3c8cda97235bc8293240d5b9dadcdfbb5d4b0b90ee731cac1bd11c3";
    public const string BlockNumber = "0x4e66733";
    public const string BlockHash = "0xe8e3a373b127a614a3df09c20e23085c2881804d05aea7f3122039f08f7586ec";
    public const string Hub = "0xe57f4773bd9c9d8b6cd70431117d353298b9f5bf";
    private const string Source = "self-protocol/celo/registry-root/" + BlockHash;
    private readonly RegistryBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    public SelfProtocolRegistryQuerySource(Guid owner, IEnumerable<int> attestations)
        : this(owner, attestations, null) { }

    public static SelfProtocolRegistryQuerySource ForReviewedTeachingCatalogue(Guid owner) =>
        new(owner, new[] { 1, 3, 4 });

    internal SelfProtocolRegistryQuerySource(Guid owner, IEnumerable<int> attestations, HttpMessageHandler? handler)
        : this(new RegistryBackend(owner, attestations, handler)) { }

    private SelfProtocolRegistryQuerySource(RegistryBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Self Protocol registry roots at a reviewed Celo block";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3,
            admittedRecordIds: backend.RecordIds);
    }

    public PType QueryProviderType => PType.SelfProtocolOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static Guid LogicalId(Guid owner, int attestationId)
    {
        if (owner == Guid.Empty || attestationId is not (1 or 3 or 4))
            throw new ArgumentException("A source owner and reviewed public registry attestation are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.SelfProtocolOASIS, Source,
            attestationId.ToString(CultureInfo.InvariantCulture));
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

    private sealed class RegistryBackend : IKeyValueBackend, IDisposable
    {
        private static readonly Uri Endpoint = new("https://forno.celo.org");
        private static readonly ReviewedEvmReadClient.Block At = new(BlockNumber, BlockHash);
        private readonly Guid owner;
        private readonly Dictionary<Guid, int> attestations;
        private readonly ReviewedEvmReadClient client;
        public Guid Probe { get; }
        public IReadOnlySet<Guid> RecordIds => attestations.Keys.ToHashSet();

        public RegistryBackend(Guid owner, IEnumerable<int> configured, HttpMessageHandler? handler)
        {
            if (configured is null) throw new ArgumentNullException(nameof(configured));
            var selected = configured.Take(4).ToArray();
            if (selected.Length is < 1 or > 3 || selected.Distinct().Count() != selected.Length)
                throw new ArgumentException("One to three distinct reviewed registry attestations are required.");
            attestations = selected.ToDictionary(attestation => LogicalId(owner, attestation));
            this.owner = owner;
            Probe = LogicalId(owner, selected[0]);
            client = new(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!attestations.TryGetValue(id, out var attestation))
                throw new InvalidDataException("The ID is outside the configured Self registry catalogue.");
            await client.VerifyChainAsync(Endpoint, "0xa4ec", GenesisHash, token);
            await client.VerifyCanonicalBlockAsync(Endpoint, At, token);
            var hubCode = await client.ReadDeployedCodeAsync(Endpoint, Hub, At, token);
            var key = attestation.ToString(CultureInfo.InvariantCulture);
            // ABI bytes32 is the right-aligned attestation integer, matching
            // the existing proof verifier and the documented hub contract.
            var addressWord = await client.ReadAbiWordAsync(Endpoint, Hub,
                "0x7ef50298" + attestation.ToString("x64", CultureInfo.InvariantCulture), At, token);
            if (addressWord.AsSpan(2, 24).ContainsAnyExcept('0') || !addressWord.AsSpan(26).ContainsAnyExcept('0'))
                throw new InvalidDataException("The hub must resolve a nonzero canonical registry address.");
            var registry = "0x" + addressWord[26..];
            var registryCode = await client.ReadDeployedCodeAsync(Endpoint, registry, At, token);
            var root = await client.ReadAbiWordAsync(Endpoint, registry, "0x4bf4dc7a", At, token);
            var rootValue = new BigInteger(Convert.FromHexString(root[2..]), isUnsigned: true, isBigEndian: true);
            var fields = new Dictionary<string, object>
            {
                ["name"] = "Self registry " + key + " at " + BlockNumber,
                ["SelfProtocolAttestationId"] = key,
                ["SelfProtocolChainId"] = "42220",
                ["SelfProtocolGenesisHash"] = GenesisHash,
                ["SelfProtocolBlockNumber"] = BlockNumber,
                ["SelfProtocolBlockHash"] = BlockHash,
                ["SelfProtocolHub"] = Hub,
                ["SelfProtocolRegistry"] = registry,
                ["SelfProtocolRootHex"] = root,
                ["SelfProtocolRootDecimal"] = rootValue.ToString(CultureInfo.InvariantCulture),
                ["SelfProtocolHubCodeSha256"] = CodeHash(hubCode),
                ["SelfProtocolRegistryCodeSha256"] = CodeHash(registryCode)
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.SelfProtocolOASIS,
                new NativeRecord(Source, key, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.SelfProtocolOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static string CodeHash(string code) =>
            Convert.ToHexStringLower(SHA256.HashData(Convert.FromHexString(code[2..])));

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Self registry CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Self registry scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Self registry observations are read-only."));
        public void Dispose() => client.Dispose();
    }
}
