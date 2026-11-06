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

namespace NextGenSoftware.OASIS.API.Providers.ProofOfHumanityOASIS;

/// <summary>Public registry observations at one finalized block on each reviewed chain.</summary>
public sealed class ProofOfHumanityRegistryQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "proof-of-humanity-v2/cross-chain";
    private readonly RegistryBackend backend;
    private readonly string probe;

    public ProofOfHumanityRegistryQuerySource(Guid owner, IEnumerable<string> addresses, string probe)
        : this(CreateBackend(owner, addresses, probe, null), probe) { }

    internal ProofOfHumanityRegistryQuerySource(Guid owner, IEnumerable<string> addresses, string probe, HttpMessageHandler transport)
        : this(CreateBackend(owner, addresses, probe, transport), probe) { }

    private ProofOfHumanityRegistryQuerySource(RegistryBackend backend, string probe) : base(backend, "proof-of-humanity:")
    {
        this.backend = backend;
        this.probe = probe;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Proof of Humanity v2 cross-chain registry observations";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Identity);
    }

    public static Guid LogicalId(Guid owner, string address)
    {
        if (!ProofOfHumanityPublicRegistryReader.IsCanonicalAddress(address))
            throw new ArgumentException("A canonical public registry address is required.", nameof(address));
        return NativeRecordHolonMapper.StableHolonId(owner, PType.ProofOfHumanityOASIS, Source, address);
    }

    private static RegistryBackend CreateBackend(Guid owner, IEnumerable<string> addresses, string probe, HttpMessageHandler? transport)
    {
        var catalogue = addresses?.Take(9).ToArray() ?? Array.Empty<string>();
        if (owner == Guid.Empty || catalogue.Length is < 1 or > 8 ||
            catalogue.Any(address => !ProofOfHumanityPublicRegistryReader.IsCanonicalAddress(address)) ||
            catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length || !catalogue.Contains(probe, StringComparer.Ordinal))
            throw new ArgumentException("A source owner, bounded public registry catalogue and catalogue probe are required.");
        return new RegistryBackend(owner, catalogue, new ProofOfHumanityPublicRegistryReader(transport));
    }

    public PType QueryProviderType => PType.ProofOfHumanityOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        try
        {
            await backend.ReadAsync(new[] { probe }, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return Failure<bool>(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE",
                "The public registry probe could not be verified on both chains.", error);
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
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Proof of Humanity target is required.");
            if (maximumResults is < 1 or > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            if (IsVersionControlEnabled)
                return Error("CQL_CAPABILITY_UNSUPPORTED", "Historical registry selection is unavailable.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the reviewed registry source first.");
            var addresses = selection.Result.Select(backend.AddressFor).ToArray();
            var rows = addresses.Length == 0 ? Array.Empty<IHolon>() : await backend.ReadAsync(addresses, cancellationToken);
            if (rows.Any(row => !plan.Query.ReadAccess.CanRead(row)))
                throw new InvalidDataException("The public registry rows differ from the admitted authority.");
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Length).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (Exception error)
        {
            return Error(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_FAILED",
                "The native registry read failed without publishing partial rows.", error);
        }
    }

    private static OASISResult<T> Failure<T>(string code, string message, Exception? error) =>
        new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public void Dispose()
    {
        IsProviderActivated = false;
        backend.Dispose();
    }

    private sealed class RegistryBackend(Guid owner, IEnumerable<string> catalogue, ProofOfHumanityPublicRegistryReader reader)
        : IKeyValueBackend, IDisposable
    {
        private readonly Dictionary<Guid, string> addresses = catalogue.ToDictionary(address => LogicalId(owner, address));

        public string AddressFor(Guid id) => addresses.TryGetValue(id, out var address)
            ? address : throw new InvalidDataException("The selected ID is outside the public registry catalogue.");

        public async Task<IHolon[]> ReadAsync(IReadOnlyList<string> selected, CancellationToken token)
        {
            var snapshots = await reader.ReadAsync(selected, token);
            var byChain = snapshots.ToDictionary(snapshot => snapshot.Chain, StringComparer.Ordinal);
            return selected.Select(address => Map(address, byChain["ethereum"], byChain["gnosis"])).ToArray();
        }

        private IHolon Map(string address, ProofOfHumanityPublicRegistryReader.Snapshot ethereum,
            ProofOfHumanityPublicRegistryReader.Snapshot gnosis)
        {
            var fields = new Dictionary<string, object>
            {
                ["name"] = address,
                ["description"] = "Proof of Humanity v2 registry observations on Ethereum and Gnosis",
                ["ProofOfHumanityAddress"] = address,
                ["ProofOfHumanityEthereumReported"] = ethereum.Records[address],
                ["ProofOfHumanityGnosisReported"] = gnosis.Records[address],
                ["ProofOfHumanityEthereumBlock"] = ethereum.BlockNumber,
                ["ProofOfHumanityEthereumBlockHash"] = ethereum.BlockHash,
                ["ProofOfHumanityGnosisBlock"] = gnosis.BlockNumber,
                ["ProofOfHumanityGnosisBlockHash"] = gnosis.BlockHash,
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ProofOfHumanityOASIS,
                new NativeRecord(Source, address, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.ProofOfHumanityOASIS] = address;
            return row;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted registry CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Public registry scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("The registry profile exposes exact CQL reads."));
        public void Dispose() => reader.Dispose();
    }
}
