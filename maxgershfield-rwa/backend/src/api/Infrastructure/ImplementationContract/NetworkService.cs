namespace Infrastructure.ImplementationContract;

/// <summary>
/// Provides network-related operations including retrieval of network lists and details,
/// as well as creation, update, and deletion of network entries.
/// </summary>
public sealed class NetworkService(
    DataContext dbContext,
    IHttpContextAccessor accessor,
    ILogger<NetworkService> logger) : INetworkService
{
    /// <summary>
    /// Retrieves a paged list of networks matching the provided filter.
    /// The query applies filtering on network name and description,
    /// includes associated network tokens, and returns paged results.
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
    /// that contains the paged list of networks if successful;
    /// otherwise, it returns an error result.
    /// </returns>
    public async Task<Result<PagedResponse<IEnumerable<GetNetworkResponse>>>> GetNetworksAsync(NetworkFilter filter,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetNetworksAsync), date);

        IQueryable<GetNetworkResponse> query = dbContext.Networks
            .AsNoTracking()
            .ApplyFilter(filter.Name, x => x.Name)
            .ApplyFilter(filter.Description, x => x.Description)
            .Include(n => n.NetworkTokens)
            .OrderBy(x => x.Id)
            .Select(n => new GetNetworkResponse(
                n.Id,
                n.Name,
                n.Description,
                n.NetworkTokens.Select(nt => nt.Symbol).ToList()
            ));

        int totalCount = await query.CountAsync(token);

        PagedResponse<IEnumerable<GetNetworkResponse>> pagedResult =
            PagedResponse<IEnumerable<GetNetworkResponse>>.Create(
                filter.PageSize,
                filter.PageNumber,
                totalCount,
                query.Page(filter.PageNumber, filter.PageSize));

        logger.OperationCompleted(nameof(GetNetworksAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return Result<PagedResponse<IEnumerable<GetNetworkResponse>>>.Success(pagedResult);
    }

    /// <summary>
    /// Retrieves detailed information for a specific network identified by its unique identifier.
    /// The details include network tokens and associated descriptive data.
    /// </summary>
    /// <param name="networkId">
    /// The unique identifier of the network.
    /// </param>
    /// <param name="token">
    /// A cancellation token for the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="Result{GetNetworkDetailResponse}"/> containing the network details if found;
    /// otherwise, an error result indicating that the network was not found.
    /// </returns>
    public async Task<Result<GetNetworkDetailResponse>> GetNetworkDetailAsync(Guid networkId, CancellationToken token)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetNetworkDetailAsync), date);

        GetNetworkDetailResponse? network = await dbContext.Networks
            .AsNoTracking()
            .Where(x => x.Id == networkId)
            .Include(n => n.NetworkTokens)
            .Select(n => new GetNetworkDetailResponse(
                n.Id,
                n.Name,
                n.Description,
                n.NetworkTokens.Select(nt => nt.Symbol).ToList()
            ))
            .FirstOrDefaultAsync(token);

        logger.OperationCompleted(nameof(GetNetworkDetailAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - date);

        return network is not null
            ? Result<GetNetworkDetailResponse>.Success(network)
            : Result<GetNetworkDetailResponse>.Failure(ResultPatternError.NotFound(Messages.NetworkNotFound));
    }

    /// <summary>
    /// Creates a new network entry in the system.
    /// The process verifies that the network name is unique before mapping the request to the network entity.
    /// </summary>
    /// <param name="request">
    /// The creation request containing the necessary network data.
    /// </param>
    /// <param name="token">
    /// A cancellation token for the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="Result{CreateNetworkResponse}"/> containing the new network's identifier if successful;
    /// otherwise, an error result is returned.
    /// </returns>
    public async Task<Result<CreateNetworkResponse>> CreateNetworkAsync(CreateNetworkRequest request,
        CancellationToken token)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CreateNetworkAsync), date);

        bool networkExists = await dbContext.Networks.AnyAsync(x => x.Name == request.Name, token);
        if (networkExists)
        {
            logger.OperationCompleted(nameof(CreateNetworkAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<CreateNetworkResponse>.Failure(ResultPatternError.Conflict(Messages.NetworkAlreadyExist));
        }

        try
        {
            Network newNetwork = request.ToEntity(accessor);
            await dbContext.Networks.AddAsync(newNetwork, token);

            logger.OperationCompleted(nameof(CreateNetworkAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);

            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<CreateNetworkResponse>.Success(new(newNetwork.Id))
                : Result<CreateNetworkResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.CreateNetworkFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(CreateNetworkAsync), ex.Message);
            logger.OperationCompleted(nameof(CreateNetworkAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<CreateNetworkResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.CreateNetworkFailed));
        }
    }

    /// <summary>
    /// Updates an existing network entry identified by its unique identifier.
    /// The update includes changes to the network’s name and description while checking for uniqueness of the new name.
    /// </summary>
    /// <param name="networkId">
    /// The unique identifier of the network to be updated.
    /// </param>
    /// <param name="request">
    /// The update request containing the new data for the network.
    /// </param>
    /// <param name="token">
    /// A cancellation token for the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="Result{UpdateNetworkResponse}"/> indicating successful update with the network's identifier,
    /// or an error result if the update fails.
    /// </returns>
    public async Task<Result<UpdateNetworkResponse>> UpdateNetworkAsync(Guid networkId, UpdateNetworkRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(UpdateNetworkAsync), date);

        Network? network = await dbContext.Networks.FirstOrDefaultAsync(x => x.Id == networkId, token);
        if (network is null)
        {
            logger.OperationCompleted(nameof(UpdateNetworkAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateNetworkResponse>.Failure(ResultPatternError.NotFound(Messages.NetworkNotFound));
        }

        if (!string.IsNullOrEmpty(request.Name) && request.Name != network.Name)
        {
            bool nameExists =
                await dbContext.Networks.AnyAsync(x => x.Name == request.Name && x.Id != networkId, token);
            if (nameExists)
            {
                logger.OperationCompleted(nameof(UpdateNetworkAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<UpdateNetworkResponse>.Failure(ResultPatternError.Conflict(Messages.NetworkAlreadyExist));
            }
        }

        if (request.Description is not null)
            network.Description = request.Description;

        try
        {
            network.ToEntity(accessor, request);
            logger.OperationCompleted(nameof(UpdateNetworkAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<UpdateNetworkResponse>.Success(new(network.Id))
                : Result<UpdateNetworkResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.UpdateNetworkFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(UpdateNetworkAsync), ex.Message);
            logger.OperationCompleted(nameof(UpdateNetworkAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateNetworkResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.UpdateNetworkFailed));
        }
    }

    /// <summary>
    /// Deletes an existing network entry identified by its unique identifier.
    /// This operation removes the network entity from the database.
    /// </summary>
    /// <param name="networkId">
    /// The unique identifier of the network to be deleted.
    /// </param>
    /// <param name="token">
    /// A cancellation token for the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="Result{DeleteNetworkResponse}"/> indicating whether the delete operation was successful,
    /// along with the identifier of the deleted network; otherwise, an error result is returned.
    /// </returns>
    public async Task<Result<DeleteNetworkResponse>> DeleteNetworkAsync(Guid networkId,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(DeleteNetworkAsync), date);

        Network? network = await dbContext.Networks.FirstOrDefaultAsync(x => x.Id == networkId, token);
        if (network is null)
        {
            logger.OperationCompleted(nameof(DeleteNetworkAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<DeleteNetworkResponse>.Failure(ResultPatternError.NotFound(Messages.NetworkNotFound));
        }

        try
        {
            network.ToEntity(accessor);
            logger.OperationCompleted(nameof(DeleteNetworkAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<DeleteNetworkResponse>.Success(new(network.Id))
                : Result<DeleteNetworkResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.DeleteNetworkFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(DeleteNetworkAsync), ex.Message);
            logger.OperationCompleted(nameof(DeleteNetworkAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<DeleteNetworkResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.DeleteNetworkFailed));
        }
    }
}