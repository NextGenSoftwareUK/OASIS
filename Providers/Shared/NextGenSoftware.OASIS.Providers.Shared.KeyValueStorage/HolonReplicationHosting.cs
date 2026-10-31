using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public sealed class HolonReplicationHostOptions
{
    public string WorkerId {get;}
    public TimeSpan PollInterval {get;}
    public TimeSpan ErrorPollInterval {get;}
    public TimeSpan LeaseDuration {get;}
    public int MaximumBatch {get;}
    public int MaximumAttempts {get;}
    public HolonReplicationHostOptions(string workerId,TimeSpan? pollInterval=null,TimeSpan? errorPollInterval=null,
        TimeSpan? leaseDuration=null,int maximumBatch=25,int maximumAttempts=10)
    {
        HolonReplicationCatalog.Identity(workerId);WorkerId=workerId;
        PollInterval=pollInterval ?? TimeSpan.FromSeconds(1);ErrorPollInterval=errorPollInterval ?? TimeSpan.FromSeconds(5);
        LeaseDuration=leaseDuration ?? TimeSpan.FromMinutes(2);MaximumBatch=maximumBatch;MaximumAttempts=maximumAttempts;
        if(PollInterval<TimeSpan.FromMilliseconds(10) || PollInterval>TimeSpan.FromMinutes(5)
            || ErrorPollInterval<PollInterval || ErrorPollInterval>TimeSpan.FromMinutes(15)
            || LeaseDuration<=TimeSpan.Zero || LeaseDuration>TimeSpan.FromHours(1)
            || maximumBatch<1 || maximumBatch>100 || maximumAttempts<1 || maximumAttempts>1000)
            throw new ArgumentOutOfRangeException(nameof(pollInterval),"Bounded polling, lease, batch and attempt limits are required.");
    }
}

