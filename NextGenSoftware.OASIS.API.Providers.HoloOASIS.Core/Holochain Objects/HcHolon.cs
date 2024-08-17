
using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.Holochain.HoloNET.ORM.Entries;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HcHolon : HoloNETAuditEntryBase, IHcHolon
    {
        public HcHolon() : base("oasis", "get_entry_holon", "create_entry_holon", "update_entry_holon", "delete_entry_holon") { }
        public HcHolon(IHoloNETClientAppAgent holoNETClient) : base("oasis", "get_entry_holon", "create_entry_holon", "update_entry_holon", "delete_entry_holon", holoNETClient) { }

        #region IHolonBase Properties

        [HolochainRustFieldName("id")]
        public Guid Id { get; set; }

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

        [HolochainRustFieldName("created_provider_type")]
        public EnumValue<ProviderType> CreatedProviderType { get; set; }

        [HolochainRustFieldName("created_oasis_type")]
        public EnumValue<OASISType> CreatedOASISType { get; set; }

        #endregion
    }
}
