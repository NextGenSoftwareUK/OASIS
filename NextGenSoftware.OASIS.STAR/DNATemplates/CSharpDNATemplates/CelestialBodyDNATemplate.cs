//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.STAR.CelestialBodies;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

//namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
//{
//    public class CelestialBodyDNATemplate : CelestialBody, ICelestialBody
//    {
//        public CelestialBodyDNATemplate(Dictionary<ProviderType, string> providerKey) : base(providerKey, GenesisType.CelestialBody)
//        {

//        }

//        public CelestialBodyDNATemplate() : base(GenesisType.CelestialBody)
//        {

//        }

        

//        //public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
//        //{
//        //    return await base.CelestialBodyCore.LoadHolonAsync(hcEntryAddressHash);
//        //}

//        public async Task<IHolon> LoadHOLONAsync(Dictionary<ProviderType, string> providerKey)
//        {
//            return await base.CelestialBodyCore.LoadHolonAsync(providerKey);
//        }

//        public async Task<OASISResult<IHolon>> SaveHOLONAsync(IHolon holon)
//        {
//            //return await base.CelestialBodyCore.SaveHolonAsync("{holon}", holon);
//            return await base.CelestialBodyCore.SaveHolonAsync(holon);
//        }

//        /*
//        //TODO: Do we still need these now? Nice to call the method what the holon type is I guess...
//        public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
//        {
//            return await base.LoadHolonAsync(hcEntryAddressHash);
//        }

//        public async Task<IHolon> SaveHOLONAsync(IHolon holon)
//        {
//            //return await base.SaveHolonAsync("{holon}", holon);
//            return await base.SaveHolonAsync(holon);
//        }*/
//    }
//}
