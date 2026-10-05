namespace Infrastructure.ImplementationContract;

public sealed class RwaTokenPriceHistoryService(
    ILogger<RwaTokenPriceHistoryService> logger,
    DataContext dbContext) : IRwaTokenPriceHistoryService
{
    public async Task<Result<PagedResponse<IEnumerable<GetRwaTokenPriceHistoryResponse>>>> GetAsync(
        RwaTokenPriceHistoryFilter filter, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetAsync), date);

        try
        {
            IQueryable<GetRwaTokenPriceHistoryResponse> query = dbContext.RwaTokenPriceHistories
                .AsNoTracking()
                .WhereIf(filter.Id is not null, x => x.Id == filter.Id)
                .WhereIf(filter.ChangedAt is not null, x => x.ChangedAt == filter.ChangedAt)
                .WhereIf(filter.NewPrice is not null, x => x.NewPrice == filter.NewPrice)
                .WhereIf(filter.OldPrice is not null, x => x.OldPrice == filter.OldPrice)
                .WhereIf(filter.RwaTokenId is not null, x => x.RwaTokenId == filter.RwaTokenId)
                .OrderBy(x => x.Id)
                .Select(x => x.ToRead());

            int totalCount = await query.CountAsync(token);

            PagedResponse<IEnumerable<GetRwaTokenPriceHistoryResponse>> result =
                PagedResponse<IEnumerable<GetRwaTokenPriceHistoryResponse>>.Create(
                    filter.PageSize,
                    filter.PageNumber,
                    totalCount,
                    query.Page(filter.PageNumber, filter.PageSize));

            logger.OperationCompleted(nameof(GetAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<PagedResponse<IEnumerable<GetRwaTokenPriceHistoryResponse>>>.Success(result);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetAsync), ex.Message);
            logger.OperationCompleted(nameof(GetAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<PagedResponse<IEnumerable<GetRwaTokenPriceHistoryResponse>>>.Failure(
                ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<IEnumerable<GetRwaTokenPriceHistoryResponse>>> GetAsync(Guid id,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetAsync), date);

        try
        {
            return Result<IEnumerable<GetRwaTokenPriceHistoryResponse>>.Success(await dbContext.RwaTokenPriceHistories
                .Where(x => x.RwaTokenId == id)
                .Select(x => x.ToRead())
                .ToListAsync(token));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetAsync), ex.Message);
            logger.OperationCompleted(nameof(GetAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<IEnumerable<GetRwaTokenPriceHistoryResponse>>.Failure(
                ResultPatternError.InternalServerError(ex.Message));
        }
    }
}