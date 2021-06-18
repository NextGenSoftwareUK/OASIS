using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IOmiverse : IHolon
    {
        IGreatGrandSuperStar GreatGrandSuperStar { get; set; }
        List<IUniverse> Universes { get; set; }
    }
}