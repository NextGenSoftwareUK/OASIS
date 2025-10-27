using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedNFTCollection : DownloadedSTARNETHolon, IDownloadedNFTCollection
    {
        public DownloadedNFTCollection() : base("NFTCollectionDNAJSON")
        {
            this.HolonType = HolonType.DownloadedNFTCollection;
        }

        [CustomOASISProperty]
        public NFTCollectionType NFTCollectionType { get; set; }
    }
}