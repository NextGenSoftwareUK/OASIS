using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedMission : DownloadedSTARNETHolon, IDownloadedMission
    {
        public DownloadedMission() : base("MissionDNAJSON")
        {
            this.HolonType = HolonType.DownloadedMission;
        }
    }
}