//using System;
//using System.Linq;
//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Objects;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using Neo4jOgm.Attribute;

//namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

//    [NeoNodeEntity("AvatarModel", "Avatar")]

//    public class AvatarModel {

//        [NeoNodeId]
//        public long? Id { get; set; }

//        public string Title { get; set; }
//        public string AvatarId { get; set; }
//        public string FirstName { get; set; }
//        public string LastName { get; set; }
//        //public string FullName
//        //{
//        //    get
//        //    {
//        //        return string.Concat(Title, " ", FirstName, " ", LastName);
//        //    }
//        //}
//        public DateTime DOB { get; set; }
//        public string Address { get; set; }
//        public string Town { get; set; }
//        public string County { get; set; }
//        public string Country { get; set; }
//        public string Postcode { get; set; }
//        public string Mobile { get; set; }
//        public string Landline { get; set; }

//        public string Username { get; set; } 
//        public string Password { get; set; }
//        public string Email { get; set; }
//        public string Image2D { get; set; }
//        public string Model3D { get; set; }

//        public dynamic FavouriteColour { get; set ; }
//        public dynamic STARCLIColour { get; set; }
        
        
//        public dynamic AvatarType { get; set; }
//        public dynamic CreatedOASISType { get; set; }


//        public int Karma { get; set; }
//        public int XP { get; set; }
//        public int Level{set; get;}
//        public dynamic AcceptTerms { get; set; }
//        public string VerificationToken { get; set; }
//        public DateTime? Verified { get; set; }
//        public string ResetToken { get; set; }
//        public string JwtToken { get; set; }

//        public string RefreshToken { get; set; }
//        public DateTime? ResetTokenExpires { get; set; }
//        public DateTime? PasswordReset { get; set; }
//        //public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;

//        //public DateTime Created { get; set; }
//        //public DateTime? Updated { get; set; }
//        [NeoRelationship("Attributes_FOR")]

//        public AvatarAttributesModel Attributes { get; set; }
//        [NeoRelationship("Aura_FOR")]

//        public AvatarAuraModel Aura { get; set; }
//        [NeoRelationship("HumanDesign_FOR")]

//        public AvatarHumanDesignModel HumanDesign { get; set; }
//        [NeoRelationship("Skills_FOR")]

//        public AvatarSkillsModel Skills { get; set; }
//        [NeoRelationship("Stats_FOR")]

//        public AvatarStatsModel Stats { get; set; }
//        [NeoRelationship("SuperPowers_FOR")]

//        public AvatarSuperPowersModel SuperPowers { get; set; }
//        //[NeoRelationship("AvatarChakras_FOR")]

//        //public List<AvatarChakraModel> AvatarChakras { get; set; } = new List<AvatarChakraModel>();
//        [NeoRelationship("Gifts_FOR")]

//        public List<AvatarGiftModel> Gifts { get; set; } = new List<AvatarGiftModel>();
//        [NeoRelationship("HeartRates_FOR")]

//        public List<HeartRateEntryModel> HeartRates { get; set; } = new List<HeartRateEntryModel>();
//        [NeoRelationship("RefreshTokens_FOR")]

//        public List<RefreshTokenModel> RefreshTokens { get; set; } = new List<RefreshTokenModel>();
//        [NeoRelationship("InventoryItems_FOR")]

//        public List<InventoryItemModel> InventoryItems { set; get; } = new List<InventoryItemModel>();
//        [NeoRelationship("GeneKeys_FOR")]

//        public List<GeneKeyModel> GeneKeys { get; set; } = new List<GeneKeyModel>();
//        [NeoRelationship("Spells_FOR")]

//        public List<SpellModel> Spells { get; set; } = new List<SpellModel>();
//        [NeoRelationship("Achievements_FOR")]

//        public List<AchievementModel> Achievements { get; set; } = new List<AchievementModel>();
//        [NeoRelationship("KarmaAkashicRecords_FOR")]

//        public List<KarmaAkashicRecordModel> KarmaAkashicRecords { get; set; } = new List<KarmaAkashicRecordModel>();
//        [NeoRelationship("ProviderPrivateKey_FOR")]


//        public List<ProviderPrivateKeyModel> ProviderPrivateKey { get; set; } = new List<ProviderPrivateKeyModel>();
//        [NeoRelationship("ProviderPublicKey_FOR")]

//        public List<ProviderPublicKeyModel> ProviderPublicKey { get; set; } = new List<ProviderPublicKeyModel>();
//        [NeoRelationship("ProviderWalletAddress_FOR")]

//        public List<ProviderWalletAddressModel> ProviderWalletAddress { get; set; } = new List<ProviderWalletAddressModel>();


//        public AvatarModel(){}

//        public AvatarModel(Avatar source){
//            this.AvatarId = source.AvatarId.ToString();
//            this.Title=source.Title;
//            this.FirstName=source.FirstName;
//            this.LastName=source.LastName;

//            this.DOB=source.DOB;
//            this.Address=source.Address;
//            this.Town=source.Town;
//            this.County=source.County;
//            this.Country=source.Country;
//            this.Postcode=source.Postcode;
//            this.Mobile=source.Mobile;
//            this.Landline=source.Landline;

