using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.Interfaces
{
    public interface IPlanetCore : ICelestialBodyCore
    {
        IPlanet Planet { get; set; }
        Task<OASISResult<IMoon>> AddMoonAsync(IMoon moon);
        Task<List<IMoon>> GetMoons();
    }
}