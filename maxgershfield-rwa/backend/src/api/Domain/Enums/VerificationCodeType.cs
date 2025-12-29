namespace Domain.Enums;

/// <summary>
/// Defines the different types of verification codes used in the system.
/// </summary>
public enum VerificationCodeType
{
    /// <summary>
    /// The verification code used for resetting a user's password.
    /// </summary>
    PasswordReset,

    /// <summary>
    /// The verification code used for confirming the user's email address.
    /// </summary>
    EmailConfirmation,

    /// <summary>
    /// The verification code used for two-factor authentication (2FA).
    /// </summary>
    TwoFactorAuth,

    /// <summary>
    /// A default or no verification code type, used when no specific code type is needed.
    /// </summary>
    None,

    /// <summary>
    /// The verification code used for restoring a user's account.
    /// </summary>
    AccountRestore
}