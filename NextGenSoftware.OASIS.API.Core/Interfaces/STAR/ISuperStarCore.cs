using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each Galaxy (creates SolarSysyems (with a Star at the centre of each).
    public interface ISuperStarCore : IStarCore
    {
        ISuperStar SuperStar { get; set; }
        Task<IStar> AddStarAsync(IStar star);
        Task<ISolarSystem> AddSolarSystemAsync(ISolarSystem star);
        Task<List<IStar>> GetStars();
        Task<List<ISolarSystem>> GetSolarSystem();
    }
}