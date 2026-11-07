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

namespace NextGenSoftware.OASIS.API.Providers.ReclaimProtocolOASIS;

/// <summary>Exact deployed Reclaim verifier observations.</summary>
public sealed class ReclaimVerifierQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "reclaim-verifier";
    private const string Probe = "base";
    private sealed record Deployment(Uri Rpc, string ChainId, string Genesis, string Contract, string Semaphore);

    private static readonly Dictionary<string, Deployment> Reviewed = new(StringComparer.Ordinal)
    {
        ["base"] = new(new Uri("https://mainnet.base.org/"), "0x2105",
            "0xf712aa9241cc24369b143cf6dce85f0902a9731e70d66818a3a5845b296c73dd",
            "0x8cdc031d5b7f148ab0435028b16c682c469cefc3", "0xa2bff333d2e5468cf4dc6194eb4b5ddefa2625c0"),
        ["arbitrum"] = new(new Uri("https://arb1.arbitrum.io/rpc"), "0xa4b1",
            "0x7ee576b35482195fc49205cec9af72ce14f003b9ae69f6ba0faef4514be8b442",
            "0x9f0472fd02ca1bc2d6c3a1702803ba822c7c7e91", "0x9c04beb93d3c84f94a89e4102a24475e29084dbe"),
        ["polygon"] = new(new Uri("https://rpc-mainnet.matic.quiknode.pro/"), "0x89",
            "0xa9c28ce2141b56c474f1dc504bee9b01eb1bd7d1a507580d5519d4437a97de1b",
            "0xd6534f52ceb3d0139b915bc0c3278a94687fa5c7", "0xf90085f5fd1a3beb8678623409b3811ecec5f6a5")
    };

    private readonly VerifierBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public ReclaimVerifierQuerySource(Guid owner) : this(owner, handler: null) { }

    internal ReclaimVerifierQuerySource(Guid owner, HttpMessageHandler? handler)
        : this(new VerifierBackend(owner, handler), owner) { }

    private ReclaimVerifierQuerySource(VerifierBackend backend, Guid owner) : base(backend, "reclaim-verifier:")
    {
        this.backend = backend;
        probeId = LogicalId(owner, Probe);
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed Reclaim verifier metadata";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Network);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3);
    }

    public static Guid LogicalId(Guid owner, string network)
    {
        if (owner == Guid.Empty || network is null || !Reviewed.ContainsKey(network))
            throw new ArgumentException("A teaching owner and reviewed Reclaim verifier are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.ReclaimProtocolOASIS, Source, network);
    }

    public PType QueryProviderType => PType.ReclaimProtocolOASIS;
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

    private sealed class VerifierBackend : IKeyValueBackend, IDisposable
    {
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> networks;
        private readonly ReviewedEvmReadClient client;

        public VerifierBackend(Guid owner, HttpMessageHandler? handler)
        {
            this.owner = owner;
            networks = Reviewed.Keys.ToDictionary(network => LogicalId(owner, network));
            client = new ReviewedEvmReadClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!networks.TryGetValue(id, out var network))
                throw new InvalidDataException("The selected ID is outside the reviewed Reclaim verifier catalogue.");

            var deployment = Reviewed[network];
            await client.VerifyChainAsync(deployment.Rpc, deployment.ChainId, deployment.Genesis, token);
            var block = await client.ReadFinalizedBlockAsync(deployment.Rpc, token);
            Task<string> Read(string selector) => client.ReadAbiWordAsync(deployment.Rpc, deployment.Contract, selector, block, token);
            var words = await Task.WhenAll(Read("0x76671808"), Read("0x289989fd"), Read("0xf7da2ac9"));
            if (words[2].AsSpan(2, 24).ContainsAnyExcept('0') ||
                !words[2].AsSpan(26).SequenceEqual(deployment.Semaphore.AsSpan(2)))
                throw new InvalidDataException("The Reclaim verifier's Semaphore address differs from the reviewed deployment.");

            var record = new NativeRecord(Source, network, new Dictionary<string, object>
            {
                ["name"] = network,
                ["ReclaimVerifier"] = deployment.Contract,
                ["ReclaimEvmChainId"] = deployment.ChainId,
                ["ReclaimSemaphore"] = deployment.Semaphore,
                ["ReclaimCurrentEpoch"] = ReadUInt32(words[0]),
                ["ReclaimEpochDurationSeconds"] = ReadUInt32(words[1]),
                ["ReclaimBlockNumber"] = block.Number,
                ["ReclaimBlockHash"] = block.Hash
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ReclaimProtocolOASIS, record, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.Message);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.ReclaimProtocolOASIS] = network;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static long ReadUInt32(string word)
        {
            if (word.AsSpan(2, 56).ContainsAnyExcept('0') ||
                !uint.TryParse(word.AsSpan(58), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException("The Reclaim verifier must return a canonical ABI uint32.");

            return value;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Reclaim verifier CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Reclaim verifier scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Reclaim exposes exact public verifier reads."));
        public void Dispose() => client.Dispose();
    }
}
