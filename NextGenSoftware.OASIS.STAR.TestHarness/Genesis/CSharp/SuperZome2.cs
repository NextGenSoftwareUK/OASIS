using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.Zomes;

namespace NextGenSoftware.OASIS.STAR.TestHarness.Genesis
{
    public class SuperZome2 : ZomeBase, IZome
    {
        public SuperZome2() : base(new Guid("00000000-0000-0000-0000-000000000000")) { }
 
        public OASISResult<SuperTest2> LoadSuperTest2(Guid id)
        {
            return base.LoadHolon<SuperTest2>(id);
        }

        public async Task<OASISResult<SuperTest2>> LoadSuperTest2Async(Guid id)
        {
            return await base.LoadHolonAsync<SuperTest2>(id);
        }

        public OASISResult<SuperTest2> LoadSuperTest2(ProviderType providerType, string providerKey)
        {
            return base.LoadHolon<SuperTest2>(providerType, providerKey);
        }

        public async Task<OASISResult<SuperTest2>> LoadSuperTest2Async(ProviderType providerType, string providerKey)
        {
            return await base.LoadHolonAsync<SuperTest2>(providerType, providerKey);
        }

        public OASISResult<SuperTest2> SaveSuperTest2(SuperTest2 holon)
        {
            return base.SaveHolon<SuperTest2>(holon);
        }

        public async Task<OASISResult<SuperTest2>> SaveSuperTest2Async(SuperTest2 holon)
        {
            return await base.SaveHolonAsync<SuperTest2>(holon);
        }

        public OASISResult<SuperHolon2> LoadSuperHolon2(Guid id)
        {
            return base.LoadHolon<SuperHolon2>(id);
        }

        public async Task<OASISResult<SuperHolon2>> LoadSuperHolon2Async(Guid id)
        {
            return await base.LoadHolonAsync<SuperHolon2>(id);
        }

        public OASISResult<SuperHolon2> LoadSuperHolon2(ProviderType providerType, string providerKey)
        {
            return base.LoadHolon<SuperHolon2>(providerType, providerKey);
        }

        public async Task<OASISResult<SuperHolon2>> LoadSuperHolon2Async(ProviderType providerType, string providerKey)
        {
            return await base.LoadHolonAsync<SuperHolon2>(providerType, providerKey);
        }

        public OASISResult<SuperHolon2> SaveSuperHolon2(SuperHolon2 holon)
        {
            return base.SaveHolon<SuperHolon2>(holon);
        }

        public async Task<OASISResult<SuperHolon2>> SaveSuperHolon2Async(SuperHolon2 holon)
        {
            return await base.SaveHolonAsync<SuperHolon2>(holon);
        }
   }
}