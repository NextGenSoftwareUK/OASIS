using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public sealed class HolonReplicationProviderResolver
{
    internal Func<ProviderType,IOASISStorageProvider> Resolve {get;}
    internal Func<CancellationToken,Task> Initialize {get;}
    public HolonReplicationProviderResolver(Func<ProviderType,IOASISStorageProvider> resolve,Func<CancellationToken,Task> initialize=null)
    {Resolve=resolve ?? throw new ArgumentNullException(nameof(resolve));Initialize=initialize ?? (_=>Task.CompletedTask);}
}

/// <summary>Startup owns validation and worker lifetime; the platform continues to own shared registered providers.</summary>
public sealed class ConfiguredHolonReplicationHostedService : BackgroundService
{
    private readonly HolonReplicationConfiguration _configuration;
    private readonly HolonReplicationProviderResolver _resolver;
    private readonly HolonReplicationHostState _state;
    private readonly ILoggerFactory _loggerFactory;
    private HolonReplicationHostedService _worker;
    public IReadOnlyList<HolonReplicationCatalog> Catalogs {get;private set;}=Array.Empty<HolonReplicationCatalog>();
    public ConfiguredHolonReplicationHostedService(HolonReplicationConfiguration configuration,HolonReplicationProviderResolver resolver,
        HolonReplicationHostState state,ILoggerFactory loggerFactory)
    {_configuration=configuration;_resolver=resolver;_state=state;_loggerFactory=loggerFactory;}
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Factory errors deliberately fail host startup, even when background failures are globally ignored.
        await _resolver.Initialize(cancellationToken).ConfigureAwait(false);cancellationToken.ThrowIfCancellationRequested();
        Catalogs=await HolonReplicationCatalogFactory.BuildAsync(_configuration,_resolver.Resolve,cancellationToken).ConfigureAwait(false);
        _worker=new HolonReplicationHostedService(Catalogs,_configuration.Options,_loggerFactory.CreateLogger<HolonReplicationHostedService>(),_state);
        await _worker.StartAsync(cancellationToken).ConfigureAwait(false);
        try{await base.StartAsync(cancellationToken).ConfigureAwait(false);}
        catch(Exception startup)
        {try{await _worker.StopAsync(CancellationToken.None).ConfigureAwait(false);}catch(Exception cleanup){throw new AggregateException(startup,cleanup);}throw;}
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)=>_worker.ExecuteTask;
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Exception failure=null;
        if(_worker!=null){try{await _worker.StopAsync(cancellationToken).ConfigureAwait(false);}catch(Exception cause){failure=cause;}}
        try{await base.StopAsync(cancellationToken).ConfigureAwait(false);}catch(Exception cause){failure=failure==null ? cause:new AggregateException(failure,cause);}
        if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    public override void Dispose(){_worker?.Dispose();base.Dispose();}
}

public static class ConfiguredHolonReplicationRegistration
{
    public static IServiceCollection AddConfiguredCosmicHolonReplication(this IServiceCollection services,string json,
        Func<ProviderType,IOASISStorageProvider> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return AddConfiguredCosmicHolonReplicationFromServices(services,json,_=>new HolonReplicationProviderResolver(resolver));
    }
    public static IServiceCollection AddConfiguredCosmicHolonReplicationFromServices(this IServiceCollection services,string json,
        Func<IServiceProvider,HolonReplicationProviderResolver> resolverFactory)
    {
        ArgumentNullException.ThrowIfNull(services);ArgumentNullException.ThrowIfNull(resolverFactory);
        var configuration=HolonReplicationConfiguration.Parse(json);
        if(!configuration.Enabled)return services;
        if(services.Any(descriptor=>descriptor.ServiceType==typeof(HolonReplicationHostOptions) || descriptor.ServiceType==typeof(HolonReplicationConfiguration)))
            throw new InvalidOperationException("Configure COSMIC replication exactly once.");
        services.AddLogging();services.AddSingleton(configuration);services.AddSingleton<HolonReplicationProviderResolver>(resolverFactory);
        services.AddSingleton<HolonReplicationHostState>();services.AddSingleton<ConfiguredHolonReplicationHostedService>();
        services.AddSingleton<IHostedService>(provider=>provider.GetRequiredService<ConfiguredHolonReplicationHostedService>());
        services.AddHealthChecks().AddCheck<HolonReplicationHealthCheck>("cosmic-holon-replication",tags:new[]{"ready"});
        return services;
    }
}
