using System;
using System.Linq;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("Avatar")]
    public class AvatarModel {

        public String Id{ set; get; }

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
        
        
        public string AvatarType { get; set; }
        public string CreatedOASISType { get; set; }


        public int Karma { get; set; }
        public int XP { get; set; }
        public int Level{set; get;}
        public bool AcceptTerms { get; set; }
        public string VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public string ResetToken { get; set; }
        public string JwtToken { get; set; }

        public string RefreshToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        
        //public DateTime Created { get; set; }
        //public DateTime? Updated { get; set; }

        public AvatarAttributesModel Attributes { get; set; }
        public AvatarAuraModel Aura { get; set; }
        public AvatarHumanDesignModel HumanDesign { get; set; }
        public AvatarSkillsModel Skills { get; set; }
        public AvatarStatsModel Stats { get; set; }
        public AvatarSuperPowersModel SuperPowers { get; set; }

        public List<AvatarChakraModel> AvatarChakras { get; set; } = new List<AvatarChakraModel>();
        public List<AvatarGiftModel> Gifts { get; set; } = new List<AvatarGiftModel>();
        public List<HeartRateEntryModel> HeartRates { get; set; } = new List<HeartRateEntryModel>();
        public List<RefreshTokenModel> RefreshTokens { get; set; } = new List<RefreshTokenModel>();
        public List<InventoryItemModel> InventoryItems { set; get; } = new List<InventoryItemModel>();
        public List<GeneKeyModel> GeneKeys { get; set; } = new List<GeneKeyModel>();
        public List<SpellModel> Spells { get; set; } = new List<SpellModel>();
        public List<AchievementModel> Achievements { get; set; } = new List<AchievementModel>();
        public List<KarmaAkashicRecordModel> KarmaAkashicRecords { get; set; } = new List<KarmaAkashicRecordModel>();

        public List<ProviderPrivateKeyModel> ProviderPrivateKey { get; set; } = new List<ProviderPrivateKeyModel>();
        public List<ProviderPublicKeyModel> ProviderPublicKey { get; set; } = new List<ProviderPublicKeyModel>();
        public List<ProviderWalletAddressModel> ProviderWalletAddress { get; set; } = new List<ProviderWalletAddressModel>();

        public AvatarModel(){}
        public AvatarModel(Avatar source){

            this.Id=source.AvatarId.ToString();
            this.Title=source.Title;
            this.FirstName=source.FirstName;
            this.LastName=source.LastName;

            this.DOB=source.DOB;
            this.Address=source.Address;
            this.Town=source.Town;
            this.County=source.County;
            this.Country=source.Country;
            this.Postcode=source.Postcode;
            this.Mobile=source.Mobile;
            this.Landline=source.Landline;

            this.Username=source.Username;
            this.Password=source.Password;
            this.Email=source.Email;
            this.Image2D=source.Image2D;
            this.Model3D=source.Model3D;

            this.FavouriteColour=source.FavouriteColour;
            this.STARCLIColour=source.STARCLIColour;
            
            this.AvatarType=source.AvatarType.Name;
            this.CreatedOASISType=source.CreatedOASISType.Name;

            this.Karma=source.Karma;
            this.XP=source.XP;
            this.Level=source.Level;
            this.AcceptTerms=source.AcceptTerms;
            this.VerificationToken=source.VerificationToken;
            this.Verified=source.Verified;
            this.ResetToken=source.ResetToken;
            this.JwtToken=source.JwtToken;
            this.RefreshToken=source.RefreshToken;
            this.ResetTokenExpires=source.ResetTokenExpires;
            this.PasswordReset=this.PasswordReset;

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

                HeartRateEntryModel heartRateModel=new HeartRateEntryModel(heartRate);
                heartRateModel.AvatarId=this.Id;
                this.HeartRates.Add(heartRateModel);
            }

            foreach(RefreshToken token in source.RefreshTokens){

                RefreshTokenModel tokenModel=new RefreshTokenModel(token);
                //tokenModel.Avatar=source;
                tokenModel.AvatarId=this.Id;
                this.RefreshTokens.Add(tokenModel);
            }

            foreach(InventoryItem item in source.Inventory){

                InventoryItemModel inventoryModel=new InventoryItemModel(item);
                inventoryModel.AvatarId=this.Id;
                this.InventoryItems.Add(inventoryModel);
            }

            foreach(GeneKey geneKey in source.GeneKeys){

                GeneKeyModel geneKeyModel=new GeneKeyModel(geneKey);
                geneKeyModel.AvatarId=this.Id;
                this.GeneKeys.Add(geneKeyModel);
            }

            foreach(Spell spell in source.Spells){

                SpellModel spellModel=new SpellModel(spell);
                spellModel.AvatarId=this.Id;
                this.Spells.Add(spellModel);
            }

            foreach(Achievement item in source.Achievements){
                this.Achievements.Add(new AchievementModel(item));
            }

            foreach(KarmaAkashicRecord record in source.KarmaAkashicRecords){
                this.KarmaAkashicRecords.Add(new KarmaAkashicRecordModel(record));
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderPrivateKey){

                ProviderPrivateKeyModel privateKey=new ProviderPrivateKeyModel(key);
                privateKey.AvatarId=this.Id;
                this.ProviderPrivateKey.Add(privateKey);
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderPublicKey){

                ProviderPublicKeyModel publicKey=new ProviderPublicKeyModel(key);
                publicKey.AvatarId=this.Id;
                this.ProviderPublicKey.Add(publicKey);
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderWalletAddress){

                ProviderWalletAddressModel walletAddressModel=new ProviderWalletAddressModel(key);
                walletAddressModel.AvatarId=this.Id;
                this.ProviderWalletAddress.Add(walletAddressModel);
            }

            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Root));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Sacral));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.SoloarPlexus));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Heart));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Throat));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.ThirdEye));
            this.AvatarChakras.Add(new AvatarChakraModel(source.Chakras.Crown));
        }

        public Avatar GetAvatar(){

            Avatar item=new Avatar();

            item.Id= new Guid(this.Id);
            item.Title=this.Title;
            item.FirstName=this.FirstName;
            item.LastName=this.LastName;

            item.DOB=this.DOB;
            item.Address=this.Address;
            item.Town=this.Town;
            item.County=this.County;
            item.Country=this.Country;
            item.Postcode=this.Postcode;
            item.Mobile=this.Mobile;
            item.Landline=this.Landline;

            item.Username=this.Username;
            item.Password=this.Password;
            item.Email=this.Email;
            item.Image2D=this.Image2D;
            item.Model3D=this.Model3D;

            item.FavouriteColour=this.FavouriteColour;
            item.STARCLIColour=this.STARCLIColour;

            AvatarType avatarType=Enum.Parse<AvatarType>(this.AvatarType);
            item.AvatarType= new EnumValue<AvatarType>(avatarType);

            OASISType oasisType=Enum.Parse<OASISType>(this.CreatedOASISType);
            item.CreatedOASISType= new EnumValue<OASISType>(oasisType);

            item.Karma=this.Karma;
            item.XP=this.XP;
            item.AcceptTerms=this.AcceptTerms;
            item.VerificationToken=this.VerificationToken;
            item.Verified=this.Verified;
            item.ResetToken=this.ResetToken;
            item.JwtToken=this.JwtToken;
            item.RefreshToken=this.RefreshToken;
            item.ResetTokenExpires=this.ResetTokenExpires;
            item.PasswordReset=this.PasswordReset;

            AvatarAttributesModel attributesModel=this.Attributes;
            item.Attributes=attributesModel.GetAvatarAttributes();

            AvatarAuraModel auraModel = this.Aura;
            item.Aura = auraModel.GetAvatarAura();

            AvatarHumanDesignModel humanDesignModel = this.HumanDesign;
            item.HumanDesign=humanDesignModel.GetHumanDesign();

            AvatarSkillsModel skillsModel=this.Skills;
            item.Skills=skillsModel.GetAvatarSkills();

            AvatarStatsModel statsModel = this.Stats;
            item.Stats=statsModel.GetAvatarStats();

            AvatarSuperPowersModel powersModel=this.SuperPowers;
            item.SuperPowers=powersModel.GetAvatarSuperPowers();


            List<HeartRateEntryModel> rateEntryModels=this.HeartRates;
            foreach(HeartRateEntryModel model in rateEntryModels){

                item.HeartRateData.Add(model.GetHeartRateEntry());
            }

            List<RefreshTokenModel> tokenModels=this.RefreshTokens;
            foreach(RefreshTokenModel model in tokenModels){

                item.RefreshTokens.Add(model.GetRefreshToken());
            }

            List<InventoryItemModel> inventoryModels=this.InventoryItems;
            foreach(InventoryItemModel model in inventoryModels){

                item.Inventory.Add(model.GetInventoryItem());
            }

            List<GeneKeyModel> geneKeyModels=this.GeneKeys;
            foreach(GeneKeyModel model in geneKeyModels){

                item.GeneKeys.Add(model.GetGeneKey());
            }

            List<SpellModel> spellModels=this.Spells;
            foreach(SpellModel model in spellModels){

                item.Spells.Add(model.GetSpell());
            }

            List<AchievementModel> achievementModels=this.Achievements;
            foreach(AchievementModel model in achievementModels){

                item.Achievements.Add(model.GetAchievement());
            }

            List<KarmaAkashicRecordModel> akashicRecordModels=this.KarmaAkashicRecords;
            foreach(KarmaAkashicRecordModel model in akashicRecordModels){

                item.KarmaAkashicRecords.Add(model.GetKarmaAkashicRecord());
            }

            List<ProviderPrivateKeyModel> privateKeyModels=this.ProviderPrivateKey;
            foreach(ProviderPrivateKeyModel model in privateKeyModels){

                ProviderKeyAbstract providerKey=model.GetProviderKey();
                item.ProviderPrivateKey.Add((ProviderType)providerKey.KeyId, providerKey.Value);
            }

            List<ProviderPublicKeyModel> publicKeyModels=this.ProviderPublicKey;
            foreach(ProviderPublicKeyModel model in publicKeyModels){

                ProviderKeyAbstract providerKey=model.GetProviderKey();
                item.ProviderPublicKey.Add((ProviderType)providerKey.KeyId, providerKey.Value);
            }

            List<ProviderWalletAddressModel> walletAddressModels=this.ProviderWalletAddress;
            foreach(ProviderWalletAddressModel model in walletAddressModels){

                ProviderKeyAbstract providerKey=model.GetProviderKey();
                item.ProviderWalletAddress.Add((ProviderType)providerKey.KeyId, providerKey.Value);
            }

            return(item);
        }
    }
}