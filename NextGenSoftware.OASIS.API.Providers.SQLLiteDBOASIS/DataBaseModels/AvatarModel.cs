using System;
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
        
        
        public AvatarType AvatarType { get; set; }
        public OASISType CreatedOASISType { get; set; }


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

        public List<ProviderKeyModel> ProviderKey { get; set; } = new List<ProviderKeyModel>();

        public List<ProviderPrivateKeyModel> ProviderPrivateKey { get; set; } = new List<ProviderPrivateKeyModel>();
        public List<ProviderPublicKeyModel> ProviderPublicKey { get; set; } = new List<ProviderPublicKeyModel>();
        public List<ProviderWalletAddressModel> ProviderWalletAddress { get; set; } = new List<ProviderWalletAddressModel>();

        public int Version { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        
        public DateTime DeletedDate { get; set; }
        public string DeletedByAvatarId { get; set; }

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
            
            this.AvatarType=source.AvatarType.Value;
            this.CreatedOASISType=source.CreatedOASISType.Value;

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
            this.PasswordReset=source.PasswordReset;

            this.Version=source.Version;
            this.IsActive=source.IsActive;

            this.CreatedDate=source.CreatedDate;
            this.ModifiedDate=source.ModifiedDate;

            this.DeletedDate=source.DeletedDate;
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

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderKey){

                ProviderKeyModel providerKey=new ProviderKeyModel(key.Key,key.Value);
                providerKey.ParentId=this.Id;
                this.ProviderKey.Add(providerKey);
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderPrivateKey){

                ProviderPrivateKeyModel privateKey=new ProviderPrivateKeyModel(key.Key,key.Value);
                privateKey.ParentId=this.Id;
                this.ProviderPrivateKey.Add(privateKey);
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderPublicKey){

                ProviderPublicKeyModel publicKey=new ProviderPublicKeyModel(key.Key,key.Value);
                publicKey.ParentId=this.Id;
                this.ProviderPublicKey.Add(publicKey);
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderWalletAddress){

                ProviderWalletAddressModel walletAddressModel=new ProviderWalletAddressModel(key.Key,key.Value);
                walletAddressModel.ParentId=this.Id;
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

            item.AvatarType = new EnumValue<AvatarType>(this.AvatarType);
            item.CreatedOASISType= new EnumValue<OASISType>(this.CreatedOASISType);

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

            item.Version=this.Version;
            item.IsActive=this.IsActive;

            item.CreatedDate=this.CreatedDate;
            item.ModifiedDate=this.ModifiedDate;

            item.DeletedDate=this.DeletedDate;
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
            
            foreach(RefreshTokenModel model in this.RefreshTokens){

                item.RefreshTokens.Add(model.GetRefreshToken());
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

            foreach(AchievementModel model in this.Achievements){

                item.Achievements.Add(model.GetAchievement());
            }

            foreach(KarmaAkashicRecordModel model in this.KarmaAkashicRecords){

                item.KarmaAkashicRecords.Add(model.GetKarmaAkashicRecord());
            }

            foreach(ProviderKeyModel model in this.ProviderKey){

                ProviderKeyAbstract providerKey=model.GetProviderKey();
                item.ProviderKey.Add(providerKey.ProviderId, providerKey.Value);
            }

            foreach(ProviderPrivateKeyModel model in this.ProviderPrivateKey){

                ProviderKeyAbstract providerKey=model.GetProviderKey();
                item.ProviderPrivateKey.Add(providerKey.ProviderId, providerKey.Value);
            }

            foreach(ProviderPublicKeyModel model in this.ProviderPublicKey){

                ProviderKeyAbstract providerKey=model.GetProviderKey();
                item.ProviderPublicKey.Add(providerKey.ProviderId, providerKey.Value);
            }

            foreach(ProviderWalletAddressModel model in this.ProviderWalletAddress){

                ProviderKeyAbstract providerKey=model.GetProviderKey();
                item.ProviderWalletAddress.Add(providerKey.ProviderId, providerKey.Value);
            }

            return(item);
        }
    }
}