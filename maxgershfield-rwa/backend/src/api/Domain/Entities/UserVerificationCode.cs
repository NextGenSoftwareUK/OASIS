namespace Domain.Entities;

/// <summary>
/// Represents a verification code associated with a user for actions like email/phone verification, password reset, etc.
/// This entity stores information about the code, its validity period, and usage status.
/// </summary>
public sealed class UserVerificationCode : BaseEntity
{
    /// <summary>
    /// The ID of the user associated with the verification code.
    /// This establishes a relationship between the code and the user for whom it was generated.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user associated with the verification code.
    /// This represents the full user entity and provides a navigation property to the associated user.
    /// </summary>
    public User User { get; set; } = default!;

    /// <summary>
    /// The verification code itself.
    /// This code is usually a numeric or alphanumeric value sent to the user for verification purposes.
    /// </summary>
    public long Code { get; set; }

    /// <summary>
    /// The time when the verification code was generated.
    /// This helps determine the validity period and expiration time of the code.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// The expiration time of the verification code.
    /// The code becomes invalid after this time.
    /// </summary>
    public DateTimeOffset ExpiryTime { get; set; }

    /// <summary>
    /// The type of verification code (e.g., email verification, password reset).
    /// This distinguishes between different purposes for which the code is used.
    /// </summary>
    public VerificationCodeType Type { get; set; }

    /// <summary>
    /// Indicates whether the verification code has been used.
    /// Once the code is used, it can no longer be used again.
    /// </summary>
    public bool IsUsed { get; set; }
}