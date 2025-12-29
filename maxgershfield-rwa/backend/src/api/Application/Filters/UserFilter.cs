namespace Application.Filters;

/// <summary>
/// Filter parameters used for querying users based on profile information.
/// </summary>
/// <param name="FirstName">
/// Optional filter by user's first name (case-insensitive, partial match supported).
/// </param>
/// <param name="LastName">
/// Optional filter by user's last name (case-insensitive, partial match supported).
/// </param>
/// <param name="Email">
/// Optional filter by user's email address (case-insensitive, partial match supported).
/// </param>
/// <param name="PhoneNumber">
/// Optional filter by user's phone number (exact match).
/// </param>
/// <param name="UserName">
/// Optional filter by username (case-insensitive, partial match supported).
/// </param>
public sealed record UserFilter(
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? UserName) : BaseFilter;