using Application.Contracts;

namespace API.Infrastructure.Workers.CorporateAction;

/// <summary>
/// Service that updates corporate actions for tracked symbols
/// </summary>
public sealed class CorporateActionUpdaterService(
    ICorporateActionService corporateActionService,
    ILogger<CorporateActionUpdaterService> logger)
{
    /// <summary>
    /// List of symbols to track (can be configured)
    /// </summary>
    private readonly List<string> _trackedSymbols = new()
    {
        "AAPL", "MSFT", "GOOGL", "AMZN", "META", "TSLA", "NVDA", "JPM", "V", "JNJ"
    };

    /// <summary>
    /// Update corporate actions for all tracked symbols
    /// </summary>
    public async Task UpdateCorporateActionsAsync(CancellationToken token = default)
    {
        logger.LogInformation("Starting corporate action update for {Count} symbols", _trackedSymbols.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var symbol in _trackedSymbols)
        {
            try
            {
                logger.LogInformation("Fetching corporate actions for symbol {Symbol}", symbol);

                // Fetch from last 2 years to catch any missed actions
                var fromDate = DateTime.UtcNow.AddYears(-2);
                var actions = await corporateActionService.FetchCorporateActionsAsync(symbol, fromDate, token);

                if (actions.Count > 0)
                {
                    // Validate actions before saving
                    var validActions = new List<Domain.Entities.CorporateAction>();
                    foreach (var action in actions)
                    {
                        if (await corporateActionService.ValidateCorporateActionAsync(action, token))
                        {
                            validActions.Add(action);
                        }
                    }

                    if (validActions.Count > 0)
                    {
                        await corporateActionService.SaveCorporateActionsAsync(validActions, token);
                        logger.LogInformation(
                            "Saved {Count} corporate actions for symbol {Symbol}",
                            validActions.Count, symbol);
                        successCount++;
                    }
                    else
                    {
                        logger.LogWarning("No valid corporate actions found for symbol {Symbol}", symbol);
                    }
                }
                else
                {
                    logger.LogInformation("No corporate actions found for symbol {Symbol}", symbol);
                    successCount++; // Not an error, just no data
                }

                // Small delay to respect rate limits
                await Task.Delay(1000, token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating corporate actions for symbol {Symbol}", symbol);
                errorCount++;
            }
        }

        logger.LogInformation(
            "Corporate action update completed. Success: {Success}, Errors: {Errors}",
            successCount, errorCount);
    }
}

