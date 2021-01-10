using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness.Genesis
{
    public class OurWorld : Planet, IPlanet
    {
        public OurWorld(string providerKey) : base(providerKey)
        {

        }

        public OurWorld() : base()
        {

        }

        //public OurWorld(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey)
        //{

        //}

        //public OurWorld(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type)
        //{

        //}

        //public OurWorld(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey)
        //{

        //}

        //public OurWorld(HoloNETClientBase holoNETClient) : base(holoNETClient)
        //{

        //}


        public async Task<IHolon> LoadSuperTestAsync(string hcEntryAddressHash)
        {
            return await CelestialBodyCore.LoadHolonAsync("super_test", hcEntryAddressHash);
        }

        public async Task<IHolon> SaveSuperTestAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("super_test", holon);
            return await CelestialBodyCore.SaveHolonAsync("super_test", holon);
        }

        /*
        //TODO: Do we still need these now? Nice to call the method what the holon type is I guess...
        public async Task<IHolon> LoadSuperTestAsync(string hcEntryAddressHash)
        {
            return await base.LoadHolonAsync(hcEntryAddressHash);
        }

        public async Task<IHolon> SaveSuperTestAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("super_test", holon);
            return await base.SaveHolonAsync(holon);
        }*/
    }
}
