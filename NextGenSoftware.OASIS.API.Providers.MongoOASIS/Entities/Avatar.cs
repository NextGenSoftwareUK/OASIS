
using NextGenSoftware.OASIS.API.Core;
using System;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public class Avatar : BaseEntity
    {
        public string AvatarId { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName
        {
            get
            {
                return string.Concat(Title, " ", FirstName, " ", LastName);
            }
        }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Address { get; set; }
        public string Postcode { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string Landline { get; set; }
        public string Mobile { get; set; }
        public DateTime DOB { get; set; }
        public AvatarType AvatarType { get; set; }
        public int Karma { get; set; }
        public int Level { get; set; }
        public HolonType HolonType { get; set; }

        public bool AcceptTerms { get; set; }
        public string VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string ResetToken { get; set; }
        public string JwtToken { get; set; }

     //   [JsonIgnore] // refresh token is returned in http only cookie
        public string RefreshToken { get; set; }

        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }

    }

    ////TODO: More types will be added later.
    //public enum AvatarType
    //{
    //    Admin, //0
    //    Standard //1
    //}
}