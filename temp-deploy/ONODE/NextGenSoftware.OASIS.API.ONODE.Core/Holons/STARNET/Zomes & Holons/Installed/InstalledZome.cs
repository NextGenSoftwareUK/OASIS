using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledZome : InstalledSTARNETHolon, IInstalledZome
    {
        public InstalledZome() : base("STARZomeDNAJSON")
        {
            this.HolonType = HolonType.InstalledZome;
        }
    }
}