using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IOmiverse : ICelestialSpace
    {
        IGreatGrandSuperStar GreatGrandSuperStar { get; set; } //lets you jump between Multiverses.
        IOmniverseDimensions Dimensions { get; set; }
        List<IMultiverse> Multiverses { get; set; }
    }
}