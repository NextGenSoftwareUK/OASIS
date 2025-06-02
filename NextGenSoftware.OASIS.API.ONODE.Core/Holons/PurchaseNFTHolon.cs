using System;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class PurchaseNFTHolon : Holon
    {
        [CustomOASISProperty]
        public string WalletAddress { get; set; }

        [CustomOASISProperty]
        public string AvatarUsername { get; set; }

        [CustomOASISProperty]
        public Guid AvatarId { get; set; }

        [CustomOASISProperty]
        public string JsonSelectedTiles { get; set; }
    }
}