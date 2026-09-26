using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>Runs ordered, durable provider fan-out for hosted HyperDrive synchronization.</summary>
    public sealed class HyperDriveFanOutHostedService : BackgroundService
    {
        private readonly ILogger<HyperDriveFanOutHostedService> _logger;
        private readonly HyperDriveHostedProviderAccessor _providerAccessor;
        private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

        public HyperDriveFanOutHostedService(ILogger<HyperDriveFanOutHostedService> logger,
            HyperDriveHostedProviderAccessor providerAccessor)
        {
            _logger = logger;
            _providerAccessor = providerAccessor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = NextGenSoftware.OASIS.API.DNA.OASISDNAManager.OASISDNA?.OASIS?.OASISHyperDriveConfig;
            if (config?.EnableHostedSync != true)
            {
                _logger.LogInformation("Durable hosted HyperDrive synchronization is disabled in OASIS DNA.");
                return;
            }
            if (!config.AutoReplicationEnabled)
            {
                _logger.LogInformation("HyperDrive provider fan-out is disabled in OASIS DNA.");
                return;
            }

            var providerResult = await _providerAccessor.GetAsync().ConfigureAwait(false);
            if (providerResult == null || providerResult.IsError || providerResult.Result == null)
                throw new InvalidOperationException(providerResult?.Message ?? "The default hosted sync provider could not be activated.");
            if (!(providerResult.Result is IHostedHyperDriveSyncStore))
                throw new InvalidOperationException(
                    $"Durable hosted synchronization is enabled but provider '{providerResult.Result.ProviderName}' does not implement its authoritative sync store contract.");
            if (!(providerResult.Result is IHostedHyperDriveFanOutStore fanOutStore))
                throw new InvalidOperationException($"Hosted sync provider '{providerResult.Result.ProviderName}' must expose its durable fan-out outbox.");

            var targets = ResolveTargets(providerResult.Result);
            var dispatcher = new HostedHyperDriveFanOutDispatcher(fanOutStore,
                new OASISProviderFanOutHandler(targets), _workerId);
            _logger.LogInformation("HyperDrive fan-out worker {WorkerId} started with {TargetCount} explicit targets.",
                _workerId, targets.Count);

            while (!stoppingToken.IsCancellationRequested)
            {
                var dispatched = await dispatcher.DispatchOnceAsync(25, TimeSpan.FromMinutes(2), stoppingToken)
                    .ConfigureAwait(false);
                if (dispatched.IsError)
                    _logger.LogError("HyperDrive fan-out failed ({Code}): {Message}", dispatched.ErrorCode, dispatched.Message);
                await Task.Delay(dispatched.IsError ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(1), stoppingToken)
                    .ConfigureAwait(false);
            }
        }

        private static IReadOnlyList<IHyperDriveIdempotentReplicationTarget> ResolveTargets(
            IOASISStorageProvider sourceProvider)
        {
            var targets = new List<IHyperDriveIdempotentReplicationTarget>();
            foreach (var providerType in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
            {
                var provider = ProviderManager.Instance.GetStorageProvider(providerType.Value);
                if (provider == null)
                    throw new InvalidOperationException($"Configured replication provider '{providerType.Name}' is not registered.");
                if (ReferenceEquals(provider, sourceProvider)) continue;
                if (!(provider is IHyperDriveIdempotentReplicationTarget target))
                    throw new InvalidOperationException(
                        $"Configured replication provider '{provider.ProviderName}' does not implement the idempotent HyperDrive replication contract.");
                targets.Add(target);
            }
            return targets;
        }
    }
}
