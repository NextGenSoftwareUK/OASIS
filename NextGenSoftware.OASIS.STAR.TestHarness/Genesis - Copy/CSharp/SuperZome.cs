using System.Threading.Tasks; 
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.STAR.TestHarness.Genesis
{
    //public class SuperZome : ZomeBase, ISuperZome
    public class SuperZome : ZomeBase, IZome
    {
        //public SuperZome(HoloNETClientBase holoNETClient) : base(holoNETClient, "super_zome")
        public SuperZome() : base()
        {

        }

        //public SuperZome(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, "super_zome", type)
        //{

        //}

        public async Task<IHolon> LoadSuperTestAsync(string hcEntryAddressHash)
        {
            //return await base.LoadHolonAsync("super_test", hcEntryAddressHash);
            return await base.LoadHolonAsync(hcEntryAddressHash);
        }

        public async Task<IHolon> SaveSuperTestAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("super_test", holon);
            return await base.SaveHolonAsync(holon);
        }
        public async Task<IHolon> LoadSuperHolonAsync(string hcEntryAddressHash)
        {
            //return await base.LoadHolonAsync("super_holon", hcEntryAddressHash);
            return await base.LoadHolonAsync(hcEntryAddressHash);
        }
        public async Task<IHolon> SaveSuperHolonAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("super_holon", holon);
            return await base.SaveHolonAsync(holon);
        }
    }
}
