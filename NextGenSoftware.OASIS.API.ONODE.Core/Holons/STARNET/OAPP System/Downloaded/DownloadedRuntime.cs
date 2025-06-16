using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedRuntime : DownloadedSTARHolon, IDownloadedRuntime
    {
        public DownloadedRuntime() : base("RuntimeDNAJSON")
        {
            this.HolonType = HolonType.DownloadedRuntime;
        }
    }
}