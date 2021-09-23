using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("Avatar")]
    public class AvatarModel {

        [Required, Key]
        public string Id { get; set;}

        public AvatarType AvatarType { get; set;}
        public HolonType HolonType { get; set;}

        public string Name { get; set;}
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set;}
        public string FullName {set; get;}
        public string Description { get; set;}

        public string Username { get; set;}
        public string Email { get; set;}
        public string Password { get; set; }

        public bool AcceptTerms { get; set;}
        public bool IsVerified{ set; get;}
        public string JwtToken { get; set;}
        public string RefreshToken { get; set;}

        public DateTime? PasswordReset { get; set;}
        public string ResetToken { get; set;}
        public DateTime? ResetTokenExpires { get; set; }
        public string VerificationToken { get; set;}
        public DateTime? Verified { get; set;}
        public string Image2D { get; set;}

        public int Karma { get; set;}
        public int Level{set; get;}
        public int XP { get; set;}

        public ProviderType CreatedProviderType { get; set;}
        public OASISType CreatedOASISType { get; set;}

        public int Version { get; set;}
        public bool IsActive { get; set;}
        public bool IsChanged { get; set;}
        public bool IsNewHolon { get; set;}

        public DateTime CreatedDate { get; set;}
        public DateTime ModifiedDate { get; set;}
        public DateTime DeletedDate { get; set;}

        public String CreatedByAvatarId { get; set;}
        public String ModifiedByAvatarId { get; set;}
        public String DeletedByAvatarId { get; set;}


        public List<RefreshTokenModel> RefreshTokens { get; set;}
        public List<ProviderKeyModel> ProviderKey { get; set;}
        public List<MetaDataModel> MetaData { get; set;}

        public AvatarModel(){}
        public AvatarModel(Avatar source){

<<<<<<< HEAD
            if(source.Id == Guid.Empty){
                this.Id = Guid.NewGuid().ToString();
            }
            else{
                this.Id = source.Id.ToString();
            }
            
            this.Title=source.Title;
            this.FirstName=source.FirstName;
            this.LastName=source.LastName;
            this.FullName = source.FullName;

            this.Name = source.Name;
            this.Description = source.Description;
            this.HolonType = source.HolonType;
            this.AvatarType = source.AvatarType.Value;

=======
            this.Title=source.Title;
            this.FirstName=source.FirstName;
            this.LastName=source.LastName;
>>>>>>> bf93d6d737f7b0f215fcd0016d43a34097fe193e
            this.Username=source.Username;
            this.Email=source.Email;
            this.Image2D=source.Image2D;
            
            this.AcceptTerms = source.AcceptTerms;
            this.IsVerified = source.IsVerified;
            this.JwtToken = source.JwtToken;
            this.RefreshToken = source.RefreshToken;

            this.PasswordReset = source.PasswordReset;
            this.ResetToken = source.ResetToken;
            this.ResetTokenExpires = source.ResetTokenExpires;
            this.VerificationToken = source.VerificationToken;
            this.Verified = source.Verified;


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
<<<<<<< HEAD

            this.CreatedByAvatarId=source.CreatedByAvatarId.ToString();
            this.ModifiedByAvatarId=source.ModifiedByAvatarId.ToString();
            this.DeletedByAvatarId=source.DeletedByAvatarId.ToString();

            foreach(RefreshToken refreshToken in source.RefreshTokens){

                RefreshTokenModel model=new RefreshTokenModel(refreshToken);
                model.AvatarId=this.Id;
                this.RefreshTokens.Add(model);
            }

            foreach(KeyValuePair<ProviderType, string> key in source.ProviderKey){

                ProviderKeyModel model=new ProviderKeyModel(key.Key,key.Value);
                model.OwnerId=this.Id;
                this.ProviderKey.Add(model);
            }

            foreach(KeyValuePair<string, string> item in source.MetaData){

                MetaDataModel metaModel=new MetaDataModel(item.Key,item.Value);
                metaModel.OwnerId=this.Id;
                this.MetaData.Add(metaModel);
            }

=======
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
>>>>>>> bf93d6d737f7b0f215fcd0016d43a34097fe193e
        }

        public Avatar GetAvatar(){

            Avatar item = new Avatar();
            
            item.Id=Guid.Parse(this.Id);
            item.Title=this.Title;
            item.FirstName=this.FirstName;
            item.LastName=this.LastName;
<<<<<<< HEAD

            item.Description = this.Description;
            item.HolonType = this.HolonType;
            item.AvatarType = new EnumValue<AvatarType>(this.AvatarType);

=======
            
>>>>>>> bf93d6d737f7b0f215fcd0016d43a34097fe193e
            item.Username=this.Username;
            item.Email=this.Email;
            item.Image2D=this.Image2D;
<<<<<<< HEAD
            
            item.AcceptTerms = this.AcceptTerms;
            item.JwtToken = this.JwtToken;
            item.RefreshToken = this.RefreshToken;

            item.PasswordReset = this.PasswordReset;
            item.ResetToken = this.ResetToken;
            item.ResetTokenExpires = this.ResetTokenExpires;
            item.VerificationToken = this.VerificationToken;
            item.Verified = this.Verified;
=======
>>>>>>> bf93d6d737f7b0f215fcd0016d43a34097fe193e

            
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

<<<<<<< HEAD
            item.CreatedByAvatarId=Guid.Parse(this.CreatedByAvatarId);
            item.ModifiedByAvatarId=Guid.Parse(this.ModifiedByAvatarId);
            item.DeletedByAvatarId=Guid.Parse(this.DeletedByAvatarId);

=======
>>>>>>> bf93d6d737f7b0f215fcd0016d43a34097fe193e
            foreach(RefreshTokenModel model in this.RefreshTokens){

                item.RefreshTokens.Add(model.GetRefreshToken());
            }

            foreach(ProviderKeyModel model in this.ProviderKey){

<<<<<<< HEAD
                item.ProviderKey.Add(model.ProviderId, model.Value);
            }

            foreach(MetaDataModel model in this.MetaData){

                item.MetaData.Add(model.Property, model.Value);
=======
                ProviderKeyAbstract providerKey=model.GetProviderKey();
                item.ProviderKey.Add(providerKey.ProviderId, providerKey.Value);
>>>>>>> bf93d6d737f7b0f215fcd0016d43a34097fe193e
            }

            return(item);
        }
    }
}