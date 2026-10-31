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

public sealed record CqlHostedSourceDescription(string Provider, bool QueryOnly, bool Available,
    bool FederationAvailable, string Features, string ErrorCode, IReadOnlyList<CqlHostedQueryExample> Examples = null);

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

    public CqlHostedSources(PricedStorageAdmissionFactory admission, ICqlHostedSourceRegistry registry,
        ILogger<CqlHostedSources> logger, CqlHostedQueryExamples examples = null)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.examples = examples ?? CqlHostedQueryExamples.FromJson(null);
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
                    target => ReadyAdapter(target)?.QueryCapabilities)
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
        }).ToArray();

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
