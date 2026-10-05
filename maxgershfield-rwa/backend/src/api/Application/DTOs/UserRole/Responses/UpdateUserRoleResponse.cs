namespace Application.DTOs.UserRole.Responses;

/// <summary>
/// Response object returned after updating a user role association.
/// </summary>
/// <param name="Id">
/// The unique identifier of the updated user role association.
/// </param>
public sealed record UpdateUserRoleResponse(Guid Id);