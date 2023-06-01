using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models.Security
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}