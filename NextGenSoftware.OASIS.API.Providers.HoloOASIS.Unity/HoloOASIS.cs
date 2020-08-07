using NextGenSoftware.Holochain.HDK.HoloNET.Client.Unity;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity
{
    public class HoloOASIS : HoloOASISBase
    {
        public HoloOASIS(string holochainURI) : base(new HoloNETClient(holochainURI))
        {
            
        }

       
    }
}
