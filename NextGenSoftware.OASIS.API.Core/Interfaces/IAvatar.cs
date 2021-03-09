using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface IAvatar : IHolon
    {
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string FullName { get; }
        DateTime DOB { get; set; }
        string Address { get; set; }
        int Karma { get; }
        int Level { get; }
        string Town { get; set; }
        string County { get; set; }
        string Country { get; set; }
        string Postcode { get; set; }
        string Mobile { get; set; }
        string Landline { get; set; }
        AvatarType AvatarType { get; set; }

        bool AcceptTerms { get; set; }
        public string VerificationToken { get; set; }
        DateTime? Verified { get; set; }
        bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        string ResetToken { get; set; }
        string JwtToken { get; set; }
        string RefreshToken { get; set; }
        DateTime? ResetTokenExpires { get; set; }
        DateTime? PasswordReset { get; set; }
        //public DateTime Created { get; set; }
        //public DateTime? Updated { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }

        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }

        Task<KarmaAkashicRecord> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true, int karmaOverride = 0);
        Task<KarmaAkashicRecord> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true, int karmaOverride = 0);

        KarmaAkashicRecord KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true, int karmaOverride = 0);
        KarmaAkashicRecord KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true, int karmaOverride = 0);


        List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        Task<IAvatar> SaveAsync();
        IAvatar Save();
    }
}
