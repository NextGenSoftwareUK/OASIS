using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Zomes;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class ZomeDNATemplate : ZomeBase, IZome
    {
        public ZomeDNATemplate() : base()
        {

        }

        public async Task<OASISResult<IHolon>> LoadHOLONAsync(Guid id)
        {
            return await base.LoadHolonAsync(id);
        }

        public async Task<OASISResult<IHolon>> LoadHOLONAsync(string providerKey)
        {
            return await base.LoadHolonAsync(providerKey);
        }

        public async Task<OASISResult<IHolon>> SaveHOLONAsync(IHolon holon)
        {
            return await base.SaveHolonAsync(holon);
        }
    }
}