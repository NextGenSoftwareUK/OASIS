using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.Holochain.HoloNET.ORM.Entries;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

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
        public long Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?

        //[HolochainRustFieldName("level")]
        //public int Level { get; set; }

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

        [HolochainRustFieldName("attributes")]
        public IAvatarAttributes Attributes { get; set; }

        [HolochainRustFieldName("aura")]
        public IAvatarAura Aura { get; set; }

        [HolochainRustFieldName("chakras")]
        public IAvatarChakras Chakras { get; set; }

        [HolochainRustFieldName("dimension_level_ids")]
        public IDictionary<DimensionLevel, Guid> DimensionLevelIds { get; set; }

        [HolochainRustFieldName("dimension_levels")]
        public IDictionary<DimensionLevel, IHolon> DimensionLevels { get; set; }

        [HolochainRustFieldName("favourite_colour")]
        public ConsoleColor FavouriteColour { get; set; }

        [HolochainRustFieldName("gene_keys")]
        public IList<IGeneKey> GeneKeys { get; set; }

        [HolochainRustFieldName("gifts")]
        public IList<IAvatarGift> Gifts { get; set; }

        [HolochainRustFieldName("heart_rate_data")]
        public IList<IHeartRateEntry> HeartRateData { get; set; }

        [HolochainRustFieldName("human_design")]
        public IHumanDesign HumanDesign { get; set; }

        [HolochainRustFieldName("inventory")]
        public IList<IInventoryItem> Inventory { get; set; }

        [HolochainRustFieldName("karma_akashic_records")]
        public IList<IKarmaAkashicRecord> KarmaAkashicRecords { get; set; }

        [HolochainRustFieldName("omniverse")]
        public IOmiverse Omniverse { get; set; }

        [HolochainRustFieldName("skills")]
        public IAvatarSkills Skills { get; set; }

        [HolochainRustFieldName("spells")]
        public IList<ISpell> Spells { get; set; }

        [HolochainRustFieldName("star_cli_colour")]
        public ConsoleColor STARCLIColour { get; set; }

        [HolochainRustFieldName("stats")]
        public IAvatarStats Stats { get; set; }

        [HolochainRustFieldName("super_powers")]
        public IAvatarSuperPowers SuperPowers { get; set; }

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

        [HolochainRustFieldName("version")]
        public int Version { get; set; }

        [HolochainRustFieldName("version_id")]
        public Guid VersionId { get; set; }

        [HolochainRustFieldName("previous_version_id")]
        public Guid PreviousVersionId { get; set; }

        [HolochainRustFieldName("is_active")]
        public bool IsActive { get; set; }

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