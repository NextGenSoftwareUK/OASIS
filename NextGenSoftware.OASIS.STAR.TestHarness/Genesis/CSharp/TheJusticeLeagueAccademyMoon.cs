using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.TestHarness.Genesis
{
    public class TheJusticeLeagueAccademy : Moon, IMoon
    {
        public TheJusticeLeagueAccademy() : base(new Guid("7d300cf6-17f9-4e1f-a6be-1c7d160d5091")) { }
 
        public OASISResult<SuperTest> LoadSuperTest(Guid id)
        {
            return base.CelestialBodyCore.LoadHolon<SuperTest>(id);
        }

        public async Task<OASISResult<SuperTest>> LoadSuperTestAsync(Guid id)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<SuperTest>(id);
        }

        public OASISResult<SuperTest> LoadSuperTest(ProviderType providerType, string providerKey)
        {
            return base.CelestialBodyCore.LoadHolon<SuperTest>(providerType, providerKey);
        }

        public async Task<OASISResult<SuperTest>> LoadSuperTestAsync(ProviderType providerType, string providerKey)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<SuperTest>(providerType, providerKey);
        }

        public OASISResult<SuperTest> SaveSuperTest(SuperTest holon)
        {
            return base.CelestialBodyCore.SaveHolon<SuperTest>(holon);
        }

        public async Task<OASISResult<SuperTest>> SaveSuperTestAsync(SuperTest holon)
        {
            return await base.CelestialBodyCore.SaveHolonAsync<SuperTest>(holon);
        }

        public OASISResult<SuperHolon> LoadSuperHolon(Guid id)
        {
            return base.CelestialBodyCore.LoadHolon<SuperHolon>(id);
        }

        public async Task<OASISResult<SuperHolon>> LoadSuperHolonAsync(Guid id)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<SuperHolon>(id);
        }

        public OASISResult<SuperHolon> LoadSuperHolon(ProviderType providerType, string providerKey)
        {
            return base.CelestialBodyCore.LoadHolon<SuperHolon>(providerType, providerKey);
        }

        public async Task<OASISResult<SuperHolon>> LoadSuperHolonAsync(ProviderType providerType, string providerKey)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<SuperHolon>(providerType, providerKey);
        }

        public OASISResult<SuperHolon> SaveSuperHolon(SuperHolon holon)
        {
            return base.CelestialBodyCore.SaveHolon<SuperHolon>(holon);
        }

        public async Task<OASISResult<SuperHolon>> SaveSuperHolonAsync(SuperHolon holon)
        {
            return await base.CelestialBodyCore.SaveHolonAsync<SuperHolon>(holon);
        }

        public OASISResult<SuperTest2> LoadSuperTest2(Guid id)
        {
            return base.CelestialBodyCore.LoadHolon<SuperTest2>(id);
        }

        public async Task<OASISResult<SuperTest2>> LoadSuperTest2Async(Guid id)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<SuperTest2>(id);
        }

        public OASISResult<SuperTest2> LoadSuperTest2(ProviderType providerType, string providerKey)
        {
            return base.CelestialBodyCore.LoadHolon<SuperTest2>(providerType, providerKey);
        }

        public async Task<OASISResult<SuperTest2>> LoadSuperTest2Async(ProviderType providerType, string providerKey)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<SuperTest2>(providerType, providerKey);
        }

        public OASISResult<SuperTest2> SaveSuperTest2(SuperTest2 holon)
        {
            return base.CelestialBodyCore.SaveHolon<SuperTest2>(holon);
        }

        public async Task<OASISResult<SuperTest2>> SaveSuperTest2Async(SuperTest2 holon)
        {
            return await base.CelestialBodyCore.SaveHolonAsync<SuperTest2>(holon);
        }

        public OASISResult<SuperHolon2> LoadSuperHolon2(Guid id)
        {
            return base.CelestialBodyCore.LoadHolon<SuperHolon2>(id);
        }

        public async Task<OASISResult<SuperHolon2>> LoadSuperHolon2Async(Guid id)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<SuperHolon2>(id);
        }

        public OASISResult<SuperHolon2> LoadSuperHolon2(ProviderType providerType, string providerKey)
        {
            return base.CelestialBodyCore.LoadHolon<SuperHolon2>(providerType, providerKey);
        }

        public async Task<OASISResult<SuperHolon2>> LoadSuperHolon2Async(ProviderType providerType, string providerKey)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<SuperHolon2>(providerType, providerKey);
        }

        public OASISResult<SuperHolon2> SaveSuperHolon2(SuperHolon2 holon)
        {
            return base.CelestialBodyCore.SaveHolon<SuperHolon2>(holon);
        }

        public async Task<OASISResult<SuperHolon2>> SaveSuperHolon2Async(SuperHolon2 holon)
        {
            return await base.CelestialBodyCore.SaveHolonAsync<SuperHolon2>(holon);
        }
   }
}