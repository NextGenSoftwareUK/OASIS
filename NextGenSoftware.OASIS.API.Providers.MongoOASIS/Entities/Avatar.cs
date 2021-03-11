
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using System;
using System.Collections.Generic;

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
        public EnumValue<AvatarType> AvatarType { get; set; }
        public int Karma { get; set; }
        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
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
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }

        public bool IsActive { get; set; }

    }

    ////TODO: More types will be added later.
    //public enum AvatarType
    //{
    //    Admin, //0
    //    Standard //1
    //}
}