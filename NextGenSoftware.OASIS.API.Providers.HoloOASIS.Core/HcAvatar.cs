
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.TestHarness;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HcAvatar : HoloNETAuditEntryBaseClass, IHcAvatar
    {
        public HcAvatar() : base("oasis", "get_entry_avatar", "create_entry_avatar", "update_entry_avatar", "delete_entry_avatar") { }
        public HcAvatar(HoloNETClient holoNETClient) : base("oasis", "get_entry_avatar", "create_entry_avatar", "update_entry_avatar", "delete_entry_avatar", holoNETClient) { }


        #region IAvatar Properties

        [HolochainFieldName("id")]
        public Guid Id { get; set; }

        [HolochainFieldName("username")]
        public string Username { get; set; }

        [HolochainFieldName("password")]
        public string Password { get; set; }

        [HolochainFieldName("email")]
        public string Email { get; set; }

        [HolochainFieldName("title")]
        public string Title { get; set; }

        [HolochainFieldName("first_name")]
        public string FirstName { get; set; }

        [HolochainFieldName("last_name")]
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

        [HolochainFieldName("holon_type")]
        public HolonType HolonType { get; set; }

        [HolochainFieldName("provider_key")]
        public string ProviderUniqueStorageKey { get; set; }

        [HolochainFieldName("previous_version_provider_unique_storage_key")]
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }

        [HolochainFieldName("provider_wallets")]
        public Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get; set; }

        [HolochainFieldName("provider_username")]
        public Dictionary<ProviderType, string> ProviderUsername { get; set; }

        [HolochainFieldName("provider_meta_data")]
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }

        [HolochainFieldName("meta_data")]
        public Dictionary<string, string> MetaData { get; set; }
        public int Version { get; set; }
        public Guid VersionId { get; set; }
        public Guid PreviousVersionId { get; set; }
        public bool IsActive { get; set; }
        public bool IsChanged { get; set; }
        public bool IsNewHolon { get; set; }
        public bool IsSaving { get; set; }
        public IHolon Original { get; set; }

        //Part of HoloNETAuditEntryBaseClass so no need to re-define here.
        //Guid CreatedByAvatarId { get; set; }
        //DateTime CreatedDate { get; set; }
        //Guid ModifiedByAvatarId { get; set; }
        //DateTime ModifiedDate { get; set; }
        //Guid DeletedByAvatarId { get; set; }
        //DateTime DeletedDate { get; set; }
        public EnumValue<ProviderType> CreatedProviderType { get; set; }
        public EnumValue<OASISType> CreatedOASISType { get; set; }

        #endregion
    }
}
