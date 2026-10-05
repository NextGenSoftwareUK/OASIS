namespace Application.DTOs.Account.Requests
{
    /// <summary>
    /// Represents a request to confirm an email address using a confirmation code.
    /// This request typically occurs after an email confirmation code is sent to the user's email
    /// to verify the validity of the email address.
    /// </summary>
    /// <param name="Email">
    /// The email address to which the confirmation code was sent. It must be a valid email format.
    /// </param>
    /// <param name="Code">
    /// The confirmation code provided by the user to verify their email address.
    /// The code must be exactly 6 characters in length.
    /// </param>
    public sealed record ConfirmEmailCodeRequest(
        [Required, EmailAddress] string Email,
        [Required, MinLength(6), MaxLength(6)] string Code
    );
}