using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of the Omiverse (creates Universes (with a GrandSuperStar at the centre of each).
    public interface IGreatGrandSuperStarCore : ICelestialBodyCore
    {
        IGreatGrandSuperStar GreatGrandSuperStar { get; set; }
       // Task<OASISResult<IGrandSuperStar>> AddGrandSuperStarAsync(IGrandSuperStar grandSuperStar);
        Task<OASISResult<IUniverse>> AddUniverseAsync(IUniverse universe);
        OASISResult<IUniverse> AddUniverse(IUniverse universe);
        Task<OASISResult<IEnumerable<IGrandSuperStar>>> GetGrandSuperStarsAsync(bool refresh = true); //Helper method which gets the GrandSuperStars at the centre of each Universe.
        OASISResult<IEnumerable<IGrandSuperStar>> GetGrandSuperStars(bool refresh = true); //Helper method which gets the GrandSuperStars at the centre of each Universe.
        Task<OASISResult<IEnumerable<IUniverse>>> GetUniversesAsync(bool refresh = true);
        OASISResult<IEnumerable<IUniverse>> GetUniverses(bool refresh = true);
    }
}