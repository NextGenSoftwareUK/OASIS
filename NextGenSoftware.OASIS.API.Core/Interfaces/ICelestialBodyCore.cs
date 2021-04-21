using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface ICelestialBodyCore : IZome
    {
      //  string CoreZomeName { get; set; }
       // string CoreHolonType { get; set; }
        //string HolonsType { get; set; }
        //string HolonType { get; set; }
        string ProviderKey { get; set; }

        //event HolonsLoaded OnHolonsLoaded;
        //event ZomesLoaded OnZomesLoaded;

        Task<IHolon> LoadCelestialBodyAsync();
       // Task<List<IHolon>> LoadHolons();
        List<IZome> LoadZomes();
        Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon);
        public List<IZome> Zomes { get; set; }
        public new List<IHolon> Holons { get; }
    }
}