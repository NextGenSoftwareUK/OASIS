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
        public SuperZome() : base(new Guid("00000000-0000-0000-0000-000000000000")) { }
 
        public OASISResult<SuperTest> LoadSuperTest(Guid id)
        {
            return base.LoadHolon<SuperTest>(id);
        }

        public async Task<OASISResult<SuperTest>> LoadSuperTestAsync(Guid id)
        {
            return await base.LoadHolonAsync<SuperTest>(id);
        }

        public OASISResult<SuperTest> LoadSuperTest(ProviderType providerType, string providerKey)
        {
            return base.LoadHolon<SuperTest>(providerType, providerKey);
        }

        public async Task<OASISResult<SuperTest>> LoadSuperTestAsync(ProviderType providerType, string providerKey)
        {
            return await base.LoadHolonAsync<SuperTest>(providerType, providerKey);
        }

        public OASISResult<SuperTest> SaveSuperTest(SuperTest holon)
        {
            return base.SaveHolon<SuperTest>(holon);
        }

        public async Task<OASISResult<SuperTest>> SaveSuperTestAsync(SuperTest holon)
        {
            return await base.SaveHolonAsync<SuperTest>(holon);
        }

        public OASISResult<SuperHolon> LoadSuperHolon(Guid id)
        {
            return base.LoadHolon<SuperHolon>(id);
        }

        public async Task<OASISResult<SuperHolon>> LoadSuperHolonAsync(Guid id)
        {
            return await base.LoadHolonAsync<SuperHolon>(id);
        }

        public OASISResult<SuperHolon> LoadSuperHolon(ProviderType providerType, string providerKey)
        {
            return base.LoadHolon<SuperHolon>(providerType, providerKey);
        }

        public async Task<OASISResult<SuperHolon>> LoadSuperHolonAsync(ProviderType providerType, string providerKey)
        {
            return await base.LoadHolonAsync<SuperHolon>(providerType, providerKey);
        }

        public OASISResult<SuperHolon> SaveSuperHolon(SuperHolon holon)
        {
            return base.SaveHolon<SuperHolon>(holon);
        }

        public async Task<OASISResult<SuperHolon>> SaveSuperHolonAsync(SuperHolon holon)
        {
            return await base.SaveHolonAsync<SuperHolon>(holon);
        }
   }
}