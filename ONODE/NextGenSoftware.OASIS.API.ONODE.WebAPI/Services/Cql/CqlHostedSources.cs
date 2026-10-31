using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public interface ICqlHostedSourceRegistry
{
    IOASISStorageProvider Find(ProviderType provider);
    Task ActivateReadSourceAsync(ProviderType provider, CancellationToken cancellationToken);
}

/// <summary>Registers explicitly enrolled CQL-only sources without switching the default
/// provider or editing replication/failover lists. Registry owns provider lifetime.</summary>
public sealed class CqlHostedSourceRegistry : ICqlHostedSourceRegistry
{
    public IOASISStorageProvider Find(ProviderType provider)
        => ProviderManager.Instance.GetStorageProvider(provider);

    public async Task ActivateReadSourceAsync(ProviderType provider, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var registration = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(provider,
            activateProviderIfOASISProviderBootTypeIsHot: false);
        if (registration == null || registration.IsError || registration.Result == null)
            throw new InvalidOperationException("CQL_SOURCE_REGISTRATION_FAILED");
        var storage = registration.Result;
        if (storage.ProviderType?.Value != provider || storage is not ICosmicQueryStorageProvider adapter
            || adapter.QueryProviderType != provider || adapter.QueryCapabilities == null
            || storage is not ICosmicReadOnlyQueryActivation activationProvider)
            throw new InvalidOperationException("CQL_SOURCE_ADAPTER_INVALID");
        cancellationToken.ThrowIfCancellationRequested();
        // Activation is an instance/schema health check, not a user query or business save.
        // Await the owning provider operation; do not detach it on an observation timeout.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var activation = await activationProvider.ActivateQuerySourceAsync(deadline.Token);
        cancellationToken.ThrowIfCancellationRequested();
        if (activation == null || activation.IsError || !activation.Result || !storage.IsProviderActivated)
            throw new InvalidOperationException("CQL_SOURCE_ACTIVATION_FAILED");
    }
}

public sealed record CqlHostedSourceDescription(string Provider, bool QueryOnly, bool Available,
    bool FederationAvailable, string Features, string ErrorCode);

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

    public CqlHostedSources(PricedStorageAdmissionFactory admission, ICqlHostedSourceRegistry registry,
        ILogger<CqlHostedSources> logger)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        providers = admission?.QueryProviders.ToArray() ?? Array.Empty<ProviderType>();
        queryOnly = new HashSet<ProviderType>(admission?.QueryOnlyProviders ?? Array.Empty<ProviderType>());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var provider in queryOnly.OrderBy(type => type.ToString(), StringComparer.Ordinal))
        {
            if (stoppingToken.IsCancellationRequested) break;
            try
            {
                await registry.ActivateReadSourceAsync(provider, stoppingToken);
                stoppingToken.ThrowIfCancellationRequested();
                verified[provider] = true;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception cause)
            {
                failures[provider] = "CQL_SOURCE_UNAVAILABLE";
                // Do not put native connection strings/tokens into public discovery or logs.
                logger.LogWarning("CQL read source {Provider} activation failed ({FailureType}); it remains unavailable.",
                    provider, cause.GetType().Name);
            }
            // Optional source failure is explicit in discovery; never select another provider.
        }
    }

    public IReadOnlyList<CqlHostedSourceDescription> Describe()
        => providers.Select(type =>
        {
            if (failures.TryGetValue(type, out var failure))
                return new CqlHostedSourceDescription(type.ToString(), queryOnly.Contains(type), false, false, null, failure);
            var storage = registry.Find(type);
            var adapter = storage as ICosmicQueryStorageProvider;
            var available = (!queryOnly.Contains(type) || verified.ContainsKey(type))
                && storage?.IsProviderActivated == true && storage.ProviderType?.Value == type
                && adapter?.QueryProviderType == type && adapter.QueryCapabilities != null
                && CqlV1Compiler.SupportedReadProfile(type);
            return new CqlHostedSourceDescription(type.ToString(), queryOnly.Contains(type), available,
                available && CqlV1Compiler.Supported(type), available ? adapter.QueryCapabilities.Features.ToString() : null,
                available ? null : "CQL_SOURCE_NOT_READY");
        }).ToArray();
}
