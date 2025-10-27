using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledHolon : InstalledSTARNETHolon, IInstalledHolon
    {
        public InstalledHolon() : base("STARHolonDNAJSON")
        {
            this.HolonType = HolonType.InstalledHolon;
        }
    }
}