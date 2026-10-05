namespace Application.DTOs.RiskManagement;

/// <summary>
/// Represents a position in a perpetual futures market
/// </summary>
public sealed record Position
{
    /// <summary>
    /// Position ID from perp DEX
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Position size
    /// </summary>
    public decimal Size { get; init; }

    /// <summary>
    /// Current leverage
    /// </summary>
    public decimal Leverage { get; init; }

    /// <summary>
    /// Entry price
    /// </summary>
    public decimal EntryPrice { get; init; }

    /// <summary>
    /// Current mark price
    /// </summary>
    public decimal MarkPrice { get; init; }

    /// <summary>
    /// Liquidation price
    /// </summary>
    public decimal LiquidationPrice { get; init; }

    /// <summary>
    /// User/avatar address (optional)
    /// </summary>
    public string? UserAddress { get; init; }
}

