namespace Application.DTOs.FundingRate;

/// <summary>
/// Response containing funding rate information with all factors
/// </summary>
public sealed record FundingRateResponse
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Rate { get; init; } // Annualized percentage (e.g., 0.1 = 10%)
    public decimal HourlyRate { get; init; } // Rate per hour
    public decimal MarkPrice { get; init; }
    public decimal SpotPrice { get; init; }
    public decimal AdjustedSpotPrice { get; init; }
    public decimal Premium { get; init; } // Mark - Spot (can be negative)
    public decimal PremiumPercentage { get; init; } // (Mark - Spot) / Spot * 100
    public FundingRateFactorsDto Factors { get; init; } = null!;
    public DateTime CalculatedAt { get; init; }
    public DateTime ValidUntil { get; init; } // Usually +1 hour
    public string? OnChainTransactionHash { get; init; }
}

/// <summary>
/// Breakdown of funding rate factors
/// </summary>
public sealed record FundingRateFactorsDto
{
    public decimal BaseRate { get; init; } // Standard funding from premium
    public decimal CorporateActionAdjustment { get; init; } // Boost during CA windows
    public decimal LiquidityAdjustment { get; init; } // Adjustment for low liquidity
    public decimal VolatilityAdjustment { get; init; } // Adjustment based on volatility
    public decimal FinalRate { get; init; } // Sum of all factors
}

/// <summary>
/// Historical funding rate item
/// </summary>
public sealed record FundingRateHistoryItem
{
    public decimal Rate { get; init; }
    public decimal HourlyRate { get; init; }
    public decimal MarkPrice { get; init; }
    public decimal SpotPrice { get; init; }
    public decimal AdjustedSpotPrice { get; init; }
    public decimal Premium { get; init; }
    public decimal PremiumPercentage { get; init; }
    public DateTime CalculatedAt { get; init; }
    public DateTime ValidUntil { get; init; }
}

/// <summary>
/// Funding rate factors breakdown response
/// </summary>
public sealed record FundingRateFactorsResponse
{
    public string Symbol { get; init; } = string.Empty;
    public FundingRateFactorsDto Factors { get; init; } = null!;
    public decimal MarkPrice { get; init; }
    public decimal SpotPrice { get; init; }
    public decimal AdjustedSpotPrice { get; init; }
    public decimal Premium { get; init; }
    public DateTime CalculatedAt { get; init; }
}

/// <summary>
/// On-chain status information for funding rate
/// </summary>
public sealed record OnChainStatusDto
{
    public bool Published { get; init; }
    public string? TransactionHash { get; init; }
    public string? AccountAddress { get; init; }
    public DateTime? LastPublishedAt { get; init; }
}

/// <summary>
/// Funding rate response with on-chain status
/// </summary>
public sealed record FundingRateWithOnChainStatusResponse
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public decimal HourlyRate { get; init; }
    public decimal MarkPrice { get; init; }
    public decimal SpotPrice { get; init; }
    public decimal AdjustedSpotPrice { get; init; }
    public decimal Premium { get; init; }
    public decimal PremiumPercentage { get; init; }
    public FundingRateFactorsDto Factors { get; init; } = null!;
    public DateTime CalculatedAt { get; init; }
    public DateTime ValidUntil { get; init; }
    public OnChainStatusDto? OnChainStatus { get; init; }
}

/// <summary>
/// Funding rate history response
/// </summary>
public sealed record FundingRateHistoryResponseDto
{
    public string Symbol { get; init; } = string.Empty;
    public List<FundingRateHistoryItem> Rates { get; init; } = new();
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
}

/// <summary>
/// Batch funding rate response
/// </summary>
public sealed record BatchFundingRateResponseDto
{
    public List<FundingRateSummaryDto> Rates { get; init; } = new();
    public DateTime RequestedAt { get; init; }
}

/// <summary>
/// Summary of funding rate for batch responses
/// </summary>
public sealed record FundingRateSummaryDto
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public decimal HourlyRate { get; init; }
    public DateTime CalculatedAt { get; init; }
    public DateTime ValidUntil { get; init; }
    public bool OnChainPublished { get; init; }
}

/// <summary>
/// Response for on-chain publishing operation
/// </summary>
public sealed record PublishOnChainResponseDto
{
    public string Symbol { get; init; } = string.Empty;
    public string? TransactionHash { get; init; }
    public string? AccountAddress { get; init; }
    public DateTime PublishedAt { get; init; }
}

