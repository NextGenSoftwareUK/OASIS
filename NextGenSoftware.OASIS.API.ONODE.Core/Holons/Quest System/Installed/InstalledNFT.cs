using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledNFT : InstalledSTARHolon, IInstalledNFT
    {
        public InstalledNFT() : base("NFTDNAJSON")
        {
            this.HolonType = HolonType.InstalledNFT;
        }
    }
}
