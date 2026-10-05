namespace Application.DTOs.User.Responses;

/// <summary>
/// Represents detailed user profile information for internal/private usage.
/// </summary>
/// <param name="Id">
/// Unique identifier of the user.
/// </param>
/// <param name="FirstName">
/// Optional first name of the user.
/// </param>
/// <param name="LastName">
/// Optional last name of the user.
/// </param>
/// <param name="Email">
/// Registered email address of the user.
/// </param>
/// <param name="PhoneNumber">
/// Registered phone number of the user.
/// </param>
/// <param name="UserName">
/// Unique username used for authentication or display.
/// </param>
/// <param name="Dob">
/// Optional date of birth of the user.
/// </param>
/// <param name="LastLoginAt">
/// Timestamp of the user's last login (if available).
/// </param>
/// <param name="TotalLogins">
/// Total number of successful logins the user has performed.
/// </param>
public sealed record GetUserDetailPrivateResponse(
    Guid Id,
    string? FirstName,
    string? LastName,
    string Email,
    string PhoneNumber,
    string UserName,
    DateTimeOffset? Dob,
    DateTimeOffset? LastLoginAt,
    long TotalLogins
);