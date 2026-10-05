namespace API.Infrastructure.Workers.CorporateAction;

/// <summary>
/// Background service that periodically updates corporate actions for tracked symbols
/// Runs daily to fetch and store corporate actions from multiple data sources
/// </summary>
public sealed class CorporateActionWorker(
    ILogger<CorporateActionWorker> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(24); // Daily updates

    /// <summary>
    /// Main execution loop that runs daily to update corporate actions
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CorporateActionWorker started. Will update corporate actions daily.");

        // Wait a bit on startup before first run
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var updaterService = scope.ServiceProvider
                    .GetRequiredService<CorporateActionUpdaterService>();

                await updaterService.UpdateCorporateActionsAsync(stoppingToken);

                logger.LogInformation(
                    "Completed daily corporate action update. Next update in 24 hours.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in corporate action update cycle");
            }

            // Wait for the specified interval before next update
            await Task.Delay(_updateInterval, stoppingToken);
        }

        logger.LogInformation("CorporateActionWorker stopped.");
    }
}

