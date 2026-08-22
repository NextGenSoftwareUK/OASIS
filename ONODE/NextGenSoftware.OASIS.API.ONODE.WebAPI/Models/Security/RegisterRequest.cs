using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
    public class RegisterRequest : CreateRequest
    {
        //[Required]
        //public string Title { get; set; }

        //[Required]
        //public string FirstName { get; set; }

        //[Required]
        //public string LastName { get; set; }

        //[Required]
        //[EmailAddress]
        //public string Email { get; set; }

        //[Required]
        //[MinLength(6)]
        //public string Password { get; set; }

        //[Required]
        //[Compare("Password")]
        //public string ConfirmPassword { get; set; }

        [Range(typeof(bool), "true", "true")]
        public bool AcceptTerms { get; set; }

        /// <summary>
        /// When true, suppresses the verification email that is normally sent on registration.
        /// Only honoured when the caller is a Wizard avatar. Intended for automated flows
        /// (e.g. NFT mint) that handle their own notification and verify the email immediately.
        /// </summary>
        public bool SuppressVerificationEmail { get; set; } = false;
    }
}