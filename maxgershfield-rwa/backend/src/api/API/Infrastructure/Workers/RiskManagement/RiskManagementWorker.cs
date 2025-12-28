namespace API.Infrastructure.Workers.RiskManagement;

/// <summary>
/// Background service that periodically updates risk windows and generates recommendations
/// Runs daily to identify risk windows and generate deleveraging/return-to-baseline recommendations
/// </summary>
public sealed class RiskManagementWorker(
    ILogger<RiskManagementWorker> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(24); // Daily updates

    /// <summary>
    /// Main execution loop that runs daily to update risk management
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RiskManagementWorker started. Will update risk management daily.");

        // Wait a bit on startup before first run
        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var updaterService = scope.ServiceProvider
                    .GetRequiredService<RiskManagementUpdaterService>();

                await updaterService.UpdateRiskManagementAsync(stoppingToken);

                logger.LogInformation(
                    "Completed daily risk management update. Next update in 24 hours.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in risk management update cycle");
            }

            // Wait for the specified interval before next update
            await Task.Delay(_updateInterval, stoppingToken);
        }

        logger.LogInformation("RiskManagementWorker stopped.");
    }
}

