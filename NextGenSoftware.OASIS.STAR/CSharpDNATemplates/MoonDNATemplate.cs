
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.STAR.CSharpTemplates
{
    public class MoonDNATemplate : Moon, IMoon
    {
        public MoonDNATemplate(string providerKey) : base(providerKey)
        {

        }

        public MoonDNATemplate() : base()
        {

        }

        /*
        public MoonDNATemplate(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey)
        {

        }

        public MoonDNATemplate(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type)
        {

        }

        public MoonDNATemplate(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey)
        {

        }

        public MoonDNATemplate(HoloNETClientBase holoNETClient) : base(holoNETClient)
        {

        }*/

        public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
        {
            //return await CelestialBodyCore.LoadHolonAsync("{holon}", hcEntryAddressHash);
            return await CelestialBodyCore.LoadHolonAsync(hcEntryAddressHash);
        }

        public async Task<IHolon> SaveHOLONAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("{holon}", holon);
            return await CelestialBodyCore.SaveHolonAsync(holon);
        }

        /*
        //TODO: Do we still need these now? Nice to call the method what the holon type is I guess...
        public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
        {
            return await base.LoadHolonAsync(hcEntryAddressHash);
        }

        public async Task<IHolon> SaveHOLONAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("{holon}", holon);
            return await base.SaveHolonAsync(holon);
        }*/
    }
}
