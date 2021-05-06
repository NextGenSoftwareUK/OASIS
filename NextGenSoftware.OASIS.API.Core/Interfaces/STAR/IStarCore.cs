using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IStarCore : ICelestialBodyCore
    {
        IStar Star { get; set; }

        Task<OASISResult<IPlanet>> AddPlanetAsync(IPlanet planet);
       // Task<IStar> AddStarAsync(IStar star);
        Task<List<IPlanet>> GetPlanets();
      //  Task<List<IStar>> GetStars();
    }
}