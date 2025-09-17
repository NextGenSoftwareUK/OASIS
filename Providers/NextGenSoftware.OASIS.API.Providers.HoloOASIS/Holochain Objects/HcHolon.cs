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

        #region IHolon Properties

        [HolochainRustFieldName("parent_omniverse_id")]
        public Guid ParentOmniverseId { get; set; } //The Omniverse this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IOmiverse ParentOmniverse { get; set; } //The Omniverse this Holon belongs to.

        [HolochainRustFieldName("parent_multiverse_id")]
        public Guid ParentMultiverseId { get; set; } //The Multiverse this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IMultiverse ParentMultiverse { get; set; } //The Multiverse this Holon belongs to.

        [HolochainRustFieldName("parent_universe_id")]
        public Guid ParentUniverseId { get; set; } //The Universe this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IUniverse ParentUniverse { get; set; } //The Universe this Holon belongs to.

        [HolochainRustFieldName("parent_dimension_id")]
        public Guid ParentDimensionId { get; set; } //The Dimension this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IDimension ParentDimension { get; set; } //The Dimension this Holon belongs to.

        [HolochainRustFieldName("dimension_level")]
        public DimensionLevel DimensionLevel { get; set; } //The dimension this Holon belongs to (a holon can have a different version of itself in each dimension (asscended/evolved versions of itself).

        [HolochainRustFieldName("sub_dimension_level")]
        public SubDimensionLevel SubDimensionLevel { get; set; } //The sub-dimension/plane this Holon belongs to.

        [HolochainRustFieldName("parent_galaxy_cluster_id")]
        public Guid ParentGalaxyClusterId { get; set; } //The GalaxyCluster this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IGalaxyCluster ParentGalaxyCluster { get; set; } //The GalaxyCluster this Holon belongs to.

        [HolochainRustFieldName("parent_galaxy_id")]
        public Guid ParentGalaxyId { get; set; } //The Galaxy this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IGalaxy ParentGalaxy { get; set; } //The Galaxy this Holon belongs to.

        [HolochainRustFieldName("parent_solar_system_id")]
        public Guid ParentSolarSystemId { get; set; } //The SolarSystem this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public ISolarSystem ParentSolarSystem { get; set; } //The SolarSystem this Holon belongs to.

        [HolochainRustFieldName("parent_great_grand_super_star_id")]
        public Guid ParentGreatGrandSuperStarId { get; set; } //The GreatGrandSuperStar this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; } //The GreatGrandSuperStar this Holon belongs to.

        [HolochainRustFieldName("parent_grand_super_star_id")]
        public Guid ParentGrandSuperStarId { get; set; } //The GrandSuperStar this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IGrandSuperStar ParentGrandSuperStar { get; set; } //The GrandSuperStar this Holon belongs to.

        [HolochainRustFieldName("parent_super_star_id")]
        public Guid ParentSuperStarId { get; set; } //The SuperStar this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public ISuperStar ParentSuperStar { get; set; } //The SuperStar this Holon belongs to.

        [HolochainRustFieldName("parent_star_id")]
        public Guid ParentStarId { get; set; } //The Star this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IStar ParentStar { get; set; } //The Star this Holon belongs to.

        [HolochainRustFieldName("parent_planet_id")]
        public Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.

        [HolochainRustFieldName("parent_moon_id")]
        public Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.    

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.

        [HolochainRustFieldName("parent_celestial_space_id")]
        public Guid ParentCelestialSpaceId { get; set; } // The CelestialSpace Id this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public ICelestialSpace ParentCelestialSpace { get; set; } // The CelestialSpace this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 

        [HolochainRustFieldName("parent_celestial_body_id")]
        public Guid ParentCelestialBodyId { get; set; } // The CelestialBody Id this holon belongs to (this could be a moon, planet, star, etc). 

        //[HolochainRustFieldName("all_child_id_list_cache")]
        //public ICelestialBody ParentCelestialBody { get; set; } // The CelestialBody  this holon belongs to (this could be a moon, planet, star, etc). 

        [HolochainRustFieldName("parent_zome_id")]
        public Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.

        //[HolochainRustFieldName("parent_zome")]
        //public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        //public  ObservableCollection<INode> Nodes { get; set; }

        [HolochainRustFieldName("nodes")]
        public IList<INode> Nodes { get; set; }

        #endregion
    }
}
