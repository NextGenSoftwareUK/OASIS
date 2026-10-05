namespace Application.DTOs.Role.Responses;

/// <summary>
/// Response DTO returned after successfully creating a role.
/// </summary>
/// <param name="Id">The unique identifier of the newly created role.</param>
public sealed record CreateRoleResponse(Guid Id);