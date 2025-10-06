using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("Holon")]
    public class HolonModel {

        [Required, Key]
        public String Id{ set; get;}
        public String ParentHolonId{ set; get;}

        public HolonType HolonType { get; set; }
        public string Name{set;get;}
        public string Description{set;get;}

        public bool IsNewHolon { get; set; }
        public bool IsChanged { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
        
        public DimensionModel ParentDimension { get; set; }
        public DimensionLevel DimensionLevel { get; set; }
        public SubDimensionLevel SubDimensionLevel { get; set; }

        public StarModel ParentStar{ set; get;}
        public SuperStarModel ParentSuperStar{ get; set; }
        public GrandSuperStarModel ParentGrandSuperStar{ get; set; }
        public GreatGrandSuperStarModel ParentGreatGrandSuperStar{ get; set; }
        public MoonModel ParentMoon{ get; set; }
        public PlanetModel ParentPlanet { get; set; }
        public SolarSystemModel ParentSolarSystem { get; set; }
        public GalaxyModel ParentGalaxy { get; set; }
        public GalaxyClusterModel ParentGalaxyCluster { get; set; }
        public UniverseModel ParentUniverse { get; set; }

        public ProviderType CreatedProviderType { get; set; }
        public OASISType CreatedOASISType { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime DeletedDate { get; set; }

        public string CreatedByAvatarId { get; set; }
        public String ModifiedByAvatarId { get; set; }
        public String DeletedByAvatarId { get; set; }

        public List<HolonModel> Childrens{ set; get;} = new List<HolonModel>();

        public List<MetaDataModel> MetaData { get; set; } = new List<MetaDataModel>();
        public List<ProviderKeyModel> ProviderKey { get; set; } = new List<ProviderKeyModel>();
        public List<ProviderMetaData> ProviderMetaData { get; set; } = new List<ProviderMetaData>();

        public HolonModel(){}
        public HolonModel(IHolon source){
            
            if(source.Id == Guid.Empty){
                this.Id = Guid.NewGuid().ToString();
            }
            else{
                this.Id = source.Id.ToString();
            }

            this.ParentHolonId = source.ParentHolonId.ToString();
            this.Name = source.Name;
            this.Description = source.Description;
            this.HolonType = source.HolonType;

            this.IsChanged = source.IsChanged;
            this.IsNewHolon = source.IsNewHolon;
            this.IsActive = source.IsActive;
            this.Version = source.Version;

            this.DimensionLevel = source.DimensionLevel;
            this.SubDimensionLevel = source.SubDimensionLevel;

            if (source.CreatedProviderType != null)
                this.CreatedProviderType = source.CreatedProviderType.Value;
            else
                this.CreatedProviderType = ProviderType.SQLLiteDBOASIS;

            this.CreatedOASISType = source.CreatedOASISType.Value;
            
            this.CreatedDate = source.CreatedDate;
            this.ModifiedDate = source.ModifiedDate;
            this.DeletedDate = source.DeletedDate;

            this.CreatedByAvatarId = source.CreatedByAvatarId.ToString();
            this.ModifiedByAvatarId = source.ModifiedByAvatarId.ToString();
            this.DeletedByAvatarId = source.DeletedByAvatarId.ToString();


            if(source.ParentDimension != null){
                this.ParentDimension = new DimensionModel(source.ParentDimension);
            }

            if(source.ParentStar != null){
                this.ParentStar = new StarModel(source.ParentStar);
            }

            if(source.ParentSuperStar != null){
                this.ParentSuperStar = new SuperStarModel(source.ParentSuperStar);
            }

            if(source.ParentGrandSuperStar != null){
                this.ParentGrandSuperStar = new GrandSuperStarModel(source.ParentGrandSuperStar);
            }

            if(source.ParentGreatGrandSuperStar != null){
                this.ParentGreatGrandSuperStar = new GreatGrandSuperStarModel(source.ParentGreatGrandSuperStar);
            }

            if(source.ParentMoon != null){
                this.ParentMoon = new MoonModel(source.ParentMoon);
            }

            if(source.ParentPlanet != null){
                this.ParentPlanet = new PlanetModel(source.ParentPlanet);
            }

            if(source.ParentSolarSystem != null){
                this.ParentSolarSystem = new SolarSystemModel(source.ParentSolarSystem);
            }

            if(source.ParentGalaxy != null){
                this.ParentGalaxy = new GalaxyModel(source.ParentGalaxy);
            }

            if(source.ParentGalaxyCluster != null){
                this.ParentGalaxyCluster = new GalaxyClusterModel(source.ParentGalaxyCluster);
            }

            if(source.ParentUniverse != null){
                this.ParentUniverse = new UniverseModel(source.ParentUniverse);
            }

            foreach(KeyValuePair<ProviderType, string> item in source.ProviderUniqueStorageKey){

                ProviderKeyModel providerKey=new ProviderKeyModel(item.Key,item.Value);
                providerKey.OwnerId=this.Id;
                this.ProviderKey.Add(providerKey);
            }

            foreach(KeyValuePair<string, object> item in source.MetaData){

                MetaDataModel metaModel=new MetaDataModel(item.Key,item.Value);
                metaModel.OwnerId=this.Id;
                this.MetaData.Add(metaModel);
            }

            Dictionary<string,string> providerMeta = source.ProviderMetaData.GetValueOrDefault(ProviderType.SQLLiteDBOASIS);
            if(providerMeta != null){

                foreach(KeyValuePair<string, string> item in providerMeta){

                    ProviderMetaData metaModel=new ProviderMetaData(ProviderType.SQLLiteDBOASIS, item.Key,item.Value);
                    metaModel.OwnerId=this.Id;
                    this.ProviderMetaData.Add(metaModel);
                }
            }

        }

        public Holon GetHolon()
        {
            Holon item=new Holon();

            item.Id = Guid.Parse(this.Id);

            item.ParentHolonId = Guid.Parse(this.ParentHolonId);
            item.Name = this.Name;
            item.Description = this.Description;
            item.HolonType = this.HolonType;

            item.IsChanged = this.IsChanged;
            item.IsNewHolon = this.IsNewHolon;
            item.IsActive = this.IsActive;
            item.Version = this.Version;

            item.DimensionLevel = this.DimensionLevel;
            item.SubDimensionLevel = this.SubDimensionLevel;

            item.CreatedProviderType = new EnumValue<ProviderType>(this.CreatedProviderType);
            item.CreatedOASISType = new EnumValue<OASISType>(this.CreatedOASISType);

            item.CreatedByAvatarId = Guid.Parse(this.CreatedByAvatarId);
            item.ModifiedByAvatarId = Guid.Parse(this.ModifiedByAvatarId);
            item.DeletedByAvatarId = Guid.Parse(this.DeletedByAvatarId);

            item.CreatedDate = this.CreatedDate;
            item.ModifiedDate = this.ModifiedDate;           
            item.DeletedDate = this.DeletedDate;


            // item.ParentDimension = this.ParentDimension.GetDimension();
            // item.ParentStar = this.ParentStar.GetStar();
            // item.ParentSuperStar = this.ParentSuperStar.GetSuperStar();
            // item.ParentGrandSuperStar = this.ParentGrandSuperStar.GetGrandSuperStar();
            // item.ParentGreatGrandSuperStar = this.ParentGreatGrandSuperStar.GetGreatGrandSuperStar();
            //
            // item.ParentPlanet = this.ParentPlanet.GetPlanet();
            // item.ParentMoon = this.ParentMoon.GetMoon();
            // item.ParentSolarSystem = this.ParentSolarSystem.GetSolarSystem();
            //
            // item.ParentGalaxy = this.ParentGalaxy.GetGalaxy();
            // item.ParentGalaxyCluster = this.ParentGalaxyCluster.GetGalaxyCluster();
            // item.ParentUniverse = this.ParentUniverse.GetUniverse();

            foreach(ProviderKeyModel model in this.ProviderKey){

                item.ProviderUniqueStorageKey.Add(model.ProviderId, model.Value);
            }

            foreach(MetaDataModel model in this.MetaData){

                item.MetaData.Add(model.Property, model.Value);
            }

            Dictionary<string,string> providerMetaData = new Dictionary<string, string>();
            foreach(ProviderMetaData metaData in this.ProviderMetaData){

                providerMetaData.Add(metaData.Property, metaData.Value);
            }

            item.ProviderMetaData.Add(ProviderType.SQLLiteDBOASIS, providerMetaData);

            return(item);
        }
    }
}