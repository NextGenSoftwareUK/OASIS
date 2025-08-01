using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedCelestialSpace : DownloadedSTARNETHolon, IDownloadedCelestialSpace
    {
        public DownloadedCelestialSpace() : base("STARCelestialSpaceDNAJSON")
        {
            this.HolonType = HolonType.DownloadedCelestialSpace;
        }
    }
}