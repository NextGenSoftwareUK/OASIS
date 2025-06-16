using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledChapter : InstalledSTARNETHolon, IInstalledChapter
    {
        public InstalledChapter() : base("InstalledDNAJSON")
        {
            this.HolonType = HolonType.InstalledChapter;
        }
    }
}
