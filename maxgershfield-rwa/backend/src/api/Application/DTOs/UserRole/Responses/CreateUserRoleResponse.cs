namespace Application.DTOs.UserRole.Responses;

/// <summary>
/// Response object returned after creating a user role.
/// </summary>
/// <param name="Id">
/// The unique identifier of the newly created user role.
/// </param>
public sealed record CreateUserRoleResponse(Guid Id);