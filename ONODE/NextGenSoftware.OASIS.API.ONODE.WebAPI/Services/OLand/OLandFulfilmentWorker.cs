using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;

/// <summary>Durable paid orders form the NFT-delivery outbox; closing a client does not stop fulfilment.</summary>
public sealed class OLandFulfilmentWorker(IServiceProvider services, ILogger<OLandFulfilmentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = services.GetRequiredService<OLandCommerceSettings>();
        if (settings.FulfilmentPollSeconds < 1 || settings.FulfilmentPollSeconds > 3600) throw new InvalidOperationException("OLAND fulfilment polling interval is invalid.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.FulfilmentPollSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            if (settings.Enabled)
            {
                try
                {
                    var store = services.GetRequiredService<IOLandOrderStore>();
                    var commerce = services.GetRequiredService<OLandCommerceService>();
                    foreach (var order in await store.PendingMint(stoppingToken))
                    {
                        try { await commerce.Fulfil(Guid.Parse(order.AvatarId), order.Id, stoppingToken); }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                        catch (Exception ex) { logger.LogError(ex, "OLAND order {OrderId} NFT delivery needs reconciliation.", order.Id); }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception ex) { logger.LogError(ex, "OLAND paid-order outbox could not be read."); }
            }
            if (!await timer.WaitForNextTickAsync(stoppingToken)) return;
        }
    }
}
