namespace Application.DTOs.Account.Requests
{
    /// <summary>
    /// Represents a request for initiating the password recovery process.
    /// Typically used when a user forgets their password and needs to reset it.
    /// A verification email will be sent to the provided email address containing a reset password link.
    /// </summary>
    /// <param name="EmailAddress">
    /// The email address associated with the user's account.
    /// The email address must be in a valid email format.
    /// </param>
    public sealed record ForgotPasswordRequest(
        [Required, EmailAddress] string EmailAddress
    );
}