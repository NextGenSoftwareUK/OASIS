using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    public sealed class HyperDriveSyncCompactionHostedService : BackgroundService
    {
        private readonly ILogger<HyperDriveSyncCompactionHostedService> _logger;
        private readonly HyperDriveHostedProviderAccessor _providerAccessor;

        public HyperDriveSyncCompactionHostedService(
            ILogger<HyperDriveSyncCompactionHostedService> logger,
            HyperDriveHostedProviderAccessor providerAccessor)
        {
            _logger = logger;
            _providerAccessor = providerAccessor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = NextGenSoftware.OASIS.API.DNA.OASISDNAManager.OASISDNA?.OASIS?.OASISHyperDriveConfig;
            if (config?.EnableHostedSync != true || !config.EnableSyncHistoryCompaction)
            {
                _logger.LogInformation("HyperDrive sync-history compaction is disabled in OASIS DNA.");
                return;
            }

            var providerResult = await _providerAccessor.GetAsync().ConfigureAwait(false);
            if (providerResult == null || providerResult.IsError || providerResult.Result == null)
                throw new InvalidOperationException(providerResult?.Message ??
                    "The default hosted sync provider could not be activated for compaction.");
            if (!(providerResult.Result is IHostedHyperDriveMaintenanceStore maintenanceStore))
                throw new InvalidOperationException(
                    $"Hosted sync compaction is enabled but provider '{providerResult.Result.ProviderName}' does not implement its maintenance contract.");

            var interval = TimeSpan.FromMinutes(config.SyncHistoryCompactionIntervalMinutes);
            _logger.LogInformation(
                "HyperDrive sync-history compaction started; interval {Interval}, inactive-device retention {RetentionDays} days.",
                interval, config.InactiveSyncDeviceRetentionDays);

            while (!stoppingToken.IsCancellationRequested)
            {
                var cutoff = DateTime.UtcNow.AddDays(-config.InactiveSyncDeviceRetentionDays);
                var compacted = await maintenanceStore.CompactSyncHistoryAsync(cutoff, stoppingToken)
                    .ConfigureAwait(false);
                if (compacted == null || compacted.IsError || compacted.Result == null)
                    _logger.LogError("HyperDrive sync-history compaction failed ({Code}): {Message}",
                        compacted?.ErrorCode, compacted?.Message ?? "The provider returned no compaction result.");
                else
                    _logger.LogInformation(
                        "HyperDrive sync-history compacted through sequence {SafeSequence}; removed {Changes} changes, {Checkpoints} checkpoints and {Snapshots} snapshot records.",
                        compacted.Result.SafeSequence, compacted.Result.DeletedChangeCount,
                        compacted.Result.DeletedCheckpointCount, compacted.Result.DeletedSnapshotCount);

                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
