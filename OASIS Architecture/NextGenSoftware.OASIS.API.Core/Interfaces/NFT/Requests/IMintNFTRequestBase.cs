using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IMintNFTRequestBase : IMintWeb4TokenRequest
    {
        public string JSONMetaDataURL { get; set; }
        public string JSONMetaData { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool? IsForSale { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }
    }
}