//            this.Username=source.Username;
//            this.Password=source.Password;
//            this.Email=source.Email;
//            this.Image2D=source.Image2D;
//            this.Model3D=source.Model3D;

//            this.FavouriteColour=source.FavouriteColour;
//            this.STARCLIColour=source.STARCLIColour;
            
//            this.AvatarType=source.AvatarType;
//            this.CreatedOASISType=source.CreatedOASISType;

//            this.Karma=source.Karma;
//            this.XP=source.XP;
//            this.Level=source.Level;
//            this.AcceptTerms=source.AcceptTerms;
//            this.VerificationToken=source.VerificationToken;
//            this.Verified=source.Verified;
//            this.ResetToken=source.ResetToken;
//            this.JwtToken=source.JwtToken;
//            this.RefreshToken=source.RefreshToken;
//            this.ResetTokenExpires=source.ResetTokenExpires;
//            this.PasswordReset=this.PasswordReset;

//            this.Stats=new AvatarStatsModel(source.Stats);
//            this.Stats.AvatarId= source.AvatarId.ToString();

//            this.Aura=new AvatarAuraModel(source.Aura);
//            this.Aura.AvatarId = source.AvatarId.ToString();

//            this.HumanDesign = new AvatarHumanDesignModel(source.HumanDesign);
//            this.HumanDesign.AvatarId = source.AvatarId.ToString();

//            this.Skills=new AvatarSkillsModel(source.Skills);
//            this.Skills.AvatarId=this.Id.ToString();

//            this.Attributes=new AvatarAttributesModel(source.Attributes);
//            this.Attributes.AvatarId=this.Id.ToString();

//            this.SuperPowers=new AvatarSuperPowersModel(source.SuperPowers);
//            this.SuperPowers.AvatarId= source.AvatarId.ToString();

//            foreach(AvatarGift gift in source.Gifts){
//                this.Gifts.Add(new AvatarGiftModel(gift));
//            }

//            foreach(HeartRateEntry heartRate in source.HeartRateData){

//                HeartRateEntryModel heartRateModel=new HeartRateEntryModel(heartRate);
//                heartRateModel.AvatarId = source.AvatarId.ToString();
//                this.HeartRates.Add(heartRateModel);
//            }

//            foreach(RefreshToken token in source.RefreshTokens){

//                RefreshTokenModel tokenModel=new RefreshTokenModel(token);
//                //tokenModel.Avatar=source;
//                tokenModel.AvatarId= source.AvatarId.ToString();
//                this.RefreshTokens.Add(tokenModel);
//            }

//            foreach(InventoryItem item in source.Inventory){

//                InventoryItemModel inventoryModel=new InventoryItemModel(item);
//                inventoryModel.AvatarId= source.AvatarId.ToString();
//                this.InventoryItems.Add(inventoryModel);
//            }

//            foreach(GeneKey geneKey in source.GeneKeys){

//                GeneKeyModel geneKeyModel=new GeneKeyModel(geneKey);
//                geneKeyModel.AvatarId= source.AvatarId.ToString();
//                this.GeneKeys.Add(geneKeyModel);
//            }

//            foreach(Spell spell in source.Spells){

//                SpellModel spellModel=new SpellModel(spell);
//                spellModel.AvatarId= source.AvatarId.ToString();
//                this.Spells.Add(spellModel);
//            }

//            foreach(Achievement item in source.Achievements){
//                this.Achievements.Add(new AchievementModel(item));
//            }

//            foreach(KarmaAkashicRecord record in source.KarmaAkashicRecords){
//                this.KarmaAkashicRecords.Add(new KarmaAkashicRecordModel(record));
//            }

//            foreach(KeyValuePair<ProviderType, string> key in source.ProviderPrivateKey){

//                ProviderPrivateKeyModel privateKey=new ProviderPrivateKeyModel(key);
//                privateKey.AvatarId= source.AvatarId.ToString();
//                this.ProviderPrivateKey.Add(privateKey);
//            }

//            foreach(KeyValuePair<ProviderType, string> key in source.ProviderPublicKey){

//                ProviderPublicKeyModel publicKey=new ProviderPublicKeyModel(key);
//                publicKey.AvatarId= source.AvatarId.ToString();
//                this.ProviderPublicKey.Add(publicKey);
//            }

//            foreach(KeyValuePair<ProviderType, string> key in source.ProviderWalletAddress){

//                ProviderWalletAddressModel walletAddressModel=new ProviderWalletAddressModel(key);
//                walletAddressModel.AvatarId= source.AvatarId.ToString();
//                this.ProviderWalletAddress.Add(walletAddressModel);
//            }

//            //this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Root));
//            //this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Sacral));
//            //this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.SoloarPlexus));
//            //this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Heart));
//            //this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Throat));
//            //this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.ThirdEye));
//            //this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Crown));
//        }

//        public Avatar GetAvatar(){

//            Avatar item=new Avatar();

