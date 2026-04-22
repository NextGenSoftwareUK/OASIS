using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Requests
{
    public interface ICollectGeoNFTRequest
    {
        Guid CollectedByAvatarId { get; set; }
        string GameSource { get; set; }
        Guid GeoNFTId { get; set; }
        byte[] Image2D { get; set; }
        Uri Image2DURI { get; set; }
        //InventoryItemType ItemType { get; set; }
        byte[] Object3D { get; set; }
        Uri Object3DURI { get; set; }
        int Quantity { get; set; }
        bool Stack { get; set; }
    }
}