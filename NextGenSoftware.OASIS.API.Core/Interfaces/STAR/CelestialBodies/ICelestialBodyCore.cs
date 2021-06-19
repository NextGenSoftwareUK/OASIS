using System.Collections.Generic;
using System.Threading.Tasks;
using static NextGenSoftware.OASIS.API.Core.Events.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialBodyCore : IZome
    {
        IEnumerable<IHolon> Holons { get; }
        List<IZome> Zomes { get; set; }

        //event CelestialBodyCore.HolonsLoaded OnHolonsLoaded;
        //event CelestialBodyCore.ZomesLoaded OnZomesLoaded;
        event HolonsLoaded OnHolonsLoaded;
        event ZomesLoaded OnZomesLoaded;

        Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync();
        OASISResult<ICelestialBody> LoadCelestialBody();
        Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync();
        OASISResult<IEnumerable<IZome>> LoadZomes();
        Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon);
        OASISResult<IHolon> SaveCelestialBody(IHolon savingHolon);
        Task<OASISResult<IZome>> AddZomeAsync(IZome zome);
        OASISResult<IZome> AddZome(IZome zome);
        Task<OASISResult<IEnumerable<IZome>>> RemoveZomeAsync(IZome zome);
        OASISResult<IEnumerable<IZome>> RemoveZome(IZome zome);
    }
}