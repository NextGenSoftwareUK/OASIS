using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedOAPP : DownloadedSTARNETHolon, IDownloadedOAPP
    {
        public DownloadedOAPP() : base("OAPPDNAJSON")
        {
            this.HolonType = HolonType.DownloadedOAPP;
        }
    }
}