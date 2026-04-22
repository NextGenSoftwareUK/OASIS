using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    public class CollectGeoNFTRequest : ICollectGeoNFTRequest
    {
        public Guid CollectedByAvatarId { get; set; }
        public Guid GeoNFTId { get; set; }
        //public InventoryItemType ItemType { get; set; }
        public string GameSource { get; set; }
        public byte[] Image2D { get; set; }
        public string Image2DURI { get; set; }
        public byte[] Object3D { get; set; }
        public string Object3DURI { get; set; }
        public int Quantity { get; set; }
        public bool Stack { get; set; }
    }
}