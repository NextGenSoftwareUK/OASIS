namespace ScGen.Lib.Shared.DTOs.Requests;

/// <summary>
/// Request to verify x402 payment
/// </summary>
public record VerifyPaymentRequest
{
    /// <summary>
    /// Solana transaction signature
    /// </summary>
    public required string Signature { get; init; }

    /// <summary>
    /// Operation being paid for (generate, compile, deploy)
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Blockchain/language (Solidity, Rust, Scrypto)
    /// </summary>
    public required string Blockchain { get; init; }

    /// <summary>
    /// Amount paid in SOL
    /// </summary>
    public decimal Amount { get; init; }
}

