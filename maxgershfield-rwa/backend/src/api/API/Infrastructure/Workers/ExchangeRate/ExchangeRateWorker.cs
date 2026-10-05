namespace API.Infrastructure.Workers.ExchangeRate;

/// <summary>
/// A background service that periodically updates exchange rates between cryptocurrencies 
/// by invoking the <see cref="ExchangeRateUpdaterService"/> every five minutes.
/// This worker retrieves and updates exchange rates for tokens like SOL and XRD by calling 
/// the <see cref="ExchangeRateUpdaterService"/> and logging the results for monitoring purposes.
/// It runs indefinitely until cancellation is requested.
/// </summary>
public class ExchangeRateWorker(
    ILogger<ExchangeRateWorker> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly TimeSpan
        _updateInterval = TimeSpan.FromMinutes(5); // Defines the interval between updates (5 minutes).

    private const int Count = 1; // Used for logging and identifying update cycles.

    /// <summary>
    /// The main execution loop of the background service that runs indefinitely until cancellation.
    /// This method calls <see cref="ExchangeRateUpdaterService.UpdateExchangeRatesAsync"/> to 
    /// update exchange rates, logs the success or failure of each update, and waits for the 
    /// specified interval before executing again.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token to gracefully stop the service.</param>
    /// <returns>A task that represents the asynchronous operation of updating exchange rates.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ExchangeRateWorker started.");
        logger.LogInformation("-----------------------------------------------------------------------------------");
        logger.LogInformation("-----------------------------------------------------------------------------------");

        // Continuous execution until cancellation is requested
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope
                    scope = serviceScopeFactory
                        .CreateScope(); // Creating a scoped service to fetch the updater service.
                ExchangeRateUpdaterService exchangeRateUpdater =
                    scope.ServiceProvider
                        .GetRequiredService<
                            ExchangeRateUpdaterService>(); // Getting the updater service from DI container.
                await exchangeRateUpdater
                    .UpdateExchangeRatesAsync(stoppingToken); // Updating the exchange rates asynchronously.

                // Logging success message after each successful update
                logger.LogInformation($"Completed UpdateExchangeRatesAsync-{Count} ");
                logger.LogInformation(
                    "-----------------------------------------------------------------------------------");
                logger.LogInformation(
                    "-----------------------------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                // Logging error details if something goes wrong during the update process
                logger.LogError(ex, "Error updating exchange rates.");
                logger.LogError("-----------------------------------------------------------------------------------");
                logger.LogError("-----------------------------------------------------------------------------------");
            }

            // Waiting for the specified interval before starting the next update cycle
            await Task.Delay(_updateInterval, stoppingToken);
        }
    }
}