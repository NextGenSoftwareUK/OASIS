using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedQuest : DownloadedSTARNETHolon, IDownloadedQuest
    {
        public DownloadedQuest() : base("QuestDNAJSON")
        {
            this.HolonType = HolonType.DownloadedQuest;
        }
    }
}