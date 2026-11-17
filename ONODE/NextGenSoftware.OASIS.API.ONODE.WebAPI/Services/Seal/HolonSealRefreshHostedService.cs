using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Seal
{
    /// <summary>Registers the policy and keeps its in-memory switches fresh. Does nothing at all while HolonSeal.Enabled is false.</summary>
    public sealed class HolonSealRefreshHostedService : BackgroundService
    {
        private readonly HolonSealPolicy _policy;
        private readonly IHolonSealStore _store;
        private readonly IServiceProvider _services;
        public HolonSealRefreshHostedService(HolonSealPolicy policy, IHolonSealStore store, IServiceProvider services) { _policy = policy; _store = store; _services = services; }

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
                        _policy.ReplaceOwners(await _store.ListEnabledAsync(stoppingToken).ConfigureAwait(false));
                        var repo = (Subscription.MongoSubscriptionUsageRepository)_services.GetService(typeof(Subscription.MongoSubscriptionUsageRepository));
                        if (repo != null)
                        {
                            var accounts = await repo.ListAppAccountsAsync(stoppingToken).ConfigureAwait(false);
                            _policy.ReplaceApps(accounts.Where(a => a.Overrides?.SealedHistoryEnabled == true).Select(a => a.AppId));
                        }
                    }
                    catch (Exception) { /* keep the last known switches */ }
                }
                var seconds = Math.Max(5, _policy.Settings.RefreshIntervalSeconds);
                try { await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
            }
        }
    }
}
