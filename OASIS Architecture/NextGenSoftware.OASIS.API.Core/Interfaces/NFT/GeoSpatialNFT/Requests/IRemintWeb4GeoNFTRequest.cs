using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request
{
    public interface IRemintWeb4GeoNFTRequest : INFTOptions
    {
        IWeb4GeoSpatialNFT Web4GeoNFT { get; set; }
        IList<IMintWeb3NFTRequest> Web3NFTs { get; set; }
    }
}