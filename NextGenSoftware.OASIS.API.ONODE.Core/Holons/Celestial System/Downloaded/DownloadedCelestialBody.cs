using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedCelestialBody : DownloadedSTARHolon, IDownloadedCelestialBody
    {
        public DownloadedCelestialBody() : base("CelestialBodyDNAJSON")
        {
            this.HolonType = HolonType.DownloadedCelestialBody;
        }
    }
}