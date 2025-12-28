namespace Application.Contracts
{
    /// <summary>
    /// Defines the operations related to networks such as retrieving networks,
    /// network details, and performing create, update, and delete operations.
    /// </summary>
    public interface INetworkService
    {
        /// <summary>
        /// Retrieves a paged list of networks matching the provided filter criteria.
        /// </summary>
        /// <param name="filter">
        /// The criteria used to filter and paginate the networks.
        /// </param>
        /// <param name="token">
        /// A cancellation token for the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see>
        ///     <cref>Result{PagedResponse{IEnumerable{GetNetworkResponse}}}</cref>
        /// </see>
        /// containing the paged list of networks.
        /// If the operation fails, it returns an error result.
        /// </returns>
        Task<Result<PagedResponse<IEnumerable<GetNetworkResponse>>>>
            GetNetworksAsync(NetworkFilter filter, CancellationToken token = default);

        /// <summary>
        /// Retrieves detailed information for a specific network by its identifier.
        /// </summary>
        /// <param name="networkId">
        /// The unique identifier of the network.
        /// </param>
        /// <param name="token">
        /// A cancellation token for the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see cref="Result{GetNetworkDetailResponse}"/> containing the network's details if successful;
        /// otherwise, an error result is returned.
        /// </returns>
        Task<Result<GetNetworkDetailResponse>>
            GetNetworkDetailAsync(Guid networkId, CancellationToken token);

        /// <summary>
        /// Creates a new network using the provided request data.
        /// </summary>
        /// <param name="request">
        /// The data required to create a new network.
        /// </param>
        /// <param name="token">
        /// A cancellation token for the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see cref="Result{CreateNetworkResponse}"/> containing the result of the creation process.
        /// If the creation is successful, it returns the new network's identifier; otherwise, it returns an error result.
        /// </returns>
        Task<Result<CreateNetworkResponse>>
            CreateNetworkAsync(CreateNetworkRequest request, CancellationToken token);

        /// <summary>
        /// Updates an existing network identified by the provided network identifier.
        /// </summary>
        /// <param name="networkId">
        /// The unique identifier of the network to update.
        /// </param>
        /// <param name="request">
        /// The updated data for the network.
        /// </param>
        /// <param name="token">
        /// A cancellation token for the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see cref="Result{UpdateNetworkResponse}"/> containing the result of the update process.
        /// If the update is successful, it returns the network's identifier; otherwise, it returns an error result.
        /// </returns>
        Task<Result<UpdateNetworkResponse>>
            UpdateNetworkAsync(Guid networkId, UpdateNetworkRequest request, CancellationToken token = default);

        /// <summary>
        /// Deletes a network identified by the provided network identifier.
        /// </summary>
        /// <param name="networkId">
        /// The unique identifier of the network to delete.
        /// </param>
        /// <param name="token">
        /// A cancellation token for the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see cref="Result{DeleteNetworkResponse}"/> containing the result of the delete process.
        /// If the deletion is successful, it returns the network's identifier; otherwise, it returns an error result.
        /// </returns>
        Task<Result<DeleteNetworkResponse>>
            DeleteNetworkAsync(Guid networkId, CancellationToken token = default);
    }
}