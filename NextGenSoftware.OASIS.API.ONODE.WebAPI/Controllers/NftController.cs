using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.ONode.Core.Managers;
using NextGenSoftware.OASIS.Common;

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
        [Route("send-nft")]
        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest request)
        {
            return await NFTManager.Instance.SendNFTAsync(request);
        }

        //[HttpPost]
        //[Route("CreateNftTransaction")]
        //public async Task<OASISResult<TransactionRespone>> CreateNftTransaction(NFTWalletTransaction request)
        //{
        //    return await NFTManager.Instance.CreateNftTransactionAsync(request);
        //}

        [Authorize]
        [HttpPost]
        [Route("mint-nft")]
        public async Task<OASISResult<INFTTransactionRespone>> MintNftAsync(Models.NFT.MintNFTTransactionRequest request)
        {
            ProviderType onChainProvider = ProviderType.None;
            ProviderType offChainProvider = ProviderType.None;
            Object onChainProviderObject = null;
            Object offChainProviderObject = null;

            if (Enum.TryParse(typeof(ProviderType), request.OnChainProvider, out onChainProviderObject))
                onChainProvider = (ProviderType)onChainProviderObject;
            else
                return new OASISResult<INFTTransactionRespone>() { IsError = true, Message = $"The OnChainProvider is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.OffChainProvider, out offChainProviderObject))
                offChainProvider = (ProviderType)offChainProviderObject;
            else
                return new OASISResult<INFTTransactionRespone>() { IsError = true, Message = $"The OffChainProvider is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };

            MintNFTTransactionRequest mintRequest = new MintNFTTransactionRequest()
            {
                //MintWalletAddress = request.MintWalletAddress,
                //MintedByAvatarId = request.MintedByAvatarId,
                Title = request.Title,
                Description = request.Description,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                Price = request.Price,
                Discount = request.Discount,
                MemoText = request.MemoText,
                NumberToMint = request.NumberToMint,
                MetaData = request.MetaData,
                OnChainProvider = new EnumValue<ProviderType>(onChainProvider),
                OffChainProvider = new EnumValue<ProviderType>(offChainProvider)
            };

            //if (request.MintedByAvatarId == Guid.Empty)
            //{
                IAvatar avatar = Request.HttpContext.Items["Avatar"] as IAvatar;

                if (avatar != null)
                    mintRequest.MintedByAvatarId = avatar.Id;
            //}

            return await NFTManager.Instance.MintNftAsync(mintRequest);
        }

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-id/{id}")]
        public async Task<OASISResult<IOASISNFT>> LoadNftByIdAsync(Guid id)
        {
            return await NFTManager.Instance.LoadNftAsync(id);
        }

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IOASISNFT>> LoadNftByIdAsync(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await LoadNftByIdAsync(id);
        }

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-hash/{hash}")]
        public async Task<OASISResult<IOASISNFT>> LoadNftByHashAsync(string hash)
        {
            return await NFTManager.Instance.LoadNftAsync(hash);
        }

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-hash/{hash}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IOASISNFT>> LoadNftByHashAsync(string hash, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await LoadNftByHashAsync(hash);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for_avatar/{avatarId}")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
        {
            return await NFTManager.Instance.LoadAllNFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for_avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await LoadAllNFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for_mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType, bool setGlobally = false)
        {
            return await NFTManager.Instance.LoadAllNFTsForMintAddressAsync(mintWalletAddress, providerType);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for_avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            return await NFTManager.Instance.LoadAllNFTsForAvatarAsync(avatarId, providerType);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for_mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType, bool setGlobally = false)
        {
            return await NFTManager.Instance.LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress, providerType);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-nfts/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsAsync(ProviderType providerType, bool setGlobally = false)
        {
            return await NFTManager.Instance.LoadAllNFTsAsync(providerType);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-geo-nfts/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsAsync(ProviderType providerType, bool setGlobally = false)
        {
            return await NFTManager.Instance.LoadAllGeoNFTsAsync(providerType);
        }

        [Authorize]
        [HttpPost]
        [Route("place-geo-nft")]
        public async Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        {
            return await NFTManager.Instance.PlaceGeoNFTAsync(request);
        }

        [Authorize]
        [HttpPost]
        [Route("mint-and-place-geo-nft")]
        public async Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            return await NFTManager.Instance.MintAndPlaceGeoNFTAsync(request);
        }

        //[Authorize]
        //[HttpGet]
        //[Route("get-provider-type-from-nft-provider-type/{nftProviderType}")]
        //public ProviderType GetProviderTypeFromNFTProviderType(NFTProviderType nftProviderType)
        //{
        //    return NFTManager.Instance.GetProviderTypeFromNFTProviderType(nftProviderType);
        //}

        //[HttpGet]
        //[Route("get-nft-provider-type-from-provider-type/{providerType}")]
        //public NFTProviderType GetNFTProviderTypeFromProviderType(ProviderType providerType)
        //{
        //    return NFTManager.Instance.GetNFTProviderTypeFromProviderType(providerType);
        //}

        //[HttpGet]
        //[Route("get-nft-provider-from-nft-provider-type/{nftProviderType}")]
        //public OASISResult<IOASISNFTProvider> GetNFTProviderFromNftProviderType(NFTProviderType nftProviderType)
        //{
        //    return NFTManager.Instance.GetNFTProvider(nftProviderType);
        //}

        [HttpGet]
        [Route("get-nft-provider-from-provider-type/{providerType}")]
        public OASISResult<IOASISNFTProvider> GetNFTProviderFromProviderType(ProviderType providerType)
        {
            return NFTManager.Instance.GetNFTProvider(providerType);
        }
    }
}