using NextGenSoftware.Holochain.HoloNET.Client.Desktop;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop
{
    public class HoloOASIS : HoloOASISBase
    {
        public HoloOASIS(string holochainURI) : base(new HoloNETClient(holochainURI))
        {
            
        }
    }
}
