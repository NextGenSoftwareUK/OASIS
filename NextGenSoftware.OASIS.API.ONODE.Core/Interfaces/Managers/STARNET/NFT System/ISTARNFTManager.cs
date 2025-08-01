using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ISTARNFTManager : ISTARNETManagerBase<STARNFT, DownloadedNFT, InstalledNFT, NFTDNA>
    {
        OASISResult<ISTARNFT> CreateNFT(Guid avatarId, string name, string description, string fullPathToNFTSource, NFTType nftType, Guid OASISNFTId, bool checkIfSourcePathExists = true,  ProviderType providerType = ProviderType.Default);
        OASISResult<ISTARNFT> CreateNFT(Guid avatarId, string name, string description, string fullPathToNFTSource, NFTType nftType, IOASISNFT OASISNFT, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARNFT>> CreateNFTAsync(Guid avatarId, string name, string description, string fullPathToNFTSource, NFTType nftType, Guid OASISNFTId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARNFT>> CreateNFTAsync(Guid avatarId, string name, string description, string fullPathToNFTSource, NFTType nftType, IOASISNFT OASISNFT, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
    }
}