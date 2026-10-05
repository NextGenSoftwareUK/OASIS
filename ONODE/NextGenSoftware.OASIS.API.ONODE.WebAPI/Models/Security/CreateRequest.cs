using NextGenSoftware.OASIS.API.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
    public class CreateRequest
    {
        public string Title { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
       // [EnumDataType(typeof(AvatarType))]
        public string AvatarType { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        public OASISType CreatedOASISType { get; set; } = OASISType.OASISAPIREST;
    }
}