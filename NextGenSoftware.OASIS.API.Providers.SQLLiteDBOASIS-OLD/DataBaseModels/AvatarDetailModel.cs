using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels
{

    [Table("AvatarDetail")]
    public class AvatarDetailModel {

        [Required, Key]
        public String Id{ set; get; }

        public string Name { get; set; }
        public string Description { get; set; }
        public HolonType HolonType { get; set; }

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
        public DateTime DOB { get; set; }
        public string Address { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string Postcode { get; set; }
        public string Mobile { get; set; }
        public string Landline { get; set; }

        public string Username { get; set; } 
        public string Password { get; set; }
        public string Email { get; set; }
        public string Image2D { get; set; }
        public string Model3D { get; set; }

        public ConsoleColor FavouriteColour { get; set; }
        public ConsoleColor STARCLIColour { get; set; }
        
        public AvatarType AvatarType { get; set; }
        public OASISType CreatedOASISType { get; set; }
        public ProviderType CreatedProviderType { get; set; }

        public int Karma { get; set; }
        public int XP { get; set; }
        public int Level{set; get;}

        public AvatarAttributesModel Attributes { get; set; }
        public AvatarAuraModel Aura { get; set; }
        public AvatarHumanDesignModel HumanDesign { get; set; }
        public AvatarSkillsModel Skills { get; set; }
        public AvatarStatsModel Stats { get; set; }
        public AvatarSuperPowersModel SuperPowers { get; set; }

        public List<AvatarChakraModel> AvatarChakras { get; set; } = new List<AvatarChakraModel>();
        public List<AvatarGiftModel> Gifts { get; set; } = new List<AvatarGiftModel>();
        public List<HeartRateEntryModel> HeartRates { get; set; } = new List<HeartRateEntryModel>();
        public List<InventoryItemModel> InventoryItems { set; get; } = new List<InventoryItemModel>();
        public List<GeneKeyModel> GeneKeys { get; set; } = new List<GeneKeyModel>();
        public List<SpellModel> Spells { get; set; } = new List<SpellModel>();
        public List<AchievementModel> Achievements { get; set; } = new List<AchievementModel>();
        public List<KarmaAkashicRecordModel> KarmaAkashicRecords { get; set; } = new List<KarmaAkashicRecordModel>();

        public List<ProviderKeyModel> ProviderKey { get; set; } = new List<ProviderKeyModel>();
        public List<ProviderPrivateKeyModel> ProviderPrivateKey { get; set; } = new List<ProviderPrivateKeyModel>();
        public List<ProviderPublicKeyModel> ProviderPublicKey { get; set; } = new List<ProviderPublicKeyModel>();
        public List<ProviderWalletAddressModel> ProviderWalletAddress { get; set; } = new List<ProviderWalletAddressModel>();

        public List<MetaDataModel> MetaData {set; get;} = new List<MetaDataModel>();

        public int Version { get; set; }
        public bool IsActive { get; set; }
        public bool IsChanged { get; set; }
        public bool IsNewHolon { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime DeletedDate { get; set; }

        public string CreatedByAvatarId { get; set; }
        public string ModifiedByAvatarId { get; set; }
        public string DeletedByAvatarId { get; set; }

        
        //public Dictionary<DimensionLevel, Guid> DimensionLevelIds { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        //public Dictionary<DimensionLevel, IHolon> DimensionLevels { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        
        //public IOmiverse Omiverse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }        
        //public string UmaJson { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        //public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        //public IHolon Original { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        
        public AvatarDetailModel(){}
        public AvatarDetailModel(IAvatarDetail source){

            if(source.Id == Guid.Empty){
                this.Id = Guid.NewGuid().ToString();
            }
            else{
                this.Id = source.Id.ToString();
            }
            
            this.Title=source.Title;
            this.FirstName=source.FirstName;
            this.LastName=source.LastName;

            this.Name = source.Name;
            this.Description = source.Description;
            this.HolonType = source.HolonType;

            this.DOB=source.DOB;
            this.Address=source.Address;
            this.Town=source.Town;
            this.County=source.County;
            this.Postcode=source.Postcode;
            this.Mobile=source.Mobile;
            this.Landline=source.Landline;

            this.Username=source.Username;
            this.Email=source.Email;
            this.Image2D=source.Image2D;
            this.Model3D=source.Model3D;

            this.FavouriteColour=source.FavouriteColour;
            this.STARCLIColour=source.STARCLIColour;
            
            this.AvatarType=source.AvatarType.Value;
            this.CreatedOASISType=source.CreatedOASISType.Value;
            this.CreatedProviderType = source.CreatedProviderType.Value;

            this.Karma=source.Karma;
            this.XP=source.XP;
            this.Level=source.Level;

            this.Version=source.Version;
            this.IsActive=source.IsActive;
            this.IsChanged = source.IsChanged;
            this.IsNewHolon = source.IsNewHolon;

            this.CreatedDate=source.CreatedDate;
            this.ModifiedDate=source.ModifiedDate;
            this.DeletedDate=source.DeletedDate;

            this.CreatedByAvatarId=source.CreatedByAvatarId.ToString();
            this.ModifiedByAvatarId=source.ModifiedByAvatarId.ToString();
            this.DeletedByAvatarId=source.DeletedByAvatarId.ToString();

            this.Stats=new AvatarStatsModel(source.Stats);
            this.Stats.AvatarId=this.Id;

            this.Aura=new AvatarAuraModel(source.Aura);
            this.Aura.AvatarId=this.Id;

            this.HumanDesign = new AvatarHumanDesignModel(source.HumanDesign);
            this.HumanDesign.AvatarId=this.Id;

            this.Skills=new AvatarSkillsModel(source.Skills);
            this.Skills.AvatarId=this.Id;

            this.Attributes=new AvatarAttributesModel(source.Attributes);
            this.Attributes.AvatarId=this.Id;

            this.SuperPowers=new AvatarSuperPowersModel(source.SuperPowers);
            this.SuperPowers.AvatarId=this.Id;

            foreach(AvatarGift gift in source.Gifts){
                this.Gifts.Add(new AvatarGiftModel(gift));
            }

            foreach(HeartRateEntry heartRate in source.HeartRateData){

                HeartRateEntryModel model=new HeartRateEntryModel(heartRate);
                model.AvatarId=this.Id;
                this.HeartRates.Add(model);
            }

            foreach(InventoryItem item in source.Inventory){

                InventoryItemModel model=new InventoryItemModel(item);
                model.AvatarId=this.Id;
                this.InventoryItems.Add(model);
            }

            foreach(GeneKey geneKey in source.GeneKeys){

                GeneKeyModel model=new GeneKeyModel(geneKey);
                model.AvatarId=this.Id;
                this.GeneKeys.Add(model);
            }

            foreach(Spell spell in source.Spells){

                SpellModel model=new SpellModel(spell);
                model.AvatarId=this.Id;
                this.Spells.Add(model);
            }

            foreach(Achievement item in source.Achievements){
                this.Achievements.Add(new AchievementModel(item));
            }

            foreach(KarmaAkashicRecord record in source.KarmaAkashicRecords){
                this.KarmaAkashicRecords.Add(new KarmaAkashicRecordModel(record));
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderKey){

                ProviderKeyModel model=new ProviderKeyModel(key.Key,key.Value);
                model.OwnerId=this.Id;
                this.ProviderKey.Add(model);
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderPrivateKey){

                ProviderPrivateKeyModel model=new ProviderPrivateKeyModel(key.Key,key.Value);
                model.OwnerId=this.Id;
                this.ProviderPrivateKey.Add(model);
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderPublicKey){

                ProviderPublicKeyModel model=new ProviderPublicKeyModel(key.Key,key.Value);
                model.OwnerId=this.Id;
                this.ProviderPublicKey.Add(model);
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderWalletAddress){

                ProviderWalletAddressModel model=new ProviderWalletAddressModel(key.Key,key.Value);
                model.OwnerId=this.Id;
                this.ProviderWalletAddress.Add(model);
            }

            foreach(KeyValuePair<string, string> item in source.MetaData){

                MetaDataModel metaModel=new MetaDataModel(item.Key,item.Value);
                metaModel.OwnerId=this.Id;
                this.MetaData.Add(metaModel);
            }

            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Root));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Sacral));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.SoloarPlexus));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Heart));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Throat));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.ThirdEye));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Crown));
        }

        public AvatarDetail GetAvatar(){

            AvatarDetail item=new AvatarDetail();

            item.Id = Guid.Parse(this.Id);
            item.Title=this.Title;
            item.FirstName=this.FirstName;
            item.LastName=this.LastName;

            item.Description = this.Description;
            item.HolonType = this.HolonType;

            item.DOB=this.DOB;
            item.Address=this.Address;
            item.Town=this.Town;
            item.County=this.County;
            item.Postcode=this.Postcode;
            item.Mobile=this.Mobile;
            item.Landline=this.Landline;

            item.Username=this.Username;
            item.Email=this.Email;
            item.Image2D=this.Image2D;
            item.Model3D=this.Model3D;

            item.FavouriteColour=this.FavouriteColour;
            item.STARCLIColour=this.STARCLIColour;
            
            item.AvatarType= new EnumValue<AvatarType>(this.AvatarType);
            item.CreatedOASISType= new EnumValue<OASISType>(this.CreatedOASISType);
            item.CreatedProviderType = new EnumValue<ProviderType>(this.CreatedProviderType);

            item.Karma=this.Karma;
            item.XP=this.XP;

            item.Version=this.Version;
            item.IsActive=this.IsActive;
            item.IsChanged = this.IsChanged;
            item.IsNewHolon = this.IsNewHolon;

            item.CreatedDate=this.CreatedDate;
            item.ModifiedDate=this.ModifiedDate;
            item.DeletedDate=this.DeletedDate;

            item.CreatedByAvatarId=Guid.Parse(this.CreatedByAvatarId);
            item.ModifiedByAvatarId=Guid.Parse(this.ModifiedByAvatarId);
            item.DeletedByAvatarId=Guid.Parse(this.DeletedByAvatarId);

            item.Attributes=this.Attributes.GetAvatarAttributes();
            item.Aura = this.Aura.GetAvatarAura();
            item.HumanDesign=this.HumanDesign.GetHumanDesign();
            item.Skills=this.Skills.GetAvatarSkills();
            item.Stats=this.Stats.GetAvatarStats();
            item.SuperPowers=this.SuperPowers.GetAvatarSuperPowers();

            foreach(HeartRateEntryModel model in this.HeartRates){

                item.HeartRateData.Add(model.GetHeartRateEntry());
            }

            foreach(InventoryItemModel model in this.InventoryItems){

                item.Inventory.Add(model.GetInventoryItem());
            }

            foreach(GeneKeyModel model in this.GeneKeys){

                item.GeneKeys.Add(model.GetGeneKey());
            }

            foreach(SpellModel model in this.Spells){

                item.Spells.Add(model.GetSpell());
            }

            foreach(AvatarGiftModel model in this.Gifts){

                item.Gifts.Add(model.GetAvatarGift());
            }

            foreach(AchievementModel model in this.Achievements){

                item.Achievements.Add(model.GetAchievement());
            }

            foreach(KarmaAkashicRecordModel model in this.KarmaAkashicRecords){

                item.KarmaAkashicRecords.Add(model.GetKarmaAkashicRecord());
            }

            foreach(ProviderKeyModel model in this.ProviderKey){

                item.ProviderKey.Add(model.ProviderId, model.Value);
            }

            foreach(ProviderPrivateKeyModel model in this.ProviderPrivateKey){

                item.ProviderPrivateKey.Add(model.ProviderId, model.Value);
            }

            foreach(ProviderPublicKeyModel model in this.ProviderPublicKey){

                item.ProviderPublicKey.Add(model.ProviderId, model.Value);
            }

            foreach(ProviderWalletAddressModel model in this.ProviderWalletAddress){

                item.ProviderWalletAddress.Add(model.ProviderId, model.Value);
            }

            foreach(MetaDataModel model in this.MetaData){

                item.MetaData.Add(model.Property, model.Value);
            }

            foreach (var chakra in this.AvatarChakras)
            {
                if(chakra.Type == ChakraType.Root){
                    item.Chakras.Root = (RootChakra)chakra.GetAvatarChakra();
                }
                if(chakra.Type == ChakraType.Crown){
                    item.Chakras.Crown = (CrownChakra)chakra.GetAvatarChakra();
                }
                if(chakra.Type == ChakraType.Heart){
                    item.Chakras.Heart = (HeartChakra)chakra.GetAvatarChakra();
                }
                if(chakra.Type == ChakraType.Sacral){
                    item.Chakras.Sacral = (SacralChakra)chakra.GetAvatarChakra();
                }
                if(chakra.Type == ChakraType.Throat){
                    item.Chakras.Throat = (ThroatChakra)chakra.GetAvatarChakra();
                }
                if(chakra.Type == ChakraType.SolarPlexus){
                    item.Chakras.SoloarPlexus = (SolarPlexusChakra)chakra.GetAvatarChakra();
                }
                if(chakra.Type == ChakraType.ThirdEye){
                    item.Chakras.ThirdEye = (ThirdEyeChakra)chakra.GetAvatarChakra();
                }
            }

            return(item);
        }
    }
}