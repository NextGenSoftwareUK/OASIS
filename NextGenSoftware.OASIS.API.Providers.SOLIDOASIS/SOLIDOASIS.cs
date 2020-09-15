//using Microsoft.AspNetCore.Mvc.RazorPages;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS
{
    public class SOLIDOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        //public event AvatarManager.StorageProviderError OnStorageProviderError;

        public SOLIDOASIS()
        {
            this.ProviderName = "SOLIDOASIS";
            this.ProviderDescription = "SOLID Provider";
            this.ProviderType = ProviderType.SOLIDOASIS;
            this.ProviderCategory = ProviderCategory.StorageAndNetwork;
        }

        public List<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public List<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(string providerKey)
        {
            //TODO: Call into JS SOLID Code here.
            //Page.ClientScript.RegisterStartupScript(this.GetType(), "CallMyFunction", "MyFunction()", true);

            return null;
        }

        public override Task<IAvatar> LoadAvatarAsync(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public async override void ActivateProvider()
        {
            //TODO: Login/Connect to SOLID POD here.
            base.ActivateProvider();
        }

        public async override void DeActivateProvider()
        {
            //TODO: Logout of SOLID POD here.
            base.DeActivateProvider();
        }

        public override Task<ISearchResults> SearchAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(Guid Id)
        {
            throw new NotImplementedException();
        }
    }
}
