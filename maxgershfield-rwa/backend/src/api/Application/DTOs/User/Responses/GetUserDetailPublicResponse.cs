namespace Application.DTOs.User.Responses;

/// <summary>
/// Public view model for user details, used in contexts where sensitive data is not exposed.
/// </summary>
/// <param name="Id">
/// Unique identifier of the user.
/// </param>
/// <param name="FirstName">
/// User's first name (nullable).
/// </param>
/// <param name="LastName">
/// User's last name (nullable).
/// </param>
/// <param name="Email">
/// User's email address.
/// </param>
/// <param name="PhoneNumber">
/// User's phone number.
/// </param>
/// <param name="UserName">
/// User's username.
/// </param>
/// <param name="Dob">
/// User's date of birth (nullable).
/// </param>
public sealed record GetUserDetailPublicResponse(
    Guid Id,
    string? FirstName,
    string? LastName,
    string Email,
    string PhoneNumber,
    string UserName,
    DateTimeOffset? Dob
);