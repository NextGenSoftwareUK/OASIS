namespace Application.DTOs.User.Responses;

/// <summary>
/// Represents a simplified view of user data used in user listings.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="FirstName">The user's first name (optional).</param>
/// <param name="LastName">The user's last name (optional).</param>
/// <param name="Email">The user's email address.</param>
/// <param name="PhoneNumber">The user's phone number.</param>
/// <param name="UserName">The username associated with the account.</param>
/// <param name="Dob">Date of birth of the user (optional).</param>
public sealed record GetAllUserResponse(
    Guid Id,
    string? FirstName,
    string? LastName,
    string Email,
    string PhoneNumber,
    string UserName,
    DateTimeOffset? Dob
);