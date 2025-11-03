using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedCelestialBody : DownloadedSTARNETHolon, IDownloadedCelestialBody
    {
        public DownloadedCelestialBody() : base("STARCelestialBodyDNAJSON")
        {
            this.HolonType = HolonType.DownloadedCelestialBody;
        }
    }
}