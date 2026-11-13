using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Gardens;

public sealed class GardenRewardWorker:BackgroundService
{
    private readonly GardenService gardens;
    private readonly ILogger<GardenRewardWorker> log;
    public GardenRewardWorker(GardenService gardens,ILogger<GardenRewardWorker> log){this.gardens=gardens;this.log=log;}
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try{int pending=await gardens.RetryPending(stoppingToken);if(pending>0)log.LogWarning("Gardening Karma remains pending for {Count} gardeners; durable actions are retained.",pending);}
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){log.LogWarning("Gardening reward processing failed ({Type}); durable rewards remain pending.",ex.GetType().Name);}
            await Task.Delay(TimeSpan.FromSeconds(30),stoppingToken);
        }
    }
}
