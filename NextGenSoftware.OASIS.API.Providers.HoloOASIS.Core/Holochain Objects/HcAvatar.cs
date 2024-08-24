using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.Holochain.HoloNET.ORM.Entries;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HcAvatar : HoloNETAuditEntryBase, IHcAvatar
    {
        public HcAvatar() : base("oasis", "get_entry_avatar", "create_entry_avatar", "update_entry_avatar", "delete_entry_avatar") { }
        public HcAvatar(IHoloNETClientAppAgent holoNETClient) : base("oasis", "get_entry_avatar", "create_entry_avatar", "update_entry_avatar", "delete_entry_avatar", holoNETClient) { }


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

        [HolochainRustFieldName("avatar_type")]
        //public int AvatarType { get; set; }
        public EnumValue<AvatarType> AvatarType { get; set; } //TODO: Will attempt to do mappings like this in HoloNET ORM itself (such as converting Enums to ints and DateTimes to strings etc).

        [HolochainRustFieldName("accept_terms")]
        public bool AcceptTerms { get; set; }

        [HolochainRustFieldName("is_verified")]
        public bool IsVerified { get; }

        [HolochainRustFieldName("jwt_token")]
        public string JwtToken { get; set; }

        [HolochainRustFieldName("password_reset")]
        public DateTime? PasswordReset { get; set; }

        [HolochainRustFieldName("refresh_token")]
        public string RefreshToken { get; set; }

        [HolochainRustFieldName("refresh_tokens")]
        public List<RefreshToken> RefreshTokens { get; set; }

        [HolochainRustFieldName("reset_token")]
        public string ResetToken { get; set; }

        [HolochainRustFieldName("reset_token_expires")]
        public DateTime? ResetTokenExpires { get; set; }

        [HolochainRustFieldName("verification_token")]
        public string VerificationToken { get; set; }

        [HolochainRustFieldName("verified")]
        public DateTime? Verified { get; set; }

        [HolochainRustFieldName("last_beamed_in")]
        public DateTime? LastBeamedIn { get; set; }

        [HolochainRustFieldName("last_beamed_out")]
        public DateTime? LastBeamedOut { get; set; }

        [HolochainRustFieldName("is_beamed_in")]
        public bool IsBeamedIn { get; set; }

        #endregion

        #region IHolonBase Properties

        [HolochainRustFieldName("name")]
        public string Name { get; set; }

        [HolochainRustFieldName("description")]
        public string Description { get; set; }

        [HolochainRustFieldName("holon_type")]
        public HolonType HolonType { get; set; }

        [HolochainRustFieldName("provider_unique_storage_key")]
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }

        [HolochainRustFieldName("previous_version_provider_unique_storage_key")]
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; }

        [HolochainRustFieldName("provider_wallets")]
        public Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get; set; }

        [HolochainRustFieldName("provider_username")]
        public Dictionary<ProviderType, string> ProviderUsername { get; set; }

        [HolochainRustFieldName("provider_meta_data")]
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }

        [HolochainRustFieldName("meta_data")]
        public Dictionary<string, object> MetaData { get; set; }

        //Already on HoloNETAuditEntryBase
        //[HolochainRustFieldName("version")]
        //public int Version { get; set; }

        [HolochainRustFieldName("version_id")]
        public Guid VersionId { get; set; }

        [HolochainRustFieldName("previous_version_id")]
        public Guid PreviousVersionId { get; set; }

        //Already on HoloNETAuditEntryBase
        //[HolochainRustFieldName("is_active")]
        //public bool IsActive { get; set; }

        [HolochainRustFieldName("created_provider_type")]
        public EnumValue<ProviderType> CreatedProviderType { get; set; }

        [HolochainRustFieldName("created_oasis_type")]
        public EnumValue<OASISType> CreatedOASISType { get; set; }

        [HolochainRustFieldName("children")]
        public IList<IHolon> Children { get; set; }

        [HolochainRustFieldName("custom_key")]
        public string CustomKey { get; set; }

        [HolochainRustFieldName("instance_saved_on_provider_type")]
        public EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }

        [HolochainRustFieldName("parent_holon_id")]
        public Guid ParentHolonId { get; set; }

        [HolochainRustFieldName("child_id_list_cache")]
        public string ChildIdListCache { get; set; }

        [HolochainRustFieldName("all_child_id_list_cache")]
        public string AllChildIdListCache { get; set; }

        #endregion
    }
}
