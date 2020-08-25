
using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class Zome : ZomeBase, IZome
    {
        public Zome(HoloNETClientBase holoNETClient, string zomeName) : base(holoNETClient, zomeName)
        {

        }

        public Zome(string holochainConductorURI, HoloNETClientType type, string zomeName) : base(holochainConductorURI, zomeName, type)
        {

        }

        //public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
        //{
        //    return await base.LoadHolonAsync("{holon}", hcEntryAddressHash);
        //}

        //public async Task<IHolon> SaveHOLONAsync(IHolon holon)
        //{
        //    //return await base.SaveHolonAsync("{holon}", holon);
        //    return await base.SaveHolonAsync(holon);
        //}
    }
}
