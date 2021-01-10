
namespace NextGenSoftware.OASIS.API.Core
{
    public class CoronalEjection
    {
        public string Message { get; set; }
        public bool ErrorOccured { get; set; }
        public ICelestialBody CelestialBody { get; set; }
    }
}
