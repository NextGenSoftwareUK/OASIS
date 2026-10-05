using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public class InstalledGeoNFTCollection : InstalledSTARNETHolon, IInstalledGeoNFTCollection
    {
        public InstalledGeoNFTCollection() : base("GeoNFTCollectionDNAJSON")
        {
            this.HolonType = HolonType.InstalledGeoNFTCollection;
        }

        [CustomOASISProperty]
        public NFTType NFTCollectionType { get; set; }
    }
}
