using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
    /// <summary>
    /// NFT (Non-Fungible Token) management endpoints for creating, managing, and trading digital assets.
    /// Provides comprehensive NFT functionality including minting, transferring, and metadata management.
    /// </summary>
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

        /// <summary>
        /// Loads an NFT by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to load.</param>
        /// <returns>OASIS result containing the NFT details or error information.</returns>
        /// <response code="200">NFT loaded successfully</response>
        /// <response code="400">Error loading NFT</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet]
        [Route("load-nft-by-id/{id}")]
        [ProducesResponseType(typeof(OASISResult<IWeb4NFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<IWeb4NFT>> LoadWeb4NftByIdAsync(Guid id)
        {
            return await NFTManager.LoadWeb4NftAsync(id);
        }

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IWeb4NFT>> LoadWeb4NftByIdAsync(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadWeb4NftByIdAsync(id);
        }

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-hash/{hash}")]
        public async Task<OASISResult<IWeb4NFT>> LoadWeb4NftByHashAsync(string hash)
        {
            return await NFTManager.LoadWeb4NftAsync(hash);
        }

        [Authorize]
        [HttpGet]
        [Route("load-nft-by-hash/{hash}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IWeb4NFT>> LoadWeb4NftByHashAsync(string hash, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadWeb4NftByHashAsync(hash);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for_avatar/{avatarId}")]
        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsForAvatarAsync(Guid avatarId)
        {
            return await NFTManager.LoadAllWeb4NFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for_avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsForAvatarAsync(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllWeb4NFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for-mint-wallet-address/{mintWalletAddress}")]
        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsForMintAddressAsync(string mintWalletAddress)
        {
            return await NFTManager.LoadAllWeb4NFTsForMintAddressAsync(mintWalletAddress);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllWeb4NFTsForMintAddressAsync(mintWalletAddress);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-avatar/{avatarId}")]
        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllWeb4GeoNFTsForAvatarAsync(Guid avatarId)
        {
            return await NFTManager.LoadAllWeb4GeoNFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllWeb4GeoNFTsForAvatarAsync(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllWeb4GeoNFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}")]
        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        {
            return await NFTManager.LoadAllWeb4GeoNFTsForMintAddressAsync(mintWalletAddress);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-nfts")]
        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsAsync()
        {
            return await NFTManager.LoadAllWeb4NFTsAsync();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-nfts/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsAsync(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllWeb4NFTsAsync();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-geo-nfts")]
        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsAsync()
        {
            return await NFTManager.LoadAllWeb4GeoNFTsAsync();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-geo-nfts/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllWeb4GeoNFTsAsync(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllGeoNFTsAsync();
        }

        [HttpPost]
        [Route("send-nft")]
        public async Task<OASISResult<IWeb3NFTTransactionRespone>> SendNFTAsync(Models.NFT.NFTWalletTransactionRequest request)
        {
            ProviderType fromProviderType = ProviderType.None;
            ProviderType toProviderType = ProviderType.None;
            Object fromProviderTypeObject = null;
            Object toProviderTypeObject = null;

            if (Enum.TryParse(typeof(ProviderType), request.FromProvider, out fromProviderTypeObject))
                fromProviderType = (ProviderType)fromProviderTypeObject;
            else
                return new OASISResult<IWeb3NFTTransactionRespone>() { IsError = true, Message = $"The FromProvider is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.ToProvider, out toProviderTypeObject))
                toProviderType = (ProviderType)toProviderTypeObject;
            else
                return new OASISResult<IWeb3NFTTransactionRespone>() { IsError = true, Message = $"The ToProvider is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };

            API.Core.Objects.NFT.Request.Web4NFTWalletTransactionRequest nftRequest = new API.Core.Objects.NFT.Request.Web4NFTWalletTransactionRequest()
            {
                 //MintWalletAddress = request.MintWalletAddress,
                 FromWalletAddress = request.FromWalletAddress,
                 ToWalletAddress = request.ToWalletAddress,
                 FromProvider = new EnumValue<ProviderType>(fromProviderType),
                 ToProvider = new EnumValue<ProviderType>(toProviderType),
                 Amount = request.Amount,
                 MemoText = request.MemoText,
                 WaitTillNFTSent = request.WaitTillNFTSent,
                 WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                 AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds
            };

            return await NFTManager.SendNFTAsync(nftRequest);
        }

        [Authorize]
        [HttpPost]
        [Route("mint-nft")]
        public async Task<OASISResult<IWeb4NFT>> MintNftAsync(Models.NFT.MintNFTTransactionRequest request)
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
                return new OASISResult<IWeb4NFT>() { IsError = true, Message = $"The OnChainProvider is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.OffChainProvider, out offChainProviderObject))
                offChainProvider = (ProviderType)offChainProviderObject;
            else
                return new OASISResult<IWeb4NFT>() { IsError = true, Message = $"The OffChainProvider is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(NFTOffChainMetaType), request.NFTOffChainMetaType, out NFTOffChainMetaTypeObject))
                NFTOffChainMetaType = (NFTOffChainMetaType)NFTOffChainMetaTypeObject;
            else
                return new OASISResult<IWeb4NFT>() { IsError = true, Message = $"The NFTOffChainMetaType is not valid. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(NFTOffChainMetaType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(NFTStandardType), request.NFTStandardType, out NFTStandardTypeObject))
                NFTStandardType = (NFTStandardType)NFTStandardTypeObject;
            else
                return new OASISResult<IWeb4NFT>() { IsError = true, Message = $"The NFTStandardType is not valid. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(NFTStandardType), EnumHelperListType.ItemsSeperatedByComma)}" };

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingId) && !Guid.TryParse(request.SendToAvatarAfterMintingId, out sendToAvatarAfterMintingId))
                return new OASISResult<IWeb4NFT>() { IsError = true, Message = $"The SendToAvatarAfterMintingId is not valid. Please make sure it is a valid GUID!" };

            API.Core.Objects.NFT.Request.MintWeb4NFTRequest mintRequest = new API.Core.Objects.NFT.Request.MintWeb4NFTRequest()
            {
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
                NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(NFTOffChainMetaType),
                NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType),
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = sendToAvatarAfterMintingId,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                WaitTillNFTSent = request.WaitTillNFTSent,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds
            };

            return await NFTManager.MintNftAsync(mintRequest, false, Core.Enums.ResponseFormatType.SimpleText);
        }

        [Authorize]
        [HttpPost]
        [Route("place-geo-nft")]
        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> PlaceGeoNFTAsync(Models.NFT.PlaceGeoSpatialNFTRequest request)
        {
            ProviderType originalOASISNFTProviderType = ProviderType.None;
            ProviderType geoNFTMetaDataProvider = ProviderType.None;
            Object originalOASISNFTProviderTypeObject = null;
            Object geoNFTMetaDataProviderObject = null;

            if (Enum.TryParse(typeof(ProviderType), request.OriginalOASISNFTOffChainProvider, out originalOASISNFTProviderTypeObject))
                originalOASISNFTProviderType = (ProviderType)originalOASISNFTProviderTypeObject;
            else
                return new OASISResult<IWeb4OASISGeoSpatialNFT>() { IsError = true, Message = $"The OriginalOASISNFTOffChainProviderType is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.GeoNFTMetaDataProvider, out geoNFTMetaDataProviderObject))
                geoNFTMetaDataProvider = (ProviderType)geoNFTMetaDataProviderObject;
            else
                return new OASISResult<IWeb4OASISGeoSpatialNFT>() { IsError = true, Message = $"The ProviderType is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };

            API.Core.Objects.NFT.Request.PlaceWeb4GeoSpatialNFTRequest placeRequest = new API.Core.Objects.NFT.Request.PlaceWeb4GeoSpatialNFTRequest()
            {
                OriginalWeb4OASISNFTId = request.OriginalOASISNFTId,
                OriginalWeb4OASISNFTOffChainProvider = new EnumValue<ProviderType>(originalOASISNFTProviderType),
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
                GeoNFTMetaDataProvider = new EnumValue<ProviderType>(geoNFTMetaDataProvider)
            };

            return await NFTManager.PlaceWeb4GeoNFTAsync(placeRequest, Core.Enums.ResponseFormatType.SimpleText);
        }

        [Authorize]
        [HttpPost]
        [Route("mint-and-place-geo-nft")]
        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(Models.NFT.MintAndPlaceGeoSpatialNFTRequest request)
        {
            ProviderType onChainProvider = ProviderType.None;
            ProviderType offChainProvider = ProviderType.None;
            ProviderType geoNFTMetaDataProvider = ProviderType.None;
            NFTOffChainMetaType NFTOffChainMetaType = NFTOffChainMetaType.OASIS;
            NFTStandardType NFTStandardType = NFTStandardType.ERC1155;
            Object onChainProviderObject = null;
            Object offChainProviderObject = null;
            Object geoNFTMetaDataProviderObject = null;
            object NFTOffChainMetaTypeObject = null;
            object NFTStandardTypeObject = null;
            Guid sendToAvatarAfterMintingId = Guid.Empty;

            if (Enum.TryParse(typeof(ProviderType), request.OnChainProvider, out onChainProviderObject))
                onChainProvider = (ProviderType)onChainProviderObject;
            else
                return new OASISResult<IWeb4OASISGeoSpatialNFT>() { IsError = true, Message = $"The OnChainProvider is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.OffChainProvider, out offChainProviderObject))
                offChainProvider = (ProviderType)offChainProviderObject;
            else
                return new OASISResult<IWeb4OASISGeoSpatialNFT>() { IsError = true, Message = $"The OffChainProvider is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.GeoNFTMetaDataProvider, out geoNFTMetaDataProviderObject))
                geoNFTMetaDataProvider = (ProviderType)geoNFTMetaDataProviderObject;
            else
                return new OASISResult<IWeb4OASISGeoSpatialNFT>() { IsError = true, Message = $"The GeoNFTMetaDataProvider is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(NFTOffChainMetaType), request.NFTOffChainMetaType, out NFTOffChainMetaTypeObject))
                NFTOffChainMetaType = (NFTOffChainMetaType)NFTOffChainMetaTypeObject;
            else
                return new OASISResult<IWeb4OASISGeoSpatialNFT>() { IsError = true, Message = $"The NFTOffChainMetaType is not valid. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(NFTOffChainMetaType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(NFTStandardType), request.NFTStandardType, out NFTStandardTypeObject))
                NFTStandardType = (NFTStandardType)NFTStandardTypeObject;
            else
                return new OASISResult<IWeb4OASISGeoSpatialNFT>() { IsError = true, Message = $"The NFTStandardType is not valid. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(NFTStandardType), EnumHelperListType.ItemsSeperatedByComma)}" };

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingId) && !Guid.TryParse(request.SendToAvatarAfterMintingId, out sendToAvatarAfterMintingId))
                return new OASISResult<IWeb4OASISGeoSpatialNFT>() { IsError = true, Message = $"The SendToAvatarAfterMintingId is not valid. Please make sure it is a valid GUID!" };


            API.Core.Objects.NFT.Request.MintAndPlaceWeb4GeoSpatialNFTRequest mintRequest = new API.Core.Objects.NFT.Request.MintAndPlaceWeb4GeoSpatialNFTRequest()
            {
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
                NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(NFTOffChainMetaType),
                NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType),
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = sendToAvatarAfterMintingId,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                WaitTillNFTSent = request.WaitTillNFTSent,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds,
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
                GeoNFTMetaDataProvider = new EnumValue<ProviderType>(geoNFTMetaDataProvider)
            };

            return await NFTManager.MintAndPlaceWeb4GeoNFTAsync(mintRequest, Core.Enums.ResponseFormatType.SimpleText);
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
        //public OASISResult<IWeb4OASISNFTProvider> GetNFTProviderFromNftProviderType(NFTProviderType nftProviderType)
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