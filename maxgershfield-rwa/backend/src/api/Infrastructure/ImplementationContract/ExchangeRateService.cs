namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service responsible for handling operations related to exchange rates.
/// Includes functionality to retrieve paginated lists, specific rate details,
/// and the most recent exchange rate between two tokens.
/// Ensures logging for operation tracking and performance diagnostics.
/// </summary>
public sealed class ExchangeRateService(
    DataContext dbContext,
    ILogger<ExchangeRateService> logger) : IExchangeRateService
{
    /// <summary>
    /// Retrieves a paginated list of exchange rates based on filtering criteria.
    /// Applies optional filters for source and destination token IDs.
    /// </summary>
    /// <param name="filter">Filtering and pagination options.</param>
    /// <param name="token">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A successful result with paged exchange rate data, or an error result.
    /// </returns>
    public async Task<Result<PagedResponse<IEnumerable<GetExchangeRateResponse>>>> GetExchangeRatesAsync(
        ExchangeRateFilter filter, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetExchangeRatesAsync), date);

        IQueryable<GetExchangeRateResponse> exchangeRatesQuery = dbContext.ExchangeRates
            .AsNoTracking()
            .ApplyFilter(filter.FromTokenId.ToString(), x => x.FromTokenId.ToString())
            .ApplyFilter(filter.ToTokenId.ToString(), x => x.ToTokenId.ToString())
            .OrderBy(x => x.Id)
            .Select(x => new GetExchangeRateResponse(
                x.Id,
                x.FromTokenId,
                x.FromToken.ToReadDetail(),
                x.ToTokenId,
                x.ToToken.ToReadDetail(),
                x.Rate,
                x.CreatedAt));

        int totalCount = await exchangeRatesQuery.CountAsync(token);

        PagedResponse<IEnumerable<GetExchangeRateResponse>> result =
            PagedResponse<IEnumerable<GetExchangeRateResponse>>.Create(
                filter.PageSize,
                filter.PageNumber,
                totalCount,
                exchangeRatesQuery.Page(filter.PageNumber, filter.PageSize));

        logger.OperationCompleted(nameof(GetExchangeRatesAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - date);

        return Result<PagedResponse<IEnumerable<GetExchangeRateResponse>>>.Success(result);
    }

    /// <summary>
    /// Retrieves detailed information about a specific exchange rate by its ID.
    /// </summary>
    /// <param name="exchangeRateId">The unique identifier of the exchange rate.</param>
    /// <param name="token">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A result containing exchange rate details if found, or an error result if not found.
    /// </returns>
    public async Task<Result<GetExchangeRateDetailResponse>> GetExchangeRateDetailAsync(Guid exchangeRateId,
        CancellationToken token)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetExchangeRateDetailAsync), date);

        GetExchangeRateDetailResponse? exchangeRate = await dbContext.ExchangeRates
            .AsNoTracking()
            .Where(x => x.Id == exchangeRateId)
            .Select(x => new GetExchangeRateDetailResponse(
                x.Id,
                x.FromTokenId,
                x.FromToken.ToReadDetail(),
                x.ToTokenId,
                x.ToToken.ToReadDetail(),
                x.Rate,
                x.CreatedAt))
            .FirstOrDefaultAsync(token);

        logger.OperationCompleted(nameof(GetExchangeRateDetailAsync),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);

        if (exchangeRate is not null)
            return Result<GetExchangeRateDetailResponse>.Success(exchangeRate);

        return Result<GetExchangeRateDetailResponse>.Failure(
            ResultPatternError.NotFound(Messages.ExchangeRateNotFound));
    }

    /// <summary>
    /// Retrieves the most recently recorded exchange rate between two tokens,
    /// based on their symbols.
    /// </summary>
    /// <param name="request">Contains the symbols of the source and destination tokens.</param>
    /// <param name="token">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A result containing the most recent exchange rate, or an error result if not found.
    /// </returns>
    public async Task<Result<GetCurrentExchangeRateDetailResponse>> GetCurrentExchangeRateDetailAsync(
        ExchangeRateRequest request, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetCurrentExchangeRateDetailAsync), date);

        GetCurrentExchangeRateDetailResponse? exchangeRate = await dbContext.ExchangeRates
            .AsNoTracking()
            .Include(x => x.FromToken)
            .Include(x => x.ToToken)
            .Where(x => x.FromToken.Symbol == request.FromToken && x.ToToken.Symbol == request.ToToken)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new GetCurrentExchangeRateDetailResponse(
                x.Id,
                x.FromToken.Symbol,
                x.ToToken.Symbol,
                x.Rate,
                x.CreatedAt))
            .FirstOrDefaultAsync(token);

        logger.OperationCompleted(nameof(GetCurrentExchangeRateDetailAsync),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);

        if (exchangeRate is not null)
            return Result<GetCurrentExchangeRateDetailResponse>.Success(exchangeRate);

        return Result<GetCurrentExchangeRateDetailResponse>.Failure(
            ResultPatternError.NotFound(Messages.ExchangeRateNotFound));
    }
}