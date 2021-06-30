using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IUniverse : ICelestialSpace
    {
        List<IDimension> Dimensions { get; set; } //TODO: Think SuperStars at the centre of each Galaxy let you jump between dimensions so should dimensions be inside Galaxies? But they also let you jump to different Galaxies within that Universe so think ok how it is? :)
    }
}