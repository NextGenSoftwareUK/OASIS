namespace Application.DTOs.ExchangeRate.Responses;

/// <summary>
/// Represents a lightweight response model for an exchange rate record,
/// typically used in list or summary views.
/// </summary>
/// <param name="Id">The unique identifier of the exchange rate entry.</param>
/// <param name="FromTokenId">The ID of the source token in the exchange.</param>
/// <param name="FromNetworkToken">
/// Detailed information about the source token and its associated network.
/// </param>
/// <param name="ToTokenId">The ID of the destination token in the exchange.</param>
/// <param name="ToNetworkToken">
/// Detailed information about the destination token and its associated network.
/// </param>
/// <param name="Rate">The numerical value representing the exchange rate.</param>
/// <param name="CreatedAt">The timestamp indicating when the rate was recorded.</param>
public record GetExchangeRateResponse(
    Guid Id,
    Guid FromTokenId,
    GetNetworkTokenDetailResponse FromNetworkToken,
    Guid ToTokenId,
    GetNetworkTokenDetailResponse ToNetworkToken,
    decimal Rate,
    DateTimeOffset CreatedAt
);