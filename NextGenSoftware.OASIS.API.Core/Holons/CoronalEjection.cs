
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class CoronalEjection
    {
        public string Message { get; set; }
        public bool ErrorOccured { get; set; }
        public ICelestialBody CelestialBody { get; set; }
    }
}
