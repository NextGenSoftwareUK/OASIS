using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
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
        public string Description { get; set; }
        public string DID { get; set; }
        public string DIDPublicKey { get; set; }
        public bool? AcceptTerms { get; set; }
        public bool? IsActive { get; set; }

        /// <summary>Arbitrary key-value metadata stored on the Avatar.</summary>
        public Dictionary<string, object> MetaData { get; set; }

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