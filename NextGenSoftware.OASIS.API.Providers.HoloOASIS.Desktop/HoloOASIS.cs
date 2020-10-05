using NextGenSoftware.Holochain.HoloNET.Client.Desktop;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop
{
    public class HoloOASIS : HoloOASISBase
    {
        public HoloOASIS(string holochainURI) : base(new HoloNETClient(holochainURI))
        {
            
        }

        public override bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username)
        {
            //throw new NotImplementedException();
            return new Avatar() { ProviderType = ProviderType.HoloOASIS };
        }

        public override IAvatar SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }



        //public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        //{
        //    throw new System.NotImplementedException();
        //}

        //public override IAvatar LoadAvatar(string username, string password)
        //{
        //    throw new System.NotImplementedException();
        //}

        //public override Task<ISearchResults> SearchAsync(string searchTerm)
        //{
        //    throw new System.NotImplementedException();
        //}
    }
}
