namespace Application.DTOs.Role.Responses;

/// <summary>
/// Response DTO returned after successfully updating a role.
/// </summary>
/// <param name="Id">The unique identifier of the role that was updated.</param>
public sealed record UpdateRoleResponse(Guid Id);