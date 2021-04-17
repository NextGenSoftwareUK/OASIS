using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class StarDNATemplate : Star, IStar
    {
        public StarDNATemplate(string providerKey) : base(providerKey)
        {

        }

        public StarDNATemplate() : base()
        {

        }


        //public StarDNATemplate(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey)
        //{

        //}

        //public StarDNATemplate(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type)
        //{

        //}

        //public StarDNATemplate(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey)
        //{

        //}

        //public StarDNATemplate(HoloNETClientBase holoNETClient) : base(holoNETClient)
        //{

        //}

        public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
        {
            //return await CelestialBodyCore.LoadHolonAsync("{holon}", hcEntryAddressHash);
            return await CelestialBodyCore.LoadHolonAsync(hcEntryAddressHash);
        }

        public async Task<IHolon> SaveHOLONAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("{holon}", holon);
            //return await CelestialBodyCore.SaveHolonAsync("{holon}", holon);
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
