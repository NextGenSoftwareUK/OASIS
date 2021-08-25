using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatar : IHolon
    {
       // Guid AvatarId { get; }
        Dictionary<ProviderType, string> ProviderPrivateKey { get; set; }
        Dictionary<ProviderType, string> ProviderPublicKey { get; set; } 
        Dictionary<ProviderType, string> ProviderUsername { get; set; } 
        Dictionary<ProviderType, string> ProviderWalletAddress { get; set; }

        string Image2D { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        ConsoleColor FavouriteColour { get; set; }
        ConsoleColor STARCLIColour { get; set; }
        string FullName { get; }
        DateTime DOB { get; set; }
        string Address { get; set; }
        int Karma { get; set; }
        int Level { get; }
        int XP { get; set; }
        IOmiverse Omiverse { get; set; } //We have all of creation inside of us... ;-)
        List<AvatarGift> Gifts { get; set; }
        //public List<Chakra> Chakras { get; set; }
        AvatarChakras Chakras { get; set; }
        AvatarAura Aura { get; set; }
        AvatarStats Stats { get; set; }
        List<GeneKey> GeneKeys { get; set; } 
        HumanDesign HumanDesign { get; set; } 
        AvatarSkills Skills { get; set; } 
        AvatarAttributes Attributes { get; set; }
        AvatarSuperPowers SuperPowers { get; set; } 
        List<Spell> Spells { get; set; } 
        List<Achievement> Achievements { get; set; } 
        List<InventoryItem> Inventory { get; set; } 
        string Town { get; set; }
        string County { get; set; }
        string Country { get; set; }
        string Postcode { get; set; }
        string Mobile { get; set; }
        string Landline { get; set; }
        EnumValue<AvatarType> AvatarType { get; set; }
        EnumValue<OASISType> CreatedOASISType { get; set; }
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

        Task<KarmaAkashicRecord> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        Task<KarmaAkashicRecord> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);

        KarmaAkashicRecord KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);
        KarmaAkashicRecord KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0);


        List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        Task<IAvatar> SaveAsync();
        IAvatar Save();
    }
}
