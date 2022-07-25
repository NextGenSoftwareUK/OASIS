using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Zomes;

namespace NextGenSoftware.OASIS.STAR.TestHarness.Genesis
{
    public class SuperZome : ZomeBase, IZome
    {
        public SuperZome() : base()
        {

        }

        public async Task<OASISResult<ISuperTest>> LoadSuperTestAsync(Guid id)
        {
            return await base.LoadHolonAsync(id);
        }

        public async Task<OASISResult<ISuperTest>> LoadSuperTestAsync(Dictionary<ProviderType, string> providerKey)
        {
            return await base.LoadHolonAsync(providerKey);
        }

        public async Task<OASISResult<ISuperTest>> SaveSuperTestAsync(ISuperTest holon)
        {
            return await base.SaveHolonAsync(holon);
        }
    }
}