using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Seal
{
    /// <summary>Registers the policy and keeps its in-memory switches fresh. Does nothing at all while HolonSeal.Enabled is false.</summary>
    public sealed class HolonSealRefreshHostedService : BackgroundService
    {
        private readonly HolonSealPolicy _policy;
        private readonly IHolonSealStore _store;
        private readonly IServiceProvider _services;
        private readonly ILogger<HolonSealRefreshHostedService> _logger;
        public HolonSealRefreshHostedService(HolonSealPolicy policy, IHolonSealStore store, IServiceProvider services, ILogger<HolonSealRefreshHostedService> logger) { _policy = policy; _store = store; _services = services; _logger = logger; }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            HolonSealing.Policy = _policy; // dormant (IsAvailable false) unless the DNA enables it and holds a key
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_policy.IsAvailable)
                {
                    try
                    {
                        var revision = _policy.SwitchRevision;
                        var owners = await _store.ListEnabledAsync(stoppingToken).ConfigureAwait(false);
                        var repo = (Subscription.MongoSubscriptionUsageRepository)_services.GetService(typeof(Subscription.MongoSubscriptionUsageRepository));
                        var apps = Array.Empty<string>();
                        if (repo != null)
                        {
                            var accounts = await repo.ListAppPolicyOverridesAsync(stoppingToken).ConfigureAwait(false);
                            apps = accounts.Where(a => a.SealedHistoryEnabled == true).Select(a => a.AppId).ToArray();
                        }
                        if (!_policy.TryReplaceSwitches(revision, owners, apps))
                            _logger.LogDebug("HOLON_SEAL_REFRESH_SUPERSEDED: a newer local setting was preserved.");
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                    catch (Exception ex)
                    {
                        // Retain the complete last published policy; never publish a partial refresh.
                        _logger.LogError("HOLON_SEAL_REFRESH_FAILED {ExceptionType}", ex.GetType().Name);
                    }
                }
                var seconds = Math.Max(5, _policy.Settings.RefreshIntervalSeconds);
                try { await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
            }
        }
    }
}
