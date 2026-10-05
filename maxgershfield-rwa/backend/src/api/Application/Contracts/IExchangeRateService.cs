namespace Application.Contracts;

/// <summary>
/// Defines the contract for exchange rate operations,
/// including retrieval of exchange rate lists, details, and current rates.
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Retrieves a paginated list of exchange rates based on provided filter criteria.
    /// </summary>
    /// <param name="filter">The filtering options, including token IDs and pagination parameters.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result object containing a paged collection of exchange rate responses.
    /// </returns>
    Task<Result<PagedResponse<IEnumerable<GetExchangeRateResponse>>>> GetExchangeRatesAsync(
        ExchangeRateFilter filter,
        CancellationToken token = default);

    /// <summary>
    /// Retrieves detailed information about a specific exchange rate by its unique identifier.
    /// </summary>
    /// <param name="exchangeRateId">The unique identifier of the exchange rate.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result object containing detailed information about the exchange rate if found.
    /// </returns>
    Task<Result<GetExchangeRateDetailResponse>> GetExchangeRateDetailAsync(
        Guid exchangeRateId,
        CancellationToken token = default);

    /// <summary>
    /// Retrieves the most recent exchange rate between two tokens,
    /// identified by their symbols.
    /// </summary>
    /// <param name="request">An object containing the symbols of the source and destination tokens.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result object containing the most recent exchange rate detail if available.
    /// </returns>
    Task<Result<GetCurrentExchangeRateDetailResponse>> GetCurrentExchangeRateDetailAsync(
        ExchangeRateRequest request,
        CancellationToken token = default);
}