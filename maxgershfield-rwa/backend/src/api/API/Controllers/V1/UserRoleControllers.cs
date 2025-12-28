namespace API.Controllers.V1;

/// <summary>
/// Controller for managing user roles within the system.
/// Provides endpoints for retrieving, creating, updating, and deleting user roles.
/// </summary>
[Route($"{ApiAddresses.Base}/user-roles")]
public sealed class UserRoleController(IUserRoleService userRoleService) : V1BaseController
{
    /// <summary>
    /// Retrieves a list of user roles based on the provided filter parameters.
    /// </summary>
    /// <param name="filter">Filter parameters for querying user roles.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>A list of user roles matching the filter criteria.</returns>
    [HttpGet]
    public async Task<IActionResult> GetUserRolesAsync([FromQuery] UserRoleFilter filter,
        CancellationToken cancellationToken)
        => (await userRoleService.GetUserRolesAsync(filter, cancellationToken)).ToActionResult();

    /// <summary>
    /// Retrieves the details of a specific user role by its unique ID.
    /// </summary>
    /// <param name="id">The unique identifier of the user role to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>The details of the specified user role.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserRoleDetailAsync(Guid id, CancellationToken cancellationToken)
        => (await userRoleService.GetUserRoleDetailAsync(id, cancellationToken)).ToActionResult();

    /// <summary>
    /// Creates a new user role based on the provided request data.
    /// Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="request">The request containing the details of the user role to create.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>A response indicating the success or failure of the role creation.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateUserRoleAsync([FromBody] CreateUserRoleRequest request,
        CancellationToken cancellationToken)
        => (await userRoleService.CreateUserRoleAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Updates an existing user role by its unique ID.
    /// Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="id">The unique identifier of the user role to update.</param>
    /// <param name="request">The request containing the updated user role details.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>A response indicating the success or failure of the role update.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateUserRoleAsync(Guid id, [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
        => (await userRoleService.UpdateUserRoleAsync(id, request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Deletes a user role by its unique ID.
    /// Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="id">The unique identifier of the user role to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>A response indicating the success or failure of the role deletion.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteUserRoleAsync(Guid id, CancellationToken cancellationToken)
        => (await userRoleService.DeleteUserRoleAsync(id, cancellationToken)).ToActionResult();
}