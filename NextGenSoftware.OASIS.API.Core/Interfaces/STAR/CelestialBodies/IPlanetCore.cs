using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IPlanetCore : ICelestialBodyCore
    {
        IPlanet Planet { get; set; }

        OASISResult<IEnumerable<IMoon>> GetMoons(bool refresh = true);
        Task<OASISResult<IEnumerable<IMoon>>> GetMoonsAsync(bool refresh = true);
        OASISResult<IEnumerable<IMoon>> SaveMoons(bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<IEnumerable<IMoon>>> SaveMoonsAsync(bool saveChildren = true, bool recursive = true, bool continueOnError = true);
    }
}