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

            this.Title=source.Title;
            this.FirstName=source.FirstName;
            this.LastName=source.LastName;
            this.Username=source.Username;
            this.Password=source.Password;
            this.Email=source.Email;
            this.Image2D=source.Image2D;
            
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

            this.Stats.AvatarId=this.Id;

            this.Aura.AvatarId=this.Id;

            this.HumanDesign.AvatarId=this.Id;

            this.Skills.AvatarId=this.Id;

            this.Attributes.AvatarId=this.Id;

            this.SuperPowers.AvatarId=this.Id;

            foreach(RefreshToken token in source.RefreshTokens){

                RefreshTokenModel tokenModel=new RefreshTokenModel(token);
                //tokenModel.Avatar=source;
                tokenModel.AvatarId=this.Id;
                this.RefreshTokens.Add(tokenModel);
            }
        }

        public Avatar GetAvatar(){

            Avatar item=new Avatar();

            item.Id= new Guid(this.Id);
            item.Title=this.Title;
            item.FirstName=this.FirstName;
            item.LastName=this.LastName;
            
            item.Username=this.Username;
            item.Password=this.Password;
            item.Email=this.Email;
            item.Image2D=this.Image2D;

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

            foreach(RefreshTokenModel model in this.RefreshTokens){

                item.RefreshTokens.Add(model.GetRefreshToken());
            }

            foreach(ProviderKeyModel model in this.ProviderKey){

                ProviderKeyAbstract providerKey=model.GetProviderKey();
                item.ProviderKey.Add(providerKey.ProviderId, providerKey.Value);
            }

            return(item);
        }
    }
}