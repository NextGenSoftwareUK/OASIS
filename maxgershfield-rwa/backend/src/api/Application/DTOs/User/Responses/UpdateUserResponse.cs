namespace Application.DTOs.User.Responses;

/// <summary>
/// Represents the response returned after successfully updating a user's profile.
/// </summary>
/// <param name="UserId">
/// The unique identifier of the user whose profile was updated.
/// </param>
public record UpdateUserResponse(Guid UserId);