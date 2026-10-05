namespace Application.Filters;

/// <summary>
/// Filter parameters used for querying user roles based on user details and role properties.
/// </summary>
/// <param name="FirstName">
/// Optional filter by the user's first name (case-insensitive, partial match supported).
/// </param>
/// <param name="LastName">
/// Optional filter by the user's last name (case-insensitive, partial match supported).
/// </param>
/// <param name="Email">
/// Optional filter by the user's email address (case-insensitive, partial match supported).
/// </param>
/// <param name="PhoneNumber">
/// Optional filter by the user's phone number (case-insensitive, partial match supported).
/// </param>
/// <param name="UserName">
/// Optional filter by the user's username (case-insensitive, partial match supported).
/// </param>
/// <param name="RoleName">
/// Optional filter by the role's name (case-insensitive, partial match supported).
/// </param>
/// <param name="RoleKeyword">
/// Optional filter by the role's internal keyword or identifier (case-insensitive, partial match supported).
/// </param>
/// <param name="RoleDescription">
/// Optional filter by the role's description (partial match supported).
/// </param>
public sealed record UserRoleFilter(
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? UserName,
    string? RoleName,
    string? RoleKeyword,
    string? RoleDescription) : BaseFilter;