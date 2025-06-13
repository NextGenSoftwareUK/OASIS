using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedNFT : DownloadedSTARHolon, IDownloadedNFT
    {
        public DownloadedNFT() : base("NFTDNAJSON")
        {
            this.HolonType = HolonType.DownloadedNFT;
        }
    }
}