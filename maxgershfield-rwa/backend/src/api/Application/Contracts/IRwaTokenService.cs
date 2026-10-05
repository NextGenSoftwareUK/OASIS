namespace Application.Contracts;

public interface IRwaTokenService
{
    Task<Result<PagedResponse<IEnumerable<GetRwaTokensResponse>>>>
        GetAllAsync(RwaTokenFilter filter, CancellationToken token = default);

    Task<Result<GetRwaTokenDetailResponse>>
        GetDetailAsync(Guid id, CancellationToken token = default);

    Task<Result<PagedResponse<IEnumerable<GetRwaTokenDetailResponse>>>>
        GetTokensOwnedByCurrentUserAsync(RwaTokenOwnerFilter filter, CancellationToken token = default);

    Task<Result<CreateRwaTokenResponse>>
        CreateAsync(CreateRwaTokenRequest request, CancellationToken token = default);

    Task<Result<UpdateRwaTokenResponse>>
        UpdateAsync(Guid id, UpdateRwaTokenRequest request, CancellationToken token = default);
}