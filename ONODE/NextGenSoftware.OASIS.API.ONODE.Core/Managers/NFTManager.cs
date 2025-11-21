using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ipfs;
using Nethereum.Contracts.Standards.ERC721;
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
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
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

        public async Task<OASISResult<IWeb3NFTTransactionRespone>> SendNFTAsync(IWeb3NFTWalletTransactionRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb3NFTTransactionRespone> result = new OASISResult<IWeb3NFTTransactionRespone>();
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
                        result = await nftProviderResult.Result.SendNFTAsync(request);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Result.Web3NFT.SendNFTTransactionHash = result.Result.TransactionResult;
                            result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }
                        else if (!request.WaitTillNFTSent)
                        {
                            result.Result.Web3NFT.SendNFTTransactionHash = $"Error occured attempting to send NFT & WaitTillNFTSent is false. Reason: {result.Message}";
                            result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            result.Result.Web3NFT.SendNFTTransactionHash = $"Error occured attempting to send NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!";
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

        public OASISResult<IWeb3NFTTransactionRespone> SendNFT(IWeb3NFTWalletTransactionRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb3NFTTransactionRespone> result = new OASISResult<IWeb3NFTTransactionRespone>();
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
                        result = nftProviderResult.Result.SendNFT(request);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Result.Web3NFT.SendNFTTransactionHash = result.Result.TransactionResult;
                            result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }
                        else if (!request.WaitTillNFTSent)
                        {
                            result.Result.Web3NFT.SendNFTTransactionHash = $"Error occured attempting to send NFT & WaitTillNFTSent is false. Reason: {result.Message}";
                            result.Message = FormatSuccessMessage(request, result, responseFormatType);
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            result.Result.Web3NFT.SendNFTTransactionHash = $"Error occured attempting to send NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!";
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


        public async Task<OASISResult<IWeb4OASISNFT>> MintNftAsync(IMintWeb4NFTRequest request, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4OASISNFT> result = new OASISResult<IWeb4OASISNFT>();
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

                if (request.Web3NFTs != null && request.Web3NFTs.Count > 0)
                {
                    //foreach (IMintWeb3NFTRequest web3Request in request.Web3NFTs)
                    //{
                    //    if (web3Request.RequestId == Guid.Empty)
                    //        web3Request.RequestId = Guid.NewGuid();
                    //}

                    int i = 0;
                    foreach (IMintWeb3NFTRequest web3Request in request.Web3NFTs)
                    {
                        i++;

                        if (web3Request.NumberToMint == 0)
                            web3Request.NumberToMint = request.NumberToMint;

                        result = await MintWeb3NFTsAsync(result, request, web3Request, isGeoNFT, responseFormatType, i == request.Web3NFTs.Count);
                    }
                }
                else
                    result = await MintWeb3NFTsAsync(result, request, null, isGeoNFT, responseFormatType, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        private async Task<OASISResult<IWeb4OASISNFT>> MintWeb3NFTsAsync(OASISResult<IWeb4OASISNFT> result, IMintWeb4NFTRequest request, IMintWeb3NFTRequest web3Request = null, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, bool isLastWeb3NFT = false)
        {
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

                result = await MintNFTInternalAsync(result, request, web3Request, NFTMetaDataProviderType, nftProviderResult, responseFormatType, isLastWeb3NFT);
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $"Error occured minting web3 NFT in MintWeb3NFTsAsync. Error occured calling GetNFTProvider. Reason: {nftProviderResult.Message}");
                //result.Result = null;
                //result.Message = nftProviderResult.Message;
                //result.IsError = true;
            }

            return result;
        }

        //public OASISResult<IWeb4NFTTransactionRespone> MintNft(IMintWeb4NFTRequest request, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        //{
        //    OASISResult<IWeb4NFTTransactionRespone> result = new OASISResult<IWeb4NFTTransactionRespone>();
        //    string errorMessage = "Error occured in MintNft in NFTManager. Reason:";
        //    IAvatar currentAvatar = null;

        //    try
        //    {
        //        OASISResult<bool> nftStandardValid = IsNFTStandardTypeValid(request, errorMessage);

        //        if (nftStandardValid != null && nftStandardValid.IsError)
        //        {
        //            result.IsError = true;
        //            result.Message = nftStandardValid.Message;
        //            return result;
        //        }

        //        if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingEmail))
        //        {
        //            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(request.SendToAvatarAfterMintingEmail);

        //            if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
        //                request.SendToAvatarAfterMintingId = avatarResult.Result.Id;
        //            else
        //            {
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to load the avatar details for the SendToAvatarAfterMintingEmail {request.SendToAvatarAfterMintingEmail}. The email is likely not valid. Reason: {avatarResult.Message}");
        //                return result;
        //            }
        //        }

        //        if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingUsername))
        //        {
        //            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(request.SendToAvatarAfterMintingUsername);

        //            if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
        //                request.SendToAvatarAfterMintingId = avatarResult.Result.Id;
        //            else
        //            {
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to load the avatar details for the SendToAvatarAfterMintingUsername {request.SendToAvatarAfterMintingEmail}. The username is likely not valid. Reason: {avatarResult.Message}");
        //                return result;
        //            }
        //        }

        //        if (string.IsNullOrEmpty(request.SendToAddressAfterMinting) && request.SendToAvatarAfterMintingId == Guid.Empty)
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} You need to specify at least one of the following: SendToAddressAfterMinting, SendToAvatarAfterMintingId, SendToAvatarAfterMintingUsername or SendToAvatarAfterMintingEmail.");
        //            return result;
        //        }

        //        //If the wallet Address hasn't been set then set it now by looking up the relevant wallet address for this avatar and provider type.
        //        if (string.IsNullOrEmpty(request.SendToAddressAfterMinting) && request.SendToAvatarAfterMintingId != Guid.Empty)
        //        {
        //            if (currentAvatar == null)
        //            {
        //                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(request.MintedByAvatarId);

        //                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
        //                {
        //                    currentAvatar = avatarResult.Result;

        //                    foreach (ProviderType providerType in currentAvatar.ProviderWallets.Keys)
        //                    {
        //                        if (providerType == request.OnChainProvider.Value)
        //                        {
        //                            request.SendToAddressAfterMinting = currentAvatar.ProviderWallets[request.OnChainProvider.Value][0].WalletAddress;
        //                            break;
        //                        }
        //                    }

        //                    if (string.IsNullOrEmpty(request.SendToAddressAfterMinting))
        //                    {
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for avatar {request.MintedByAvatarId} and provider {request.OnChainProvider.Value}. Please make sure you link a valid wallet to the avatar using the Wallet API or Key API.");
        //                        return result;
        //                    }
        //                }
        //                else
        //                {
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to load the avatar details for the SendToAvatarAfterMinting {request.MintedByAvatarId}. Reason: {avatarResult.Message}");
        //                    return result;
        //                }
        //            }
        //        }

        //        if (string.IsNullOrEmpty(request.SendToAddressAfterMinting))
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} SendToAddressAfterMinting is null! Please make sure a valid SendToAddressAfterMinting is set or a valid SendToAvatarAfterMinting.");
        //            return result;
        //        }

        //        OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.OnChainProvider.Value);

        //        if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
        //        {
        //            string geoNFTMemoText = "";

        //            if (isGeoNFT)
        //                geoNFTMemoText = "Geo";

        //            request.MemoText = $"{request.OnChainProvider.Name} {geoNFTMemoText}NFT minted on The OASIS with title '{request.Title}' by avatar with id {request.MintedByAvatarId} for the price of {request.Price}. {request.MemoText}";

        //            if (request.OffChainProvider == null)
        //                request.OffChainProvider = new EnumValue<ProviderType>(ProviderType.None);

        //            EnumValue<ProviderType> NFTMetaDataProviderType;

        //            if (request.StoreNFTMetaDataOnChain)
        //                NFTMetaDataProviderType = request.OnChainProvider;
        //            else
        //                NFTMetaDataProviderType = request.OffChainProvider;

        //            if (string.IsNullOrEmpty(request.Symbol))
        //            {
        //                if (isGeoNFT)
        //                    request.Symbol = "GEONFT";
        //                else
        //                    request.Symbol = "OASISNFT";
        //            }

        //            result = MintNFTInternal(request, request.NFTStandardType.Value, NFTMetaDataProviderType, nftProviderResult, result, errorMessage, responseFormatType);
        //        }
        //        else
        //        {
        //            result.Result = null;
        //            result.Message = nftProviderResult.Message;
        //            result.IsError = true;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IWeb4OASISNFT>> ImportWeb3NFTAsync(IImportWeb3NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4OASISNFT> result = new OASISResult<IWeb4OASISNFT>();
            string errorMessage = "Error occured in ImportWeb3NFT in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                result.Result = CreateWeb4NFT(request);

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

        public async Task<OASISResult<IWeb4OASISNFT>> ImportWeb3NFT(IImportWeb3NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4OASISNFT> result = new OASISResult<IWeb4OASISNFT>();
            string errorMessage = "Error occured in ImportWeb3NFT in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                result.Result = CreateWeb4NFT(request);

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

        public async Task<OASISResult<IWeb4OASISNFT>> ImportOASISNFTAsync(Guid importedByAvatarId, string fullPathToOASISNFTJsonFile, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            return await ImportOASISNFTAsync(importedByAvatarId, JsonConvert.DeserializeObject<IWeb4OASISNFT>(await File.ReadAllTextAsync(fullPathToOASISNFTJsonFile)));
        }

        public async Task<OASISResult<IWeb4OASISNFT>> ImportOASISNFTAsync(Guid importedByAvatarId, IWeb4OASISNFT OASISNFT, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4OASISNFT> result = new OASISResult<IWeb4OASISNFT>();
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

        public async Task<OASISResult<IWeb4OASISNFT>> ExportOASISNFTAsync(Guid OASISNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4OASISNFT> exportResult = await LoadNftAsync(OASISNFTId, providerType);

            if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
            {
                return await ExportOASISNFTAsync(exportResult.Result, fullPathToExportTo, providerType, responseFormatType);
            }
            else
                return exportResult;
        }

        public async Task<OASISResult<IWeb4OASISNFT>> ExportOASISNFTAsync(IWeb4OASISNFT OASISNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            await File.WriteAllTextAsync(fullPathToExportTo, JsonConvert.SerializeObject(OASISNFT, Formatting.Indented));
            return new OASISResult<IWeb4OASISNFT>(OASISNFT);
        }

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ImportOASISGeoNFTAsync(Guid importedByAvatarId, string fullPathToOASISGeoNFTJsonFile, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            return await ImportOASISGeoNFTAsync(importedByAvatarId, JsonConvert.DeserializeObject<IWeb4OASISGeoSpatialNFT>(await File.ReadAllTextAsync(fullPathToOASISGeoNFTJsonFile)));
        }

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ImportOASISGeoNFTAsync(Guid importedByAvatarId, IWeb4OASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
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

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ExportOASISGeoNFTAsync(Guid OASISGeoNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> exportResult = await LoadGeoNftAsync(OASISGeoNFTId, providerType);

            if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
                return await ExportOASISGeoNFTAsync(exportResult.Result, fullPathToExportTo, providerType, responseFormatType);
            else
                return exportResult;
        }

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ExportOASISGeoNFTAsync(IWeb4OASISGeoSpatialNFT OASISGeoNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            await File.WriteAllTextAsync(fullPathToExportTo, JsonConvert.SerializeObject(OASISGeoNFT, Formatting.Indented));
            return new OASISResult<IWeb4OASISGeoSpatialNFT>(OASISGeoNFT);
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

        public async Task<OASISResult<IWeb4OASISNFT>> LoadNftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFT> result = new OASISResult<IWeb4OASISNFT>();
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

        public OASISResult<IWeb4OASISNFT> LoadNft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFT> result = new OASISResult<IWeb4OASISNFT>();
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

        public async Task<OASISResult<IWeb4OASISNFT>> LoadNftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFT> result = new OASISResult<IWeb4OASISNFT>();
            string errorMessage = "Error occured in LoadNftAsync in NFTManager. Reason:";

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

        public OASISResult<IWeb4OASISNFT> LoadNft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFT> result = new OASISResult<IWeb4OASISNFT>();
            string errorMessage = "Error occured in LoadNft in NFTManager. Reason:";

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

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> LoadGeoNftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
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

        public OASISResult<IWeb4OASISGeoSpatialNFT> LoadGeoNft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
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

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> LoadGeoNftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            string errorMessage = "Error occured in LoadGeoNftAsync in NFTManager. Reason:";

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

        public OASISResult<IWeb4OASISGeoSpatialNFT> LoadGeoNft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            string errorMessage = "Error occured in LoadGeoNft in NFTManager. Reason:";

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

        public async Task<OASISResult<IEnumerable<IWeb4OASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISNFT>> result = new OASISResult<IEnumerable<IWeb4OASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForAvatarAsync in NFTManager. Reason:";

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

        public OASISResult<IEnumerable<IWeb4OASISNFT>> LoadAllNFTsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISNFT>> result = new OASISResult<IEnumerable<IWeb4OASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForAvatar in NFTManager. Reason:";

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

        public async Task<OASISResult<IEnumerable<IWeb4OASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISNFT>> result = new OASISResult<IEnumerable<IWeb4OASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForMintAddressAsync in NFTManager. Reason:";

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

        public OASISResult<IEnumerable<IWeb4OASISNFT>> LoadAllNFTsForMintAddress(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISNFT>> result = new OASISResult<IEnumerable<IWeb4OASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForMintAddress in NFTManager. Reason:";

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

        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForAvatarAsync in NFTManager. Reason:";

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

        public OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForAvatar in NFTManager. Reason:";

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

        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForMintAddressAsync in NFTManager. Reason:";

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

        public OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForMintAddress in NFTManager. Reason:";

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

        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarLocationAsync(long latLocation, long longLocation, int radius, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>();
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
                    OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> geoNfts = await LoadAllGeoNFTsAsync(providerType);

                    if (geoNfts != null && !geoNfts.IsError && geoNfts.Result != null)
                    {
                        List<IWeb4OASISGeoSpatialNFT> matchedGeoNFTs = new List<IWeb4OASISGeoSpatialNFT>();

                        foreach (IWeb4OASISGeoSpatialNFT geoSpatialNFT in geoNfts.Result)
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

        public OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatarLocation(long latLocation, long longLocation, int radius, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>();
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
                    OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> geoNfts = LoadAllGeoNFTs(providerType);

                    if (geoNfts != null && !geoNfts.IsError && geoNfts.Result != null)
                    {
                        List<IWeb4OASISGeoSpatialNFT> matchedGeoNFTs = new List<IWeb4OASISGeoSpatialNFT>();

                        foreach (IWeb4OASISGeoSpatialNFT geoSpatialNFT in geoNfts.Result)
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

        public async Task<OASISResult<IEnumerable<IWeb4OASISNFT>>> LoadAllNFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISNFT>> result = new OASISResult<IEnumerable<IWeb4OASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsAsync in NFTManager. Reason:";

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

        public OASISResult<IEnumerable<IWeb4OASISNFT>> LoadAllNFTs(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISNFT>> result = new OASISResult<IEnumerable<IWeb4OASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTs in NFTManager. Reason:";

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

        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsAsync in NFTManager. Reason:";

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

        public OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> LoadAllGeoNFTs(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTs in NFTManager. Reason:";

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

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            string errorMessage = "Error occured in PlaceGeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IWeb4OASISNFT> loadNftResult = await LoadNftAsync(request.OriginalWeb4OASISNFTId, request.OriginalWeb4OASISNFTOffChainProvider.Value);

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
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading original OASIS NFT with id {request.OriginalWeb4OASISNFTId}. Reason: {loadNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb4OASISGeoSpatialNFT> PlaceGeoNFT(IPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            string errorMessage = "Error occured in PlaceGeoNFT in NFTManager. Reason:";

            try
            {
                OASISResult<IWeb4OASISNFT> loadNftResult = LoadNft(request.OriginalWeb4OASISNFTId, request.OriginalWeb4OASISNFTOffChainProvider.Value);

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
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading original OASIS NFT with id {request.OriginalWeb4OASISNFTId}. Reason: {loadNftResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            string errorMessage = "Error occured in MintAndPlaceGeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IWeb4OASISNFT> mintNftResult = await MintNftAsync(CreateMintNFTTransactionRequest(request), true);

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

                    result.Result = CreateGeoSpatialNFT(placeGeoSpatialNFTRequest, mintNftResult.Result);
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

        //TODO: Put back in when MintNft is created! :)
        //public OASISResult<IWeb4OASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        //{
        //    OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
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

        public async Task<OASISResult<IWeb4OASISNFT>> UpdateNFTAsync(IUpdateWeb4NFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFT> result = new();
            string errorMessage = "Error occured in UpdateNFTAsync in NFTManager. Reason:";

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
                    OASISResult<IWeb4OASISNFT> nftResult = DecodeNFTMetaData(nftHolonResult, result, errorMessage);

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

                        OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(UpdateNFTMetaDataHolon(nftHolonResult.Result, nftResult.Result), request.ModifiedByAvatarId, providerType: providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(nftResult, result);
                            result.Result = nftResult.Result;
                            result.Message = "OASIS NFT Updated Successfully.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS NFT. Reason: {saveHolonResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS NFT. Reason: {nftResult?.Message}");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS NFT Holon. Reason: {nftHolonResult?.Message}");
                    return result;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> UpdateGeoNFTAsync(IUpdateWeb4GeoNFTRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new();
            string errorMessage = "Error occured in UpdateGeoNFTAsync in NFTManager. Reason:";

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
                    OASISResult<IWeb4OASISGeoSpatialNFT> nftResult = DecodeGeoNFTMetaData(nftHolonResult, result, errorMessage);

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

                        OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(UpdateGeoNFTMetaDataHolon(nftHolonResult.Result, nftResult.Result), request.ModifiedByAvatarId, providerType: providerType);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(nftResult, result);
                            result.Result = nftResult.Result;
                            result.Message = "OASIS Geo-NFT Updated Successfully.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS Geo-NFT. Reason: {saveHolonResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS Geo-NFT. Reason: {nftResult?.Message}");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS Geo-NFT Holon. Reason: {nftHolonResult?.Message}");
                    return result;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteNFTAsync(Guid avatarId, Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteNFTAsync in NFTManager. Reason:";

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
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting NFT. Reason: {del?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteGeoNFTAsync(Guid avatarId, Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteGeoNFTAsync in NFTManager. Reason:";

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
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting Geo-NFT. Reason: {del?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4OASISNFT>>> SearchNFTsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4OASISNFT>> result = new OASISResult<IEnumerable<IWeb4OASISNFT>>();
            result = DecodeNFTMetaData(await Data.SearchHolonsAsync(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public OASISResult<IEnumerable<IWeb4OASISNFT>> SearchNFTs(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISNFT>> result = new OASISResult<IEnumerable<IWeb4OASISNFT>>();
            string errorMessage = "Error occured in SearchNFTs in NFTManager. Reason:";
            result = DecodeNFTMetaData(Data.SearchHolons(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> SearchGeoNFTsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in SearchGeoNFTsAsync in NFTManager. Reason:";
            result = DecodeGeoNFTMetaData(await Data.SearchHolonsAsync(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }
        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> SearchGeoNFTs(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in SearchGeoNFT in NFTManager. Reason:";
            result = DecodeGeoNFTMetaData(Data.SearchHolons(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4OASISNFTCollection>>> SearchNFTCollectionsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTCollectionsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4OASISNFTCollection>> result = new OASISResult<IEnumerable<IWeb4OASISNFTCollection>>();
            OASISResult<IEnumerable<Web4OASISNFTCollection>> collectionResults = await Data.SearchHolonsAsync<Web4OASISNFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4NFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public OASISResult<IEnumerable<IWeb4OASISNFTCollection>> SearchNFTCollections(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTCollections in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4OASISNFTCollection>> result = new OASISResult<IEnumerable<IWeb4OASISNFTCollection>>();
            OASISResult<IEnumerable<Web4OASISNFTCollection>> collectionResults = Data.SearchHolons<Web4OASISNFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4NFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>> SearchGeoNFTCollectionsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchGeoNFTCollectionsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> result = new OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>();
            OASISResult<IEnumerable<Web4OASISGeoNFTCollection>> collectionResults = await Data.SearchHolonsAsync<Web4OASISGeoNFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> SearchGeoNFTCollections(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchGeoNFTCollections in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> result = new OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>();
            OASISResult<IEnumerable<Web4OASISGeoNFTCollection>> collectionResults = Data.SearchHolons<Web4OASISGeoNFTCollection>(searchTerm, avatarId, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
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


        public async Task<OASISResult<IWeb4OASISNFTCollection>> CreateNFTCollectionAsync(ICreateWeb4NFTCollectionRequest createOASISNFTCollectionRequest, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new OASISResult<IWeb4OASISNFTCollection>();
            string errorMessage = "Error occured in CreateNFTCollectionAsync in NFTManager. Reason:";

            Web4OASISNFTCollection OASISNFTCollection = new Web4OASISNFTCollection()
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
                Web4OASISNFTs = createOASISNFTCollectionRequest.Web4OASISNFTs,
                Web4OASISNFTIds = createOASISNFTCollectionRequest.Web4OASISNFTIds,
                Tags = createOASISNFTCollectionRequest.Tags
            };

            if (createOASISNFTCollectionRequest.Web4OASISNFTIds == null)
                createOASISNFTCollectionRequest.Web4OASISNFTIds = new List<string>();

            if (createOASISNFTCollectionRequest.Web4OASISNFTs != null)
            {
                foreach (IWeb4OASISNFT oasisNft in createOASISNFTCollectionRequest.Web4OASISNFTs)
                {
                    if (!OASISNFTCollection.Web4OASISNFTIds.Contains(oasisNft.Id.ToString()))
                        OASISNFTCollection.Web4OASISNFTIds.Add(oasisNft.Id.ToString());
                }
            }

            //TODO: Not sure if we should store the entire NFTs in the collection or just their IDs?
            List<IWeb4OASISNFT> nfts = OASISNFTCollection.Web4OASISNFTs;
            OASISNFTCollection.Web4OASISNFTs = null;

            OASISResult<Web4OASISNFTCollection> saveResult = await OASISNFTCollection.SaveAsync<Web4OASISNFTCollection>();

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
            //    { "OASISNFTCOLLECTION.Web4OASISNFTIds", createOASISNFTCollectionRequest.Web4OASISNFTIds  },
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
                OASISNFTCollection.Web4OASISNFTs = nfts;
                result.Result = OASISNFTCollection;
                result.Message = "OASIS NFT Collection created successfully.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS NFT Collection holon. Reason: {saveResult.Message}");

            return result;
        }

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> CreateGeoNFTCollectionAsyc(ICreateWeb4GeoNFTCollectionRequest createWeb4OASISGeoNFTCollectionRequest, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new OASISResult<IWeb4OASISGeoNFTCollection>();
            string errorMessage = "Error occured in CreateGeoNFTCollectionAsyc in NFTManager. Reason:";

            Web4OASISGeoNFTCollection Web4OASISGeoNFTCollection = new Web4OASISGeoNFTCollection()
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
                Web4OASISGeoNFTs = createWeb4OASISGeoNFTCollectionRequest.Web4OASISGeoNFTs,
                Web4OASISGeoNFTIds = createWeb4OASISGeoNFTCollectionRequest.Web4OASISGeoNFTIds,
                Tags = createWeb4OASISGeoNFTCollectionRequest.Tags
            };

            if (createWeb4OASISGeoNFTCollectionRequest.Web4OASISGeoNFTIds == null)
                createWeb4OASISGeoNFTCollectionRequest.Web4OASISGeoNFTIds = new List<string>();

            if (createWeb4OASISGeoNFTCollectionRequest.Web4OASISGeoNFTIds != null)
            {
                foreach (IWeb4OASISGeoSpatialNFT geoNFT in createWeb4OASISGeoNFTCollectionRequest.Web4OASISGeoNFTs)
                {
                    if (!Web4OASISGeoNFTCollection.Web4OASISGeoNFTIds.Contains(geoNFT.Id.ToString()))
                        Web4OASISGeoNFTCollection.Web4OASISGeoNFTIds.Add(geoNFT.Id.ToString());
                }
            }

            //TODO: Not sure if we should store the entire NFTs in the collection or just their IDs?
            List<IWeb4OASISGeoSpatialNFT> nfts = Web4OASISGeoNFTCollection.Web4OASISGeoNFTs;
            Web4OASISGeoNFTCollection.Web4OASISGeoNFTs = null;
            OASISResult<Web4OASISGeoNFTCollection> saveResult = await Web4OASISGeoNFTCollection.SaveAsync<Web4OASISGeoNFTCollection>();

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
            //    { "OASISGEONFTCOLLECTION.Web4OASISGeoNFTIds", createWeb4OASISGeoNFTCollectionRequest.Web4OASISGeoNFTIds  },
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
                Web4OASISGeoNFTCollection.Web4OASISGeoNFTs = nfts;
                result.Result = Web4OASISGeoNFTCollection;
                result.Message = "OASIS GeoNFT Collection created successfully.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS GeoNFT Collection holon. Reason: {saveResult.Message}");


            return result;
        }


        public async Task<OASISResult<IWeb4OASISNFTCollection>> UpdateNFTCollectionAsync(IUpdateWeb4NFTCollectionRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new();
            string errorMessage = "Error occured in UpdateNFCollectionAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<Web4OASISNFTCollection> holonResult = await Data.LoadHolonAsync<Web4OASISNFTCollection>(request.Id, providerType: providerType);

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
                    //holonResult.Result.Web4OASISNFTIds = request.Web4OASISNFTIds ?? holonResult.Result.Web4OASISNFTIds;
                    holonResult.Result.Tags = request.Tags ?? holonResult.Result.Tags;

                    OASISResult<Web4OASISNFTCollection> saveResult = await Data.SaveHolonAsync<Web4OASISNFTCollection>(holonResult.Result);

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

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> UpdateGeoNFTCollectionAsync(IUpdateWeb4OASISGeoNFTCollectionRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new();
            string errorMessage = "Error occured in UpdateGeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<Web4OASISGeoNFTCollection> holonResult = await Data.LoadHolonAsync<Web4OASISGeoNFTCollection>(request.Id, providerType: providerType);

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
                    //    { "OASISGEONFTCOLLECTION.Web4OASISGeoNFTIds", request.Web4OASISGeoNFTIds ?? new List<string>() },
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
                    // holonResult.Result.Web4OASISGeoNFTIds = request.Web4OASISGeoNFTIds ?? holonResult.Result.Web4OASISGeoNFTIds;
                    holonResult.Result.Tags = request.Tags ?? holonResult.Result.Tags;

                    OASISResult<Web4OASISGeoNFTCollection> saveResult = await Data.SaveHolonAsync<Web4OASISGeoNFTCollection>(holonResult.Result);

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
                        //    Web4OASISGeoNFTIds = request.Web4OASISGeoNFTIds ?? new List<string>(),
                        //    Web4OASISGeoNFTs = request.Web4OASISGeoNFTs,
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

        public async Task<OASISResult<IWeb4OASISNFTCollection>> AddNFTToCollectionAsync(Guid collectionId, Guid OASISNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new();
            string errorMessage = "Error occured in AddNFTToCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4OASISNFTCollection> holonResult = await Data.LoadHolonAsync<Web4OASISNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (!holonResult.Result.Web4OASISNFTIds.Contains(OASISNFTId.ToString()))
                    {
                        holonResult.Result.Web4OASISNFTIds.Add(OASISNFTId.ToString());

                        OASISResult<Web4OASISNFTCollection> saveResult = await Data.SaveHolonAsync<Web4OASISNFTCollection>(holonResult.Result);

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
        public async Task<OASISResult<IWeb4OASISNFTCollection>> AddNFTToCollectionAsync(Guid collectionId, IWeb4OASISNFT OASISNFT, ProviderType providerType = ProviderType.Default)
        {
            return await AddNFTToCollectionAsync(collectionId, OASISNFT.Id, providerType);
        }

        public async Task<OASISResult<IWeb4OASISNFTCollection>> RemoveNFTFromCollectionAsync(Guid collectionId, Guid OASISNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new();
            string errorMessage = "Error occured in RemoveNFTFromCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4OASISNFTCollection> holonResult = await Data.LoadHolonAsync<Web4OASISNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (holonResult.Result.Web4OASISNFTIds.Contains(OASISNFTId.ToString()))
                    {
                        holonResult.Result.Web4OASISNFTIds.Remove(OASISNFTId.ToString());

                        OASISResult<Web4OASISNFTCollection> saveResult = await Data.SaveHolonAsync<Web4OASISNFTCollection>(holonResult.Result);

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

        public async Task<OASISResult<IWeb4OASISNFTCollection>> RemoveNFTFromCollectionAsync(Guid collectionId, IWeb4OASISNFT OASISNFT, ProviderType providerType = ProviderType.Default)
        {
            return await RemoveNFTFromCollectionAsync(collectionId, OASISNFT.Id, providerType);
        }

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> AddGeoNFTToCollectionAsync(Guid collectionId, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new();
            string errorMessage = "Error occured in AddOASISGeoNFTToCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4OASISGeoNFTCollection> holonResult = await Data.LoadHolonAsync<Web4OASISGeoNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (!holonResult.Result.Web4OASISGeoNFTIds.Contains(OASISGeoNFTId.ToString()))
                    {
                        holonResult.Result.Web4OASISGeoNFTIds.Add(OASISGeoNFTId.ToString());

                        OASISResult<Web4OASISGeoNFTCollection> saveResult = await Data.SaveHolonAsync<Web4OASISGeoNFTCollection>(holonResult.Result);

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

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> AddGeoNFTToCollectionAsync(Guid collectionId, IWeb4OASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            return await AddGeoNFTToCollectionAsync(collectionId, OASISGeoNFT.Id, providerType);
        }

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> RemoveGeoNFTFromCollectionAsync(Guid collectionId, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new();
            string errorMessage = "Error occured in RemoveGeoNFTFromCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4OASISGeoNFTCollection> holonResult = await Data.LoadHolonAsync<Web4OASISGeoNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (holonResult.Result.Web4OASISGeoNFTIds.Contains(OASISGeoNFTId.ToString()))
                    {
                        holonResult.Result.Web4OASISGeoNFTIds.Remove(OASISGeoNFTId.ToString());

                        OASISResult<Web4OASISGeoNFTCollection> saveResult = await Data.SaveHolonAsync<Web4OASISGeoNFTCollection>(holonResult.Result);

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

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> RemoveGeoNFTFromCollectionAsync(Guid collectionId, IWeb4OASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            return await RemoveGeoNFTFromCollectionAsync(collectionId, OASISGeoNFT.Id, providerType);
        }

        public async Task<OASISResult<bool>> DeleteNFTCollectionAsync(Guid avatarId, Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteNFTCollectionAsync in NFTManager. Reason:";

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

        public async Task<OASISResult<bool>> DeleteGeoNFTCollectionAsync(Guid avatarId, Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteGeoNFTCollectionAsync in NFTManager. Reason:";

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

        public async Task<OASISResult<IList<IWeb4OASISNFT>>> LoadChildNFTsForNFTCollectionAsync(List<string> Web4OASISNFTIds, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IList<IWeb4OASISNFT>> result = new OASISResult<IList<IWeb4OASISNFT>>();

            if (Web4OASISNFTIds != null && Web4OASISNFTIds.Count > 0)
            {
                result.Result = new List<IWeb4OASISNFT>();

                foreach (string nftId in Web4OASISNFTIds)
                {
                    OASISResult<IWeb4OASISNFT> nftRes = await LoadNftAsync(Guid.Parse(nftId), providerType: providerType);

                    if (nftRes != null && !nftRes.IsError && nftRes.Result != null)
                        result.Result.Add(nftRes.Result);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadChildNFTsForNFTCollection loading child nft for id {nftId}. Reason: {nftRes.Message}");
                }
            }

            if (result.ErrorCount > 0)
                result.Message = $"Error(s) occured in LoadChildNFTsForNFTCollection loading child nfts. Reason(s): {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

            return result;
        }

        public async Task<OASISResult<IList<IWeb4OASISGeoSpatialNFT>>> LoadChildGeoNFTsForNFTCollectionAsync(List<string> Web4OASISGeoNFTIds, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IList<IWeb4OASISGeoSpatialNFT>> result = new OASISResult<IList<IWeb4OASISGeoSpatialNFT>>();

            if (Web4OASISGeoNFTIds != null && Web4OASISGeoNFTIds.Count > 0)
            {
                result.Result = new List<IWeb4OASISGeoSpatialNFT>();

                foreach (string nftId in Web4OASISGeoNFTIds)
                {
                    OASISResult<IWeb4OASISGeoSpatialNFT> nftRes = await LoadGeoNftAsync(Guid.Parse(nftId), providerType: providerType);

                    if (nftRes != null && !nftRes.IsError && nftRes.Result != null)
                        result.Result.Add(nftRes.Result);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadChildGeoNFTsForNFTCollection loading child nft for id {nftId}. Reason: {nftRes.Message}");
                }
            }

            if (result.ErrorCount > 0)
                result.Message = $"Error(s) occured in LoadChildGeoNFTsForNFTCollection loading child nfts. Reason(s): {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

            return result;
        }

        public async Task<OASISResult<IWeb4OASISNFTCollection>> LoadNFTCollectionAsync(Guid id, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new();
            string errorMessage = "Error occured in LoadNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4OASISNFTCollection> holonRes = await Data.LoadHolonAsync<Web4OASISNFTCollection>(id, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs && holonRes.Result.Web4OASISNFTIds != null && holonRes.Result.Web4OASISNFTIds.Count > 0)
                    {
                        OASISResult<IList<IWeb4OASISNFT>> childrenResult = await LoadChildNFTsForNFTCollectionAsync(holonRes.Result.Web4OASISNFTIds, providerType);

                        if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                            holonRes.Result.Web4OASISNFTs = childrenResult.Result.ToList();
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

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> LoadGeoNFTCollectionAsync(Guid id, bool loadChildGeoNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new();
            string errorMessage = "Error occured in LoadGeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4OASISGeoNFTCollection> holonRes = await Data.LoadHolonAsync<Web4OASISGeoNFTCollection>(id, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildGeoNFTs && holonRes.Result.Web4OASISGeoNFTIds != null && holonRes.Result.Web4OASISGeoNFTIds.Count > 0)
                    {
                        OASISResult<IList<IWeb4OASISGeoSpatialNFT>> childrenResult = await LoadChildGeoNFTsForNFTCollectionAsync(holonRes.Result.Web4OASISGeoNFTIds, providerType);

                        if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                            holonRes.Result.Web4OASISGeoNFTs = childrenResult.Result.ToList();
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

        public async Task<OASISResult<IEnumerable<IWeb4OASISNFTCollection>>> LoadAllNFTCollectionsAsync(bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadAllNFTCollectionsAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4OASISNFTCollection>> holonRes = await Data.LoadAllHolonsAsync<Web4OASISNFTCollection>(HolonType.Web4NFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4OASISNFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4OASISNFT>> childrenResult = await LoadChildNFTsForNFTCollectionAsync(collection.Web4OASISNFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4OASISNFTs = childrenResult.Result.ToList();
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

        public async Task<OASISResult<IEnumerable<IWeb4OASISNFTCollection>>> LoadNFTCollectionsForAvatarAsync(Guid avatarId, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadNFTCollectionsForAvatarAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4OASISNFTCollection>> holonRes = await Data.LoadHolonsForParentAsync<Web4OASISNFTCollection>(avatarId, HolonType.Web4NFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4OASISNFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4OASISNFT>> childrenResult = await LoadChildNFTsForNFTCollectionAsync(collection.Web4OASISNFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4OASISNFTs = childrenResult.Result.ToList();
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

        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>> LoadAllGeoNFTCollectionsAsync(bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadAllGeoNFTCollectionsAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4OASISGeoNFTCollection>> holonRes = await Data.LoadAllHolonsAsync<Web4OASISGeoNFTCollection>(HolonType.Web4GeoNFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4OASISGeoNFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4OASISGeoSpatialNFT>> childrenResult = await LoadChildGeoNFTsForNFTCollectionAsync(collection.Web4OASISGeoNFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4OASISGeoNFTs = childrenResult.Result.ToList();
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

        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>> LoadGeoNFTCollectionsForAvatarAsync(Guid avatarId, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadGeoNFTCollectionsForAvatarAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4OASISGeoNFTCollection>> holonRes = await Data.LoadHolonsForParentAsync<Web4OASISGeoNFTCollection>(avatarId, HolonType.Web4GeoNFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4OASISGeoNFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4OASISGeoSpatialNFT>> childrenResult = await LoadChildGeoNFTsForNFTCollectionAsync(collection.Web4OASISGeoNFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4OASISGeoNFTs = childrenResult.Result.ToList();
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

        private async Task<OASISResult<IWeb4OASISNFT>> MintNFTInternalAsync(OASISResult<IWeb4OASISNFT> result, IMintWeb4NFTRequest request, IMintWeb3NFTRequest web3Request, EnumValue<ProviderType> metaDataProviderType, OASISResult<IOASISNFTProvider> nftProviderResult, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, bool isLastWeb3NFT = false)
        {
            string errorMessage = "Error occured in NFTManager.MintNFTInternalAsync. Reason:";
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

            if (!string.IsNullOrEmpty(request.ImageUrl) || request.NFTOffChainMetaType.Value == NFTOffChainMetaType.ExternalJSONURL)
            {
                string json = request.JSONMetaData;

                if (string.IsNullOrEmpty(json))
                    json = CreateMetaDataJson(request, request.NFTStandardType.Value);

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

                //Set NumberToMint to 1 in case the provider attempts to mint multiple nfts (we currently control the multi-minting here in the NFT Manager).
                //TODO: Is it better to let the providers control the multi-minting or the NFTManager? Its safer for NFTManager I think in case the providers do not implement properly etc...
                int numberToMint = request.NumberToMint;
                request.NumberToMint = 1;
                Web3NFT currentWeb3NFT = new Web3NFT();
                string mintErrorMessage = string.Empty;

                web3Request.JSONMetaDataURL = request.JSONMetaDataURL;
                web3Request.ImageUrl = request.ImageUrl;

                for (int i = 0; i < numberToMint; i++)
                {
                    do
                    {
                        try
                        {
                            OASISResult<IWeb3NFTTransactionRespone> mintResult = await nftProviderResult.Result.MintNFTAsync(web3Request);

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

                            if (!request.WaitTillNFTMinted)
                            {
                                currentWeb3NFT.MintTransactionHash = $"{mintErrorMessage}. WaitTillNFTMinted is false so aborting! ";
                                break;
                            }
                        }

                        //TODO: May cause issues in the non-async version because will block the calling thread! Need to look into this and find better way if needed...
                        Thread.Sleep(request.AttemptToMintEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.WaitForNFTToMintInSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            mintErrorMessage = $"{mintErrorMessage}Timeout expired, WaitForNFTToMintInSeconds ({request.WaitForNFTToMintInSeconds}) exceeded, try increasing and trying again!";
                            currentWeb3NFT.MintTransactionHash = mintErrorMessage;
                            OASISErrorHandling.HandleError(ref result, mintErrorMessage);
                            break;
                        }

                        mintErrorMessage = "";

                    } while (attemptingToMint);

                    if (!string.IsNullOrEmpty(currentWeb3NFT.MintTransactionHash) && !string.IsNullOrEmpty(request.SendToAddressAfterMinting))
                    {
                        bool attemptingToSend = true;
                        startTime = DateTime.Now;
                        CLIEngine.SupressConsoleLogging = true;

                        do
                        {
                            try
                            {
                                OASISResult<IWeb3NFTTransactionRespone> sendResult = await nftProviderResult.Result.SendNFTAsync(new Web3NFTWalletTransactionRequest()
                                {
                                    FromWalletAddress = currentWeb3NFT.OASISMintWalletAddress,
                                    ToWalletAddress = web3Request.SendToAddressAfterMinting,
                                    TokenAddress = currentWeb3NFT.NFTTokenAddress,
                                    FromProvider = request.OnChainProvider,
                                    ToProvider = request.OnChainProvider,
                                    Amount = 1,
                                    MemoText = $"Sending NFT from OASIS Wallet {currentWeb3NFT.OASISMintWalletAddress} to {request.SendToAddressAfterMinting} on chain {request.OnChainProvider.Name}.",
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

                                if (!request.WaitTillNFTSent)
                                {
                                    currentWeb3NFT.SendNFTTransactionHash = $"{mintErrorMessage}. WaitTillNFTSent is false so aborting! ";
                                    break;
                                }

                                mintErrorMessage = "";
                            }

                            Thread.Sleep(request.AttemptToSendEveryXSeconds * 1000);

                            if (startTime.AddSeconds(request.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
                            {
                                mintErrorMessage = $"{mintErrorMessage}Timeout expired, WaitForNFTToSendInSeconds ({request.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!";
                                currentWeb3NFT.SendNFTTransactionHash = mintErrorMessage;
                                OASISErrorHandling.HandleError(ref result, mintErrorMessage);
                                break;
                            }

                        } while (attemptingToSend);

                        CLIEngine.SupressConsoleLogging = false;
                    }
                }

                request.NumberToMint = numberToMint;
                CLIEngine.SupressConsoleLogging = false;

                if (!string.IsNullOrEmpty(currentWeb3NFT.MintTransactionHash))
                {
                    if (result.Result == null)
                        result.Result = CreateWeb4NFT(request);

                    result.Result.Web3NFTs.Add((Web3NFT)UpdateWeb3NFT(currentWeb3NFT, web3Request));

                    //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                    if (metaDataProviderType.Value == ProviderType.None)
                        metaDataProviderType.Value = ProviderType.MongoDBOASIS;

                    //Check if this is the last Web3 NFT to mint. If so then we can save the Holon otherwise we wait till the final one to save.
                    //if (request.Web3NFTs == null || (request.Web3NFTs != null && request.Web3NFTs.Count == 0) || (request.Web3NFTs != null && request.Web3NFTs.Count > 0 && web3Request != null && web3Request.RequestId == request.Web3NFTs.Last().RequestId))
                    if (isLastWeb3NFT)
                    {


                        OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateNFTMetaDataHolon(result.Result, request), request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                        {
                            result.IsError = false;
                            result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
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

        //private OASISResult<IWeb4NFTTransactionRespone> MintNFTInternal(IMintWeb4NFTRequest request, NFTStandardType NFTStandardType, EnumValue<ProviderType> metaDataProviderType, OASISResult<IOASISNFTProvider> nftProviderResult, OASISResult<IWeb4NFTTransactionRespone> result, string errorMessage, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        //{
        //    OASISResult<IHolon> jsonSaveResult = null;

        //    //Need to save the image to the off-chain provider first to get the URL to pass into the onchain provider.
        //    if (request.Image != null)
        //    {
        //        switch (request.NFTOffChainMetaType.Value)
        //        {
        //            case NFTOffChainMetaType.Pinata:
        //                {
        //                    Guid imageId = Guid.NewGuid();
        //                    OASISResult<string> pinataResult = Pinata.UploadFileToPinataAsync(request.Image, imageId.ToString()).Result;

        //                    if (pinataResult != null && pinataResult.Result != null && !pinataResult.IsError)
        //                        request.ImageUrl = Pinata.GetFileUrl(pinataResult.Result);
        //                    else
        //                    {
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to Pinata. Reason: {pinataResult.Message}");
        //                        return result;
        //                    }
        //                }
        //                break;

        //            case NFTOffChainMetaType.IPFS:
        //                {
        //                    Guid imageId = Guid.NewGuid();
        //                    //_ipfs.SaveStream(new MemoryStream(request.Image), imageId.ToString(), new Ipfs.CoreApi.AddFileOptions() { Progress = new Progress<>} );
        //                    OASISResult<IFileSystemNode> ipfsResult = IPFS.SaveStreamAsync(new MemoryStream(request.Image), imageId.ToString()).Result;

        //                    if (ipfsResult != null && ipfsResult.Result != null && !ipfsResult.IsError)
        //                        request.ImageUrl = IPFS.GetFileUrl(ipfsResult.Result.Id.ToString());
        //                    else
        //                    {
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to IPFS. Reason: {ipfsResult.Message}");
        //                        return result;
        //                    }
        //                }
        //                break;

        //            case NFTOffChainMetaType.OASIS:
        //                {
        //                    OASISResult<IHolon> imageSaveResult = Data.SaveHolon(new Holon()
        //                    {
        //                        MetaData = new Dictionary<string, object>()
        //                        {
        //                            { "data",  request.Image }
        //                        }
        //                    }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

        //                    if (imageSaveResult != null && imageSaveResult.Result != null && !imageSaveResult.IsError)
        //                        request.ImageUrl = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/getdata/", imageSaveResult.Result.Id);

        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to the OASIS and offchain provider {request.OffChainProvider.Name}. Reason: {imageSaveResult.Message}");
        //                }
        //                break;
        //        }
        //    }

        //    if (!string.IsNullOrEmpty(request.ImageUrl))
        //    {
        //        string json = request.JSONMetaData;

        //        if (string.IsNullOrEmpty(json))
        //            json = CreateMetaDataJson(request, NFTStandardType);

        //        switch (request.NFTOffChainMetaType.Value)
        //        {
        //            case NFTOffChainMetaType.Pinata:
        //                {
        //                    Guid imageId = Guid.NewGuid();
        //                    OASISResult<string> pinataResult = Pinata.UploadJsonToPinataAsync(json).Result;

        //                    if (pinataResult != null && pinataResult.Result != null && !pinataResult.IsError)
        //                        request.JSONMetaDataURL = Pinata.GetFileUrl(pinataResult.Result);
        //                    else
        //                    {
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to Pinata. Reason: {pinataResult.Message}");
        //                        return result;
        //                    }
        //                }
        //                break;

        //            case NFTOffChainMetaType.IPFS:
        //                {
        //                    Guid imageId = Guid.NewGuid();
        //                    OASISResult<IFileSystemNode> ipfsResult = IPFS.SaveTextAsync(json).Result;

        //                    if (ipfsResult != null && ipfsResult.Result != null && !ipfsResult.IsError)
        //                        request.JSONMetaDataURL = IPFS.GetFileUrl(ipfsResult.Result.Id.ToString());
        //                    else
        //                    {
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to IPFS. Reason: {ipfsResult.Message}");
        //                        return result;
        //                    }
        //                }
        //                break;

        //            case NFTOffChainMetaType.OASIS:
        //                {
        //                    jsonSaveResult = SaveJSONMetaDataToOASIS(request, metaDataProviderType, json);

        //                    if (jsonSaveResult != null && jsonSaveResult.Result != null && !jsonSaveResult.IsError)
        //                        request.JSONMetaDataURL = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/load-file/", jsonSaveResult.Result.Id);
        //                    else
        //                    {
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to the OASIS and offchain provider {request.OffChainProvider.Name}. Reason: {jsonSaveResult.Message}");
        //                        return result;
        //                    }
        //                }
        //                break;

        //            case NFTOffChainMetaType.ExternalJSONURL:
        //                {
        //                    if (string.IsNullOrEmpty(request.JSONMetaDataURL))
        //                    {
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} When setting NFTOffChainMetaType to ExternalJsonURL, the JSONUrl cannot be empty!");
        //                        return result;
        //                    }
        //                    break;
        //                }
        //        }

        //        bool attemptingToMint = true;
        //        DateTime startTime = DateTime.Now;
        //        CLIEngine.SupressConsoleLogging = true;

        //        //Set NumberToMint to 1 in case the provider attempts to mint multiple nfts (we currently control the multi-minting here in the NFT Manager).
        //        //TODO: Is it better to let the providers control the multi-minting or the NFTManager? Its safer for NFTManager I think in case the providers do not implement properly etc...
        //        int numberToMint = request.NumberToMint;
        //        request.NumberToMint = 1;

        //        for (int i = 0; i < numberToMint; i++)
        //        {
        //            do
        //            {
        //                result = nftProviderResult.Result.MintNFT(request);

        //                if (result != null && result.Result != null && !result.IsError)
        //                    break;

        //                else if (!request.WaitTillNFTMinted)
        //                {
        //                    result.Result.Web4OASISNFT.MintTransactionHash = $"Error occured attempting to mint NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToMintInSeconds}) exceeded, try increasing and trying again!";
        //                    break;
        //                }

        //                //TODO: May cause issues in the non-async version because will block the calling thread! Need to look into this and find better way if needed...
        //                Thread.Sleep(request.AttemptToMintEveryXSeconds * 1000);

        //                if (startTime.AddSeconds(request.WaitForNFTToMintInSeconds) > DateTime.Now)
        //                    break;

        //            } while (attemptingToMint);
        //        }

        //        request.NumberToMint = numberToMint;
        //        CLIEngine.SupressConsoleLogging = false;

        //        if (result != null && !result.IsError && result.Result != null)
        //        {
        //            result.Result.Web4OASISNFT = CreateWeb4OASISNFT(request, result.Result);

        //            //Default to Mongo for storing the OASIS NFT meta data if none is specified.
        //            if (metaDataProviderType.Value == ProviderType.None)
        //                metaDataProviderType.Value = ProviderType.MongoDBOASIS;

        //            if (jsonSaveResult != null)
        //            {
        //                result.Result.Web4OASISNFT.JSONMetaDataURLHolonId = jsonSaveResult.Result.Id;
        //                result.Result.Web4OASISNFT.JSONMetaData = jsonSaveResult.Result.MetaData["data"].ToString();
        //            }

        //            OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateNFTMetaDataHolon(result.Result.Web4OASISNFT, request), request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

        //            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
        //            {
        //                if (!string.IsNullOrEmpty(request.SendToAddressAfterMinting))
        //                {
        //                    bool attemptingToSend = true;
        //                    startTime = DateTime.Now;

        //                    do
        //                    {
        //                        OASISResult<IWeb4NFTTransactionRespone> sendResult = nftProviderResult.Result.SendNFT(new Web4NFTWalletTransactionRequest()
        //                        {
        //                            FromWalletAddress = result.Result.Web4OASISNFT.OASISMintWalletAddress,
        //                            ToWalletAddress = request.SendToAddressAfterMinting,
        //                            TokenAddress = result.Result.Web4OASISNFT.NFTTokenAddress,
        //                            FromProvider = request.OnChainProvider,
        //                            ToProvider = request.OnChainProvider,
        //                            Amount = 1,
        //                            MemoText = $"Sending NFT from OASIS Wallet {result.Result.Web4OASISNFT.OASISMintWalletAddress} to {request.SendToAddressAfterMinting} on chain {request.OnChainProvider.Name}.",
        //                        });

        //                        if (sendResult != null && sendResult.Result != null && !sendResult.IsError)
        //                        {
        //                            result.Result.Web4OASISNFT.SendNFTTransactionHash = sendResult.Result.TransactionResult;
        //                            result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
        //                            break;
        //                        }
        //                        else if (!request.WaitTillNFTSent)
        //                        {
        //                            result.Result.Web4OASISNFT.SendNFTTransactionHash = $"Error occured attempting to send NFT & WaitTillNFTSent is false. Reason: {sendResult.Message}";
        //                            result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
        //                            break;
        //                        }

        //                        Thread.Sleep(request.AttemptToSendEveryXSeconds * 1000);

        //                        if (startTime.AddSeconds(request.WaitForNFTToSendInSeconds).Ticks < DateTime.Now.Ticks)
        //                        {
        //                            result.Result.Web4OASISNFT.SendNFTTransactionHash = $"Error occured attempting to send NFT. Reason: Timeout expired, WaitSeconds ({request.WaitForNFTToSendInSeconds}) exceeded, try increasing and trying again!";
        //                            result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
        //                            break;
        //                        }

        //                    } while (attemptingToSend);
        //                }
        //            }
        //            else
        //            {
        //                result.Result = null;
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {metaDataProviderType.Name} {Enum.GetName(typeof(ProviderType), metaDataProviderType.Value)}. Reason: {saveHolonResult.Message}");
        //            }
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured minting the OASISNFT: Reason: {result.Message}");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The ImageUrl is null!");

        //    return result;
        //}

        private string FormatSuccessMessage(IMintWeb4NFTRequest request, OASISResult<IWeb4OASISNFT> response, EnumValue<ProviderType> metaDataProviderType, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                string JSONMetaDataURIHolonId = "";

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                {
                    string sendNFTMessage = GenerateSendMessage(response.Result, request, web3NFT.SendNFTTransactionHash, "", 2);
                    message = string.Concat(message, $"Successfully minted the NFT on the {response.Result.Web3NFTs[0].OnChainProvider.Name} provider with hash {response.Result.Web3NFTs[0].MintTransactionHash} and title '{response.Result.Web3NFTs[0].Title}' by AvatarId {request.MintedByAvatarId} using OASIS Minting Account {response.Result.Web3NFTs[0].OASISMintWalletAddress} for price {response.Result.Web3NFTs[0].Price}. NFT Address: {response.Result.Web3NFTs[0].NFTTokenAddress}. The OASIS metadata is stored on the {response.Result.Web3NFTs[0].OffChainProvider.Name} provider with the id {response.Result.Web3NFTs[0].Id} and JSON URL {response.Result.Web3NFTs[0].JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.Web3NFTs[0].ImageUrl}, Mint Date: {response.Result.Web3NFTs[0].MintedOn}. {sendNFTMessage}", lineBreak);
                }

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";


            message = string.Concat(message, $"Successfully minted {response.Result.Web3NFTs.Count} OASIS NFT(s)!{lineBreak}");
            message = string.Concat(message, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                message = string.Concat(message, GenerateNFTSummary(web3NFT, request, lineBreak, colWidth));

            if (response.IsWarning)
                message = string.Concat(message, " Warning:".PadRight(colWidth), response.Message, lineBreak);

            return message;
        }

        private string FormatSuccessMessage(IImportWeb3NFTRequest request, OASISResult<IWeb4OASISNFT> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                string JSONMetaDataURIHolonId = "";

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                    message = string.Concat(message, $"Successfully imported the Web3 NFT on the {web3NFT.OnChainProvider.Name} provider with NFTTokenAddress {web3NFT.NFTTokenAddress} and title '{web3NFT.Title}' by AvatarId {request.ImportedByByAvatarId}. NFT minted using wallet address: {request.NFTMintedUsingWalletAddress}. Price: {request.Price}. The OASIS metadata is stored on the {request.OnChainProvider.Name} provider with the id {response.Result.Id} and JSON URL {response.Result.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.ImageUrl}, Imported Date: {response.Result.MintedOn}.", lineBreak);
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";


            message = string.Concat(message, $"Successfully imported the Web3 NFT!{lineBreak}");
            message = string.Concat(message, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                message = string.Concat(message, GenerateNFTSummary(web3NFT, lineBreak, colWidth));

            if (response.IsWarning)
                message = string.Concat(message, " Warning:".PadRight(colWidth), response.Message, lineBreak);

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IWeb4OASISNFT> response, Guid importedByByAvatarId, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";
                lineBreak = "|";

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                    message = string.Concat(message, $"Successfully imported the OASIS NFT on the {web3NFT.OnChainProvider.Name} provider with NFTTokenAddress {web3NFT.NFTTokenAddress} and title '{web3NFT.Title}' by AvatarId {importedByByAvatarId}. NFT minted using wallet address: {web3NFT.NFTMintedUsingWalletAddress}. Price: {web3NFT.Price}. The OASIS metadata is stored on the {web3NFT.OnChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {web3NFT.ImageUrl}, Imported Date: {web3NFT.MintedOn}.", lineBreak);

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, $"Successfully imported the OASIS NFT!{lineBreak}");
            message = string.Concat(message, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                message = string.Concat(message, GenerateNFTSummary(web3NFT, lineBreak, colWidth));

            if (response.IsWarning)
                message = string.Concat(message, " Warning:".PadRight(colWidth), response.Message, lineBreak);

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IWeb4OASISGeoSpatialNFT> response, Guid importedByByAvatarId, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";
                lineBreak = "|";

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                //TODO: Get original code
                //foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                //    message = string.Concat(message, $"Successfully imported the OASIS GeoNFT on the {response.Result.OnChainProvider.Name} provider with NFTTokenAddress {response.Result.NFTTokenAddress} and title '{response.Result.Title}' by AvatarId {importedByByAvatarId}. NFT minted using wallet address: {response.Result.NFTMintedUsingWalletAddress}. Price: {response.Result.Price}. The OASIS metadata is stored on the {response.Result.OnChainProvider.Name} provider with the id {response.Result.Id} and JSON URL {response.Result.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.ImageUrl}, Imported Date: {response.Result.MintedOn}.", lineBreak);

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";


            message = string.Concat(message, $"Successfully imported the OASIS GeoNFT!{lineBreak}");
            message = string.Concat(message, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                message = string.Concat(message, GenerateNFTSummary(web3NFT, lineBreak, colWidth));

            if (response.IsWarning)
                message = string.Concat(message, " Warning:".PadRight(colWidth), response.Message, lineBreak);

            return message;
        }

        private string FormatSuccessMessage(IMintWeb4NFTRequest request, OASISResult<IWeb4OASISGeoSpatialNFT> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = 40)
        {
            string lineBreak = "\n";
            string message = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";
                lineBreak = "|";

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                {
                    string sendNFTMessage = GenerateSendMessage(response.Result, request, web3NFT.SendNFTTransactionHash, "", 2);
                    message = string.Concat(message, $"Successfully minted and placed the new OASIS GeoNFT. The meta data is stored on the GeoNFTMetaDataProvider {response.Result.GeoNFTMetaDataProvider.Name} with id {response.Result.Id} and was placed by the avatar with id {response.Result.PlacedByAvatarId}. The NFT was originally minted on the {web3NFT.OnChainProvider.Name} onchain provider with hash {web3NFT.MintTransactionHash} and title '{web3NFT.Title}' by the avatar with id {web3NFT.MintedByAvatarId} for the price of {web3NFT.Price} using OASIS Minting Account {web3NFT.OASISMintWalletAddress}. NFT Address: {web3NFT.NFTTokenAddress}. The OASIS metadata for the original NFT is stored on the {web3NFT.OffChainProvider.Name} offchain provider with the id {response.Result.OriginalWeb4OASISNFTId} and JSON URL {web3NFT.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {web3NFT.ImageUrl}, Mint Date: {web3NFT.MintedOn}. {sendNFTMessage}", lineBreak);
                }

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, $"Successfully minted & placed the OASIS Geo-NFT!{lineBreak}");
            message = string.Concat(message, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                message = string.Concat(message, GenerateNFTSummary(web3NFT, request, lineBreak, colWidth));

            message = string.Concat(message, GenerateGeoNFTSummary(response.Result, lineBreak, colWidth));

            if (response.IsWarning)
                message = string.Concat(message, " Warning:".PadRight(colWidth), response.Message, lineBreak);

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IWeb4OASISGeoSpatialNFT> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = 40)
        {
            string lineBreak = "\n";
            string message = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";
                lineBreak = "|";

                if (response.Result.JSONMetaDataURLHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, " ");

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                    message = string.Concat(message, $"Successfully created and placed the new OASIS GeoNFT. The meta data is stored on the GeoNFTMetaDataProvider {response.Result.GeoNFTMetaDataProvider.Name} with id {response.Result.Id} and was placed by the avatar with id {response.Result.PlacedByAvatarId}. The NFT was originally minted on the {web3NFT.OnChainProvider.Name} onchain provider with hash {web3NFT.MintTransactionHash} and title '{web3NFT.Title}' by the avatar with id {web3NFT.MintedByAvatarId} for the price of {web3NFT.Price} using OASIS Minting Account {web3NFT.OASISMintWalletAddress}. NFT Address: {web3NFT.NFTTokenAddress}. The OASIS metadata for the original NFT is stored on the {web3NFT.OffChainProvider.Name} offchain provider with the id {response.Result.OriginalWeb4OASISNFTId} and JSON URL {web3NFT.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {web3NFT.ImageUrl}, Mint Date: {web3NFT.MintedOn}.");

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";


            message = string.Concat(message, $"Successfully created & placed the OASIS Geo-NFT!{lineBreak}");
            message = string.Concat(message, lineBreak);
            message = string.Concat(message, $"ORIGINAL NFT INFO:{lineBreak}");

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                message = string.Concat(message, GenerateNFTSummary(web3NFT, null, lineBreak, colWidth));

            message = string.Concat(message, lineBreak);
            message = string.Concat(message, GenerateGeoNFTSummary(response.Result, lineBreak, colWidth));

            return message;
        }

        private string FormatSuccessMessage(IWeb3NFTWalletTransactionRequest request, OASISResult<IWeb3NFTTransactionRespone> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
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

        private string GenerateNFTSummary(IWeb3NFT web3NFT, IMintWeb4NFTRequest request, string lineBreak, int colWidth)
        {
            string message = GenerateNFTSummary(web3NFT, lineBreak, colWidth);

            if (request != null)
                message = string.Concat(message, " Number To Mint:".PadRight(colWidth), request.NumberToMint, lineBreak);

            message = string.Concat(message, GenerateSendMessage(web3NFT, request, web3NFT.SendNFTTransactionHash, lineBreak, colWidth), lineBreak);
            return message;
        }

        private string GenerateNFTSummary(IWeb3NFT web3NFT, string lineBreak, int colWidth)
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

        private string GenerateGeoNFTSummary(IWeb4OASISGeoSpatialNFT OASISNFT, string lineBreak, int colWidth)
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



        private IWeb3NFT UpdateWeb3NFT(IWeb3NFT web3NFT, IMintWeb3NFTRequest request)
        {
            if (web3NFT.Id == Guid.Empty)
                web3NFT.Id = Guid.NewGuid();

            //web3NFT.MintTransactionHash = mintNFTResponse.TransactionResult,
            //web3NFT.SellerFeeBasisPoints = mintNFTResponse.Web3NFT != null ? mintNFTResponse.Web3NFT.SellerFeeBasisPoints : 0,
            //web3NFT.MetaData = mintNFTResponse.Web3NFT != null ? mintNFTResponse.Web3NFT.MetaData : null,
            //web3NFT.OASISMintWalletAddress = mintNFTResponse.Web3NFT != null ? mintNFTResponse.Web3NFT.OASISMintWalletAddress : null,
            //web3NFT.UpdateAuthority = mintNFTResponse.Web3NFT != null ? mintNFTResponse.Web3NFT.UpdateAuthority : null,
            //web3NFT.NFTTokenAddress = mintNFTResponse.Web3NFT != null ? mintNFTResponse.Web3NFT.NFTTokenAddress : null, //TODO: Need to pull this from the provider mint functions...
            web3NFT.MintedByAvatarId = request.MintedByAvatarId;
            web3NFT.SendToAddressAfterMinting = request.SendToAddressAfterMinting;
            web3NFT.SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId;
            web3NFT.SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername;
            //web3NFT.SendNFTTransactionHash = mintNFTResponse.Web3NFT.SendNFTTransactionHash;
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

            return web3NFT;
        }

        //private Web3NFT CreateWeb3OASISNFT(IMintWeb3NFTRequest request, IWeb3NFTTransactionRespone mintNFTResponse)
        //{
        //    return new Web3NFT()
        //    {
        //        Id = mintNFTResponse.Web3NFT != null && mintNFTResponse.Web3NFT.Id != Guid.Empty ? mintNFTResponse.Web3NFT.Id : Guid.NewGuid(),
        //        MintTransactionHash = mintNFTResponse.TransactionResult,
        //        SellerFeeBasisPoints = mintNFTResponse.Web3NFT != null ? mintNFTResponse.Web3NFT.SellerFeeBasisPoints : 0,
        //        MetaData = mintNFTResponse.Web3NFT != null ? mintNFTResponse.Web3NFT.MetaData : null,
        //        OASISMintWalletAddress = mintNFTResponse.Web3NFT != null ? mintNFTResponse.Web3NFT.OASISMintWalletAddress : null,
        //        UpdateAuthority = mintNFTResponse.Web3NFT != null ? mintNFTResponse.Web3NFT.UpdateAuthority : null,
        //        NFTTokenAddress = mintNFTResponse.Web3NFT != null ? mintNFTResponse.Web3NFT.NFTTokenAddress : null, //TODO: Need to pull this from the provider mint functions...
        //        MintedByAvatarId = request.MintedByAvatarId,
        //        SendToAddressAfterMinting = request.SendToAddressAfterMinting,
        //        SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
        //        SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
        //        SendNFTTransactionHash = mintNFTResponse.Web3NFT.SendNFTTransactionHash,
        //        Title = request.Title,
        //        Description = request.Description,
        //        Price = request.Price,
        //        Discount = request.Discount,
        //        RoyaltyPercentage = request.RoyaltyPercentage.Value,
        //        Image = request.Image,
        //        ImageUrl = request.ImageUrl,
        //        Thumbnail = request.Thumbnail,
        //        ThumbnailUrl = request.ThumbnailUrl,
        //        OnChainProvider = request.OnChainProvider,
        //        OffChainProvider = request.OffChainProvider,
        //        StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
        //        NFTOffChainMetaType = request.NFTOffChainMetaType,
        //        NFTStandardType = request.NFTStandardType,
        //        Symbol = request.Symbol,
        //        MintedOn = DateTime.Now,
        //        MemoText = request.MemoText,
        //        JSONMetaDataURL = request.JSONMetaDataURL,
        //        IsForSale = request.IsForSale.Value,
        //        SaleStartDate = request.SaleStartDate,
        //        SaleEndDate = request.SaleEndDate
        //        //OffChainProviderHolonId = Guid.NewGuid(),
        //        //Token= request.Token
        //    };
        //}

        //private Web4OASISNFT CreateWeb4OASISNFT(IMintWeb4NFTRequest request, IWeb4NFTTransactionRespone mintNFTResponse)
        private Web4OASISNFT CreateWeb4NFT(IMintWeb4NFTRequest request)
        {
            return new Web4OASISNFT()
            {
                Id = Guid.NewGuid(),
                //Id = mintNFTResponse.Web4OASISNFT != null && mintNFTResponse.Web4OASISNFT.Id != Guid.Empty ? mintNFTResponse.Web4OASISNFT.Id : Guid.NewGuid(),
                //Web3NFTs = mintNFTResponse.Web4OASISNFT != null ? mintNFTResponse.Web4OASISNFT.Web3NFTs : null,
                //MintTransactionHash = mintNFTResponse.TransactionResult,
                //SellerFeeBasisPoints = mintNFTResponse.Web4OASISNFT != null ? mintNFTResponse.Web4OASISNFT.SellerFeeBasisPoints : 0,
                //MetaData = mintNFTResponse.Web4OASISNFT != null ? mintNFTResponse.Web4OASISNFT.MetaData : null,
                //OASISMintWalletAddress = mintNFTResponse.Web4OASISNFT != null ? mintNFTResponse.Web4OASISNFT.OASISMintWalletAddress : null,
                //UpdateAuthority = mintNFTResponse.Web4OASISNFT != null ? mintNFTResponse.Web4OASISNFT.UpdateAuthority : null,
                //NFTTokenAddress = mintNFTResponse.Web4OASISNFT != null ? mintNFTResponse.Web4OASISNFT.NFTTokenAddress : null, //TODO: Need to pull this from the provider mint functions...
                MetaData = request.MetaData,
                MintedByAvatarId = request.MintedByAvatarId,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                //SendNFTTransactionHash = mintNFTResponse.Web4OASISNFT.SendNFTTransactionHash,
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
                //OffChainProviderHolonId = Guid.NewGuid(),
                //Token= request.Token
            };
        }

        private Web4OASISNFT CreateWeb4NFT(IImportWeb3NFTRequest request)
        {
            return new Web4OASISNFT()
            {
                Id = Guid.NewGuid(),
                //MintTransactionHash = request.MintTransactionHash,
                MetaData = request.MetaData,
                //NFTMintedUsingWalletAddress = request.NFTMintedUsingWalletAddress,
                //UpdateAuthority = request.UpdateAuthority,
                //NFTTokenAddress = request.NFTTokenAddress,
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
                ImportedOn = DateTime.Now,
                Web3NFTs = new List<Web3NFT>() { new Web3NFT()
                {
                    MintTransactionHash = request.MintTransactionHash,
                    NFTMintedUsingWalletAddress = request.NFTMintedUsingWalletAddress,
                    UpdateAuthority = request.UpdateAuthority,
                    NFTTokenAddress = request.NFTTokenAddress
                } }
            };
        }

        private Web4OASISGeoSpatialNFT CreateGeoSpatialNFT(IPlaceWeb4GeoSpatialNFTRequest request, IWeb4OASISNFT originalNftMetaData)
        {
            return new Web4OASISGeoSpatialNFT()
            {
                Id = Guid.NewGuid(),  //The NFT could be placed many times so we need a new ID for each time
                OriginalWeb4OASISNFTId = request.OriginalWeb4OASISNFTId, //We need to link back to the orignal NFT (but we copy across the NFT properties making it quicker and easier to get at the data). TODO: Do we want to copy the data across? Pros and Cons? Need to think about this... for now it's fine... ;-)
                GeoNFTMetaDataProvider = request.GeoNFTMetaDataProvider,
                //MintTransactionHash = originalNftMetaData.MintTransactionHash,
                JSONMetaDataURL = originalNftMetaData.JSONMetaDataURL,
                //OASISMintWalletAddress = originalNftMetaData.OASISMintWalletAddress,
                MintedByAvatarId = originalNftMetaData.MintedByAvatarId,
                SendToAddressAfterMinting = originalNftMetaData.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = originalNftMetaData.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = originalNftMetaData.SendToAvatarAfterMintingUsername,
                //SendNFTTransactionHash = originalNftMetaData.SendNFTTransactionHash,
                //UpdateAuthority = originalNftMetaData.UpdateAuthority,
                //OffChainProviderHolonId = originalNftMetaData.OffChainProviderHolonId,
                SellerFeeBasisPoints = originalNftMetaData.SellerFeeBasisPoints,
                //NFTTokenAddress = originalNftMetaData.NFTTokenAddress,
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
                Nft2DSpriteURI = request.Nft2DSpriteURI,
                Web3NFTs = originalNftMetaData.Web3NFTs
            };
        }

        private IHolon CreateNFTMetaDataHolon(IWeb4OASISNFT nftMetaData, IMintWeb4NFTRequest request = null)
        {
            return UpdateNFTMetaDataHolon(new Holon(HolonType.Web4NFT), nftMetaData, request);
        }

        private IHolon UpdateNFTMetaDataHolon(IHolon holonNFT, IWeb4OASISNFT nftMetaData, IMintWeb4NFTRequest request = null)
        {
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} NFT Minted On The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.OASISNFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData); //TODO: May remove this because its duplicated data. BUT we may need this for other purposes later such as exporting it to a file etc (but then we could just serialaize it there and then).
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

        private IHolon CreateGeoSpatialNFTMetaDataHolon(IWeb4OASISGeoSpatialNFT geoNFTMetaData, IMintWeb4NFTRequest request = null)
        {
            return UpdateGeoNFTMetaDataHolon(new Holon(HolonType.Web4GeoNFT), geoNFTMetaData, request);
        }

        private IHolon UpdateGeoNFTMetaDataHolon(IHolon holonNFT, IWeb4OASISGeoSpatialNFT geoNFTMetaData, IMintWeb4NFTRequest request = null)
        {
            holonNFT.Id = geoNFTMetaData.Id;
            holonNFT.Name = "OASIS GEO NFT";
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
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Id"] = geoNFTMetaData.OriginalWeb4OASISNFTId;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MemoText"] = geoNFTMetaData.MemoText;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Title"] = geoNFTMetaData.Title;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Description"] = geoNFTMetaData.Description;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MintedByAvatarId"] = geoNFTMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingId"] = geoNFTMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingUsername"] = geoNFTMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAddressAfterMinting"] = geoNFTMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Price"] = geoNFTMetaData.Price.ToString();
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

        private IHolon CreateNFTMetaDataHolon(IWeb4OASISNFT nftMetaData, IImportWeb3NFTRequest request)
        {
            IHolon holonNFT = new Holon(HolonType.Web4NFT);
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} NFT Imported OnTo The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = request.Description;
            holonNFT.MetaData["NFT.OASISNFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData); //TODO: May remove this because its duplicated data. BUT we may need this for other purposes later such as exporting it to a file etc (but then we could just serialaize it there and then).
            holonNFT.MetaData["NFT.MintTransactionHash"] = request.MintTransactionHash;
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.ImportedByByAvatarId"] = request.ImportedByByAvatarId.ToString();
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
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(request.MetaData);
            holonNFT.ParentHolonId = nftMetaData.ImportedByAvatarId;

            if (nftMetaData.Web3NFTs.Count > 0)
            {
                holonNFT.MetaData["NFT.UpdateAuthority"] = nftMetaData.Web3NFTs[0].UpdateAuthority;
                holonNFT.MetaData["NFT.NFTMintedUsingWalletAddress"] = nftMetaData.Web3NFTs[0].NFTMintedUsingWalletAddress;
                holonNFT.MetaData["NFT.NFTTokenAddress"] = nftMetaData.Web3NFTs[0].NFTTokenAddress;
            }

            return holonNFT;
        }

        private IMintWeb4NFTRequest CreateMintNFTTransactionRequest(IMintAndPlaceWeb4GeoSpatialNFTRequest mintAndPlaceGeoSpatialNFTRequest)
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

        private OASISResult<IWeb4OASISNFT> DecodeNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IWeb4OASISNFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (IWeb4OASISNFT)System.Text.Json.JsonSerializer.Deserialize(holonResult.Result.MetaData["NFT.OASISNFT"].ToString(), typeof(Web4OASISNFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IWeb4OASISGeoSpatialNFT> DecodeGeoNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IWeb4OASISGeoSpatialNFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (Web4OASISGeoSpatialNFT)System.Text.Json.JsonSerializer.Deserialize(holonResult.Result.MetaData["GEONFT.OASISGEONFT"].ToString(), typeof(Web4OASISGeoSpatialNFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IWeb4OASISNFT>> DecodeNFTMetaData(OASISResult<IEnumerable<IHolon>> holonsResult, OASISResult<IEnumerable<IWeb4OASISNFT>> result, string errorMessage)
        {
            List<IWeb4OASISNFT> nfts = new List<IWeb4OASISNFT>();

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                if (holonsResult.Result.Count() > 0)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        nfts.Add((IWeb4OASISNFT)System.Text.Json.JsonSerializer.Deserialize(holon.MetaData["NFT.OASISNFT"].ToString(), typeof(Web4OASISNFT)));

                    result.Result = nfts;
                }
                else
                    result.Message = "No NFT's Found.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");

            return result;
        }

        private OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> DecodeGeoNFTMetaData(OASISResult<IEnumerable<IHolon>> holonsResult, OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> result, string errorMessage)
        {
            List<IWeb4OASISGeoSpatialNFT> nfts = new List<IWeb4OASISGeoSpatialNFT>();

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                if (holonsResult.Result.Count() > 0)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        nfts.Add((IWeb4OASISGeoSpatialNFT)System.Text.Json.JsonSerializer.Deserialize(holon.MetaData["GEONFT.OASISGEONFT"].ToString(), typeof(Web4OASISGeoSpatialNFT)));

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