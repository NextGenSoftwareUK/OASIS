using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ISTARGeoNFTManager
    {
        OASISResult<ISTARGeoNFT> CreateGeoNFT(Guid avatarId, string name, string description, string fullPathToGeoNFTSource, NFTType nftType, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default);
        OASISResult<ISTARGeoNFT> CreateGeoNFT(Guid avatarId, string name, string description, string fullPathToGeoNFTSource, NFTType nftType, IOASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARGeoNFT>> CreateGeoNFTAsync(Guid avatarId, string name, string description, string fullPathToGeoNFTSource, NFTType nftType, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARGeoNFT>> CreateGeoNFTAsync(Guid avatarId, string name, string description, string fullPathToGeoNFTSource, NFTType nftType, IOASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default);
    }
}