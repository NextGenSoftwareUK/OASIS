using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // At the centre of the Omiverse (creates Universes (with a GrandSuperStar at the centre of each).
    public interface IGreatGrandSuperStarCore : IGrandSuperStarCore
    {
        IGreatGrandSuperStar GreatGrandSuperStar { get; set; }
        Task<IGrandSuperStar> AddGrandSuperStarAsync(IGrandSuperStar star);
        Task<IUniverse> AddUniveseAsync(IUniverse star);
        Task<List<IGrandSuperStar>> GetGrandSuperStars();
        Task<List<IUniverse>> GetUniverses();
    }
}