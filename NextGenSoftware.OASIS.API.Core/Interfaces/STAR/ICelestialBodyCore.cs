using System.Collections.Generic;
using System.Threading.Tasks;
using static NextGenSoftware.OASIS.API.Core.Events.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialBodyCore : IZome
    {
        List<IHolon> Holons { get; }
      //  string ProviderKey { get; set; }
        List<IZome> Zomes { get; set; }

        //event CelestialBodyCore.HolonsLoaded OnHolonsLoaded;
        //event CelestialBodyCore.ZomesLoaded OnZomesLoaded;

        event HolonsLoaded OnHolonsLoaded;
        event ZomesLoaded OnZomesLoaded;

        Task<OASISResult<IZome>> AddZome(IZome zome);
        Task<IHolon> LoadCelestialBodyAsync();
        Task<OASISResult<List<IZome>>> LoadZomesAsync();
        Task<OASISResult<IEnumerable<IHolon>>> RemoveZome(IZome zome);
        Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon);
    }
}