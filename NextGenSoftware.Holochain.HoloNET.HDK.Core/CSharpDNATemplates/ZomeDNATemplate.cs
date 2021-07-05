using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.Holochain.HoloNET.HDK.Core;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates
{
    //public class ZomeDNATemplate : ZomeBase, IZomeDNATemplate
    public class ZomeDNATemplate : ZomeBase, IZome
    {
        public ZomeDNATemplate(HoloNETClientBase holoNETClient) : base(holoNETClient, "{zome}")
        {

        }

        public ZomeDNATemplate(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, "{zome}", type)
        {

        }

        public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
        {
            return await base.LoadHolonAsync("{holon}", hcEntryAddressHash);
        }

        public async Task<IHolon> SaveHOLONAsync(IHolon holon)
        {
            return await base.SaveHolonAsync("{holon}", holon);
        }
    }
}
