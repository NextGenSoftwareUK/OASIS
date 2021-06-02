using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IUniverse
    {
        IGrandSuperStar GrandSuperStar { get; set; }
        List<IGalaxy> Galaxies { get; set; }
    }
}