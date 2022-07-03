//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.STAR.CelestialBodies;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

//namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
//{
//    public class TheJusticeLeagueAccademy : Moon, IMoon
//    {
//        public TheJusticeLeagueAccademy(Dictionary<ProviderType, string> providerKey) : base(providerKey, GenesisType.Moon)
//        {

//        }

//        public TheJusticeLeagueAccademy() : base(GenesisType.Moon)
//        {

//        }

        

//        //public async Task<IHolon> LoadSuperTestAsync(string hcEntryAddressHash)
//        //{
//        //    return await base.MoonCore.LoadHolonAsync(hcEntryAddressHash);
//        //}

//        public async Task<IHolon> LoadSuperTestAsync(Dictionary<ProviderType, string> providerKey)
//        {
//            return await base.MoonCore.LoadHolonAsync(providerKey);
//        }

//        public async Task<OASISResult<IHolon>> SaveSuperTestAsync(IHolon holon)
//        {
//            //return await base.MoonCore.SaveHolonAsync("super_test", holon);
//            return await base.MoonCore.SaveHolonAsync(holon);
//        }

//        /*
//        //TODO: Do we still need these now? Nice to call the method what the holon type is I guess...
//        public async Task<IHolon> LoadSuperTestAsync(string hcEntryAddressHash)
//        {
//            return await base.LoadHolonAsync(hcEntryAddressHash);
//        }

//        public async Task<IHolon> SaveSuperTestAsync(IHolon holon)
//        {
//            //return await base.SaveHolonAsync("super_test", holon);
//            return await base.SaveHolonAsync(holon);
//        }*/
//    }
//}
