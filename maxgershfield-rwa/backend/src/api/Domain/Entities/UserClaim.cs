namespace Domain.Entities;

/// <summary>
/// Represents a claim associated with a user.
/// Claims are used to store specific permissions or attributes granted to the user.
/// </summary>
public sealed class UserClaim : BaseEntity
{
    /// <summary>
    /// The type of the claim.
    /// This could represent a specific permission or attribute granted to the user (e.g., "Admin", "Premium").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The value associated with the claim.
    /// This represents the specific value of the claim (e.g., "True" for an active permission).
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user to whom the claim belongs.
    /// This creates a relationship between the claim and the user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user to whom the claim belongs.
    /// This represents the full user entity and provides a navigation property to the associated user.
    /// </summary>
    public User User { get; set; } = default!;
}