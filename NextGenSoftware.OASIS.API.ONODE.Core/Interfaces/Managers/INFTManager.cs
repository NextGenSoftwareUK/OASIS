using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.ONode.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Managers
{
    public interface INFTManager
    {
        Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest request);
        OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest request);
        //Task<OASISResult<INFTTransactionRespone>> CreateNftTransactionAsync(INFTWalletTransactionRequest request);
        //OASISResult<INFTTransactionRespone> CreateNftTransaction(INFTWalletTransactionRequest request);
        Task<OASISResult<INFTTransactionRespone>> MintNftAsync(IMintNFTTransactionRequest request, bool isGeoNFT = false);
        OASISResult<INFTTransactionRespone> MintNft(IMintNFTTransactionRequest request, bool isGeoNFT = false);
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