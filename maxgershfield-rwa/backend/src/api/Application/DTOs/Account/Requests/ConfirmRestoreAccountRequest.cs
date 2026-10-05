namespace Application.DTOs.Account.Requests
{
    /// <summary>
    /// Represents a request to confirm the restoration of a user account.
    /// This request requires the user's email and a verification code sent to the email for confirmation.
    /// </summary>
    /// <param name="Email">
    /// The email address associated with the user account that is being restored.
    /// It must be a valid email format to proceed with the restoration process.
    /// </param>
    /// <param name="Code">
    /// A 6-digit verification code sent to the user's email to confirm the restoration of their account.
    /// This code must exactly match the one sent during the restoration process.
    /// </param>
    public sealed record ConfirmRestoreAccountRequest(
        [Required, EmailAddress] string Email,
        [Required, MinLength(6), MaxLength(6)] string Code);
}