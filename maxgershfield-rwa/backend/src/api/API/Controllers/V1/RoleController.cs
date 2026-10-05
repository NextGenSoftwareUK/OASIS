namespace API.Controllers.V1;

/// <summary>
/// Controller for managing user roles within the system.
/// Provides endpoints for retrieving, creating, updating, and deleting roles.
/// Only accessible by users with the Admin role.
/// </summary>
[Route($"{ApiAddresses.Base}/roles")]
[Authorize(Roles = Roles.Admin)]
public sealed class RoleController(IRoleService roleService) : V1BaseController
{
    /// <summary>
    /// Retrieves a list of roles based on the provided filter parameters.
    /// </summary>
    /// <param name="filter">Filter parameters for querying roles.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>A list of roles matching the filter criteria.</returns>
    [HttpGet]
    public async Task<IActionResult> GetRolesAsync([FromQuery] RoleFilter filter, CancellationToken cancellationToken)
        => (await roleService.GetRolesAsync(filter, cancellationToken)).ToActionResult();

    /// <summary>
    /// Retrieves details of a specific role identified by its unique ID.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>The details of the specified role.</returns>
    [HttpGet("{roleId:guid}")]
    public async Task<IActionResult> GetRoleDetailAsync(Guid roleId, CancellationToken cancellationToken)
        => (await roleService.GetRoleDetailAsync(roleId, cancellationToken)).ToActionResult();

    /// <summary>
    /// Creates a new role with the provided request details.
    /// </summary>
    /// <param name="request">The request containing the details for creating the new role.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>A response indicating the success or failure of the role creation.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateRoleAsync([FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
        => (await roleService.CreateRoleAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Updates an existing role identified by its unique ID with the provided request details.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to update.</param>
    /// <param name="request">The request containing the updated role details.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>A response indicating the success or failure of the role update.</returns>
    [HttpPut("{roleId:guid}")]
    public async Task<IActionResult> UpdateRoleAsync(Guid roleId, [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
        => (await roleService.UpdateRoleAsync(roleId, request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Deletes an existing role identified by its unique ID.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>A response indicating the success or failure of the role deletion.</returns>
    [HttpDelete("{roleId:guid}")]
    public async Task<IActionResult> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken)
        => (await roleService.DeleteRoleAsync(roleId, cancellationToken)).ToActionResult();
}