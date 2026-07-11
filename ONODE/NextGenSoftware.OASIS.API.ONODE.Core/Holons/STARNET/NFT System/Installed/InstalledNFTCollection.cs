using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledNFTCollection : InstalledSTARNETHolon, IInstalledNFTCollection
    {
        public InstalledNFTCollection() : base("STARNFTCollectionDNAJSON")
        {
            this.HolonType = HolonType.InstalledNFTCollection;
        }

        [CustomOASISProperty]
        public NFTCollectionType NFTCollectionType { get; set; }
    }
}
