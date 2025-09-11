using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NftController : OASISControllerBase
    {
        NFTManager _NFTManager = null;
       
        NFTManager NFTManager 
        {
            get 
            {
                if (_NFTManager == null)
                    _NFTManager = new NFTManager(AvatarId);

                return _NFTManager;
            }   
        }

        public NftController()
        {
           
        }


        //[HttpPost]
        //[Route("CreateNftTransaction")]
        //public async Task<OASISResult<TransactionRespone>> CreateNftTransaction(NFTWalletTransaction request)
        //{
        //    return await NFTManager.Instance.CreateNftTransactionAsync(request);
        //}

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-id/{id}")]
        public async Task<OASISResult<IOASISNFT>> LoadNftByIdAsync(Guid id)
        {
            return await NFTManager.LoadNftAsync(id);
        }

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IOASISNFT>> LoadNftByIdAsync(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadNftByIdAsync(id);
        }

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-hash/{hash}")]
        public async Task<OASISResult<IOASISNFT>> LoadNftByHashAsync(string hash)
        {
            return await NFTManager.LoadNftAsync(hash);
        }

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-hash/{hash}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IOASISNFT>> LoadNftByHashAsync(string hash, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadNftByHashAsync(hash);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for_avatar/{avatarId}")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
        {
            return await NFTManager.LoadAllNFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for_avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllNFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for-mint-wallet-address/{mintWalletAddress}")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
        {
            return await NFTManager.LoadAllNFTsForMintAddressAsync(mintWalletAddress);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllNFTsForMintAddressAsync(mintWalletAddress);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-avatar/{avatarId}")]
        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
        {
            return await NFTManager.LoadAllGeoNFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllGeoNFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}")]
        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        {
            return await NFTManager.LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-nfts")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsAsync()
        {
            return await NFTManager.LoadAllNFTsAsync();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-nfts/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsAsync(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllNFTsAsync();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-geo-nfts")]
        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsAsync()
        {
            return await NFTManager.LoadAllGeoNFTsAsync();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-geo-nfts/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsAsync(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllGeoNFTsAsync();
        }

        [HttpPost]
        [Route("send-nft")]
        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(Models.NFT.NFTWalletTransactionRequest request)
        {
            ProviderType fromProviderType = ProviderType.None;
            ProviderType toProviderType = ProviderType.None;
            Object fromProviderTypeObject = null;
            Object toProviderTypeObject = null;

            if (Enum.TryParse(typeof(ProviderType), request.FromProviderType, out fromProviderTypeObject))
                fromProviderType = (ProviderType)fromProviderTypeObject;
            else
                return new OASISResult<INFTTransactionRespone>() { IsError = true, Message = $"The FromProviderType is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.ToProviderType, out toProviderTypeObject))
                toProviderType = (ProviderType)toProviderTypeObject;
            else
                return new OASISResult<INFTTransactionRespone>() { IsError = true, Message = $"The ToProviderType is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };

            API.Core.Objects.NFT.Request.NFTWalletTransactionRequest nftRequest = new API.Core.Objects.NFT.Request.NFTWalletTransactionRequest()
            {
                 MintWalletAddress = request.MintWalletAddress,
                 FromWalletAddress = request.FromWalletAddress,
                 ToWalletAddress = request.ToWalletAddress,
                 FromProviderType = fromProviderType,
                 ToProviderType = toProviderType,
                 Amount = request.Amount,
                 MemoText = request.MemoText
            };

            return await NFTManager.SendNFTAsync(nftRequest);
        }

        [Authorize]
        [HttpPost]
        [Route("mint-nft")]
        public async Task<OASISResult<INFTTransactionRespone>> MintNftAsync(Models.NFT.MintNFTTransactionRequest request)
        {
            ProviderType onChainProvider = ProviderType.None;
            ProviderType offChainProvider = ProviderType.None;
            NFTOffChainMetaType NFTOffChainMetaType = NFTOffChainMetaType.OASIS;
            NFTStandardType NFTStandardType = NFTStandardType.ERC1155;
            Object onChainProviderObject = null;
            Object offChainProviderObject = null;
            object NFTOffChainMetaTypeObject = null;
            object NFTStandardTypeObject = null;
            Guid sendToAvatarAfterMintingId = Guid.Empty;

            if (Enum.TryParse(typeof(ProviderType), request.OnChainProvider, out onChainProviderObject))
                onChainProvider = (ProviderType)onChainProviderObject;
            else
                return new OASISResult<INFTTransactionRespone>() { IsError = true, Message = $"The OnChainProvider is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.OffChainProvider, out offChainProviderObject))
                offChainProvider = (ProviderType)offChainProviderObject;
            else
                return new OASISResult<INFTTransactionRespone>() { IsError = true, Message = $"The OffChainProvider is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(NFTOffChainMetaType), request.NFTOffChainMetaType, out NFTOffChainMetaTypeObject))
                NFTOffChainMetaType = (NFTOffChainMetaType)NFTOffChainMetaTypeObject;
            else
                return new OASISResult<INFTTransactionRespone>() { IsError = true, Message = $"The NFTOffChainMetaType is not valid. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(NFTOffChainMetaType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(NFTStandardType), request.NFTStandardType, out NFTStandardTypeObject))
                NFTStandardType = (NFTStandardType)NFTStandardTypeObject;
            else
                return new OASISResult<INFTTransactionRespone>() { IsError = true, Message = $"The NFTStandardType is not valid. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(NFTStandardType), EnumHelperListType.ItemsSeperatedByComma)}" };

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingId) && !Guid.TryParse(request.SendToAvatarAfterMintingId, out sendToAvatarAfterMintingId))
                return new OASISResult<INFTTransactionRespone>() { IsError = true, Message = $"The SendToAvatarAfterMintingId is not valid. Please make sure it is a valid GUID!" };

            API.Core.Objects.NFT.Request.MintNFTTransactionRequest mintRequest = new API.Core.Objects.NFT.Request.MintNFTTransactionRequest()
            {
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = sendToAvatarAfterMintingId,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                MintedByAvatarId = AvatarId,
                Title = request.Title,
                Description = request.Description,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                Price = request.Price,
                Symbol = request.Symbol,
                Discount = request.Discount,
                MemoText = request.MemoText,
                NumberToMint = request.NumberToMint,
                MetaData = request.MetaData,
                OnChainProvider = new EnumValue<ProviderType>(onChainProvider),
                OffChainProvider = new EnumValue<ProviderType>(offChainProvider),
                JSONMetaDataURL = request.JSONMetaDataURL,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = NFTOffChainMetaType,
                NFTStandardType = NFTStandardType
            };

            return await NFTManager.MintNftAsync(mintRequest);
        }

        [Authorize]
        [HttpPost]
        [Route("place-geo-nft")]
        public async Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(Models.NFT.PlaceGeoSpatialNFTRequest request)
        {
            ProviderType originalOASISNFTProviderType = ProviderType.None;
            ProviderType providerType = ProviderType.None;
            Object originalOASISNFTProviderTypeObject = null;
            Object providerTypeObject = null;

            if (Enum.TryParse(typeof(ProviderType), request.OriginalOASISNFTOffChainProviderType, out originalOASISNFTProviderTypeObject))
                originalOASISNFTProviderType = (ProviderType)originalOASISNFTProviderTypeObject;
            else
                return new OASISResult<IOASISGeoSpatialNFT>() { IsError = true, Message = $"The OriginalOASISNFTOffChainProviderType is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.ProviderType, out providerTypeObject))
                providerType = (ProviderType)providerTypeObject;
            else
                return new OASISResult<IOASISGeoSpatialNFT>() { IsError = true, Message = $"The ProviderType is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };

            API.Core.Objects.NFT.Request.PlaceGeoSpatialNFTRequest placeRequest = new API.Core.Objects.NFT.Request.PlaceGeoSpatialNFTRequest()
            {
                OriginalOASISNFTId = request.OriginalOASISNFTId,
                OriginalOASISNFTOffChainProviderType = originalOASISNFTProviderType,
                Lat = request.Lat,
                Long = request.Long,
                AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect,
                PermSpawn = request.PermSpawn,
                GlobalSpawnQuantity = request.GlobalSpawnQuantity,
                PlayerSpawnQuantity = request.PlayerSpawnQuantity,
                ProviderType = providerType,
                PlacedByAvatarId = AvatarId
            };

            return await NFTManager.PlaceGeoNFTAsync(placeRequest);
        }

        [Authorize]
        [HttpPost]
        [Route("mint-and-place-geo-nft")]
        public async Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(Models.NFT.MintAndPlaceGeoSpatialNFTRequest request)
        {
            ProviderType onChainProvider = ProviderType.None;
            ProviderType offChainProvider = ProviderType.None;
            NFTOffChainMetaType NFTOffChainMetaType = NFTOffChainMetaType.OASIS;
            NFTStandardType NFTStandardType = NFTStandardType.ERC1155;
            Object onChainProviderObject = null;
            Object offChainProviderObject = null;
            object NFTOffChainMetaTypeObject = null;
            object NFTStandardTypeObject = null;
            Guid sendToAvatarAfterMintingId = Guid.Empty;

            if (Enum.TryParse(typeof(ProviderType), request.OnChainProvider, out onChainProviderObject))
                onChainProvider = (ProviderType)onChainProviderObject;
            else
                return new OASISResult<IOASISGeoSpatialNFT>() { IsError = true, Message = $"The OnChainProvider is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.OffChainProvider, out offChainProviderObject))
                offChainProvider = (ProviderType)offChainProviderObject;
            else
                return new OASISResult<IOASISGeoSpatialNFT>() { IsError = true, Message = $"The OffChainProvider is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(NFTOffChainMetaType), request.NFTOffChainMetaType, out NFTOffChainMetaTypeObject))
                NFTOffChainMetaType = (NFTOffChainMetaType)NFTOffChainMetaTypeObject;
            else
                return new OASISResult<IOASISGeoSpatialNFT>() { IsError = true, Message = $"The NFTOffChainMetaType is not valid. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(NFTOffChainMetaType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(NFTStandardType), request.NFTStandardType, out NFTStandardTypeObject))
                NFTStandardType = (NFTStandardType)NFTStandardTypeObject;
            else
                return new OASISResult<IOASISGeoSpatialNFT>() { IsError = true, Message = $"The NFTStandardType is not valid. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(NFTStandardType), EnumHelperListType.ItemsSeperatedByComma)}" };

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingId) && !Guid.TryParse(request.SendToAvatarAfterMintingId, out sendToAvatarAfterMintingId))
                return new OASISResult<IOASISGeoSpatialNFT>() { IsError = true, Message = $"The SendToAvatarAfterMintingId is not valid. Please make sure it is a valid GUID!" };


            API.Core.Objects.NFT.Request.MintAndPlaceGeoSpatialNFTRequest mintRequest = new API.Core.Objects.NFT.Request.MintAndPlaceGeoSpatialNFTRequest()
            {
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = sendToAvatarAfterMintingId,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                Title = request.Title,
                Description = request.Description,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                Price = request.Price,
                Symbol = request.Symbol,
                Discount = request.Discount,
                MemoText = request.MemoText,
                NumberToMint = request.NumberToMint,
                MetaData = request.MetaData,
                OnChainProvider = new EnumValue<ProviderType>(onChainProvider),
                OffChainProvider = new EnumValue<ProviderType>(offChainProvider),
                JSONMetaDataURL = request.JSONMetaDataURL,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = NFTOffChainMetaType,
                NFTStandardType = NFTStandardType,
                Lat = request.Lat,
                Long = request.Long,
                AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect,
                PermSpawn = request.PermSpawn,
                GlobalSpawnQuantity = request.GlobalSpawnQuantity,
                PlayerSpawnQuantity = request.PlayerSpawnQuantity,
                RespawnDurationInSeconds = request.RespawnDurationInSeconds,
                Nft2DSprite = request.Nft2DSprite,
                Nft2DSpriteURI = request.Nft2DSpriteURI,
                Nft3DObject = request.Nft3DObject,
                Nft3DObjectURI = request.Nft3DObjectURI,
                PlacedByAvatarId = AvatarId,
            };

            return await NFTManager.MintAndPlaceGeoNFTAsync(mintRequest);
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
            return NFTManager.GetNFTProvider(providerType);
        }
    }
}