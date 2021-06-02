using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each Universe (creates Galaxies (with a SuperStar at the centre of each).
    public interface IGrandSuperStarCore : ISuperStarCore
    {
        IGrandSuperStar GrandSuperStar { get; set; }
        Task<ISuperStar> AddSuperStarAsync(ISuperStar star);
        Task<IGalaxy> AddGalaxyAsync(IGalaxy star);
        Task<List<ISuperStar>> GetSuperStars();
        Task<List<IGalaxy>> GetGalaxies();
    }
}