using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONode.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Managers
{
    public interface INFTManager
    {
        Task<OASISResult<INFTTransactionRespone>> CreateNftTransactionAsync(CreateNftTransactionRequest request);
        OASISResult<INFTTransactionRespone> CreateNftTransaction(CreateNftTransactionRequest request);
        Task<OASISResult<INFTTransactionRespone>> CreateNftTransactionAsync(INFTWalletTransaction request);
        OASISResult<INFTTransactionRespone> CreateNftTransaction(INFTWalletTransaction request);
        Task<OASISResult<INFTTransactionRespone>> MintNftAsync(IMintNFTTransaction request);
        OASISResult<INFTTransactionRespone> MintNft(IMintNFTTransaction request);
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