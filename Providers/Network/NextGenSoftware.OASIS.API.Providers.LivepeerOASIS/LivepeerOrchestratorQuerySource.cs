#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.LivepeerOASIS;

/// <summary>Reviewed public orchestrator statuses at one finalized Arbitrum One block per query.</summary>
public sealed class LivepeerOrchestratorQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "livepeer-bonding-manager/arbitrum-one";
    private static readonly HashSet<string> Reviewed = new(StringComparer.Ordinal)
    {
        "0x525419ff5707190389bfb5c87c375d710f5fcb0e",
        "0xac2e50c8f7ac0f82923a7df9d9903f6ec4741919",
        "0x4f4758f7167b18e1f5b3c1a7575e3eb584894dbc"
    };
    private readonly OrchestratorBackend backend;
    private readonly string probe;

    public LivepeerOrchestratorQuerySource(Guid owner, IEnumerable<string> addresses, string probe)
        : this(CreateBackend(owner, addresses, probe, null), probe) { }

    internal LivepeerOrchestratorQuerySource(Guid owner, IEnumerable<string> addresses, string probe, HttpMessageHandler transport)
        : this(CreateBackend(owner, addresses, probe, transport), probe) { }

    private LivepeerOrchestratorQuerySource(OrchestratorBackend backend, string probe) : base(backend, "livepeer-orchestrator:")
    {
        this.backend = backend;
        this.probe = probe;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Livepeer BondingManager orchestrator statuses";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Network);
    }

    private static bool IsReviewedAddress(string? address) => address != null && Reviewed.Contains(address);

    public static Guid LogicalId(Guid owner, string address)
    {
        if (owner == Guid.Empty || !IsReviewedAddress(address))
            throw new ArgumentException("A reviewed Livepeer orchestrator address is required.", nameof(address));
        return NativeRecordHolonMapper.StableHolonId(owner, PType.LivepeerOASIS, Source, address);
    }

    private static OrchestratorBackend CreateBackend(Guid owner, IEnumerable<string> addresses, string probe, HttpMessageHandler? transport)
    {
        var catalogue = addresses?.Take(Reviewed.Count + 1).ToArray() ?? Array.Empty<string>();
        if (owner == Guid.Empty || catalogue.Length == 0 || catalogue.Length > Reviewed.Count ||
            catalogue.Any(address => !IsReviewedAddress(address)) ||
            catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length || !catalogue.Contains(probe, StringComparer.Ordinal))
            throw new ArgumentException("A source owner, bounded public orchestrator catalogue and catalogue probe are required.");
        return new OrchestratorBackend(owner, catalogue, new LivepeerBondingManagerReader(transport));
    }

    public PType QueryProviderType => PType.LivepeerOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(20));
            await backend.VerifyChainAsync(deadline.Token);
            await backend.ReadAsync(new[] { probe }, deadline.Token);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return Failure<bool>(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE",
                "The public Livepeer BondingManager probe could not be verified.", error);
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? error = null) =>
            Failure<IReadOnlyList<IHolon>>(code, message, error);

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Livepeer target is required.");
            if (maximumResults is < 1 or > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            if (IsVersionControlEnabled)
                return Error("CQL_CAPABILITY_UNSUPPORTED", "Historical orchestrator selection is unavailable.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the reviewed Livepeer source first.");
            var addresses = selection.Result.Select(backend.AddressFor).ToArray();
            var rows = addresses.Length == 0 ? Array.Empty<IHolon>() : await backend.ReadAsync(addresses, cancellationToken);
            if (rows.Any(row => !plan.Query.ReadAccess.CanRead(row)))
                throw new InvalidDataException("The public orchestrator rows differ from the admitted authority.");
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Length).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (Exception error)
        {
            return Error(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_FAILED",
                "The native Livepeer read failed without publishing partial rows.", error);
        }
    }

    private static OASISResult<T> Failure<T>(string code, string message, Exception? error) =>
        new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public void Dispose()
    {
        IsProviderActivated = false;
        backend.Dispose();
    }

    private sealed class OrchestratorBackend(Guid owner, IEnumerable<string> catalogue, LivepeerBondingManagerReader reader)
        : IKeyValueBackend, IDisposable
    {
        private readonly Dictionary<Guid, string> addresses = catalogue.ToDictionary(address => LogicalId(owner, address));

        public string AddressFor(Guid id) => addresses.TryGetValue(id, out var address)
            ? address : throw new InvalidDataException("The selected ID is outside the public orchestrator catalogue.");

        public Task VerifyChainAsync(CancellationToken token) => reader.VerifyChainAsync(token);

        public async Task<IHolon[]> ReadAsync(IReadOnlyList<string> selected, CancellationToken token)
        {
            var observations = await reader.ReadAsync(selected, token);
            return observations.Select(Map).ToArray();
        }

        private IHolon Map(LivepeerBondingManagerReader.Observation observation)
        {
            var fields = new Dictionary<string, object>
            {
                ["name"] = observation.Address,
                ["description"] = "Livepeer BondingManager observation on Arbitrum One",
                ["LivepeerOrchestratorAddress"] = observation.Address,
                ["LivepeerOrchestratorActive"] = observation.IsActive,
                ["LivepeerArbitrumBlock"] = observation.BlockNumber,
                ["LivepeerArbitrumBlockHash"] = observation.BlockHash
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.LivepeerOASIS,
                new NativeRecord(Source, observation.Address, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.LivepeerOASIS] = observation.Address;
            return row;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted orchestrator CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Public orchestrator scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("The orchestrator profile exposes exact CQL reads."));
        public void Dispose() => reader.Dispose();
    }
}
