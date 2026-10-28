using System;
using System.Linq;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>
    /// Delivers HyperDrive V2 replication obligations. Owned writes enrol their replicas in the authority's native outbox
    /// atomically with the write (<see cref="HolonReplicationRoutes"/>); this worker claims each pending obligation and
    /// applies it to the target provider through <see cref="HolonOutboxDispatcher"/>, with leases, retries and
    /// reconciliation handled by the outbox. It runs right after every enrolled commit and on a poll for retries.
    /// </summary>
    public sealed class HolonReplicationDeliveryHostedService : BackgroundService
    {
        private static readonly TimeSpan Poll = TimeSpan.FromSeconds(15);
        private const int MaximumRoundsPerTarget = 20;
        private readonly ILogger<HolonReplicationDeliveryHostedService> _logger;
        private readonly Func<ProviderManager> _providers;
        private readonly Func<bool> _isReady;
        private readonly IHolonReplicationAuthorizationPolicy _authorization;
        // Authorization context schema, not a claim of persisted endpoint binding.
        private static readonly Guid AuthorizationContextVersion = new Guid("e1d13ed7-2146-4f40-a799-c2bbbfd9113f");
        private readonly Dictionary<ProviderType,string> _routeContinuations=new Dictionary<ProviderType,string>();
        private readonly string _workerId = $"replication:{Environment.MachineName}:{Guid.NewGuid():N}";
        private readonly SemaphoreSlim _committed = new SemaphoreSlim(0, 1);

        public HolonReplicationDeliveryHostedService(ILogger<HolonReplicationDeliveryHostedService> logger)
            : this(logger, new CommittedOwnerReplicationPolicy()) { }

        public HolonReplicationDeliveryHostedService(ILogger<HolonReplicationDeliveryHostedService> logger,
            IHolonReplicationAuthorizationPolicy authorization)
            : this(logger, () => ProviderManager.Instance,
                () => OASISBootLoader.OASISBootLoader.IsOASISBooted &&
                    OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.HyperDriveMode == HyperDriveModes.V2, authorization) { }

        /// <summary>Host-owned registry and readiness dependencies. Both construction
        /// paths use the same delivery loop and native outbox transaction owners.</summary>
        public HolonReplicationDeliveryHostedService(ILogger<HolonReplicationDeliveryHostedService> logger,
            Func<ProviderManager> providers, Func<bool> isReady)
            : this(logger, providers, isReady, new CommittedOwnerReplicationPolicy()) { }

        public HolonReplicationDeliveryHostedService(ILogger<HolonReplicationDeliveryHostedService> logger,
            Func<ProviderManager> providers, Func<bool> isReady, IHolonReplicationAuthorizationPolicy authorization)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _isReady = isReady ?? throw new ArgumentNullException(nameof(isReady));
            _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            HolonReplicationRoutes.UseAuthorizationBinding(_providers(),()=>_authorization.BindingIdentity);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            void OnCommitted(NextGenSoftware.OASIS.API.Core.Enums.ProviderType _)
            {
                if (_committed.CurrentCount == 0)
                    try { _committed.Release(); } catch (SemaphoreFullException) { }
            }

            HolonReplicationRoutes.EnrolledWriteCommitted += OnCommitted;
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_isReady())
                    {
                        try
                        {
                            await DeliverAsync(stoppingToken);
                            _logger.LogDebug("HyperDrive replication delivery pass completed.");
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                        catch (Exception exception) { _logger.LogError(exception, "HyperDrive replication delivery pass failed."); }
                    }
                    try { await _committed.WaitAsync(Poll, stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }
            finally { HolonReplicationRoutes.EnrolledWriteCommitted -= OnCommitted; }
        }

        private sealed class ReconciliationTarget(string target,string code) : IHyperDriveIdempotentReplicationTarget
        {
            public string ReplicationTargetId=>target;
            public Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item,CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(new OASISResult<bool>{IsError=true,ErrorCount=1,ErrorCode=code,
                    Message="The persisted automatic route does not match the native source,target or current policy. Explicit reconciliation is required; no replica was applied."});
            }
        }

        private async Task DeliverAsync(CancellationToken cancellationToken)
        {
            var manager=_providers();
            HolonReplicationRoutes.UseAuthorizationBinding(manager,()=>_authorization.BindingIdentity);
            var storage=manager.GetAllRegisteredProviders().OfType<IOASISStorageProvider>().ToList();
            foreach(var source in storage.Where(provider=>provider is IHolonReplicationOutboxSource))
            {
                var sourceType=source.ProviderType.Value;
                var activated=manager.ResolveStorageProvider(sourceType);
                if(activated.IsError || activated.Result is not IHolonReplicationOutboxSource outboxSource)continue;
                var outbox=outboxSource.GetHolonReplicationOutbox();
                if(outbox.IsError || outbox.Result is not IHolonReplicationRouteInventoryOutbox inventory)
                {_logger.LogWarning("Replication source {Source} has no verified route inventory: {Message}",sourceType,outbox.Message);continue;}
                var policy=_authorization.BindingIdentity;
                var routes=new Dictionary<string,(HolonReplicationRouteBinding Binding,IOASISStorageProvider Provider)>(StringComparer.Ordinal);
                var complete=true;
                try
                {
                    var sourceStorage=await HolonReplicationRouteBinding.CapturePinnedAsync(activated.Result,cancellationToken);
                    foreach(var target in storage.Where(value=>value.ProviderType.Value!=sourceType && value is IBoundCommittedHolonReplicaStorageProvider))
                    {
                        try
                        {
                            var targetProvider=HolonReplicationRoutes.ResolveBindingProvider(manager,target.ProviderType.Value);
                            var targetStorage=await HolonReplicationRouteBinding.CapturePinnedAsync(targetProvider,cancellationToken);
                            var binding=new HolonReplicationRouteBinding(sourceStorage,targetStorage,policy);
                            routes.Add(binding.TargetId,(binding,targetProvider));
                        }
                        catch(OperationCanceledException)when(cancellationToken.IsCancellationRequested){throw;}
                        catch(Exception cause){complete=false;_logger.LogWarning(cause,"Replication target binding {Target} unavailable.",target.ProviderType.Value);}
                    }
                }
                catch(OperationCanceledException)when(cancellationToken.IsCancellationRequested){throw;}
                catch(Exception cause){complete=false;_logger.LogWarning(cause,"Replication source binding {Source} unavailable.",sourceType);}
                _routeContinuations.TryGetValue(sourceType,out var after);
                // Bounded work per pass; continue next pass rather than starving
                // later routes behind a large retained reconciliation backlog.
                for(var pageIndex=0;pageIndex<8;pageIndex++)
                {
                    var page=await inventory.ReadRouteHeadsAsync(64,after,cancellationToken);
                    if(page.IsError || page.Result==null){_logger.LogWarning("Replication route inventory {Source} failed: {Message}",sourceType,page.Message);break;}
                    foreach(var head in page.Result.Heads)
                    {
                        var legacy=head.TargetId.StartsWith("hyperdrive-target:",StringComparison.Ordinal);
                        if(!legacy && !head.TargetId.StartsWith("hyperdrive-target-v2:",StringComparison.Ordinal))continue;
                        if(head.State==HolonDeliveryState.ReconciliationRequired)continue;
                        IHyperDriveIdempotentReplicationTarget receiver;
                        if(routes.TryGetValue(head.TargetId,out var current))
                        {
                            var resolved=manager.ResolveStorageProvider(current.Provider.ProviderType.Value);
                            if(resolved.IsError || resolved.Result is not IBoundCommittedHolonReplicaStorageProvider native)
                            {_logger.LogWarning("Replication receiver {Target} unavailable: {Message}",head.TargetId,resolved.Message);continue;}
                            receiver=new CommittedHolonReplicaTarget(head.TargetId,native,current.Binding.TargetStorage.Authority,
                                current.Binding.SourceId,sourceType,_authorization,"hyperdrive-bound-routing-v2",AuthorizationContextVersion,
                                current.Binding.PolicyBinding,current.Binding.TargetStorage);
                        }
                        else
                        {
                            if(!legacy && !complete)continue; // Failed discovery cannot prove a route changed.
                            receiver=new ReconciliationTarget(head.TargetId,legacy?"COSMIC_REPLICATION_ROUTE_UNBOUND":"COSMIC_REPLICATION_ROUTE_CHANGED");
                        }
                        var dispatcher=new HolonOutboxDispatcher(outbox.Result,new[]{receiver},_workerId);
                        for(var round=0;round<MaximumRoundsPerTarget;round++)
                        {
                            var delivered=await dispatcher.DispatchOnceAsync(head.TargetId,cancellationToken:cancellationToken);
                            if(delivered.IsError){_logger.LogWarning("Replication delivery {Source} -> {Target} failed: {Message}",sourceType,head.TargetId,delivered.Message);break;}
                            var counts=delivered.Result;
                            if(counts.CompletedCount>0 || counts.DeferredCount>0 || counts.ReconciliationCount>0)
                                _logger.LogInformation("Replication {Source} -> {Target}: {Completed} delivered, {Deferred} deferred, {Reconcile} need reconciliation.",
                                    sourceType,head.TargetId,counts.CompletedCount,counts.DeferredCount,counts.ReconciliationCount);
                            if(counts.ClaimedCount==0 || counts.CompletedCount==0)break;
                        }
                    }
                    after=page.Result.NextAfterTargetId;
                    _routeContinuations[sourceType]=after;
                    if(after==null)break;
                }
            }
        }
    }
}
