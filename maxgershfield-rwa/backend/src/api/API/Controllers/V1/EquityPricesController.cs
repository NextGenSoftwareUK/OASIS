using API.Controllers.Base;
using Application.Contracts;
using Application.DTOs.EquityPrice;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.V1;

/// <summary>
/// Controller for equity price endpoints
/// </summary>
[Route("api/oracle/rwa/equity")]
[Authorize]
public class EquityPricesController(IEquityPriceService equityPriceService) : V1BaseController
{
    /// <summary>
    /// Get current price for a symbol (defaults to adjusted)
    /// </summary>
    /// <param name="symbol">Stock ticker symbol (e.g., "AAPL")</param>
    /// <param name="adjusted">Whether to return adjusted price (default: true)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Current equity price with source breakdown</returns>
    [HttpGet("{symbol}/price")]
    [ResponseCache(Duration = 60)] // Cache for 1 minute
    public async Task<IActionResult> GetPrice(
        string symbol,
        [FromQuery] bool adjusted = true,
        CancellationToken token = default)
    {
        var result = adjusted
            ? await equityPriceService.GetAdjustedPriceAsync(symbol, token)
            : await equityPriceService.GetRawPriceAsync(symbol, token);

        return result.ToActionResult();
    }

    /// <summary>
    /// Get historical prices for a symbol
    /// </summary>
    /// <param name="symbol">Stock ticker symbol (e.g., "AAPL")</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="adjusted">Whether to return adjusted prices (default: true)</param>
    /// <param name="interval">Time interval (1d, 1h, 1m) - currently only 1d is supported</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Historical price data</returns>
    [HttpGet("{symbol}/price/history")]
    public async Task<IActionResult> GetPriceHistory(
        string symbol,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] bool adjusted = true,
        [FromQuery] string interval = "1d",
        CancellationToken token = default)
    {
        // Validate date range
        if (from > to)
        {
            return BadRequest(new { error = "From date must be before To date" });
        }

        // Validate interval (currently only daily is supported)
        if (interval != "1d")
        {
            return BadRequest(new { error = "Only '1d' interval is currently supported" });
        }

        var result = await equityPriceService.GetPriceHistoryAsync(symbol, from, to, adjusted, token);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get prices for multiple symbols at once
    /// </summary>
    /// <param name="symbols">Comma-separated list of symbols (e.g., "AAPL,MSFT,GOOGL")</param>
    /// <param name="adjusted">Whether to return adjusted prices (default: true)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Batch price data for all requested symbols</returns>
    [HttpGet("prices/batch")]
    [ResponseCache(Duration = 60)] // Cache for 1 minute
    public async Task<IActionResult> GetBatchPrices(
        [FromQuery] string symbols,
        [FromQuery] bool adjusted = true,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(symbols))
        {
            return BadRequest(new { error = "Symbols parameter is required" });
        }

        // Parse symbols and limit to 50
        var symbolList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .Take(50)
            .ToList();

        if (symbolList.Count == 0)
        {
            return BadRequest(new { error = "At least one valid symbol is required" });
        }

        var result = await equityPriceService.GetBatchPricesAsync(symbolList, adjusted, token);

        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        // Map to batch response DTO
        var batchResponse = new BatchEquityPriceResponseDto
        {
            Prices = result.Value!.Select(p => new EquityPriceSummaryDto
            {
                Symbol = p.Symbol,
                RawPrice = p.RawPrice,
                AdjustedPrice = p.AdjustedPrice,
                Confidence = p.Confidence,
                PriceDate = p.PriceDate,
                LastUpdated = p.LastUpdated
            }).ToList(),
            RequestedAt = DateTime.UtcNow
        };

        var batchResult = Result<BatchEquityPriceResponseDto>.Success(batchResponse);
        return batchResult.ToActionResult();
    }

    /// <summary>
    /// Get price at a specific date (for historical queries)
    /// </summary>
    /// <param name="symbol">Stock ticker symbol (e.g., "AAPL")</param>
    /// <param name="date">Target date</param>
    /// <param name="adjusted">Whether to return adjusted price (default: true)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Price at the specified date</returns>
    [HttpGet("{symbol}/price/at-date")]
    public async Task<IActionResult> GetPriceAtDate(
        string symbol,
        [FromQuery] DateTime date,
        [FromQuery] bool adjusted = true,
        CancellationToken token = default)
    {
        // Validate date is not in the future
        if (date > DateTime.UtcNow.Date.AddDays(1)) // Allow 1 day buffer for timezone differences
        {
            return BadRequest(new { error = "Date cannot be in the future" });
        }

        var result = await equityPriceService.GetPriceAtDateAsync(symbol, date, adjusted, token);
        return result.ToActionResult();
    }
}

