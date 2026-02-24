using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

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
        public List<MetaDataModel> MetaData { get; set;}
        public List<ProviderKeyModel> ProviderKey { get; set; } = new List<ProviderKeyModel>();

        public AvatarModel(){}
        public AvatarModel(IAvatar source){

            if(source.Id == Guid.Empty){
                // Use ProviderUniqueStorageKey if available (immutable), otherwise fallback to a stable identifier
                var immutableKey = source.ProviderUniqueStorageKey?.Values?.FirstOrDefault() 
                    ?? source.ProviderUniqueStorageKey?.Keys?.FirstOrDefault().ToString() 
                    ?? $"avatar:{source.CreatedDate.Ticks}";
                this.Id = CreateDeterministicGuid(immutableKey).ToString();
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

            this.Username=source.Username;
            this.Email=source.Email;
            
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

            if (this.RefreshTokens == null)
                this.RefreshTokens = new List<RefreshTokenModel>();

            foreach(RefreshToken refreshToken in source.RefreshTokens){

                RefreshTokenModel model=new RefreshTokenModel(refreshToken);
                model.AvatarId=this.Id;
                this.RefreshTokens.Add(model);
            }

          //  if (this.ProviderKey == null)
            //    this.ProviderKey = new List<ProviderKeyModel>();

            /*
            foreach (KeyValuePair<ProviderType, string> key in source.ProviderKey){

                ProviderKeyModel model=new ProviderKeyModel(key.Key,key.Value);
                model.OwnerId=this.Id;
                this.ProviderKey.Add(model);
            }*/

            if (this.MetaData == null)
                this.MetaData = new List<MetaDataModel>();

            foreach (KeyValuePair<string, object> item in source.MetaData){

                MetaDataModel metaModel=new MetaDataModel(item.Key,item.Value);
                metaModel.OwnerId=this.Id;
                this.MetaData.Add(metaModel);
            }

        }

        public Avatar GetAvatar(){

            Avatar item = new Avatar();
            
            item.Id=Guid.Parse(this.Id);
            item.Title=this.Title;
            item.FirstName=this.FirstName;
            item.LastName=this.LastName;

            item.Description = this.Description;
            item.HolonType = this.HolonType;
            item.AvatarType = new EnumValue<AvatarType>(this.AvatarType);

            item.Username=this.Username;
            item.Email=this.Email;
            
            item.AcceptTerms = this.AcceptTerms;
            item.JwtToken = this.JwtToken;
            item.RefreshToken = this.RefreshToken;

            item.PasswordReset = this.PasswordReset;
            item.ResetToken = this.ResetToken;
            item.ResetTokenExpires = this.ResetTokenExpires;
            item.VerificationToken = this.VerificationToken;
            item.Verified = this.Verified;

            
            item.CreatedOASISType= new EnumValue<OASISType>(this.CreatedOASISType);
            item.CreatedProviderType = new EnumValue<ProviderType>(this.CreatedProviderType);
            
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

            foreach(RefreshTokenModel model in this.RefreshTokens){

                item.RefreshTokens.Add(model.GetRefreshToken());
            }

            //foreach(ProviderKeyModel model in this.ProviderKey){

            //    item.ProviderKey.Add(model.ProviderId, model.Value);
            //}

            foreach(MetaDataModel model in this.MetaData){

                item.MetaData.Add(model.Property, model.Value);
            }

            return(item);
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }
    }
}