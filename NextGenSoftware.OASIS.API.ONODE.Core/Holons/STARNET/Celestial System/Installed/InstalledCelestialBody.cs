using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledCelestialBody : InstalledSTARNETHolon, IInstalledCelestialBody
    {
        public InstalledCelestialBody() : base("STARCelestialBodyDNAJSON")
        {
            this.HolonType = HolonType.InstalledCelestialBody;
        }
    }
}