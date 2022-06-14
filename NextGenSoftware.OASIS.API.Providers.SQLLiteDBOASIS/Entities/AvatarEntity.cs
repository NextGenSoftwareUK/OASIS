//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{
    public class AvatarEntity : HolonBase, IAvatar
    {

        public AvatarEntity()
        {
            this.HolonType = HolonType.Avatar;
        }

        //TODO: Think best to encrypt these?
        public Dictionary<ProviderType, string> ProviderPrivateKey { get; set; } = new Dictionary<ProviderType, string>();  //Unique private key used by each provider (part of private/public key pair).
        //public Dictionary<ProviderType, string> ProviderPublicKey { get; set; } = new Dictionary<ProviderType, string>();
        public Dictionary<ProviderType, List<string>> ProviderPublicKey { get; set; } = new Dictionary<ProviderType, List<string>>();
        public Dictionary<ProviderType, string> ProviderUsername { get; set; } = new Dictionary<ProviderType, string>();  // This is only really needed when we need to store BOTH a id and username for a provider (ProviderUniqueStorageKey on Holon already stores either id/username etc).                                                                                                            // public Dictionary<ProviderType, string> ProviderId { get; set; } = new Dictionary<ProviderType, string>(); // The ProviderUniqueStorageKey property on the base Holon object can store ids, usernames, etc that uniqueliy identity that holon in the provider (although the Guid is globally unique we still need to map the Holons the unique id/username/etc for each provider).
        //public Dictionary<ProviderType, string> ProviderWalletAddress { get; set; } = new Dictionary<ProviderType, string>();
        public Dictionary<ProviderType, List<string>> ProviderWalletAddress { get; set; } = new Dictionary<ProviderType, List<string>>();


        //public new Guid Id 
        //{
        //    get
        //    {
        //        return base.Id;
        //    }
        //    set
        //    {
        //        base.Id = value;
        //    }
        //}

        //Needed to work around bug in WebAPI where base class properties are not returned/serialized (it cannot be the same property name or it breaks Mongo for some unknown reason!)
        public Guid AvatarId
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }

        public  string Name { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
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
        public EnumValue<AvatarType> AvatarType { get; set; }
        public bool AcceptTerms { get; set; }
        public string VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string ResetToken { get; set; }
        public string JwtToken { get; set; }

        [JsonIgnore] // refresh token is returned in http only cookie
        public string RefreshToken { get; set; }

        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }
        public DateTime? LastBeamedIn { get; set; }
        public DateTime? LastBeamedOut { get; set; }
        public bool IsBeamedIn { get; set; }
        public Guid HolonId { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        // public string Image2D { get; set; }

        /*
        public int Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        public int XP { get; set; }
        public int Level
        {
            get
            {
                if (this.Karma > 0 && this.Karma < 100)
                    return 1;

                if (this.Karma >= 100 && this.Karma < 200)
                    return 2;

                if (this.Karma >= 200 && this.Karma < 300)
                    return 3;

                if (this.Karma >= 777)
                    return 99;

                //TODO: Add all the other levels here, all the way up to 100 for now! ;=)

                return 1; //Default.
            }
        }*/

        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }

        public async Task<IAvatar> SaveAsync()
        {
            OASISResult<IAvatar> result = await (ProviderManager.CurrentStorageProvider).SaveAvatarAsync(this);
            return result.Result;
        }
        public IAvatar Save()
        {
            return (ProviderManager.CurrentStorageProvider).SaveAvatar(this).Result;
        }

        public OASISResult<IAvatar> Save(ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IAvatar>> SaveAsync(ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public OASISResult<bool> SaveProviderWallets(ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<IAvatar>> SaveAsync(AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            return await AvatarManager.Instance.SaveAvatarAsync(this, autoReplicationMode, autoFailOverMode, waitForAutoReplicationResult, providerType);
        }

        public OASISResult<IAvatar> Save(AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            return AvatarManager.Instance.SaveAvatar(this, autoReplicationMode, autoFailOverMode, waitForAutoReplicationResult, providerType);
        }
    }
}