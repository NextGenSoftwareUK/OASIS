#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
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

namespace NextGenSoftware.OASIS.API.Providers.CivicOASIS;

/// <summary>
/// Public network names at each record's RPC-reported finalized Polygon block.
/// Network configuration observations do not verify a person or a Civic Pass.
/// </summary>
public sealed class CivicNetworkQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string GenesisHash = "0xa9c28ce2141b56c474f1dc504bee9b01eb1bd7d1a507580d5519d4437a97de1b";
    public const string GatewayToken = "0xf65b6396df6b7e2d8a6270e3ab6c7bb08baef22e";
    private const string Source = "civic/polygon/network";
    private readonly NetworkBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    public CivicNetworkQuerySource(Guid owner, IEnumerable<int> networks)
        : this(owner, networks, null) { }

    public static CivicNetworkQuerySource ForReviewedTeachingCatalogue(Guid owner) =>
        new(owner, new[] { 1, 4, 10 });

    internal CivicNetworkQuerySource(Guid owner, IEnumerable<int> networks, HttpMessageHandler? handler)
        : this(new NetworkBackend(owner, networks, handler)) { }

    private CivicNetworkQuerySource(NetworkBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Civic network names at per-record finalized Polygon blocks";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3,
            admittedRecordIds: backend.RecordIds);
    }

    public PType QueryProviderType => PType.CivicOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static Guid LogicalId(Guid owner, int networkId)
    {
        if (owner == Guid.Empty || networkId is not (1 or 4 or 10))
            throw new ArgumentException("A source owner and reviewed public network ID are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.CivicOASIS, Source,
            networkId.ToString(CultureInfo.InvariantCulture));
    }

    public static string ReviewedNetworkName(int networkId) => networkId switch
    {
        1 => "tgnuXXNMDLK8dy7Xm1TdeGyc95MDym4bvAQCwcW21Bf",
        4 => "ignREusXmGrscGNUesoU9mxfds9AiYTezUKex2PsZV6",
        10 => "uniqobk8oGh4XBLMqM68K8M2zNu3CdYX7q5go7whQiv",
        _ => throw new ArgumentException("A reviewed public network ID is required.")
    };

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

    private sealed class NetworkBackend : IKeyValueBackend, IDisposable
    {
        private static readonly Uri Endpoint = new("https://polygon-bor-rpc.publicnode.com/");
        private readonly Guid owner;
        private readonly Dictionary<Guid, int> networks;
        private readonly ReviewedEvmReadClient client;
        public Guid Probe { get; }
        public IReadOnlySet<Guid> RecordIds => networks.Keys.ToHashSet();

        public NetworkBackend(Guid owner, IEnumerable<int> configured, HttpMessageHandler? handler)
        {
            if (configured is null) throw new ArgumentNullException(nameof(configured));
            var selected = configured.Take(4).ToArray();
            if (selected.Length is < 1 or > 3 || selected.Distinct().Count() != selected.Length)
                throw new ArgumentException("One to three distinct reviewed registry networks are required.");
            networks = selected.ToDictionary(network => LogicalId(owner, network));
            this.owner = owner;
            Probe = LogicalId(owner, selected[0]);
            client = new(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!networks.TryGetValue(id, out var network))
                throw new InvalidDataException("The ID is outside the configured Civic network catalogue.");
            await client.VerifyChainAsync(Endpoint, "0x89", GenesisHash, token);
            var at = await client.ReadFinalizedBlockAsync(Endpoint, token);
            await client.VerifyCanonicalBlockAsync(Endpoint, at, token);
            var gatewayCode = await client.ReadDeployedCodeAsync(Endpoint, GatewayToken, at, token);
            var key = network.ToString(CultureInfo.InvariantCulture);
            var networkName = await client.ReadAbiStringAsync(Endpoint, GatewayToken,
                "0x95ea5717" + network.ToString("x64", CultureInfo.InvariantCulture), at, 128, token);
            if (networkName != ReviewedNetworkName(network))
                throw new InvalidDataException("The network name differs from the reviewed public catalogue.");
            var fields = new Dictionary<string, object>
            {
                ["name"] = "Civic network " + key + " at " + at.Number,
                ["CivicNetworkId"] = key,
                ["CivicChainId"] = "137",
                ["CivicGenesisHash"] = GenesisHash,
                ["CivicBlockNumber"] = at.Number,
                ["CivicBlockHash"] = at.Hash,
                ["CivicGatewayToken"] = GatewayToken,
                ["CivicNetworkName"] = networkName,
                ["CivicGatewayCodeSha256"] = CodeHash(gatewayCode)
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.CivicOASIS,
                new NativeRecord(Source, key, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.CivicOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static string CodeHash(string code) =>
            Convert.ToHexStringLower(SHA256.HashData(Convert.FromHexString(code[2..])));

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Civic network CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Civic network scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Civic network observations are read-only."));
        public void Dispose() => client.Dispose();
    }
}
