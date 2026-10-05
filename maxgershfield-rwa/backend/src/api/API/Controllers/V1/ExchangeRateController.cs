namespace API.Controllers.V1;

/// <summary>
/// Controller for handling exchange rate-related requests. 
/// This includes retrieving historical exchange rates, details of a specific exchange rate, 
/// and fetching the current exchange rate.
/// </summary>
[Route($"{ApiAddresses.Base}")]
[AllowAnonymous]
public sealed class ExchangeRateController(IExchangeRateService exchangeRateService) : V1BaseController
{
    /// <summary>
    /// Retrieves the historical exchange rates based on the provided filter.
    /// The filter may include parameters such as date range, currency pair, etc.
    /// This method is designed to be efficient, leveraging asynchronous processing for 
    /// potentially large datasets.
    /// </summary>
    /// <param name="filter">The filter criteria for retrieving exchange rate history.</param>
    /// <param name="cancellationToken">A token for requesting cancellation of the operation.</param>
    /// <returns>Returns an <see cref="IActionResult"/> containing the historical exchange rates.</returns>
    [HttpGet("history")]
    public async Task<IActionResult> GetExchangeRatesAsync([FromQuery] ExchangeRateFilter filter,
        CancellationToken cancellationToken)
        => (await exchangeRateService.GetExchangeRatesAsync(filter, cancellationToken)).ToActionResult();

    /// <summary>
    /// Retrieves the detailed information for a specific exchange rate identified by its GUID.
    /// This method provides a deep dive into the exchange rate data, which may include 
    /// metadata and associated details that aren't present in the standard historical data.
    /// </summary>
    /// <param name="exchangeRateId">The unique identifier (GUID) for the specific exchange rate.</param>
    /// <param name="cancellationToken">A token for requesting cancellation of the operation.</param>
    /// <returns>Returns an <see cref="IActionResult"/> containing the detailed exchange rate information.</returns>
    [HttpGet("history/{exchangeRateId:guid}")]
    public async Task<IActionResult> GetExchangeRateDetailAsync(Guid exchangeRateId,
        CancellationToken cancellationToken)
        => (await exchangeRateService.GetExchangeRateDetailAsync(exchangeRateId, cancellationToken)).ToActionResult();

    /// <summary>
    /// Retrieves the current exchange rate details for a specific currency pair.
    /// This method is optimized for low-latency responses, ideal for applications requiring 
    /// real-time currency exchange information.
    /// </summary>
    /// <param name="request">The request containing the currency pair for which the exchange rate is requested.</param>
    /// <param name="token">A token for requesting cancellation of the operation.</param>
    /// <returns>Returns an <see cref="IActionResult"/> containing the current exchange rate details.</returns>
    [HttpGet("exchange-rate")]
    public async Task<IActionResult> ExchangeRateAsync([FromQuery] ExchangeRateRequest request,
        CancellationToken token)
        => (await exchangeRateService.GetCurrentExchangeRateDetailAsync(request, token)).ToActionResult();
}