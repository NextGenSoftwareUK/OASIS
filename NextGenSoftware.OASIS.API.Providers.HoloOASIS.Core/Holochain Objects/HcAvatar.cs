
using System;
using System.Collections.Generic;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.ORM.Entries;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HcAvatar : HoloNETAuditEntryBase, IHcAvatar
    {
        public HcAvatar() : base("oasis", "get_entry_avatar", "create_entry_avatar", "update_entry_avatar", "delete_entry_avatar") { }
        public HcAvatar(HoloNETClientAppAgent holoNETClient) : base("oasis", "get_entry_avatar", "create_entry_avatar", "update_entry_avatar", "delete_entry_avatar", holoNETClient) { }


        #region IAvatar Properties

        [HolochainRustFieldName("id")]
        public Guid Id { get; set; }

        [HolochainRustFieldName("username")]
        public string Username { get; set; }

        [HolochainRustFieldName("password")]
        public string Password { get; set; }

        [HolochainRustFieldName("email")]
        public string Email { get; set; }

        [HolochainRustFieldName("title")]
        public string Title { get; set; }

        [HolochainRustFieldName("first_name")]
        public string FirstName { get; set; }

        [HolochainRustFieldName("last_name")]
        public string LastName { get; set; }

        public EnumValue<AvatarType> AvatarType { get; set; }
        public bool AcceptTerms { get; set; }
        public bool IsVerified { get; }
        public string JwtToken { get; set; }
        public DateTime? PasswordReset { get; set; }
        public string RefreshToken { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }
        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public string VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public DateTime? LastBeamedIn { get; set; }
        public DateTime? LastBeamedOut { get; set; }
        public bool IsBeamedIn { get; set; }

        #endregion

        #region IHolonBase Properties

        public string Name { get; set; }
        public string Description { get; set; }

        [HolochainRustFieldName("holon_type")]
        public HolonType HolonType { get; set; }

        [HolochainRustFieldName("provider_key")]
        public string ProviderUniqueStorageKey { get; set; }

        [HolochainRustFieldName("previous_version_provider_unique_storage_key")]
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }

        [HolochainRustFieldName("provider_wallets")]
        public Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get; set; }

        [HolochainRustFieldName("provider_username")]
        public Dictionary<ProviderType, string> ProviderUsername { get; set; }

        [HolochainRustFieldName("provider_meta_data")]
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }

        [HolochainRustFieldName("meta_data")]
        public Dictionary<string, string> MetaData { get; set; }

        [HolochainRustFieldName("version")]
        public int Version { get; set; }

        [HolochainRustFieldName("version_id")]
        public Guid VersionId { get; set; }

        [HolochainRustFieldName("previous_version_id")]
        public Guid PreviousVersionId { get; set; }

        [HolochainRustFieldName("is_active")]
        public bool IsActive { get; set; }


        //Part of HoloNETAuditEntryBaseClass so no need to re-define here.
        //Guid CreatedByAvatarId { get; set; }
        //DateTime CreatedDate { get; set; }
        //Guid ModifiedByAvatarId { get; set; }
        //DateTime ModifiedDate { get; set; }
        //Guid DeletedByAvatarId { get; set; }
        //DateTime DeletedDate { get; set; }

        [HolochainRustFieldName("created_provider_type")]
        public EnumValue<ProviderType> CreatedProviderType { get; set; }

        [HolochainRustFieldName("created_oasis_type")]
        public EnumValue<OASISType> CreatedOASISType { get; set; }

        #endregion
    }
}
