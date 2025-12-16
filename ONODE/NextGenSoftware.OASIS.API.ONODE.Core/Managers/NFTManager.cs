using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class NFTManager : COSMICManagerBase, INFTManager
    {
        private const int FORMAT_SUCCESS_MESSAGE_COL_WIDTH = 34;
        IPFSOASIS _ipfs = new IPFSOASIS();
        PinataOASIS _pinata = new PinataOASIS();

        public IPFSOASIS IPFS
        {
            get
            {
                if (_ipfs == null)
                    _ipfs = new IPFSOASIS(OASISDNA);

                if (!_ipfs.IsProviderActivated)
                    _ipfs.ActivateProvider();

                return _ipfs;
            }
        }

        public PinataOASIS Pinata
        {
            get
            {
                if (_pinata == null)
                    _pinata = new PinataOASIS(OASISDNA);

                if (!_pinata.IsProviderActivated)
                    _pinata.ActivateProvider();

                return _pinata;
            }
        }

        //private static NFTManager _instance = null;

        // public Guid AvatarId { get; set; }

        //public static NFTManager Instance
        //{
        //    get
        //    {
        //        if (_instance == null)
        //            _instance = new NFTManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.Id); //TODO: Need to remove LoggedInAvatar ASAP! Not sure how to pass the avatarId to the instance prop?

        //        return _instance;
        //    }
        //}

        public NFTManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public NFTManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

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

        public async Task<OASISResult<ISendWeb4NFTResponse>> SendNFTAsync(Guid avatarId, ISendWeb4NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<ISendWeb4NFTResponse> result = new OASISResult<ISendWeb4NFTResponse>();
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

            if (request.Web3NFTs != null && request.Web3NFTs.Count > 0)
            {
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
                OASISErrorHandling.HandleError(ref result, "mintWeb3NFTRequests is null or empty!");

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> RemintGeoNftAsync(IRemintWeb4GeoNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
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
            string errorMessage = "Error occured in MintNftAsync in NFTManager. Reason:";

            try
            {
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
                else
                    result = await MintWeb3NFTsAsync(result, request, null, null, isGeoNFT, responseFormatType, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ImportWeb3NFTAsync(IImportWeb3NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in ImportWeb3NFT in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                result.Result = CreateWeb4NFT(request);

                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (request.OffChainProvider.Value == ProviderType.None)
                    request.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateWeb4NFTMetaDataHolon(result.Result, request), request.ImportedByAvatarId, true, true, 0, true, false, request.OffChainProvider.Value);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                    result.Message = FormatSuccessMessage(request, result, responseFormatType);
                else
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {request.OffChainProvider.Name}. Reason: {saveHolonResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ImportWeb3NFT(IImportWeb3NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in ImportWeb3NFT in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                result.Result = CreateWeb4NFT(request);

                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (request.OffChainProvider.Value == ProviderType.None)
                    request.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateWeb4NFTMetaDataHolon(result.Result, request), request.ImportedByAvatarId, true, true, 0, true, false, request.OffChainProvider.Value);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                    result.Message = FormatSuccessMessage(request, result, responseFormatType);
                else
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {request.OffChainProvider.Name}. Reason: {saveHolonResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ImportWeb4NFTAsync(Guid importedByAvatarId, string fullPathToOASISNFTJsonFile, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            return await ImportWeb4NFTAsync(importedByAvatarId, JsonConvert.DeserializeObject<IWeb4NFT>(await File.ReadAllTextAsync(fullPathToOASISNFTJsonFile)));
        }

        public async Task<OASISResult<IWeb4NFT>> ImportWeb4NFTAsync(Guid importedByAvatarId, IWeb4NFT OASISNFT, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in ImportWeb4NFTAsync in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (OASISNFT.OffChainProvider.Value == ProviderType.None)
                    OASISNFT.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                IHolon holon = CreateWeb4NFTMetaDataHolon(OASISNFT);
                holon.MetaData["NFT.ImportedOn"] = DateTime.Now;
                holon.MetaData["NFT.ImportedBy"] = importedByAvatarId;

                OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(holon, importedByAvatarId, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                    result.Message = FormatSuccessMessage(result, importedByAvatarId, responseFormatType);
                else
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {OASISNFT.OffChainProvider.Name}. Reason: {saveHolonResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ExportWeb4NFTAsync(Guid OASISNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> exportResult = await LoadWeb4NftAsync(OASISNFTId, providerType);

            if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
            {
                return await ExportWeb4NFTAsync(exportResult.Result, fullPathToExportTo, providerType, responseFormatType);
            }
            else
                return exportResult;
        }

        public async Task<OASISResult<IWeb4NFT>> ExportWeb4NFTAsync(IWeb4NFT OASISNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            await File.WriteAllTextAsync(fullPathToExportTo, JsonConvert.SerializeObject(OASISNFT, Formatting.Indented));
            return new OASISResult<IWeb4NFT>(OASISNFT);
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ImportWeb4GeoNFTAsync(Guid importedByAvatarId, string fullPathToOASISGeoNFTJsonFile, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            return await ImportWeb4GeoNFTAsync(importedByAvatarId, JsonConvert.DeserializeObject<IWeb4GeoSpatialNFT>(await File.ReadAllTextAsync(fullPathToOASISGeoNFTJsonFile)));
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ImportWeb4GeoNFTAsync(Guid importedByAvatarId, IWeb4GeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in ImportOASISGeoNFTAsync in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (OASISGeoNFT.OffChainProvider.Value == ProviderType.None)
                    OASISGeoNFT.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                IHolon holon = CreateWeb4GeoSpatialNFTMetaDataHolon(OASISGeoNFT);
                holon.MetaData["GEONFT.OriginalOASISNFT.ImportedOn"] = DateTime.Now;
                holon.MetaData["GEONFT.OriginalOASISNFT.ImportedBy"] = importedByAvatarId;

                OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(holon, importedByAvatarId, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                    result.Message = FormatSuccessMessage(result, importedByAvatarId, responseFormatType);
                else
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {OASISGeoNFT.OffChainProvider.Name}. Reason: {saveHolonResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ExportWeb4GeoNFTAsync(Guid OASISGeoNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> exportResult = await LoadWeb4GeoNftAsync(OASISGeoNFTId, providerType);

            if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
                return await ExportWeb4GeoNFTAsync(exportResult.Result, fullPathToExportTo, providerType, responseFormatType);
            else
                return exportResult;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ExportWeb4GeoNFTAsync(IWeb4GeoSpatialNFT OASISGeoNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            await File.WriteAllTextAsync(fullPathToExportTo, JsonConvert.SerializeObject(OASISGeoNFT, Formatting.Indented));
            return new OASISResult<IWeb4GeoSpatialNFT>(OASISGeoNFT);
        }

        public OASISResult<bool> IsNFTStandardTypeValid(IMintWeb4NFTRequest request, string errorMessage = "")
        {
            return IsNFTStandardTypeValid(request.NFTStandardType.Value, request.OnChainProvider.Value, errorMessage);
        }

        public OASISResult<bool> IsNFTStandardTypeValid(NFTStandardType NFTStandardType, ProviderType onChainProvider, string errorMessage = "")
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (NFTStandardType == NFTStandardType.SPL && onChainProvider != ProviderType.SolanaOASIS)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} NFTStandardType is set to SPL but OnChainProvider is not set to SolanaOASIS! Please make sure you set the OnChainProvider to SolanaOASIS when minting SPL NFTs.");
                return result;
            }

            if (NFTStandardType != NFTStandardType.SPL && onChainProvider == ProviderType.SolanaOASIS)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} OnChainProvider is set to SolanaOASIS but NFTStandardType is not set to SPL! Please make sure you set the NFTStandardType to SPL when minting NFTs on SolanaOASIS.");
                return result;
            }

            if ((NFTStandardType == NFTStandardType.ERC721 || NFTStandardType == NFTStandardType.ERC1155) && (onChainProvider == ProviderType.ArbitrumOASIS || onChainProvider == ProviderType.EthereumOASIS || onChainProvider == ProviderType.PolygonOASIS))
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} When selecting NFTStandardType ERC721 or ERC1155 then the OnChainProvider needs to be set to a supported EVM chain such as ArbitrumOASIS, EthereumOASIS or PolygonOASIS.");
                return result;
            }

            return result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadWeb3NftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb3NFT> LoadWeb3Nft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3Nft in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolon(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadWeb3NftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb3NFT> LoadWeb3Nft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3Nft in NFTManager. Reason:";

            try
            {
                //result = DecodeNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> LoadWeb4NftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb4NFT> LoadWeb4Nft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4Nft in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolon(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public async Task<OASISResult<IWeb4NFT>> LoadWeb4NftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public OASISResult<IWeb4NFT> LoadWeb4Nft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4Nft in NFTManager. Reason:";

            try
            {
                //result = DecodeNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> LoadWeb4GeoNftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb4GeoSpatialNFT> LoadWeb4GeoNft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNft in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadHolon(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public async Task<OASISResult<IWeb4GeoSpatialNFT>> LoadWeb4GeoNftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNftAsync in NFTManager. Reason:";

            try
            {
                //result = DecodeGeoNFTMetaData(await Data.LoadHolonByCustomKeyAsync(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeGeoNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public OASISResult<IWeb4GeoSpatialNFT> LoadWeb4GeoNft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNft in NFTManager. Reason:";

            try
            {
                //result = DecodeGeoNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeGeoNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> LoadAllWeb3NFTsForAvatarAsync(Guid avatarId, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                {
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync(new Dictionary<string, string>()
                    {
                        { "NFT.MintedByAvatarId", avatarId.ToString() },
                        { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() }
                    }, MetaKeyValuePairMatchMode.All, HolonType.Web3NFT, providerType: providerType), result, errorMessage);
                }
                else
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb3NFT>> LoadAllWeb3NFTsForAvatar(Guid avatarId, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTsForAvatar in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                {
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData(new Dictionary<string, string>()
                    {
                        { "NFT.MintedByAvatarId", avatarId.ToString() },
                        { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() }
                    }, MetaKeyValuePairMatchMode.All, HolonType.Web3NFT, providerType: providerType), result, errorMessage);
                }
                else
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> LoadAllWeb3NFTsForMintAddressAsync(string mintWalletAddress, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTsForMintAddressAsync in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                {
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync(new Dictionary<string, string>()
                    {
                        { "NFT.MintWalletAddress", mintWalletAddress },
                        { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() }
                    }, MetaKeyValuePairMatchMode.All, HolonType.Web3NFT, providerType: providerType), result, errorMessage);
                }
                else
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintWalletAddress", mintWalletAddress, HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb3NFT>> LoadAllWeb3NFTsForMintAddress(string mintWalletAddress, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForMintAddress in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                {
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData(new Dictionary<string, string>()
                    {
                        { "NFT.MintWalletAddress", mintWalletAddress },
                        { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() }
                    }, MetaKeyValuePairMatchMode.All, HolonType.Web3NFT, providerType: providerType), result, errorMessage);
                }
                else
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintWalletAddress", mintWalletAddress, HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web4NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

                //TODO: Want to add new LoadHolonsForAvatar methods to HolonManager eventually, which we would use here instead. It would load all Holons that had CreatedByAvatarId = avatarId. But for now we can just set the ParentId on the holons to the AvatarId.
                // OASISResult<IEnumerable<IHolon>> holonsResult = await Data.LoadHolonsForParentAsync(avatarId, HolonType.Web4NFT, true, true, 0, true, 0, providerType); //This line would also work because by default all holons created have their parent set to the avatar that created them in the HolonManger.
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4NFT>> LoadAllWeb4NFTsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTsForAvatar in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web4NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTsForMintAddressAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintWalletAddress", mintWalletAddress, HolonType.Web4NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

                //TODO: We could possibly add a CustomKey2 property to Holons to load by but not sure how far we go with this? I think eventually we may have 3 custom keys you can load by but for now Search will do... ;-)
                //OASISResult<ISearchResults> searchResult = await SearchManager.Instance.SearchAsync(new SearchParams()
                //{
                //    SearchGroups = new List<ISearchGroupBase>()
                //    {
                //        new SearchTextGroup()
                //        {
                //            SearchQuery = mintWalletAddress,
                //            SearchHolons = true,
                //            HolonSearchParams = new SearchHolonParams()
                //            {
                //                 MetaData = true,
                //                 MetaDataKey = "NFT.MintWalletAddress"
                //            }
                //        },
                //        new SearchTextGroup()
                //        {
                //            PreviousSearchGroupOperator = SearchParamGroupOperator.And, //This wll currently not work in MongoDBOASIS Provider because it currently only supports OR and not AND...
                //            SearchQuery = "NFT",
                //            SearchHolons = true,
                //            HolonSearchParams = new SearchHolonParams()
                //            {
                //                Name = true
                //            }
                //        }
                //    }
                //});

                //if (searchResult != null && !searchResult.IsError && searchResult.Result != null)
                //{
                //    foreach (IHolon holon in searchResult.Result.SearchResultHolons)
                //        result.Result.Add((IWeb4OASISNFT)JsonSerializer.Deserialize(holon.MetaData["OASISNFT"].ToString(), typeof(IWeb4OASISNFT)));
                //}
                //else
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading/searching the holon metadata. Reason: {searchResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4NFT>> LoadAllWeb4NFTsForMintAddress(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTsForMintAddress in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintedByAvatarId", mintWalletAddress, HolonType.Web4NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadHolonsByMetaDataAsync("GEONFT.PlacedByAvatarId", avatarId.ToString(), HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> LoadAllWeb4GeoNFTsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForAvatar in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadHolonsByMetaData("GEONFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForMintAddressAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadHolonsByMetaDataAsync("GEONFT.OriginalOASISNFT.MintWalletAddress", mintWalletAddress, HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> LoadAllWeb4GeoNFTsForMintAddress(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForMintAddress in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadHolonsByMetaData("GEONFT.OriginalOASISNFT.MintWalletAddress", mintWalletAddress, HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsForAvatarLocationAsync(long latLocation, long longLocation, int radius, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForAvatarLocationAsync in NFTManager. Reason:";

            try
            {
                if (radius > 0)
                {
                    //Create a bounding box.
                    long topLeftLat = latLocation - radius;
                    long topLeftLong = longLocation - radius;
                    long topRightLat = latLocation - radius;
                    long topRightLong = longLocation + radius;
                    long bottomRightLat = latLocation - radius;
                    long bottomRightLong = longLocation + radius;
                    long bottomLeftLat = latLocation - radius;
                    long bottomLeftLong = longLocation - radius;

                    if (topLeftLat < 0)
                        topLeftLat = 0;

                    if (topLeftLong < 0)
                        topLeftLong = 0;

                    if (topRightLat < 0)
                        topRightLat = 0;

                    if (topRightLong < 0)
                        topRightLong = 0;

                    if (bottomRightLat < 0)
                        bottomRightLat = 0;

                    if (bottomRightLong < 0)
                        bottomRightLong = 0;

                    if (bottomLeftLat < 0)
                        bottomLeftLat = 0;

                    if (bottomLeftLong < 0)
                        bottomLeftLong = 0;

                    //TODO: Eventually we want to be able to load only the NFTs we need rather than having to load them all into memory! We need to run the geo-spatial query on the provider itself! ;-)
                    OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> geoNfts = await LoadAllWeb4GeoNFTsAsync(providerType);

                    if (geoNfts != null && !geoNfts.IsError && geoNfts.Result != null)
                    {
                        List<IWeb4GeoSpatialNFT> matchedGeoNFTs = new List<IWeb4GeoSpatialNFT>();

                        foreach (IWeb4GeoSpatialNFT geoSpatialNFT in geoNfts.Result)
                        {
                            if (geoSpatialNFT.Lat >= bottomLeftLat && geoSpatialNFT.Long >= bottomLeftLong
                                && geoSpatialNFT.Lat <= topLeftLat && geoSpatialNFT.Long >= topLeftLong
                                && geoSpatialNFT.Lat <= topRightLat && geoSpatialNFT.Long <= topRightLong
                                && geoSpatialNFT.Lat >= bottomRightLat && geoSpatialNFT.Long <= bottomRightLong)
                                matchedGeoNFTs.Add(geoSpatialNFT);
                        }

                        result.Result = matchedGeoNFTs;
                    }
                }
                else
                    result = DecodeGeoNFTMetaData(await Data.LoadHolonsByMetaDataAsync("GEONFT.LatLong", string.Concat(latLocation.ToString(), ":", longLocation.ToString()), HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> LoadAllWeb4GeoNFTsForAvatarLocation(long latLocation, long longLocation, int radius, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsForAvatarLocationAsync in NFTManager. Reason:";

            try
            {
                if (radius > 0)
                {
                    //Create a bounding box.
                    long topLeftLat = latLocation - radius;
                    long topLeftLong = longLocation - radius;
                    long topRightLat = latLocation - radius;
                    long topRightLong = longLocation + radius;
                    long bottomRightLat = latLocation - radius;
                    long bottomRightLong = longLocation + radius;
                    long bottomLeftLat = latLocation - radius;
                    long bottomLeftLong = longLocation - radius;

                    if (topLeftLat < 0)
                        topLeftLat = 0;

                    if (topLeftLong < 0)
                        topLeftLong = 0;

                    if (topRightLat < 0)
                        topRightLat = 0;

                    if (topRightLong < 0)
                        topRightLong = 0;

                    if (bottomRightLat < 0)
                        bottomRightLat = 0;

                    if (bottomRightLong < 0)
                        bottomRightLong = 0;

                    if (bottomLeftLat < 0)
                        bottomLeftLat = 0;

                    if (bottomLeftLong < 0)
                        bottomLeftLong = 0;

                    //TODO: Eventually we want to be able to load only the NFTs we need rather than having to load them all into memory! We need to run the geo-spatial query on the provider itself! ;-)
                    OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> geoNfts = LoadAllWeb4GeoNFTs(providerType);

                    if (geoNfts != null && !geoNfts.IsError && geoNfts.Result != null)
                    {
                        List<IWeb4GeoSpatialNFT> matchedGeoNFTs = new List<IWeb4GeoSpatialNFT>();

                        foreach (IWeb4GeoSpatialNFT geoSpatialNFT in geoNfts.Result)
                        {
                            if (geoSpatialNFT.Lat >= bottomLeftLat && geoSpatialNFT.Long >= bottomLeftLong
                                && geoSpatialNFT.Lat <= topLeftLat && geoSpatialNFT.Long >= topLeftLong
                                && geoSpatialNFT.Lat <= topRightLat && geoSpatialNFT.Long <= topRightLong
                                && geoSpatialNFT.Lat >= bottomRightLat && geoSpatialNFT.Long <= bottomRightLong)
                                matchedGeoNFTs.Add(geoSpatialNFT);
                        }

                        result.Result = matchedGeoNFTs;
                    }
                }
                else
                    result = DecodeGeoNFTMetaData(Data.LoadHolonsByMetaData("GEONFT.LatLong", string.Concat(latLocation.ToString(), ":", longLocation.ToString()), HolonType.Web4GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> LoadAllWeb3NFTsAsync(Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTsAsync in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString(), HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
                else
                    result = DecodeNFTMetaData(await Data.LoadAllHolonsAsync(HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb3NFT>> LoadAllWeb3NFTs(Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTs in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString(), HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
                else
                    result = DecodeNFTMetaData(Data.LoadAllHolons(HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTsAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadAllHolonsAsync(HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4NFT>> LoadAllWeb4NFTs(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in LoadAllWeb4NFTs in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadAllHolons(HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> LoadAllWeb4GeoNFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTsAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadAllHolonsAsync(HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> LoadAllWeb4GeoNFTs(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTs in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadAllHolons(HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> PlaceWeb4GeoNFTAsync(IPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in PlaceWeb4GeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IWeb4NFT> loadNftResult = await LoadWeb4NftAsync(request.OriginalWeb4OASISNFTId, request.OriginalWeb4OASISNFTOffChainProvider.Value);

                if (loadNftResult != null && !loadNftResult.IsError && loadNftResult.Result != null)
                {
                    result.Result = CreateWeb4GeoSpatialNFT(request, loadNftResult.Result);
                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateWeb4GeoSpatialNFTMetaDataHolon(result.Result), request.PlacedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider.Value);

                    if ((saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null)) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), request.GeoNFTMetaDataProvider)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = FormatSuccessMessage(result, responseFormatType: responseFormatType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading original OASIS NFT with id {request.OriginalWeb4OASISNFTId}. Reason: {loadNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb4GeoSpatialNFT> PlaceWeb4GeoNFT(IPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in PlaceWeb4GeoNFT in NFTManager. Reason:";

            try
            {
                OASISResult<IWeb4NFT> loadNftResult = LoadWeb4Nft(request.OriginalWeb4OASISNFTId, request.OriginalWeb4OASISNFTOffChainProvider.Value);

                if (loadNftResult != null && !loadNftResult.IsError && loadNftResult.Result != null)
                {
                    result.Result = CreateWeb4GeoSpatialNFT(request, loadNftResult.Result);
                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateWeb4GeoSpatialNFTMetaDataHolon(result.Result), request.PlacedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider.Value);

                    if ((saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null)) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), request.GeoNFTMetaDataProvider)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = FormatSuccessMessage(result, responseFormatType: responseFormatType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading original OASIS NFT with id {request.OriginalWeb4OASISNFTId}. Reason: {loadNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> MintAndPlaceWeb4GeoNFTAsync(IMintAndPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in MintAndPlaceGeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IWeb4NFT> mintNftResult = await MintNftAsync(CreateMintWeb4NFTTransactionRequest(request), true);

                if (mintNftResult != null && mintNftResult.Result != null && !mintNftResult.IsError)
                {
                    PlaceWeb4GeoSpatialNFTRequest placeGeoSpatialNFTRequest = new PlaceWeb4GeoSpatialNFTRequest()
                    {
                        OriginalWeb4OASISNFTId = mintNftResult.Result.Id,
                        OriginalWeb4OASISNFTOffChainProvider = request.OffChainProvider != null ? request.OffChainProvider : new EnumValue<ProviderType>(ProviderType.None),
                        GeoNFTMetaDataProvider = request.GeoNFTMetaDataProvider,
                        PlacedByAvatarId = request.MintedByAvatarId,
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
                        Nft3DObjectURI = request.Nft3DObjectURI
                    };

                    result.Result = CreateWeb4GeoSpatialNFT(placeGeoSpatialNFTRequest, mintNftResult.Result);
                    OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateWeb4GeoSpatialNFTMetaDataHolon(result.Result), request.MintedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider.Value);

                    if (saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the OffChainProvider {Enum.GetName(typeof(ProviderType), request.OffChainProvider.Value)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = FormatSuccessMessage(request, result, responseFormatType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured minting the GEONFT in function MintNftAsync. Reason: {mintNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Put back in when MintNft is created! :)
        //public OASISResult<IWeb4GeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        //{
        //    OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
        //    string errorMessage = "Error occured in MintAndPlaceGeoNFT in NFTManager. Reason:";

        //    try
        //    {
        //        OASISResult<IWeb4OASISNFT> mintNftResult = MintNft(CreateMintNFTTransactionRequest(request), true);

        //        if (mintNftResult != null && mintNftResult.Result != null && !mintNftResult.IsError)
        //        {
        //            PlaceWeb4GeoSpatialNFTRequest placeGeoSpatialNFTRequest = new PlaceWeb4GeoSpatialNFTRequest()
        //            {
        //                OriginalWeb4OASISNFTId = mintNftResult.Result.Id,
        //                OriginalWeb4OASISNFTOffChainProvider = request.OffChainProvider,
        //                GeoNFTMetaDataProvider = request.GeoNFTMetaDataProvider,
        //                PlacedByAvatarId = request.MintedByAvatarId,
        //                Lat = request.Lat,
        //                Long = request.Long,
        //                AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect,
        //                PermSpawn = request.PermSpawn,
        //                GlobalSpawnQuantity = request.GlobalSpawnQuantity,
        //                PlayerSpawnQuantity = request.PlayerSpawnQuantity,
        //                RespawnDurationInSeconds = request.RespawnDurationInSeconds,
        //                Nft2DSprite = request.Nft2DSprite,
        //                Nft2DSpriteURI = request.Nft2DSpriteURI,
        //                Nft3DObject = request.Nft3DObject,
        //                Nft3DObjectURI = request.Nft3DObjectURI
        //            };

        //            result.Result = CreateGeoSpatialNFT(placeGeoSpatialNFTRequest, mintNftResult.Result);
        //            OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateGeoSpatialNFTMetaDataHolon(result.Result), request.MintedByAvatarId, true, true, 0, true, false, request.OffChainProvider.Value);

        //            if (saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null) || saveHolonResult == null)
        //            {
        //                result.Result = null;
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the OffChainProvider {Enum.GetName(typeof(ProviderType), request.OffChainProvider.Value)}. Reason: {saveHolonResult.Message}");
        //            }
        //            else
        //                result.Message = FormatSuccessMessage(request, result, responseFormatType);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured minting the GEONFT in function MintNft. Reason: {mintNftResult.Message}");
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IWeb4NFT>> UpdateWeb4NFTAsync(IUpdateWeb4NFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new();
            string errorMessage = "Error occured in UpdateWeb4NFTAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<IHolon> nftHolonResult = await Data.LoadHolonAsync(request.Id, providerType: providerType);

                if (nftHolonResult != null && nftHolonResult.Result != null && !nftHolonResult.IsError)
                {
                    OASISResult<IWeb4NFT> nftResult = DecodeNFTMetaData(nftHolonResult, result, errorMessage);

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        nftResult.Result.Title = !string.IsNullOrEmpty(request.Title) ? request.Title : nftResult.Result.Title;
                        nftResult.Result.Description = !string.IsNullOrEmpty(request.Description) ? request.Description : nftResult.Result.Description;
                        nftResult.Result.ModifiedByAvatarId = request.ModifiedByAvatarId != Guid.Empty ? request.ModifiedByAvatarId : nftResult.Result.ModifiedByAvatarId;
                        nftResult.Result.ModifiedOn = DateTime.Now;
                        nftResult.Result.ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : nftResult.Result.ImageUrl;
                        nftResult.Result.Image = request.Image != null ? request.Image : nftResult.Result.Image;
                        nftResult.Result.ThumbnailUrl = !string.IsNullOrEmpty(request.ThumbnailUrl) ? request.ThumbnailUrl : nftResult.Result.ThumbnailUrl;
                        nftResult.Result.Thumbnail = request.Thumbnail != null ? request.Thumbnail : nftResult.Result.Thumbnail;
                        nftResult.Result.MetaData = request.MetaData != null ? request.MetaData : nftResult.Result.MetaData;
                        nftResult.Result.Tags = request.Tags ?? nftResult.Result.Tags;
                        nftResult.Result.Price = request.Price.HasValue ? request.Price.Value : nftResult.Result.Price;
                        nftResult.Result.Discount = request.Discount.HasValue ? request.Discount.Value : nftResult.Result.Discount;
                        nftResult.Result.IsForSale = request.IsForSale.HasValue ? request.IsForSale.Value : nftResult.Result.IsForSale;
                        nftResult.Result.SalesHistory = request.SalesHistory ?? nftResult.Result.SalesHistory;
                        nftResult.Result.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : nftResult.Result.RoyaltyPercentage;
                        nftResult.Result.CurrentOwnerAvatarId = request.CurrentOwnerAvatarId != Guid.Empty ? request.CurrentOwnerAvatarId : nftResult.Result.CurrentOwnerAvatarId;
                        nftResult.Result.PreviousOwnerAvatarId = request.PreviousOwnerAvatarId != Guid.Empty ? request.PreviousOwnerAvatarId : nftResult.Result.PreviousOwnerAvatarId;
                        nftResult.Result.LastPurchasedByAvatarId = request.LastPurchasedByAvatarId != Guid.Empty ? request.LastPurchasedByAvatarId : nftResult.Result.LastPurchasedByAvatarId;
                        nftResult.Result.LastSaleAmount = request.LastSaleAmount.HasValue ? request.LastSaleAmount.Value : nftResult.Result.LastSaleAmount;
                        nftResult.Result.LastSaleDate = request.LastSaleDate != DateTime.MinValue ? request.LastSaleDate : nftResult.Result.LastSaleDate;
                        nftResult.Result.LastSaleDiscount = request.LastSaleDiscount.HasValue ? request.LastSaleDiscount.Value : nftResult.Result.LastSaleDiscount;
                        nftResult.Result.LastSalePrice = request.LastSalePrice.HasValue ? request.LastSalePrice.Value : nftResult.Result.LastSalePrice;
                        nftResult.Result.LastSaleQuantity = request.LastSaleQuantity.HasValue ? request.LastSaleQuantity.Value : nftResult.Result.LastSaleQuantity;
                        nftResult.Result.LastSaleTax = request.LastSaleTax.HasValue ? request.LastSaleTax.Value : nftResult.Result.LastSaleTax;
                        nftResult.Result.LastSaleTransactionHash = !string.IsNullOrEmpty(request.LastSaleTransactionHash) ? request.LastSaleTransactionHash : nftResult.Result.LastSaleTransactionHash;
                        nftResult.Result.LastSoldByAvatarId = request.LastSoldByAvatarId != Guid.Empty ? request.LastSoldByAvatarId : nftResult.Result.LastSoldByAvatarId;
                        nftResult.Result.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : nftResult.Result.RoyaltyPercentage;
                        nftResult.Result.SaleEndDate = request.SaleEndDate.HasValue ? request.SaleEndDate.Value : nftResult.Result.SaleEndDate;
                        nftResult.Result.SaleStartDate = request.SaleStartDate.HasValue ? request.SaleStartDate.Value : nftResult.Result.SaleStartDate;
                        nftResult.Result.TotalNumberOfSales = request.TotalNumberOfSales.HasValue ? request.TotalNumberOfSales.Value : nftResult.Result.TotalNumberOfSales;

                        if (request.UpdateChildWebNFTIds == null)
                            request.UpdateChildWebNFTIds = new List<string>();

                        if (request.UpdateAllChildWeb3NFTs)
                        {
                            foreach (Web3NFT web3NFT in nftResult.Result.Web3NFTs)
                                request.UpdateChildWebNFTIds.Add(web3NFT.Id.ToString());
                        }

                        //Update the embedded web3 NFTs... TODO: (maybe one day we will not need to embed?)
                        foreach (Web3NFT web3NFT in nftResult.Result.Web3NFTs)
                        {
                            if (request.UpdateChildWebNFTIds.Contains(web3NFT.Id.ToString()))
                            {
                                //web3NFT = UpdateWeb3NFT(web3NFT, request);
                                UpdateWeb3NFT(web3NFT, request); //TODO: Double check that the web3NFTs are updated.
                            }
                        }

                        OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(UpdateWeb4NFTMetaDataHolon(nftHolonResult.Result, nftResult.Result), request.ModifiedByAvatarId, providerType: providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(nftResult, result);
                            result.Result = nftResult.Result;

                            //Now we need to update the web3 NFT Holons...
                            OASISResult<bool> updateWeb3NFTHolonsResult = await UpdateWeb3NFTHolonsAsync(request, providerType);

                            if (updateWeb3NFTHolonsResult != null && updateWeb3NFTHolonsResult.Result && !updateWeb3NFTHolonsResult.IsError)
                                result.Message = "Web4 OASIS NFT Updated Successfully.";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured updating the child WEB3 NFT Holons. Reason: {updateWeb3NFTHolonsResult?.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving Web4 OASIS NFT. Reason: {saveHolonResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading Web4 OASIS NFT. Reason: {nftResult?.Message}");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading Web4 OASIS NFT Holon. Reason: {nftHolonResult?.Message}");
                    return result;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> UpdateWeb4GeoNFTAsync(IUpdateWeb4GeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new();
            string errorMessage = "Error occured in UpdateWeb4GeoNFTAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<IHolon> nftHolonResult = await Data.LoadHolonAsync(request.Id, providerType: providerType);

                if (nftHolonResult != null && nftHolonResult.Result != null && !nftHolonResult.IsError)
                {
                    OASISResult<IWeb4GeoSpatialNFT> nftResult = DecodeGeoNFTMetaData(nftHolonResult, result, errorMessage);

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        nftResult.Result.Title = !string.IsNullOrEmpty(request.Title) ? request.Title : nftResult.Result.Title;
                        nftResult.Result.Description = !string.IsNullOrEmpty(request.Description) ? request.Description : nftResult.Result.Description;
                        nftResult.Result.ModifiedByAvatarId = request.ModifiedByAvatarId != Guid.Empty ? request.ModifiedByAvatarId : nftResult.Result.ModifiedByAvatarId;
                        nftResult.Result.ModifiedOn = DateTime.Now;
                        nftResult.Result.ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : nftResult.Result.ImageUrl;
                        nftResult.Result.Image = request.Image != null ? request.Image : nftResult.Result.Image;
                        nftResult.Result.ThumbnailUrl = !string.IsNullOrEmpty(request.ThumbnailUrl) ? request.ThumbnailUrl : nftResult.Result.ThumbnailUrl;
                        nftResult.Result.Thumbnail = request.Thumbnail != null ? request.Thumbnail : nftResult.Result.Thumbnail;
                        nftResult.Result.MetaData = request.MetaData != null ? request.MetaData : nftResult.Result.MetaData;
                        nftResult.Result.Tags = request.Tags ?? nftResult.Result.Tags;
                        nftResult.Result.Lat = request.Lat.HasValue ? request.Lat.Value : nftResult.Result.Lat;
                        nftResult.Result.Long = request.Long.HasValue ? request.Long.Value : nftResult.Result.Long;
                        nftResult.Result.AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect.HasValue ? request.AllowOtherPlayersToAlsoCollect.Value : nftResult.Result.AllowOtherPlayersToAlsoCollect;
                        nftResult.Result.PermSpawn = request.PermSpawn.HasValue ? request.PermSpawn.Value : nftResult.Result.PermSpawn;
                        nftResult.Result.GlobalSpawnQuantity = request.GlobalSpawnQuantity.HasValue ? request.GlobalSpawnQuantity.Value : nftResult.Result.GlobalSpawnQuantity;
                        nftResult.Result.PlayerSpawnQuantity = request.PlayerSpawnQuantity.HasValue ? request.PlayerSpawnQuantity.Value : nftResult.Result.PlayerSpawnQuantity;
                        nftResult.Result.RespawnDurationInSeconds = request.RespawnDurationInSeconds.HasValue ? request.RespawnDurationInSeconds.Value : nftResult.Result.RespawnDurationInSeconds;
                        nftResult.Result.Nft2DSprite = request.Nft2DSprite != null ? request.Nft2DSprite : nftResult.Result.Nft2DSprite;
                        nftResult.Result.Nft2DSpriteURI = !string.IsNullOrEmpty(request.Nft2DSpriteURI) ? request.Nft2DSpriteURI : nftResult.Result.Nft2DSpriteURI;
                        nftResult.Result.Nft3DObject = request.Nft3DObject != null ? request.Nft3DObject : nftResult.Result.Nft3DObject;
                        nftResult.Result.Nft3DObjectURI = !string.IsNullOrEmpty(request.Nft3DObjectURI) ? request.Nft3DObjectURI : nftResult.Result.Nft3DObjectURI;

                        if (request.UpdateChildWebNFTIds == null)
                            request.UpdateChildWebNFTIds = new List<string>();

                        if (request.UpdateAllChildWeb3NFTs)
                        {
                            foreach (Web3NFT web3NFT in nftResult.Result.Web3NFTs)
                                request.UpdateChildWebNFTIds.Add(web3NFT.Id.ToString());
                        }

                        foreach (Web3NFT web3NFT in nftResult.Result.Web3NFTs)
                        {
                            if (request.UpdateChildWebNFTIds.Contains(web3NFT.Id.ToString()))
                                UpdateWeb3NFT(web3NFT, request); //TODO: Double check that the web3NFTs are updated.
                        }

                        OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(UpdateWeb4GeoNFTMetaDataHolon(nftHolonResult.Result, nftResult.Result), request.ModifiedByAvatarId, providerType: providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(nftResult, result);
                            result.Result = nftResult.Result;

                            //Now we need to update the web3 NFT Holons...
                            OASISResult<bool> updateWeb3NFTHolonsResult = await UpdateWeb3NFTHolonsAsync(request, providerType);

                            if (updateWeb3NFTHolonsResult != null && updateWeb3NFTHolonsResult.Result && !updateWeb3NFTHolonsResult.IsError)
                                result.Message = "Web4 OASIS Geo-NFT Updated Successfully.";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured updating the child WEB3 NFT Holons. Reason: {updateWeb3NFTHolonsResult?.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving Web4 OASIS Geo-NFT. Reason: {saveHolonResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading Web4 OASIS Geo-NFT. Reason: {nftResult?.Message}");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading Web4 OASIS Geo-NFT Holon. Reason: {nftHolonResult?.Message}");
                    return result;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        private async Task<OASISResult<bool>> UpdateWeb3NFTHolonsAsync(IUpdateWeb4NFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<IWeb3NFT>> web3NFTs = await LoadAllWeb3NFTsAsync(request.Id, providerType);
            string errorMessage = "Error occured in NFTManager.UpdateWeb3NFTHolonsAsync. Reason: ";

            if (web3NFTs != null && web3NFTs.Result != null && !web3NFTs.IsError)
            {
                foreach (IWeb3NFT web3NFT in web3NFTs.Result)
                {
                    if (request.UpdateChildWebNFTIds.Contains(web3NFT.Id.ToString()))
                    {
                        IWeb3NFT updatedWeb3NFT = UpdateWeb3NFT(web3NFT, request);

                        OASISResult<IHolon> web3NftHolonResult = await Data.LoadHolonAsync(request.Id, providerType: providerType);

                        if (web3NftHolonResult != null && web3NftHolonResult.Result != null && !web3NftHolonResult.IsError)
                        {
                            OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(UpdateWeb3NFTMetaDataHolon(web3NftHolonResult.Result, updatedWeb3NFT, request.Id), request.ModifiedByAvatarId, providerType: providerType);

                            if (!(saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the Web3 NFT Holon for id {updatedWeb3NFT.Id} and title '{updatedWeb3NFT.Title}'. Reason: {saveHolonResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Web3 OASIS NFT Holon. Reason: {web3NftHolonResult?.Message}");
                    }
                }
            }

            if (!result.IsError)
                result.Result = true;

            return result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnWeb3NFTAsync(Guid avatarId, IBurnWeb3NFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFTTransactionResponse> result = new();
            string errorMessage = "Error occured in BurnWeb3NFTAsync in NFTManager. Reason:";

            try
            {
                if (string.IsNullOrEmpty(request.NFTTokenAddress) && request.Web3NFTId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Both the NFTTokenAddress and Web3NFTId are missing, you need to specify at least one of these!");
                    return result;
                }

                if (string.IsNullOrEmpty(request.NFTTokenAddress))
                {
                    OASISResult<IWeb3NFT> web3NFTResult = await LoadWeb3NftAsync(request.Web3NFTId);

                    if (web3NFTResult != null && web3NFTResult.Result != null && web3NFTResult.IsError!)
                    {
                        request.NFTTokenAddress = web3NFTResult.Result.NFTTokenAddress;
                        //request.MintWalletAddress = web3NFTResult.Result.OASISMintWalletAddress;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadWeb3NftAsync to resolve the NFTTokenAddress. Reason: {web3NFTResult.Message}");
                        return result;
                    }
                }

                OASISResult<IOASISNFTProvider> providerResult = GetNFTProvider(providerType);

                if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                {
                    bool burnt = false;
                    DateTime startTime = DateTime.Now;

                    do
                    {
                        try
                        {
                            OASISResult<IWeb3NFTTransactionResponse> burnResult = await providerResult.Result.BurnNFTAsync(request);
                            string burnErrorMessage = "";

                            if (burnResult != null && burnResult.Result != null && !burnResult.IsError)
                            {
                                burnt = true;
                                result.Result = burnResult.Result;
                                result.Message = "Web3 NFT Burnt Successfully!";
                            }
                            else
                                burnErrorMessage = $"{errorMessage} Error occured calling BurnNFTAsync on provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {providerResult.Message}.";

                            if (!burnt && !request.WaitTillNFTBurnt)
                            {
                                OASISErrorHandling.HandleError(ref result, $"{burnErrorMessage} WaitTillNFTBurnt is false so aborting!");
                                break;
                            }

                            Thread.Sleep(request.AttemptToBurnEveryXSeconds * 1000);

                            if (startTime.AddSeconds(request.WaitForNFTToBurnInSeconds).Ticks < DateTime.Now.Ticks)
                            {
                                OASISErrorHandling.HandleError(ref result, $"{burnErrorMessage}Timeout expired, WaitForNFTToBurnInSeconds ({request.WaitForNFTToBurnInSeconds}) exceeded, try increasing and trying again!");
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured calling BurnNFTAsync on provider {Enum.GetName(typeof(ProviderType), providerType)} : {e.Message}", e);
                        }
                    }
                    while (!burnt);
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteWeb3NFTAsync(Guid avatarId, Guid id, bool softDelete = true, bool burnWeb3NFT = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteWeb3NFTAsync in NFTManager. Reason:";

            try
            {
                if (burnWeb3NFT)
                {
                    OASISResult<IWeb3NFTTransactionResponse> burnResult = await BurnWeb3NFTAsync(avatarId, new BurnWeb3NFTRequest() 
                    { 
                        Web3NFTId = id, 
                        OwnerPrivateKey = "", 
                        OwnerPublicKey = "", 
                        OwnerSeedPhrase = "" }, providerType);

                    if (!(burnResult != null && burnResult.Result != null && !burnResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured burning Web3 NFT with id {id}. Reason: {burnResult?.Message}");
                }

                OASISResult<IHolon> deleteWeb4NFTResult = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (deleteWeb4NFTResult != null && !deleteWeb4NFTResult.IsError && deleteWeb4NFTResult.Result != null)
                {
                    result.Result = true;

                    if (result.IsWarning)
                        result.Message = $"Web3 NFT deleted successfully but there was an issue burning the web3 NFT:\n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)} ";
                    else
                        result.Message = "Web3 NFT deleted successfully"; ;
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting Web3 NFT. Reason: {deleteWeb4NFTResult?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteWeb4NFTAsync(Guid avatarId, Guid id, bool softDelete = true, bool deleteChildWeb3NFTs = true, bool burnChildWeb3NFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteWeb4NFTAsync in NFTManager. Reason:";

            try
            {
                if (deleteChildWeb3NFTs)
                {
                    OASISResult<IEnumerable<IWeb3NFT>> web3NFTs = await LoadAllWeb3NFTsAsync(id, providerType);

                    if (web3NFTs != null && web3NFTs.Result != null && !web3NFTs.IsError)
                    {
                        foreach (IWeb3NFT web3NFT in web3NFTs.Result)
                        {
                            OASISResult<bool> deleteWeb3NFTResult = await DeleteWeb3NFTAsync(avatarId, id, softDelete, burnChildWeb3NFTs, providerType);

                            if (!(deleteWeb3NFTResult != null && !deleteWeb3NFTResult.IsError && deleteWeb3NFTResult.Result != null))
                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting Web3 NFT with id {web3NFT.Id} and title '{web3NFT.Title}'. Reason: {deleteWeb3NFTResult?.Message}");
                        }
                    }
                }

                OASISResult<IHolon> deleteWeb4NFTResult = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (deleteWeb4NFTResult != null && !deleteWeb4NFTResult.IsError && deleteWeb4NFTResult.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;

                    if (result.IsWarning)
                        result.Message = $"Web4 NFT deleted successfully but there were issues deleting one or more of the child web3 NFTs:\n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)} ";
                    else
                        result.Message = "Web4 NFT deleted successfully";
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting Web4 NFT. Reason: {deleteWeb4NFTResult?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteWeb4GeoNFTAsync(Guid avatarId, Guid id, bool softDelete = true, bool deleteChildWeb3NFTs = true, bool burnChildWeb3NFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteWeb4GeoNFTAsync in NFTManager. Reason:";

            try
            {
                if (deleteChildWeb3NFTs)
                {
                    OASISResult<IEnumerable<IWeb3NFT>> web3NFTs = await LoadAllWeb3NFTsAsync(id, providerType);

                    if (web3NFTs != null && web3NFTs.Result != null && !web3NFTs.IsError)
                    {
                        foreach (IWeb3NFT web3NFT in web3NFTs.Result)
                        {
                            OASISResult<bool> deleteWeb3NFTResult = await DeleteWeb3NFTAsync(avatarId, id, softDelete, burnChildWeb3NFTs, providerType);

                            if (!(deleteWeb3NFTResult != null && !deleteWeb3NFTResult.IsError && deleteWeb3NFTResult.Result != null))
                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting Web3 NFT with id {web3NFT.Id} and title '{web3NFT.Title}'. Reason: {deleteWeb3NFTResult?.Message}");
                        }
                    }
                }

                OASISResult<IHolon> deleteWeb4NFTResult = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (deleteWeb4NFTResult != null && !deleteWeb4NFTResult.IsError && deleteWeb4NFTResult.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;

                    if (result.IsWarning)
                        result.Message = $"Web4 Geo-NFT deleted successfully but there were issues deleting one or more of the child web3 NFTs:\n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)} ";
                    else
                        result.Message = "Web4 Geo-NFT deleted successfully";
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting Web4 Geo-NFT. Reason: {deleteWeb4NFTResult?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> SearchWeb3NFTsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchWeb3NFTsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            result = DecodeNFTMetaData(await Data.SearchHolonsAsync(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public OASISResult<IEnumerable<IWeb3NFT>> SearchWeb3NFTs(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in SearchWeb3NFTs in NFTManager. Reason:";
            result = DecodeNFTMetaData(Data.SearchHolons(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> SearchWeb4NFTsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            result = DecodeNFTMetaData(await Data.SearchHolonsAsync(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public OASISResult<IEnumerable<IWeb4NFT>> SearchWeb4NFTs(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in SearchNFTs in NFTManager. Reason:";
            result = DecodeNFTMetaData(Data.SearchHolons(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> SearchWeb4GeoNFTsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in SearchGeoNFTsAsync in NFTManager. Reason:";
            result = DecodeGeoNFTMetaData(await Data.SearchHolonsAsync(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }
        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> SearchWeb4GeoNFTs(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in SearchGeoNFT in NFTManager. Reason:";
            result = DecodeGeoNFTMetaData(Data.SearchHolons(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFTCollection>>> SearchWeb4NFTCollectionsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTCollectionsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4NFTCollection>> result = new OASISResult<IEnumerable<IWeb4NFTCollection>>();
            OASISResult<IEnumerable<Web4NFTCollection>> collectionResults = await Data.SearchHolonsAsync<Web4NFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4NFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public OASISResult<IEnumerable<IWeb4NFTCollection>> SearchWeb4NFTCollections(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTCollections in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4NFTCollection>> result = new OASISResult<IEnumerable<IWeb4NFTCollection>>();
            OASISResult<IEnumerable<Web4NFTCollection>> collectionResults = Data.SearchHolons<Web4NFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4NFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoNFTCollection>>> SearchWeb4GeoNFTCollectionsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchGeoNFTCollectionsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4GeoNFTCollection>> result = new OASISResult<IEnumerable<IWeb4GeoNFTCollection>>();
            OASISResult<IEnumerable<Web4GeoNFTCollection>> collectionResults = await Data.SearchHolonsAsync<Web4GeoNFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public OASISResult<IEnumerable<IWeb4GeoNFTCollection>> SearchWeb4GeoNFTCollections(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchGeoNFTCollections in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4GeoNFTCollection>> result = new OASISResult<IEnumerable<IWeb4GeoNFTCollection>>();
            OASISResult<IEnumerable<Web4GeoNFTCollection>> collectionResults = Data.SearchHolons<Web4GeoNFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public OASISResult<IOASISNFTProvider> GetNFTProvider(ProviderType providerType)
        {
            OASISResult<IOASISNFTProvider> result = new OASISResult<IOASISNFTProvider>();
            IOASISProvider OASISProvider = ProviderManager.Instance.GetProvider(providerType);

            if (OASISProvider != null)
            {
                if (!OASISProvider.IsProviderActivated)
                {
                    OASISResult<bool> activateProviderResult = OASISProvider.ActivateProvider();

                    if (activateProviderResult.IsError)
                        OASISErrorHandling.HandleError(ref result, $"Error occured in GetNFTProvider. Error occured activating provider. Reason: {activateProviderResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetNFTProvider. The {Enum.GetName(typeof(ProviderType), providerType)} provider was not found.");

            if (!result.IsError)
            {
                result.Result = OASISProvider as IOASISNFTProvider;

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetNFTProvider. The {Enum.GetName(typeof(ProviderType), providerType)} provider is not a valid OASISNFTProvider.");
            }

            return result;
        }


        public async Task<OASISResult<IWeb4NFTCollection>> CreateWeb4NFTCollectionAsync(ICreateWeb4NFTCollectionRequest createOASISNFTCollectionRequest, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new OASISResult<IWeb4NFTCollection>();
            string errorMessage = "Error occured in CreateNFTCollectionAsync in NFTManager. Reason:";

            Web4NFTCollection OASISNFTCollection = new Web4NFTCollection()
            {
                Name = createOASISNFTCollectionRequest.Title,
                Description = createOASISNFTCollectionRequest.Description,
                CreatedDate = DateTime.Now,
                CreatedByAvatarId = createOASISNFTCollectionRequest.CreatedBy,
                Image = createOASISNFTCollectionRequest.Image,
                ImageUrl = createOASISNFTCollectionRequest.ImageUrl,
                Thumbnail = createOASISNFTCollectionRequest.Thumbnail,
                ThumbnailUrl = createOASISNFTCollectionRequest.ThumbnailUrl,
                MetaData = createOASISNFTCollectionRequest.MetaData,
                Web4NFTs = createOASISNFTCollectionRequest.Web4NFTs,
                Web4NFTIds = createOASISNFTCollectionRequest.Web4NFTIds,
                Tags = createOASISNFTCollectionRequest.Tags
            };

            if (createOASISNFTCollectionRequest.Web4NFTIds == null)
                createOASISNFTCollectionRequest.Web4NFTIds = new List<string>();

            if (createOASISNFTCollectionRequest.Web4NFTs != null)
            {
                foreach (IWeb4NFT oasisNft in createOASISNFTCollectionRequest.Web4NFTs)
                {
                    if (!OASISNFTCollection.Web4NFTIds.Contains(oasisNft.Id.ToString()))
                        OASISNFTCollection.Web4NFTIds.Add(oasisNft.Id.ToString());
                }
            }

            //TODO: Not sure if we should store the entire NFTs in the collection or just their IDs?
            List<IWeb4NFT> nfts = OASISNFTCollection.Web4NFTs;
            OASISNFTCollection.Web4NFTs = null;

            OASISResult<Web4NFTCollection> saveResult = await OASISNFTCollection.SaveAsync<Web4NFTCollection>();

            //Dictionary<string, object> metaData = new Dictionary<string, object>()
            //{
            //    { "OASISNFTCOLLECTION.ID", Guid.NewGuid() },
            //    { "OASISNFTCOLLECTION.Title", createOASISNFTCollectionRequest.Title },
            //    { "OASISNFTCOLLECTION.Description", createOASISNFTCollectionRequest.Description  },
            //    { "OASISNFTCOLLECTION.CreatedDate", OASISNFTCollection.CreatedDate  },
            //    { "OASISNFTCOLLECTION.CreatedBy", createOASISNFTCollectionRequest.CreatedBy  },
            //    { "OASISNFTCOLLECTION.ImageUrl", createOASISNFTCollectionRequest.ImageUrl  },
            //    { "OASISNFTCOLLECTION.Image", createOASISNFTCollectionRequest.Image  },
            //    { "OASISNFTCOLLECTION.ThumbnailUrl", createOASISNFTCollectionRequest.ThumbnailUrl  },
            //    { "OASISNFTCOLLECTION.Thumbnail", createOASISNFTCollectionRequest.Thumbnail  },
            //    { "OASISNFTCOLLECTION.Web4NFTIds", createOASISNFTCollectionRequest.Web4NFTIds  },
            //    { "OASISNFTCOLLECTION.Tags", createOASISNFTCollectionRequest.Tags },
            //    { "OASISNFTCOLLECTION.MetaData", createOASISNFTCollectionRequest.MetaData }
            //};

            //OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(new Holon()
            //{
            //    Id = Guid.Parse(metaData["OASISNFTCOLLECTION.ID"].ToString()),
            //    Name = $"OASIS NFT Collection with title {createOASISNFTCollectionRequest.Title}",
            //    Description = createOASISNFTCollectionRequest.Description,
            //    HolonType = HolonType.Web4NFTCollection,
            //    MetaData = metaData
            //}, providerType : providerType);

            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
            {
                OASISNFTCollection.Web4NFTs = nfts;
                result.Result = OASISNFTCollection;
                result.Message = "OASIS NFT Collection created successfully.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS NFT Collection holon. Reason: {saveResult.Message}");

            return result;
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> CreateWeb4GeoNFTCollectionAsyc(ICreateWeb4GeoNFTCollectionRequest createWeb4OASISGeoNFTCollectionRequest, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoNFTCollection> result = new OASISResult<IWeb4GeoNFTCollection>();
            string errorMessage = "Error occured in CreateGeoNFTCollectionAsyc in NFTManager. Reason:";

            Web4GeoNFTCollection Web4OASISGeoNFTCollection = new Web4GeoNFTCollection()
            {
                Name = createWeb4OASISGeoNFTCollectionRequest.Title,
                Description = createWeb4OASISGeoNFTCollectionRequest.Description,
                CreatedDate = DateTime.Now,
                CreatedByAvatarId = createWeb4OASISGeoNFTCollectionRequest.CreatedBy,
                Image = createWeb4OASISGeoNFTCollectionRequest.Image,
                ImageUrl = createWeb4OASISGeoNFTCollectionRequest.ImageUrl,
                Thumbnail = createWeb4OASISGeoNFTCollectionRequest.Thumbnail,
                ThumbnailUrl = createWeb4OASISGeoNFTCollectionRequest.ThumbnailUrl,
                MetaData = createWeb4OASISGeoNFTCollectionRequest.MetaData,
                Web4GeoNFTs = createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTs,
                Web4GeoNFTIds = createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTIds,
                Tags = createWeb4OASISGeoNFTCollectionRequest.Tags
            };

            if (createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTIds == null)
                createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTIds = new List<string>();

            if (createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTIds != null)
            {
                foreach (IWeb4GeoSpatialNFT geoNFT in createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTs)
                {
                    if (!Web4OASISGeoNFTCollection.Web4GeoNFTIds.Contains(geoNFT.Id.ToString()))
                        Web4OASISGeoNFTCollection.Web4GeoNFTIds.Add(geoNFT.Id.ToString());
                }
            }

            //TODO: Not sure if we should store the entire NFTs in the collection or just their IDs?
            List<IWeb4GeoSpatialNFT> nfts = Web4OASISGeoNFTCollection.Web4GeoNFTs;
            Web4OASISGeoNFTCollection.Web4GeoNFTs = null;
            OASISResult<Web4GeoNFTCollection> saveResult = await Web4OASISGeoNFTCollection.SaveAsync<Web4GeoNFTCollection>();

            //Dictionary<string, object> metaData = new Dictionary<string, object>()
            //{
            //    { "OASISGEONFTCOLLECTION.ID", Guid.NewGuid() },
            //    { "OASISGEONFTCOLLECTION.Title", createWeb4OASISGeoNFTCollectionRequest.Title },
            //    { "OASISGEONFTCOLLECTION.Description", createWeb4OASISGeoNFTCollectionRequest.Description  },
            //    { "OASISGEONFTCOLLECTION.CreatedDate", Web4OASISGeoNFTCollection.CreatedDate  },
            //    { "OASISGEONFTCOLLECTION.CreatedBy", createWeb4OASISGeoNFTCollectionRequest.CreatedBy  },
            //    { "OASISGEONFTCOLLECTION.ImageUrl", createWeb4OASISGeoNFTCollectionRequest.ImageUrl  },
            //    { "OASISGEONFTCOLLECTION.Image", createWeb4OASISGeoNFTCollectionRequest.Image  },
            //    { "OASISGEONFTCOLLECTION.ThumbnailUrl", createWeb4OASISGeoNFTCollectionRequest.ThumbnailUrl  },
            //    { "OASISGEONFTCOLLECTION.Thumbnail", createWeb4OASISGeoNFTCollectionRequest.Thumbnail  },
            //    { "OASISGEONFTCOLLECTION.Web4GeoNFTIds", createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTIds  },
            //    { "OASISGEONFTCOLLECTION.Tags", createWeb4OASISGeoNFTCollectionRequest.Tags },
            //    { "OASISGEONFTCOLLECTION.MetaData", createWeb4OASISGeoNFTCollectionRequest.MetaData }
            //};

            //OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(new Holon()
            //{
            //    Id = Guid.Parse(metaData["OASISGEONFTCOLLECTION.ID"].ToString()),
            //    Name = $"OASIS GeoNFT Collection with title {createWeb4OASISGeoNFTCollectionRequest.Title}",
            //    Description = createWeb4OASISGeoNFTCollectionRequest.Description,
            //    HolonType = HolonType.Web4NFTCollection,
            //    MetaData = metaData
            //}, providerType: providerType);

            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
            {
                Web4OASISGeoNFTCollection.Web4GeoNFTs = nfts;
                result.Result = Web4OASISGeoNFTCollection;
                result.Message = "OASIS GeoNFT Collection created successfully.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS GeoNFT Collection holon. Reason: {saveResult.Message}");


            return result;
        }


        public async Task<OASISResult<IWeb4NFTCollection>> UpdateWeb4NFTCollectionAsync(IUpdateWeb4NFTCollectionRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new();
            string errorMessage = "Error occured in UpdateNFCollectionAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<Web4NFTCollection> holonResult = await Data.LoadHolonAsync<Web4NFTCollection>(request.Id, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    holonResult.Result.Name = !string.IsNullOrEmpty(request.Title) ? request.Title : holonResult.Result.Name;
                    holonResult.Result.Description = !string.IsNullOrEmpty(request.Description) ? request.Description : holonResult.Result.Description;
                    holonResult.Result.ModifiedByAvatarId = request.ModifiedBy != Guid.Empty ? request.ModifiedBy : holonResult.Result.ModifiedByAvatarId;
                    holonResult.Result.ModifiedDate = DateTime.Now;
                    holonResult.Result.ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : holonResult.Result.ImageUrl;
                    holonResult.Result.Image = request.Image != null ? request.Image : holonResult.Result.Image;
                    holonResult.Result.ThumbnailUrl = !string.IsNullOrEmpty(request.ThumbnailUrl) ? request.ThumbnailUrl : holonResult.Result.ThumbnailUrl;
                    holonResult.Result.Thumbnail = request.Thumbnail != null ? request.Thumbnail : holonResult.Result.Thumbnail;
                    holonResult.Result.MetaData = request.MetaData != null ? request.MetaData : holonResult.Result.MetaData;
                    //holonResult.Result.Web4NFTIds = request.Web4NFTIds ?? holonResult.Result.Web4NFTIds;
                    holonResult.Result.Tags = request.Tags ?? holonResult.Result.Tags;

                    OASISResult<Web4NFTCollection> saveResult = await Data.SaveHolonAsync<Web4NFTCollection>(holonResult.Result);

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        result.Result = saveResult.Result;
                        result.Message = "OASIS NFT Collection Updated Successfully.";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS NFT Collection holon. Reason: {saveResult?.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS NFT Collection holon. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> UpdateWeb4GeoNFTCollectionAsync(IUpdateWeb4GeoNFTCollectionRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoNFTCollection> result = new();
            string errorMessage = "Error occured in UpdateGeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<Web4GeoNFTCollection> holonResult = await Data.LoadHolonAsync<Web4GeoNFTCollection>(request.Id, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    //Dictionary<string, object> metaData = new Dictionary<string, object>()
                    //{
                    //    { "OASISGEONFTCOLLECTION.ID", request.Id == Guid.Empty ? holonResult.Result.Id : request.Id },
                    //    //{ "OASISGEONFTCOLLECTION.Title", !string.IsNullOrEmpty(request.Title) ? request.Title : holonResult.Result.MetaData != null && holonResult.Result.MetaData.ContainsKey("Title") ? holonResult.Result.MetaData["Title"] : "" },
                    //    { "OASISGEONFTCOLLECTION.Title", !string.IsNullOrEmpty(request.Title) ? request.Title : holonResult.Result.Name },
                    //    { "OASISGEONFTCOLLECTION.Description",  !string.IsNullOrEmpty(request.Description) ? request.Description : holonResult.Result.Description },
                    //    { "OASISGEONFTCOLLECTION.ModifiedBy", request.ModifiedBy != Guid.Empty ? request.ModifiedBy : holonResult.Result.ModifiedByAvatarId },
                    //    { "OASISGEONFTCOLLECTION.ModifiedOn", DateTime.Now },
                    //    { "OASISGEONFTCOLLECTION.ImageUrl", !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : holonResult.Result.ImageUrl },
                    //    { "OASISGEONFTCOLLECTION.Image", request.Image },
                    //    { "OASISGEONFTCOLLECTION.ThumbnailUrl", request.ThumbnailUrl },
                    //    { "OASISGEONFTCOLLECTION.Thumbnail", request.Thumbnail },
                    //    { "OASISGEONFTCOLLECTION.Web4GeoNFTIds", request.Web4GeoNFTIds ?? new List<string>() },
                    //    { "OASISGEONFTCOLLECTION.Tags", request.Tags },
                    //    { "OASISGEONFTCOLLECTION.MetaData", request.MetaData }
                    //};

                    //holonResult.Result.MetaData = metaData;

                    holonResult.Result.Name = !string.IsNullOrEmpty(request.Title) ? request.Title : holonResult.Result.Name;
                    holonResult.Result.Description = !string.IsNullOrEmpty(request.Description) ? request.Description : holonResult.Result.Description;
                    holonResult.Result.ModifiedByAvatarId = request.ModifiedBy != Guid.Empty ? request.ModifiedBy : holonResult.Result.ModifiedByAvatarId;
                    holonResult.Result.ModifiedDate = DateTime.Now;
                    holonResult.Result.ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : holonResult.Result.ImageUrl;
                    holonResult.Result.Image = request.Image != null ? request.Image : holonResult.Result.Image;
                    holonResult.Result.ThumbnailUrl = !string.IsNullOrEmpty(request.ThumbnailUrl) ? request.ThumbnailUrl : holonResult.Result.ThumbnailUrl;
                    holonResult.Result.Thumbnail = request.Thumbnail != null ? request.Thumbnail : holonResult.Result.Thumbnail;
                    holonResult.Result.MetaData = request.MetaData != null ? request.MetaData : holonResult.Result.MetaData;
                    // holonResult.Result.Web4GeoNFTIds = request.Web4GeoNFTIds ?? holonResult.Result.Web4GeoNFTIds;
                    holonResult.Result.Tags = request.Tags ?? holonResult.Result.Tags;

                    OASISResult<Web4GeoNFTCollection> saveResult = await Data.SaveHolonAsync<Web4GeoNFTCollection>(holonResult.Result);

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        //IWeb4OASISGeoNFTCollection coll = new Web4OASISGeoNFTCollection()
                        //{
                        //    Id = holonResult.Result.Id,
                        //    Name = request.Title,
                        //    Description = request.Description,
                        //    CreatedDate = holonResult.Result.CreatedDate,
                        //    CreatedByAvatarId = holonResult.Result.CreatedByAvatarId,
                        //    ModifiedDate = holonResult.Result.ModifiedDate,
                        //    ModifiedByAvatarId = holonResult.Result.ModifiedByAvatarId,
                        //    Image = request.Image,
                        //    ImageUrl = request.ImageUrl,
                        //    Thumbnail = request.Thumbnail,
                        //    ThumbnailUrl = request.ThumbnailUrl,
                        //    MetaData = request.MetaData,
                        //    Web4GeoNFTIds = request.Web4GeoNFTIds ?? new List<string>(),
                        //    Web4GeoNFTs = request.Web4GeoNFTs,
                        //    Tags = request.Tags
                        //};

                        //result.Result = coll;

                        result.Result = saveResult.Result;
                        result.Message = "OASIS GeoNFT Collection Updated Successfully.";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS GeoNFT Collection holon. Reason: {saveResult?.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS GeoNFT Collection holon. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFTCollection>> AddWeb4NFTToCollectionAsync(Guid collectionId, Guid OASISNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new();
            string errorMessage = "Error occured in AddNFTToCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4NFTCollection> holonResult = await Data.LoadHolonAsync<Web4NFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (!holonResult.Result.Web4NFTIds.Contains(OASISNFTId.ToString()))
                    {
                        holonResult.Result.Web4NFTIds.Add(OASISNFTId.ToString());

                        OASISResult<Web4NFTCollection> saveResult = await Data.SaveHolonAsync<Web4NFTCollection>(holonResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            result.Result = saveResult.Result;
                            result.Message = "OASIS NFT Added To Collection Successfully.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured adding OASIS NFT to collection. Reason: {saveResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured adding OASIS NFT to collection. Reason: NFT already added!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS NFT Collection. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }
        public async Task<OASISResult<IWeb4NFTCollection>> AddWeb4NFTToCollectionAsync(Guid collectionId, IWeb4NFT OASISNFT, ProviderType providerType = ProviderType.Default)
        {
            return await AddWeb4NFTToCollectionAsync(collectionId, OASISNFT.Id, providerType);
        }

        public async Task<OASISResult<IWeb4NFTCollection>> RemoveWeb4NFTFromCollectionAsync(Guid collectionId, Guid OASISNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new();
            string errorMessage = "Error occured in RemoveNFTFromCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4NFTCollection> holonResult = await Data.LoadHolonAsync<Web4NFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (holonResult.Result.Web4NFTIds.Contains(OASISNFTId.ToString()))
                    {
                        holonResult.Result.Web4NFTIds.Remove(OASISNFTId.ToString());

                        OASISResult<Web4NFTCollection> saveResult = await Data.SaveHolonAsync<Web4NFTCollection>(holonResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            result.Result = saveResult.Result;
                            result.Message = "OASIS NFT Removed From Collection Successfully.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured removing OASIS NFT from collection. Reason: {saveResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured removing OASIS NFT from collection. Reason: NFT Not Found!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS NFT Collection. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFTCollection>> RemoveWeb4NFTFromCollectionAsync(Guid collectionId, IWeb4NFT OASISNFT, ProviderType providerType = ProviderType.Default)
        {
            return await RemoveWeb4NFTFromCollectionAsync(collectionId, OASISNFT.Id, providerType);
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> AddWeb4GeoNFTToCollectionAsync(Guid collectionId, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoNFTCollection> result = new();
            string errorMessage = "Error occured in AddOASISGeoNFTToCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4GeoNFTCollection> holonResult = await Data.LoadHolonAsync<Web4GeoNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (!holonResult.Result.Web4GeoNFTIds.Contains(OASISGeoNFTId.ToString()))
                    {
                        holonResult.Result.Web4GeoNFTIds.Add(OASISGeoNFTId.ToString());

                        OASISResult<Web4GeoNFTCollection> saveResult = await Data.SaveHolonAsync<Web4GeoNFTCollection>(holonResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            result.Result = saveResult.Result;
                            result.Message = "OASIS GeoNFT Added To Collection Successfully.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured adding OASIS GeoNFT to collection. Reason: {saveResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured adding OASIS GeoNFT to collection. Reason: GeoNFT Already Added!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS GeoNFT Collection. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> AddWeb4GeoNFTToCollectionAsync(Guid collectionId, IWeb4GeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            return await AddWeb4GeoNFTToCollectionAsync(collectionId, OASISGeoNFT.Id, providerType);
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> RemoveWeb4GeoNFTFromCollectionAsync(Guid collectionId, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoNFTCollection> result = new();
            string errorMessage = "Error occured in RemoveGeoNFTFromCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4GeoNFTCollection> holonResult = await Data.LoadHolonAsync<Web4GeoNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (holonResult.Result.Web4GeoNFTIds.Contains(OASISGeoNFTId.ToString()))
                    {
                        holonResult.Result.Web4GeoNFTIds.Remove(OASISGeoNFTId.ToString());

                        OASISResult<Web4GeoNFTCollection> saveResult = await Data.SaveHolonAsync<Web4GeoNFTCollection>(holonResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            result.Result = saveResult.Result;
                            result.Message = "OASIS GeoNFT Removed From Collection Successfully.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured removing OASIS GeoNFT from collection. Reason: {saveResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured removing OASIS GeoNFT from collection. Reason: GeoNFT Not Found!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS GeoNFT Collection. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> RemoveWeb4GeoNFTFromCollectionAsync(Guid collectionId, IWeb4GeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            return await RemoveWeb4GeoNFTFromCollectionAsync(collectionId, OASISGeoNFT.Id, providerType);
        }

        public async Task<OASISResult<bool>> DeleteWeb4NFTCollectionAsync(Guid avatarId, Guid id, bool softDelete = true, bool deleteChildWeb4NFTs = false, bool deleteChildWeb3NFTs = false, bool burnChildWebNFTs = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteWeb4NFTCollectionAsync in NFTManager. Reason:";

            try
            {
                if (deleteChildWeb4NFTs)
                {
                    OASISResult<IWeb4NFTCollection> loadCollectionResult = await LoadWeb4NFTCollectionAsync(id, loadChildNFTs: false, providerType: providerType);

                    if (loadCollectionResult != null && loadCollectionResult.Result != null && !loadCollectionResult.IsError)
                    {
                        foreach (string web4NFTId in loadCollectionResult.Result.Web4NFTIds)
                        {
                            OASISResult<bool> deleteWeb4NFTResult = await DeleteWeb4NFTAsync(avatarId, new Guid(web4NFTId), softDelete, deleteChildWeb3NFTs, burnChildWebNFTs);

                            if (!(deleteWeb4NFTResult != null && !deleteWeb4NFTResult.IsError && deleteWeb4NFTResult.Result != null))
                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting Web4 NFT with id {web4NFTId}. Reason: {deleteWeb4NFTResult?.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the collection. Reason: {loadCollectionResult?.Message}");
                }

                OASISResult<IHolon> deleteCollectionResult = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (deleteCollectionResult != null && !deleteCollectionResult.IsError && deleteCollectionResult.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;

                    if (result.IsWarning)
                        result.Message = $"Web4 OASIS NFT Collection Successfull Deleted but one or more errors occured deleting it's child Web4 NFT's: \n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                    else
                        result.Message = $"Web4 OASIS NFT Collection Successfull Deleted";
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting collection. Reason: {deleteCollectionResult?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteWeb4GeoNFTCollectionAsync(Guid avatarId, Guid id, bool softDelete = true, bool deleteChildWeb4GeoNFTs = false, bool deleteChildWeb3NFTs = false, bool burnChildWebNFTs = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteWeb4GeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                if (deleteChildWeb4GeoNFTs)
                {
                    OASISResult<IWeb4GeoNFTCollection> loadCollectionResult = await LoadWeb4GeoNFTCollectionAsync(id, loadChildGeoNFTs: false, providerType: providerType);

                    if (loadCollectionResult != null && loadCollectionResult.Result != null && !loadCollectionResult.IsError)
                    {
                        foreach (string web4NFTId in loadCollectionResult.Result.Web4GeoNFTIds)
                        {
                            OASISResult<bool> deleteWeb4NFTResult = await DeleteWeb4GeoNFTAsync(avatarId, new Guid(web4NFTId), softDelete, deleteChildWeb3NFTs, burnChildWebNFTs);

                            if (!(deleteWeb4NFTResult != null && !deleteWeb4NFTResult.IsError && deleteWeb4NFTResult.Result != null))
                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting Web4 Geo-NFT with id {web4NFTId}. Reason: {deleteWeb4NFTResult?.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the collection. Reason: {loadCollectionResult?.Message}");
                }

                OASISResult<IHolon> deleteCollectionResult = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (deleteCollectionResult != null && !deleteCollectionResult.IsError && deleteCollectionResult.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;

                    if (result.IsWarning)
                        result.Message = $"Web4 OASIS Geo-NFT Collection Successfull Deleted but one or more errors occured deleting it's child Web4 Geo-NFT's: \n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                    else
                        result.Message = $"Web4 OASIS Geo-NFT Collection Successfull Deleted";
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting geo-nft collection. Reason: {deleteCollectionResult?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IList<IWeb4NFT>>> LoadChildWeb4NFTsForNFTCollectionAsync(List<string> Web4NFTIds, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IList<IWeb4NFT>> result = new OASISResult<IList<IWeb4NFT>>();

            if (Web4NFTIds != null && Web4NFTIds.Count > 0)
            {
                result.Result = new List<IWeb4NFT>();

                foreach (string nftId in Web4NFTIds)
                {
                    OASISResult<IWeb4NFT> nftRes = await LoadWeb4NftAsync(Guid.Parse(nftId), providerType: providerType);

                    if (nftRes != null && !nftRes.IsError && nftRes.Result != null)
                        result.Result.Add(nftRes.Result);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadChildWeb4NFTsForNFTCollection loading child nft for id {nftId}. Reason: {nftRes.Message}");
                }
            }

            if (result.ErrorCount > 0)
                result.Message = $"Error(s) occured in LoadChildWeb4NFTsForNFTCollection loading child nfts. Reason(s): {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

            return result;
        }

        public async Task<OASISResult<IList<IWeb4GeoSpatialNFT>>> LoadChildWeb4GeoNFTsForNFTCollectionAsync(List<string> Web4GeoNFTIds, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IList<IWeb4GeoSpatialNFT>> result = new OASISResult<IList<IWeb4GeoSpatialNFT>>();

            if (Web4GeoNFTIds != null && Web4GeoNFTIds.Count > 0)
            {
                result.Result = new List<IWeb4GeoSpatialNFT>();

                foreach (string nftId in Web4GeoNFTIds)
                {
                    OASISResult<IWeb4GeoSpatialNFT> nftRes = await LoadWeb4GeoNftAsync(Guid.Parse(nftId), providerType: providerType);

                    if (nftRes != null && !nftRes.IsError && nftRes.Result != null)
                        result.Result.Add(nftRes.Result);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadChildWeb4GeoNFTsForNFTCollection loading child nft for id {nftId}. Reason: {nftRes.Message}");
                }
            }

            if (result.ErrorCount > 0)
                result.Message = $"Error(s) occured in LoadChildWeb4GeoNFTsForNFTCollection loading child nfts. Reason(s): {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

            return result;
        }

        public async Task<OASISResult<IWeb4NFTCollection>> LoadWeb4NFTCollectionAsync(Guid id, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new();
            string errorMessage = "Error occured in LoadNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4NFTCollection> holonRes = await Data.LoadHolonAsync<Web4NFTCollection>(id, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs && holonRes.Result.Web4NFTIds != null && holonRes.Result.Web4NFTIds.Count > 0)
                    {
                        OASISResult<IList<IWeb4NFT>> childrenResult = await LoadChildWeb4NFTsForNFTCollectionAsync(holonRes.Result.Web4NFTIds, providerType);

                        if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                            holonRes.Result.Web4NFTs = childrenResult.Result.ToList();
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                    }

                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collection. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> LoadWeb4GeoNFTCollectionAsync(Guid id, bool loadChildGeoNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoNFTCollection> result = new();
            string errorMessage = "Error occured in LoadGeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4GeoNFTCollection> holonRes = await Data.LoadHolonAsync<Web4GeoNFTCollection>(id, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildGeoNFTs && holonRes.Result.Web4GeoNFTIds != null && holonRes.Result.Web4GeoNFTIds.Count > 0)
                    {
                        OASISResult<IList<IWeb4GeoSpatialNFT>> childrenResult = await LoadChildWeb4GeoNFTsForNFTCollectionAsync(holonRes.Result.Web4GeoNFTIds, providerType);

                        if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                            holonRes.Result.Web4GeoNFTs = childrenResult.Result.ToList();
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                    }

                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collection. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFTCollection>>> LoadAllWeb4NFTCollectionsAsync(bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFTCollection>> result = new();
            string errorMessage = "Error occured in LoadAllWeb4NFTCollectionsAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4NFTCollection>> holonRes = await Data.LoadAllHolonsAsync<Web4NFTCollection>(HolonType.Web4NFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4NFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4NFT>> childrenResult = await LoadChildWeb4NFTsForNFTCollectionAsync(collection.Web4NFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4NFTs = childrenResult.Result.ToList();
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                        }
                    }
                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFTCollection>>> LoadWeb4NFTCollectionsForAvatarAsync(Guid avatarId, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFTCollection>> result = new();
            string errorMessage = "Error occured in LoadWeb4NFTCollectionsForAvatarAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4NFTCollection>> holonRes = await Data.LoadHolonsForParentAsync<Web4NFTCollection>(avatarId, HolonType.Web4NFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4NFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4NFT>> childrenResult = await LoadChildWeb4NFTsForNFTCollectionAsync(collection.Web4NFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4NFTs = childrenResult.Result.ToList();
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                        }
                    }
                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoNFTCollection>>> LoadAllWeb4GeoNFTCollectionsAsync(bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTCollectionsAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4GeoNFTCollection>> holonRes = await Data.LoadAllHolonsAsync<Web4GeoNFTCollection>(HolonType.Web4GeoNFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4GeoNFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4GeoSpatialNFT>> childrenResult = await LoadChildWeb4GeoNFTsForNFTCollectionAsync(collection.Web4GeoNFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4GeoNFTs = childrenResult.Result.ToList();
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                        }
                    }
                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoNFTCollection>>> LoadWeb4GeoNFTCollectionsForAvatarAsync(Guid avatarId, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadWeb4GeoNFTCollectionsForAvatarAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4GeoNFTCollection>> holonRes = await Data.LoadHolonsForParentAsync<Web4GeoNFTCollection>(avatarId, HolonType.Web4GeoNFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4GeoNFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4GeoSpatialNFT>> childrenResult = await LoadChildWeb4GeoNFTsForNFTCollectionAsync(collection.Web4GeoNFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4GeoNFTs = childrenResult.Result.ToList();
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                        }
                    }
                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }


        private IMintWeb4NFTRequest CloneWeb4NFTRequest(IMintWeb4NFTRequest request)
        {
            return new MintWeb4NFTRequest()
            {
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds,
                Description = request.Description,
                Discount = request.Discount,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                IsForSale = request.IsForSale,
                JSONMetaData = request.JSONMetaData,
                MintedByAvatarId = request.MintedByAvatarId,
                Title = request.Title,
                MemoText = request.MemoText,
                Price = request.Price,
                RoyaltyPercentage = request.RoyaltyPercentage,
                NumberToMint = request.NumberToMint,
                SaleStartDate = request.SaleStartDate,
                SaleEndDate = request.SaleEndDate,
                OnChainProvider = request.OnChainProvider,
                OffChainProvider = request.OffChainProvider,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = request.NFTOffChainMetaType,
                NFTStandardType = request.NFTStandardType,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                JSONMetaDataURL = request.JSONMetaDataURL,
                Tags = request.Tags != null ? new List<string>(request.Tags) : null,
                MetaData = request.MetaData != null ? new Dictionary<string, object>(request.MetaData) : null,
                Symbol = request.Symbol,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                WaitTillNFTSent = request.WaitTillNFTSent,
                Web3NFTs = request.Web3NFTs != null ? request.Web3NFTs : new List<IMintWeb3NFTRequest>()
            };
        }

        private async Task<OASISResult<IWeb4NFT>> MintWeb3NFTsAsync(OASISResult<IWeb4NFT> result, IMintWeb4NFTRequest request, IMintWeb3NFTRequest web3Request = null, IWeb4NFT existingWeb4NFT = null, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, bool isLastWeb3NFT = false)
        {
            IMintWeb4NFTRequest originalWeb4Request = CloneWeb4NFTRequest(request);

            //Web3 Request overrides web4 (optional).
            if (web3Request != null)
            {
                if (!string.IsNullOrEmpty(web3Request.Title))
                    request.Title = web3Request.Title;

                if (!string.IsNullOrEmpty(web3Request.Description))
                    request.Description = web3Request.Description;

                if (web3Request.Image != null)
                    request.Image = web3Request.Image;

                if (!string.IsNullOrEmpty(web3Request.ImageUrl))
                    request.ImageUrl = web3Request.ImageUrl;

                if (web3Request.Thumbnail != null)
                    request.Thumbnail = web3Request.Thumbnail;

                if (!string.IsNullOrEmpty(web3Request.ThumbnailUrl))
                    request.ThumbnailUrl = web3Request.ThumbnailUrl;

                if (web3Request.Discount.HasValue)
                    request.Discount = web3Request.Discount.Value;

                if (web3Request.Price.HasValue)
                    request.Price = web3Request.Price.Value;

                if (web3Request.RoyaltyPercentage.HasValue)
                    request.RoyaltyPercentage = web3Request.RoyaltyPercentage.Value;

                if (web3Request.IsForSale.HasValue)
                    request.IsForSale = web3Request.IsForSale.Value;

                if (web3Request.SaleStartDate.HasValue)
                    request.SaleStartDate = web3Request.SaleStartDate.Value;

                if (web3Request.SaleEndDate.HasValue)
                    request.SaleEndDate = web3Request.SaleEndDate.Value;

                if (!string.IsNullOrEmpty(web3Request.Symbol))
                    request.Symbol = web3Request.Symbol;

                if (!string.IsNullOrEmpty(web3Request.JSONMetaData))
                    request.JSONMetaData = web3Request.JSONMetaData;

                if (!string.IsNullOrEmpty(web3Request.JSONMetaDataURL))
                    request.JSONMetaDataURL = web3Request.JSONMetaDataURL;

                if (web3Request.NumberToMint.HasValue)
                    request.NumberToMint = web3Request.NumberToMint.Value;

                if (web3Request.NFTOffChainMetaType.HasValue)
                    request.NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(web3Request.NFTOffChainMetaType.Value);

                if (web3Request.NFTStandardType.HasValue)
                    request.NFTStandardType = new EnumValue<NFTStandardType>(web3Request.NFTStandardType.Value);

                if (web3Request.OnChainProvider.HasValue)
                    request.OnChainProvider = new EnumValue<ProviderType>(web3Request.OnChainProvider.Value);

                if (web3Request.OffChainProvider.HasValue)
                    request.OffChainProvider = new EnumValue<ProviderType>(web3Request.OffChainProvider.Value);

                if (web3Request.StoreNFTMetaDataOnChain.HasValue)
                    request.StoreNFTMetaDataOnChain = web3Request.StoreNFTMetaDataOnChain.Value;

                if (!string.IsNullOrEmpty(web3Request.SendToAddressAfterMinting))
                    request.SendToAddressAfterMinting = web3Request.SendToAddressAfterMinting;

                if (web3Request.SendToAvatarAfterMintingId != Guid.Empty)
                    request.SendToAvatarAfterMintingId = web3Request.SendToAvatarAfterMintingId;

                if (!string.IsNullOrEmpty(web3Request.SendToAvatarAfterMintingUsername))
                    request.SendToAvatarAfterMintingUsername = web3Request.SendToAvatarAfterMintingUsername;

                if (!string.IsNullOrEmpty(web3Request.SendToAvatarAfterMintingEmail))
                    request.SendToAvatarAfterMintingEmail = web3Request.SendToAvatarAfterMintingEmail;

                if (web3Request.AttemptToMintEveryXSeconds.HasValue)
                    request.AttemptToMintEveryXSeconds = web3Request.AttemptToMintEveryXSeconds.Value;

                if (web3Request.WaitForNFTToMintInSeconds.HasValue)
                    request.AttemptToSendEveryXSeconds = web3Request.AttemptToSendEveryXSeconds.Value;

                if (web3Request.WaitTillNFTMinted.HasValue)
                    request.WaitForNFTToMintInSeconds = web3Request.WaitForNFTToMintInSeconds.Value;

                if (web3Request.WaitTillNFTSent.HasValue)
                    request.WaitForNFTToSendInSeconds = web3Request.WaitForNFTToSendInSeconds.Value;

                if (web3Request.NFTTagsMergeStrategy == NFTTagsMergeStrategy.Replace)
                    request.Tags.Clear();

                if (web3Request.Tags != null)
                {
                    if (request.Tags == null)
                        request.Tags = new List<string>();

                    foreach (string tag in web3Request.Tags)
                    {
                        if (request.Tags.Contains(tag))
                            continue;

                        request.Tags.Add(tag);
                    }
                }

                //Add web3 metadata to web4 (if any keys already exist then web3 overrides web4).
                if (web3Request.NFTMetaDataMergeStrategy == NFTMetaDataMergeStrategy.Replace)
                    request.MetaData.Clear();

                if (web3Request.MetaData != null)
                {
                    if (request.MetaData == null)
                        request.MetaData = new Dictionary<string, object>();

                    foreach (string key in web3Request.MetaData.Keys)
                    {
                        if (request.MetaData.ContainsKey(key) && web3Request.NFTMetaDataMergeStrategy == NFTMetaDataMergeStrategy.Merge)
                            continue;

                        request.MetaData[key] = web3Request.MetaData[key];
                    }
                }
            }

            OASISResult<bool> validateResult = await ValidateNFTRequest(request);

            if (validateResult != null && validateResult.Result && !validateResult.IsError)
            {
                if (request.OffChainProvider == null)
                    request.OffChainProvider = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);

                if (web3Request == null)
                    web3Request = new MintWeb3NFTRequest();

                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.OnChainProvider.Value);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                {
                    string geoNFTMemoText = "";

                    if (isGeoNFT)
                        geoNFTMemoText = "Geo";

                    request.MemoText = $"{request.OnChainProvider.Name} {geoNFTMemoText}NFT minted on The OASIS with title '{request.Title}' by avatar with id {request.MintedByAvatarId} for the price of {request.Price}. {request.MemoText}";

                    EnumValue<ProviderType> NFTMetaDataProviderType;
                    //request.OffChainProvider = new EnumValue<ProviderType>(ProviderType.None); //TODO: Not sure why it was defaulting to None?! lol

                    if (request.StoreNFTMetaDataOnChain)
                        NFTMetaDataProviderType = request.OnChainProvider;
                    else
                        NFTMetaDataProviderType = request.OffChainProvider;

                    if (string.IsNullOrEmpty(request.Symbol))
                    {
                        if (isGeoNFT)
                            request.Symbol = "GEONFT";
                        else
                            request.Symbol = "OASISNFT";
                    }

                    //Sync web3Request with web4.
                    web3Request.AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds;
                    web3Request.AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds;
                    web3Request.Description = request.Description;
                    web3Request.Discount = request.Discount;
                    web3Request.JSONMetaDataURL = request.JSONMetaDataURL;
                    web3Request.JSONMetaData = request.JSONMetaData;
                    web3Request.MetaData = request.MetaData;
                    web3Request.SaleStartDate = request.SaleStartDate;
                    web3Request.SaleEndDate = request.SaleEndDate;
                    web3Request.Image = request.Image;
                    web3Request.ImageUrl = request.ImageUrl;
                    web3Request.IsForSale = request.IsForSale;
                    web3Request.MemoText = request.MemoText;
                    web3Request.MintedByAvatarId = request.MintedByAvatarId;
                    web3Request.NFTOffChainMetaType = request.NFTOffChainMetaType.Value;
                    web3Request.NFTStandardType = request.NFTStandardType.Value;
                    web3Request.OffChainProvider = request.OffChainProvider.Value;
                    web3Request.OnChainProvider = request.OnChainProvider.Value;
                    web3Request.Price = request.Price;
                    web3Request.RoyaltyPercentage = request.RoyaltyPercentage;
                    web3Request.SendToAddressAfterMinting = request.SendToAddressAfterMinting;
                    web3Request.SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail;
                    web3Request.SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId;
                    web3Request.SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername;
                    web3Request.StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain;
                    web3Request.Symbol = request.Symbol;
                    web3Request.Tags = request.Tags;
                    web3Request.Thumbnail = request.Thumbnail;
                    web3Request.ThumbnailUrl = request.ThumbnailUrl;
                    web3Request.Title = request.Title;
                    web3Request.WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds;
                    web3Request.WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds;
                    web3Request.WaitTillNFTMinted = request.WaitTillNFTMinted;
                    web3Request.WaitTillNFTSent = request.WaitTillNFTSent;

                    result = await MintNFTInternalAsync(result, originalWeb4Request, web3Request, request, NFTMetaDataProviderType, nftProviderResult, existingWeb4NFT, isGeoNFT, responseFormatType, isLastWeb3NFT);
                }
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, $"Error occured minting web3 NFT in MintWeb3NFTsAsync. Error occured calling GetNFTProvider. Reason: {nftProviderResult.Message}");
                    //result.Result = null;
                    //result.Message = nftProviderResult.Message;
                    //result.IsError = true;
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured validating the NFT Request. Reason: {validateResult.Message}");

            return result;
        }

        private async Task<OASISResult<bool>> ValidateNFTRequest(IMintWeb4NFTRequest request)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = $"Error occured in ValidateNFTRequest. Reason: ";
            IAvatar currentAvatar = null;
            OASISResult<bool> nftStandardValid = IsNFTStandardTypeValid(request, errorMessage);

            if (nftStandardValid != null && nftStandardValid.IsError)
            {
                result.IsError = true;
                result.Message = nftStandardValid.Message;
                return result;
            }

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingEmail))
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(request.SendToAvatarAfterMintingEmail);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    request.SendToAvatarAfterMintingId = avatarResult.Result.Id;
                    currentAvatar = avatarResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to load the avatar details for the SendToAvatarAfterMintingEmail {request.SendToAvatarAfterMintingEmail}. The email is likely not valid. Reason: {avatarResult.Message}");
                    return result;
                }
            }

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingUsername))
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(request.SendToAvatarAfterMintingUsername);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    request.SendToAvatarAfterMintingId = avatarResult.Result.Id;
                    currentAvatar = avatarResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to load the avatar details for the SendToAvatarAfterMintingUsername {request.SendToAvatarAfterMintingEmail}. The username is likely not valid. Reason: {avatarResult.Message}");
                    return result;
                }
            }

            if (string.IsNullOrEmpty(request.SendToAddressAfterMinting) && request.SendToAvatarAfterMintingId == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} You need to specify at least one of the following: SendToAddressAfterMinting, SendToAvatarAfterMintingId, SendToAvatarAfterMintingUsername or SendToAvatarAfterMintingEmail.");
                return result;
            }

            //If the wallet Address hasn't been set then set it now by looking up the relevant wallet address for this avatar and provider type.
            if (string.IsNullOrEmpty(request.SendToAddressAfterMinting) && request.SendToAvatarAfterMintingId != Guid.Empty)
            {
                if (currentAvatar == null)
                {
                    OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(request.MintedByAvatarId);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        currentAvatar = avatarResult.Result;
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to load the avatar details for the SendToAvatarAfterMinting {request.MintedByAvatarId}. Reason: {avatarResult.Message}");
                        return result;
                    }
                }

                if (currentAvatar != null)
                {
                    foreach (ProviderType providerType in currentAvatar.ProviderWallets.Keys)
                    {
                        if (providerType == request.OnChainProvider.Value)
                        {
                            if (currentAvatar.ProviderWallets[request.OnChainProvider.Value].Count > 0)
                            {
                                IProviderWallet providerWallet = currentAvatar.ProviderWallets[request.OnChainProvider.Value].FirstOrDefault(x => x.IsDefaultWallet);

                                if (providerWallet == null)
                                    providerWallet = currentAvatar.ProviderWallets[request.OnChainProvider.Value][0];

                                request.SendToAddressAfterMinting = providerWallet.WalletAddress;
                            }
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(request.SendToAddressAfterMinting))
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for avatar {request.MintedByAvatarId} and provider {request.OnChainProvider.Value}. Please make sure you link a valid wallet to the avatar using the Wallet API or Key API.");
                        return result;
                    }
                }
            }

            if (string.IsNullOrEmpty(request.SendToAddressAfterMinting))
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} SendToAddressAfterMinting is null! Please make sure a valid SendToAddressAfterMinting is set or a valid SendToAvatarAfterMinting.");
                return result;
            }

            result.Result = true;
            return result;
        }



        ///// <summary>
        ///// Mint multiple NFTs in a single batch operation for improved efficiency
        ///// </summary>
        //public async Task<OASISResult<List<IWeb4OASISNFT>>> MintNFTBatchAsync(List<IMintWeb4NFTRequest> requests, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        //{
        //    var result = new OASISResult<List<IWeb4OASISNFT>>();
        //    string errorMessage = "Error occured in MintNFTBatchAsync in NFTManager. Reason:";

        //    try
        //    {
        //        if (requests == null || !requests.Any())
        //        {
        //            OASISErrorHandling.HandleError(ref result, "No NFT mint requests provided");
        //            return result;
        //        }

        //        CLIEngine.ShowWorkingMessage($"Starting batch minting of {requests.Count} NFTs...");

        //        var batchResults = new List<IWeb4OASISNFT>();
        //        var successfulMints = 0;
        //        var failedMints = 0;

        //        // Process NFTs in parallel batches for optimal performance
        //        var batchSize = Math.Min(10, requests.Count); // Process up to 10 NFTs concurrently
        //        var batches = requests.Chunk(batchSize);

        //        foreach (var batch in batches)
        //        {
        //            var batchTasks = batch.Select(async request =>
        //            {
        //                try
        //                {
        //                    OASISResult<IWeb4OASISNFT> mintResult = await MintNftAsync(request, false, responseFormatType);
        //                    if (mintResult.IsError)
        //                    {
        //                        Interlocked.Increment(ref failedMints);
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling MintNFTAsync. Reason: {mintResult.Message}", true);

        //                        return 

        //                        //return new Web4OASISNFT
        //                        //{
        //                        //    TransactionResult = mintResult.Message,
        //                        //    Web4OASISNFT = null,
        //                        //    SendNFTTransactionResult = string.Empty
        //                        //};
        //                    }
        //                    else
        //                    {
        //                        Interlocked.Increment(ref successfulMints);
        //                        return mintResult.Result;
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    Interlocked.Increment(ref failedMints);
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling MintNFTAsync. Reason: {ex}", true);

        //                    //return new Web4NFTTransactionRespone
        //                    //{
        //                    //    TransactionResult = $"Error minting NFT: {ex.Message}",
        //                    //    Web4OASISNFT = null,
        //                    //    SendNFTTransactionResult = string.Empty
        //                    //};
        //                }
        //            });

        //            var currentBatchResults = await Task.WhenAll(batchTasks);
        //            batchResults.AddRange(currentBatchResults);

        //            // Brief pause between batches to prevent overwhelming the network
        //            await Task.Delay(100);
        //        }

        //        result.Result = batchResults;
        //        result.IsError = false;
        //        result.Message = $"Batch minting completed: {successfulMints} successful, {failedMints} failed";

        //        CLIEngine.ShowSuccessMessage($"Batch minting completed: {successfulMints} successful, {failedMints} failed");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error during batch minting: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        private async Task<OASISResult<IWeb4NFT>> MintNFTInternalAsync(OASISResult<IWeb4NFT> result, IMintWeb4NFTRequest originalWeb4Request, IMintWeb3NFTRequest web3Request, IMintWeb4NFTRequest mergedRequest, EnumValue<ProviderType> metaDataProviderType, OASISResult<IOASISNFTProvider> nftProviderResult, IWeb4NFT existingWeb4NFT = null, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, bool isLastWeb3NFT = false)
        {
            string errorMessage = "Error occured in NFTManager.MintNFTInternalAsync. Reason:";
            OASISResult<IHolon> jsonSaveResult = null;

            //Need to save the image to the off-chain provider first to get the URL to pass into the onchain provider.
            if (mergedRequest.Image != null)
            {
                switch (mergedRequest.NFTOffChainMetaType.Value)
                {
                    case NFTOffChainMetaType.Pinata:
                        {
                            Guid imageId = Guid.NewGuid();
                            OASISResult<string> pinataResult = await Pinata.UploadFileToPinataAsync(mergedRequest.Image, imageId.ToString());

                            if (pinataResult != null && pinataResult.Result != null && !pinataResult.IsError)
                                mergedRequest.ImageUrl = string.Concat("http://", Pinata.GetFileUrl(pinataResult.Result));
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to Pinata. Reason: {pinataResult.Message}");
                                return result;
                            }
                        }
                        break;

                    case NFTOffChainMetaType.IPFS:
                        {
                            Guid imageId = Guid.NewGuid();
                            //_ipfs.SaveStream(new MemoryStream(request.Image), imageId.ToString(), new Ipfs.CoreApi.AddFileOptions() { Progress = new Progress<>} );
                            OASISResult<IFileSystemNode> ipfsResult = await IPFS.SaveStreamAsync(new MemoryStream(mergedRequest.Image), imageId.ToString());

                            if (ipfsResult != null && ipfsResult.Result != null && !ipfsResult.IsError)
                                mergedRequest.ImageUrl = IPFS.GetFileUrl(ipfsResult.Result.Id.ToString());
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to IPFS. Reason: {ipfsResult.Message}");
                                return result;
                            }
                        }
                        break;

                    case NFTOffChainMetaType.OASIS:
                        {
                            OASISResult<IHolon> imageSaveResult = await Data.SaveHolonAsync(new Holon()
                            {
                                MetaData = new Dictionary<string, object>()
                                {
                                    { "data",  mergedRequest.Image }
                                }
                            }, mergedRequest.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                            if (imageSaveResult != null && imageSaveResult.Result != null && !imageSaveResult.IsError)
                                mergedRequest.ImageUrl = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/getdata/", imageSaveResult.Result.Id);

                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to the OASIS and offchain provider {mergedRequest.OffChainProvider.Name}. Reason: {imageSaveResult.Message}");
                        }
                        break;
                }
            }

            if (!string.IsNullOrEmpty(mergedRequest.ImageUrl) || mergedRequest.NFTOffChainMetaType.Value == NFTOffChainMetaType.ExternalJSONURL)
            {
                string json = mergedRequest.JSONMetaData;

                if (string.IsNullOrEmpty(json))
                    json = CreateMetaDataJson(mergedRequest, mergedRequest.NFTStandardType.Value);

                switch (mergedRequest.NFTOffChainMetaType.Value)
                {
                    case NFTOffChainMetaType.Pinata:
                        {
                            Guid imageId = Guid.NewGuid();
                            OASISResult<string> pinataResult = await Pinata.UploadJsonToPinataAsync(json);

                            if (pinataResult != null && pinataResult.Result != null && !pinataResult.IsError)
                                mergedRequest.JSONMetaDataURL = string.Concat("http://", Pinata.GetFileUrl(pinataResult.Result));
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to Pinata. Reason: {pinataResult.Message}");
                                return result;
                            }
                        }
                        break;

                    case NFTOffChainMetaType.IPFS:
                        {
                            Guid imageId = Guid.NewGuid();
                            OASISResult<IFileSystemNode> ipfsResult = await IPFS.SaveTextAsync(json);

                            if (ipfsResult != null && ipfsResult.Result != null && !ipfsResult.IsError)
                                mergedRequest.JSONMetaDataURL = IPFS.GetFileUrl(ipfsResult.Result.Id.ToString());
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to IPFS. Reason: {ipfsResult.Message}");
                                return result;
                            }
                        }
                        break;

                    case NFTOffChainMetaType.OASIS:
                        {
                            jsonSaveResult = await SaveJSONMetaDataToOASISAsync(mergedRequest, metaDataProviderType, json);

                            if (jsonSaveResult != null && jsonSaveResult.Result != null && !jsonSaveResult.IsError)
                                mergedRequest.JSONMetaDataURL = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/load-file/", jsonSaveResult.Result.Id);
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to the OASIS and offchain provider {mergedRequest.OffChainProvider.Name}. Reason: {jsonSaveResult.Message}");
                                return result;
                            }
                        }
                        break;

                    case NFTOffChainMetaType.ExternalJSONURL:
                        {
                            if (string.IsNullOrEmpty(mergedRequest.JSONMetaDataURL))
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} When setting NFTOffChainMetaType to ExternalJsonURL, the JSONUrl cannot be empty!");
                                return result;
                            }
                            break;
                        }
                }

                bool attemptingToMint = true;
                DateTime startTime = DateTime.Now;
                CLIEngine.SupressConsoleLogging = true;

                //Set NumberToMint to 1 in case the provider attempts to mint multiple nfts (we currently control the multi-minting here in the NFT Manager).
                //TODO: Is it better to let the providers control the multi-minting or the NFTManager? Its safer for NFTManager I think in case the providers do not implement properly etc...
                int numberToMint = mergedRequest.NumberToMint;
                mergedRequest.NumberToMint = 1;
                Web3NFT currentWeb3NFT = new Web3NFT();
                string mintErrorMessage = string.Empty;

                web3Request.JSONMetaDataURL = mergedRequest.JSONMetaDataURL;
                web3Request.ImageUrl = mergedRequest.ImageUrl;

                for (int i = 0; i < numberToMint; i++)
                {
                    do
                    {
                        try
                        {
                            OASISResult<IWeb3NFTTransactionResponse> mintResult = await nftProviderResult.Result.MintNFTAsync(web3Request);

                            if (mintResult != null && mintResult.Result != null && !mintResult.IsError)
                            {
                                currentWeb3NFT = (Web3NFT)mintResult.Result.Web3NFT;

                                if (!string.IsNullOrEmpty(currentWeb3NFT.MintTransactionHash))
                                    currentWeb3NFT.MintTransactionHash = mintResult.Result.TransactionResult;

                                if (jsonSaveResult != null)
                                {
                                    currentWeb3NFT.JSONMetaDataURLHolonId = jsonSaveResult.Result.Id;
                                    currentWeb3NFT.JSONMetaData = jsonSaveResult.Result.MetaData["data"].ToString();
                                }
                                break;
                            }
                            else
                                mintErrorMessage = $"{errorMessage} Error occured minting the OASISNFT: Reason: {mintResult.Message}";
                        }
                        catch (Exception e)
                        {
                            mintErrorMessage = $"{errorMessage} Unknown error occured minting the OASISNFT: Reason: {e.Message}";
                        }

                        if (!string.IsNullOrEmpty(mintErrorMessage))
                        {
                            OASISErrorHandling.HandleError(ref result, mintErrorMessage);

                            if (!mergedRequest.WaitTillNFTMinted)
                            {
                                currentWeb3NFT.MintTransactionHash = $"{mintErrorMessage}. WaitTillNFTMinted is false so aborting! ";
                                break;
                            }
                        }

                        //TODO: May cause issues in the non-async version because will block the calling thread! Need to look into this and find better way if needed...
                        Thread.Sleep(mergedRequest.AttemptToMintEveryXSeconds * 1000);

                        if (startTime.AddSeconds(mergedRequest.WaitForNFTToMintInSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            mintErrorMessage = $"{mintErrorMessage}Timeout expired, WaitForNFTToMintInSeconds ({mergedRequest.WaitForNFTToMintInSeconds}) exceeded, try increasing and trying again!";
                            currentWeb3NFT.MintTransactionHash = mintErrorMessage;
                            OASISErrorHandling.HandleError(ref result, mintErrorMessage);
                            break;
                        }

                        mintErrorMessage = "";

                    } while (attemptingToMint);

                    if (!string.IsNullOrEmpty(currentWeb3NFT.MintTransactionHash) && !string.IsNullOrEmpty(mergedRequest.SendToAddressAfterMinting))
                    {
                        bool attemptingToSend = true;
                        startTime = DateTime.Now;
                        CLIEngine.SupressConsoleLogging = true;

                        do
                        {
                            try
                            {
                                OASISResult<IWeb3NFTTransactionResponse> sendResult = await nftProviderResult.Result.SendNFTAsync(new SendWeb3NFTRequest()
                                {
                                    FromWalletAddress = currentWeb3NFT.OASISMintWalletAddress,
                                    ToWalletAddress = web3Request.SendToAddressAfterMinting,
                                    TokenAddress = currentWeb3NFT.NFTTokenAddress,
                                    //FromProvider = mergedRequest.OnChainProvider,
                                    //ToProvider = mergedRequest.OnChainProvider,
                                    Amount = 1,
                                    MemoText = $"Sending NFT from OASIS Wallet {currentWeb3NFT.OASISMintWalletAddress} to {mergedRequest.SendToAddressAfterMinting} on chain {mergedRequest.OnChainProvider.Name}.",
                                });

                                if (sendResult != null && sendResult.Result != null && !sendResult.IsError)
                                {
                                    currentWeb3NFT.SendNFTTransactionHash = sendResult.Result.TransactionResult;
                                    break;
                                }
                                else
                                    mintErrorMessage = $"Error occured attempting to send NFT. Reason: {sendResult.Message}";
                            }
                            catch (Exception e)
                            {
                                mintErrorMessage = $"{errorMessage} Unknown error occured sending the OASISNFT: Reason: {e.Message}";
                            }

                            if (!string.IsNullOrEmpty(mintErrorMessage))
                            {
                                OASISErrorHandling.HandleError(ref result, mintErrorMessage);

                                if (!mergedRequest.WaitTillNFTSent)
                                {
                                    currentWeb3NFT.SendNFTTransactionHash = $"{mintErrorMessage}. WaitTillNFTSent is false so aborting! ";
                                    break;
                                }

                                mintErrorMessage = "";
                            }

                            Thread.Sleep(mergedRequest.AttemptToSendEveryXSeconds * 1000);

                            if (startTime.AddSeconds(mergedRequest.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
                            {
                                mintErrorMessage = $"{mintErrorMessage}Timeout expired, WaitForNFTToSendInSeconds ({mergedRequest.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!";
                                currentWeb3NFT.SendNFTTransactionHash = mintErrorMessage;
                                OASISErrorHandling.HandleError(ref result, mintErrorMessage);
                                break;
                            }

                        } while (attemptingToSend);

                        CLIEngine.SupressConsoleLogging = false;
                    }
                }

                mergedRequest.NumberToMint = numberToMint;
                CLIEngine.SupressConsoleLogging = false;

                if (!string.IsNullOrEmpty(currentWeb3NFT.MintTransactionHash))
                {
                    if (!string.IsNullOrEmpty(currentWeb3NFT.NFTTokenAddress))
                        result.SavedCount++;

                    if (result.Result == null)
                    {
                        if (existingWeb4NFT == null)
                            result.Result = CreateWeb4NFT(originalWeb4Request);
                        else
                        {
                            result.Result = existingWeb4NFT;

                            foreach (IWeb3NFT web3NFT in existingWeb4NFT.Web3NFTs)
                            {
                                if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                                    web3NFT.MetaData.Remove("new");
                            }
                        }
                    }

                    result.Result.Web3NFTs.Add((Web3NFT)UpdateWeb3NFT(currentWeb3NFT, web3Request));

                    IHolon web3NFTHolon = CreateWeb3NFTMetaDataHolon(currentWeb3NFT, result.Result.Id, web3Request);
                    OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(web3NFTHolon, web3Request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                    if (!(saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the WEB3 NFT metadata holon to the {metaDataProviderType.Name} {Enum.GetName(typeof(ProviderType), metaDataProviderType.Value)}. Reason: {saveHolonResult.Message}");

                    //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                    if (metaDataProviderType.Value == ProviderType.None)
                        metaDataProviderType.Value = ProviderType.MongoDBOASIS;

                    //Check if this is the last Web3 NFT to mint. If so then we can save the Holon otherwise we wait till the final one to save.
                    if (isLastWeb3NFT)
                    {
                        IHolon webNFTHolon = null;

                        if (existingWeb4NFT == null)
                            webNFTHolon = CreateWeb4NFTMetaDataHolon(result.Result, originalWeb4Request);
                        else
                        {
                            //Update the existing Web4 NFT Holon (with any new Web3NFTs that have been minted via the Remint function above).
                            OASISResult<IHolon> holonLoadResult = await Data.LoadHolonAsync(existingWeb4NFT.Id);

                            if (holonLoadResult != null && holonLoadResult.Result != null && !holonLoadResult.IsError)
                            {
                                webNFTHolon = holonLoadResult.Result;

                                if (isGeoNFT)
                                    webNFTHolon.MetaData["GEONFT.WEB4GEONFT"] = System.Text.Json.JsonSerializer.Serialize(result.Result);
                                else
                                    webNFTHolon.MetaData["NFT.WEB4NFT"] = System.Text.Json.JsonSerializer.Serialize(result.Result);
                            }
                        }

                        saveHolonResult = await Data.SaveHolonAsync(webNFTHolon, originalWeb4Request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.IsError = false;
                            result.Message = FormatSuccessMessage(mergedRequest, result, metaDataProviderType, responseFormatType);
                        }
                        else
                        {
                            result.Result = null;
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the WEB4 NFT metadata holon to the {metaDataProviderType.Name} {Enum.GetName(typeof(ProviderType), metaDataProviderType.Value)}. Reason: {saveHolonResult.Message}");
                        }
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The ImageUrl is null!");

            return result;
        }

        private string FormatSuccessMessage(IMintWeb4NFTRequest request, OASISResult<IWeb4NFT> response, EnumValue<ProviderType> metaDataProviderType, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully minted the OASIS NFT containing {response.SavedCount} Web NFT(s) & {response.ErrorCount} errored!";
            string summary = $"Successfully minted the OASIS NFT containing {response.SavedCount} Web NFT(s)";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                {
                    if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                    {
                        string sendNFTMessage = GenerateSendMessage(response.Result, request, web3NFT.SendNFTTransactionHash, "", 2);
                        message = string.Concat(message, $"Successfully minted the Web3 NFT on the {web3NFT.OnChainProvider.Name} provider with hash {web3NFT.MintTransactionHash} and title '{web3NFT.Title}' by AvatarId {request.MintedByAvatarId} using OASIS Minting Account {web3NFT.OASISMintWalletAddress} for price {web3NFT.Price}. NFT Address: {web3NFT.NFTTokenAddress}. The OASIS metadata is stored on the {web3NFT.OffChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, ", Image URL: ", web3NFT.ImageUrl, ", Mint Date: ", web3NFT.MintedOn, ". ", sendNFTMessage, lineBreak);
                        web3NFT.MetaData.Remove("new");
                    }
                }

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, summary, lineBreak, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
            {
                if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                    message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, request, lineBreak, colWidth));
            }

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
            {
                if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                    web3NFT.MetaData.Remove("new");
            }

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(IMintWeb4NFTRequest request, OASISResult<IWeb4GeoSpatialNFT> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = 40)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully minted & placed the OASIS Geo-NFT containing {response.SavedCount} Web3 NFT(s) & {response.ErrorCount} errored!";
            string summary = $"Successfully minted & placed the OASIS Geo-NFT containing {response.SavedCount} Web3 NFT(s)";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                {
                    if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                    {
                        string sendNFTMessage = GenerateSendMessage(response.Result, request, web3NFT.SendNFTTransactionHash, "", 2);
                        message = string.Concat(message, $"Successfully minted the Web3 NFT on the {web3NFT.OnChainProvider.Name} provider with hash {web3NFT.MintTransactionHash} and title '{web3NFT.Title}' by AvatarId {request.MintedByAvatarId} using OASIS Minting Account {web3NFT.OASISMintWalletAddress} for price {web3NFT.Price}. NFT Address: {web3NFT.NFTTokenAddress}. The OASIS metadata is stored on the {web3NFT.OffChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, ", Image URL: ", web3NFT.ImageUrl, ", Mint Date: ", web3NFT.MintedOn, ". The GeoNFT meta data is stored on the GeoNFTMetaDataProvider ", response.Result.GeoNFTMetaDataProvider.Name, " with id ", response.Result.Id, " and was placed by the avatar with id ", response.Result.PlacedByAvatarId, sendNFTMessage, lineBreak);
                        web3NFT.MetaData.Remove("new");
                    }
                }

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, summary, lineBreak, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
            {
                if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                {
                    message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, request, lineBreak, colWidth));
                    web3NFT.MetaData.Remove("new");
                }
            }

            message = string.Concat(message, GenerateWeb4GeoNFTSummary(response.Result, lineBreak, colWidth));

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IWeb4GeoSpatialNFT> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = 40)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully created & placed OASIS Geo-NFT containing {response.SavedCount} Web3 NFT(s) & {response.ErrorCount} errored!";
            string summary = $"Successfully created & placed OASIS Geo-NFT containing {response.SavedCount} Web3 NFT(s)";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                {
                    if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                    {
                        message = string.Concat(message, $"{summary} The meta data is stored on the GeoNFTMetaDataProvider {response.Result.GeoNFTMetaDataProvider.Name} with id {response.Result.Id} and was placed by the avatar with id {response.Result.PlacedByAvatarId}. The NFT was originally minted on the {web3NFT.OnChainProvider.Name} onchain provider with hash {web3NFT.MintTransactionHash} and title '{web3NFT.Title}' by the avatar with id {web3NFT.MintedByAvatarId} for the price of {web3NFT.Price} using OASIS Minting Account {web3NFT.OASISMintWalletAddress}. NFT Address: {web3NFT.NFTTokenAddress}. The OASIS metadata for the original NFT is stored on the {web3NFT.OffChainProvider.Name} offchain provider with the id {response.Result.OriginalWeb4OASISNFTId} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URL Holon Id: ", web3NFT.JSONMetaDataURLHolonId, ", Image URL: {web3NFT.ImageUrl}, Mint Date: {web3NFT.MintedOn}.");
                        web3NFT.MetaData.Remove("new");
                    }
                }

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, summary, lineBreak, lineBreak);
            message = string.Concat(message, $"ORIGINAL NFT INFO:{lineBreak}");

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
            {
                if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                {
                    message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, null, lineBreak, colWidth));
                    web3NFT.MetaData.Remove("new");
                }
            }

            message = string.Concat(message, lineBreak);
            message = string.Concat(message, GenerateWeb4GeoNFTSummary(response.Result, lineBreak, colWidth));

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(IImportWeb3NFTRequest request, OASISResult<IWeb4NFT> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully imported {response.SavedCount} Web3 NFT(s) & {response.ErrorCount} errored!";
            string summary = $"Successfully imported {response.SavedCount} Web3 NFT(s)";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                {
                    if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                    {
                        message = string.Concat(message, $"Web3 NFT OnChain Provider: {web3NFT.OnChainProvider.Name}, NFTTokenAddress {web3NFT.NFTTokenAddress}, title '{web3NFT.Title}', Imported By Avatar Id: {web3NFT.ImportedByAvatarId}. NFT minted using wallet address: {web3NFT.NFTMintedUsingWalletAddress}. Price: {web3NFT.Price}. The OASIS metadata is stored on the {web3NFT.OnChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, ", Image URL: ", web3NFT.ImageUrl, ", Imported Date: ", web3NFT.ImportedOn, lineBreak);
                        web3NFT.MetaData.Remove("new");
                    }
                } 
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, summary, lineBreak, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
            {
                if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                {
                    message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, lineBreak, colWidth));
                    web3NFT.MetaData.Remove("new");
                }
            }  

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IWeb4NFT> response, Guid importedByByAvatarId, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully imported the OASIS NFT containing {response.SavedCount} Web3 NFT(s) & {response.ErrorCount} errored!";
            string summary = $"Successfully imported the OASIS NFT containing {response.SavedCount} Web3 NFT(s)";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";
                lineBreak = "|";

                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                {
                    if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                    {
                        message = string.Concat(message, $"Web3 NFT OnChain Provider: {web3NFT.OnChainProvider.Name}, NFTTokenAddress {web3NFT.NFTTokenAddress}, title '{web3NFT.Title}', Imported By Avatar Id: {web3NFT.ImportedByAvatarId}. NFT minted using wallet address: {web3NFT.NFTMintedUsingWalletAddress}. Price: {web3NFT.Price}. The OASIS metadata is stored on the {web3NFT.OnChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, ", Image URL: ", web3NFT.ImageUrl, ", Imported Date: ", web3NFT.ImportedOn, lineBreak);
                        web3NFT.MetaData.Remove("new");
                    }
                }

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, summary, lineBreak, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
            {
                if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                {
                    message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, lineBreak, colWidth));
                    web3NFT.MetaData.Remove("new");
                }
            }

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IWeb4GeoSpatialNFT> response, Guid importedByByAvatarId, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully imported the GeoNFT containing {response.SavedCount} Web3 NFT(s) & {response.ErrorCount} errored!";
            string summary = $"Successfully imported the GeoNFT containing {response.SavedCount} Web3 NFT(s)";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                {
                    if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                    {
                        message = string.Concat(message, $"Web3 NFT OnChain Provider: {web3NFT.OnChainProvider.Name}, NFTTokenAddress {web3NFT.NFTTokenAddress}, title '{web3NFT.Title}', Imported By Avatar Id: {web3NFT.ImportedByAvatarId}. NFT minted using wallet address: {web3NFT.NFTMintedUsingWalletAddress}. Price: {web3NFT.Price}. The OASIS metadata is stored on the {web3NFT.OnChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, ", Image URL: ", web3NFT.ImageUrl, ", Imported Date: ", web3NFT.ImportedOn, lineBreak);
                        web3NFT.MetaData.Remove("new");
                    }
                } 

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";


            message = string.Concat(message, summary, lineBreak, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
            {
                if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("new"))
                {
                    message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, lineBreak, colWidth));
                    web3NFT.MetaData.Remove("new");
                }
            }

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(ISendWeb4NFTRequest request, OASISResult<IWeb3NFTTransactionResponse> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";

            if (responseFormatType == ResponseFormatType.SimpleText)
                return $"Successfully sent the NFT from wallet {request.FromWalletAddress} to wallet {request.ToWalletAddress}. Transaction Hash: {response.Result.TransactionResult}, From Provider: {request.FromProvider.Name}, To Provider: {request.ToProvider.Name}, Amount: {request.Amount}, Memo: {request.MemoText}.";

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            string message = "";
            message = string.Concat(message, $" NFT Successfully Sent!{lineBreak}");
            message = string.Concat(message, lineBreak);
            message = string.Concat(message, " From Wallet:".PadRight(colWidth), request.FromWalletAddress, lineBreak);
            message = string.Concat(message, " To Wallet:".PadRight(colWidth), request.ToWalletAddress, lineBreak);
            message = string.Concat(message, " From Provider:".PadRight(colWidth), request.FromProvider.Name, lineBreak);
            message = string.Concat(message, " To Provider:".PadRight(colWidth), request.ToProvider.Name, lineBreak);
            message = string.Concat(message, " Amount:".PadRight(colWidth), request.Amount, lineBreak);
            message = string.Concat(message, " Memo:".PadRight(colWidth), request.MemoText, lineBreak);
            message = string.Concat(message, " Transaction Hash:".PadRight(colWidth), response.Result.TransactionResult, lineBreak);

            return message;
        }

        private string GenerateWeb3NFTSummary(IWeb3NFT web3NFT, IMintWeb4NFTRequest request, string lineBreak, int colWidth)
        {
            string message = GenerateWeb3NFTSummary(web3NFT, lineBreak, colWidth);

            if (request != null)
                message = string.Concat(message, " Number To Mint:".PadRight(colWidth), request.NumberToMint, lineBreak);

            message = string.Concat(message, GenerateSendMessage(web3NFT, request, web3NFT.SendNFTTransactionHash, lineBreak, colWidth), lineBreak);
            return message;
        }

        private string GenerateWeb3NFTSummary(IWeb3NFT web3NFT, string lineBreak, int colWidth)
        {
            string message = "";
            message = string.Concat(message, " OASIS NFT Id:".PadRight(colWidth), web3NFT.Id, lineBreak);
            message = string.Concat(message, " Onchain Provider:".PadRight(colWidth), web3NFT.OnChainProvider.Name, lineBreak);
            message = string.Concat(message, " Offchain Provider:".PadRight(colWidth), web3NFT.OffChainProvider.Name, lineBreak);
            message = string.Concat(message, " Mint Transaction Hash:".PadRight(colWidth), web3NFT.MintTransactionHash, lineBreak);
            message = string.Concat(message, " Title:".PadRight(colWidth), web3NFT.Title, lineBreak);
            message = string.Concat(message, " Description:".PadRight(colWidth), web3NFT.Description, lineBreak);
            message = string.Concat(message, " Price:".PadRight(colWidth), web3NFT.Price, lineBreak);
            message = string.Concat(message, " Symbol:".PadRight(colWidth), web3NFT.Symbol, lineBreak);
            message = string.Concat(message, " NFT Standard Type:".PadRight(colWidth), web3NFT.NFTStandardType.Name, lineBreak);
            message = string.Concat(message, " Minted By Avatar Id:".PadRight(colWidth), web3NFT.MintedByAvatarId, lineBreak);
            message = string.Concat(message, " Minted Date:".PadRight(colWidth), web3NFT.MintedOn, lineBreak);
            message = string.Concat(message, " OASIS Minting Account:".PadRight(colWidth), web3NFT.OASISMintWalletAddress, lineBreak);
            message = string.Concat(message, " NFT Address:".PadRight(colWidth), web3NFT.NFTTokenAddress, lineBreak);
            message = string.Concat(message, " Store NFT MetaData OnChain:".PadRight(colWidth), web3NFT.StoreNFTMetaDataOnChain, lineBreak);
            message = string.Concat(message, " NFT OffChain MetaType:".PadRight(colWidth), web3NFT.NFTOffChainMetaType.Name, lineBreak);
            message = string.Concat(message, " JSON MetaData URL:".PadRight(colWidth), web3NFT.JSONMetaDataURL, lineBreak);
            //TODO: Add rest of properties.

            if (web3NFT.JSONMetaDataURLHolonId != Guid.Empty)
                message = string.Concat(message, " JSON MetaData URL Holon Id:".PadRight(colWidth), web3NFT.JSONMetaDataURLHolonId, lineBreak);

            message = string.Concat(message, " Image URL:".PadRight(colWidth), web3NFT.ImageUrl, lineBreak);
            message = string.Concat(message, " Thumbnail URL:".PadRight(colWidth), web3NFT.ThumbnailUrl, lineBreak);

            return message;
        }

        private string GenerateWeb4GeoNFTSummary(IWeb4GeoSpatialNFT OASISNFT, string lineBreak, int colWidth)
        {
            string message = "";
            message = string.Concat(message, " Lat/Long:".PadRight(colWidth), OASISNFT.Lat, "/", OASISNFT.Long, lineBreak);
            message = string.Concat(message, " Perm Spawn:".PadRight(colWidth), OASISNFT.PermSpawn, lineBreak);

            if (!OASISNFT.PermSpawn)
            {
                message = string.Concat(message, " Allow Other Players To Also Collect:".PadRight(colWidth), OASISNFT.AllowOtherPlayersToAlsoCollect, lineBreak);

                if (OASISNFT.AllowOtherPlayersToAlsoCollect)
                {
                    message = string.Concat(message, " Global Spawn Quantity:".PadRight(colWidth), OASISNFT.GlobalSpawnQuantity, lineBreak);
                    message = string.Concat(message, " Player Spawn Quantity:".PadRight(colWidth), OASISNFT.PlayerSpawnQuantity, lineBreak);
                    message = string.Concat(message, " Respawn Duration In Seconds:".PadRight(colWidth), OASISNFT.RespawnDurationInSeconds, lineBreak);
                }
                else
                {
                    message = string.Concat(message, " Global Spawn Quantity:".PadRight(colWidth), "N/A", lineBreak);
                    message = string.Concat(message, " Player Spawn Quantity:".PadRight(colWidth), "N/A", lineBreak);
                    message = string.Concat(message, " Respawn Duration In Seconds:".PadRight(colWidth), "N/A", lineBreak);
                }
            }
            else
            {
                message = string.Concat(message, " Allow Other Players To Also Collect:".PadRight(colWidth), "N/A", lineBreak);
                message = string.Concat(message, " Global Spawn Quantity:".PadRight(colWidth), "N/A", lineBreak);
                message = string.Concat(message, " Player Spawn Quantity:".PadRight(colWidth), "N/A", lineBreak);
                message = string.Concat(message, " Respawn Duration In Seconds:".PadRight(colWidth), "N/A", lineBreak);
            }

            message = string.Concat(message, " 2D Sprite URI:".PadRight(colWidth), !string.IsNullOrEmpty(OASISNFT.Nft2DSpriteURI) ? OASISNFT.Nft2DSpriteURI : "None", lineBreak);
            message = string.Concat(message, " 2D Sprite:".PadRight(colWidth), OASISNFT.Nft2DSprite != null ? "Yes" : "None", lineBreak);
            message = string.Concat(message, " 3D Object URI:".PadRight(colWidth), !string.IsNullOrEmpty(OASISNFT.Nft3DObjectURI) ? OASISNFT.Nft3DObjectURI : "None", lineBreak);
            message = string.Concat(message, " 3D Object:".PadRight(colWidth), OASISNFT.Nft3DObject != null ? "Yes" : "None", lineBreak);
            message = string.Concat(message, " GeoNFT MetaData Provider:".PadRight(colWidth), OASISNFT.GeoNFTMetaDataProvider.Name, lineBreak);

            return message;
        }

        private string GenerateSendMessage(INFTBase nft, IMintWeb4NFTRequest request, string sendNFTTransactionHash = "", string lineBreak = "", int colWidth = 20)
        {
            string sendNFTMessage = "";

            if (!string.IsNullOrEmpty(nft.SendToAddressAfterMinting))
                sendNFTMessage = string.Concat(" Send To Address After Minting: ".PadRight(colWidth), nft.SendToAddressAfterMinting, $". {lineBreak}");

            if (!string.IsNullOrEmpty(nft.SendToAvatarAfterMintingId.ToString()) && nft.SendToAvatarAfterMintingId.ToString() != Guid.Empty.ToString())
                sendNFTMessage = string.Concat(sendNFTMessage, " Send To Avatar After Minting Id: ".PadRight(colWidth), nft.SendToAvatarAfterMintingId, $". {lineBreak}");

            if (!string.IsNullOrEmpty(nft.SendToAvatarAfterMintingUsername))
                sendNFTMessage = string.Concat(sendNFTMessage, " Send To Avatar After Minting Username: ".PadRight(colWidth), nft.SendToAvatarAfterMintingUsername, $". {lineBreak}");

            if (request != null)
            {
                if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingEmail))
                    sendNFTMessage = string.Concat(sendNFTMessage, " Send To Avatar After Minting Email: ".PadRight(colWidth), request.SendToAvatarAfterMintingEmail, $". {lineBreak}");
            }

            if (!string.IsNullOrEmpty(sendNFTTransactionHash))
                sendNFTMessage = string.Concat(sendNFTMessage, " Send NFT Hash: ".PadRight(colWidth), sendNFTTransactionHash, $". {lineBreak}");

            return sendNFTMessage;
        }

        private OASISResult<IHolon> SaveJSONMetaDataToOASIS(IMintWeb4NFTRequest request, EnumValue<ProviderType> metaDataProviderType, string json)
        {
            return Data.SaveHolon(new Holon()
            {
                MetaData = new Dictionary<string, object>()
                            {
                                { "data",  json }
                            }
            }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);
        }

        private async Task<OASISResult<IHolon>> SaveJSONMetaDataToOASISAsync(IMintWeb4NFTRequest request, EnumValue<ProviderType> metaDataProviderType, string json)
        {
            return await Data.SaveHolonAsync(new Holon()
            {
                MetaData = new Dictionary<string, object>()
                            {
                                { "data",  json }
                            }
            }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);
        }

        public string CreateMetaDataJson(IMintWeb4NFTRequest request, NFTStandardType NFTStandardType)
        {
            if (request.OnChainProvider.Value == ProviderType.SolanaOASIS)
                return CreateMetaplexJson(request);
            else
            {
                switch (NFTStandardType)
                {
                    case NFTStandardType.ERC721:
                        return CreateERC721Json(request);

                    case NFTStandardType.ERC1155:
                        return CreateERC1155Json(request);
                }
            }

            return "";
        }

        private string CreateMetaplexJson(IMintWeb4NFTRequest request)
        {
            var metadata = new
            {
                name = request.Title,
                symbol = request.Symbol,
                description = request.Description,
                seller_fee_basis_points = 500,
                image = request.ImageUrl,
                thumbnail = request.ThumbnailUrl,
                attributes = request.MetaData != null ? request.MetaData : new Dictionary<string, object>(),
                price = request.Price,
                discount = request.Discount,
                memo = request.MemoText
            };

            return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }

        private string CreateERC721Json(IMintWeb4NFTRequest request)
        {
            var metadata = new
            {
                title = request.Title,
                description = request.Description,
                image = request.ImageUrl,
                thumbnail = request.ThumbnailUrl,
                attributes = request.MetaData != null ? request.MetaData : new Dictionary<string, object>(),
                price = request.Price,
                discount = request.Discount,
                memo = request.MemoText
            };

            return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }

        private string CreateERC1155Json(IMintWeb4NFTRequest request)
        {
            var metadata = new
            {
                title = request.Title,
                description = request.Description,
                image = request.ImageUrl,
                thumbnail = request.ThumbnailUrl,
                copies = request.NumberToMint,
                attributes = request.MetaData != null ? request.MetaData : new Dictionary<string, object>(),
                price = request.Price,
                discount = request.Discount,
                memo = request.MemoText
            };

            return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }

        private IWeb3NFT UpdateWeb3NFT(IWeb3NFT web3NFT, IMintWeb3NFTRequest request)
        {
            if (web3NFT.Id == Guid.Empty)
                web3NFT.Id = Guid.NewGuid();

            web3NFT.MintedByAvatarId = request.MintedByAvatarId;
            web3NFT.SendToAddressAfterMinting = request.SendToAddressAfterMinting;
            web3NFT.SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId;
            web3NFT.SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername;
            web3NFT.Title = request.Title;
            web3NFT.Description = request.Description;
            web3NFT.Price = request.Price.Value;
            web3NFT.Discount = request.Discount.Value;
            web3NFT.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : 0;
            web3NFT.Image = request.Image;
            web3NFT.ImageUrl = request.ImageUrl;
            web3NFT.Thumbnail = request.Thumbnail;
            web3NFT.ThumbnailUrl = request.ThumbnailUrl;
            web3NFT.OnChainProvider = new EnumValue<ProviderType>(request.OnChainProvider.Value);
            web3NFT.OffChainProvider = new EnumValue<ProviderType>(request.OffChainProvider.Value);
            web3NFT.StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain.HasValue ? request.StoreNFTMetaDataOnChain.Value : false;
            web3NFT.NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(request.NFTOffChainMetaType.Value);
            web3NFT.NFTStandardType = new EnumValue<NFTStandardType>(request.NFTStandardType.Value);
            web3NFT.Symbol = request.Symbol;
            web3NFT.MintedOn = DateTime.Now;
            web3NFT.MemoText = request.MemoText;
            web3NFT.JSONMetaDataURL = request.JSONMetaDataURL;
            web3NFT.IsForSale = request.IsForSale.HasValue ? request.IsForSale.Value : false;
            web3NFT.SaleStartDate = request.SaleStartDate;
            web3NFT.SaleEndDate = request.SaleEndDate;
            web3NFT.Tags = request.Tags;
            web3NFT.MetaData = request.MetaData;
            web3NFT.MetaData["new"] = true;

            return web3NFT;
        }

        private Web4NFT CreateWeb4NFT(IMintWeb4NFTRequest request)
        {
            return new Web4NFT()
            {
                Id = Guid.NewGuid(),
                MetaData = request.MetaData,
                Tags = request.Tags,
                MintedByAvatarId = request.MintedByAvatarId,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Discount = request.Discount,
                RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : 0,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                OnChainProvider = request.OnChainProvider,
                OffChainProvider = request.OffChainProvider,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = request.NFTOffChainMetaType,
                NFTStandardType = request.NFTStandardType,
                Symbol = request.Symbol,
                MintedOn = DateTime.Now,
                MemoText = request.MemoText,
                JSONMetaDataURL = request.JSONMetaDataURL,
                IsForSale = request.IsForSale.HasValue ? request.IsForSale.Value : false,
                SaleStartDate = request.SaleStartDate,
                SaleEndDate = request.SaleEndDate
            };
        }

        private Web4NFT CreateWeb4NFT(IImportWeb3NFTRequest request)
        {
            return new Web4NFT()
            {
                Id = Guid.NewGuid(),
                MetaData = request.MetaData,
                Tags = request.Tags,
                ImportedByAvatarId = request.ImportedByAvatarId,
                ImportedOn = DateTime.Now,
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Discount = request.Discount,
                RoyaltyPercentage = request.RoyaltyPercentage,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                OnChainProvider = request.OnChainProvider,
                OffChainProvider = request.OffChainProvider,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = request.NFTOffChainMetaType,
                NFTStandardType = request.NFTStandardType,
                Symbol = request.Symbol,
                MemoText = request.MemoText,
                JSONMetaDataURL = request.JSONMetaDataURL,
                IsForSale = request.IsForSale.Value,
                SaleStartDate = request.SaleStartDate,
                SaleEndDate = request.SaleEndDate,
                Web3NFTs = new List<Web3NFT>() { new Web3NFT()
                {
                    MintTransactionHash = request.MintTransactionHash,
                    NFTMintedUsingWalletAddress = request.NFTMintedUsingWalletAddress,
                    UpdateAuthority = request.UpdateAuthority,
                    NFTTokenAddress = request.NFTTokenAddress
                } }
            };
        }

        private Web4OASISGeoSpatialNFT CreateWeb4GeoSpatialNFT(IPlaceWeb4GeoSpatialNFTRequest request, IWeb4NFT originalNftMetaData)
        {
            return new Web4OASISGeoSpatialNFT()
            {
                Id = Guid.NewGuid(),  //The NFT could be placed many times so we need a new ID for each time
                OriginalWeb4OASISNFTId = request.OriginalWeb4OASISNFTId, //We need to link back to the orignal NFT (but we copy across the NFT properties making it quicker and easier to get at the data). TODO: Do we want to copy the data across? Pros and Cons? Need to think about this... for now it's fine... ;-)
                GeoNFTMetaDataProvider = request.GeoNFTMetaDataProvider,
                JSONMetaDataURL = originalNftMetaData.JSONMetaDataURL,
                MintedByAvatarId = originalNftMetaData.MintedByAvatarId,
                SendToAddressAfterMinting = originalNftMetaData.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = originalNftMetaData.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = originalNftMetaData.SendToAvatarAfterMintingUsername,
                SellerFeeBasisPoints = originalNftMetaData.SellerFeeBasisPoints,
                Title = originalNftMetaData.Title,
                Description = originalNftMetaData.Description,
                Price = originalNftMetaData.Price,
                Discount = originalNftMetaData.Discount,
                RoyaltyPercentage = originalNftMetaData.RoyaltyPercentage,
                IsForSale = originalNftMetaData.IsForSale,
                SaleStartDate = originalNftMetaData.SaleStartDate,
                SaleEndDate = originalNftMetaData.SaleEndDate,
                Image = originalNftMetaData.Image,
                ImageUrl = originalNftMetaData.ImageUrl,
                Thumbnail = originalNftMetaData.Thumbnail,
                ThumbnailUrl = originalNftMetaData.ThumbnailUrl,
                MetaData = originalNftMetaData.MetaData,
                Tags = originalNftMetaData.Tags,
                OnChainProvider = originalNftMetaData.OnChainProvider,
                OffChainProvider = originalNftMetaData.OffChainProvider,
                StoreNFTMetaDataOnChain = originalNftMetaData.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = originalNftMetaData.NFTOffChainMetaType,
                NFTStandardType = originalNftMetaData.NFTStandardType,
                Symbol = originalNftMetaData.Symbol,
                MintedOn = originalNftMetaData.MintedOn,
                MemoText = originalNftMetaData.MemoText,
                PlacedByAvatarId = request.PlacedByAvatarId,
                Lat = request.Lat,
                Long = request.Long,
                PermSpawn = request.PermSpawn,
                PlayerSpawnQuantity = request.PlayerSpawnQuantity,
                AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect,
                GlobalSpawnQuantity = request.GlobalSpawnQuantity,
                RespawnDurationInSeconds = request.RespawnDurationInSeconds,
                PlacedOn = DateTime.Now,
                Nft2DSprite = request.Nft2DSprite,
                Nft3DObject = request.Nft3DObject,
                Nft3DObjectURI = request.Nft3DObjectURI,
                Nft2DSpriteURI = request.Nft2DSpriteURI,
                Web3NFTs = originalNftMetaData.Web3NFTs
            };
        }

        private IHolon CreateWeb4NFTMetaDataHolon(IWeb4NFT nftMetaData, IImportWeb3NFTRequest request)
        {
            IHolon holonNFT = new Holon(HolonType.Web4NFT);
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} WEB3 NFT Imported OnTo The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = request.Description;
            holonNFT.MetaData["NFT.OASISNFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData); //TODO: May remove this because its duplicated data. BUT we may need this for other purposes later such as exporting it to a file etc (but then we could just serialaize it there and then).
            holonNFT.MetaData["NFT.MintTransactionHash"] = request.MintTransactionHash;
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = request.Price.ToString();
            holonNFT.MetaData["NFT.RoyaltyPercentage"] = nftMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["NFT.IsForSale"] = nftMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["NFT.SaleStartDate"] = nftMetaData.SaleStartDate.HasValue ? nftMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.SaleEndDate"] = nftMetaData.SaleEndDate.HasValue ? nftMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.Discount"] = request.Discount.ToString();
            holonNFT.MetaData["NFT.OnChainProvider"] = nftMetaData.OnChainProvider.Name;
            holonNFT.MetaData["NFT.OffChainProvider"] = nftMetaData.OffChainProvider.Name;
            holonNFT.MetaData["NFT.StoreNFTMetaDataOnChain"] = request.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["NFT.NFTOffChainMetaType"] = nftMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["NFT.NFTStandardType"] = request.NFTStandardType.Name;
            holonNFT.MetaData["NFT.Symbol"] = request.Symbol;
            holonNFT.MetaData["NFT.Image"] = request.Image;
            holonNFT.MetaData["NFT.ImageUrl"] = request.ImageUrl;
            holonNFT.MetaData["NFT.Thumbnail"] = request.Thumbnail;
            holonNFT.MetaData["NFT.ThumbnailUrl"] = request.ThumbnailUrl;
            holonNFT.MetaData["NFT.JSONMetaDataURL"] = request.JSONMetaDataURL;
            holonNFT.MetaData["NFT.SellerFeeBasisPoints"] = nftMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(request.MetaData);
            holonNFT.MetaData["NFT.Tags"] = System.Text.Json.JsonSerializer.Serialize(request.Tags);
            holonNFT.MetaData["NFT.ImportedByAvatarId"] = request.ImportedByAvatarId.ToString();
            holonNFT.MetaData["NFT.ImportedOn"] = DateTime.Now;
            holonNFT.ParentHolonId = nftMetaData.ImportedByAvatarId;

            if (nftMetaData.Web3NFTs.Count > 0)
            {
                holonNFT.MetaData["NFT.UpdateAuthority"] = nftMetaData.Web3NFTs[0].UpdateAuthority;
                holonNFT.MetaData["NFT.NFTMintedUsingWalletAddress"] = nftMetaData.Web3NFTs[0].NFTMintedUsingWalletAddress;
                holonNFT.MetaData["NFT.NFTTokenAddress"] = nftMetaData.Web3NFTs[0].NFTTokenAddress;
            }

            return holonNFT;
        }

        private IHolon CreateWeb4NFTMetaDataHolon(IWeb4NFT nftMetaData, IMintWeb4NFTRequest request = null)
        {
            return UpdateWeb4NFTMetaDataHolon(new Holon(HolonType.Web4NFT), nftMetaData, request);
        }

        private IHolon UpdateWeb4NFTMetaDataHolon(IHolon holonNFT, IWeb4NFT nftMetaData, IMintWeb4NFTRequest request = null)
        {
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} WEB4 NFT Minted On The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.WEB4NFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData);
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.MintedByAvatarId"] = nftMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingId"] = nftMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingUsername"] = nftMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["NFT.SendToAddressAfterMinting"] = nftMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = nftMetaData.Price.ToString();
            holonNFT.MetaData["NFT.Discount"] = nftMetaData.Discount.ToString();
            holonNFT.MetaData["NFT.RoyaltyPercentage"] = nftMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["NFT.IsForSale"] = nftMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["NFT.SaleStartDate"] = nftMetaData.SaleStartDate.HasValue ? nftMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.SaleEndDate"] = nftMetaData.SaleEndDate.HasValue ? nftMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.NumberToMint"] = request != null ? request.NumberToMint.ToString() : "";
            holonNFT.MetaData["NFT.OnChainProvider"] = nftMetaData.OnChainProvider.Name;
            holonNFT.MetaData["NFT.OffChainProvider"] = nftMetaData.OffChainProvider.Name;
            holonNFT.MetaData["NFT.StoreNFTMetaDataOnChain"] = nftMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["NFT.NFTOffChainMetaType"] = nftMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["NFT.NFTStandardType"] = nftMetaData.NFTStandardType.Name;
            holonNFT.MetaData["NFT.Symbol"] = nftMetaData.Symbol;
            holonNFT.MetaData["NFT.Image"] = nftMetaData.Image;
            holonNFT.MetaData["NFT.ImageUrl"] = nftMetaData.ImageUrl;
            holonNFT.MetaData["NFT.Thumbnail"] = nftMetaData.Thumbnail;
            holonNFT.MetaData["NFT.ThumbnailUrl"] = nftMetaData.ThumbnailUrl;
            holonNFT.MetaData["NFT.JSONMetaDataURL"] = nftMetaData.JSONMetaDataURL;
            holonNFT.MetaData["NFT.JSONMetaDataURLHolonId"] = nftMetaData.JSONMetaDataURLHolonId;
            holonNFT.MetaData["NFT.MintedOn"] = nftMetaData.MintedOn.ToShortDateString();
            holonNFT.MetaData["NFT.SellerFeeBasisPoints"] = nftMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["NFT.Tags"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.Tags);
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.MetaData);
            //holonNFT.MetaData["NFT.MetaData"] = nftMetaData.MetaData; //TODO: Currently the line above works fine for normal metaData but for objects such as file uploads then it causes issues displaying the meta because it is displayed/stored as a string so there is no way to know if its a binary file.
            holonNFT.ParentHolonId = nftMetaData.MintedByAvatarId;

            //TODO: Not even sure if we need to record this anymore? Because these are not stored at the web4 level anymore, only at the web3 level.
            //if (nftMetaData.Web3NFTs.Count > 0)
            //{
            //    holonNFT.MetaData["NFT.MintTransactionHash"] = nftMetaData.Web3NFTs[0].MintTransactionHash;
            //    holonNFT.MetaData["NFT.OASISMintWalletAddress"] = nftMetaData.Web3NFTs[0].OASISMintWalletAddress;
            //    holonNFT.MetaData["NFT.SendNFTTransactionHash"] = nftMetaData.Web3NFTs[0].SendNFTTransactionHash;
            //    holonNFT.MetaData["NFT.NFTTokenAddress"] = nftMetaData.Web3NFTs[0].NFTTokenAddress;
            //    holonNFT.MetaData["NFT.UpdateAuthority"] = nftMetaData.Web3NFTs[0].UpdateAuthority;
            //}

            return holonNFT;
        }

        private IHolon CreateWeb3NFTMetaDataHolon(IWeb3NFT nftMetaData, Guid parentWeb4NFTId, IMintWeb3NFTRequest request = null)
        {
            return UpdateWeb3NFTMetaDataHolon(new Holon(HolonType.Web3NFT), nftMetaData, parentWeb4NFTId, request);
        }

        private IHolon UpdateWeb3NFTMetaDataHolon(IHolon holonNFT, IWeb3NFT nftMetaData, Guid parentWeb4NFTId, IMintWeb3NFTRequest request = null)
        {
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} WEB3 NFT Minted On The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.WEB3NFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData);
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.ParentWeb4NFTId"] = parentWeb4NFTId.ToString();
            holonNFT.MetaData["NFT.MintedByAvatarId"] = nftMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingId"] = nftMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingUsername"] = nftMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["NFT.SendToAddressAfterMinting"] = nftMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = nftMetaData.Price.ToString();
            holonNFT.MetaData["NFT.Discount"] = nftMetaData.Discount.ToString();
            holonNFT.MetaData["NFT.RoyaltyPercentage"] = nftMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["NFT.IsForSale"] = nftMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["NFT.SaleStartDate"] = nftMetaData.SaleStartDate.HasValue ? nftMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.SaleEndDate"] = nftMetaData.SaleEndDate.HasValue ? nftMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.NumberToMint"] = request != null ? request.NumberToMint.ToString() : "";
            holonNFT.MetaData["NFT.OnChainProvider"] = nftMetaData.OnChainProvider.Name;
            holonNFT.MetaData["NFT.OffChainProvider"] = nftMetaData.OffChainProvider.Name;
            holonNFT.MetaData["NFT.StoreNFTMetaDataOnChain"] = nftMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["NFT.NFTOffChainMetaType"] = nftMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["NFT.NFTStandardType"] = nftMetaData.NFTStandardType.Name;
            holonNFT.MetaData["NFT.Symbol"] = nftMetaData.Symbol;
            holonNFT.MetaData["NFT.Image"] = nftMetaData.Image;
            holonNFT.MetaData["NFT.ImageUrl"] = nftMetaData.ImageUrl;
            holonNFT.MetaData["NFT.Thumbnail"] = nftMetaData.Thumbnail;
            holonNFT.MetaData["NFT.ThumbnailUrl"] = nftMetaData.ThumbnailUrl;
            holonNFT.MetaData["NFT.JSONMetaDataURL"] = nftMetaData.JSONMetaDataURL;
            holonNFT.MetaData["NFT.JSONMetaDataURLHolonId"] = nftMetaData.JSONMetaDataURLHolonId;
            holonNFT.MetaData["NFT.MintedOn"] = nftMetaData.MintedOn.ToShortDateString();
            holonNFT.MetaData["NFT.SellerFeeBasisPoints"] = nftMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["NFT.Tags"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.Tags);
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.MetaData);
            //holonNFT.MetaData["NFT.MetaData"] = nftMetaData.MetaData; //TODO: Currently the line above works fine for normal metaData but for objects such as file uploads then it causes issues displaying the meta because it is displayed/stored as a string so there is no way to know if its a binary file.
            holonNFT.ParentHolonId = nftMetaData.MintedByAvatarId;

            return holonNFT;
        }

        private IHolon CreateWeb4GeoSpatialNFTMetaDataHolon(IWeb4GeoSpatialNFT geoNFTMetaData, IMintWeb4NFTRequest request = null)
        {
            return UpdateWeb4GeoNFTMetaDataHolon(new Holon(HolonType.Web4GeoNFT), geoNFTMetaData, request);
        }

        private IHolon UpdateWeb4GeoNFTMetaDataHolon(IHolon holonNFT, IWeb4GeoSpatialNFT geoNFTMetaData, IMintWeb4NFTRequest request = null)
        {
            holonNFT.Id = geoNFTMetaData.Id;
            holonNFT.Name = "WEB4 OASIS GEO NFT";
            holonNFT.Description = "WEB4 OASIS GEO NFT";
            holonNFT.MetaData["GEONFT.WEB4GEONFT"] = System.Text.Json.JsonSerializer.Serialize(geoNFTMetaData); //TODO: May remove this because its duplicated data.
            holonNFT.MetaData["GEONFT.Id"] = geoNFTMetaData.Id;
            holonNFT.MetaData["GEONFT.GeoNFTMetaDataProvider"] = geoNFTMetaData.GeoNFTMetaDataProvider.Name;
            holonNFT.MetaData["GEONFT.PlacedByAvatarId"] = geoNFTMetaData.PlacedByAvatarId.ToString();
            holonNFT.MetaData["GEONFT.PlacedOn"] = geoNFTMetaData.PlacedOn.ToShortDateString();
            holonNFT.MetaData["GEONFT.Lat"] = geoNFTMetaData.Lat;
            holonNFT.MetaData["GEONFT.Long"] = geoNFTMetaData.Long;
            holonNFT.MetaData["GEONFT.LatLong"] = string.Concat(geoNFTMetaData.Lat, ":", geoNFTMetaData.Long);
            holonNFT.MetaData["GEONFT.PermSpawn"] = geoNFTMetaData.PermSpawn;
            holonNFT.MetaData["GEONFT.PlayerSpawnQuantity"] = geoNFTMetaData.PlayerSpawnQuantity;
            holonNFT.MetaData["GEONFT.AllowOtherPlayersToAlsoCollect"] = geoNFTMetaData.AllowOtherPlayersToAlsoCollect;
            holonNFT.MetaData["GEONFT.GlobalSpawnQuantity"] = geoNFTMetaData.GlobalSpawnQuantity;
            holonNFT.MetaData["GEONFT.RespawnDurationInSeconds"] = geoNFTMetaData.RespawnDurationInSeconds;
            holonNFT.MetaData["GEONFT.Nft2DSprite"] = geoNFTMetaData.Nft2DSprite;
            holonNFT.MetaData["GEONFT.Nft2DSpriteURI"] = geoNFTMetaData.Nft2DSpriteURI;
            holonNFT.MetaData["GEONFT.Nft3DObject"] = geoNFTMetaData.Nft3DObject;
            holonNFT.MetaData["GEONFT.Nft3DObjectURI"] = geoNFTMetaData.Nft3DObjectURI;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Id"] = geoNFTMetaData.OriginalWeb4OASISNFTId;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MemoText"] = geoNFTMetaData.MemoText;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Title"] = geoNFTMetaData.Title;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Description"] = geoNFTMetaData.Description;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MintedByAvatarId"] = geoNFTMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingId"] = geoNFTMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingUsername"] = geoNFTMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAddressAfterMinting"] = geoNFTMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Price"] = geoNFTMetaData.Price.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Discount"] = geoNFTMetaData.Discount.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.RoyaltyPercentage"] = geoNFTMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.IsForSale"] = geoNFTMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SaleStartDate"] = geoNFTMetaData.SaleStartDate.HasValue ? geoNFTMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SaleEndDate"] = geoNFTMetaData.SaleEndDate.HasValue ? geoNFTMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.OnChainProvider"] = geoNFTMetaData.OnChainProvider.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.OffChainProvider"] = geoNFTMetaData.OffChainProvider.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.StoreNFTMetaDataOnChain"] = geoNFTMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.NFTOffChainMetaType"] = geoNFTMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.NFTStandardType"] = geoNFTMetaData.NFTStandardType.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Symbol"] = geoNFTMetaData.Symbol;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Image"] = geoNFTMetaData.Image;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.ImageUrl"] = geoNFTMetaData.ImageUrl;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Thumbnail"] = geoNFTMetaData.Thumbnail;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.ThumbnailUrl"] = geoNFTMetaData.ThumbnailUrl;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.JSONMetaDataURL"] = geoNFTMetaData.JSONMetaDataURL;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.JSONMetaDataURLHolonId"] = geoNFTMetaData.JSONMetaDataURLHolonId;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MintedOn"] = geoNFTMetaData.MintedOn.ToShortDateString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SellerFeeBasisPoints"] = geoNFTMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MetaData"] = geoNFTMetaData.MetaData;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Tags"] = geoNFTMetaData.Tags;

            //TODO: Not even sure if we need to record this anymore? Because these are not stored at the web4 level anymore, only at the web3 level.
            //if (geoNFTMetaData.Web3NFTs.Count > 0)
            //{
            //    holonNFT.MetaData["NFT.MintTransactionHash"] = geoNFTMetaData.Web3NFTs[0].MintTransactionHash;
            //    holonNFT.MetaData["NFT.OASISMintWalletAddress"] = geoNFTMetaData.Web3NFTs[0].OASISMintWalletAddress;
            //    holonNFT.MetaData["NFT.SendNFTTransactionHash"] = geoNFTMetaData.Web3NFTs[0].SendNFTTransactionHash;
            //    holonNFT.MetaData["NFT.NFTTokenAddress"] = geoNFTMetaData.Web3NFTs[0].NFTTokenAddress;
            //    holonNFT.MetaData["NFT.UpdateAuthority"] = geoNFTMetaData.Web3NFTs[0].UpdateAuthority;
            //}

            return holonNFT;
        }

        private IMintWeb4NFTRequest CreateMintWeb4NFTTransactionRequest(IMintAndPlaceWeb4GeoSpatialNFTRequest mintAndPlaceGeoSpatialNFTRequest)
        {
            return new MintWeb4NFTRequest()
            {
                //MintWalletAddress = mintAndPlaceGeoSpatialNFTRequest.MintWalletAddress,
                MintedByAvatarId = mintAndPlaceGeoSpatialNFTRequest.MintedByAvatarId,
                Title = mintAndPlaceGeoSpatialNFTRequest.Title,
                Description = mintAndPlaceGeoSpatialNFTRequest.Description,
                Image = mintAndPlaceGeoSpatialNFTRequest.Image,
                ImageUrl = mintAndPlaceGeoSpatialNFTRequest.ImageUrl,
                Thumbnail = mintAndPlaceGeoSpatialNFTRequest.Thumbnail,
                ThumbnailUrl = mintAndPlaceGeoSpatialNFTRequest.ThumbnailUrl,
                Price = mintAndPlaceGeoSpatialNFTRequest.Price,
                Discount = mintAndPlaceGeoSpatialNFTRequest.Discount,
                RoyaltyPercentage = mintAndPlaceGeoSpatialNFTRequest.RoyaltyPercentage,
                IsForSale = mintAndPlaceGeoSpatialNFTRequest.IsForSale,
                SaleStartDate = mintAndPlaceGeoSpatialNFTRequest.SaleStartDate,
                SaleEndDate = mintAndPlaceGeoSpatialNFTRequest.SaleEndDate,
                MemoText = mintAndPlaceGeoSpatialNFTRequest.MemoText,
                NumberToMint = mintAndPlaceGeoSpatialNFTRequest.NumberToMint,
                MetaData = mintAndPlaceGeoSpatialNFTRequest.MetaData,
                Tags = mintAndPlaceGeoSpatialNFTRequest.Tags,
                OffChainProvider = mintAndPlaceGeoSpatialNFTRequest.OffChainProvider,
                OnChainProvider = mintAndPlaceGeoSpatialNFTRequest.OnChainProvider,
                JSONMetaDataURL = mintAndPlaceGeoSpatialNFTRequest.JSONMetaDataURL,
                NFTOffChainMetaType = mintAndPlaceGeoSpatialNFTRequest.NFTOffChainMetaType,
                NFTStandardType = mintAndPlaceGeoSpatialNFTRequest.NFTStandardType,
                SendToAddressAfterMinting = mintAndPlaceGeoSpatialNFTRequest.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingUsername,
                StoreNFTMetaDataOnChain = mintAndPlaceGeoSpatialNFTRequest.StoreNFTMetaDataOnChain,
                Symbol = mintAndPlaceGeoSpatialNFTRequest.Symbol,
                AttemptToMintEveryXSeconds = mintAndPlaceGeoSpatialNFTRequest.AttemptToMintEveryXSeconds,
                AttemptToSendEveryXSeconds = mintAndPlaceGeoSpatialNFTRequest.AttemptToSendEveryXSeconds,
                JSONMetaData = mintAndPlaceGeoSpatialNFTRequest.JSONMetaData,
                SendToAvatarAfterMintingEmail = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingEmail,
                WaitForNFTToMintInSeconds = mintAndPlaceGeoSpatialNFTRequest.WaitForNFTToMintInSeconds,
                WaitForNFTToSendInSeconds = mintAndPlaceGeoSpatialNFTRequest.WaitForNFTToSendInSeconds,
                WaitTillNFTMinted = mintAndPlaceGeoSpatialNFTRequest.WaitTillNFTMinted,
                WaitTillNFTSent = mintAndPlaceGeoSpatialNFTRequest.WaitTillNFTSent,
                Web3NFTs = mintAndPlaceGeoSpatialNFTRequest.Web3NFTs
            };
        }

        private OASISResult<IWeb3NFT> DecodeNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IWeb3NFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (IWeb3NFT)System.Text.Json.JsonSerializer.Deserialize(holonResult.Result.MetaData["NFT.WEB3NFT"].ToString(), typeof(Web3NFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IWeb4NFT> DecodeNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IWeb4NFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (IWeb4NFT)System.Text.Json.JsonSerializer.Deserialize(holonResult.Result.MetaData["NFT.WEB4NFT"].ToString(), typeof(Web4NFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IWeb4GeoSpatialNFT> DecodeGeoNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IWeb4GeoSpatialNFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (Web4OASISGeoSpatialNFT)System.Text.Json.JsonSerializer.Deserialize(holonResult.Result.MetaData["GEONFT.WEB4GEONFT"].ToString(), typeof(Web4OASISGeoSpatialNFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IWeb3NFT>> DecodeNFTMetaData(OASISResult<IEnumerable<IHolon>> holonsResult, OASISResult<IEnumerable<IWeb3NFT>> result, string errorMessage)
        {
            List<IWeb3NFT> nfts = new List<IWeb3NFT>();

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                if (holonsResult.Result.Count() > 0)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        nfts.Add((IWeb3NFT)System.Text.Json.JsonSerializer.Deserialize(holon.MetaData["NFT.WEB3NFT"].ToString(), typeof(Web3NFT)));

                    result.Result = nfts;
                }
                else
                    result.Message = "No NFT's Found.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IWeb4NFT>> DecodeNFTMetaData(OASISResult<IEnumerable<IHolon>> holonsResult, OASISResult<IEnumerable<IWeb4NFT>> result, string errorMessage)
        {
            List<IWeb4NFT> nfts = new List<IWeb4NFT>();

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                if (holonsResult.Result.Count() > 0)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        nfts.Add((IWeb4NFT)System.Text.Json.JsonSerializer.Deserialize(holon.MetaData["NFT.WEB4NFT"].ToString(), typeof(Web4NFT)));

                    result.Result = nfts;
                }
                else
                    result.Message = "No NFT's Found.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> DecodeGeoNFTMetaData(OASISResult<IEnumerable<IHolon>> holonsResult, OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result, string errorMessage)
        {
            List<IWeb4GeoSpatialNFT> nfts = new List<IWeb4GeoSpatialNFT>();

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                if (holonsResult.Result.Count() > 0)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        nfts.Add((IWeb4GeoSpatialNFT)System.Text.Json.JsonSerializer.Deserialize(holon.MetaData["GEONFT.WEB4GEONFT"].ToString(), typeof(Web4OASISGeoSpatialNFT)));

                    result.Result = nfts;
                }
                else
                    result.Message = "No GeoNFT's Found.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");

            return result;
        }

        private IWeb3NFT UpdateWeb3NFT(IWeb3NFT web3NFT, IUpdateWeb4NFTRequest request)
        {
            web3NFT.Title = !string.IsNullOrEmpty(request.Title) ? request.Title : web3NFT.Title;
            web3NFT.Description = !string.IsNullOrEmpty(request.Description) ? request.Description : web3NFT.Description;
            web3NFT.ModifiedByAvatarId = request.ModifiedByAvatarId != Guid.Empty ? request.ModifiedByAvatarId : web3NFT.ModifiedByAvatarId;
            web3NFT.ModifiedOn = DateTime.Now;
            web3NFT.ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : web3NFT.ImageUrl;
            web3NFT.Image = request.Image != null ? request.Image : web3NFT.Image;
            web3NFT.ThumbnailUrl = !string.IsNullOrEmpty(request.ThumbnailUrl) ? request.ThumbnailUrl : web3NFT.ThumbnailUrl;
            web3NFT.Thumbnail = request.Thumbnail != null ? request.Thumbnail : web3NFT.Thumbnail;
            web3NFT.MetaData = request.MetaData != null ? request.MetaData : web3NFT.MetaData;
            web3NFT.Tags = request.Tags ?? web3NFT.Tags;
            web3NFT.Price = request.Price.HasValue ? request.Price.Value : web3NFT.Price;
            web3NFT.Discount = request.Discount.HasValue ? request.Discount.Value : web3NFT.Discount;
            web3NFT.IsForSale = request.IsForSale.HasValue ? request.IsForSale.Value : web3NFT.IsForSale;
            web3NFT.SalesHistory = request.SalesHistory ?? web3NFT.SalesHistory;
            web3NFT.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : web3NFT.RoyaltyPercentage;
            web3NFT.CurrentOwnerAvatarId = request.CurrentOwnerAvatarId != Guid.Empty ? request.CurrentOwnerAvatarId : web3NFT.CurrentOwnerAvatarId;
            web3NFT.PreviousOwnerAvatarId = request.PreviousOwnerAvatarId != Guid.Empty ? request.PreviousOwnerAvatarId : web3NFT.PreviousOwnerAvatarId;
            web3NFT.LastPurchasedByAvatarId = request.LastPurchasedByAvatarId != Guid.Empty ? request.LastPurchasedByAvatarId : web3NFT.LastPurchasedByAvatarId;
            web3NFT.LastSaleAmount = request.LastSaleAmount.HasValue ? request.LastSaleAmount.Value : web3NFT.LastSaleAmount;
            web3NFT.LastSaleDate = request.LastSaleDate != DateTime.MinValue ? request.LastSaleDate : web3NFT.LastSaleDate;
            web3NFT.LastSaleDiscount = request.LastSaleDiscount.HasValue ? request.LastSaleDiscount.Value : web3NFT.LastSaleDiscount;
            web3NFT.LastSalePrice = request.LastSalePrice.HasValue ? request.LastSalePrice.Value : web3NFT.LastSalePrice;
            web3NFT.LastSaleQuantity = request.LastSaleQuantity.HasValue ? request.LastSaleQuantity.Value : web3NFT.LastSaleQuantity;
            web3NFT.LastSaleTax = request.LastSaleTax.HasValue ? request.LastSaleTax.Value : web3NFT.LastSaleTax;
            web3NFT.LastSaleTransactionHash = !string.IsNullOrEmpty(request.LastSaleTransactionHash) ? request.LastSaleTransactionHash : web3NFT.LastSaleTransactionHash;
            web3NFT.LastSoldByAvatarId = request.LastSoldByAvatarId != Guid.Empty ? request.LastSoldByAvatarId : web3NFT.LastSoldByAvatarId;
            web3NFT.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : web3NFT.RoyaltyPercentage;
            web3NFT.SaleEndDate = request.SaleEndDate.HasValue ? request.SaleEndDate.Value : web3NFT.SaleEndDate;
            web3NFT.SaleStartDate = request.SaleStartDate.HasValue ? request.SaleStartDate.Value : web3NFT.SaleStartDate;
            web3NFT.TotalNumberOfSales = request.TotalNumberOfSales.HasValue ? request.TotalNumberOfSales.Value : web3NFT.TotalNumberOfSales;

            return web3NFT;
        }

        //TODO: Lots more coming soon! ;-)
    }
}