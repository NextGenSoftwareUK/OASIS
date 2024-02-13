
using System;
using System.Collections.Generic;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.Holochain.HoloNET.ORM.Entries;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HcAvatarDetail : HoloNETAuditEntryBase, IHcAvatarDetail
    {
        public HcAvatarDetail() : base("oasis", "get_entry_avatar_detail", "create_entry_avatar_detail", "update_entry_avatar_detail", "delete_entry_avatar_detail") { }
        public HcAvatarDetail(IHoloNETClientAppAgent holoNETClient) : base("oasis", "get_entry_avatar_detail", "create_entry_avatar_detail", "update_entry_avatar_detail", "delete_entry_avatar_detail", holoNETClient) { }

        #region IAvatarDetail Properties

        [HolochainRustFieldName("id")]
        public Guid Id { get; set; }

        [HolochainRustFieldName("username")]
        public string Username { get; set; }

        [HolochainRustFieldName("email")]
        public string Email { get; set; }

        [HolochainRustFieldName("karma")]
        public int Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?

        [HolochainRustFieldName("level")]
        public int Level { get; set; }

        [HolochainRustFieldName("xp")]
        public int XP { get; set; }

        [HolochainRustFieldName("model_3d")]
        public string Model3D { get; set; }

        [HolochainRustFieldName("uma_json")]
        public string UmaJson { get; set; }

        [HolochainRustFieldName("portrait")]
        public string Portrait { get; set; }

        [HolochainRustFieldName("dob")]
        public string DOB { get; set; }

        [HolochainRustFieldName("address")]
        public string Address { get; set; }

        [HolochainRustFieldName("town")]
        public string Town { get; set; }

        [HolochainRustFieldName("county")]
        public string County { get; set; }

        [HolochainRustFieldName("country")]
        public string Country { get; set; }

        [HolochainRustFieldName("post_code")]
        public string Postcode { get; set; }

        [HolochainRustFieldName("landline")]
        public string Landline { get; set; }

        [HolochainRustFieldName("mobile")]
        public string Mobile { get; set; }

        [HolochainRustFieldName("achievements")]
        public IList<IAchievement> Achievements { get; set; }
        public IAvatarAttributes Attributes { get; set; }
        public IAvatarAura Aura { get; set; }
        //EnumValue<AvatarType> AvatarType { get; set; }
        public IAvatarChakras Chakras { get; set; }
        //EnumValue<OASISType> CreatedOASISType { get; set; }
        public IDictionary<DimensionLevel, Guid> DimensionLevelIds { get; set; }
        public IDictionary<DimensionLevel, IHolon> DimensionLevels { get; set; }
        public ConsoleColor FavouriteColour { get; set; }
        public IList<IGeneKey> GeneKeys { get; set; }
        public IList<IAvatarGift> Gifts { get; set; }
        public IList<HeartRateEntry> HeartRateData { get; set; }
        public IHumanDesign HumanDesign { get; set; }
        public IList<IInventoryItem> Inventory { get; set; }
        public IList<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        public IOmiverse Omniverse { get; set; }
        public IAvatarSkills Skills { get; set; }
        public IList<ISpell> Spells { get; set; }
        public ConsoleColor STARCLIColour { get; set; }
        public IAvatarStats Stats { get; set; }
        public IAvatarSuperPowers SuperPowers { get; set; }

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