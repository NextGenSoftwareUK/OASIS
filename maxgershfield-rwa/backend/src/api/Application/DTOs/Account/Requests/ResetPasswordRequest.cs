namespace Application.DTOs.Account.Requests
{
    /// <summary>
    /// Represents a request for resetting the password of a user account.
    /// This request is typically made after the user has successfully received a reset code via email.
    /// The user provides their email, the reset code, and the new password they wish to set.
    ///</summary>
    public sealed record ResetPasswordRequest
    {
        /// <summary>
        /// Gets or sets the email address associated with the user account.
        /// </summary>
        [Required, EmailAddress]
        public string EmailAddress { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the 6-digit reset code received by the user.
        /// </summary>
        [Required, RegularExpression(@"^\d{6}$",
             ErrorMessage = "The reset code must be a 6-digit number.")]
        public string ResetCode { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the new password for the user account.
        /// </summary>
        [Required, MinLength(8), MaxLength(128)]
        public string NewPassword { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the confirmation password to ensure the new password is accurate.
        /// </summary>
        [Required, Compare(nameof(NewPassword),
             ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}
