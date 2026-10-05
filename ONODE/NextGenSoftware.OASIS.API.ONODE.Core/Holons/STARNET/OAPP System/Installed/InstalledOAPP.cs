using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledOAPP : InstalledSTARNETHolon, IInstalledOAPP
    {
        public InstalledOAPP() : base("OAPPDNAJSON")
        {
            this.HolonType = HolonType.InstalledOAPP;
        }
    }
}
