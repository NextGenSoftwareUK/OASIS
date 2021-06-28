using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IDimension : ICelestialSpace
    {
        public int DimensionLevel { get; set; }
        List<IGalaxyCluster> GalaxyClusters { get; set; }
    }
}