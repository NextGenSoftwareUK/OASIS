using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class MoonDNATemplate : Moon, IMoon
    {
        public MoonDNATemplate(Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {

        }

        public MoonDNATemplate(Guid id) : base(id)
        {

        }

        public MoonDNATemplate() : base()
        {

        }

        public async Task<OASISResult<IHolon>> LoadHOLONAsync(Dictionary<ProviderType, string> providerKey)
        {
            return await CelestialBodyCore.LoadHolonAsync(providerKey);
        }

        public async Task<OASISResult<IHolon>> LoadHOLONAsync(Guid id)
        {
            return await CelestialBodyCore.LoadHolonAsync(id);
        }

        public async Task<OASISResult<IHolon>> SaveHOLONAsync(IHolon holon)
        {
            return await CelestialBodyCore.SaveHolonAsync(holon);
        }
    }
}
