namespace Application.Contracts;

public interface IRwaTokenPriceHistoryService
{
    Task<Result<PagedResponse<IEnumerable<GetRwaTokenPriceHistoryResponse>>>> GetAsync(
        RwaTokenPriceHistoryFilter filter, CancellationToken token = default);

    Task<Result<IEnumerable<GetRwaTokenPriceHistoryResponse>>> GetAsync(Guid id, CancellationToken token = default);
}