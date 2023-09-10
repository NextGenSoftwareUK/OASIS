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
        Task<OASISResult<TransactionRespone>> CreateNftTransactionAsync(CreateNftTransactionRequest request);
        OASISResult<TransactionRespone> CreateNftTransaction(CreateNftTransactionRequest request);
        Task<OASISResult<TransactionRespone>> CreateNftTransactionAsync(INFTWalletTransaction request);
        OASISResult<TransactionRespone> CreateNftTransaction(INFTWalletTransaction request);
        Task<OASISResult<TransactionRespone>> MintNftAsync(IMintNFTTransaction request);
        OASISResult<TransactionRespone> MintNft(IMintNFTTransaction request);
        Task<OASISResult<IOASISNFT>> LoadNftAsync(Guid id, NFTProviderType NFTProviderType);
        OASISResult<IOASISNFT> LoadNft(Guid id, NFTProviderType NFTProviderType);
        OASISResult<IOASISNFTProvider> GetNFTProvider(NFTProviderType NFTProviderType, string errorMessage = "");
        OASISResult<IOASISNFTProvider> GetNFTProvider(ProviderType providerType, string errorMessage = "");
        NFTProviderType GetNFTProviderTypeFromProviderType(ProviderType providerType);
        ProviderType GetProviderTypeFromNFTProviderType(NFTProviderType nftProviderType);
    }
}