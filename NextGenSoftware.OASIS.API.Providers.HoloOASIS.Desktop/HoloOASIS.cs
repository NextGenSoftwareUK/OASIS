using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client.Desktop;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Threading.Tasks;

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
