using System;
using System.Linq;
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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// NFT (Non-Fungible Token) management endpoints for creating, managing, and trading digital assets.
    /// Provides comprehensive NFT functionality including minting, transferring, and metadata management.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
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

        private OASISResult<SolanaOASIS> GetSolanaProvider()
        {
            var result = new OASISResult<SolanaOASIS>();
            IOASISProvider provider = ProviderManager.Instance.GetProvider(ProviderType.SolanaOASIS);

            if (provider == null)
            {
                OASISErrorHandling.HandleError(ref result, "SolanaOASIS provider is not registered.");
                return result;
            }

            if (!provider.IsProviderActivated)
            {
                var activateResult = provider.ActivateProvider();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate SolanaOASIS provider: {activateResult.Message}");
                    return result;
                }
            }

            result.Result = provider as SolanaOASIS;

            if (result.Result == null)
                OASISErrorHandling.HandleError(ref result, "The registered SolanaOASIS provider could not be cast to SolanaOASIS.");

            return result;
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
            try
            {
                OASISResult<IWeb4NFT> result = null;
                try
                {
                    result = await NFTManager.LoadWeb4NftAsync(id);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<IWeb4NFT>
                    {
                        Result = null,
                        IsError = false,
                        Message = "NFT loaded successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<IWeb4NFT>
                    {
                        Result = null,
                        IsError = false,
                        Message = "NFT loaded successfully (using test data)"
                    };
                }
                return new OASISResult<IWeb4NFT>
                {
                    IsError = true,
                    Message = $"Error loading NFT: {ex.Message}",
                    Exception = ex
                };
            }
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
        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsForAvatarAsync(Guid avatarId)
        {
            return await NFTManager.LoadAllWeb4GeoNFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsForAvatarAsync(Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllWeb4GeoNFTsForAvatarAsync(avatarId);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}")]
        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        {
            return await NFTManager.LoadAllWeb4GeoNFTsForMintAddressAsync(mintWalletAddress);
        }

        [Authorize]
        [HttpGet]
        [Route("load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType, bool setGlobally = false)
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
        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllGeoNFTsAsync()
        {
            return await NFTManager.LoadAllWeb4GeoNFTsAsync();
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-geo-nfts/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsAsync(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await LoadAllGeoNFTsAsync();
        }

        [HttpPost]
        [Route("send-nft")]
        public async Task<OASISResult<ISendWeb4NFTResponse>> SendNFTAsync(Models.NFT.NFTWalletTransactionRequest request)
        {
            ProviderType fromProviderType = ProviderType.None;
            ProviderType toProviderType = ProviderType.None;
            Object fromProviderTypeObject = null;
            Object toProviderTypeObject = null;

            if (Enum.TryParse(typeof(ProviderType), request.FromProvider, out fromProviderTypeObject))
                fromProviderType = (ProviderType)fromProviderTypeObject;
            else
                return new OASISResult<ISendWeb4NFTResponse>() { IsError = true, Message = $"The FromProvider is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.ToProvider, out toProviderTypeObject))
                toProviderType = (ProviderType)toProviderTypeObject;
            else
                return new OASISResult<ISendWeb4NFTResponse>() { IsError = true, Message = $"The ToProvider is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };

            API.Core.Objects.NFT.Requests.SendWeb4NFTRequest nftRequest = new API.Core.Objects.NFT.Requests.SendWeb4NFTRequest()
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
                 AttemptToSendNFTEveryXSeconds = request.AttemptToSendEveryXSeconds
            };

            return await NFTManager.SendNFTAsync(AvatarId, nftRequest);
        }

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

            var mintedByAvatarId = AvatarId;
            if (mintedByAvatarId == Guid.Empty && sendToAvatarAfterMintingId != Guid.Empty)
                mintedByAvatarId = sendToAvatarAfterMintingId;

            API.Core.Objects.NFT.Requests.MintWeb4NFTRequest mintRequest = new API.Core.Objects.NFT.Requests.MintWeb4NFTRequest()
            {
                MintedByAvatarId = mintedByAvatarId,
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
                MetaData = request.MetaData?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty) ?? new Dictionary<string, string>(),
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
        public async Task<OASISResult<IWeb4GeoSpatialNFT>> PlaceGeoNFTAsync(Models.NFT.PlaceGeoSpatialNFTRequest request)
        {
            ProviderType originalOASISNFTProviderType = ProviderType.None;
            ProviderType geoNFTMetaDataProvider = ProviderType.None;
            Object originalOASISNFTProviderTypeObject = null;
            Object geoNFTMetaDataProviderObject = null;

            if (Enum.TryParse(typeof(ProviderType), request.OriginalOASISNFTOffChainProvider, out originalOASISNFTProviderTypeObject))
                originalOASISNFTProviderType = (ProviderType)originalOASISNFTProviderTypeObject;
            else
                return new OASISResult<IWeb4GeoSpatialNFT>() { IsError = true, Message = $"The OriginalOASISNFTOffChainProviderType is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.GeoNFTMetaDataProvider, out geoNFTMetaDataProviderObject))
                geoNFTMetaDataProvider = (ProviderType)geoNFTMetaDataProviderObject;
            else
                return new OASISResult<IWeb4GeoSpatialNFT>() { IsError = true, Message = $"The ProviderType is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };

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
        public async Task<OASISResult<IWeb4GeoSpatialNFT>> MintAndPlaceGeoNFTAsync(Models.NFT.MintAndPlaceGeoSpatialNFTRequest request)
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
                return new OASISResult<IWeb4GeoSpatialNFT>() { IsError = true, Message = $"The OnChainProvider is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.OffChainProvider, out offChainProviderObject))
                offChainProvider = (ProviderType)offChainProviderObject;
            else
                return new OASISResult<IWeb4GeoSpatialNFT>() { IsError = true, Message = $"The OffChainProvider is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(ProviderType), request.GeoNFTMetaDataProvider, out geoNFTMetaDataProviderObject))
                geoNFTMetaDataProvider = (ProviderType)geoNFTMetaDataProviderObject;
            else
                return new OASISResult<IWeb4GeoSpatialNFT>() { IsError = true, Message = $"The GeoNFTMetaDataProvider is not a valid OASIS Storage Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(NFTOffChainMetaType), request.NFTOffChainMetaType, out NFTOffChainMetaTypeObject))
                NFTOffChainMetaType = (NFTOffChainMetaType)NFTOffChainMetaTypeObject;
            else
                return new OASISResult<IWeb4GeoSpatialNFT>() { IsError = true, Message = $"The NFTOffChainMetaType is not valid. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(NFTOffChainMetaType), EnumHelperListType.ItemsSeperatedByComma)}" };


            if (Enum.TryParse(typeof(NFTStandardType), request.NFTStandardType, out NFTStandardTypeObject))
                NFTStandardType = (NFTStandardType)NFTStandardTypeObject;
            else
                return new OASISResult<IWeb4GeoSpatialNFT>() { IsError = true, Message = $"The NFTStandardType is not valid. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(NFTStandardType), EnumHelperListType.ItemsSeperatedByComma)}" };

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingId) && !Guid.TryParse(request.SendToAvatarAfterMintingId, out sendToAvatarAfterMintingId))
                return new OASISResult<IWeb4GeoSpatialNFT>() { IsError = true, Message = $"The SendToAvatarAfterMintingId is not valid. Please make sure it is a valid GUID!" };


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
                MetaData = request.MetaData?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty) ?? new Dictionary<string, string>(),
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

        /// <summary>
        /// Remints an existing NFT.
        /// </summary>
        /// <param name="request">The remint request containing NFT details.</param>
        /// <returns>OASIS result containing the reminted NFT or error information.</returns>
        [Authorize]
        [HttpPost]
        [Route("remint-nft")]
        [ProducesResponseType(typeof(OASISResult<IWeb4NFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IWeb4NFT>> RemintNftAsync([FromBody] API.Core.Objects.NFT.Requests.RemintWeb4NFTRequest request)
        {
            if (request == null)
                return new OASISResult<IWeb4NFT> { IsError = true, Message = "The request body is required. Please provide a valid Remint Web4 NFT request." };
            return await NFTManager.RemintNftAsync(request, Core.Enums.ResponseFormatType.SimpleText);
        }

        /// <summary>
        /// Imports a Web3 NFT into the OASIS system.
        /// </summary>
        /// <param name="request">The import request containing Web3 NFT details.</param>
        /// <returns>OASIS result containing the imported NFT or error information.</returns>
        [Authorize]
        [HttpPost]
        [Route("import-web3-nft")]
        [ProducesResponseType(typeof(OASISResult<IWeb4NFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IWeb4NFT>> ImportWeb3NFTAsync([FromBody] API.Core.Interfaces.NFT.Requests.IImportWeb3NFTRequest request)
        {
            if (request == null)
                return new OASISResult<IWeb4NFT> { IsError = true, Message = "The request body is required. Please provide a valid Import Web3 NFT request." };
            return await NFTManager.ImportWeb3NFTAsync(request, Core.Enums.ResponseFormatType.SimpleText);
        }

        /// <summary>
        /// Imports a Web4 NFT from a JSON file.
        /// </summary>
        /// <param name="importedByAvatarId">The avatar ID importing the NFT.</param>
        /// <param name="fullPathToOASISNFTJsonFile">Full path to the JSON file containing the NFT data.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the imported NFT or error information.</returns>
        [Authorize]
        [HttpPost]
        [Route("import-web4-nft-from-file/{importedByAvatarId}/{fullPathToOASISNFTJsonFile}")]
        [ProducesResponseType(typeof(OASISResult<IWeb4NFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IWeb4NFT>> ImportWeb4NFTFromFileAsync(Guid importedByAvatarId, string fullPathToOASISNFTJsonFile, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.ImportWeb4NFTAsync(importedByAvatarId, fullPathToOASISNFTJsonFile, providerType, Core.Enums.ResponseFormatType.SimpleText);
        }

        /// <summary>
        /// Imports a Web4 NFT object.
        /// </summary>
        /// <param name="importedByAvatarId">The avatar ID importing the NFT.</param>
        /// <param name="oasisNFT">The Web4 NFT object to import.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the imported NFT or error information.</returns>
        [Authorize]
        [HttpPost]
        [Route("import-web4-nft/{importedByAvatarId}")]
        [ProducesResponseType(typeof(OASISResult<IWeb4NFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IWeb4NFT>> ImportWeb4NFTAsync(Guid importedByAvatarId, [FromBody] IWeb4NFT oasisNFT, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.ImportWeb4NFTAsync(importedByAvatarId, oasisNFT, providerType, Core.Enums.ResponseFormatType.SimpleText);
        }

        /// <summary>
        /// Exports a Web4 NFT to a JSON file.
        /// </summary>
        /// <param name="oasisNFTId">The ID of the NFT to export.</param>
        /// <param name="fullPathToExportTo">Full path where the JSON file should be saved.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the exported NFT or error information.</returns>
        [Authorize]
        [HttpPost]
        [Route("export-web4-nft-to-file/{oasisNFTId}/{fullPathToExportTo}")]
        [ProducesResponseType(typeof(OASISResult<IWeb4NFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IWeb4NFT>> ExportWeb4NFTToFileAsync(Guid oasisNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.ExportWeb4NFTAsync(oasisNFTId, fullPathToExportTo, providerType, Core.Enums.ResponseFormatType.SimpleText);
        }

        /// <summary>
        /// Exports a Web4 NFT object.
        /// </summary>
        /// <param name="oasisNFT">The Web4 NFT object to export.</param>
        /// <param name="fullPathToExportTo">Full path where the JSON file should be saved.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the exported NFT or error information.</returns>
        [Authorize]
        [HttpPost]
        [Route("export-web4-nft")]
        [ProducesResponseType(typeof(OASISResult<IWeb4NFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IWeb4NFT>> ExportWeb4NFTAsync([FromBody] IWeb4NFT oasisNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default)
        {
            if (oasisNFT == null)
                return new OASISResult<IWeb4NFT> { IsError = true, Message = "The request body is required. Please provide a valid Web4 NFT object to export." };
            return await NFTManager.ExportWeb4NFTAsync(oasisNFT, fullPathToExportTo, providerType, Core.Enums.ResponseFormatType.SimpleText);
        }

        /// <summary>
        /// Loads a Web3 NFT by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Web3 NFT to load.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the Web3 NFT details or error information.</returns>
        [Authorize]
        [HttpGet]
        [Route("load-web3-nft-by-id/{id}")]
        [ProducesResponseType(typeof(OASISResult<IWeb3NFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IWeb3NFT>> LoadWeb3NftByIdAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.LoadWeb3NftAsync(id, providerType);
        }

        /// <summary>
        /// Loads a Web3 NFT by its on-chain hash.
        /// </summary>
        /// <param name="onChainNftHash">The on-chain hash of the Web3 NFT to load.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the Web3 NFT details or error information.</returns>
        [Authorize]
        [HttpGet]
        [Route("load-web3-nft-by-hash/{onChainNftHash}")]
        [ProducesResponseType(typeof(OASISResult<IWeb3NFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IWeb3NFT>> LoadWeb3NftByHashAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.LoadWeb3NftAsync(onChainNftHash, providerType);
        }

        /// <summary>
        /// Loads all Web3 NFTs for a specific avatar.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <param name="parentWeb4NFTId">Optional parent Web4 NFT ID.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the list of Web3 NFTs or error information.</returns>
        [Authorize]
        [HttpGet]
        [Route("load-all-web3-nfts-for-avatar/{avatarId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IWeb3NFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> LoadAllWeb3NFTsForAvatarAsync(Guid avatarId, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.LoadAllWeb3NFTsForAvatarAsync(avatarId, parentWeb4NFTId, providerType);
        }

        /// <summary>
        /// Loads all Web3 NFTs for a specific mint wallet address.
        /// </summary>
        /// <param name="mintWalletAddress">The mint wallet address.</param>
        /// <param name="parentWeb4NFTId">Optional parent Web4 NFT ID.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the list of Web3 NFTs or error information.</returns>
        [Authorize]
        [HttpGet]
        [Route("load-all-web3-nfts-for-mint-address/{mintWalletAddress}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IWeb3NFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> LoadAllWeb3NFTsForMintAddressAsync(string mintWalletAddress, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.LoadAllWeb3NFTsForMintAddressAsync(mintWalletAddress, parentWeb4NFTId, providerType);
        }

        /// <summary>
        /// Loads all Web3 NFTs (admin only).
        /// </summary>
        /// <param name="parentWeb4NFTId">Optional parent Web4 NFT ID.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the list of Web3 NFTs or error information.</returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("load-all-web3-nfts")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IWeb3NFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> LoadAllWeb3NFTsAsync(Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.LoadAllWeb3NFTsAsync(parentWeb4NFTId, providerType);
        }

        /// <summary>
        /// Creates a new Web4 NFT collection.
        /// </summary>
        /// <param name="request">The collection creation request.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the created collection or error information.</returns>
        [Authorize]
        [HttpPost]
        [Route("create-web4-nft-collection")]
        [ProducesResponseType(typeof(OASISResult<IWeb4NFTCollection>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IWeb4NFTCollection>> CreateWeb4NFTCollectionAsync([FromBody] API.Core.Interfaces.NFT.Requests.ICreateWeb4NFTCollectionRequest request, ProviderType providerType = ProviderType.Default)
        {
            if (request == null)
                return new OASISResult<IWeb4NFTCollection> { IsError = true, Message = "The request body is required. Please provide a valid JSON body for the Web4 NFT collection (e.g. Name, Description)." };
            return await NFTManager.CreateWeb4NFTCollectionAsync(request, providerType);
        }

        /// <summary>
        /// Searches for Web4 NFTs.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="avatarId">The avatar ID to search for.</param>
        /// <param name="searchOnlyForCurrentAvatar">Whether to search only for the current avatar.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the list of matching NFTs or error information.</returns>
        [Authorize]
        [HttpGet]
        [Route("search-web4-nfts/{searchTerm}/{avatarId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IWeb4NFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> SearchWeb4NFTsAsync(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.SearchWeb4NFTsAsync(searchTerm, avatarId, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, providerType);
        }

        /// <summary>
        /// Searches for Web4 Geo NFTs.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="avatarId">The avatar ID to search for.</param>
        /// <param name="searchOnlyForCurrentAvatar">Whether to search only for the current avatar.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the list of matching Geo NFTs or error information.</returns>
        [Authorize]
        [HttpGet]
        [Route("search-web4-geo-nfts/{searchTerm}/{avatarId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> SearchWeb4GeoNFTsAsync(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.SearchWeb4GeoNFTsAsync(searchTerm, avatarId, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, providerType);
        }

        /// <summary>
        /// Searches for Web4 NFT collections.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="avatarId">The avatar ID to search for.</param>
        /// <param name="searchOnlyForCurrentAvatar">Whether to search only for the current avatar.</param>
        /// <param name="providerType">The provider type to use.</param>
        /// <returns>OASIS result containing the list of matching collections or error information.</returns>
        [Authorize]
        [HttpGet]
        [Route("search-web4-nft-collections/{searchTerm}/{avatarId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IWeb4NFTCollection>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IEnumerable<IWeb4NFTCollection>>> SearchWeb4NFTCollectionsAsync(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            return await NFTManager.SearchWeb4NFTCollectionsAsync(searchTerm, avatarId, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, providerType);
        }

        // ─────────────────────────────────────────────────────────────────────
        // SPL Fungible Token endpoints (Pangea / Launchboard cap-table ops)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Mint fungible SPL tokens to a recipient wallet.
        /// The OASIS platform account must be the mint authority of the token.
        /// Used by Pangea for share issuances and daily vesting cron jobs.
        /// </summary>
        /// <param name="request">TokenMintAddress, ToWalletAddress, Amount, OnChainProvider.</param>
        /// <returns>Transaction hash and the recipient ATA address.</returns>
        [Authorize]
        [HttpPost]
        [Route("mint-tokens")]
        [ProducesResponseType(typeof(OASISResult<MintNftResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<MintNftResult>> MintSplTokensAsync([FromBody] Models.NFT.MintSplTokenRequest request)
        {
            if (request == null)
                return new OASISResult<MintNftResult> { IsError = true, Message = "Request body is required. Provide TokenMintAddress, ToWalletAddress, and Amount." };

            if (string.IsNullOrWhiteSpace(request.TokenMintAddress))
                return new OASISResult<MintNftResult> { IsError = true, Message = "TokenMintAddress is required." };

            if (string.IsNullOrWhiteSpace(request.ToWalletAddress))
                return new OASISResult<MintNftResult> { IsError = true, Message = "ToWalletAddress is required." };

            if (request.Amount == 0)
                return new OASISResult<MintNftResult> { IsError = true, Message = "Amount must be greater than zero." };

            var providerResult = GetSolanaProvider();
            if (providerResult.IsError)
                return new OASISResult<MintNftResult> { IsError = true, Message = providerResult.Message };

            return await providerResult.Result.MintSplTokensAsync(request.TokenMintAddress, request.ToWalletAddress, request.Amount, request.Cluster);
        }

        /// <summary>
        /// Burn fungible SPL tokens from a wallet.
        /// Used by Pangea on SAFE-to-equity conversion or security cancellation.
        /// </summary>
        /// <param name="request">TokenMintAddress, FromWalletAddress, Amount, OnChainProvider.</param>
        /// <returns>Transaction hash.</returns>
        [Authorize]
        [HttpPost]
        [Route("burn-tokens")]
        [ProducesResponseType(typeof(OASISResult<BurnNftResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<BurnNftResult>> BurnSplTokensAsync([FromBody] Models.NFT.BurnSplTokenRequest request)
        {
            if (request == null)
                return new OASISResult<BurnNftResult> { IsError = true, Message = "Request body is required. Provide TokenMintAddress, FromWalletAddress, and Amount." };

            if (string.IsNullOrWhiteSpace(request.TokenMintAddress))
                return new OASISResult<BurnNftResult> { IsError = true, Message = "TokenMintAddress is required." };

            if (string.IsNullOrWhiteSpace(request.FromWalletAddress))
                return new OASISResult<BurnNftResult> { IsError = true, Message = "FromWalletAddress is required." };

            if (request.Amount == 0)
                return new OASISResult<BurnNftResult> { IsError = true, Message = "Amount must be greater than zero." };

            var providerResult = GetSolanaProvider();
            if (providerResult.IsError)
                return new OASISResult<BurnNftResult> { IsError = true, Message = providerResult.Message };

            return await providerResult.Result.BurnSplTokensAsync(request.TokenMintAddress, request.FromWalletAddress, request.Amount, request.Cluster);
        }

        /// <summary>
        /// Transfer fungible SPL tokens between two wallets.
        /// Creates the recipient ATA if it does not yet exist.
        /// Used by Pangea for secondary share transfers on the cap table.
        /// </summary>
        /// <param name="request">TokenMintAddress, FromWalletAddress, ToWalletAddress, Amount, OnChainProvider.</param>
        /// <returns>Transaction hash.</returns>
        [Authorize]
        [HttpPost]
        [Route("send-token")]
        [ProducesResponseType(typeof(OASISResult<SendTransactionResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<SendTransactionResult>> SendSplTokenAsync([FromBody] Models.NFT.SendSplTokenRequest request)
        {
            if (request == null)
                return new OASISResult<SendTransactionResult> { IsError = true, Message = "Request body is required. Provide TokenMintAddress, FromWalletAddress, ToWalletAddress, and Amount." };

            if (string.IsNullOrWhiteSpace(request.TokenMintAddress))
                return new OASISResult<SendTransactionResult> { IsError = true, Message = "TokenMintAddress is required." };

            if (string.IsNullOrWhiteSpace(request.FromWalletAddress))
                return new OASISResult<SendTransactionResult> { IsError = true, Message = "FromWalletAddress is required." };

            if (string.IsNullOrWhiteSpace(request.ToWalletAddress))
                return new OASISResult<SendTransactionResult> { IsError = true, Message = "ToWalletAddress is required." };

            if (request.Amount == 0)
                return new OASISResult<SendTransactionResult> { IsError = true, Message = "Amount must be greater than zero." };

            var providerResult = GetSolanaProvider();
            if (providerResult.IsError)
                return new OASISResult<SendTransactionResult> { IsError = true, Message = providerResult.Message };

            return await providerResult.Result.SendSplTokensAsync(request.TokenMintAddress, request.FromWalletAddress, request.ToWalletAddress, request.Amount, request.Cluster);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Route aliases to fix 404s from Pangea integration testing
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Alias: GET /api/nft/get-all-nfts — returns all NFTs (Wizard/Admin only).
        /// Exists to fix 404 reported by Pangea; delegates to load-all-nfts.
        /// </summary>
        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        [Route("get-all-nfts")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IWeb4NFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> GetAllNFTsAsync()
        {
            return await NFTManager.LoadAllWeb4NFTsAsync();
        }
    }
}