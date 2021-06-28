using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IGalaxyCluster : ICelestialSpace
    {
        List<IGalaxy> Galaxies { get; set; }
        List<IStar> Stars { get; set; }
        bool IsSuperCluster { get; }
    }
}