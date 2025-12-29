namespace Domain.Entities;

/// <summary>
/// Represents the association between a user and a role.
/// This entity is used to manage the roles assigned to a user, allowing for role-based access control.
/// </summary>
public sealed class UserRole : BaseEntity
{
    /// <summary>
    /// The ID of the user to whom the role is assigned.
    /// This creates a relationship between the user and the role.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user to whom the role is assigned.
    /// This represents the full user entity and provides a navigation property to the associated user.
    /// </summary>
    public User User { get; set; } = default!;

    /// <summary>
    /// The ID of the role assigned to the user.
    /// This creates a relationship between the user and the role.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The role assigned to the user.
    /// This represents the full role entity and provides a navigation property to the associated role.
    /// </summary>
    public Role Role { get; set; } = default!;
}