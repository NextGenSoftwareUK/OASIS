using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models.Security
{
    public class ValidateResetTokenRequest
    {
        [Required]
        public string Token { get; set; }
    }
}