//            item.Id= new Guid();
//            item.Title=this.Title;
//            item.FirstName=this.FirstName;
//            item.LastName=this.LastName;

//            item.DOB=this.DOB;
//            item.Address=this.Address;
//            item.Town=this.Town;
//            item.County=this.County;
//            item.Country=this.Country;
//            item.Postcode=this.Postcode;
//            item.Mobile=this.Mobile;
//            item.Landline=this.Landline;
                
//            item.Username=this.Username;
//            item.Password=this.Password;
//            item.Email=this.Email;
//            item.Image2D=this.Image2D;
//            item.Model3D=this.Model3D;

//            item.FavouriteColour= Enum.Parse<ConsoleColor>(this.FavouriteColour)  ;
//            item.STARCLIColour= Enum.Parse<ConsoleColor> (this.STARCLIColour)    ;

//            item.AvatarType = this.AvatarType == null ? this.AvatarType :  AvatarType.Wizard;
//            //item.AvatarType =  Enum.Parse<AvatarType>(this.AvatarType == null ?  : this.AvatarType); ;

//            //item.CreatedOASISType = Enum.Parse<OASISType>(this.CreatedOASISType);

//            item.Karma=this.Karma;  
//            item.XP=this.XP;
//            item.AcceptTerms= Convert.ToBoolean( this.AcceptTerms);
//            item.VerificationToken=this.VerificationToken;
//            item.Verified=this.Verified;
//            item.ResetToken=this.ResetToken;
//            item.JwtToken=this.JwtToken;
//            item.RefreshToken=this.RefreshToken;
//            item.ResetTokenExpires=this.ResetTokenExpires;
//            item.PasswordReset=this.PasswordReset;

//            AvatarAttributesModel attributesModel=this.Attributes;
//            item.Attributes=attributesModel.GetAvatarAttributes();

//            AvatarAuraModel auraModel = this.Aura;
//            item.Aura = auraModel.GetAvatarAura();

//            AvatarHumanDesignModel humanDesignModel = this.HumanDesign;
//            item.HumanDesign=humanDesignModel.GetHumanDesign();

//            AvatarSkillsModel skillsModel=this.Skills;
//            item.Skills=skillsModel.GetAvatarSkills();

//            AvatarStatsModel statsModel = this.Stats;
//            item.Stats=statsModel.GetAvatarStats();

//            AvatarSuperPowersModel powersModel=this.SuperPowers;
//            item.SuperPowers=powersModel.GetAvatarSuperPowers();


//            List<HeartRateEntryModel> rateEntryModels=this.HeartRates;
//            foreach(HeartRateEntryModel model in rateEntryModels){

//                item.HeartRateData.Add(model.GetHeartRateEntry());
//            }

//            List<RefreshTokenModel> tokenModels=this.RefreshTokens;
//            foreach(RefreshTokenModel model in tokenModels){

//                item.RefreshTokens.Add(model.GetRefreshToken());
//            }

//            List<InventoryItemModel> inventoryModels=this.InventoryItems;
//            foreach(InventoryItemModel model in inventoryModels){

//                item.Inventory.Add(model.GetInventoryItem());
//            }

//            List<GeneKeyModel> geneKeyModels=this.GeneKeys;
//            foreach(GeneKeyModel model in geneKeyModels){

//                item.GeneKeys.Add(model.GetGeneKey());
//            }

//            List<SpellModel> spellModels=this.Spells;
//            foreach(SpellModel model in spellModels){

//                item.Spells.Add(model.GetSpell());
//            }

//            List<AchievementModel> achievementModels=this.Achievements;
//            foreach(AchievementModel model in achievementModels){

//                item.Achievements.Add(model.GetAchievement());
//            }

//            List<KarmaAkashicRecordModel> akashicRecordModels=this.KarmaAkashicRecords;
//            foreach(KarmaAkashicRecordModel model in akashicRecordModels){

//                item.KarmaAkashicRecords.Add(model.GetKarmaAkashicRecord());
//            }

//            List<ProviderPrivateKeyModel> privateKeyModels=this.ProviderPrivateKey;
//            foreach(ProviderPrivateKeyModel model in privateKeyModels){

//                ProviderKeyAbstract providerKey=model.GetProviderKey();
//                item.ProviderPrivateKey.Add((ProviderType)providerKey.KeyId, providerKey.Value);
//            }

//            List<ProviderPublicKeyModel> publicKeyModels=this.ProviderPublicKey;
//            foreach(ProviderPublicKeyModel model in publicKeyModels){

//                ProviderKeyAbstract providerKey=model.GetProviderKey();
//                item.ProviderPublicKey.Add((ProviderType)providerKey.KeyId, providerKey.Value);
//            }

//            List<ProviderWalletAddressModel> walletAddressModels=this.ProviderWalletAddress;
//            foreach(ProviderWalletAddressModel model in walletAddressModels){

//                ProviderKeyAbstract providerKey=model.GetProviderKey();
//                item.ProviderWalletAddress.Add((ProviderType)providerKey.KeyId, providerKey.Value);
//            }

//            return(item);
//        }
//    }
//}