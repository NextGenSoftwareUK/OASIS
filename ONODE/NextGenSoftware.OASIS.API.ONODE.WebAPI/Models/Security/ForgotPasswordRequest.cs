using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Optional. If provided, the password reset email link will point to this URL
        /// (with ?token= appended) instead of the default OASIS portal URL.
        /// Use this to keep users on your own site's reset-password page.
        /// </summary>
        public string ReturnUrl { get; set; }
    }
}