using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedHolon : DownloadedSTARNETHolon, IDownloadedHolon
    {
        public DownloadedHolon() : base("STARHolonDNAJSON")
        {
            this.HolonType = HolonType.DownloadedHolon;
        }
    }
}