using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public interface ICqlHostedSourceRegistry
{
    IOASISStorageProvider Find(ProviderType provider);
    Task ActivateReadSourceAsync(ProviderType provider, CancellationToken cancellationToken);
}

public sealed record CqlHostedSourceDescription(string Provider, bool QueryOnly, bool Available,
    bool FederationAvailable, string Features, string ErrorCode, IReadOnlyList<CqlHostedQueryExample> Examples = null,
    long ActivationGeneration = 0, Guid ActivationHostInstance = default);

public sealed record CqlSourceRecoveryResult(string Provider, Guid OperationId,
    long Generation, bool Available, string ErrorCode, Guid HostInstanceId);

/// <summary>Host-owned enrollment. Availability is observed from actual active adapters;
/// a configuration entry is never represented as a working native source.</summary>
public sealed class CqlHostedSources : BackgroundService
{
    private readonly ICqlHostedSourceRegistry registry;
    private readonly ProviderType[] providers;
    private readonly HashSet<ProviderType> queryOnly;
    private readonly ConcurrentDictionary<ProviderType, string> failures = new();
    private readonly ConcurrentDictionary<ProviderType, bool> verified = new();
    private readonly ILogger<CqlHostedSources> logger;
    private readonly CqlHostedQueryExamples examples;
    private readonly IReadOnlyDictionary<ProviderType, CqlPublicReadCatalogue> publicCatalogues;
    private sealed class ActivationState
    {
        internal readonly SemaphoreSlim Gate = new(1, 1);
        internal long Generation;
        internal CqlSourceRecoveryResult LastRecovery;
    }
    private readonly IReadOnlyDictionary<ProviderType, ActivationState> activationStates;
    private CancellationToken hostStoppingToken;
    public Guid HostInstanceId { get; } = Guid.NewGuid();

