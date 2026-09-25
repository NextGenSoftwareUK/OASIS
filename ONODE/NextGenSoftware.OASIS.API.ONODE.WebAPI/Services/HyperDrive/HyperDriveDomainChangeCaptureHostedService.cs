using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>
    /// Projects authoritative mutations made through ordinary OASIS APIs into the durable Edge change feed.
    /// The provider owns the change-stream checkpoint and projection transaction; this host only schedules it.
    /// </summary>
    public sealed class HyperDriveDomainChangeCaptureHostedService : BackgroundService
    {
        private readonly ILogger<HyperDriveDomainChangeCaptureHostedService> _logger;

        public HyperDriveDomainChangeCaptureHostedService(
            ILogger<HyperDriveDomainChangeCaptureHostedService> logger) => _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (OASISBootLoader.OASISBootLoader.OASISDNA == null)
            {
                var boot = await OASISBootLoader.OASISBootLoader.BootOASISASync(false).ConfigureAwait(false);
                if (boot == null || boot.IsError || !boot.Result)
                    throw new InvalidOperationException(boot?.Message ??
                        "OASIS could not boot for HyperDrive domain change capture.");
            }
            if (OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.OASISHyperDriveConfig?.EnableHostedSync != true)
            {
                _logger.LogInformation("Hosted HyperDrive domain change capture is disabled in OASIS DNA.");
                return;
            }

            var providerResult = await OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync()
                .ConfigureAwait(false);
            if (providerResult == null || providerResult.IsError || providerResult.Result == null)
                throw new InvalidOperationException(providerResult?.Message ??
                    "The default domain change-capture provider could not be activated.");
            if (!(providerResult.Result is IHostedHyperDriveDomainChangeCaptureStore captureStore))
                throw new InvalidOperationException(
                    $"Hosted synchronization is enabled but provider '{providerResult.Result.ProviderName}' does not implement durable domain change capture.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var captured = await captureStore.CaptureNextDomainChangesAsync(100, TimeSpan.FromSeconds(2),
                    stoppingToken).ConfigureAwait(false);
                if (captured.IsError)
                {
                    if (captured.ErrorCode == "MONGO_DOMAIN_CAPTURE_BACKFILL_REQUIRED")
                        throw new InvalidOperationException(captured.Message);
                    _logger.LogError("HyperDrive domain capture failed ({Code}): {Message}",
                        captured.ErrorCode, captured.Message);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                    continue;
                }
                if (captured.Result?.RejectedCount > 0)
                    _logger.LogError(
                        "HyperDrive domain capture quarantined {RejectedCount} mutations. Inspect HyperDriveDomainCaptureDeadLetters before release.",
                        captured.Result.RejectedCount);
            }
        }
    }
}
