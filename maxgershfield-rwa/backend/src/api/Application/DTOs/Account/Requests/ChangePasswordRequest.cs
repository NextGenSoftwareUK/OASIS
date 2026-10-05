namespace Application.DTOs.Account.Requests;

/// <summary>
/// Represents a request to change the user's password.
/// Contains the user's current password, the new password, and a confirmation of the new password.
/// </summary>
public sealed record ChangePasswordRequest
{
    /// <summary>
    /// The current password of the user. This field is required for authentication.
    /// The password must be at least 8 characters long, but no longer than 128 characters.
    /// </summary>
    [Required, MinLength(8), MaxLength(128)]
    public string OldPassword { get; init; } = string.Empty;

    /// <summary>
    /// The new password the user wants to set. This field must be at least 8 characters long, but no longer than 128 characters.
    /// </summary>
    [Required, MinLength(8), MaxLength(128)]
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    /// Confirms the new password to ensure that the entered password and confirmation match.
    /// This must be equal to the <see cref="NewPassword"/> field.
    /// </summary>
    [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}