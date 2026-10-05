using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class RemintWeb4GeoNFTRequest : NFTOptions, IRemintWeb4GeoNFTRequest
    {
        public IWeb4GeoSpatialNFT Web4GeoNFT { get; set; }
        public IList<IMintWeb3NFTRequest> Web3NFTs { get; set; }
    }
}