    public CqlHostedSources(PricedStorageAdmissionFactory admission, ICqlHostedSourceRegistry registry,
        ILogger<CqlHostedSources> logger, CqlHostedQueryExamples examples = null,
        IEnumerable<CqlPublicReadCatalogue> publicCatalogues = null)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.examples = examples ?? CqlHostedQueryExamples.FromJson(null);
        this.publicCatalogues = (publicCatalogues ?? Array.Empty<CqlPublicReadCatalogue>()).ToDictionary(policy => policy.Provider);
        providers = admission?.QueryProviders.ToArray() ?? Array.Empty<ProviderType>();
        queryOnly = new HashSet<ProviderType>(admission?.QueryOnlyProviders ?? Array.Empty<ProviderType>());
        activationStates = queryOnly.ToDictionary(type => type, _ => new ActivationState());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        hostStoppingToken = stoppingToken;
        foreach (var provider in queryOnly.OrderBy(type => type.ToString(), StringComparer.Ordinal))
        {
            if (stoppingToken.IsCancellationRequested) break;
            var state = activationStates[provider];
            await state.Gate.WaitAsync(stoppingToken);
            try
            {
                await ActivateOneAsync(provider, state, stoppingToken);
                if (stoppingToken.IsCancellationRequested) break;
            }
            finally { state.Gate.Release(); }
            // Optional source failure is explicit in discovery; never select another provider.
        }
    }

    private async Task<string> ActivateOneAsync(ProviderType provider, ActivationState state, CancellationToken token)
    {
        // Keep discovery unavailable until the complete owning attempt finishes,
        // even if an adapter retained IsProviderActivated from an earlier attempt.
        verified.TryRemove(provider, out _);
        failures[provider] = "CQL_SOURCE_NOT_READY";
        var started = Stopwatch.GetTimestamp();
        try
        {
            await registry.ActivateReadSourceAsync(provider, token);
            token.ThrowIfCancellationRequested();
            verified[provider] = true;
            failures.TryRemove(provider, out _);
            if (ReadyAdapter(provider) == null)
                throw new InvalidOperationException("CQL_SOURCE_ADAPTER_INVALID");
            return null;
        }
        catch (Exception cause)
        {
            verified.TryRemove(provider, out _);
            failures[provider] = "CQL_SOURCE_UNAVAILABLE";
            CqlNativeFailureDiagnostics.Record(logger, provider, true, cause,
                (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return cause is OperationCanceledException && token.IsCancellationRequested
                ? "CQL_SOURCE_RECOVERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE";
        }
        finally { Interlocked.Increment(ref state.Generation); }
    }

    /// <summary>Explicit operator action on the same enrolled read-only source.
    /// Never called by discovery/query execution. Await native ownership, including
    /// cancellation; a lost HTTP response must not start a parallel activation.</summary>
    public async Task<OASISResult<CqlSourceRecoveryResult>> RecoverReadSourceAsync(
        ProviderType provider, Guid operationId, long expectedGeneration, Guid expectedHostInstance, CancellationToken token)
    {
        OASISResult<CqlSourceRecoveryResult> Error(string code) => new()
            { IsError = true, ErrorCount = 1, ErrorCode = code };
        OASISResult<CqlSourceRecoveryResult> Acknowledged(CqlSourceRecoveryResult result) => new(result)
            { IsError = !result.Available, ErrorCount = result.Available ? 0 : 1, ErrorCode = result.ErrorCode };
        if (operationId == Guid.Empty || expectedGeneration < 0)
            return Error("CQL_SOURCE_RECOVERY_REQUEST_INVALID");
        // Receipts are process-scoped. A request from an earlier host must never
        // silently become a fresh attempt after a restart resets generations.
        if (expectedHostInstance != HostInstanceId)
            return Error("CQL_SOURCE_HOST_INSTANCE_CONFLICT");
        if (!activationStates.TryGetValue(provider, out var state))
            return Error("CQL_SOURCE_RECOVERY_NOT_ENROLLED");
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, hostStoppingToken);
        token = lifetime.Token;
        try
        {
            if (!await state.Gate.WaitAsync(0, token)) return Error("CQL_SOURCE_RECOVERY_BUSY");
        }
        catch (OperationCanceledException) { return Error("CQL_SOURCE_RECOVERY_CANCELLED"); }
        try
        {
            // One bounded receipt per source plus a monotonically increasing
            // generation fences old/reordered requests without unbounded memory.
            if (state.LastRecovery?.OperationId == operationId)
                return expectedGeneration == state.LastRecovery.Generation - 1
                    ? Acknowledged(state.LastRecovery) : Error("CQL_SOURCE_OPERATION_CONFLICT");
            if (Interlocked.Read(ref state.Generation) != expectedGeneration)
                return Error("CQL_SOURCE_GENERATION_CONFLICT");
            if (!failures.TryGetValue(provider, out var failure) || failure != "CQL_SOURCE_UNAVAILABLE")
                return Error("CQL_SOURCE_RECOVERY_NOT_FAILED");
            var code = await ActivateOneAsync(provider, state, token);
            var receipt = new CqlSourceRecoveryResult(provider.ToString(), operationId,
                Interlocked.Read(ref state.Generation), code == null, code, HostInstanceId);
            state.LastRecovery = receipt;
            logger.LogInformation(new EventId(4833, "CqlSourceRecovery"),
                "CQL source recovery provider={Provider} operation={Operation} generation={Generation} available={Available}",
                provider, operationId, receipt.Generation, receipt.Available);
            return Acknowledged(receipt);
        }
        finally { state.Gate.Release(); }
    }

    public IReadOnlyList<CqlHostedSourceDescription> Describe(bool includeFederationExamples = false)
        => providers.Select(type =>
        {
            if (failures.TryGetValue(type, out var failure))
                return new CqlHostedSourceDescription(type.ToString(), queryOnly.Contains(type), false, false, null, failure);
            var adapter = ReadyAdapter(type);
            var available = adapter != null;
            IReadOnlyList<CqlHostedQueryExample> selected = Array.Empty<CqlHostedQueryExample>();
            if (available)
            {
                try { selected = examples.For(type, adapter.QueryCapabilities,
                    target => ReadyAdapter(target)?.QueryCapabilities,
                    (target, actor) => publicCatalogues.TryGetValue(target, out var catalogue)
                        ? catalogue.Authorize(actor, target).Result : CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                    .Where(example => includeFederationExamples || example.Operation == "Rows").ToArray(); }
                catch (ArgumentException)
                {
                    return new CqlHostedSourceDescription(type.ToString(), queryOnly.Contains(type), false,
                        false, null, "CQL_DEMO_EXAMPLE_UNSUPPORTED");
                }
            }
            return new CqlHostedSourceDescription(type.ToString(), queryOnly.Contains(type), available,
                available && CqlV1Compiler.SupportedReadProfile(type), available ? adapter.QueryCapabilities.Features.ToString() : null,
                available ? null : "CQL_SOURCE_NOT_READY", selected);
        }).Select(row => row with { ActivationHostInstance = HostInstanceId, ActivationGeneration =
            Enum.TryParse<ProviderType>(row.Provider, out var type) && activationStates.TryGetValue(type, out var state)
                ? Interlocked.Read(ref state.Generation) : 0 }).ToArray();

    private ICosmicQueryStorageProvider ReadyAdapter(ProviderType type)
    {
        if (!providers.Contains(type) || failures.ContainsKey(type)
            || queryOnly.Contains(type) && !verified.ContainsKey(type)) return null;
        var storage = registry.Find(type);
        var adapter = storage as ICosmicQueryStorageProvider;
        return storage?.IsProviderActivated == true && storage.ProviderType?.Value == type
            && adapter?.QueryProviderType == type && adapter.QueryCapabilities != null
            && CqlV1Compiler.SupportedReadProfile(type) ? adapter : null;
    }
}
