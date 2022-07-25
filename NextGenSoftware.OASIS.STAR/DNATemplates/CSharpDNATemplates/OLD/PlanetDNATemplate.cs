//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.STAR.CelestialBodies;

//namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
//{
//    public class PlanetDNATemplate : Planet, IPlanet
//    {
//        public PlanetDNATemplate(Dictionary<ProviderType, string> providerKey) : base(providerKey)
//        {

//        }

//        public PlanetDNATemplate(Guid id) : base(id)
//        {

//        }

//        public PlanetDNATemplate() : base()
//        {

//        }

//        public async Task<OASISResult<IHolon>> LoadHOLONAsync(Dictionary<ProviderType, string> providerKey)
//        {
//            return await CelestialBodyCore.LoadHolonAsync(providerKey);
//        }

//        public async Task<OASISResult<IHolon>> LoadHOLONAsync(Guid id)
//        {
//            return await CelestialBodyCore.LoadHolonAsync(id);
//        }

//        public async Task<OASISResult<IHolon>> SaveHOLONAsync(IHolon holon)
//        {
//            return await CelestialBodyCore.SaveHolonAsync(holon);
//        }
//    }
//}
