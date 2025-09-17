using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
    //TODO: Need to add rest of Avatar fields here (do we need seperate object? Could just add this DataAnnotations to the main Avatar object in Core?
    public class UpdateRequest
    {
        private string _password;
        private string _confirmPassword;
        private string _avatarType;
        private string _email;
        
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }

        [EnumDataType(typeof(AvatarType))]
        public string AvatarType
        {
            get => _avatarType;
            set => _avatarType = replaceEmptyWithNull(value);
        }

        [EmailAddress]
        public string Email
        {
            get => _email;
            set => _email = replaceEmptyWithNull(value);
        }

        [MinLength(6)]
        public string Password
        {
            get => _password;
            set => _password = replaceEmptyWithNull(value);
        }

        [Compare("Password")]
        public string ConfirmPassword 
        {
            get => _confirmPassword;
            set => _confirmPassword = replaceEmptyWithNull(value);
        }

        // helpers

        private string replaceEmptyWithNull(string value)
        {
            // replace empty string with null to make field optional
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}