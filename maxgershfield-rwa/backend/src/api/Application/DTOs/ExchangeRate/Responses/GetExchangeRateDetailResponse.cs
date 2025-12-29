namespace Application.DTOs.ExchangeRate.Responses;

/// <summary>
/// Represents a detailed response model for a single exchange rate entry,
/// including token identifiers, network-level details, rate value, and timestamp.
/// Typically used for detailed views or record inspection.
/// </summary>
/// <param name="Id">The unique identifier of the exchange rate.</param>
/// <param name="FromTokenId">The unique ID of the token being exchanged from.</param>
/// <param name="FromNetworkToken">
/// Full details of the source token and its network (e.g., symbol, name, network name).
/// </param>
/// <param name="ToTokenId">The unique ID of the token being exchanged to.</param>
/// <param name="ToNetworkToken">
/// Full details of the destination token and its network (e.g., symbol, name, network name).
/// </param>
/// <param name="Rate">The actual exchange rate between the two tokens.</param>
/// <param name="CreatedAt">The UTC timestamp when this exchange rate was recorded.</param>
public record GetExchangeRateDetailResponse(
    Guid Id,
    Guid FromTokenId,
    GetNetworkTokenDetailResponse FromNetworkToken,
    Guid ToTokenId,
    GetNetworkTokenDetailResponse ToNetworkToken,
    decimal Rate,
    DateTimeOffset CreatedAt
);