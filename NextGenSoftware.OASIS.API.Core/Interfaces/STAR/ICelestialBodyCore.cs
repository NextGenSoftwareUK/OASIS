using System.Collections.Generic;
using System.Threading.Tasks;
using static NextGenSoftware.OASIS.API.Core.Events.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialBodyCore : IZome
    {
        List<IHolon> Holons { get; }
        List<IZome> Zomes { get; set; }

        //event CelestialBodyCore.HolonsLoaded OnHolonsLoaded;
        //event CelestialBodyCore.ZomesLoaded OnZomesLoaded;
        event HolonsLoaded OnHolonsLoaded;
        event ZomesLoaded OnZomesLoaded;

        Task<IHolon> LoadCelestialBodyAsync();
        IHolon LoadCelestialBody();
        Task<OASISResult<List<IZome>>> LoadZomesAsync();
        OASISResult<List<IZome>> LoadZomes();
        Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon);
        Task<OASISResult<IZome>> AddZomeAsync(IZome zome);
        Task<OASISResult<IEnumerable<IHolon>>> RemoveZomeAsync(IZome zome);
    }
}