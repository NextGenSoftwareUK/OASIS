using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.Objects
{
    public class CelestialBodiesAndSpaces : ICelestialBodiesAndSpaces
    {
        public IEnumerable<ICelestialSpace> CelestialSpaces { get; set; }
        public IEnumerable<ICelestialBody> CelestialBodies { get; set; }
    }

    public class CelestialBodiesAndSpaces<T1, T2> : ICelestialBodiesAndSpaces<T1, T2> where T1: ICelestialBody where T2: ICelestialSpace
    {
        public IEnumerable<T1> CelestialBodies { get; set; }
        public IEnumerable<T2> CelestialSpaces { get; set; }
    }
}