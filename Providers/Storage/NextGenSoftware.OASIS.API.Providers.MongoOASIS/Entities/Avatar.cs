using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities
{
    [BsonIgnoreExtraElements]
    public class Avatar : HolonBase
    {
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ProviderType, List<ProviderWallet>> ProviderWallets { get; set; } = new Dictionary<ProviderType, List<ProviderWallet>>();

        //[BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        //public Dictionary<ProviderType, string> ProviderPrivateKey { get; set; } = new Dictionary<ProviderType, string>();  //Unique private key used by each provider (part of private/public key pair).

        //[BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        //public Dictionary<ProviderType, List<string>> ProviderPublicKey { get; set; } = new Dictionary<ProviderType, List<string>>();

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ProviderType, string> ProviderUsername { get; set; } = new Dictionary<ProviderType, string>();  // This is only really needed when we need to store BOTH a id and username for a provider (ProviderUniqueStorageKey on Holon already stores either id/username etc).                                                                                                            // public Dictionary<ProviderType, string> ProviderId { get; set; } = new Dictionary<ProviderType, string>(); // The ProviderUniqueStorageKey property on the base Holon object can store ids, usernames, etc that uniqueliy identity that holon in the provider (although the Guid is globally unique we still need to map the Holons the unique id/username/etc for each provider).

        //[BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        //public Dictionary<ProviderType, List<string>> ProviderWalletAddress { get; set; } = new Dictionary<ProviderType, List<string>>();
        
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
        public EnumValue<AvatarType> AvatarType { get; set; }
       // public EnumValue<OASISType> CreatedOASISType { get; set; }
        //public int Karma { get; set; }
        //public int XP { get; set; }
        //public int Level { get; set; }
        public bool AcceptTerms { get; set; }
        public string VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string ResetToken { get; set; }
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }
        public DateTime? LastBeamedIn { get; set; }
        public DateTime? LastBeamedOut { get; set; }
        public bool IsBeamedIn { get; set; }
    }
}