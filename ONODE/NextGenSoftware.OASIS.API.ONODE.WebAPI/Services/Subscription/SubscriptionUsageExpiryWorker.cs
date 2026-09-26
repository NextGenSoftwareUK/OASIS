using System;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed class SubscriptionUsageExpiryWorker : BackgroundService
    {
        private readonly ISubscriptionUsageRepository _repository;
        private readonly ILogger<SubscriptionUsageExpiryWorker> _logger;
        private static readonly Meter Meter = new("OASIS.WEB4.SubscriptionLedger", "1.0.0");
        private static readonly Counter<long> Expired = Meter.CreateCounter<long>("subscription.reservations.expired");
        private static readonly Counter<long> Failures = Meter.CreateCounter<long>("subscription.expiry.failures");
        public SubscriptionUsageExpiryWorker(ISubscriptionUsageRepository repository, ILogger<SubscriptionUsageExpiryWorker> logger)
            { _repository = repository; _logger = logger; }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            do
            {
                try
                {
                    int count = await _repository.ExpireAsync(DateTime.UtcNow, 500, stoppingToken);
                    if (count > 0)
                    {
                        Expired.Add(count);
                        _logger.LogWarning("{Count} subscription execution leases expired; reserved exposure requires provider reconciliation", count);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    Failures.Add(1);
                    _logger.LogError(ex, "Subscription reservation expiry scan failed; retry at the next scheduled scan, reservations remain held");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
