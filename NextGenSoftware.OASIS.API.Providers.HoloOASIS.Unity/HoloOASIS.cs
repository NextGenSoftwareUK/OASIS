using NextGenSoftware.Holochain.HoloNET.Client.Unity;
using NextGenSoftware.OASIS.API.DNA.Enums;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core;


namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity
{
    public class HoloOASIS : HoloOASISBase
    {
        public HoloOASIS(string holochainURI, HolochainVersion version) : base(new HoloNETClient(holochainURI, version))
        {
            
        }
    }
}
