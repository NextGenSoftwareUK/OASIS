#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Globalization;
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

namespace NextGenSoftware.OASIS.API.Providers.LayerZeroOASIS;

/// <summary>Exact deployed LayerZero v1 endpoint observations.</summary>
public sealed class LayerZeroV1EndpointQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "layerzero-v1-endpoint";
    private const string Probe = "ethereum";
    private sealed record Deployment(Uri Rpc, string ChainId, string Genesis, string Contract,
        int ContractChainId, int PublishedChainId);

    private static readonly Dictionary<string, Deployment> Reviewed = new(StringComparer.Ordinal)
    {
        ["ethereum"] = new(new Uri("https://ethereum-rpc.publicnode.com/"), "0x1",
            "0xd4e56740f876aef8c010b86a40d5f56745a118d0906a34e69aec8c0db1cb8fa3",
            "0x66a71dcef29a0ffbdbe3c6a460a3b5bc225cd675", 1, 101),
        ["polygon"] = new(new Uri("https://tenderly.rpc.polygon.community/"), "0x89",
            "0xa9c28ce2141b56c474f1dc504bee9b01eb1bd7d1a507580d5519d4437a97de1b",
            "0x3c2269811836af69497e5f486a85d7316753cf62", 9, 109),
        ["arbitrum"] = new(new Uri("https://arb1.arbitrum.io/rpc"), "0xa4b1",
            "0x7ee576b35482195fc49205cec9af72ce14f003b9ae69f6ba0faef4514be8b442",
            "0x3c2269811836af69497e5f486a85d7316753cf62", 10, 110)
    };

    private readonly EndpointBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public LayerZeroV1EndpointQuerySource(Guid owner) : this(owner, handler: null) { }

    internal LayerZeroV1EndpointQuerySource(Guid owner, HttpMessageHandler? handler)
        : this(new EndpointBackend(owner, handler), owner) { }

    private LayerZeroV1EndpointQuerySource(EndpointBackend backend, Guid owner) : base(backend, "layerzero-v1-endpoint:")
    {
        this.backend = backend;
        probeId = LogicalId(owner, Probe);
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed LayerZero v1 endpoint metadata";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Network);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static Guid LogicalId(Guid owner, string network)
    {
        if (owner == Guid.Empty || network is null || !Reviewed.ContainsKey(network))
            throw new ArgumentException("A teaching owner and reviewed LayerZero v1 endpoint are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.LayerZeroOASIS, Source, network);
    }

    public PType QueryProviderType => PType.LayerZeroOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(probeId, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose()
    {
        IsProviderActivated = false;
        backend.Dispose();
    }

    private sealed class EndpointBackend : IKeyValueBackend, IDisposable
    {
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> networks;
        private readonly ReviewedEvmReadClient client;

        public EndpointBackend(Guid owner, HttpMessageHandler? handler)
        {
            this.owner = owner;
            networks = Reviewed.Keys.ToDictionary(network => LogicalId(owner, network));
            client = new ReviewedEvmReadClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!networks.TryGetValue(id, out var network))
                throw new InvalidDataException("The selected ID is outside the reviewed LayerZero v1 endpoint catalogue.");

            var deployment = Reviewed[network];
            await client.VerifyChainAsync(deployment.Rpc, deployment.ChainId, deployment.Genesis, token);
            var block = await client.ReadFinalizedBlockAsync(deployment.Rpc, token);
            Task<string> Read(string selector) => client.ReadAbiWordAsync(deployment.Rpc, deployment.Contract, selector, block, token);
            var words = await Task.WhenAll(Read("0x3408e470"), Read("0x5d9ef01a"), Read("0xfaee093f"));
            var chainId = ReadUInt16(words[0]);
            if (chainId != deployment.ContractChainId)
                throw new InvalidDataException("The LayerZero v1 endpoint chain ID differs from the reviewed deployment.");

            var record = new NativeRecord(Source, network, new Dictionary<string, object>
            {
                ["name"] = network,
                ["LayerZeroV1Contract"] = deployment.Contract,
                ["LayerZeroV1EvmChainId"] = deployment.ChainId,
                ["LayerZeroV1ContractChainId"] = chainId,
                ["LayerZeroV1PublishedChainId"] = deployment.PublishedChainId,
                ["LayerZeroV1DefaultSendVersion"] = ReadUInt16(words[1]),
                ["LayerZeroV1DefaultReceiveVersion"] = ReadUInt16(words[2]),
                ["LayerZeroV1BlockNumber"] = block.Number,
                ["LayerZeroV1BlockHash"] = block.Hash
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.LayerZeroOASIS, record, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.Message);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.LayerZeroOASIS] = network;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static int ReadUInt16(string word)
        {
            if (word.AsSpan(2, 60).ContainsAnyExcept('0') ||
                !ushort.TryParse(word.AsSpan(62), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException("LayerZero v1 must return a canonical ABI uint16.");

            return value;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact LayerZero endpoint CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("LayerZero endpoint scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("LayerZero v1 exposes exact public endpoint reads."));
        public void Dispose() => client.Dispose();
    }
}
