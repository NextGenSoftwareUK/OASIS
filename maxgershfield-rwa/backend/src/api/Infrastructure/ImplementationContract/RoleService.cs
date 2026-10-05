using Role = Domain.Entities.Role;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Provides operations for managing roles, including retrieval, creation, updating, and deletion.
/// Implements business logic and interacts with the data layer for role-related functionality.
/// </summary>
public sealed class RoleService(
    DataContext dbContext,
    IHttpContextAccessor accessor,
    ILogger<RoleService> logger) : IRoleService
{
    /// <summary>
    /// Retrieves a paginated list of roles based on the provided filtering options.
    /// </summary>
    /// <param name="filter">Filtering options such as name, keyword, or description.</param>
    /// <param name="token">Optional cancellation token for async operation.</param>
    /// <returns>A result containing a paginated list of roles that match the filter.</returns>
    public async Task<Result<PagedResponse<IEnumerable<GetRolesResponse>>>> GetRolesAsync(RoleFilter filter,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetRolesAsync), date);

        IQueryable<GetRolesResponse> rolesQuery = dbContext.Roles.AsNoTracking()
            .ApplyFilter(filter.Name, x => x.Name)
            .ApplyFilter(filter.Keyword, x => x.RoleKey)
            .ApplyFilter(filter.Description, x => x.Description)
            .OrderBy(x => x.Id)
            .Select(x => x.ToRead());

        int totalCount = await rolesQuery.CountAsync(token);

        PagedResponse<IEnumerable<GetRolesResponse>> result = PagedResponse<IEnumerable<GetRolesResponse>>.Create(
            filter.PageSize,
            filter.PageNumber,
            totalCount,
            rolesQuery.Page(filter.PageNumber, filter.PageSize));

        logger.OperationCompleted(nameof(GetRolesAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return Result<PagedResponse<IEnumerable<GetRolesResponse>>>.Success(result);
    }

    /// <summary>
    /// Retrieves detailed information for a specific role by its unique identifier.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to retrieve.</param>
    /// <param name="token">Optional cancellation token for async operation.</param>
    /// <returns>A result containing role detail if found; otherwise, a failure result.</returns>
    public async Task<Result<GetRoleDetailResponse>> GetRoleDetailAsync(Guid roleId, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetRoleDetailAsync), date);

        GetRoleDetailResponse? role = await dbContext.Roles.AsNoTracking()
            .Where(x => x.Id == roleId)
            .Select(x => x.ToReadDetail())
            .FirstOrDefaultAsync(token);

        logger.OperationCompleted(nameof(GetRoleDetailAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return role is not null
            ? Result<GetRoleDetailResponse>.Success(role)
            : Result<GetRoleDetailResponse>.Failure(ResultPatternError.NotFound(Messages.RoleNotFound));
    }

    /// <summary>
    /// Creates a new role in the system based on the provided request data.
    /// Ensures that the role name and key are unique.
    /// </summary>
    /// <param name="request">The request containing role creation data.</param>
    /// <param name="token">Optional cancellation token for async operation.</param>
    /// <returns>A result containing the ID of the created role, or a failure result if creation fails.</returns>
    public async Task<Result<CreateRoleResponse>> CreateRoleAsync(CreateRoleRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CreateRoleAsync), date);

        bool roleExists = await dbContext.Roles
            .AnyAsync(x => x.Name == request.RoleName || x.RoleKey == request.RoleKey, token);

        if (roleExists)
        {
            logger.OperationCompleted(nameof(CreateRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<CreateRoleResponse>.Failure(ResultPatternError.Conflict(Messages.RoleAlreadyExist));
        }

        Role newRole = request.ToEntity(accessor);

        try
        {
            await dbContext.Roles.AddAsync(newRole, token);

            logger.OperationCompleted(nameof(CreateRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<CreateRoleResponse>.Success(new CreateRoleResponse(newRole.Id))
                : Result<CreateRoleResponse>.Failure(ResultPatternError.InternalServerError(Messages.CreateRoleFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(CreateRoleAsync), ex.Message);
            logger.OperationCompleted(nameof(CreateRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<CreateRoleResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.CreateRoleFailed));
        }
    }

    /// <summary>
    /// Updates an existing role's details.
    /// Validates for unique name and key, and updates metadata based on the current user.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to update.</param>
    /// <param name="request">The update request containing new values.</param>
    /// <param name="token">Optional cancellation token for async operation.</param>
    /// <returns>A result indicating success or failure of the update operation.</returns>
    public async Task<Result<UpdateRoleResponse>> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(UpdateRoleAsync), date);

        Role? role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == roleId, token);

        if (role is null)
        {
            logger.OperationCompleted(nameof(UpdateRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateRoleResponse>.Failure(ResultPatternError.NotFound(Messages.RoleNotFound));
        }

        if (!string.IsNullOrEmpty(request.RoleName) && request.RoleName != role.Name)
        {
            bool roleNameExists = await dbContext.Roles
                .AnyAsync(x => x.Name == request.RoleName && x.Id != roleId, token);
            if (roleNameExists)
            {
                logger.OperationCompleted(nameof(UpdateRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return Result<UpdateRoleResponse>.Failure(ResultPatternError.Conflict(Messages.RoleAlreadyExist));
            }
        }

        if (!string.IsNullOrEmpty(request.RoleKey) && request.RoleKey != role.RoleKey)
        {
            bool roleKeyExists = await dbContext.Roles
                .AnyAsync(x => x.RoleKey == request.RoleKey && x.Id != roleId, token);
            if (roleKeyExists)
            {
                logger.OperationCompleted(nameof(UpdateRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return Result<UpdateRoleResponse>.Failure(ResultPatternError.Conflict(Messages.RoleAlreadyExist));
            }
        }

        try
        {
            role.ToEntity(accessor, request);

            logger.OperationCompleted(nameof(UpdateRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<UpdateRoleResponse>.Success(new(roleId))
                : Result<UpdateRoleResponse>.Failure(ResultPatternError.InternalServerError(Messages.UpdateRoleFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(UpdateRoleAsync), ex.Message);
            logger.OperationCompleted(nameof(UpdateRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateRoleResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.UpdateRoleFailed));
        }
    }

    /// <summary>
    /// Deletes a role from the system by its unique identifier.
    /// Performs a logical delete if applicable and updates metadata.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to delete.</param>
    /// <param name="token">Optional cancellation token for async operation.</param>
    /// <returns>A result indicating success or failure of the deletion operation.</returns>
    public async Task<BaseResult> DeleteRoleAsync(Guid roleId, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(DeleteRoleAsync), date);

        Role? role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == roleId, token);

        if (role is null)
        {
            logger.OperationCompleted(nameof(DeleteRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.RoleNotFound));
        }

        try
        {
            role.ToEntity(accessor);

            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<UpdateRoleResponse>.Success(new(roleId))
                : Result<UpdateRoleResponse>.Failure(ResultPatternError.InternalServerError(Messages.DeleteRoleFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(DeleteRoleAsync), ex.Message);
            logger.OperationCompleted(nameof(DeleteRoleAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<UpdateRoleResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.DeleteRoleFailed));
        }
    }
}