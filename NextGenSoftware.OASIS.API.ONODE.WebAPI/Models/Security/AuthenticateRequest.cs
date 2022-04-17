using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
    public class AuthenticateRequest
    {
        [Required]
        //[EmailAddress] //The username will default to their email address but they can later change it to something else so this could be any string.
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}