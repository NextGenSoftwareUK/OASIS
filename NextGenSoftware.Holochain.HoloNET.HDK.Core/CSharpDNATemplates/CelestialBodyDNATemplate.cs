using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates
{
    public class CelestialBodyDNATemplate : CelestialBody, ICelestialBody
    {
        //public PlanetDNATemplate(HoloNETClientBase holoNETClient, string providerKey, Guid id) : base(holoNETClient, providerKey)
        //{

      //  }

        public CelestialBodyDNATemplate(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey, GenesisType.Star)
        {

        }

        //public PlanetDNATemplate(string holochainConductorURI, HoloNETClientType type, Guid id) : base(holochainConductorURI, type, id)
        //{

        //}

        public CelestialBodyDNATemplate(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, GenesisType.Star)
        {

        }

        public CelestialBodyDNATemplate(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey, GenesisType.Star)
        {

        }

        public CelestialBodyDNATemplate(HoloNETClientBase holoNETClient) : base(holoNETClient, GenesisType.Star)
        {

        }


        public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
        {
            return await base.LoadHolonAsync("{holon}", hcEntryAddressHash);
        }

        public async Task<IHolon> SaveHOLONAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("{holon}", holon);
            return await base.SaveHolonAsync("{holon}", holon);
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
