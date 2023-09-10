using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.ONode.Core.Managers;
using NextGenSoftware.OASIS.API.ONode.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NftController : OASISControllerBase
    {
        public NftController()
        {
           
        }

        [HttpPost]
        [Route("create-nft-transaction")]
        public async Task<OASISResult<TransactionRespone>> CreateNftTransactionAsync(CreateNftTransactionRequest request)
        {
            return await NFTManager.Instance.CreateNftTransactionAsync(request);
        }

        //[HttpPost]
        //[Route("CreateNftTransaction")]
        //public async Task<OASISResult<TransactionRespone>> CreateNftTransaction(NFTWalletTransaction request)
        //{
        //    return await NFTManager.Instance.CreateNftTransactionAsync(request);
        //}

        [HttpPost]
        [Route("mint-nft")]
        public async Task<OASISResult<TransactionRespone>> MintNftAsync(MintNFTTransaction request)
        {
            return await NFTManager.Instance.MintNftAsync(request);
        }

        [HttpGet]
        [Route("load-nft-by-id/{id}/{nftProviderType}")]
        public async Task<OASISResult<IOASISNFT>> LoadNftByIdAsync(Guid id, NFTProviderType nftProviderType)
        {
            return await NFTManager.Instance.LoadNftAsync(id, nftProviderType);
        }

        [HttpGet]
        [Route("load-nft-by-hash/{hash}/{nftProviderType}")]
        public async Task<OASISResult<IOASISNFT>> LoadNftByHashAsync(string hash, NFTProviderType nftProviderType)
        {
            return await NFTManager.Instance.LoadNftAsync(hash, nftProviderType);
        }

        [HttpGet]
        [Route("load-all-nfts-for_avatar/{avatarId}/{nftProviderType}")]
        public async Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId, NFTProviderType nftProviderType)
        {
            return await NFTManager.Instance.LoadAllNFTsForAvatarAsync(avatarId, nftProviderType);
        }

        [HttpGet]
        [Route("load-all-nfts-for_mint-wallet-address/{mintWalletAddress}/{nftProviderType}")]
        public async Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress, NFTProviderType nftProviderType)
        {
            return await NFTManager.Instance.LoadAllNFTsForMintAddressAsync(mintWalletAddress, nftProviderType);
        }

        [HttpGet]
        [Route("load-all-geo-nfts-for_avatar/{avatarId}/{nftProviderType}")]
        public async Task<OASISResult<List<IOASISNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId, NFTProviderType nFTProviderType)
        {
            return await NFTManager.Instance.LoadAllNFTsForAvatarAsync(avatarId, nFTProviderType);
        }

        [HttpGet]
        [Route("load-all-geo-nfts-for_mint-wallet-address/{mintWalletAddress}/{nftProviderType}")]
        public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress, NFTProviderType nftProviderType)
        {
            return await NFTManager.Instance.LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress, nftProviderType);
        }

        [HttpPost]
        [Route("place-geo-nft")]
        public async Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        {
            return await NFTManager.Instance.PlaceGeoNFTAsync(request);
        }

        [HttpPost]
        [Route("mint-and-place-geo-nft")]
        public async Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            return await NFTManager.Instance.MintAndPlaceGeoNFTAsync(request);
        }

        [HttpGet]
        [Route("get-provider-type-from-nft-provider-type/{nftProviderType}")]
        public ProviderType GetProviderTypeFromNFTProviderType(NFTProviderType nftProviderType)
        {
            return NFTManager.Instance.GetProviderTypeFromNFTProviderType(nftProviderType);
        }

        [HttpGet]
        [Route("get-nft-provider-type-from-provider-type/{providerType}")]
        public NFTProviderType GetNFTProviderTypeFromProviderType(ProviderType providerType)
        {
            return NFTManager.Instance.GetNFTProviderTypeFromProviderType(providerType);
        }

        [HttpGet]
        [Route("get-nft-provider-from-nft-provider-type/{nftProviderType}")]
        public OASISResult<IOASISNFTProvider> GetNFTProviderFromNftProviderType(NFTProviderType nftProviderType)
        {
            return NFTManager.Instance.GetNFTProvider(nftProviderType);
        }

        [HttpGet]
        [Route("get-nft-provider-from-provider-type/{providerType}")]
        public OASISResult<IOASISNFTProvider> GetNFTProviderFromNftProviderType(ProviderType providerType)
        {
            return NFTManager.Instance.GetNFTProvider(providerType);
        }
    }
}