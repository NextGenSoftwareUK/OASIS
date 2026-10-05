namespace Application.DTOs.Account.Requests
{
    /// <summary>
    /// Represents a request to register a new user account.
    /// Contains the email address, username, and password (with confirmation) required for registration.
    /// </summary>
    public sealed record RegisterRequest
    {
        /// <summary>
        /// Gets the email address of the user.
        /// The email must be provided and in a valid email address format.
        /// </summary>
        [Required, EmailAddress]
        public string EmailAddress { get; init; } = string.Empty;

        /// <summary>
        /// Gets the username chosen by the user.
        /// The username is required and must be between 4 and 128 characters long.
        /// </summary>
        [Required, MinLength(4), MaxLength(128)]
        public string UserName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the password for the new user account.
        /// The password is required and must be between 8 and 128 characters long.
        /// </summary>
        [Required, MinLength(8), MaxLength(128)]
        public string Password { get; init; } = string.Empty;

        /// <summary>
        /// Gets the confirmation of the password.
        /// This value must match the value of the <see cref="Password"/> property.
        /// </summary>
        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}