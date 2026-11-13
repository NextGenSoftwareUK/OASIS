#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
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

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS;

/// <summary>
/// Public token names and symbols at each record's RPC-reported finalized Polygon block.
/// Contract metadata observations do not read personal holdings or prove ownership.
/// </summary>
public sealed class SandboxContractQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string GenesisHash = "0xa9c28ce2141b56c474f1dc504bee9b01eb1bd7d1a507580d5519d4437a97de1b";
    private const string Source = "sandbox/polygon/contracts";
    private readonly ContractBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    public SandboxContractQuerySource(Guid owner, IEnumerable<string> contracts)
        : this(owner, contracts, null) { }

    public static SandboxContractQuerySource ForReviewedTeachingCatalogue(Guid owner) =>
        new(owner, new[] { "land", "sand", "asset" });

    internal SandboxContractQuerySource(Guid owner, IEnumerable<string> contracts, HttpMessageHandler? handler)
        : this(new ContractBackend(owner, contracts, handler)) { }

    private SandboxContractQuerySource(ContractBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Sandbox contract metadata at per-record finalized Polygon blocks";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Map);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3,
            admittedRecordIds: backend.RecordIds);
    }

    public PType QueryProviderType => PType.TheSandboxOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static Guid LogicalId(Guid owner, string key)
    {
        if (owner == Guid.Empty) throw new ArgumentException("A source owner is required.");
        _ = ContractAddress(key);
        return NativeRecordHolonMapper.StableHolonId(owner, PType.TheSandboxOASIS, Source, key);
    }

    public static string ContractAddress(string key) => key switch
    {
        "land" => "0x9d305a42a3975ee4c1c57555bed5919889dce63f",
        "sand" => "0xbbba073c31bf03b8acf7c28ef0738decf3695683",
        "asset" => "0xdbc52cd5b8eda1a7bcbabb838ca927d23e3673e5",
        _ => throw new ArgumentException("A reviewed public contract key is required.")
    };

    public static string ReviewedName(string key) => key switch
    {
        "land" => "Sandbox's LANDs", "sand" => "SAND", "asset" => "The Sandbox's ASSETs",
        _ => throw new ArgumentException("A reviewed public contract key is required.")
    };

    public static string ReviewedSymbol(string key) => key switch
    {
        "land" => "LAND", "sand" => "SAND", "asset" => "ASSET",
        _ => throw new ArgumentException("A reviewed public contract key is required.")
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

    private sealed class ContractBackend : IKeyValueBackend, IDisposable
    {
        private static readonly Uri Endpoint = new("https://polygon-bor-rpc.publicnode.com/");
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> contracts;
        private readonly ReviewedEvmReadClient client;
        public Guid Probe { get; }
        public IReadOnlySet<Guid> RecordIds => contracts.Keys.ToHashSet();

        public ContractBackend(Guid owner, IEnumerable<string> configured, HttpMessageHandler? handler)
        {
            if (configured is null) throw new ArgumentNullException(nameof(configured));
            var selected = configured.Take(4).ToArray();
            if (selected.Length is < 1 or > 3 || selected.Distinct().Count() != selected.Length)
                throw new ArgumentException("One to three distinct reviewed Sandbox contract keys are required.");
            contracts = selected.ToDictionary(contractKey => LogicalId(owner, contractKey));
            this.owner = owner;
            Probe = LogicalId(owner, selected[0]);
            client = new(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!contracts.TryGetValue(id, out var contractKey))
                throw new InvalidDataException("The ID is outside the configured Sandbox contract catalogue.");
            await client.VerifyChainAsync(Endpoint, "0x89", GenesisHash, token);
            var at = await client.ReadFinalizedBlockAsync(Endpoint, token);
            await client.VerifyCanonicalBlockAsync(Endpoint, at, token);
            var address = ContractAddress(contractKey);
            var code = await client.ReadDeployedCodeAsync(Endpoint, address, at, token);
            var name = await client.ReadAbiStringAsync(Endpoint, address, "0x06fdde03", at, 128, token);
            var symbol = await client.ReadAbiStringAsync(Endpoint, address, "0x95d89b41", at, 32, token);
            if (name != ReviewedName(contractKey) || symbol != ReviewedSymbol(contractKey))
                throw new InvalidDataException("The contract metadata differs from the reviewed public catalogue.");
            var key = contractKey;
            var fields = new Dictionary<string, object>
            {
                ["name"] = "Sandbox " + symbol + " at " + at.Number,
                ["SandboxContractKey"] = key,
                ["SandboxChainId"] = "137",
                ["SandboxGenesisHash"] = GenesisHash,
                ["SandboxBlockNumber"] = at.Number,
                ["SandboxBlockHash"] = at.Hash,
                ["SandboxContractAddress"] = address,
                ["SandboxTokenName"] = name,
                ["SandboxTokenSymbol"] = symbol,
                ["SandboxContractCodeSha256"] = CodeHash(code)
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.TheSandboxOASIS,
                new NativeRecord(Source, key, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.TheSandboxOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static string CodeHash(string code) =>
            Convert.ToHexStringLower(SHA256.HashData(Convert.FromHexString(code[2..])));

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Sandbox contract CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Sandbox contract scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Sandbox contract observations are read-only."));
        public void Dispose() => client.Dispose();
    }
}
