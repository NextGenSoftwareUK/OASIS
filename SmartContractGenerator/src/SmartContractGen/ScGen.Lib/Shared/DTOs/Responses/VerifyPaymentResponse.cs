namespace ScGen.Lib.Shared.DTOs.Responses;

/// <summary>
/// Response from payment verification
/// </summary>
public record VerifyPaymentResponse
{
    /// <summary>
    /// Payment verification status
    /// </summary>
    public required bool Verified { get; init; }

    /// <summary>
    /// JWT token to use for authenticated API calls
    /// </summary>
    public string PaymentToken { get; init; } = string.Empty;

    /// <summary>
    /// Token expiration timestamp
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Transaction signature
    /// </summary>
    public string Signature { get; init; } = string.Empty;

    /// <summary>
    /// Amount verified in SOL
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Operation authorized
    /// </summary>
    public string Operation { get; init; } = string.Empty;

    /// <summary>
    /// Blockchain authorized
    /// </summary>
    public string Blockchain { get; init; } = string.Empty;
}

/// <summary>
/// 402 Payment Required response
/// </summary>
public record PaymentRequiredResponse
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Error { get; init; } = "Payment Required";

    /// <summary>
    /// Accepted payment methods
    /// </summary>
    public string[] AcceptedMethods { get; init; } = new[] { "solana-x402" };

    /// <summary>
    /// Pricing information
    /// </summary>
    public required PricingInfo Pricing { get; init; }

    /// <summary>
    /// x402 payment endpoint
    /// </summary>
    public string X402Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Treasury wallet address
    /// </summary>
    public string TreasuryAddress { get; init; } = string.Empty;
}

/// <summary>
/// Pricing information
/// </summary>
public record PricingInfo
{
    public required string Operation { get; init; }
    public required string Blockchain { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "SOL";
    public int DistributionPercentage { get; init; } = 90;
}

