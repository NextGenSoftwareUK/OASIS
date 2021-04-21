using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.STAR.Interfaces
{
    public interface IStarCore
    {
        IStar Star { get; set; }

        Task<OASISResult<IPlanet>> AddPlanetAsync(IPlanet planet);
       // Task<IStar> AddStarAsync(IStar star);
        Task<List<IPlanet>> GetPlanets();
      //  Task<List<IStar>> GetStars();
    }
}