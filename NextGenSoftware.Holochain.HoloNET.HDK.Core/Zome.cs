
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
        private const string ZOMES_LOAD_ALL = "_holons_loadall";
        private const string HOLONS_ADD = "_holons_add";
        private const string HOLONS_REMOVE = "_holons_remove";

        public Zome(HoloNETClientBase holoNETClient, string zomeName) : base(holoNETClient, zomeName)
        {

        }

        public Zome(string holochainConductorURI, HoloNETClientType type, string zomeName) : base(holochainConductorURI, zomeName, type)
        {

        }

        public async Task<IHolon> AddHolon(IZome zome)
        {
            return await base.SaveHolonAsync(string.Concat(this.CoreHolonType, HOLONS_ADD), zome);
        }

        public async Task<IHolon> RemoveHolon(IZome zome)
        {
            //TODO: Finish
            return await base.SaveHolonAsync(string.Concat(this.CoreHolonType, HOLONS_REMOVE), zome);
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
