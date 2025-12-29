using Application.Contracts;
using Microsoft.Extensions.Configuration;

namespace API.Infrastructure.Workers.RiskManagement;

/// <summary>
/// Service that updates risk windows and generates recommendations for tracked symbols
/// </summary>
public sealed class RiskManagementUpdaterService(
    IRiskWindowService riskWindowService,
    IRiskRecommendationService riskRecommendationService,
    IConfiguration configuration,
    ILogger<RiskManagementUpdaterService> logger)
{
    /// <summary>
    /// Update risk windows and generate recommendations for all tracked symbols
    /// </summary>
    public async Task UpdateRiskManagementAsync(CancellationToken token = default)
    {
        logger.LogInformation("Starting risk management update");

        try
        {
            // Get tracked symbols from configuration
            var trackedSymbols = configuration
                .GetSection("RiskManagement:TrackedSymbols")
                .Get<List<string>>() ?? new List<string> { "AAPL", "MSFT", "GOOGL" };

            logger.LogInformation("Updating risk management for {Count} symbols", trackedSymbols.Count);

            var now = DateTime.UtcNow;

            foreach (var symbol in trackedSymbols)
            {
                try
                {
                    // 1. Identify current risk window
                    var riskWindowResult = await riskWindowService.IdentifyRiskWindowAsync(
                        symbol, 
                        now, 
                        token);

                    if (riskWindowResult.IsSuccess && riskWindowResult.Value != null)
                    {
                        logger.LogInformation(
                            "Identified risk window for {Symbol}: Level={Level}, Start={Start}, End={End}",
                            symbol,
                            riskWindowResult.Value.Level,
                            riskWindowResult.Value.StartDate,
                            riskWindowResult.Value.EndDate);
                    }

                    // 2. Generate recommendations (without position data - will use default leverage)
                    var recommendationsResult = await riskRecommendationService.GenerateRecommendationsAsync(
                        symbol, 
                        null, 
                        token);

                    if (recommendationsResult.IsSuccess && recommendationsResult.Value != null)
                    {
                        logger.LogInformation(
                            "Generated {Count} recommendations for {Symbol}",
                            recommendationsResult.Value.Count,
                            symbol);
                    }

                    // Small delay to avoid rate limiting
                    await Task.Delay(100, token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating risk management for {Symbol}", symbol);
                    // Continue with next symbol
                }
            }

            logger.LogInformation("Completed risk management update");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in risk management update cycle");
            throw;
        }
    }
}