/// <summary>One joined loop per explicit route. Native stores retain fencing and providers retain resource ownership.</summary>
public sealed class HolonReplicationHostedService : BackgroundService
{
    private readonly HolonReplicationCatalog[] _catalogs;
    private readonly HolonReplicationHostOptions _options;
    private readonly ILogger<HolonReplicationHostedService> _logger;
    private int _executionEntered;
    public HolonReplicationHostState State {get;}
    public HolonReplicationHostedService(IEnumerable<HolonReplicationCatalog> catalogs,HolonReplicationHostOptions options,
        ILogger<HolonReplicationHostedService> logger) : this(catalogs,options,logger,new HolonReplicationHostState()) { }
    public HolonReplicationHostedService(IEnumerable<HolonReplicationCatalog> catalogs,HolonReplicationHostOptions options,
        ILogger<HolonReplicationHostedService> logger,HolonReplicationHostState state)
    {
        State=state ?? throw new ArgumentNullException(nameof(state));
        _catalogs=catalogs?.ToArray() ?? throw new ArgumentNullException(nameof(catalogs));
        _options=options ?? throw new ArgumentNullException(nameof(options));_logger=logger ?? throw new ArgumentNullException(nameof(logger));
        if(_catalogs.Length==0 || _catalogs.Length>16 || _catalogs.Any(catalog=>catalog==null)
            || _catalogs.Sum(catalog=>catalog.Targets.Count)>64
            || _catalogs.SelectMany(catalog=>catalog.TargetIds).Distinct(StringComparer.Ordinal).Count()!=_catalogs.Sum(catalog=>catalog.Targets.Count))
            throw new ArgumentException("A bounded catalog with unique versioned routes is required.",nameof(catalogs));
        if(_catalogs.Any(catalog=>catalog.Store is not IHolonReplicationReadinessOutbox))
            throw new ArgumentException("Configured hosting requires native queue-head readiness inspection.",nameof(catalogs));
        foreach(var group in _catalogs.GroupBy(catalog=>catalog.SourceId,StringComparer.Ordinal))
            if(group.Select(catalog=>(catalog.StoreIdentity,catalog.SourceAuthority)).Distinct().Count()!=1)
                throw new ArgumentException("A source identity cannot change store or authority inside a host.",nameof(catalogs));
        foreach(var group in _catalogs.GroupBy(catalog=>catalog.StoreIdentity,StringComparer.Ordinal))
            if(group.Select(catalog=>(catalog.SourceId,catalog.SourceAuthority)).Distinct().Count()!=1)
                throw new ArgumentException("A native outbox identity must have one source binding.",nameof(catalogs));
        foreach(var group in _catalogs.GroupBy(catalog=>catalog.Store,System.Collections.Generic.ReferenceEqualityComparer.Instance))
            if(group.Select(catalog=>catalog.StoreIdentity).Distinct(StringComparer.Ordinal).Count()!=1)
                throw new ArgumentException("One store instance cannot acquire conflicting native identities.",nameof(catalogs));
        State.Initialize(_catalogs.SelectMany(catalog=>catalog.TargetIds));
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Interlocked.Exchange(ref _executionEntered,1);
        State.Running();
        using var joined=CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var loops=_catalogs.SelectMany(catalog=>{
            var dispatcher=new HolonOutboxDispatcher(catalog.Store,catalog.Targets,_options.WorkerId,maximumAttempts:_options.MaximumAttempts);
            return catalog.TargetIds.Select(target=>RunRoute(dispatcher,(IHolonReplicationReadinessOutbox)catalog.Store,target,joined.Token));
        }).ToArray();
        await Task.WhenAny(loops).ConfigureAwait(false);
        // A terminal route must not leave sibling loops running after the host has faulted.
        Exception failure=null;
        try{await joined.CancelAsync().ConfigureAwait(false);}catch(Exception cause){failure=cause;}
        try{await Task.WhenAll(loops).ConfigureAwait(false);}
        catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){}
        catch(Exception cause){failure=Join(failure,cause);}
        State.Terminal(failure!=null);
        if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Exception failure=null;
        try{await base.StopAsync(cancellationToken).ConfigureAwait(false);}catch(Exception cause){failure=cause;}
        // BackgroundService may return at its stop deadline. Never report successful shutdown while routes still run.
        if(ExecuteTask!=null)
        {
            try{await ExecuteTask.WaitAsync(cancellationToken).ConfigureAwait(false);}
            // .NET 10 may cancel its scheduled Task.Run before ExecuteAsync enters.
            // No route exists in that case; preserve deadlines and all entered failures.
            catch(OperationCanceledException)when(!cancellationToken.IsCancellationRequested
                && ExecuteTask.IsCanceled && Volatile.Read(ref _executionEntered)==0){State.Terminal(false);}
            catch(Exception cause){failure=Join(failure,cause);}
        }
        if(failure!=null){State.Terminal(true);System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();}
    }
    private static Exception Join(Exception first,Exception second)=>first==null ? second:ReferenceEquals(first,second) ? first:new AggregateException(first,second);

    private async Task RunRoute(HolonOutboxDispatcher dispatcher,IHolonReplicationReadinessOutbox store,string targetId,CancellationToken token)
    {
        while(!token.IsCancellationRequested)
        {
            OASISResult<HolonOutboxDispatchResult> result;
            try
            {
                var probe=await store.PeekNextAsync(targetId,token).ConfigureAwait(false);
                var head=probe?.Result;
                if(probe==null || probe.IsError || head!=null && (head.TargetId!=targetId || head.Sequence<=0 || head.OperationId==Guid.Empty
                    || head.AttemptCount<0 || !Enum.IsDefined(typeof(HolonDeliveryState),head.State)))
                {
                    State.Update(targetId,HolonReplicationRouteAvailability.Degraded,probe?.ErrorCode ?? "COSMIC_OUTBOX_HEAD_UNAVAILABLE");
                    await Task.Delay(_options.ErrorPollInterval,token).ConfigureAwait(false);continue;
                }
                if(head?.State==HolonDeliveryState.ReconciliationRequired)
                {
                    State.Update(targetId,HolonReplicationRouteAvailability.Degraded,"COSMIC_OUTBOX_RECONCILIATION_REQUIRED");
                    await Task.Delay(_options.ErrorPollInterval,token).ConfigureAwait(false);continue;
                }
                var probeWarning=probe.IsWarning || probe.Exception!=null;
                var headCode=probeWarning ? "COSMIC_OUTBOX_HEAD_WARNING":head?.ErrorCode;
                State.Update(targetId,headCode==null ? HolonReplicationRouteAvailability.Healthy:HolonReplicationRouteAvailability.Degraded,headCode);
                if(head==null){await Task.Delay(probeWarning ? _options.ErrorPollInterval:_options.PollInterval,token).ConfigureAwait(false);continue;}
                result=await dispatcher.DispatchOnceAsync(targetId,_options.MaximumBatch,_options.LeaseDuration,token).ConfigureAwait(false);
                var code=result.IsError ? result.ErrorCode ?? "COSMIC_OUTBOX_DISPATCH_FAILED"
                    :result.Result?.ReconciliationCount>0 ? "COSMIC_OUTBOX_RECONCILIATION_REQUIRED"
                    :result.Result?.DeferredCount>0 ? "COSMIC_OUTBOX_DELIVERY_RETRY":result.IsWarning ? "COSMIC_OUTBOX_DELIVERY_WARNING":headCode;
                State.Update(targetId,code==null ? HolonReplicationRouteAvailability.Healthy:HolonReplicationRouteAvailability.Degraded,code);
            }
            catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}
            catch(Exception cause)
            {State.Update(targetId,HolonReplicationRouteAvailability.Degraded,"COSMIC_OUTBOX_HOST_ROUTE_FAILED");
                result=new OASISResult<HolonOutboxDispatchResult>{IsError=true,ErrorCount=1,ErrorCode="COSMIC_OUTBOX_HOST_ROUTE_FAILED",Message="The route invocation failed; native obligations remain authoritative.",Exception=cause};}
            if(result.IsError)_logger.LogError(result.Exception,"COSMIC route {TargetId} failed ({Code}).",targetId,result.ErrorCode);
            else if(result.IsWarning)_logger.LogWarning(result.Exception,"COSMIC route {TargetId} completed with a retained warning.",targetId);
            else if(result.Result?.CompletedCount>0)_logger.LogInformation("COSMIC route {TargetId} completed {Count} obligations.",targetId,result.Result.CompletedCount);
            await Task.Delay(result.IsError ? _options.ErrorPollInterval:_options.PollInterval,token).ConfigureAwait(false);
        }
    }
}

