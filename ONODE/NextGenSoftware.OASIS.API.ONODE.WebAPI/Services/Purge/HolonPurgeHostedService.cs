using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Purge
{
    /// <summary>Registers the delete hook and runs the bounded worker. Does nothing at all while VersionPurge.Enabled is false.</summary>
    public sealed class HolonPurgeHostedService : BackgroundService, IHolonDeletionHook
    {
        private readonly HolonPurgeService _service;
        private readonly IHolonPurgeStore _store;
        private readonly IServiceProvider _services;
        public HolonPurgeHostedService(HolonPurgeService service, IHolonPurgeStore store, IServiceProvider services) { _service = service; _store = store; _services = services; }

        public void OnHolonDeleted(IHolon deleted, string appId) => _service.QueueDeleted(deleted, appId);

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            HolonPurging.Hook = this; // dormant unless the DNA enables it: QueueDeleted drops everything while the master switch is off
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var policy = _service.Policy;
                if (policy.MasterEnabled)
                {
                    try
                    {
                        await RefreshSwitchesAsync(policy, stoppingToken).ConfigureAwait(false);
                        await _service.RunCycleAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception) { /* keep the last switches; try again next cycle */ }
                }
                var wait = TimeSpan.FromSeconds(Math.Max(5, policy.Settings.CycleSeconds));
                try { await _service.WaitForWorkAsync(wait, stoppingToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }

        // Two small collections (one document per avatar choice, one per app account); never the Holon collection.
        private async Task RefreshSwitchesAsync(HolonPurgePolicy policy, CancellationToken c)
        {
            policy.ReplaceOwners(await _store.ListAvatarChoicesAsync(c).ConfigureAwait(false));
            var repo = (Subscription.MongoSubscriptionUsageRepository)_services.GetService(typeof(Subscription.MongoSubscriptionUsageRepository));
            if (repo != null)
            {
                var accounts = await repo.ListAppAccountsAsync(c).ConfigureAwait(false);
                policy.ReplaceApps(accounts.Where(a => a.Overrides != null && (a.Overrides.VersionPurgeEnabled != null || a.Overrides.VersionPurgeDays != null))
                    .Select(a => new KeyValuePair<string, PurgeChoice>(a.AppId, new PurgeChoice { Enabled = a.Overrides.VersionPurgeEnabled, Days = a.Overrides.VersionPurgeDays })));
            }
        }
    }
}
