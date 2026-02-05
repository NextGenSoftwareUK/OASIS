using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledChapter : InstalledSTARNETHolon, IInstalledChapter
    {
        public InstalledChapter() : base("ChapterDNAJSON")
        {
            this.HolonType = HolonType.InstalledChapter;
        }
    }
}
