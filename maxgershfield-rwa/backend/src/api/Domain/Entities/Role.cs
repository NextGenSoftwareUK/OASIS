namespace Domain.Entities;

/// <summary>
/// Represents a role in the system, which defines a set of permissions and responsibilities for users.
/// This entity stores information about the role's name, key, description, and associated user roles and claims.
/// </summary>
public sealed class Role : BaseEntity
{
    /// <summary>
    /// The name of the role (e.g., "Admin", "User").
    /// This is used to identify the role within the system.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The unique key of the role (e.g., "admin_role", "user_role").
    /// This key is used for programmatic reference to the role in the system.
    /// </summary>
    public string RoleKey { get; set; } = string.Empty;

    /// <summary>
    /// An optional description of the role, providing additional context or details about its purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The collection of user roles associated with this role.
    /// This represents the users who have been assigned this role within the system.
    /// </summary>
    public HashSet<UserRole> UserRoles { get; } = new HashSet<UserRole>();

    /// <summary>
    /// The collection of role claims associated with this role.
    /// These claims define the permissions or responsibilities granted to users who have this role.
    /// </summary>
    public ICollection<RoleClaim> RoleClaims { get; } = new List<RoleClaim>();
}