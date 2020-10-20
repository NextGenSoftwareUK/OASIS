using NextGenSoftware.Holochain.HoloNET.Client.Unity;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity
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
            throw new NotImplementedException();
        }

        public override IAvatar SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }
    }
}
