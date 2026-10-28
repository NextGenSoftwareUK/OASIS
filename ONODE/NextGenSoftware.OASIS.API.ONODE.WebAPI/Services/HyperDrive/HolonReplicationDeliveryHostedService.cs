using System;
using System.Linq;
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

        private async Task DeliverAsync(CancellationToken cancellationToken)
        {
            var manager = _providers();
            var storage = manager.GetAllRegisteredProviders().OfType<IOASISStorageProvider>().ToList();
            foreach (var source in storage.Where(provider => provider is IHolonReplicationOutboxSource))
            {
                var sourceType = source.ProviderType.Value;
                var activated = manager.ResolveStorageProvider(sourceType);
                if (activated.IsError || activated.Result is not IHolonReplicationOutboxSource outboxSource)
                    continue;
                var outbox = outboxSource.GetHolonReplicationOutbox();
                if (outbox.IsError || outbox.Result == null)
                {
                    _logger.LogWarning("Replication outbox of {Source} is unavailable: {Message}", sourceType, outbox.Message);
                    continue;
                }

                foreach (var target in storage.Where(provider => provider != source && provider is ICommittedHolonReplicaStorageProvider))
                {
                    var targetType = target.ProviderType.Value;
                    var targetId = HolonReplicationRoutes.TargetId(targetType);
                    var pending = await outbox.Result.PeekNextAsync(targetId, cancellationToken);
                    if (pending.IsError || pending.Result == null)
                        continue;

                    var receiver = manager.ResolveStorageProvider(targetType);
                    if (receiver.IsError || receiver.Result is not ICommittedHolonReplicaStorageProvider replicas)
                    {
                        _logger.LogWarning("Replication target {Target} is unavailable: {Message}", targetType, receiver.Message);
                        continue;
                    }

                    var dispatcher = new HolonOutboxDispatcher(outbox.Result, new[]
                    {
                        new CommittedHolonReplicaTarget(targetId, replicas, targetType, HolonReplicationRoutes.SourceId(sourceType), sourceType,
                            _authorization, "hyperdrive-owned-replication", AuthorizationContextVersion)
                    }, _workerId);

                    for (var round = 0; round < MaximumRoundsPerTarget; round++)
                    {
                        var delivered = await dispatcher.DispatchOnceAsync(targetId, cancellationToken: cancellationToken);
                        if (delivered.IsError)
                        {
                            _logger.LogWarning("Replication {Source} -> {Target} failed: {Message}", sourceType, targetType, delivered.Message);
                            break;
                        }
                        var counts = delivered.Result;
                        if (counts.CompletedCount > 0 || counts.DeferredCount > 0 || counts.ReconciliationCount > 0)
                            _logger.LogInformation("Replication {Source} -> {Target}: {Completed} delivered, {Deferred} deferred, {Reconcile} need reconciliation.",
                                sourceType, targetType, counts.CompletedCount, counts.DeferredCount, counts.ReconciliationCount);
                        if (counts.ClaimedCount == 0 || counts.CompletedCount == 0) break;
                    }
                }
            }
        }
    }
}
