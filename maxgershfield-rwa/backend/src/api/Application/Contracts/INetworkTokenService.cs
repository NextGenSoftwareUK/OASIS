namespace Application.Contracts;

/// <summary>
/// Contract for the NetworkTokenService, defines the operations for managing network tokens,
/// including listing, retrieving, creating, updating, and deleting.
/// </summary>
public interface INetworkTokenService
{
    /// <summary>
    /// Retrieves a paginated list of network tokens matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">Filtering and pagination options.</param>
    /// <param name="token">Optional cancellation token.</param>
    /// <returns>Paginated result containing network token data.</returns>
    Task<Result<PagedResponse<IEnumerable<GetNetworkTokenResponse>>>>
        GetNetworkTokensAsync(NetworkTokenFilter filter, CancellationToken token = default);

    /// <summary>
    /// Retrieves the detailed information of a single network token by its unique identifier.
    /// </summary>
    /// <param name="networkTokenId">The unique identifier of the network token.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The details of the requested network token, or a failure result if not found.</returns>
    Task<Result<GetNetworkTokenDetailResponse>>
        GetNetworkTokenDetailAsync(Guid networkTokenId, CancellationToken token);

    /// <summary>
    /// Creates a new network token based on the provided request data.
    /// </summary>
    /// <param name="request">The data required to create the token.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The result of the creation operation, including the new token's ID if successful.</returns>
    Task<Result<CreateNetworkTokenResponse>>
        CreateNetworkTokenAsync(CreateNetworkTokenRequest request, CancellationToken token);

    /// <summary>
    /// Updates an existing network token with the provided data.
    /// Ensures the updated symbol remains unique within the same network.
    /// </summary>
    /// <param name="networkTokenId">The ID of the token to update.</param>
    /// <param name="request">The update data.</param>
    /// <param name="token">Optional cancellation token.</param>
    /// <returns>The result of the update operation.</returns>
    Task<Result<UpdateNetworkTokenResponse>>
        UpdateNetworkTokenAsync(Guid networkTokenId, UpdateNetworkTokenRequest request,
            CancellationToken token = default);

    /// <summary>
    /// Deletes the specified network token by marking it as removed.
    /// </summary>
    /// <param name="networkTokenId">The ID of the token to delete.</param>
    /// <param name="token">Optional cancellation token.</param>
    /// <returns>The result of the deletion operation.</returns>
    Task<Result<DeleteNetworkTokenResponse>> DeleteNetworkTokenAsync(Guid networkTokenId,
        CancellationToken token = default);
}