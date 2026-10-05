namespace Application.DTOs.ExchangeRate.Responses;

/// <summary>
/// Represents a response containing the most recent exchange rate
/// between two tokens, including basic identifiers and a human-readable message.
/// Used primarily for real-time or latest-rate views.
/// </summary>
/// <param name="ExchangeRateId">The unique identifier of the exchange rate record.</param>
/// <param name="FromToken">The symbol or short name of the source token (e.g., "BTC").</param>
/// <param name="ToToken">The symbol or short name of the destination token (e.g., "USDT").</param>
/// <param name="Rate">The most recent exchange rate value between the two tokens.</param>
/// <param name="Timestamp">The UTC time when this rate was recorded.</param>
public record GetCurrentExchangeRateDetailResponse(
    Guid ExchangeRateId,
    string FromToken,
    string ToToken,
    decimal Rate,
    DateTimeOffset Timestamp
);