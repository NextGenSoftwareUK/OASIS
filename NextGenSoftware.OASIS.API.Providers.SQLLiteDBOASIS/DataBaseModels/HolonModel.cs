using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("Holon")]
    public class HolonModel : INotifyPropertyChanged {

        private string _name;
        private string _description;

        public event PropertyChangedEventHandler PropertyChanged;

        public String Id{ set; get;}
        public String ParentHolonId{ set; get;}

        public string Name{set;get;}
        public string Description{set;get;}
        public HolonType HolonType { get; set; }
        public bool IsNewHolon { get; set; }
        public bool IsChanged { get; set; }
        
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

        public List<HolonModel> Childrens{ set; get;}

        public ProviderType CreatedProviderType { get; set; }

        public List<MetaDataModel> MetaData { get; set; } = new List<MetaDataModel>();
        public List<ProviderMetaData> ProviderMetaData { get; set; } = new List<ProviderMetaData>();
        public List<ProviderKeyModel> ProviderKey { get; set; } = new List<ProviderKeyModel>();

        public String CreatedByAvatarId { get; set; }
        public DateTime CreatedDate { get; set; }
        public String ModifiedByAvatarId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public String DeletedByAvatarId { get; set; }
        public DateTime DeletedDate { get; set; }

        public bool IsActive { get; set; }
        public int Version { get; set; }

        public HolonModel(){}
        public HolonModel(Holon source){
            
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

            this.DimensionLevel = source.DimensionLevel;
            this.SubDimensionLevel = source.SubDimensionLevel;

            this.CreatedProviderType = source.CreatedProviderType.Value;
            this.CreatedByAvatarId = source.CreatedByAvatarId.ToString();
            this.CreatedDate = source.CreatedDate;

            this.ModifiedByAvatarId = source.ModifiedByAvatarId.ToString();
            this.ModifiedDate = source.ModifiedDate;

            this.DeletedByAvatarId = source.DeletedByAvatarId.ToString();
            this.DeletedDate = source.DeletedDate;

            this.Version = source.Version;


            if(source.ParentDimension != null){
                source.ParentDimension.ParentHolonId = source.Id;
                this.ParentDimension = new DimensionModel(source.ParentDimension);
            }

            if(source.ParentStar != null){
                source.ParentStar.ParentHolonId = source.Id;
                this.ParentStar = new StarModel(source.ParentStar);
            }

            if(source.ParentSuperStar != null){
                source.ParentSuperStar.ParentHolonId = source.Id;
                this.ParentSuperStar = new SuperStarModel(source.ParentSuperStar);
            }

            if(source.ParentGrandSuperStar != null){
                source.ParentGrandSuperStar.ParentHolonId = source.Id;
                this.ParentGrandSuperStar = new GrandSuperStarModel(source.ParentGrandSuperStar);
            }

            if(source.ParentGreatGrandSuperStar != null){
                source.ParentGreatGrandSuperStar.ParentHolonId = source.Id;
                this.ParentGreatGrandSuperStar = new GreatGrandSuperStarModel(source.ParentGreatGrandSuperStar);
            }

            if(source.ParentMoon != null){
                source.ParentMoon.ParentHolonId = source.Id;
                this.ParentMoon = new MoonModel(source.ParentMoon);
            }

            if(source.ParentPlanet != null){
                source.ParentPlanet.ParentHolonId = source.Id;
                this.ParentPlanet = new PlanetModel(source.ParentPlanet);
            }

            if(source.ParentSolarSystem != null){
                source.ParentSolarSystem.ParentHolonId = source.Id;
                this.ParentSolarSystem = new SolarSystemModel(source.ParentSolarSystem);
            }

            if(source.ParentGalaxy != null){
                source.ParentGalaxy.ParentHolonId = source.Id;
                this.ParentGalaxy = new GalaxyModel(source.ParentGalaxy);
            }

            if(source.ParentGalaxyCluster != null){
                source.ParentGalaxyCluster.ParentHolonId = source.Id;
                this.ParentGalaxyCluster = new GalaxyClusterModel(source.ParentGalaxyCluster);
            }

            if(source.ParentUniverse != null){
                source.ParentUniverse.ParentHolonId = source.Id;
                this.ParentUniverse = new UniverseModel(source.ParentUniverse);
            }

            foreach(KeyValuePair<ProviderType, string> item in source.ProviderKey){

                ProviderKeyModel providerKey=new ProviderKeyModel(item.Key,item.Value);
                providerKey.ParentId=this.Id;
                this.ProviderKey.Add(providerKey);
            }

            foreach(KeyValuePair<string, string> item in source.MetaData){

                MetaDataModel metaModel=new MetaDataModel(item.Key,item.Value);
                metaModel.ParentId=this.Id;
                this.MetaData.Add(metaModel);
            }

            Dictionary<string,string> providerMeta = source.ProviderMetaData.GetValueOrDefault(ProviderType.SQLLiteDBOASIS);
            if(providerMeta != null){

                foreach(KeyValuePair<string, string> item in providerMeta){

                    ProviderMetaData metaModel=new ProviderMetaData(ProviderType.SQLLiteDBOASIS, item.Key,item.Value);
                    metaModel.ParentId=this.Id;
                    this.ProviderMetaData.Add(metaModel);
                }
            }

        }

        public Holon GetHolon(){
            Holon item=new Holon();

            item.Id = Guid.Parse(this.Id);

            item.ParentHolonId = Guid.Parse(this.ParentHolonId);
            item.Name = this.Name;
            item.Description = this.Description;
            item.HolonType = this.HolonType;

            item.IsChanged = this.IsChanged;
            item.IsNewHolon = this.IsNewHolon;
            item.IsActive = this.IsActive;

            item.DimensionLevel = this.DimensionLevel;
            item.SubDimensionLevel = this.SubDimensionLevel;

            item.CreatedProviderType = new EnumValue<ProviderType>(this.CreatedProviderType);
            item.CreatedByAvatarId = Guid.Parse(this.CreatedByAvatarId);
            item.CreatedDate = this.CreatedDate;

            item.ModifiedByAvatarId = Guid.Parse(this.ModifiedByAvatarId);
            item.ModifiedDate = this.ModifiedDate;

            item.DeletedByAvatarId = Guid.Parse(this.DeletedByAvatarId);
            item.DeletedDate = this.DeletedDate;

            item.Version = this.Version;


            item.ParentDimension = this.ParentDimension.GetDimension();
            item.ParentStar = this.ParentStar.GetStar();
            item.ParentSuperStar = this.ParentSuperStar.GetSuperStar();
            item.ParentGrandSuperStar = this.ParentGrandSuperStar.GetGrandSuperStar();
            item.ParentGreatGrandSuperStar = this.ParentGreatGrandSuperStar.GetGreatGrandSuperStar();

            item.ParentPlanet = this.ParentPlanet.GetPlanet();
            item.ParentMoon = this.ParentMoon.GetMoon();
            item.ParentSolarSystem = this.ParentSolarSystem.GetSolarSystem();

            item.ParentGalaxy = this.ParentGalaxy.GetGalaxy();
            item.ParentGalaxyCluster = this.ParentGalaxyCluster.GetGalaxyCluster();
            item.ParentUniverse = this.ParentUniverse.GetUniverse();


            foreach(ProviderKeyModel model in this.ProviderKey){

                ProviderKeyAbstract providerKey=model.GetProviderKey();
                item.ProviderKey.Add(providerKey.ProviderId, providerKey.Value);
            }

            foreach(MetaDataModel model in this.MetaData){

                MetaDataModel metaData=model.GetMetaData();
                item.MetaData.Add(metaData.PropertyId, metaData.Value);
            }

            Dictionary<string,string> providerMetaData = new Dictionary<string, string>();
            foreach(ProviderMetaData providerMeta in this.ProviderMetaData){

                ProviderMetaData metaData=providerMeta.GetMetaData();
                providerMetaData.Add(metaData.Property, metaData.Value);
            }

            item.ProviderMetaData.Add(ProviderType.SQLLiteDBOASIS, providerMetaData);

            return(item);
        }
    }
}