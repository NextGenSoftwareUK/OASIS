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

namespace NextGenSoftware.OASIS.API.Providers.WorldIDOASIS;

/// <summary>Reviewed public World ID v4 protocol configuration at one finalized World Chain block.</summary>
public sealed class WorldIdProtocolQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "world-id-v4-protocol/world-chain";
    private const string Probe = "registry";
    private readonly ComponentBackend backend;

    public WorldIdProtocolQuerySource(Guid owner) : this(owner, handler: null) { }

    internal WorldIdProtocolQuerySource(Guid owner, HttpMessageHandler? handler)
        : this(CreateBackend(owner, handler)) { }

    private WorldIdProtocolQuerySource(ComponentBackend backend) : base(backend, "world-id-protocol:")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed World ID v4 protocol component metadata";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Identity);
    }

    public static Guid LogicalId(Guid owner, string component)
    {
        if (owner == Guid.Empty || !WorldIdProtocolComponentReader.IsReviewedComponent(component))
            throw new ArgumentException("A teaching owner and reviewed World ID v4 component are required.");

        return NativeRecordHolonMapper.StableHolonId(owner, PType.WorldIDOASIS, Source, component);
    }

    private static ComponentBackend CreateBackend(Guid owner, HttpMessageHandler? handler)
    {
        if (owner == Guid.Empty) throw new ArgumentException("A teaching source owner is required.", nameof(owner));
        return new ComponentBackend(owner, new WorldIdProtocolComponentReader(handler));
    }

    public PType QueryProviderType => PType.WorldIDOASIS;
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
            await backend.ReadAsync(new[] { Probe }, deadline.Token);
            deadline.Token.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return Failure<bool>(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE",
                "The World ID v4 registry probe could not be verified.", error);
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string? code, string message, Exception? error = null) =>
            Failure<IReadOnlyList<IHolon>>(code, message, error);

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit World ID target is required.");
            if (maximumResults is < 1 or > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            if (IsVersionControlEnabled)
                return Error("CQL_CAPABILITY_UNSUPPORTED", "Historical protocol component selection is unavailable.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the reviewed World ID source first.");
            var components = selection.Result.Select(backend.ComponentFor).ToArray();
            var rows = components.Length == 0 ? Array.Empty<IHolon>() : await backend.ReadAsync(components, cancellationToken);
            if (rows.Any(row => !plan.Query.ReadAccess.CanRead(row)))
                throw new InvalidDataException("The public protocol component rows differ from the admitted authority.");
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Length).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (Exception error)
        {
            return Error(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_FAILED",
                "The native World ID read failed without publishing partial rows.", error);
        }
    }

    private static OASISResult<T> Failure<T>(string? code, string message, Exception? error) =>
        new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public void Dispose()
    {
        IsProviderActivated = false;
        backend.Dispose();
    }

    private sealed class ComponentBackend(Guid owner, WorldIdProtocolComponentReader reader)
        : IKeyValueBackend, IDisposable
    {
        private readonly Dictionary<Guid, string> components =
            WorldIdProtocolComponentReader.Components.ToDictionary(component => LogicalId(owner, component));

        public string ComponentFor(Guid id) => components.TryGetValue(id, out var component)
            ? component : throw new InvalidDataException("The selected ID is outside the reviewed World ID catalogue.");

        public Task VerifyChainAsync(CancellationToken token) => reader.VerifyChainAsync(token);

        public async Task<IHolon[]> ReadAsync(IReadOnlyList<string> selected, CancellationToken token)
        {
            var observations = await reader.ReadAsync(selected, token);
            return observations.Select(Map).ToArray();
        }

        private IHolon Map(WorldIdProtocolComponentReader.Observation observation)
        {
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.WorldIDOASIS,
                new NativeRecord(Source, observation.Component, observation.Fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.Message);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.WorldIDOASIS] = observation.Component;
            return row;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted protocol component CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Public protocol component scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("The protocol component profile exposes exact CQL reads."));
        public void Dispose() => reader.Dispose();
    }
}
