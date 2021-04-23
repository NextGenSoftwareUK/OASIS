using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using static NextGenSoftware.OASIS.API.Core.Events.Events;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface ICelestialBodyCore : IZome
    {
        List<IHolon> Holons { get; }
        string ProviderKey { get; set; }
        List<IZome> Zomes { get; set; }

        //event CelestialBodyCore.HolonsLoaded OnHolonsLoaded;
        //event CelestialBodyCore.ZomesLoaded OnZomesLoaded;

        event HolonsLoaded OnHolonsLoaded;
        event ZomesLoaded OnZomesLoaded;

        Task<OASISResult<IEnumerable<IHolon>>> AddZome(IZome zome);
        Task<IHolon> LoadCelestialBodyAsync();
        List<IZome> LoadZomes();
        Task<OASISResult<IEnumerable<IHolon>>> RemoveZome(IZome zome);
        Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon);
    }
}


//using NextGenSoftware.OASIS.API.Core.Helpers;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace NextGenSoftware.OASIS.API.Core.Interfaces
//{
//    public interface ICelestialBodyCore : IZome
//    {
//      //  string CoreZomeName { get; set; }
//       // string CoreHolonType { get; set; }
//        //string HolonsType { get; set; }
//        //string HolonType { get; set; }
//        string ProviderKey { get; set; }

//        //event HolonsLoaded OnHolonsLoaded;
//        //event ZomesLoaded OnZomesLoaded;

//        Task<IHolon> LoadCelestialBodyAsync();
//       // Task<List<IHolon>> LoadHolons();
//        List<IZome> LoadZomes();
//        Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon);
//        public List<IZome> Zomes { get; set; }
//        public new List<IHolon> Holons { get; }
//    }
//}

