
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class CoronalEjection
    {
        public string Message { get; set; }
        public bool ErrorOccured { get; set; }
        public ICelestialBody CelestialBody { get; set; }
        public ICelestialSpace CelestialSpace { get; set; }
    }
}
