//using System.Collections.Generic;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

//namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
//{
//    public class StarDNATemplate : CelestialBodies.Star, IStar
//    {
//        public StarDNATemplate(Dictionary<ProviderType, string> providerKey) : base(providerKey)
//        {

//        }

//        public StarDNATemplate() : base()
//        {

//        }


//        //public StarDNATemplate(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, providerKey)
//        //{

//        //}

//        //public StarDNATemplate(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type)
//        //{

//        //}

//        //public StarDNATemplate(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, providerKey)
//        //{

//        //}

//        //public StarDNATemplate(HoloNETClientBase holoNETClient) : base(holoNETClient)
//        //{

//        //}

//        //public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
//        //{
//        //    return await CelestialBodyCore.LoadHolonAsync(hcEntryAddressHash);
//        //}

//        public async Task<OASISResult<IHolon>> LoadHOLONAsync(Dictionary<ProviderType, string> providerKey)
//        {
//            return await CelestialBodyCore.LoadHolonAsync(providerKey);
//        }

//        public async Task<OASISResult<IHolon>> SaveHOLONAsync(IHolon holon)
//        {
//            //return await base.SaveHolonAsync("{holon}", holon);
//            //return await CelestialBodyCore.SaveHolonAsync("{holon}", holon);
//            return await CelestialBodyCore.SaveHolonAsync(holon);
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
