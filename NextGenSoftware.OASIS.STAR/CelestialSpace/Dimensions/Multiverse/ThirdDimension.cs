using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    // Physical Plane
    public class ThirdDimension : Dimension, IThirdDimension
    {
        //Parallel Universes (everything that can happen does happen (Quantum Mechanics)).
        public List<IUniverse> Universes { get; set; }

        public ThirdDimension()
        {
            this.DimensionLevel = DimensionLevel.Third;
        }
    }
}