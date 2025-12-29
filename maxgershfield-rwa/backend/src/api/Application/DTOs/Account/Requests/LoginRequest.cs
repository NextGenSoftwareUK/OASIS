namespace Application.DTOs.Account.Requests
{
    /// <summary>
    /// Represents the request data for logging in a user.
    /// Contains the user's email and password to authenticate and gain access.
    /// </summary>
    /// <param name="Email">
    /// The email address of the user. Must be a valid email format and between 4 and 128 characters long.
    /// </param>
    /// <param name="Password">
    /// The password of the user. Must be between 8 and 128 characters long.
    /// </param>
    public sealed record LoginRequest(
        [Required, MinLength(4), MaxLength(128), EmailAddress]
        string Email,

        [Required, MinLength(8), MaxLength(128)]
        string Password);
}