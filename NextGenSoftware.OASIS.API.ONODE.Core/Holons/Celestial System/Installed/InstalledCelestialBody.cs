using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledCelestialBody : InstalledSTARHolon, IInstalledCelestialBody
    {
        public InstalledCelestialBody() : base("CelestialBodyDNAJSON")
        {
            this.HolonType = HolonType.InstalledCelestialBody;
        }
    }
}