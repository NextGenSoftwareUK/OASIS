using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ISTARGeoNFTManager : ISTARNETManagerBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, GeoNFTDNA>
    {
        //OASISResult<ISTARGeoNFT> CreateGeoNFT(Guid avatarId, string name, string description, string fullPathToGeoNFTSource, NFTType nftType, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default);
        //OASISResult<ISTARGeoNFT> CreateGeoNFT(Guid avatarId, string name, string description, string fullPathToGeoNFTSource, NFTType nftType, IOASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARGeoNFT>> CreateGeoNFTAsync(Guid avatarId, string name, string description, string fullPathToGeoNFTSource, NFTType nftType, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARGeoNFT>> CreateGeoNFTAsync(Guid avatarId, string name, string description, string fullPathToGeoNFTSource, NFTType nftType, IOASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default);
    }
}