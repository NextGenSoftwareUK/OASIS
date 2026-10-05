using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialBodiesAndSpaces
    {
        IEnumerable<ICelestialBody> CelestialBodies { get; set; }
        IEnumerable<ICelestialSpace> CelestialSpaces { get; set; }
    }

    public interface ICelestialBodiesAndSpaces<T1, T2> where T1: ICelestialBody where T2 : ICelestialSpace
    {
        IEnumerable<T1> CelestialBodies { get; set; }
        IEnumerable<T2> CelestialSpaces { get; set; }
    }
}