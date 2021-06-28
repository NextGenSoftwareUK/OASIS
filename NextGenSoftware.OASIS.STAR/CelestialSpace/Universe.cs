using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Universe : Holon, IUniverse
    {
        //public IGrandSuperStar GrandSuperStar { get; set; } //TODO: Will now be ParentGrandSuperStar found at the ParentMultiverse?
        //public List<IGalaxy> Galaxies { get; set; }
        //public List<IStar> Stars { get; set; }

        //TODO: Need to add Dimensions in somewhere? Maybe have Dimensions collection here? With the clusters/superclusters inside them and then Galaxies, etc inside them...
        public List<IDimension> Dimensions { get; set; } //TODO: Think SuperStars at the centre of each Galaxy let you jump between dimensions so should dimensions be inside Galaxies? But they also let you jump to different Galaxies within that Universe so think ok how it is? :)


        //TODO; Technically a Universe can have SolarSystems stars & planets that do not belong to a Universe but not sure it would work within the OASIS COSMIC Object Model?
        //public List<ISolarSystem> SolarSystems { get; set; }
        //public List<IStar> Stars { get; set; }
        //public List<IPlanet> Planets { get; set; }
    }
}