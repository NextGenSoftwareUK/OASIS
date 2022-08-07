//using System;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.STAR.Zomes;

//namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
//{
//    public class ZomeDNATemplate : ZomeBase, IZome
//    {
//        public ZomeDNATemplate() : base()
//        {

//        }

//        public async Task<OASISResult<HOLON>> LoadHOLONAsync(Guid id)
//        {
//            return await base.LoadHolonAsync<HOLON>(id);
//        }

//        public OASISResult<HOLON> LoadHOLON(Guid id)
//        {
//            return base.LoadHolon<HOLON>(id);
//        }

//        public async Task<OASISResult<HOLON>> LoadHOLONAsync(ProviderType providerType, string providerKey)
//        {
//            return await base.LoadHolonAsync<HOLON>(providerType, providerKey);
//        }

//        public OASISResult<HOLON> LoadHOLON(ProviderType providerType, string providerKey)
//        {
//            return base.LoadHolon<HOLON>(providerType, providerKey);
//        }

//        public async Task<OASISResult<HOLON>> SaveHOLONAsync(HOLON holon)
//        {
//            return await base.SaveHolonAsync<HOLON>(holon);
//        }

//        public OASISResult<HOLON> SaveHOLON(HOLON holon)
//        {
//            return base.SaveHolon<HOLON>(holon);
//        }
//    }
//}