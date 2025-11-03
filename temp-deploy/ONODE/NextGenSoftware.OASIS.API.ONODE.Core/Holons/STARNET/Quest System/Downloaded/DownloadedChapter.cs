using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedChapter : DownloadedSTARNETHolon, IDownloadedChapter
    {
        public DownloadedChapter() : base("ChapterDNAJSON")
        {
            this.HolonType = HolonType.DownloadedChapter;
        }
    }
}