using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of each Universe (creates Galaxies (with a SuperStar at the centre of each).
    public interface IGrandSuperStarCore : ICelestialBodyCore
    {
        IGrandSuperStar GrandSuperStar { get; set; }
       // Task<OASISResult<ISuperStar>> AddSuperStarAsync(ISuperStar superStar);
        Task<OASISResult<IGalaxy>> AddGalaxyAsync(IGalaxy galaxy);
        OASISResult<IGalaxy> AddGalaxy(IGalaxy galaxy);
        Task<OASISResult<IStar>> AddStarAsync(IStar star); //TODO: Not sure if we should allow them to add stars to a Universe outside of a Glaxy? In real-life they exist so maybe ok?
        OASISResult<IStar> AddStar(IStar star);
        Task<OASISResult<IEnumerable<ISuperStar>>> GetSuperStarsAsync(bool refresh = true); //Helper method which gets the SuperStars at the centre of each Galaxy.
        OASISResult<IEnumerable<ISuperStar>> GetSuperStars(bool refresh = true); //Helper method which gets the SuperStars at the centre of each Galaxy.
        Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesAsync(bool refresh = true);
        OASISResult<IEnumerable<IGalaxy>> GetGalaxies(bool refresh = true);
    }
}