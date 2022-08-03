using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Zomes;

namespace NextGenSoftware.OASIS.STAR.TestHarness.Genesis
{
    public class SuperZome : ZomeBase, IZome
    {
        public SuperZome() : base()
        {

        }

        public async Task<OASISResult<SuperTest>> LoadSuperTestAsync(Guid id)
        {
            return await base.LoadHolonAsync<SuperTest>(id);
        }

        public OASISResult<SuperTest> LoadSuperTest(Guid id)
        {
            return base.LoadHolon<SuperTest>(id);
        }

        public async Task<OASISResult<SuperTest>> LoadSuperTestAsync(ProviderType providerType, string providerKey)
        {
            return await base.LoadHolonAsync<SuperTest>(providerType, providerKey);
        }

        public OASISResult<SuperTest> LoadSuperTest(ProviderType providerType, string providerKey)
        {
            return base.LoadHolon<SuperTest>(providerType, providerKey);
        }

        public async Task<OASISResult<SuperTest>> SaveSuperTestAsync(SuperTest holon)
        {
            return await base.SaveHolonAsync<SuperTest>(holon);
        }

        public OASISResult<SuperTest> SaveSuperTest(SuperTest holon)
        {
            return base.SaveHolon<SuperTest>(holon);
        }
    }
}