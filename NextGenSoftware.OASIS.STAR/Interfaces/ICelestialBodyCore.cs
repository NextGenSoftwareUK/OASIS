//using System.Collections.Generic;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.STAR.CelestialBodies;

//namespace NextGenSoftware.OASIS.STAR.Interfaces
//{
//    public interface ICelestialBodyCore : IZome
//    {
//      //  string CoreZomeName { get; set; }
//       // string CoreHolonType { get; set; }
//        //string HolonsType { get; set; }
//        //string HolonType { get; set; }
//        string ProviderKey { get; set; }

//        event CelestialBodyCore.HolonsLoaded OnHolonsLoaded;
//        event CelestialBodyCore.ZomesLoaded OnZomesLoaded;

//        Task<IHolon> LoadCelestialBodyAsync();
//       // Task<List<IHolon>> LoadHolons();
//        List<IZome> LoadZomes();
//        Task<IHolon> SaveCelestialBodyAsync(IHolon savingHolon);
//    }
//}