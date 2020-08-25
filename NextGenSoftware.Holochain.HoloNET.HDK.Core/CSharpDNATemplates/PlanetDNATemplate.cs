using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.Holochain.HoloNET.HDK.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static NextGenSoftware.Holochain.HoloNET.HDK.Core.PlanetBase;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates
{
    //public class ZomeDNATemplate : ZomeBase, IZomeDNATemplate
    public class PlanetDNATemplate : PlanetBase, IPlanet
    {
        //public PlanetDNATemplate(HoloNETClientBase holoNETClient) : base(holoNETClient, "{planet}")
        public PlanetDNATemplate(HoloNETClientBase holoNETClient) : base(holoNETClient)
        {

        }

        public PlanetDNATemplate(HoloNETClientBase holoNETClient, Guid id) : base(holoNETClient, id)
        {

        }

        //public PlanetDNATemplate(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, "{planet}", type)
       // {
       
       // }

        public PlanetDNATemplate(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type)
        {

        }

        public PlanetDNATemplate(string holochainConductorURI, HoloNETClientType type, Guid id) : base(holochainConductorURI, type, id)
        {

        }

        public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
        {
            return await base.LoadHolonAsync("{holon}", hcEntryAddressHash);
        }

        public async Task<IHolon> SaveHOLONAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("{holon}", holon);
            return await base.SaveHolonAsync(holon);
        }
    }
}
