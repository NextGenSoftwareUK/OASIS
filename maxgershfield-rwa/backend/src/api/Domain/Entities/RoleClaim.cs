namespace Domain.Entities;

/// <summary>
/// Represents a claim associated with a role in the system.
/// A role claim defines a specific permission or property that is granted to the role.
/// This entity stores the type and value of the claim, as well as its associated role.
/// </summary>
public sealed class RoleClaim : BaseEntity
{
    /// <summary>
    /// The type of the claim (e.g., "Permission", "FeatureAccess").
    /// This indicates the category or nature of the claim.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The value of the claim (e.g., "Read", "Write", "Admin").
    /// This represents the specific permission or access level granted by the claim.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the associated role for this claim.
    /// This establishes a relationship with the <see cref="Role"/> entity.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The role to which this claim is associated.
    /// This property creates a navigation property to the <see cref="Role"/> entity.
    /// </summary>
    public Role Role { get; set; } = default!;
}