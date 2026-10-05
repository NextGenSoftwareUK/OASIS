namespace Application.DTOs.Account.Requests
{
    /// <summary>
    /// Represents a request to restore a user account that may have been deactivated or temporarily suspended.
    /// The request requires the user's email address for identification and verification purposes.
    /// </summary>
    /// <param name="Email">
    /// The email address associated with the user account that needs restoration.
    /// It must be a valid email format for the request to be processed.
    /// </param>
    public sealed record RestoreAccountRequest(
        [Required, EmailAddress] string Email);
}