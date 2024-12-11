using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.TestHarness.Genesis.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STAR.TestHarness.Genesis
{
    public class SuperWorld : Planet, IPlanet
    {
        public SuperWorld(Dictionary<ProviderType, string> providerKey, bool autoLoad = true) : base(providerKey, autoLoad)
        {

        }

        public SuperWorld(Guid id, bool autoLoad = true) : base(id, autoLoad)
        {

        }

        public SuperWorld() : base()
        {

        }

        public async Task<OASISResult<ISuperTest>> LoadSuperTestAsync(Guid id)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<ISuperTest>(id);
        }

        public OASISResult<ISuperTest> LoadSuperTest(Guid id)
        {
            return base.CelestialBodyCore.LoadHolon<ISuperTest>(id);
        }

        public async Task<OASISResult<IHolon>> LoadSuperTestAsync(Dictionary<ProviderType, string> providerKey)
        {
            return await base.CelestialBodyCore.LoadHolonAsync(providerKey);
        }

        public OASISResult<IHolon> LoadSuperTest(Dictionary<ProviderType, string> providerKey)
        {
            return base.CelestialBodyCore.LoadHolon(providerKey);
        }

        public async Task<OASISResult<IHolon>> SaveSuperTestAsync(IHolon holon)
        {
            return await base.CelestialBodyCore.SaveHolonAsync(holon);
        }

        public OASISResult<IHolon> SaveSuperTest(IHolon holon)
        {
            return base.CelestialBodyCore.SaveHolon(holon);
        }
    }
}