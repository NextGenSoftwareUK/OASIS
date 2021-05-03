using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;
using NextGenSoftware.Holochain.HoloNET.Client.Desktop;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop
{
    public class HoloOASIS : HoloOASISBase
    {
        public HoloOASIS(string holochainURI, HolochainVersion version) : base(new HoloNETClient(holochainURI, version))
        {
            
        }

        //public HoloOASIS(string holochainURI, string version) : base(new HoloNETClient(holochainURI, (HolochainVersion)Enum.Parse(typeof(HolochainVersion), version)))
        //{

        //}
    }
}
