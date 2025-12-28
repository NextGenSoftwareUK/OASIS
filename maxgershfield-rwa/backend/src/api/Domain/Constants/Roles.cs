namespace Domain.Constants;

/// <summary>
/// Contains constant values representing the roles within the application.
/// These roles are typically used to define user permissions and access levels.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Represents the "User" role, typically assigned to regular users with standard access to the application.
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Represents the "Admin" role, typically assigned to users with administrative access and management privileges.
    /// </summary>
    public const string Admin = "Admin";
}