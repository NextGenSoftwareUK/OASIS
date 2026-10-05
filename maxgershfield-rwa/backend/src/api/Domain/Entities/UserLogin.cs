namespace Domain.Entities;

/// <summary>
/// Represents a login event for a user.
/// This entity is used to track user login attempts, including information about the provider, IP address, and whether the login was successful.
/// </summary>
public sealed class UserLogin : BaseEntity
{
    /// <summary>
    /// The ID of the user who performed the login.
    /// This creates a relationship between the login event and the specific user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user who performed the login.
    /// This represents the full user entity and provides a navigation property to the associated user.
    /// </summary>
    public User User { get; set; } = default!;

    /// <summary>
    /// The login provider used for the authentication.
    /// This indicates whether the login was performed using local credentials, external OAuth, etc.
    /// </summary>
    public LoginProviderType LoginProvider { get; set; } = LoginProviderType.Local;

    /// <summary>
    /// The IP address from which the user attempted to log in.
    /// This is useful for security and auditing purposes.
    /// </summary>
    public string? IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// The user agent string from the device used during the login attempt.
    /// This provides information about the browser or client used by the user.
    /// </summary>
    public string? UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the login attempt was successful.
    /// This helps track failed login attempts for security monitoring.
    /// </summary>
    public bool Successful { get; set; }
}