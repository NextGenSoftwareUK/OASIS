using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IUniverse : ICelestialSpace
    {
        //IGrandSuperStar GrandSuperStar { get; set; } //TODO: Will now be ParentGrandSuperStar found at the ParentMultiverse?
        //List<IGalaxy> Galaxies { get; set; }
        //List<IStar> Stars { get; set; }
        List<IDimension> Dimensions { get; set; } //TODO: Think SuperStars at the centre of each Galaxy let you jump between dimensions so should dimensions be inside Galaxies? But they also let you jump to different Galaxies within that Universe so think ok how it is? :)

        //TODO; Technically a Universe can have SolarSystems, stars & planets that do not belong to a Universe but not sure it would work within the OASIS COSMIC Object Model?
        //public List<ISolarSystem> SolarSystems { get; set; }
        //public List<IPlanet> Planets { get; set; }
    }
}