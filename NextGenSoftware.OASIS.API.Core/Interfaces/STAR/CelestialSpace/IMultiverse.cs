using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IMultiverse : ICelestialSpace
    {
        IGrandSuperStar GrandSuperStar { get; set; } //Lets you jump between universes within this multiverse.
        List<IUniverse> Universes { get; set; }
    }
}