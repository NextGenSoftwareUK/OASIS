namespace Domain.Enums;

/// <summary>
/// Defines the different types of tokens used in the system.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// The token used for refreshing access tokens.
    /// </summary>
    RefreshToken,

    /// <summary>
    /// The token used for accessing secured resources.
    /// </summary>
    AccessToken,

    /// <summary>
    /// The token used for confirming the user's email address.
    /// </summary>
    EmailConfirmation,

    /// <summary>
    /// The token used for resetting the user's password.
    /// </summary>
    PasswordReset
}