using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Zomes;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class SuperZome : ZomeBase, IZome
    {
        //public SuperZome(HoloNETClientBase holoNETClient) : base(holoNETClient, "super_zome")
        public SuperZome() : base()
        {

        }

        /*
        public SuperZome(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, "super_zome", type)
        {

        }*/

        //public async Task<IHolon> LoadSuperTestAsync(string hcEntryAddressHash)
        //{
        //    return await base.LoadHolonAsync(hcEntryAddressHash);
        //}

        public async Task<OASISResult<IHolon>> LoadSuperTestAsync(Dictionary<ProviderType, string> providerKey)
        {
            return await base.LoadHolonAsync(providerKey);
        }

        public async Task<OASISResult<IHolon>> SaveSuperTestAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("super_test", holon);
            return await base.SaveHolonAsync(holon);
        }
    public async Task<IHolon> LoadSuperHolonAsync(string hcEntryAddressHash)
        //{
        //    return await base.LoadHolonAsync(hcEntryAddressHash);
        //}

    Task<OASISResult<IHolon>> SaveSuperHolonAsync(IHolon holon)
        {
            //return await base.SaveHolonAsync("super_holon", holon);
            r}
}
