using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.ONode.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Managers
{
    public interface INFTManager
    {
        Task<OASISResult<NFTTransactionRespone>> CreateNftTransactionAsync(CreateNftTransactionRequest request);
        OASISResult<NFTTransactionRespone> CreateNftTransaction(CreateNftTransactionRequest request);
        Task<OASISResult<NFTTransactionRespone>> CreateNftTransactionAsync(INFTWalletTransaction request);
        OASISResult<NFTTransactionRespone> CreateNftTransaction(INFTWalletTransaction request);
        Task<OASISResult<NFTTransactionRespone>> MintNftAsync(IMintNFTTransaction request);
        OASISResult<NFTTransactionRespone> MintNft(IMintNFTTransaction request);
        Task<OASISResult<IOASISNFT>> LoadNftAsync(Guid id, ProviderType providerType);
        OASISResult<IOASISNFT> LoadNft(Guid id, ProviderType providerType);
        Task<OASISResult<IOASISNFT>> LoadNftAsync(string hash, ProviderType providerType);
        OASISResult<IOASISNFT> LoadNft(string hash, ProviderType providerType);
        OASISResult<IOASISNFTProvider> GetNFTProvider(NFTProviderType NFTProviderType, string errorMessage = "");
        OASISResult<IOASISNFTProvider> GetNFTProvider(ProviderType providerType, string errorMessage = "");
        NFTProviderType GetNFTProviderTypeFromProviderType(ProviderType providerType);
        ProviderType GetProviderTypeFromNFTProviderType(NFTProviderType nftProviderType);
    }
}