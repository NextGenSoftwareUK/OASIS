namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service for managing user roles, providing CRUD operations and role assignments.
/// </summary>
public sealed class UserRoleService(
    DataContext dbContext,
    IHttpContextAccessor accessor,
    ILogger<UserRoleService> logger) : IUserRoleService
{
    /// <summary>
    /// Retrieves a paginated list of user roles based on the provided filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria for retrieving user roles.</param>
    /// <param name="token">A cancellation token for the asynchronous operation.</param>
    /// <returns>A paginated list of user roles.</returns>

    public async Task<Result<PagedResponse<IEnumerable<GetUserRolesResponse>>>> GetUserRolesAsync(UserRoleFilter filter,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetUserRolesAsync), date);

        IQueryable<GetUserRolesResponse> userRolesQuery = dbContext.UserRoles
            .Include(x => x.User)
            .Include(x => x.Role).AsNoTracking().AsQueryable()
            .ApplyFilter(filter.RoleName, x => x.Role.Name)
            .ApplyFilter(filter.RoleKeyword, x => x.Role.RoleKey)
            .ApplyFilter(filter.RoleDescription, x => x.Role.Description)
            .ApplyFilter(filter.FirstName, x => x.User.FirstName)
            .ApplyFilter(filter.LastName, x => x.User.LastName)
            .ApplyFilter(filter.UserName, x => x.User.UserName)
            .ApplyFilter(filter.PhoneNumber, x => x.User.PhoneNumber)
            .ApplyFilter(filter.Email, x => x.User.Email)
            .OrderBy(x => x.Id)
            .Select(x => x.ToRead());

        int totalCount = await userRolesQuery.CountAsync(token);

        PagedResponse<IEnumerable<GetUserRolesResponse>> result =
            PagedResponse<IEnumerable<GetUserRolesResponse>>.Create(
                filter.PageSize,
                filter.PageNumber,
                totalCount,
                userRolesQuery.Page(filter.PageNumber, filter.PageSize));

        logger.OperationCompleted(nameof(GetUserRolesAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return Result<PagedResponse<IEnumerable<GetUserRolesResponse>>>.Success(result);
    }

    /// <summary>
    /// Retrieves detailed information about a specific user role by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the user role.</param>
    /// <param name="token">A cancellation token for the asynchronous operation.</param>
    /// <returns>The detailed information of the user role.</returns>

    public async Task<Result<GetUserRoleDetailResponse>> GetUserRoleDetailAsync(Guid id,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetUserRoleDetailAsync), date);

        GetUserRoleDetailResponse? userRole = await dbContext.UserRoles
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Role)
            .Where(x => x.Id == id)
            .Select(x => x.ToReadDetail())
            .FirstOrDefaultAsync(token);

        logger.OperationCompleted(nameof(GetUserRoleDetailAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - date);
        return userRole is not null
            ? Result<GetUserRoleDetailResponse>.Success(userRole)
            : Result<GetUserRoleDetailResponse>.Failure(ResultPatternError.NotFound(Messages.UserRoleNotFound));
    }

    /// <summary>
    /// Creates a new user role assignment for a user.
    /// </summary>
    /// <param name="request">The request data containing the user and role to be assigned.</param>
    /// <param name="token">A cancellation token for the asynchronous operation.</param>
    /// <returns>A response containing the ID of the newly created user role assignment.</returns>
    public async Task<Result<CreateUserRoleResponse>> CreateUserRoleAsync(CreateUserRoleRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CreateUserRoleAsync), date);

        if (!await dbContext.Roles.AnyAsync(x => x.Id == request.RoleId, token))
        {
            logger.OperationCompleted(nameof(CreateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<CreateUserRoleResponse>.Failure(ResultPatternError.NotFound(Messages.RoleNotFound));
        }

        if (!await dbContext.Users.AnyAsync(x => x.Id == request.UserId, token))
        {
            logger.OperationCompleted(nameof(CreateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<CreateUserRoleResponse>.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        if (await dbContext.UserRoles.AnyAsync(x => x.UserId == request.UserId && x.RoleId == request.RoleId, token))
        {
            logger.OperationCompleted(nameof(CreateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<CreateUserRoleResponse>.Failure(ResultPatternError.Conflict(Messages.UserRoleAlreadyExist));
        }

        try
        {
            UserRole newUserRole = request.ToEntity(accessor);
            await dbContext.UserRoles.AddAsync(newUserRole, token);

            logger.OperationCompleted(nameof(CreateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<CreateUserRoleResponse>.Success(new CreateUserRoleResponse(newUserRole.Id))
                : Result<CreateUserRoleResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.CreateUserRoleFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(CreateUserRoleAsync), ex.Message);
            logger.OperationCompleted(nameof(CreateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<CreateUserRoleResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.CreateUserRoleFailed));
        }
    }

    /// <summary>
    /// Updates an existing user role assignment.
    /// </summary>
    /// <param name="id">The unique identifier of the user role to be updated.</param>
    /// <param name="request">The request data containing the new user and role data.</param>
    /// <param name="token">A cancellation token for the asynchronous operation.</param>
    /// <returns>A response containing the ID of the updated user role assignment.</returns>
    public async Task<Result<UpdateUserRoleResponse>> UpdateUserRoleAsync(Guid id, UpdateUserRoleRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(UpdateUserRoleAsync), date);

        bool roleExists = await dbContext.Roles.AnyAsync(x => x.Id == request.RoleId, token);
        if (!roleExists)
        {
            logger.OperationCompleted(nameof(UpdateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateUserRoleResponse>.Failure(ResultPatternError.NotFound(Messages.RoleNotFound));
        }

        bool userExists = await dbContext.Users.AnyAsync(x => x.Id == request.UserId, token);
        if (!userExists)
        {
            logger.OperationCompleted(nameof(UpdateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateUserRoleResponse>.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        UserRole? userRole = await dbContext.UserRoles.FirstOrDefaultAsync(x => x.Id == id, token);
        if (userRole is null)
        {
            logger.OperationCompleted(nameof(UpdateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateUserRoleResponse>.Failure(ResultPatternError.NotFound(Messages.UserRoleNotFound));
        }

        if (userRole.UserId == request.UserId && userRole.RoleId == request.RoleId)
        {
            logger.OperationCompleted(nameof(UpdateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateUserRoleResponse>.Success(new(id));
        }

        try
        {
            userRole.ToEntity(accessor, request);
            logger.OperationCompleted(nameof(UpdateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<UpdateUserRoleResponse>.Success(new(userRole.Id))
                : Result<UpdateUserRoleResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.UpdateUserRoleFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(UpdateUserRoleAsync), ex.Message);
            logger.OperationCompleted(nameof(UpdateUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateUserRoleResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.UpdateUserRoleFailed));
        }
    }

    /// <summary>
    /// Deletes a user role assignment by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user role to be deleted.</param>
    /// <param name="token">A cancellation token for the asynchronous operation.</param>
    /// <returns>A result indicating the success or failure of the operation.</returns>
    public async Task<BaseResult> DeleteUserRoleAsync(Guid id, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(DeleteUserRoleAsync), date);

        UserRole? userRole = await dbContext.UserRoles.FirstOrDefaultAsync(x => x.Id == id, token);
        if (userRole is null)
        {
            logger.OperationCompleted(nameof(DeleteUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.UserRoleNotFound));
        }

        try
        {
            userRole.ToEntity(accessor);
            logger.OperationCompleted(nameof(DeleteUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync(token) != 0
                ? BaseResult.Success()
                : BaseResult.Failure(ResultPatternError.InternalServerError(Messages.DeleteUserRoleFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(DeleteUserRoleAsync), ex.Message);
            logger.OperationCompleted(nameof(DeleteUserRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateUserRoleResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.DeleteUserRoleFailed));
        }
    }
}