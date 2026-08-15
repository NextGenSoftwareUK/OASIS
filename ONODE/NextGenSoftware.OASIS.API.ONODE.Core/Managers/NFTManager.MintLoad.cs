using Ipfs;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class NFTManager
    {
        //TODO: This method may become obsolete if ProviderType changes to NFTProviderType on INFTWalletTransaction
        //public async Task<OASISResult<IWeb4NFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest request)
        //{
        //    return await SendNFTAsync(new NFTWalletTransactionRequest()
        //    {
        //        Amount = request.Amount,
        //        //Date = DateTime.Now,
        //        FromWalletAddress = request.FromWalletAddress,
        //        MemoText = request.MemoText,
        //        MintWalletAddress = request.MintWalletAddress,
        //        ToWalletAddress = request.ToWalletAddress,
        //        //Token = request.Token,
        //        FromProviderType = GetProviderTypeFromNFTProviderType(request.NFTProviderType)
        //    });
        //}

        ////TODO: This method may become obsolete if ProviderType changes to NFTProviderType on INFTWalletTransaction
        //public OASISResult<IWeb4NFTTransactionRespone> SendNFT(INFTWalletTransactionRequest request)
        //{
        //    return SendNFT(new NFTWalletTransactionRequest()
        //    {
        //        Amount = request.Amount,
        //        //Date = DateTime.Now,
        //        FromWalletAddress = request.FromWalletAddress,
        //        MemoText = request.MemoText,
        //        MintWalletAddress = request.MintWalletAddress,
        //        ToWalletAddress = request.ToWalletAddress,
        //        //Token = request.Token,
        //        FromProviderType = GetProviderTypeFromNFTProviderType(request.NFTProviderType)
        //    });
        //}


        public async Task<OASISResult<IInventoryItem>> CollectGeoNFTAsync(ICollectGeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            string errorMessage = "Error occured in CollectGeoNFTAsync in NFTManager. Reason:";

            try
            {
                return await AvatarManager.Instance.AddItemToAvatarInventoryAsync(request.CollectedByAvatarId, new InventoryItem()
                {
                     Image2D = request.Image2D,
                     Image2DURI = request.Image2DURI,
                     Object3D = request.Object3D,
                     Object3DURI = request.Object3DURI,
                     Quantity = request.Quantity,
                     Stack = request.Stack,
                     GameSource = request.GameSource,
                     //ItemType = request.ItemType,
                     ItemType = InventoryItemType.GeoNFT,
                    NftId = request.GeoNFTId
                }, providerType);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IInventoryItem> CollectGeoNFT(ICollectGeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            string errorMessage = "Error occured in CollectGeoNFTAsync in NFTManager. Reason:";

            try
            {
                return AvatarManager.Instance.AddItemToAvatarInventory(request.CollectedByAvatarId, new InventoryItem()
                {
                    Image2D = request.Image2D,
                    Image2DURI = request.Image2DURI,
                    Object3D = request.Object3D,
                    Object3DURI = request.Object3DURI,
                    Quantity = request.Quantity,
                    Stack = request.Stack,
                    GameSource = request.GameSource,
                    //ItemType = request.ItemType,
                    ItemType = InventoryItemType.GeoNFT,
                    NftId = request.GeoNFTId
                }, providerType);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IInventoryItem>> CollectNFTAsync(ICollectGeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            string errorMessage = "Error occured in CollectNFTAsync in NFTManager. Reason:";

            try
            {
                return await AvatarManager.Instance.AddItemToAvatarInventoryAsync(request.CollectedByAvatarId, new InventoryItem()
                {
                    Image2D = request.Image2D,
                    Image2DURI = request.Image2DURI,
                    Object3D = request.Object3D,
                    Object3DURI = request.Object3DURI,
                    Quantity = request.Quantity,
                    Stack = request.Stack,
                    GameSource = request.GameSource,
                    //ItemType = request.ItemType,
                    ItemType = InventoryItemType.NFT,
                    NftId = request.GeoNFTId
                }, providerType);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IInventoryItem> CollectNFT(ICollectGeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            string errorMessage = "Error occured in CollectNFTAsync in NFTManager. Reason:";

            try
            {
                return AvatarManager.Instance.AddItemToAvatarInventory(request.CollectedByAvatarId, new InventoryItem()
                {
                    Image2D = request.Image2D,
                    Image2DURI = request.Image2DURI,
                    Object3D = request.Object3D,
                    Object3DURI = request.Object3DURI,
                    Quantity = request.Quantity,
                    Stack = request.Stack,
                    GameSource = request.GameSource,
                    //ItemType = request.ItemType,
                    ItemType = InventoryItemType.NFT,
                    NftId = request.GeoNFTId
                }, providerType);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<ISendWeb4NFTResponse>> SendNFTAsync(Guid avatarId, ISendWeb4NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<ISendWeb4NFTResponse> result = new OASISResult<ISendWeb4NFTResponse>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid ISendWeb4NFTRequest.";
                return result;
            }
            string errorMessage = "Error occured in SendNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.FromProvider.Value);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                {
                    bool attemptingToSend = true;
                    DateTime startTime = DateTime.Now;

                    do
                    {
                        OASISResult<IWeb3NFTTransactionResponse> sendResult = await nftProviderResult.Result.SendNFTAsync(request);

                        if (sendResult != null && sendResult.Result != null && !sendResult.IsError)
                        {
                            attemptingToSend = false;
                            sendResult.Result.Web3NFT.SendNFTTransactionHash = sendResult.Result.TransactionResult;
                            result.Message = FormatSuccessMessage(request, sendResult, responseFormatType);
                            break;
                        }
                        else if (!request.WaitTillNFTSent)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error occured attempting to send NFT & WaitTillNFTSent is false. Reason: {result.Message}");
                            //result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendNFTEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error occured attempting to send NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!");
                            //result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }

                    } while (attemptingToSend);
                }
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<ISendWeb4NFTResponse> SendNFT(Guid avatarId, ISendWeb4NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<ISendWeb4NFTResponse> result = new OASISResult<ISendWeb4NFTResponse>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid ISendWeb4NFTRequest.";
                return result;
            }
            string errorMessage = "Error occured in SendNFT in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.FromProvider.Value);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                {
                    bool attemptingToSend = true;
                    DateTime startTime = DateTime.Now;

                    do
                    {
                        OASISResult<IWeb3NFTTransactionResponse> sendResult = nftProviderResult.Result.SendNFT(request);

                        if (sendResult != null && sendResult.Result != null && !sendResult.IsError)
                        {
                            attemptingToSend = false;
                            sendResult.Result.Web3NFT.SendNFTTransactionHash = sendResult.Result.TransactionResult;
                            //result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }
                        else if (!request.WaitTillNFTSent)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error occured attempting to send NFT & WaitTillNFTSent is false. Reason: {result.Message}");
                            // result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendNFTEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error occured attempting to send NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!");
                            //result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }

                    } while (attemptingToSend);
                }
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }


        public async Task<OASISResult<IWeb4NFT>> RemintNftAsync(IRemintWeb4NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid IRemintWeb4NFTRequest.";
                return result;
            }
            //if (request.Web3NFTs != null && request.Web3NFTs.Count > 0)
            //{
            MintWeb4NFTRequest web4Request = new MintWeb4NFTRequest()
            {
                MintedByAvatarId = request.Web4NFT.MintedByAvatarId,
                Title = request.Web4NFT.Title,
                Description = request.Web4NFT.Description,
                MemoText = request.Web4NFT.MemoText,
                Price = request.Web4NFT.Price,
                Discount = request.Web4NFT.Discount,
                RoyaltyPercentage = request.Web4NFT.RoyaltyPercentage,
                IsForSale = request.Web4NFT.IsForSale,
                SaleStartDate = request.Web4NFT.SaleStartDate,
                SaleEndDate = request.Web4NFT.SaleEndDate,
                OnChainProvider = request.Web4NFT.OnChainProvider,
                OffChainProvider = request.Web4NFT.OffChainProvider,
                StoreNFTMetaDataOnChain = request.Web4NFT.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = request.Web4NFT.NFTOffChainMetaType,
                NFTStandardType = request.Web4NFT.NFTStandardType,
                Image = request.Web4NFT.Image,
                ImageUrl = request.Web4NFT.ImageUrl,
                Thumbnail = request.Web4NFT.Thumbnail,
                ThumbnailUrl = request.Web4NFT.ThumbnailUrl,
                MetaData = request.Web4NFT.MetaData,
                Tags = request.Web4NFT.Tags,
                JSONMetaData = request.Web4NFT.JSONMetaData,
                JSONMetaDataURL = request.Web4NFT.JSONMetaDataURL,
                Symbol = request.Web4NFT.Symbol,
                NumberToMint = request.Web3NFTs.Count,
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                WaitTillNFTSent = request.WaitTillNFTSent,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                Web3NFTs = request.Web3NFTs
            };

            if (request.Web3NFTs.Count > 0)
            {
                int i = 0;
                foreach (IMintWeb3NFTRequest web3Request in request.Web3NFTs)
                {
                    i++;

                    if (web3Request.NumberToMint == 0)
                        web3Request.NumberToMint = 1;

                    result = await MintWeb3NFTsAsync(result, web4Request, web3Request, request.Web4NFT, false, responseFormatType, i == request.Web3NFTs.Count);
                }
            }
            else
                result = await MintWeb3NFTsAsync(result, web4Request, null, request.Web4NFT, false, responseFormatType, true);
            //}
            //else
            //    OASISErrorHandling.HandleError(ref result, "mintWeb3NFTRequests is null or empty!");

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> RemintGeoNftAsync(IRemintWeb4GeoNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid IRemintWeb4GeoNFTRequest.";
                return result;
            }
            OASISResult<IWeb4NFT> web4NFTResult = new OASISResult<IWeb4NFT>();

            if (request.Web3NFTs != null && request.Web3NFTs.Count > 0)
            {
                MintWeb4NFTRequest web4Request = new MintWeb4NFTRequest()
                {
                    MintedByAvatarId = request.Web4GeoNFT.MintedByAvatarId,
                    Title = request.Web4GeoNFT.Title,
                    Description = request.Web4GeoNFT.Description,
                    MemoText = request.Web4GeoNFT.MemoText,
                    Price = request.Web4GeoNFT.Price,
                    Discount = request.Web4GeoNFT.Discount,
                    RoyaltyPercentage = request.Web4GeoNFT.RoyaltyPercentage,
                    IsForSale = request.Web4GeoNFT.IsForSale,
                    SaleStartDate = request.Web4GeoNFT.SaleStartDate,
                    SaleEndDate = request.Web4GeoNFT.SaleEndDate,
                    OnChainProvider = request.Web4GeoNFT.OnChainProvider,
                    OffChainProvider = request.Web4GeoNFT.OffChainProvider,
                    StoreNFTMetaDataOnChain = request.Web4GeoNFT.StoreNFTMetaDataOnChain,
                    NFTOffChainMetaType = request.Web4GeoNFT.NFTOffChainMetaType,
                    NFTStandardType = request.Web4GeoNFT.NFTStandardType,
                    Image = request.Web4GeoNFT.Image,
                    ImageUrl = request.Web4GeoNFT.ImageUrl,
                    Thumbnail = request.Web4GeoNFT.Thumbnail,
                    ThumbnailUrl = request.Web4GeoNFT.ThumbnailUrl,
                    MetaData = request.Web4GeoNFT.MetaData,
                    Tags = request.Web4GeoNFT.Tags,
                    JSONMetaData = request.Web4GeoNFT.JSONMetaData,
                    JSONMetaDataURL = request.Web4GeoNFT.JSONMetaDataURL,
                    Symbol = request.Web4GeoNFT.Symbol,
                    NumberToMint = request.Web3NFTs.Count,
                    AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                    WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                    WaitTillNFTMinted = request.WaitTillNFTMinted,
                    AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds,
                    WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                    WaitTillNFTSent = request.WaitTillNFTSent,
                    SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                    SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                    SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                    SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                    Web3NFTs = request.Web3NFTs
                };

                int i = 0;
                foreach (IMintWeb3NFTRequest web3Request in request.Web3NFTs)
                {
                    i++;

                    if (web3Request.NumberToMint == 0)
                        web3Request.NumberToMint = 1;

                    web4NFTResult = await MintWeb3NFTsAsync(web4NFTResult, web4Request, web3Request, request.Web4GeoNFT, false, responseFormatType, i == request.Web3NFTs.Count);

                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(web4NFTResult, result);
                    result.Result = (IWeb4GeoSpatialNFT)web4NFTResult.Result;
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, "mintWeb3NFTRequests is null or empty!");

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> MintNftAsync(IMintWeb4NFTRequest request, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid IMintWeb4NFTRequest.";
                return result;
            }
            string errorMessage = "Error occured in MintNftAsync in NFTManager. Reason:";

            try
            {
                if (request.Web3NFTs == null || request.Web3NFTs != null && request.Web3NFTs.Count < request.NumberToMint)
                {
                    if (request.Web3NFTs == null)
                        request.Web3NFTs = new List<IMintWeb3NFTRequest>();

                    for (int i = 0; i <= (request.NumberToMint - request.Web3NFTs.Count); i++)
                        request.Web3NFTs.Add(new MintWeb3NFTRequest());
                }

                if (request.Web3NFTs != null && request.Web3NFTs.Count > 0)
                {
                    int i = 0;
                    foreach (IMintWeb3NFTRequest web3Request in request.Web3NFTs)
                    {
                        i++;

                        if (web3Request.NumberToMint == 0)
                            web3Request.NumberToMint = 1;
                        //web3Request.NumberToMint = request.NumberToMint;

                        IMintWeb4NFTRequest originalMintWeb4NFTRequest = CloneWeb4NFTRequest(request);
                        result = await MintWeb3NFTsAsync(result, originalMintWeb4NFTRequest, web3Request, null, isGeoNFT, responseFormatType, i == request.Web3NFTs.Count);
                    }
                }
                //else
                //    result = await MintWeb3NFTsAsync(result, request, null, null, isGeoNFT, responseFormatType, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

    }
}
