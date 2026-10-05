namespace Application.DTOs.Account.Requests
{
    /// <summary>
    /// Represents a request to send an email confirmation code to the specified email address.
    /// This is typically used for email verification during registration or password reset processes.
    /// </summary>
    /// <param name="Email">
    /// The email address to which the confirmation code will be sent. It must be a valid email format.
    /// </param>
    public sealed record SendEmailConfirmationCodeRequest(
        [Required, EmailAddress] string Email);
}