using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledQuest : InstalledSTARNETHolon, IInstalledQuest
    {
        public InstalledQuest() : base("QuestDNAJSON")
        {
            this.HolonType = HolonType.InstalledQuest;
        }
    }
}