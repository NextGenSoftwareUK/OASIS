using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.Holochain.HoloNET.HDK.Core;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness.Genesis
{
    //public class SuperZome : ZomeBase, ISuperZome
    public class SuperZome : ZomeBase, IZome
    {
        public SuperZome(HoloNETClientBase holoNETClient) : base(holoNETClient, "super_zome")
        {

        }

        public SuperZome(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, "super_zome", type)
        {

        }

        public async Task<IHolon> LoadSuperTestAsync(string hcEntryAddressHash)
        {
            return await base.LoadHolonAsync("super_test", hcEntryAddressHash);
        }

        public async Task<IHolon> SaveSuperTestAsync(IHolon holon)
        {
            return await base.SaveHolonAsync("super_test", holon);
        }
    public async Task<IHolon> LoadSuperHolonAsync(string hcEntryAddressHash)
        {
            return await base.LoadHolonAsync("super_holon", hcEntryAddressHash);
        }
public async Task<IHolon> SaveSuperHolonAsync(IHolon holon)
        {
            return await base.SaveHolonAsync("super_holon", holon);
        }
    }
}
