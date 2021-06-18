using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each Galaxy (creates SolarSysyems (with a Star at the centre of each).
    public interface ISuperStarCore : ICelestialBodyCore
    {
        ISuperStar SuperStar { get; set; }
        Task<OASISResult<IStar>> AddStarAsync(IStar star);
        OASISResult<IStar> AddStar(IStar star);
        Task<OASISResult<ISolarSystem>> AddSolarSystemAsync(ISolarSystem solarSystem);
        OASISResult<ISolarSystem> AddSolarSystem(ISolarSystem solarSystem);
        Task<OASISResult<IEnumerable<IStar>>> GetStarsAsync(bool refresh = true); //Helper method which gets the Stars at the centre of each SolarSystem.
        OASISResult<IEnumerable<IStar>> GetStars(bool refresh = true); //Helper method which gets the Stars at the centre of each SolarSystem.
        Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsAsync(bool refresh = true);
        OASISResult<IEnumerable<ISolarSystem>> GetSolarSystems(bool refresh = true);
    }
}