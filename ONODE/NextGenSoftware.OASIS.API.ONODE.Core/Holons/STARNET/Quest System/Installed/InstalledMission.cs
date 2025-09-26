using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledMission : InstalledSTARNETHolon, IInstalledMission
    {
        public InstalledMission() : base("MissionDNAJSON")
        {
            this.HolonType = HolonType.InstalledMission;
        }
    }
}