using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledGame : InstalledSTARNETHolon, IInstalledGame
    {
        public InstalledGame() : base("GameDNAJSON")
        {
            this.HolonType = HolonType.InstalledGame;
        }
    }
}

