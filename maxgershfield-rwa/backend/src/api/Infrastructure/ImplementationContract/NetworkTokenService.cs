namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service responsible for handling CRUD operations related to Network Tokens.
/// Implements logging and validation logic, and interacts with the database context.
/// </summary>
public sealed class NetworkTokenService(
    DataContext dbContext,
    IHttpContextAccessor accessor,
    ILogger<NetworkTokenService> logger) : INetworkTokenService
{
    /// <summary>
    /// Retrieves a paginated list of network tokens based on the provided filter criteria.
    /// </summary>
    /// <param name="filter">Filter containing symbol, description, and pagination parameters.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Paginated result of network tokens.</returns>
    public async Task<Result<PagedResponse<IEnumerable<GetNetworkTokenResponse>>>> GetNetworkTokensAsync(
        NetworkTokenFilter filter, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetNetworkTokensAsync), date);

        IQueryable<GetNetworkTokenResponse> query = dbContext.NetworkTokens
            .AsNoTracking()
            .ApplyFilter(filter.Symbol, x => x.Symbol)
            .ApplyFilter(filter.Description, x => x.Description)
            .OrderBy(x => x.Id)
            .Select(x => x.ToRead());

        int totalCount = await query.CountAsync(token);

        PagedResponse<IEnumerable<GetNetworkTokenResponse>> pagedResult =
            PagedResponse<IEnumerable<GetNetworkTokenResponse>>.Create(
                filter.PageSize,
                filter.PageNumber,
                totalCount,
                query.Page(filter.PageNumber, filter.PageSize));

        logger.OperationCompleted(nameof(GetNetworkTokensAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return Result<PagedResponse<IEnumerable<GetNetworkTokenResponse>>>.Success(pagedResult);
    }

    /// <summary>
    /// Retrieves detailed information about a specific network token by its unique identifier.
    /// </summary>
    /// <param name="networkTokenId">Unique identifier of the network token.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Detailed information of the requested network token, if found.</returns>
    public async Task<Result<GetNetworkTokenDetailResponse>> GetNetworkTokenDetailAsync(Guid networkTokenId,
        CancellationToken token)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetNetworkTokenDetailAsync), date);

        GetNetworkTokenDetailResponse? networkToken = await dbContext.NetworkTokens
            .AsNoTracking()
            .Where(x => x.Id == networkTokenId)
            .Select(x => x.ToReadDetail())
            .FirstOrDefaultAsync(token);

        logger.OperationCompleted(nameof(GetNetworkTokenDetailAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - date);

        return networkToken is not null
            ? Result<GetNetworkTokenDetailResponse>.Success(networkToken)
            : Result<GetNetworkTokenDetailResponse>.Failure(ResultPatternError.NotFound(Messages.NetworkTokenNotFound));
    }

    /// <summary>
    /// Creates a new network token if one does not already exist with the same symbol and network.
    /// </summary>
    /// <param name="request">Request object containing token data.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The result of the creation operation, including the new token's ID if successful.</returns>
    public async Task<Result<CreateNetworkTokenResponse>> CreateNetworkTokenAsync(CreateNetworkTokenRequest request,
        CancellationToken token)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CreateNetworkTokenAsync), date);

        bool tokenExists = await dbContext.NetworkTokens
            .AnyAsync(x => x.Symbol == request.Symbol && x.NetworkId == request.NetworkId, token);
        if (tokenExists)
        {
            logger.OperationCompleted(nameof(CreateNetworkTokenAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);

            return Result<CreateNetworkTokenResponse>.Failure(
                ResultPatternError.Conflict(Messages.NetworkTokenAlreadyExist));
        }

        try
        {
            NetworkToken newNetworkToken = request.ToEntity(accessor);
            await dbContext.NetworkTokens.AddAsync(newNetworkToken, token);

            logger.OperationCompleted(nameof(CreateNetworkTokenAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);

            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<CreateNetworkTokenResponse>.Success(new(newNetworkToken.Id))
                : Result<CreateNetworkTokenResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.CreateNetworkTokenFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(CreateNetworkTokenAsync), ex.Message);
            logger.OperationCompleted(nameof(CreateNetworkTokenAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CreateNetworkTokenResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.CreateNetworkTokenFailed));
        }
    }

    /// <summary>
    /// Updates an existing network token by its identifier.
    /// Ensures that the symbol remains unique within the same network context.
    /// </summary>
    /// <param name="networkTokenId">Identifier of the token to update.</param>
    /// <param name="request">Update request payload.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The result of the update operation.</returns>
    public async Task<Result<UpdateNetworkTokenResponse>> UpdateNetworkTokenAsync(Guid networkTokenId,
        UpdateNetworkTokenRequest request, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(UpdateNetworkTokenAsync), date);

        NetworkToken? networkToken =
            await dbContext.NetworkTokens.FirstOrDefaultAsync(x => x.Id == networkTokenId, token);
        if (networkToken is null)
        {
            logger.OperationCompleted(nameof(UpdateNetworkTokenAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<UpdateNetworkTokenResponse>.Failure(
                ResultPatternError.NotFound(Messages.NetworkTokenNotFound));
        }

        if (!string.IsNullOrEmpty(request.Symbol) && request.Symbol != networkToken.Symbol)
        {
            bool symbolExists = await dbContext.NetworkTokens
                .AnyAsync(
                    x => x.Symbol == request.Symbol && x.NetworkId == networkToken.NetworkId && x.Id != networkTokenId,
                    token);
            if (symbolExists)
            {
                logger.OperationCompleted(nameof(UpdateNetworkTokenAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<UpdateNetworkTokenResponse>.Failure(
                    ResultPatternError.Conflict(Messages.NetworkTokenAlreadyExist));
            }
        }

        try
        {
            networkToken.ToEntity(accessor, request);

            logger.OperationCompleted(nameof(UpdateNetworkTokenAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<UpdateNetworkTokenResponse>.Success(new(networkTokenId))
                : Result<UpdateNetworkTokenResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.UpdateNetworkTokenFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(UpdateNetworkTokenAsync), ex.Message);
            logger.OperationCompleted(nameof(UpdateNetworkTokenAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<UpdateNetworkTokenResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.UpdateNetworkTokenFailed));
        }
    }

    /// <summary>
    /// Marks a network token as deleted.
    /// </summary>
    /// <param name="networkTokenId">Identifier of the token to delete.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The result of the deletion operation.</returns>
    public async Task<Result<DeleteNetworkTokenResponse>> DeleteNetworkTokenAsync(Guid networkTokenId,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(DeleteNetworkTokenAsync), date);

        NetworkToken? networkToken =
            await dbContext.NetworkTokens.FirstOrDefaultAsync(x => x.Id == networkTokenId, token);
        if (networkToken is null)
        {
            logger.OperationCompleted(nameof(DeleteNetworkTokenAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<DeleteNetworkTokenResponse>.Failure(
                ResultPatternError.NotFound(Messages.NetworkTokenNotFound));
        }

        try
        {
            networkToken.ToEntity(accessor);
            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<DeleteNetworkTokenResponse>.Success(new(networkTokenId))
                : Result<DeleteNetworkTokenResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.DeleteNetworkTokenFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(DeleteNetworkTokenAsync), ex.Message);
            logger.OperationCompleted(nameof(DeleteNetworkTokenAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<DeleteNetworkTokenResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.DeleteNetworkTokenFailed));
        }
    }
}