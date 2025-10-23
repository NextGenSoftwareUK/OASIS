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
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using SharpCompress.Common;


namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class NFTManager : COSMICManagerBase, INFTManager
    {
        private const int FORMAT_SUCCESS_MESSAGE_COL_WIDTH = 30;
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
        //public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest request)
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
        //public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest request)
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

        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<INFTTransactionRespone> result = new OASISResult<INFTTransactionRespone>();
            string errorMessage = "Error occured in SendNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.FromProvider.Value, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                {

                    bool attemptingToSend = true;
                    DateTime startTime = DateTime.Now;

                    do
                    {
                        result = await nftProviderResult.Result.SendNFTAsync(request);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Result.OASISNFT.SendNFTTransactionHash = result.Result.TransactionResult;
                            result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }
                        else if (!request.WaitTillNFTSent)
                        {
                            result.Result.OASISNFT.SendNFTTransactionHash = $"Error occured attempting to send NFT & WaitTillNFTSent is false. Reason: {result.Message}";
                            result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            result.Result.OASISNFT.SendNFTTransactionHash = $"Error occured attempting to send NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!";
                            result.Message = FormatSuccessMessage(request, result, responseFormatType);
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

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<INFTTransactionRespone> result = new OASISResult<INFTTransactionRespone>();
            string errorMessage = "Error occured in SendNFT in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.FromProvider.Value, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                {

                    bool attemptingToSend = true;
                    DateTime startTime = DateTime.Now;

                    do
                    {
                        result = nftProviderResult.Result.SendNFT(request);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Result.OASISNFT.SendNFTTransactionHash = result.Result.TransactionResult;
                            result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }
                        else if (!request.WaitTillNFTSent)
                        {
                            result.Result.OASISNFT.SendNFTTransactionHash = $"Error occured attempting to send NFT & WaitTillNFTSent is false. Reason: {result.Message}";
                            result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            result.Result.OASISNFT.SendNFTTransactionHash = $"Error occured attempting to send NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!";
                            result.Message = FormatSuccessMessage(request, result, responseFormatType);
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


        public async Task<OASISResult<INFTTransactionRespone>> MintNftAsync(IMintNFTTransactionRequest request, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<INFTTransactionRespone> result = new OASISResult<INFTTransactionRespone>();
            string errorMessage = "Error occured in MintNftAsync in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
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
                        request.SendToAvatarAfterMintingId = avatarResult.Result.Id;
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
                        request.SendToAvatarAfterMintingId = avatarResult.Result.Id;
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
                        {
                            currentAvatar = avatarResult.Result;

                            foreach (ProviderType providerType in currentAvatar.ProviderWallets.Keys)
                            {
                                if (providerType == request.OnChainProvider.Value)
                                {
                                    request.SendToAddressAfterMinting = currentAvatar.ProviderWallets[request.OnChainProvider.Value][0].WalletAddress;
                                    break;
                                }
                            }

                            if (string.IsNullOrEmpty(request.SendToAddressAfterMinting))
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for avatar {request.MintedByAvatarId} and provider {request.OnChainProvider.Value}. Please make sure you link a valid wallet to the avatar using the Wallet API or Key API.");
                                return result;
                            }
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to load the avatar details for the SendToAvatarAfterMinting {request.MintedByAvatarId}. Reason: {avatarResult.Message}");
                            return result;
                        }
                    }
                }

                if (string.IsNullOrEmpty(request.SendToAddressAfterMinting))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} SendToAddressAfterMinting is null! Please make sure a valid SendToAddressAfterMinting is set or a valid SendToAvatarAfterMinting.");
                    return result;
                }

                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.OnChainProvider.Value, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                {
                    string geoNFTMemoText = "";

                    if (isGeoNFT)
                        geoNFTMemoText = "Geo";

                    request.MemoText = $"{request.OnChainProvider.Name} {geoNFTMemoText}NFT minted on The OASIS with title '{request.Title}' by avatar with id {request.MintedByAvatarId} for the price of {request.Price}. {request.MemoText}";

                    EnumValue<ProviderType> NFTMetaDataProviderType;

                    if (request.OffChainProvider == null)
                        request.OffChainProvider = new EnumValue<ProviderType>(ProviderType.None);

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

                    result = await MintNFTInternalAsync(request, request.NFTStandardType.Value, NFTMetaDataProviderType, nftProviderResult, result, errorMessage, responseFormatType);

                    //switch (request.NFTStandardType)
                    //{
                    //    case NFTStandardType.ERC721:
                    //        result = await MintNFTInternalAsync(request, NFTStandardType.ERC721, NFTMetaDataProviderType, nftProviderResult, result, errorMessage);
                    //        break;

                    //    case NFTStandardType.ERC1155:
                    //        result = await MintNFTInternalAsync(request, NFTStandardType.ERC1155, NFTMetaDataProviderType, nftProviderResult, result, errorMessage);
                    //        break;

                    //    case NFTStandardType.SPL:
                    //        result = await MintNFTInternalAsync(request, NFTStandardType.SPL, NFTMetaDataProviderType, nftProviderResult, result, errorMessage);
                    //        break;

                    //        //case NFTStandardType.Both:
                    //        //    result = await MintNFTInternalAsync(request, NFTStandardType.ERC721, NFTMetaDataProviderType, nftProviderResult, result, errorMessage);
                    //        //    result = await MintNFTInternalAsync(request, NFTStandardType.ERC1155, NFTMetaDataProviderType, nftProviderResult, result, errorMessage);
                    //        //    break;
                    //}
                }
                else
                {
                    result.Result = null;
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

        public OASISResult<INFTTransactionRespone> MintNft(IMintNFTTransactionRequest request, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<INFTTransactionRespone> result = new OASISResult<INFTTransactionRespone>();
            string errorMessage = "Error occured in MintNft in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                OASISResult<bool> nftStandardValid = IsNFTStandardTypeValid(request, errorMessage);

                if (nftStandardValid != null && nftStandardValid.IsError)
                {
                    result.IsError = true;
                    result.Message = nftStandardValid.Message;
                    return result;
                }

                if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingEmail))
                {
                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(request.SendToAvatarAfterMintingEmail);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        request.SendToAvatarAfterMintingId = avatarResult.Result.Id;
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to load the avatar details for the SendToAvatarAfterMintingEmail {request.SendToAvatarAfterMintingEmail}. The email is likely not valid. Reason: {avatarResult.Message}");
                        return result;
                    }
                }

                if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingUsername))
                {
                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(request.SendToAvatarAfterMintingUsername);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        request.SendToAvatarAfterMintingId = avatarResult.Result.Id;
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
                        OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(request.MintedByAvatarId);

                        if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                        {
                            currentAvatar = avatarResult.Result;

                            foreach (ProviderType providerType in currentAvatar.ProviderWallets.Keys)
                            {
                                if (providerType == request.OnChainProvider.Value)
                                {
                                    request.SendToAddressAfterMinting = currentAvatar.ProviderWallets[request.OnChainProvider.Value][0].WalletAddress;
                                    break;
                                }
                            }

                            if (string.IsNullOrEmpty(request.SendToAddressAfterMinting))
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for avatar {request.MintedByAvatarId} and provider {request.OnChainProvider.Value}. Please make sure you link a valid wallet to the avatar using the Wallet API or Key API.");
                                return result;
                            }
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to load the avatar details for the SendToAvatarAfterMinting {request.MintedByAvatarId}. Reason: {avatarResult.Message}");
                            return result;
                        }
                    }
                }

                if (string.IsNullOrEmpty(request.SendToAddressAfterMinting))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} SendToAddressAfterMinting is null! Please make sure a valid SendToAddressAfterMinting is set or a valid SendToAvatarAfterMinting.");
                    return result;
                }

                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.OnChainProvider.Value, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                {
                    string geoNFTMemoText = "";

                    if (isGeoNFT)
                        geoNFTMemoText = "Geo";

                    request.MemoText = $"{request.OnChainProvider.Name} {geoNFTMemoText}NFT minted on The OASIS with title '{request.Title}' by avatar with id {request.MintedByAvatarId} for the price of {request.Price}. {request.MemoText}";

                    if (request.OffChainProvider == null)
                        request.OffChainProvider = new EnumValue<ProviderType>(ProviderType.None);

                    EnumValue<ProviderType> NFTMetaDataProviderType;

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

                    result = MintNFTInternal(request, request.NFTStandardType.Value, NFTMetaDataProviderType, nftProviderResult, result, errorMessage, responseFormatType);
                }
                else
                {
                    result.Result = null;
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

        public async Task<OASISResult<IOASISNFT>> ImportWeb3NFTAsync(IImportWeb3NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in ImportWeb3NFT in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                result.Result = CreateOASISNFT(request);

                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (request.OffChainProvider.Value == ProviderType.None)
                    request.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateNFTMetaDataHolon(result.Result, request), request.ImportedByByAvatarId, true, true, 0, true, false, request.OffChainProvider.Value);

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

        public async Task<OASISResult<IOASISNFT>> ImportWeb3NFT(IImportWeb3NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in ImportWeb3NFT in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                result.Result = CreateOASISNFT(request);

                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (request.OffChainProvider.Value == ProviderType.None)
                    request.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateNFTMetaDataHolon(result.Result, request), request.ImportedByByAvatarId, true, true, 0, true, false, request.OffChainProvider.Value);

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

        public async Task<OASISResult<IOASISNFT>> ImportOASISNFTAsync(Guid importedByAvatarId, string fullPathToOASISNFTJsonFile, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            return await ImportOASISNFTAsync(importedByAvatarId, JsonConvert.DeserializeObject<IOASISNFT>(await File.ReadAllTextAsync(fullPathToOASISNFTJsonFile)));
        }

        public async Task<OASISResult<IOASISNFT>> ImportOASISNFTAsync(Guid importedByAvatarId, IOASISNFT OASISNFT, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in ImportOASISNFTAsync in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (OASISNFT.OffChainProvider.Value == ProviderType.None)
                    OASISNFT.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateNFTMetaDataHolon(OASISNFT), importedByAvatarId, true, true, 0, true, false, providerType);

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

        public async Task<OASISResult<IOASISNFT>> ExportOASISNFTAsync(Guid OASISNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IOASISNFT> exportResult = await LoadNftAsync(OASISNFTId, providerType);

            if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
            {
                return await ExportOASISNFTAsync(exportResult.Result, fullPathToExportTo, providerType, responseFormatType);
            }
            else
                return exportResult;
        }

        public async Task<OASISResult<IOASISNFT>> ExportOASISNFTAsync(IOASISNFT OASISNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            await File.WriteAllTextAsync(fullPathToExportTo, JsonConvert.SerializeObject(OASISNFT, Formatting.Indented));
            return new OASISResult<IOASISNFT>(OASISNFT);
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> ImportOASISGeoNFTAsync(Guid importedByAvatarId, string fullPathToOASISGeoNFTJsonFile, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            return await ImportOASISGeoNFTAsync(importedByAvatarId, JsonConvert.DeserializeObject<IOASISGeoSpatialNFT>(await File.ReadAllTextAsync(fullPathToOASISGeoNFTJsonFile)));
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> ImportOASISGeoNFTAsync(Guid importedByAvatarId, IOASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in ImportOASISGeoNFTAsync in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (OASISGeoNFT.OffChainProvider.Value == ProviderType.None)
                    OASISGeoNFT.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateGeoSpatialNFTMetaDataHolon(OASISGeoNFT), importedByAvatarId, true, true, 0, true, false, providerType);

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

        public async Task<OASISResult<IOASISGeoSpatialNFT>> ExportOASISGeoNFTAsync(Guid OASISGeoNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IOASISGeoSpatialNFT> exportResult = await LoadGeoNftAsync(OASISGeoNFTId, providerType);

            if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
                return await ExportOASISGeoNFTAsync(exportResult.Result, fullPathToExportTo, providerType, responseFormatType);
            else
                return exportResult;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> ExportOASISGeoNFTAsync(IOASISGeoSpatialNFT OASISGeoNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            await File.WriteAllTextAsync(fullPathToExportTo, JsonConvert.SerializeObject(OASISGeoNFT, Formatting.Indented));
            return new OASISResult<IOASISGeoSpatialNFT>(OASISGeoNFT);
        }

        public OASISResult<bool> IsNFTStandardTypeValid(IMintNFTTransactionRequest request, string errorMessage = "")
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

        public async Task<OASISResult<IOASISNFT>> LoadNftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in LoadNftAsync in NFTManager. Reason:";

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

        public OASISResult<IOASISNFT> LoadNft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in LoadNft in NFTManager. Reason:";

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

        public async Task<OASISResult<IOASISNFT>> LoadNftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in LoadNftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IOASISNFT> LoadNft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in LoadNft in NFTManager. Reason:";

            try
            {
                //result = DecodeNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> LoadGeoNftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in LoadGeoNftAsync in NFTManager. Reason:";

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

        public OASISResult<IOASISGeoSpatialNFT> LoadGeoNft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in LoadGeoNft in NFTManager. Reason:";

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

        public async Task<OASISResult<IOASISGeoSpatialNFT>> LoadGeoNftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in LoadGeoNftAsync in NFTManager. Reason:";

            try
            {
                //result = DecodeGeoNFTMetaData(await Data.LoadHolonByCustomKeyAsync(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeGeoNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IOASISGeoSpatialNFT> LoadGeoNft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in LoadGeoNft in NFTManager. Reason:";

            try
            {
                //result = DecodeGeoNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeGeoNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFT>> result = new OASISResult<IEnumerable<IOASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

                //TODO: Want to add new LoadHolonsForAvatar methods to HolonManager eventually, which we would use here instead. It would load all Holons that had CreatedByAvatarId = avatarId. But for now we can just set the ParentId on the holons to the AvatarId.
                // OASISResult<IEnumerable<IHolon>> holonsResult = await Data.LoadHolonsForParentAsync(avatarId, HolonType.NFT, true, true, 0, true, 0, providerType); //This line would also work because by default all holons created have their parent set to the avatar that created them in the HolonManger.
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IOASISNFT>> LoadAllNFTsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFT>> result = new OASISResult<IEnumerable<IOASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForAvatar in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFT>> result = new OASISResult<IEnumerable<IOASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForMintAddressAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintWalletAddress", mintWalletAddress, HolonType.NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

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
                //        result.Result.Add((IOASISNFT)JsonSerializer.Deserialize(holon.MetaData["OASISNFT"].ToString(), typeof(IOASISNFT)));
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

        public OASISResult<IEnumerable<IOASISNFT>> LoadAllNFTsForMintAddress(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFT>> result = new OASISResult<IEnumerable<IOASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForMintAddress in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintedByAvatarId", mintWalletAddress, HolonType.NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadHolonsByMetaDataAsync("GEONFT.PlacedByAvatarId", avatarId.ToString(), HolonType.GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForAvatar in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadHolonsByMetaData("GEONFT.MintedByAvatarId", avatarId.ToString(), HolonType.GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForMintAddressAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadHolonsByMetaDataAsync("GEONFT.OriginalOASISNFT.MintWalletAddress", mintWalletAddress, HolonType.GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForMintAddress in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadHolonsByMetaData("GEONFT.OriginalOASISNFT.MintWalletAddress", mintWalletAddress, HolonType.GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarLocationAsync(long latLocation, long longLocation, int radius, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForAvatarLocationAsync in NFTManager. Reason:";

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
                    OASISResult<IEnumerable<IOASISGeoSpatialNFT>> geoNfts = await LoadAllGeoNFTsAsync(providerType);

                    if (geoNfts != null && !geoNfts.IsError && geoNfts.Result != null)
                    {
                        List<IOASISGeoSpatialNFT> matchedGeoNFTs = new List<IOASISGeoSpatialNFT>();

                        foreach (IOASISGeoSpatialNFT geoSpatialNFT in geoNfts.Result)
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
                    result = DecodeGeoNFTMetaData(await Data.LoadHolonsByMetaDataAsync("GEONFT.LatLong", string.Concat(latLocation.ToString(), ":", longLocation.ToString()), HolonType.GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatarLocation(long latLocation, long longLocation, int radius, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForAvatarLocationAsync in NFTManager. Reason:";

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
                    OASISResult<IEnumerable<IOASISGeoSpatialNFT>> geoNfts = LoadAllGeoNFTs(providerType);

                    if (geoNfts != null && !geoNfts.IsError && geoNfts.Result != null)
                    {
                        List<IOASISGeoSpatialNFT> matchedGeoNFTs = new List<IOASISGeoSpatialNFT>();

                        foreach (IOASISGeoSpatialNFT geoSpatialNFT in geoNfts.Result)
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
                    result = DecodeGeoNFTMetaData(Data.LoadHolonsByMetaData("GEONFT.LatLong", string.Concat(latLocation.ToString(), ":", longLocation.ToString()), HolonType.GeoNFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISNFT>>> LoadAllNFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFT>> result = new OASISResult<IEnumerable<IOASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadAllHolonsAsync(HolonType.NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IOASISNFT>> LoadAllNFTs(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFT>> result = new OASISResult<IEnumerable<IOASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTs in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadAllHolons(HolonType.NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadAllHolonsAsync(HolonType.GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IOASISGeoSpatialNFT>> LoadAllGeoNFTs(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTs in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadAllHolons(HolonType.GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in PlaceGeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFT> loadNftResult = await LoadNftAsync(request.OriginalOASISNFTId, request.OriginalOASISNFTOffChainProvider.Value);

                if (loadNftResult != null && !loadNftResult.IsError && loadNftResult.Result != null)
                {
                    result.Result = CreateGeoSpatialNFT(request, loadNftResult.Result);
                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateGeoSpatialNFTMetaDataHolon(result.Result), request.PlacedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider.Value);

                    if ((saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null)) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), request.GeoNFTMetaDataProvider)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = FormatSuccessMessage(result, responseFormatType: responseFormatType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading original OASIS NFT with id {request.OriginalOASISNFTId}. Reason: {loadNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in PlaceGeoNFT in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFT> loadNftResult = LoadNft(request.OriginalOASISNFTId, request.OriginalOASISNFTOffChainProvider.Value);

                if (loadNftResult != null && !loadNftResult.IsError && loadNftResult.Result != null)
                {
                    result.Result = CreateGeoSpatialNFT(request, loadNftResult.Result);
                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateGeoSpatialNFTMetaDataHolon(result.Result), request.PlacedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider.Value);

                    if ((saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null)) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), request.GeoNFTMetaDataProvider)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = FormatSuccessMessage(result, responseFormatType: responseFormatType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading original OASIS NFT with id {request.OriginalOASISNFTId}. Reason: {loadNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in MintAndPlaceGeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<INFTTransactionRespone> mintNftResult = await MintNftAsync(CreateMintNFTTransactionRequest(request), true);

                if (mintNftResult != null && mintNftResult.Result != null && !mintNftResult.IsError)
                {
                    PlaceGeoSpatialNFTRequest placeGeoSpatialNFTRequest = new PlaceGeoSpatialNFTRequest()
                    {
                        OriginalOASISNFTId = mintNftResult.Result.OASISNFT.Id,
                        OriginalOASISNFTOffChainProvider = request.OffChainProvider != null ? request.OffChainProvider : new EnumValue<ProviderType>(ProviderType.None),
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

                    result.Result = CreateGeoSpatialNFT(placeGeoSpatialNFTRequest, mintNftResult.Result.OASISNFT);
                    OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateGeoSpatialNFTMetaDataHolon(result.Result), request.MintedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider.Value);

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

        public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in MintAndPlaceGeoNFT in NFTManager. Reason:";

            try
            {
                OASISResult<INFTTransactionRespone> mintNftResult = MintNft(CreateMintNFTTransactionRequest(request), true);

                if (mintNftResult != null && mintNftResult.Result != null && !mintNftResult.IsError)
                {
                    PlaceGeoSpatialNFTRequest placeGeoSpatialNFTRequest = new PlaceGeoSpatialNFTRequest()
                    {
                        OriginalOASISNFTId = mintNftResult.Result.OASISNFT.Id,
                        OriginalOASISNFTOffChainProvider = request.OffChainProvider,
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

                    result.Result = CreateGeoSpatialNFT(placeGeoSpatialNFTRequest, mintNftResult.Result.OASISNFT);
                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateGeoSpatialNFTMetaDataHolon(result.Result), request.MintedByAvatarId, true, true, 0, true, false, request.OffChainProvider.Value);

                    if (saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the OffChainProvider {Enum.GetName(typeof(ProviderType), request.OffChainProvider.Value)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = FormatSuccessMessage(request, result, responseFormatType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured minting the GEONFT in function MintNft. Reason: {mintNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISNFT>>> SearchNFTsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IOASISNFT>> result = new OASISResult<IEnumerable<IOASISNFT>>();
            result = DecodeNFTMetaData(await Data.SearchHolonsAsync(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public OASISResult<IEnumerable<IOASISNFT>> SearchNFTs(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFT>> result = new OASISResult<IEnumerable<IOASISNFT>>();
            string errorMessage = "Error occured in SearchNFTs in NFTManager. Reason:";
            result = DecodeNFTMetaData(Data.SearchHolons(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> SearchGeoNFTsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in SearchGeoNFTsAsync in NFTManager. Reason:";
            result = DecodeGeoNFTMetaData(await Data.SearchHolonsAsync(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }
        public async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> SearchGeoNFTs(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in SearchGeoNFT in NFTManager. Reason:";
            result = DecodeGeoNFTMetaData(Data.SearchHolons(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISNFTCollection>>> SearchNFTCollectionsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTCollectionsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IOASISNFTCollection>> result = new OASISResult<IEnumerable<IOASISNFTCollection>>();
            OASISResult<IEnumerable<OASISNFTCollection>> collectionResults = await Data.SearchHolonsAsync<OASISNFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.NFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public OASISResult<IEnumerable<IOASISNFTCollection>> SearchNFTCollections(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTCollections in NFTManager. Reason:";
            OASISResult<IEnumerable<IOASISNFTCollection>> result = new OASISResult<IEnumerable<IOASISNFTCollection>>();
            OASISResult<IEnumerable<OASISNFTCollection>> collectionResults = Data.SearchHolons<OASISNFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.NFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISGeoNFTCollection>>> SearchGeoNFTCollectionsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchGeoNFTCollectionsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IOASISGeoNFTCollection>> result = new OASISResult<IEnumerable<IOASISGeoNFTCollection>>();
            OASISResult<IEnumerable<OASISGeoNFTCollection>> collectionResults = await Data.SearchHolonsAsync<OASISGeoNFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.GeoNFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public OASISResult<IEnumerable<IOASISGeoNFTCollection>> SearchGeoNFTCollections(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchGeoNFTCollections in NFTManager. Reason:";
            OASISResult<IEnumerable<IOASISGeoNFTCollection>> result = new OASISResult<IEnumerable<IOASISGeoNFTCollection>>();
            OASISResult<IEnumerable<OASISGeoNFTCollection>> collectionResults = Data.SearchHolons<OASISGeoNFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.GeoNFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public OASISResult<IOASISNFTProvider> GetNFTProvider(ProviderType providerType, string errorMessage = "")
        {
            OASISResult<IOASISNFTProvider> result = new OASISResult<IOASISNFTProvider>();
            IOASISProvider OASISProvider = ProviderManager.Instance.GetProvider(providerType);

            if (OASISProvider != null)
            {
                if (!OASISProvider.IsProviderActivated)
                {
                    OASISResult<bool> activateProviderResult = OASISProvider.ActivateProvider();

                    if (activateProviderResult.IsError)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured activating provider. Reason: {activateProviderResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), providerType)} provider was not found.");

            if (!result.IsError)
            {
                result.Result = OASISProvider as IOASISNFTProvider;

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), providerType)} provider is not a valid OASISNFTProvider.");
            }

            return result;
        }

        
        public async Task<OASISResult<IOASISNFTCollection>> CreateOASISNFTCollectionAsync(ICreateOASISNFTCollectionRequest createOASISNFTCollectionRequest, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFTCollection> result = new OASISResult<IOASISNFTCollection>();
            string errorMessage = "Error occured in CreateOASISNFTCollectionAsync in NFTManager. Reason:";

            OASISNFTCollection OASISNFTCollection = new OASISNFTCollection()
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
                OASISNFTs = createOASISNFTCollectionRequest.OASISNFTs,
                OASISNFTIds = createOASISNFTCollectionRequest.OASISNFTIds,
                Tags = createOASISNFTCollectionRequest.Tags
            };

            if (createOASISNFTCollectionRequest.OASISNFTIds == null)
                createOASISNFTCollectionRequest.OASISNFTIds = new List<string>();

            if (createOASISNFTCollectionRequest.OASISNFTs != null)
            {
                foreach (IOASISNFT oasisNft in createOASISNFTCollectionRequest.OASISNFTs)
                {
                    if (!OASISNFTCollection.OASISNFTIds.Contains(oasisNft.Id.ToString()))
                        OASISNFTCollection.OASISNFTIds.Add(oasisNft.Id.ToString());
                }
            }

            //TODO: Not sure if we should store the entire NFTs in the collection or just their IDs?
            createOASISNFTCollectionRequest.OASISNFTs = null;

            OASISResult<OASISNFTCollection> saveResult = await OASISNFTCollection.SaveAsync<OASISNFTCollection>();

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
            //    { "OASISNFTCOLLECTION.OASISNFTIds", createOASISNFTCollectionRequest.OASISNFTIds  },
            //    { "OASISNFTCOLLECTION.Tags", createOASISNFTCollectionRequest.Tags },
            //    { "OASISNFTCOLLECTION.MetaData", createOASISNFTCollectionRequest.MetaData }
            //};

            //OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(new Holon()
            //{
            //    Id = Guid.Parse(metaData["OASISNFTCOLLECTION.ID"].ToString()),
            //    Name = $"OASIS NFT Collection with title {createOASISNFTCollectionRequest.Title}",
            //    Description = createOASISNFTCollectionRequest.Description,
            //    HolonType = HolonType.NFTCollection,
            //    MetaData = metaData
            //}, providerType : providerType);

            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
            {
                result.Result = OASISNFTCollection;
                result.Message = "OASIS NFT Collection created successfully.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS NFT Collection holon. Reason: {saveResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOASISGeoNFTCollection>> CreateOASISGeoNFTCollectionAsyc(ICreateOASISGeoNFTCollectionRequest createOASISGeoNFTCollectionRequest, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new OASISResult<IOASISGeoNFTCollection>();
            string errorMessage = "Error occured in CreateOASISGeoNFTCollectionAsyc in NFTManager. Reason:";

            OASISGeoNFTCollection OASISGeoNFTCollection = new OASISGeoNFTCollection()
            {
                Name = createOASISGeoNFTCollectionRequest.Title,
                Description = createOASISGeoNFTCollectionRequest.Description,
                CreatedDate = DateTime.Now,
                CreatedByAvatarId = createOASISGeoNFTCollectionRequest.CreatedBy,
                Image = createOASISGeoNFTCollectionRequest.Image,
                ImageUrl = createOASISGeoNFTCollectionRequest.ImageUrl,
                Thumbnail = createOASISGeoNFTCollectionRequest.Thumbnail,
                ThumbnailUrl = createOASISGeoNFTCollectionRequest.ThumbnailUrl,
                MetaData = createOASISGeoNFTCollectionRequest.MetaData,
                OASISGeoNFTs = createOASISGeoNFTCollectionRequest.OASISGeoNFTs,
                OASISGeoNFTIds = createOASISGeoNFTCollectionRequest.OASISGeoNFTIds,
                Tags = createOASISGeoNFTCollectionRequest.Tags
            };

            if (createOASISGeoNFTCollectionRequest.OASISGeoNFTIds == null)
                createOASISGeoNFTCollectionRequest.OASISGeoNFTIds = new List<string>();

            if (createOASISGeoNFTCollectionRequest.OASISGeoNFTIds != null)
            {
                foreach (IOASISGeoSpatialNFT geoNFT in createOASISGeoNFTCollectionRequest.OASISGeoNFTs)
                {
                    if (!OASISGeoNFTCollection.OASISGeoNFTIds.Contains(geoNFT.Id.ToString()))
                        OASISGeoNFTCollection.OASISGeoNFTIds.Add(geoNFT.Id.ToString());
                }
            }

            OASISResult<OASISGeoNFTCollection> saveResult = await OASISGeoNFTCollection.SaveAsync<OASISGeoNFTCollection>();

            //Dictionary<string, object> metaData = new Dictionary<string, object>()
            //{
            //    { "OASISGEONFTCOLLECTION.ID", Guid.NewGuid() },
            //    { "OASISGEONFTCOLLECTION.Title", createOASISGeoNFTCollectionRequest.Title },
            //    { "OASISGEONFTCOLLECTION.Description", createOASISGeoNFTCollectionRequest.Description  },
            //    { "OASISGEONFTCOLLECTION.CreatedDate", OASISGeoNFTCollection.CreatedDate  },
            //    { "OASISGEONFTCOLLECTION.CreatedBy", createOASISGeoNFTCollectionRequest.CreatedBy  },
            //    { "OASISGEONFTCOLLECTION.ImageUrl", createOASISGeoNFTCollectionRequest.ImageUrl  },
            //    { "OASISGEONFTCOLLECTION.Image", createOASISGeoNFTCollectionRequest.Image  },
            //    { "OASISGEONFTCOLLECTION.ThumbnailUrl", createOASISGeoNFTCollectionRequest.ThumbnailUrl  },
            //    { "OASISGEONFTCOLLECTION.Thumbnail", createOASISGeoNFTCollectionRequest.Thumbnail  },
            //    { "OASISGEONFTCOLLECTION.OASISGeoNFTIds", createOASISGeoNFTCollectionRequest.OASISGeoNFTIds  },
            //    { "OASISGEONFTCOLLECTION.Tags", createOASISGeoNFTCollectionRequest.Tags },
            //    { "OASISGEONFTCOLLECTION.MetaData", createOASISGeoNFTCollectionRequest.MetaData }
            //};

            //OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(new Holon()
            //{
            //    Id = Guid.Parse(metaData["OASISGEONFTCOLLECTION.ID"].ToString()),
            //    Name = $"OASIS GeoNFT Collection with title {createOASISGeoNFTCollectionRequest.Title}",
            //    Description = createOASISGeoNFTCollectionRequest.Description,
            //    HolonType = HolonType.NFTCollection,
            //    MetaData = metaData
            //}, providerType: providerType);

            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
            {
                result.Result = OASISGeoNFTCollection;
                result.Message = "OASIS GeoNFT Collection created successfully.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS GeoNFT Collection holon. Reason: {saveResult.Message}");


            return result;
        }


        public async Task<OASISResult<IOASISNFTCollection>> UpdateOASISNFTCollectionAsync(IUpdateOASISNFTCollectionRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFTCollection> result = new();
            string errorMessage = "Error occured in UpdateOASISNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<OASISNFTCollection> holonResult = await Data.LoadHolonAsync<OASISNFTCollection>(request.Id, providerType: providerType);

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
                    holonResult.Result.OASISNFTIds = request.OASISNFTIds ?? holonResult.Result.OASISNFTIds;
                    holonResult.Result.Tags = request.Tags ?? holonResult.Result.Tags;

                    OASISResult<OASISNFTCollection> saveResult = await Data.SaveHolonAsync<OASISNFTCollection>(holonResult.Result);

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

        public async Task<OASISResult<IOASISGeoNFTCollection>> UpdateOASISGeoNFTCollectionAsync(IUpdateOASISGeoNFTCollectionRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new();
            string errorMessage = "Error occured in UpdateOASISGeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<OASISGeoNFTCollection> holonResult = await Data.LoadHolonAsync<OASISGeoNFTCollection>(request.Id, providerType: providerType);

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
                    //    { "OASISGEONFTCOLLECTION.OASISGeoNFTIds", request.OASISGeoNFTIds ?? new List<string>() },
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
                    // holonResult.Result.OASISGeoNFTIds = request.OASISGeoNFTIds ?? holonResult.Result.OASISGeoNFTIds;
                    holonResult.Result.Tags = request.Tags ?? holonResult.Result.Tags;

                    OASISResult<OASISGeoNFTCollection> saveResult = await Data.SaveHolonAsync<OASISGeoNFTCollection>(holonResult.Result);

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        //IOASISGeoNFTCollection coll = new OASISGeoNFTCollection()
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
                        //    OASISGeoNFTIds = request.OASISGeoNFTIds ?? new List<string>(),
                        //    OASISGeoNFTs = request.OASISGeoNFTs,
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

        public async Task<OASISResult<IOASISNFTCollection>> AddOASISNFTToCollectionAsync(Guid collectionId, Guid OASISNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFTCollection> result = new();
            string errorMessage = "Error occured in AddOASISNFTToCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<OASISNFTCollection> holonResult = await Data.LoadHolonAsync<OASISNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    holonResult.Result.OASISNFTIds.Add(OASISNFTId.ToString());

                    OASISResult<OASISNFTCollection> saveResult = await Data.SaveHolonAsync<OASISNFTCollection>(holonResult.Result);

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        result.Result = saveResult.Result;
                        result.Message = "OASIS NFT Added To Collection Successfully.";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured adding OASIS NFT to collection. Reason: {saveResult?.Message}");
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
        public async Task<OASISResult<IOASISNFTCollection>> AddOASISNFTToCollectionAsync(Guid collectionId, IOASISNFT OASISNFT, ProviderType providerType = ProviderType.Default)
        {
            return await AddOASISNFTToCollectionAsync(collectionId, OASISNFT.Id, providerType);
        }

        public async Task<OASISResult<IOASISNFTCollection>> RemoveOASISNFTFromCollectionAsync(Guid collectionId, Guid OASISNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFTCollection> result = new();
            string errorMessage = "Error occured in RemoveOASISNFTFromCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<OASISNFTCollection> holonResult = await Data.LoadHolonAsync<OASISNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    holonResult.Result.OASISNFTIds.Remove(OASISNFTId.ToString());

                    OASISResult<OASISNFTCollection> saveResult = await Data.SaveHolonAsync<OASISNFTCollection>(holonResult.Result);

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        result.Result = saveResult.Result;
                        result.Message = "OASIS NFT Removed From Collection Successfully.";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured removing OASIS NFT from collection. Reason: {saveResult?.Message}");
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

        public async Task<OASISResult<IOASISNFTCollection>> RemoveOASISNFTFromCollectionAsync(Guid collectionId, IOASISNFT OASISNFT, ProviderType providerType = ProviderType.Default)
        {
            return await RemoveOASISNFTFromCollectionAsync(collectionId, OASISNFT.Id, providerType);
        }

        public async Task<OASISResult<IOASISGeoNFTCollection>> AddOASISGeoNFTToCollectionAsync(Guid collectionId, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new();
            string errorMessage = "Error occured in AddOASISGeoNFTToCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<OASISGeoNFTCollection> holonResult = await Data.LoadHolonAsync<OASISGeoNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    holonResult.Result.OASISGeoNFTIds.Add(OASISGeoNFTId.ToString());

                    OASISResult<OASISGeoNFTCollection> saveResult = await Data.SaveHolonAsync<OASISGeoNFTCollection>(holonResult.Result);

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        result.Result = saveResult.Result;
                        result.Message = "OASIS GeoNFT Added To Collection Successfully.";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured adding OASIS GeoNFT to collection. Reason: {saveResult?.Message}");
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

        public async Task<OASISResult<IOASISGeoNFTCollection>> AddOASISGeoNFTToCollectionAsync(Guid collectionId, IOASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            return await AddOASISGeoNFTToCollectionAsync(collectionId, OASISGeoNFT.Id, providerType);
        }

        public async Task<OASISResult<IOASISGeoNFTCollection>> RemoveOASISGeoNFTFromCollectionAsync(Guid collectionId, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new();
            string errorMessage = "Error occured in RemoveOASISGeoNFTFromCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<OASISGeoNFTCollection> holonResult = await Data.LoadHolonAsync<OASISGeoNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    holonResult.Result.OASISGeoNFTIds.Remove(OASISGeoNFTId.ToString());

                    OASISResult<OASISGeoNFTCollection> saveResult = await Data.SaveHolonAsync<OASISGeoNFTCollection>(holonResult.Result);

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        result.Result = saveResult.Result;
                        result.Message = "OASIS GeoNFT Removed From Collection Successfully.";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured removing OASIS GeoNFT from collection. Reason: {saveResult?.Message}");
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

        public async Task<OASISResult<IOASISGeoNFTCollection>> RemoveOASISGeoNFTFromCollectionAsync(Guid collectionId, IOASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            return await RemoveOASISGeoNFTFromCollectionAsync(collectionId, OASISGeoNFT.Id, providerType);
        }

        public async Task<OASISResult<bool>> DeleteOASISNFTCollectionAsync(Guid avatarId, Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteOASISNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IHolon> del = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (del != null && !del.IsError && del.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting collection. Reason: {del?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteOASISGeoNFTCollectionAsync(Guid avatarId, Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteOASISGeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IHolon> del = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType);

                if (del != null && !del.IsError && del.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting collection. Reason: {del?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IOASISNFTCollection>> LoadOASISNFTCollectionAsync(Guid id, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFTCollection> result = new();
            string errorMessage = "Error occured in LoadOASISNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<OASISNFTCollection> holonRes = await Data.LoadHolonAsync<OASISNFTCollection>(id, providerType: providerType);
                
                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                   if (holonRes.Result.OASISNFTs == null && loadChildNFTs && holonRes.Result.OASISNFTIds != null && holonRes.Result.OASISNFTIds.Count > 0)
                   {
                        holonRes.Result.OASISNFTs = new List<IOASISNFT>();

                        foreach (string nftId in holonRes.Result.OASISNFTIds)
                        {
                            OASISResult<IOASISNFT> nftRes = await LoadNftAsync(Guid.Parse(nftId), providerType: providerType);
                            
                            if (nftRes != null && !nftRes.IsError && nftRes.Result != null)
                                holonRes.Result.OASISNFTs.Add(nftRes.Result);
                        }
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

        public async Task<OASISResult<IOASISGeoNFTCollection>> LoadOASISGeoNFTCollectionAsync(Guid id, bool loadChildGeoNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new();
            string errorMessage = "Error occured in LoadOASISGeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<OASISGeoNFTCollection> holonRes = await Data.LoadHolonAsync<OASISGeoNFTCollection>(id, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (holonRes.Result.OASISGeoNFTs == null && loadChildGeoNFTs && holonRes.Result.OASISGeoNFTIds != null && holonRes.Result.OASISGeoNFTIds.Count > 0)
                    {
                        holonRes.Result.OASISGeoNFTs = new List<IOASISGeoSpatialNFT>();

                        foreach (string nftId in holonRes.Result.OASISGeoNFTIds)
                        {
                            OASISResult<IOASISGeoSpatialNFT> nftRes = await LoadGeoNftAsync(Guid.Parse(nftId), providerType: providerType);

                            if (nftRes != null && !nftRes.IsError && nftRes.Result != null)
                                holonRes.Result.OASISGeoNFTs.Add(nftRes.Result);
                        }
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

        public async Task<OASISResult<IEnumerable<IOASISNFTCollection>>> LoadAllNFTCollectionsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadAllNFTCollectionsAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<OASISNFTCollection>> holonRes = await Data.LoadAllHolonsAsync<OASISNFTCollection>(HolonType.NFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                    result.Result = holonRes.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISNFTCollection>>> LoadNFTCollectionsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadNFTCollectionsForAvatarAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<OASISNFTCollection>> holonRes = await Data.LoadHolonsForParentAsync<OASISNFTCollection>(avatarId, HolonType.NFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                    result.Result = holonRes.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISGeoNFTCollection>>> LoadAllGeoNFTCollectionsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadAllGeoNFTCollectionsAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<OASISGeoNFTCollection>> holonRes = await Data.LoadAllHolonsAsync<OASISGeoNFTCollection>(HolonType.GeoNFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                    result.Result = holonRes.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISGeoNFTCollection>>> LoadGeoNFTCollectionsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadGeoNFTCollectionsForAvatarAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<OASISGeoNFTCollection>> holonRes = await Data.LoadHolonsForParentAsync<OASISGeoNFTCollection>(avatarId, HolonType.NFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                    result.Result = holonRes.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }


        /// <summary>
        /// Mint multiple NFTs in a single batch operation for improved efficiency
        /// </summary>
        public async Task<OASISResult<List<INFTTransactionRespone>>> MintNFTBatchAsync(List<IMintNFTTransactionRequest> requests, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            var result = new OASISResult<List<INFTTransactionRespone>>();
            
            try
            {
                if (requests == null || !requests.Any())
                {
                    OASISErrorHandling.HandleError(ref result, "No NFT mint requests provided");
                    return result;
                }

                CLIEngine.ShowWorkingMessage($"Starting batch minting of {requests.Count} NFTs...");
                
                var batchResults = new List<INFTTransactionRespone>();
                var successfulMints = 0;
                var failedMints = 0;
                
                // Process NFTs in parallel batches for optimal performance
                var batchSize = Math.Min(10, requests.Count); // Process up to 10 NFTs concurrently
                var batches = requests.Chunk(batchSize);
                
                foreach (var batch in batches)
                {
                    var batchTasks = batch.Select(async request =>
                    {
                        try
                        {
                            var mintResult = await MintNFTAsync(request, responseFormatType);
                            if (mintResult.IsError)
                            {
                                Interlocked.Increment(ref failedMints);
                                return new NFTTransactionRespone
                                {
                                    IsError = true,
                                    Message = mintResult.Message,
                                    TransactionHash = string.Empty,
                                    NFTTokenAddress = string.Empty
                                };
                            }
                            else
                            {
                                Interlocked.Increment(ref successfulMints);
                                return mintResult.Result;
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref failedMints);
                            return new NFTTransactionRespone
                            {
                                IsError = true,
                                Message = $"Error minting NFT: {ex.Message}",
                                TransactionHash = string.Empty,
                                NFTTokenAddress = string.Empty
                            };
                        }
                    });
                    
                    var batchResults = await Task.WhenAll(batchTasks);
                    batchResults.AddRange(batchResults);
                    
                    // Brief pause between batches to prevent overwhelming the network
                    await Task.Delay(100);
                }
                
                result.Result = batchResults;
                result.IsError = false;
                result.Message = $"Batch minting completed: {successfulMints} successful, {failedMints} failed";
                
                CLIEngine.ShowSuccessMessage($"Batch minting completed: {successfulMints} successful, {failedMints} failed");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error during batch minting: {ex.Message}", ex);
            }
            
            return result;
        }
        private async Task<OASISResult<INFTTransactionRespone>> MintNFTInternalAsync(IMintNFTTransactionRequest request, NFTStandardType NFTStandardType, EnumValue<ProviderType> metaDataProviderType, OASISResult<IOASISNFTProvider> nftProviderResult, OASISResult<INFTTransactionRespone> result, string errorMessage, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IHolon> jsonSaveResult = null;

            //Need to save the image to the off-chain provider first to get the URL to pass into the onchain provider.
            if (request.Image != null)
            {
                switch (request.NFTOffChainMetaType.Value)
                {
                    case NFTOffChainMetaType.Pinata:
                        {
                            Guid imageId = Guid.NewGuid();
                            OASISResult<string> pinataResult = await Pinata.UploadFileToPinataAsync(request.Image, imageId.ToString());

                            if (pinataResult != null && pinataResult.Result != null && !pinataResult.IsError)
                                request.ImageUrl = Pinata.GetFileUrl(pinataResult.Result);
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
                            OASISResult<IFileSystemNode> ipfsResult = await IPFS.SaveStreamAsync(new MemoryStream(request.Image), imageId.ToString());

                            if (ipfsResult != null && ipfsResult.Result != null && !ipfsResult.IsError)
                                request.ImageUrl = IPFS.GetFileUrl(ipfsResult.Result.Id.ToString());
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
                                    { "data",  request.Image }
                                }
                            }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                            if (imageSaveResult != null && imageSaveResult.Result != null && !imageSaveResult.IsError)
                                request.ImageUrl = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/getdata/", imageSaveResult.Result.Id);

                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to the OASIS and offchain provider {request.OffChainProvider.Name}. Reason: {imageSaveResult.Message}");
                        }
                        break;
                }
            }

            if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                string json = request.JSONMetaData;

                if (string.IsNullOrEmpty(json))
                    json = CreateMetaDataJson(request, NFTStandardType);

                switch (request.NFTOffChainMetaType.Value)
                {
                    case NFTOffChainMetaType.Pinata:
                        {
                            Guid imageId = Guid.NewGuid();
                            OASISResult<string> pinataResult = await Pinata.UploadJsonToPinataAsync(json);

                            if (pinataResult != null && pinataResult.Result != null && !pinataResult.IsError)
                                request.JSONMetaDataURL = Pinata.GetFileUrl(pinataResult.Result);
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
                                request.JSONMetaDataURL = IPFS.GetFileUrl(ipfsResult.Result.Id.ToString());
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to IPFS. Reason: {ipfsResult.Message}");
                                return result;
                            }
                        }
                        break;

                    case NFTOffChainMetaType.OASIS:
                        {
                            jsonSaveResult = await SaveJSONMetaDataToOASISAsync(request, metaDataProviderType, json);

                            if (jsonSaveResult != null && jsonSaveResult.Result != null && !jsonSaveResult.IsError)
                                request.JSONMetaDataURL = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/load-file/", jsonSaveResult.Result.Id);
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to the OASIS and offchain provider {request.OffChainProvider.Name}. Reason: {jsonSaveResult.Message}");
                                return result;
                            }
                        }
                        break;

                    case NFTOffChainMetaType.ExternalJSONURL:
                        {
                            if (string.IsNullOrEmpty(request.JSONMetaDataURL))
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

                do
                {
                    result = await nftProviderResult.Result.MintNFTAsync(request);

                    if (result != null && result.Result != null && !result.IsError)
                        break;

                    else if (!request.WaitTillNFTMinted)
                    {
                        result.Result.OASISNFT.MintTransactionHash = $"Error occured attempting to mint NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToMintInSeconds}) exceeded, try increasing and trying again!";
                        break;
                    }

                    //TODO: May cause issues in the non-async version because will block the calling thread! Need to look into this and find better way if needed...
                    Thread.Sleep(request.AttemptToMintEveryXSeconds * 1000);

                    if (startTime.AddSeconds(request.WaitForNFTToMintInSeconds) > DateTime.Now)
                        break;

                } while (attemptingToMint);

                CLIEngine.SupressConsoleLogging = false;

                if (result != null && !result.IsError && result.Result != null)
                {
                    result.Result.OASISNFT = CreateOASISNFT(request, result.Result);

                    //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                    if (metaDataProviderType.Value == ProviderType.None)
                        metaDataProviderType.Value = ProviderType.MongoDBOASIS;

                    if (jsonSaveResult != null)
                    {
                        result.Result.OASISNFT.JSONMetaDataURLHolonId = jsonSaveResult.Result.Id;
                        result.Result.OASISNFT.JSONMetaData = jsonSaveResult.Result.MetaData["data"].ToString();
                    }

                    OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateNFTMetaDataHolon(result.Result.OASISNFT, request), request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                    if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                    {
                        if (!string.IsNullOrEmpty(request.SendToAddressAfterMinting))
                        {
                            bool attemptingToSend = true;
                            startTime = DateTime.Now;
                            CLIEngine.SupressConsoleLogging = true;

                            do
                            {
                                OASISResult<INFTTransactionRespone> sendResult = await nftProviderResult.Result.SendNFTAsync(new NFTWalletTransactionRequest()
                                {
                                    FromWalletAddress = result.Result.OASISNFT.OASISMintWalletAddress,
                                    ToWalletAddress = request.SendToAddressAfterMinting,
                                    TokenAddress = result.Result.OASISNFT.NFTTokenAddress,
                                    FromProvider = request.OnChainProvider,
                                    ToProvider = request.OnChainProvider,
                                    Amount = 1,
                                    MemoText = $"Sending NFT from OASIS Wallet {result.Result.OASISNFT.OASISMintWalletAddress} to {request.SendToAddressAfterMinting} on chain {request.OnChainProvider.Name}.",
                                });

                                if (sendResult != null && sendResult.Result != null && !sendResult.IsError)
                                {
                                    result.Result.OASISNFT.SendNFTTransactionHash = sendResult.Result.TransactionResult;
                                    result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
                                    break;
                                }
                                else if (!request.WaitTillNFTSent)
                                {
                                    result.Result.OASISNFT.SendNFTTransactionHash = $"Error occured attempting to send NFT & WaitTillNFTSent is false. Reason: {sendResult.Message}";
                                    result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
                                    break;
                                }

                                Thread.Sleep(request.AttemptToSendEveryXSeconds * 1000);

                                if (startTime.AddSeconds(request.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
                                {
                                    result.Result.OASISNFT.SendNFTTransactionHash = $"Error occured attempting to send NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!";
                                    result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
                                    break;
                                }

                            } while (attemptingToSend);

                            CLIEngine.SupressConsoleLogging = false;
                        }
                    }
                    else
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {metaDataProviderType.Name} {Enum.GetName(typeof(ProviderType), metaDataProviderType.Value)}. Reason: {saveHolonResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured minting the OASISNFT: Reason: {result.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The ImageUrl is null!");

            return result;
        }

        private OASISResult<INFTTransactionRespone> MintNFTInternal(IMintNFTTransactionRequest request, NFTStandardType NFTStandardType, EnumValue<ProviderType> metaDataProviderType, OASISResult<IOASISNFTProvider> nftProviderResult, OASISResult<INFTTransactionRespone> result, string errorMessage, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IHolon> jsonSaveResult = null;

            //Need to save the image to the off-chain provider first to get the URL to pass into the onchain provider.
            if (request.Image != null)
            {
                switch (request.NFTOffChainMetaType.Value)
                {
                    case NFTOffChainMetaType.Pinata:
                        {
                            Guid imageId = Guid.NewGuid();
                            OASISResult<string> pinataResult = Pinata.UploadFileToPinataAsync(request.Image, imageId.ToString()).Result;

                            if (pinataResult != null && pinataResult.Result != null && !pinataResult.IsError)
                                request.ImageUrl = Pinata.GetFileUrl(pinataResult.Result);
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
                            OASISResult<IFileSystemNode> ipfsResult = IPFS.SaveStreamAsync(new MemoryStream(request.Image), imageId.ToString()).Result;

                            if (ipfsResult != null && ipfsResult.Result != null && !ipfsResult.IsError)
                                request.ImageUrl = IPFS.GetFileUrl(ipfsResult.Result.Id.ToString());
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to IPFS. Reason: {ipfsResult.Message}");
                                return result;
                            }
                        }
                        break;

                    case NFTOffChainMetaType.OASIS:
                        {
                            OASISResult<IHolon> imageSaveResult = Data.SaveHolon(new Holon()
                            {
                                MetaData = new Dictionary<string, object>()
                                {
                                    { "data",  request.Image }
                                }
                            }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                            if (imageSaveResult != null && imageSaveResult.Result != null && !imageSaveResult.IsError)
                                request.ImageUrl = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/getdata/", imageSaveResult.Result.Id);

                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to the OASIS and offchain provider {request.OffChainProvider.Name}. Reason: {imageSaveResult.Message}");
                        }
                        break;
                }
            }

            if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                string json = request.JSONMetaData;

                if (string.IsNullOrEmpty(json))
                    json = CreateMetaDataJson(request, NFTStandardType);

                switch (request.NFTOffChainMetaType.Value)
                {
                    case NFTOffChainMetaType.Pinata:
                        {
                            Guid imageId = Guid.NewGuid();
                            OASISResult<string> pinataResult = Pinata.UploadJsonToPinataAsync(json).Result;

                            if (pinataResult != null && pinataResult.Result != null && !pinataResult.IsError)
                                request.JSONMetaDataURL = Pinata.GetFileUrl(pinataResult.Result);
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
                            OASISResult<IFileSystemNode> ipfsResult = IPFS.SaveTextAsync(json).Result;

                            if (ipfsResult != null && ipfsResult.Result != null && !ipfsResult.IsError)
                                request.JSONMetaDataURL = IPFS.GetFileUrl(ipfsResult.Result.Id.ToString());
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to IPFS. Reason: {ipfsResult.Message}");
                                return result;
                            }
                        }
                        break;

                    case NFTOffChainMetaType.OASIS:
                        {
                            jsonSaveResult = SaveJSONMetaDataToOASIS(request, metaDataProviderType, json);

                            if (jsonSaveResult != null && jsonSaveResult.Result != null && !jsonSaveResult.IsError)
                                request.JSONMetaDataURL = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/load-file/", jsonSaveResult.Result.Id);
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to the OASIS and offchain provider {request.OffChainProvider.Name}. Reason: {jsonSaveResult.Message}");
                                return result;
                            }
                        }
                        break;

                    case NFTOffChainMetaType.ExternalJSONURL:
                        {
                            if (string.IsNullOrEmpty(request.JSONMetaDataURL))
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} When setting NFTOffChainMetaType to ExternalJsonURL, the JSONUrl cannot be empty!");
                                return result;
                            }
                            break;
                        }
                }

                bool attemptingToMint = true;
                DateTime startTime = DateTime.Now;

                do
                {
                    result = nftProviderResult.Result.MintNFT(request);

                    if (result != null && result.Result != null && !result.IsError)
                        break;

                    else if (!request.WaitTillNFTMinted)
                    {
                        result.Result.OASISNFT.MintTransactionHash = $"Error occured attempting to mint NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToMintInSeconds}) exceeded, try increasing and trying again!";
                        break;
                    }

                    //TODO: May cause issues in the non-async version because will block the calling thread! Need to look into this and find better way if needed...
                    Thread.Sleep(request.AttemptToMintEveryXSeconds * 1000);

                    if (startTime.AddSeconds(request.WaitForNFTToMintInSeconds) > DateTime.Now)
                        break;

                } while (attemptingToMint);

                if (result != null && !result.IsError && result.Result != null)
                {
                    result.Result.OASISNFT = CreateOASISNFT(request, result.Result);

                    //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                    if (metaDataProviderType.Value == ProviderType.None)
                        metaDataProviderType.Value = ProviderType.MongoDBOASIS;

                    if (jsonSaveResult != null)
                    {
                        result.Result.OASISNFT.JSONMetaDataURLHolonId = jsonSaveResult.Result.Id;
                        result.Result.OASISNFT.JSONMetaData = jsonSaveResult.Result.MetaData["data"].ToString();
                    }

                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateNFTMetaDataHolon(result.Result.OASISNFT, request), request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                    if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                    {
                        if (!string.IsNullOrEmpty(request.SendToAddressAfterMinting))
                        {
                            bool attemptingToSend = true;
                            startTime = DateTime.Now;

                            do
                            {
                                OASISResult<INFTTransactionRespone> sendResult = nftProviderResult.Result.SendNFT(new NFTWalletTransactionRequest()
                                {
                                    FromWalletAddress = result.Result.OASISNFT.OASISMintWalletAddress,
                                    ToWalletAddress = request.SendToAddressAfterMinting,
                                    TokenAddress = result.Result.OASISNFT.NFTTokenAddress,
                                    FromProvider = request.OnChainProvider,
                                    ToProvider = request.OnChainProvider,
                                    Amount = 1,
                                    MemoText = $"Sending NFT from OASIS Wallet {result.Result.OASISNFT.OASISMintWalletAddress} to {request.SendToAddressAfterMinting} on chain {request.OnChainProvider.Name}.",
                                });

                                if (sendResult != null && sendResult.Result != null && !sendResult.IsError)
                                {
                                    result.Result.OASISNFT.SendNFTTransactionHash = sendResult.Result.TransactionResult;
                                    result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
                                    break;
                                }
                                else if (!request.WaitTillNFTSent)
                                {
                                    result.Result.OASISNFT.SendNFTTransactionHash = $"Error occured attempting to send NFT & WaitTillNFTSent is false. Reason: {sendResult.Message}";
                                    result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
                                    break;
                                }

                                Thread.Sleep(request.AttemptToSendEveryXSeconds * 1000);

                                if (startTime.AddSeconds(request.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
                                {
                                    result.Result.OASISNFT.SendNFTTransactionHash = $"Error occured attempting to send NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!";
                                    result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
                                    break;
                                }

                            } while (attemptingToSend);
                        }
                    }
                    else
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {metaDataProviderType.Name} {Enum.GetName(typeof(ProviderType), metaDataProviderType.Value)}. Reason: {saveHolonResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured minting the OASISNFT: Reason: {result.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The ImageUrl is null!");

            return result;
        }

        private string FormatSuccessMessage(IMintNFTTransactionRequest request, OASISResult<INFTTransactionRespone> response, EnumValue<ProviderType> metaDataProviderType, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";
                string sendNFTMessage = GenerateSendMessage(response.Result.OASISNFT, request, "", 2);

                if (response.Result.OASISNFT.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.OASISNFT.JSONMetaDataURLHolonId, " ");

                return $"Successfully minted the NFT on the {request.OnChainProvider.Name} provider with hash {response.Result.TransactionResult} and title '{request.Title}' by AvatarId {request.MintedByAvatarId} using OASIS Minting Account {response.Result.OASISNFT.OASISMintWalletAddress} for price {request.Price}. NFT Address: {response.Result.OASISNFT.NFTTokenAddress}. The OASIS metadata is stored on the {metaDataProviderType.Name} provider with the id {response.Result.OASISNFT.Id} and JSON URL {response.Result.OASISNFT.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.OASISNFT.ImageUrl}, Mint Date: {response.Result.OASISNFT.MintedOn}. {sendNFTMessage}";
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            string message = "";
            message = string.Concat(message, $"Successfully minted the OASIS NFT!{lineBreak}");
            message = string.Concat(message, lineBreak);
            message = string.Concat(message, GenerateNFTSummary(response.Result.OASISNFT, request, lineBreak, colWidth));

            if (response.IsWarning)
                message = string.Concat(message, " Warning:".PadRight(colWidth), response.Message, lineBreak);

            return message;
        }

        private string FormatSuccessMessage(IImportWeb3NFTRequest request, OASISResult<IOASISNFT> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                return $"Successfully imported the Web3 NFT on the {request.OnChainProvider.Name} provider with NFTTokenAddress {request.NFTTokenAddress} and title '{request.Title}' by AvatarId {request.ImportedByByAvatarId}. NFT minted using wallet address: {request.NFTMintedUsingWalletAddress}. Price: {request.Price}. The OASIS metadata is stored on the {request.OnChainProvider.Name} provider with the id {response.Result.Id} and JSON URL {response.Result.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.ImageUrl}, Imported Date: {response.Result.MintedOn}.";
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            string message = "";
            message = string.Concat(message, $"Successfully imported the Web3 NFT!{lineBreak}");
            message = string.Concat(message, lineBreak);
            message = string.Concat(message, GenerateNFTSummary(response.Result, lineBreak, colWidth));

            if (response.IsWarning)
                message = string.Concat(message, " Warning:".PadRight(colWidth), response.Message, lineBreak);

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IOASISNFT> response, Guid importedByByAvatarId, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                return $"Successfully imported the OASIS NFT on the {response.Result.OnChainProvider.Name} provider with NFTTokenAddress {response.Result.NFTTokenAddress} and title '{response.Result.Title}' by AvatarId {importedByByAvatarId}. NFT minted using wallet address: {response.Result.NFTMintedUsingWalletAddress}. Price: {response.Result.Price}. The OASIS metadata is stored on the {response.Result.OnChainProvider.Name} provider with the id {response.Result.Id} and JSON URL {response.Result.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.ImageUrl}, Imported Date: {response.Result.MintedOn}.";
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            string message = "";
            message = string.Concat(message, $"Successfully imported the OASIS NFT!{lineBreak}");
            message = string.Concat(message, lineBreak);
            message = string.Concat(message, GenerateNFTSummary(response.Result, lineBreak, colWidth));

            if (response.IsWarning)
                message = string.Concat(message, " Warning:".PadRight(colWidth), response.Message, lineBreak);

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IOASISGeoSpatialNFT> response, Guid importedByByAvatarId, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                return $"Successfully imported the OASIS GeoNFT on the {response.Result.OnChainProvider.Name} provider with NFTTokenAddress {response.Result.NFTTokenAddress} and title '{response.Result.Title}' by AvatarId {importedByByAvatarId}. NFT minted using wallet address: {response.Result.NFTMintedUsingWalletAddress}. Price: {response.Result.Price}. The OASIS metadata is stored on the {response.Result.OnChainProvider.Name} provider with the id {response.Result.Id} and JSON URL {response.Result.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.ImageUrl}, Imported Date: {response.Result.MintedOn}.";
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            string message = "";
            message = string.Concat(message, $"Successfully imported the OASIS GeoNFT!{lineBreak}");
            message = string.Concat(message, lineBreak);
            message = string.Concat(message, GenerateNFTSummary(response.Result, lineBreak, colWidth));

            if (response.IsWarning)
                message = string.Concat(message, " Warning:".PadRight(colWidth), response.Message, lineBreak);

            return message;
        }

        private string FormatSuccessMessage(IMintNFTTransactionRequest request, OASISResult<IOASISGeoSpatialNFT> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = 40)
        {
            string lineBreak = "\n";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";
                string sendNFTMessage = GenerateSendMessage(response.Result, request, "", 2);

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                return $"Successfully minted and placed the new OASIS GeoNFT. The meta data is stored on the GeoNFTMetaDataProvider {response.Result.GeoNFTMetaDataProvider.Name} with id {response.Result.Id} and was placed by the avatar with id {response.Result.PlacedByAvatarId}. The NFT was originally minted on the {response.Result.OnChainProvider.Name} onchain provider with hash {response.Result.MintTransactionHash} and title '{response.Result.Title}' by the avatar with id {response.Result.MintedByAvatarId} for the price of {response.Result.Price} using OASIS Minting Account {response.Result.OASISMintWalletAddress}. NFT Address: {response.Result.NFTTokenAddress}. The OASIS metadata for the original NFT is stored on the {response.Result.OffChainProvider.Name} offchain provider with the id {response.Result.OriginalOASISNFTId} and JSON URL {response.Result.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.ImageUrl}, Mint Date: {response.Result.MintedOn}. {sendNFTMessage}";
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            string message = "";
            message = string.Concat(message, $"Successfully minted & placed the OASIS Geo-NFT!{lineBreak}");
            message = string.Concat(message, lineBreak);
            message = string.Concat(message, GenerateNFTSummary(response.Result, request, lineBreak, colWidth));
            message = string.Concat(message, GenerateGeoNFTSummary(response.Result, lineBreak, colWidth));

            if (response.IsWarning)
                message = string.Concat(message, " Warning:".PadRight(colWidth), response.Message, lineBreak);

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IOASISGeoSpatialNFT> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = 40)
        {
            string lineBreak = "\n";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                return $"Successfully created and placed the new OASIS GeoNFT. The meta data is stored on the GeoNFTMetaDataProvider {response.Result.GeoNFTMetaDataProvider.Name} with id {response.Result.Id} and was placed by the avatar with id {response.Result.PlacedByAvatarId}. The NFT was originally minted on the {response.Result.OnChainProvider.Name} onchain provider with hash {response.Result.MintTransactionHash} and title '{response.Result.Title}' by the avatar with id {response.Result.MintedByAvatarId} for the price of {response.Result.Price} using OASIS Minting Account {response.Result.OASISMintWalletAddress}. NFT Address: {response.Result.NFTTokenAddress}. The OASIS metadata for the original NFT is stored on the {response.Result.OffChainProvider.Name} offchain provider with the id {response.Result.OriginalOASISNFTId} and JSON URL {response.Result.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.ImageUrl}, Mint Date: {response.Result.MintedOn}.";
                //return $"Successfully created and placed the new OASIS GeoNFT. The meta data is stored on the GeoNFTMetaDataProvider {response.Result.GeoNFTMetaDataProvider.Name} with id {response.Result.Id} and was placed by the avatar with id {response.Result.PlacedByAvatarId}. The NFT was originally minted on the {response.Result.OnChainProvider.Name} onchain provider with hash {response.Result.Hash} and title '{response.Result.Title}' by the avatar with id {response.Result.MintedByAvatarId} for the price of {response.Result.Price}. The OASIS metadata for the original NFT is stored on the {response.Result.OffChainProvider.Name} offchain provider with the id {response.Result.OriginalOASISNFTId}.";
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            string message = "";
            message = string.Concat(message, $"Successfully created & placed the OASIS Geo-NFT!{lineBreak}");
            message = string.Concat(message, lineBreak);
            message = string.Concat(message, $"ORIGINAL NFT INFO:{lineBreak}");
            message = string.Concat(message, GenerateNFTSummary(response.Result, null, lineBreak, colWidth));
            message = string.Concat(message, lineBreak);
            message = string.Concat(message, GenerateGeoNFTSummary(response.Result, lineBreak, colWidth));

            return message;
        }

        private string FormatSuccessMessage(INFTWalletTransactionRequest request, OASISResult<INFTTransactionRespone> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
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

        private string GenerateNFTSummary(IOASISNFT OASISNFT, IMintNFTTransactionRequest request, string lineBreak, int colWidth)
        {
            string message = GenerateNFTSummary(OASISNFT, lineBreak, colWidth);

            if (request != null)
                message = string.Concat(message, " Number To Mint:".PadRight(colWidth), request.NumberToMint, lineBreak);

            message = string.Concat(message, GenerateSendMessage(OASISNFT, request, lineBreak, colWidth));
            return message;
        }

        private string GenerateNFTSummary(IOASISNFT OASISNFT, string lineBreak, int colWidth)
        {
            string message = "";
            message = string.Concat(message, " OASIS NFT Id:".PadRight(colWidth), OASISNFT.Id, lineBreak);
            message = string.Concat(message, " Onchain Provider:".PadRight(colWidth), OASISNFT.OnChainProvider.Name, lineBreak);
            message = string.Concat(message, " Offchain Provider:".PadRight(colWidth), OASISNFT.OffChainProvider.Name, lineBreak);
            message = string.Concat(message, " Mint Transaction Hash:".PadRight(colWidth), OASISNFT.MintTransactionHash, lineBreak);
            message = string.Concat(message, " Title:".PadRight(colWidth), OASISNFT.Title, lineBreak);
            message = string.Concat(message, " Description:".PadRight(colWidth), OASISNFT.Description, lineBreak);
            message = string.Concat(message, " Price:".PadRight(colWidth), OASISNFT.Price, lineBreak);
            message = string.Concat(message, " Symbol:".PadRight(colWidth), OASISNFT.Symbol, lineBreak);
            message = string.Concat(message, " NFT Standard Type:".PadRight(colWidth), OASISNFT.NFTStandardType.Name, lineBreak);
            message = string.Concat(message, " Minted By Avatar Id:".PadRight(colWidth), OASISNFT.MintedByAvatarId, lineBreak);
            message = string.Concat(message, " Minted Date:".PadRight(colWidth), OASISNFT.MintedOn, lineBreak);
            message = string.Concat(message, " OASIS Minting Account:".PadRight(colWidth), OASISNFT.OASISMintWalletAddress, lineBreak);
            message = string.Concat(message, " NFT Address:".PadRight(colWidth), OASISNFT.NFTTokenAddress, lineBreak);
            message = string.Concat(message, " Store NFT MetaData OnChain:".PadRight(colWidth), OASISNFT.StoreNFTMetaDataOnChain, lineBreak);
            message = string.Concat(message, " NFT OffChain MetaType:".PadRight(colWidth), OASISNFT.NFTOffChainMetaType.Name, lineBreak);
            message = string.Concat(message, " JSON MetaData URL:".PadRight(colWidth), OASISNFT.JSONMetaDataURL, lineBreak);

            if (OASISNFT.JSONMetaDataURLHolonId != Guid.Empty)
                message = string.Concat(message, " JSON MetaData URL Holon Id:".PadRight(colWidth), OASISNFT.JSONMetaDataURLHolonId, lineBreak);

            message = string.Concat(message, " Image URL:".PadRight(colWidth), OASISNFT.ImageUrl, lineBreak);
            message = string.Concat(message, " Thumbnail URL:".PadRight(colWidth), OASISNFT.ThumbnailUrl, lineBreak);

            return message;
        }

        private string GenerateGeoNFTSummary(IOASISGeoSpatialNFT OASISNFT, string lineBreak, int colWidth)
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

        private string GenerateSendMessage(IOASISNFT OASISNFT, IMintNFTTransactionRequest request, string lineBreak = "", int colWidth = 20)
        {
            string sendNFTMessage = "";

            if (!string.IsNullOrEmpty(OASISNFT.SendToAddressAfterMinting))
                sendNFTMessage = string.Concat(" Send To Address After Minting: ".PadRight(colWidth), OASISNFT.SendToAddressAfterMinting, $". {lineBreak}");

            if (!string.IsNullOrEmpty(OASISNFT.SendToAvatarAfterMintingId.ToString()) && OASISNFT.SendToAvatarAfterMintingId.ToString() != Guid.Empty.ToString())
                sendNFTMessage = string.Concat(sendNFTMessage, " Send To Avatar After Minting Id: ".PadRight(colWidth), OASISNFT.SendToAvatarAfterMintingId, $". {lineBreak}");

            if (!string.IsNullOrEmpty(OASISNFT.SendToAvatarAfterMintingUsername))
                sendNFTMessage = string.Concat(sendNFTMessage, " Send To Avatar After Minting Username: ".PadRight(colWidth), OASISNFT.SendToAvatarAfterMintingUsername, $". {lineBreak}");

            if (request != null)
            {
                if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingEmail))
                    sendNFTMessage = string.Concat(sendNFTMessage, " Send To Avatar After Minting Email: ".PadRight(colWidth), request.SendToAvatarAfterMintingEmail, $". {lineBreak}");
            }

            if (!string.IsNullOrEmpty(OASISNFT.SendNFTTransactionHash))
                sendNFTMessage = string.Concat(sendNFTMessage, " Send NFT Hash: ".PadRight(colWidth), OASISNFT.SendNFTTransactionHash, $". {lineBreak}");

            return sendNFTMessage;
        }

        private OASISResult<IHolon> SaveJSONMetaDataToOASIS(IMintNFTTransactionRequest request, EnumValue<ProviderType> metaDataProviderType, string json)
        {
            return Data.SaveHolon(new Holon()
            {
                MetaData = new Dictionary<string, object>()
                            {
                                { "data",  json }
                            }
            }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);
        }

        private async Task<OASISResult<IHolon>> SaveJSONMetaDataToOASISAsync(IMintNFTTransactionRequest request, EnumValue<ProviderType> metaDataProviderType, string json)
        {
            return await Data.SaveHolonAsync(new Holon()
            {
                MetaData = new Dictionary<string, object>()
                            {
                                { "data",  json }
                            }
            }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);
        }

        public string CreateMetaDataJson(IMintNFTTransactionRequest request, NFTStandardType NFTStandardType)
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

        private string CreateMetaplexJson(IMintNFTTransactionRequest request)
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

        private string CreateERC721Json(IMintNFTTransactionRequest request)
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

        private string CreateERC1155Json(IMintNFTTransactionRequest request)
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

        //private IMintNFTTransactionRequestForProvider CreateNFTTransactionRequestForProvider(IMintNFTTransactionRequest request, string jsonUrl)
        //{
        //    MintNFTTransactionRequestForProvider providerRequest = new MintNFTTransactionRequestForProvider()
        //    {
        //        Description = request.Description,
        //        Image = request.Image,
        //        Discount = request.Discount,
        //        ImageUrl = request.ImageUrl,
        //        MemoText = request.MemoText,
        //        MetaData = request.MetaData,
        //        MintedByAvatarId = request.MintedByAvatarId,
        //        //MintWalletAddress = request.MintWalletAddress,
        //        Symbol = request.Symbol,
        //        SendToAddressAfterMinting = request.SendToAddressAfterMinting,
        //        SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
        //        SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
        //        SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
        //        NFTOffChainMetaType = request.NFTOffChainMetaType,
        //        NFTStandardType = request.NFTStandardType,
        //        NumberToMint = request.NumberToMint,
        //        OffChainProvider = request.OffChainProvider,
        //        OnChainProvider = request.OnChainProvider,
        //        Price = request.Price,
        //        StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
        //        Thumbnail = request.Thumbnail,
        //        ThumbnailUrl = request.ThumbnailUrl,
        //        Title = request.Title,
        //        JSONMetaDataURL = jsonUrl
        //    };

        //    return providerRequest;
        //}

        private OASISNFT CreateOASISNFT(IMintNFTTransactionRequest request, INFTTransactionRespone mintNFTResponse)
        {
            return new OASISNFT()
            {
                Id = Guid.NewGuid(),
                MintTransactionHash = mintNFTResponse.TransactionResult,
                SellerFeeBasisPoints = mintNFTResponse.OASISNFT != null ? mintNFTResponse.OASISNFT.SellerFeeBasisPoints : 0,
                //MetaData = mintNFTResponse.OASISNFT != null ? mintNFTResponse.OASISNFT.MetaData : null,
                MetaData = request.MetaData,
                OASISMintWalletAddress = mintNFTResponse.OASISNFT != null ? mintNFTResponse.OASISNFT.OASISMintWalletAddress : null,
                UpdateAuthority = mintNFTResponse.OASISNFT != null ? mintNFTResponse.OASISNFT.UpdateAuthority : null,
                NFTTokenAddress = mintNFTResponse.OASISNFT != null ? mintNFTResponse.OASISNFT.NFTTokenAddress : null, //TODO: Need to pull this from the provider mint functions...
                MintedByAvatarId = request.MintedByAvatarId,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                SendNFTTransactionHash = mintNFTResponse.OASISNFT.SendNFTTransactionHash,
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Discount = request.Discount,
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
                JSONMetaDataURL = request.JSONMetaDataURL
                //OffChainProviderHolonId = Guid.NewGuid(),
                //Token= request.Token
            };
        }

        private OASISNFT CreateOASISNFT(IImportWeb3NFTRequest request)
        {
            return new OASISNFT()
            {
                Id = Guid.NewGuid(),
                MintTransactionHash = request.MintTransactionHash,
                MetaData = request.MetaData,
                NFTMintedUsingWalletAddress = request.NFTMintedUsingWalletAddress,
                UpdateAuthority = request.UpdateAuthority,
                NFTTokenAddress = request.NFTTokenAddress,
                ImportedByAvatarId = request.ImportedByByAvatarId,
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Discount = request.Discount,
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
                ImportedOn = DateTime.Now
            };
        }

        private OASISGeoSpatialNFT CreateGeoSpatialNFT(IPlaceGeoSpatialNFTRequest request, IOASISNFT originalNftMetaData)
        {
            return new OASISGeoSpatialNFT()
            {
                //Id = request.OASISNFTId != Guid.Empty ? request.OASISNFTId : Guid.NewGuid(),
                //Id = request.OASISNFTId,
                Id = Guid.NewGuid(),  //The NFT could be placed many times so we need a new ID for each time
                OriginalOASISNFTId = request.OriginalOASISNFTId, //We need to link back to the orignal NFT (but we copy across the NFT properties making it quicker and easier to get at the data). TODO: Do we want to copy the data across? Pros and Cons? Need to think about this... for now it's fine... ;-)
                GeoNFTMetaDataProvider = request.GeoNFTMetaDataProvider,
                //OriginalOASISNFTProviderType = request.OriginalOASISNFTOffChainProviderType,
                MintTransactionHash = originalNftMetaData.MintTransactionHash,
                JSONMetaDataURL = originalNftMetaData.JSONMetaDataURL,
                OASISMintWalletAddress = originalNftMetaData.OASISMintWalletAddress,
                MintedByAvatarId = originalNftMetaData.MintedByAvatarId,
                SendToAddressAfterMinting = originalNftMetaData.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = originalNftMetaData.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = originalNftMetaData.SendToAvatarAfterMintingUsername,
                SendNFTTransactionHash = originalNftMetaData.SendNFTTransactionHash,
                UpdateAuthority = originalNftMetaData.UpdateAuthority,
                //OffChainProviderHolonId = originalNftMetaData.OffChainProviderHolonId,
                SellerFeeBasisPoints = originalNftMetaData.SellerFeeBasisPoints,
                NFTTokenAddress = originalNftMetaData.NFTTokenAddress,
                Title = originalNftMetaData.Title,
                Description = originalNftMetaData.Description,
                Price = originalNftMetaData.Price,
                Discount = originalNftMetaData.Discount,
                Image = originalNftMetaData.Image,
                ImageUrl = originalNftMetaData.ImageUrl,
                Thumbnail = originalNftMetaData.Thumbnail,
                ThumbnailUrl = originalNftMetaData.ThumbnailUrl,
                MetaData = originalNftMetaData.MetaData,
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
                Nft2DSpriteURI = request.Nft2DSpriteURI
            };
        }

        private IHolon CreateNFTMetaDataHolon(IOASISNFT nftMetaData, IMintNFTTransactionRequest request = null)
        {
            IHolon holonNFT = new Holon(HolonType.NFT);
            //holonNFT.Id = result.Result.OASISNFT.OffChainProviderHolonId;
            holonNFT.Id = nftMetaData.Id;
            //holonNFT.CustomKey = nftMetaData.Hash;
            //holonNFT.MetaData["OnChainNFTHash"] = nftMetaData.Hash;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} NFT Minted On The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.OASISNFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData); //TODO: May remove this because its duplicated data. BUT we may need this for other purposes later such as exporting it to a file etc (but then we could just serialaize it there and then).
            holonNFT.MetaData["NFT.MintTransactionHash"] = nftMetaData.MintTransactionHash;
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.MintedByAvatarId"] = nftMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["NFT.OASISMintWalletAddress"] = nftMetaData.OASISMintWalletAddress;
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingId"] = nftMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingUsername"] = nftMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["NFT.SendToAddressAfterMinting"] = nftMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["NFT.SendNFTTransactionHash"] = nftMetaData.SendNFTTransactionHash;
            holonNFT.MetaData["NFT.NFTTokenAddress"] = nftMetaData.NFTTokenAddress;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = nftMetaData.Price.ToString();
            holonNFT.MetaData["NFT.Discount"] = nftMetaData.Discount.ToString();
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
            holonNFT.MetaData["NFT.UpdateAuthority"] = nftMetaData.UpdateAuthority;
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.MetaData);
            holonNFT.ParentHolonId = nftMetaData.MintedByAvatarId;

            return holonNFT;
        }

        private IHolon CreateNFTMetaDataHolon(IOASISNFT nftMetaData, IImportWeb3NFTRequest request)
        {
            IHolon holonNFT = new Holon(HolonType.NFT);
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} NFT Imported OnTo The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = request.Description;
            holonNFT.MetaData["NFT.OASISNFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData); //TODO: May remove this because its duplicated data. BUT we may need this for other purposes later such as exporting it to a file etc (but then we could just serialaize it there and then).
            holonNFT.MetaData["NFT.MintTransactionHash"] = request.MintTransactionHash;
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.ImportedByByAvatarId"] = request.ImportedByByAvatarId.ToString();
            holonNFT.MetaData["NFT.NFTMintedUsingWalletAddress"] = nftMetaData.NFTMintedUsingWalletAddress;
            holonNFT.MetaData["NFT.NFTTokenAddress"] = nftMetaData.NFTTokenAddress;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = request.Price.ToString();
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
            holonNFT.MetaData["NFT.UpdateAuthority"] = nftMetaData.UpdateAuthority;
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(request.MetaData);
            holonNFT.ParentHolonId = nftMetaData.ImportedByAvatarId;

            return holonNFT;
        }

        private IHolon CreateGeoSpatialNFTMetaDataHolon(IOASISGeoSpatialNFT geoNFTMetaData)
        {
            IHolon holonNFT = new Holon(HolonType.GeoNFT);
            //holonNFT.Id = result.Result.OASISNFT.OffChainProviderHolonId;
            holonNFT.Id = geoNFTMetaData.Id;
            holonNFT.Name = "OASIS GEO NFT"; // $"{Enum.GetName(typeof(ProviderType), request.OnChainProvider)} NFT Minted On The OASIS with title {request.Title}";
            holonNFT.Description = "OASIS GEO NFT";
            holonNFT.MetaData["GEONFT.OASISGEONFT"] = System.Text.Json.JsonSerializer.Serialize(geoNFTMetaData); //TODO: May remove this because its duplicated data.
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
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Id"] = geoNFTMetaData.OriginalOASISNFTId;
            //holonNFT.MetaData["GEONFT.OriginalOASISNFT.ProviderType"] = Enum.GetName(typeof(ProviderType), geoNFTMetaData.OriginalOASISNFTProviderType);
            //holonNFT.MetaData["GEONFT.OriginalOASISNFT.ProviderType"] = geoNFTMetaData.OffChainProvider.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MintTransactionHash"] = geoNFTMetaData.MintTransactionHash;
            //holonNFT.MetaData["GEONFT.OriginalOASISNFT.MemoText"] = geoNFTMetaData.MetaData["MemoText"];
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MemoText"] = geoNFTMetaData.MemoText;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Title"] = geoNFTMetaData.Title;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Description"] = geoNFTMetaData.Description;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MintedByAvatarId"] = geoNFTMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.OASISMintWalletAddress"] = geoNFTMetaData.OASISMintWalletAddress;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingId"] = geoNFTMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingUsername"] = geoNFTMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAddressAfterMinting"] = geoNFTMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendNFTTransactionHash"] = geoNFTMetaData.SendNFTTransactionHash;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.NFTTokenAddress"] = geoNFTMetaData.NFTTokenAddress;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Price"] = geoNFTMetaData.Price.ToString();
            //holonNFT.MetaData["GEONFT.NumberToMint"] = geoNFTMetaData.NumberToMint.ToString();
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
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.UpdateAuthority"] = geoNFTMetaData.UpdateAuthority;

            return holonNFT;
        }

        private IMintNFTTransactionRequest CreateMintNFTTransactionRequest(IMintAndPlaceGeoSpatialNFTRequest mintAndPlaceGeoSpatialNFTRequest)
        {
            return new MintNFTTransactionRequest()
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
                MemoText = mintAndPlaceGeoSpatialNFTRequest.MemoText,
                NumberToMint = mintAndPlaceGeoSpatialNFTRequest.NumberToMint,
                MetaData = mintAndPlaceGeoSpatialNFTRequest.MetaData,
                OffChainProvider = mintAndPlaceGeoSpatialNFTRequest.OffChainProvider,
                OnChainProvider = mintAndPlaceGeoSpatialNFTRequest.OnChainProvider,
                JSONMetaDataURL = mintAndPlaceGeoSpatialNFTRequest.JSONMetaDataURL,
                NFTOffChainMetaType = mintAndPlaceGeoSpatialNFTRequest.NFTOffChainMetaType,
                NFTStandardType = mintAndPlaceGeoSpatialNFTRequest.NFTStandardType,
                SendToAddressAfterMinting = mintAndPlaceGeoSpatialNFTRequest.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingUsername,
                StoreNFTMetaDataOnChain = mintAndPlaceGeoSpatialNFTRequest.StoreNFTMetaDataOnChain,
                Symbol = mintAndPlaceGeoSpatialNFTRequest.Symbol
            };
        }

        private OASISResult<IOASISNFT> DecodeNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IOASISNFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (IOASISNFT)System.Text.Json.JsonSerializer.Deserialize(holonResult.Result.MetaData["NFT.OASISNFT"].ToString(), typeof(OASISNFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IOASISGeoSpatialNFT> DecodeGeoNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IOASISGeoSpatialNFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (OASISGeoSpatialNFT)System.Text.Json.JsonSerializer.Deserialize(holonResult.Result.MetaData["GEONFT.OASISGEONFT"].ToString(), typeof(OASISGeoSpatialNFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IOASISNFT>> DecodeNFTMetaData(OASISResult<IEnumerable<IHolon>> holonsResult, OASISResult<IEnumerable<IOASISNFT>> result, string errorMessage)
        {
            List<IOASISNFT> nfts = new List<IOASISNFT>();

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                if (holonsResult.Result.Count() > 0)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        nfts.Add((IOASISNFT)System.Text.Json.JsonSerializer.Deserialize(holon.MetaData["NFT.OASISNFT"].ToString(), typeof(OASISNFT)));

                    result.Result = nfts;
                }
                else
                    result.Message = "No NFT's Found.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IOASISGeoSpatialNFT>> DecodeGeoNFTMetaData(OASISResult<IEnumerable<IHolon>> holonsResult, OASISResult<IEnumerable<IOASISGeoSpatialNFT>> result, string errorMessage)
        {
            List<IOASISGeoSpatialNFT> nfts = new List<IOASISGeoSpatialNFT>();

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                if (holonsResult.Result.Count() > 0)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        nfts.Add((IOASISGeoSpatialNFT)System.Text.Json.JsonSerializer.Deserialize(holon.MetaData["GEONFT.OASISGEONFT"].ToString(), typeof(OASISGeoSpatialNFT)));

                    result.Result = nfts;
                }
                else
                    result.Message = "No GeoNFT's Found.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");

            return result;
        }

        //TODO: Lots more coming soon! ;-)
    }
}