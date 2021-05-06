using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISuperStarCore
    {
       // IStar Star { get; set; }

       // Task<IPlanet> AddPlanetAsync(IPlanet planet);
        Task<IStar> AddStarAsync(IStar star);
      //  Task<List<IPlanet>> GetPlanets();
        Task<List<IStar>> GetStars();
    }
}