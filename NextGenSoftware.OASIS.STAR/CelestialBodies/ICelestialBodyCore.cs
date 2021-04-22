//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace NextGenSoftware.OASIS.STAR.CelestialBodies
//{
//    public interface ICelestialBodyCore
//    {
//        List<IHolon> Holons { get; }
//        string ProviderKey { get; set; }
//        List<IZome> Zomes { get; set; }

//        event CelestialBodyCore.HolonsLoaded OnHolonsLoaded;
//        event CelestialBodyCore.ZomesLoaded OnZomesLoaded;

//        Task<OASISResult<IEnumerable<IHolon>>> AddZome(IZome zome);
//        Task<IHolon> LoadCelestialBodyAsync();
//        List<IZome> LoadZomes();
//        Task<OASISResult<IEnumerable<IHolon>>> RemoveZome(IZome zome);
//        Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon);
//    }
//}