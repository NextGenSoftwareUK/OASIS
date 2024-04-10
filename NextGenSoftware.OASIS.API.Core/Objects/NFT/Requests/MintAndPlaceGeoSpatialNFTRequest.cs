﻿using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    //public class MintAndPlaceGeoSpatialNFTRequest : PlaceGeoSpatialNFTRequestBase, MintNFTTransactionRequest, IMintAndPlaceGeoSpatialNFTRequest
    public class MintAndPlaceGeoSpatialNFTRequest : PlaceGeoSpatialNFTRequestBase, IMintAndPlaceGeoSpatialNFTRequest
    {
        //If c# supported multi-inheritence then we wouldnt need to include the mint props again here! ;-)
        public string MintWalletAddress { get; set; }
        public Guid MintedByAvatarId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string MemoText { get; set; }
        // public string Token { get; set; } //TODO: Should be dervied from the OnChainProvider so may not need this?
        public int NumberToMint { get; set; }
        public bool StoreNFTMetaDataOnChain { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
        public EnumValue<ProviderType> OffChainProvider { get; set; }
        public EnumValue<ProviderType> OnChainProvider { get; set; }
    }
}