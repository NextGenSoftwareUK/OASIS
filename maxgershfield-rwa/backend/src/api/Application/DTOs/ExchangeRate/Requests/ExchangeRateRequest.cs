namespace Application.DTOs.ExchangeRate.Requests;

/// <summary>
/// Represents a request to retrieve the latest exchange rate between two tokens.
/// Typically used in scenarios where a client wants to get the current rate
/// without referencing specific token IDs.
/// </summary>
/// <param name="FromToken">The symbol of the source token (e.g., "ETH").</param>
/// <param name="ToToken">The symbol of the destination token (e.g., "USDC").</param>
public record ExchangeRateRequest(
    string FromToken,
    string ToToken);