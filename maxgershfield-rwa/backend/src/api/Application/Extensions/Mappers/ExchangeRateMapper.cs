namespace Application.Extensions.Mappers;

/// <summary>
/// Provides mapping functionality for converting ExchangeRate domain entities into response models.
/// This class contains methods for mapping ExchangeRate entities to different DTOs for reading and displaying exchange rate information.
/// </summary>
public static class ExchangeRateMapper
{
    /// <summary>
    /// Maps an ExchangeRate entity to a GetExchangeRateResponse DTO.
    /// </summary>
    /// <param name="rate">The ExchangeRate entity to be mapped.</param>
    /// <returns>A GetExchangeRateResponse DTO containing exchange rate information.</returns>
    public static GetExchangeRateResponse ToRead(this ExchangeRate rate)
        => new(
            rate.Id,
            rate.FromToken.NetworkId,
            rate.FromToken.ToReadDetail(),
            rate.ToToken.NetworkId,
            rate.ToToken.ToReadDetail(),
            rate.Rate,
            rate.CreatedAt);

    /// <summary>
    /// Maps an ExchangeRate entity to a GetExchangeRateDetailResponse DTO.
    /// </summary>
    /// <param name="rate">The ExchangeRate entity to be mapped.</param>
    /// <returns>A GetExchangeRateDetailResponse DTO containing detailed exchange rate information.</returns>
    public static GetExchangeRateDetailResponse ToReadDetail(this ExchangeRate rate)
        => new(
            rate.Id,
            rate.FromToken.NetworkId,
            rate.FromToken.ToReadDetail(),
            rate.ToToken.NetworkId,
            rate.ToToken.ToReadDetail(),
            rate.Rate,
            rate.CreatedAt);
}