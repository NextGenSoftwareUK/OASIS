namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service for managing user-related operations, such as retrieving users, updating profiles,
/// and querying virtual accounts. Depends on application context, blockchain bridges, and logging.
/// </summary>
public sealed class UserService(
    DataContext dbContext,
    IHttpContextAccessor accessor,
    IRadixBridge radixBridge,
    ISolanaBridge solanaBridge,
    ILogger<UserService> logger) : IUserService
{
    private const string Sol = "SOL";

    /// <summary>
    /// Retrieves a paginated list of users filtered by the specified criteria.
    /// </summary>
    /// <param name="filter">Filtering criteria (e.g., email, phone, name, etc.).</param>
    /// <param name="token">Cancellation token for async operations.</param>
    /// <returns>Paged response with filtered users.</returns>
    public async Task<Result<PagedResponse<IEnumerable<GetAllUserResponse>>>> GetUsersAsync(UserFilter filter,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetUsersAsync), date);

        IQueryable<GetAllUserResponse> users = dbContext.Users.AsNoTracking()
            .ApplyFilter(filter.Email, x => x.Email)
            .ApplyFilter(filter.PhoneNumber, x => x.PhoneNumber)
            .ApplyFilter(filter.FirstName, x => x.FirstName)
            .ApplyFilter(filter.LastName, x => x.LastName)
            .ApplyFilter(filter.UserName, x => x.UserName)
            .OrderBy(x => x.Id)
            .Select(x => x.ToRead());

        int totalCount = await users.CountAsync(token);

        PagedResponse<IEnumerable<GetAllUserResponse>> result =
            PagedResponse<IEnumerable<GetAllUserResponse>>.Create(
                filter.PageSize,
                filter.PageNumber,
                totalCount,
                users.Page(filter.PageNumber, filter.PageSize));

        logger.OperationCompleted(nameof(GetUsersAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return Result<PagedResponse<IEnumerable<GetAllUserResponse>>>.Success(result);
    }

    /// <summary>
    /// Gets the public details of a user by ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Public user details or not found result.</returns>
    public async Task<Result<GetUserDetailPublicResponse>> GetByIdForUser(Guid userId,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetByIdForUser), date);

        GetUserDetailPublicResponse? response = await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.ToReadPublicDetail())
            .FirstOrDefaultAsync(token);

        logger.OperationCompleted(nameof(GetByIdForUser), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);

        return response is not null
            ? Result<GetUserDetailPublicResponse>.Success(response)
            : Result<GetUserDetailPublicResponse>.Failure(ResultPatternError.NotFound());
    }

    /// <summary>
    /// Retrieves virtual accounts of the currently authenticated user,
    /// along with network and token information and current balances.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>List of virtual account details or not found result.</returns>
    public async Task<Result<IEnumerable<GetVirtualAccountDetailResponse>>> GetVirtualAccountsAsync(
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetVirtualAccountsAsync), date);


        Guid userId = accessor.GetId();

        bool exists = await dbContext.Users.AnyAsync(x => x.Id == userId, token);
        if (!exists)
        {
            logger.OperationCompleted(nameof(GetVirtualAccountsAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<IEnumerable<GetVirtualAccountDetailResponse>>.Failure(
                ResultPatternError.NotFound(Messages.UserNotFound));
        }

        var accounts = await (from nt in dbContext.NetworkTokens
                              join n in dbContext.Networks on nt.NetworkId equals n.Id
                              join va in dbContext.VirtualAccounts on n.Id equals va.NetworkId
                              join u in dbContext.Users on va.UserId equals u.Id
                              where u.Id == userId
                              select new
                              {
                                  va.Address,
                                  Network = n.Name,
                                  Token = nt.Symbol,
                              }).ToListAsync(token);

        List<GetVirtualAccountDetailResponse> result = new();

        foreach (var account in accounts)
        {
            decimal accountBalance = account.Token == Sol
                ? (await solanaBridge.GetAccountBalanceAsync(account.Address, token)).Value
                : (await radixBridge.GetAccountBalanceAsync(account.Address, token)).Value;

            result.Add(new GetVirtualAccountDetailResponse(
                account.Address,
                account.Network,
                account.Token,
                accountBalance
            ));
        }

        logger.OperationCompleted(nameof(GetVirtualAccountsAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return Result<IEnumerable<GetVirtualAccountDetailResponse>>.Success(result);
    }

    /// <summary>
    /// Gets the private profile details of the currently authenticated user.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Private user detail or not found result.</returns>
    public async Task<Result<GetUserDetailPrivateResponse>> GetByIdForSelf(CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetByIdForSelf), date);

        Guid userId = accessor.GetId();


        GetUserDetailPrivateResponse? response = await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.ToReadPrivateDetail())
            .FirstOrDefaultAsync(token);

        logger.OperationCompleted(nameof(GetByIdForSelf), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return response is not null
            ? Result<GetUserDetailPrivateResponse>.Success(response)
            : Result<GetUserDetailPrivateResponse>.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
    }

    /// <summary>
    /// Updates the profile information of the currently authenticated user.
    /// Validates email, phone, and username uniqueness.
    /// </summary>
    /// <param name="request">Updated profile values.</param>
    /// <param name="token">Cancellation token.</param>
    public async Task<Result<UpdateUserResponse>> UpdateProfileAsync(UpdateUserProfileRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(UpdateProfileAsync), date);

        Guid userId = accessor.GetId();
        User? user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(UpdateProfileAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateUserResponse>.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            bool emailExists = await dbContext.Users
                .AnyAsync(x => x.Email == request.Email && x.Id != userId, token);
            if (emailExists)
            {
                logger.OperationCompleted(nameof(UpdateProfileAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<UpdateUserResponse>.Failure(ResultPatternError.Conflict(Messages.UserEmailAlreadyExist));
            }
        }

        if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
        {
            bool phoneExists = await dbContext.Users
                .AnyAsync(x => x.PhoneNumber == request.PhoneNumber && x.Id != userId, token);
            if (phoneExists)
            {
                logger.OperationCompleted(nameof(UpdateProfileAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<UpdateUserResponse>.Failure(ResultPatternError.Conflict(Messages.UserPhoneAlreadyExist));
            }
        }

        if (!string.IsNullOrEmpty(request.UserName) && request.UserName != user.UserName)
        {
            bool userNameExists = await dbContext.Users
                .AnyAsync(x => x.UserName == request.UserName && x.Id != userId, token);
            if (userNameExists)
            {
                logger.OperationCompleted(nameof(UpdateProfileAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<UpdateUserResponse>.Failure(
                    ResultPatternError.Conflict(Messages.UserUserNameAlreadyExist));
            }
        }

        try
        {
            dbContext.Users.Update(user.ToEntity(request, accessor));
            logger.OperationCompleted(nameof(UpdateProfileAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<UpdateUserResponse>.Success(new(userId))
                : Result<UpdateUserResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.UpdateUserProfileFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(UpdateProfileAsync), ex.Message);
            logger.OperationCompleted(nameof(UpdateProfileAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateUserResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.UpdateUserProfileFailed));
        }
    }
}