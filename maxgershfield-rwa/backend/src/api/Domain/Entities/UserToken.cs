namespace Domain.Entities;

/// <summary>
/// Represents a token associated with a user for authentication or authorization.
/// This entity stores information about access tokens, their expiration, and usage status.
/// </summary>
public sealed class UserToken : BaseEntity
{
    /// <summary>
    /// The ID of the user associated with the token.
    /// This creates a relationship between the token and the user it belongs to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user associated with the token.
    /// This represents the full user entity and provides a navigation property to the associated user.
    /// </summary>
    public User User { get; set; } = default!;

    /// <summary>
    /// The actual token value.
    /// This is typically used for authentication (e.g., JWT) or session management.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The type of token (e.g., Access Token, Refresh Token).
    /// This helps distinguish between different token types used for different purposes.
    /// </summary>
    public TokenType TokenType { get; set; } = TokenType.AccessToken;

    /// <summary>
    /// The expiration time of the token.
    /// This indicates when the token will no longer be valid.
    /// </summary>
    public DateTimeOffset Expiration { get; set; }

    /// <summary>
    /// Indicates whether the token has been revoked.
    /// A revoked token is no longer valid for authentication.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Indicates whether the token has been used.
    /// This is useful for preventing the reuse of tokens, such as with one-time use tokens.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// The IP address from which the token was issued or used.
    /// This helps track where the token was generated or used for security reasons.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// The user agent string from the device that issued or used the token.
    /// This provides information about the browser or client used by the user for security purposes.
    /// </summary>
    public string? UserAgent { get; set; }
}
