using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS.Entities
{
    public class AvatarDetail : Holon
    {
        public string UmaJson { get; set; }
        public string Image2D { get; set; }
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
        public EnumValue<OASISType> CreatedOASISType { get; set; }
        public int Karma { get; set; }
        public Dictionary<ProviderType, string> ProviderPrivateKey { get; set; } = new Dictionary<ProviderType, string>();  //Unique private key used by each provider (part of private/public key pair).
        public Dictionary<ProviderType, string> ProviderPublicKey { get; set; } = new Dictionary<ProviderType, string>();
        public Dictionary<ProviderType, string> ProviderUsername { get; set; } = new Dictionary<ProviderType, string>();  // This is only really needed when we need to store BOTH a id and username for a provider (ProviderKey on Holon already stores either id/username etc).                                                                                                         // public Dictionary<ProviderType, string> ProviderId { get; set; } = new Dictionary<ProviderType, string>(); // The ProviderKey property on the base Holon object can store ids, usernames, etc that uniqueliy identity that holon in the provider (although the Guid is globally unique we still need to map the Holons the unique id/username/etc for each provider).
        public Dictionary<ProviderType, string> ProviderWalletAddress { get; set; } = new Dictionary<ProviderType, string>();
        public ConsoleColor FavouriteColour { get; set; }
        public ConsoleColor STARCLIColour { get; set; }
        public int XP { get; set; }
        public List<AvatarGift> Gifts { get; set; } = new List<AvatarGift>();
        public AvatarChakras Chakras { get; set; } = new AvatarChakras();
        public AvatarAura Aura { get; set; } = new AvatarAura();
        public AvatarStats Stats { get; set; } = new AvatarStats();
        public List<GeneKey> GeneKeys { get; set; } = new List<GeneKey>();
        public HumanDesign HumanDesign { get; set; } = new HumanDesign();
        public AvatarSkills Skills { get; set; } = new AvatarSkills();
        public AvatarAttributes Attributes { get; set; } = new AvatarAttributes();
        public AvatarSuperPowers SuperPowers { get; set; } = new AvatarSuperPowers();
        public List<Spell> Spells { get; set; } = new List<Spell>();
        public List<Achievement> Achievements { get; set; } = new List<Achievement>();
        public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        public int Level { get; set; }
        //public bool AcceptTerms { get; set; }
        //public string VerificationToken { get; set; }
        //public DateTime? Verified { get; set; }
        //public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        //public string ResetToken { get; set; }
        //public string JwtToken { get; set; }
        //public string RefreshToken { get; set; }
        //public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        //public DateTime? ResetTokenExpires { get; set; }
        //public DateTime? PasswordReset { get; set; }
    }
}