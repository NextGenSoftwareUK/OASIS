using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IPlanetCore : ICelestialBodyCore
    {
        IPlanet Planet { get; set; }
        Task<OASISResult<IMoon>> AddMoonAsync(IMoon moon);
        OASISResult<IMoon> AddMoon(IMoon moon);
        Task<OASISResult<IEnumerable<IMoon>>> GetMoonsAsync(bool refresh = true);
        OASISResult<IEnumerable<IMoon>> GetMoons(bool refresh = true);
    }
}