public static class HolonReplicationServiceCollectionExtensions
{
    /// <summary>Registers one automatic worker for already initialized, explicitly authorized native sources and receivers.</summary>
    public static IServiceCollection AddCosmicHolonReplication(this IServiceCollection services,HolonReplicationHostOptions options,
        params HolonReplicationCatalog[] catalogs)
    {
        ArgumentNullException.ThrowIfNull(services);ArgumentNullException.ThrowIfNull(options);ArgumentNullException.ThrowIfNull(catalogs);
        var frozen=catalogs.ToArray();
        if(services.Any(descriptor=>descriptor.ServiceType==typeof(HolonReplicationHostOptions) || descriptor.ServiceType==typeof(HolonReplicationConfiguration)))
            throw new InvalidOperationException("Configure the COSMIC host once with its complete immutable catalog.");
        // Validate before mutating service registration or opening any native resources.
        using var validated=new HolonReplicationHostedService(frozen,options,Microsoft.Extensions.Logging.Abstractions.NullLogger<HolonReplicationHostedService>.Instance);
        services.AddLogging();services.AddSingleton(options);services.AddSingleton<HolonReplicationHostState>();
        services.AddHealthChecks().AddCheck<HolonReplicationHealthCheck>("cosmic-holon-replication",tags:new[]{"ready"});
        foreach(var catalog in frozen)services.AddSingleton(catalog);
        services.AddHostedService<HolonReplicationHostedService>();return services;
    }
}
