using API.Controllers.Base;
using Application.Contracts;
using Application.DTOs.FundingRate;
using BuildingBlocks.Extensions.ResultPattern;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.V1;

/// <summary>
/// Controller for managing funding rates for perpetual futures on RWAs/equities
/// </summary>
[Route("api/oracle/rwa/funding")]
[Authorize]
public class FundingRatesController(
    IFundingRateService fundingRateService,
    IOnChainFundingPublisherFactory publisherFactory,
    ILogger<FundingRatesController> logger) : V1BaseController
{
    /// <summary>
    /// Get current funding rate for a symbol
    /// </summary>
    /// <param name="symbol">Stock ticker symbol (e.g., "AAPL")</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Current funding rate with on-chain status</returns>
    [HttpGet("{symbol}/rate")]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<IActionResult> GetFundingRate(
        string symbol,
        CancellationToken token = default)
    {
        try
        {
            // Get funding rate from service
            var rateResult = await fundingRateService.GetCurrentFundingRateAsync(symbol, token);
            if (!rateResult.IsSuccess || rateResult.Value is null)
            {
                return rateResult.ToActionResult();
            }

            var rate = rateResult.Value;

            // Get on-chain status from publisher
            OnChainStatusDto? onChainStatus = null;
            try
            {
                var publisher = publisherFactory.GetPrimaryPublisher();
                var onChainRate = await publisher.GetOnChainFundingRateAsync(symbol, token);
                
                if (onChainRate is not null)
                {
                    onChainStatus = new OnChainStatusDto
                    {
                        Published = true,
                        TransactionHash = onChainRate.TransactionHash,
                        AccountAddress = onChainRate.AccountAddress,
                        LastPublishedAt = onChainRate.LastUpdated
                    };
                }
                else
                {
                    // Check if there's a transaction hash in the funding rate
                    if (!string.IsNullOrEmpty(rate.OnChainTransactionHash))
                    {
                        onChainStatus = new OnChainStatusDto
                        {
                            Published = true,
                            TransactionHash = rate.OnChainTransactionHash,
                            LastPublishedAt = rate.CalculatedAt
                        };
                    }
                    else
                    {
                        onChainStatus = new OnChainStatusDto
                        {
                            Published = false
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to retrieve on-chain status for {Symbol}", symbol);
                // Continue without on-chain status rather than failing the request
                onChainStatus = new OnChainStatusDto
                {
                    Published = !string.IsNullOrEmpty(rate.OnChainTransactionHash),
                    TransactionHash = rate.OnChainTransactionHash
                };
            }

            // Create response with on-chain status
            var response = new FundingRateWithOnChainStatusResponse
            {
                Symbol = rate.Symbol,
                Rate = rate.Rate,
                HourlyRate = rate.HourlyRate,
                MarkPrice = rate.MarkPrice,
                SpotPrice = rate.SpotPrice,
                AdjustedSpotPrice = rate.AdjustedSpotPrice,
                Premium = rate.Premium,
                PremiumPercentage = rate.PremiumPercentage,
                Factors = rate.Factors,
                CalculatedAt = rate.CalculatedAt,
                ValidUntil = rate.ValidUntil,
                OnChainStatus = onChainStatus
            };

            return Result<FundingRateWithOnChainStatusResponse>.Success(response).ToActionResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting funding rate for {Symbol}", symbol);
            return Result<FundingRateWithOnChainStatusResponse>
                .Failure(ResultPatternError.InternalServerError($"Error getting funding rate: {ex.Message}"))
                .ToActionResult();
        }
    }

    /// <summary>
    /// Get funding rate history for a symbol
    /// </summary>
    /// <param name="symbol">Stock ticker symbol (e.g., "AAPL")</param>
    /// <param name="hours">Number of hours to look back (default: 24, max: 168)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Funding rate history</returns>
    [HttpGet("{symbol}/rate/history")]
    public async Task<IActionResult> GetFundingRateHistory(
        string symbol,
        [FromQuery] int hours = 24,
        CancellationToken token = default)
    {
        // Limit hours to max 168 (1 week)
        if (hours > 168)
        {
            hours = 168;
        }
        if (hours < 1)
        {
            hours = 24;
        }

        var historyResult = await fundingRateService.GetFundingRateHistoryAsync(symbol, hours, token);
        if (!historyResult.IsSuccess || historyResult.Value is null)
        {
            return historyResult.ToActionResult();
        }

        var history = historyResult.Value;
        var fromDate = history.Any() ? history.Min(r => r.CalculatedAt) : DateTime.UtcNow.AddHours(-hours);
        var toDate = history.Any() ? history.Max(r => r.CalculatedAt) : DateTime.UtcNow;

        var response = new FundingRateHistoryResponseDto
        {
            Symbol = symbol,
            Rates = history,
            FromDate = fromDate,
            ToDate = toDate
        };

        return Result<FundingRateHistoryResponseDto>.Success(response).ToActionResult();
    }

    /// <summary>
    /// Get funding rates for multiple symbols in a single call
    /// </summary>
    /// <param name="symbols">Comma-separated list of symbols (e.g., "AAPL,MSFT,GOOGL")</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Batch funding rates</returns>
    [HttpGet("rates/batch")]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<IActionResult> GetBatchFundingRates(
        [FromQuery] string symbols,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(symbols))
        {
            return Result<BatchFundingRateResponseDto>
                .Failure(ResultPatternError.BadRequest("Symbols parameter is required"))
                .ToActionResult();
        }

        var symbolList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

        // Limit batch size to 50 symbols
        if (symbolList.Count > 50)
        {
            symbolList = symbolList.Take(50).ToList();
        }

        var batchResult = await fundingRateService.GetBatchFundingRatesAsync(symbolList, token);
        if (!batchResult.IsSuccess || batchResult.Value is null)
        {
            return batchResult.ToActionResult();
        }

        var rates = batchResult.Value;
        var publisher = publisherFactory.GetPrimaryPublisher();

        var summaries = new List<FundingRateSummaryDto>();

        foreach (var kvp in rates)
        {
            var symbol = kvp.Key;
            var rate = kvp.Value;

            // Try to get on-chain status
            bool onChainPublished = false;
            try
            {
                var onChainRate = await publisher.GetOnChainFundingRateAsync(symbol, token);
                onChainPublished = onChainRate is not null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to check on-chain status for {Symbol}", symbol);
                // Fallback to checking transaction hash
                onChainPublished = !string.IsNullOrEmpty(rate.OnChainTransactionHash);
            }

            summaries.Add(new FundingRateSummaryDto
            {
                Symbol = symbol,
                Rate = rate.Rate,
                HourlyRate = rate.HourlyRate,
                CalculatedAt = rate.CalculatedAt,
                ValidUntil = rate.ValidUntil,
                OnChainPublished = onChainPublished
            });
        }

        var response = new BatchFundingRateResponseDto
        {
            Rates = summaries,
            RequestedAt = DateTime.UtcNow
        };

        return Result<BatchFundingRateResponseDto>.Success(response).ToActionResult();
    }

    /// <summary>
    /// Manually trigger on-chain publishing of funding rate (admin only)
    /// </summary>
    /// <param name="symbol">Stock ticker symbol (e.g., "AAPL")</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Publishing result with transaction hash</returns>
    [HttpPost("{symbol}/publish-onchain")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> PublishOnChain(
        string symbol,
        CancellationToken token = default)
    {
        try
        {
            // Get current funding rate
            var rateResult = await fundingRateService.GetCurrentFundingRateAsync(symbol, token);
            if (!rateResult.IsSuccess || rateResult.Value is null)
            {
                return rateResult.ToActionResult();
            }

            var rate = rateResult.Value;

            // Get publisher
            var publisher = publisherFactory.GetPrimaryPublisher();

            // Publish to on-chain
            var publishResult = await publisher.PublishFundingRateAsync(symbol, rate, token);

            if (!publishResult.Success)
            {
                return Result<PublishOnChainResponseDto>
                    .Failure(ResultPatternError.InternalServerError(
                        publishResult.ErrorMessage ?? "Failed to publish funding rate on-chain"))
                    .ToActionResult();
            }

            var response = new PublishOnChainResponseDto
            {
                Symbol = symbol,
                TransactionHash = publishResult.TransactionHash,
                AccountAddress = publishResult.AccountAddress,
                PublishedAt = publishResult.PublishedAt
            };

            return Result<PublishOnChainResponseDto>.Success(response).ToActionResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing funding rate on-chain for {Symbol}", symbol);
            return Result<PublishOnChainResponseDto>
                .Failure(ResultPatternError.InternalServerError($"Error publishing on-chain: {ex.Message}"))
                .ToActionResult();
        }
    }
}
