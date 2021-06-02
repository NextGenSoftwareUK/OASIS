using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.STAR.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each SolarSystem (creates Planets)
    public interface IStarCore : IPlanetCore
    {
        IStar Star { get; set; }
        Task<OASISResult<IPlanet>> AddPlanetAsync(IPlanet planet);
        Task<List<IPlanet>> GetPlanets();
    }
}