using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using Nethereum.Contracts.Standards.ERC721;
using System.Text;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class NFTManager : COSMICManagerBase, INFTManager
    {
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

        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest request)
        {
            OASISResult<INFTTransactionRespone> result = new OASISResult<INFTTransactionRespone>();
            string errorMessage = "Error occured in SendNFTAsync in NFTManager. Reason:";

            //if (request.Date == DateTime.MinValue)
            //    request.Date = DateTime.Now;

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.FromProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = await nftProviderResult.Result.SendNFTAsync(request);
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

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest request)
        {
            OASISResult<INFTTransactionRespone> result = new OASISResult<INFTTransactionRespone>();
            string errorMessage = "Error occured in SendNFT in NFTManager. Reason:";

            //if (request.Date == DateTime.MinValue)
            //    request.Date = DateTime.Now;

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.FromProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = nftProviderResult.Result.SendNFT(request);
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
                if (request.NFTStandardType == NFTStandardType.SPL && request.OnChainProvider.Value != ProviderType.SolanaOASIS)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} NFTStandardType is set to SPL but OnChainProvider is not set to SolanaOASIS! Please make sure you set the OnChainProvider to SolanaOASIS when minting SPL NFTs.");
                    return result;
                }

                if (request.NFTStandardType != NFTStandardType.SPL && request.OnChainProvider.Value == ProviderType.SolanaOASIS)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} OnChainProvider is set to SolanaOASIS but NFTStandardType is not set to SPL! Please make sure you set the NFTStandardType to SPL when minting NFTs on SolanaOASIS.");
                    return result;
                }

                if ((request.NFTStandardType == NFTStandardType.ERC721 || request.NFTStandardType == NFTStandardType.ERC1155) && (request.OnChainProvider.Value == ProviderType.ArbitrumOASIS || request.OnChainProvider.Value == ProviderType.EthereumOASIS || request.OnChainProvider.Value == ProviderType.PolygonOASIS))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} When selecting NFTStandardType ERC721 or ERC1155 then the OnChainProvider needs to be set to a supported EVM chain such as ArbitrumOASIS, EthereumOASIS or PolygonOASIS.");
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

                    if (request.StoreNFTMetaDataOnChain)
                        NFTMetaDataProviderType = request.OnChainProvider;
                    else
                        NFTMetaDataProviderType = request.OffChainProvider;

                    result = await MintNFTInternalAsync(request, request.NFTStandardType, NFTMetaDataProviderType, nftProviderResult, result, errorMessage, responseFormatType);

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
                if ((request.NFTStandardType == NFTStandardType.ERC721 || request.NFTStandardType == NFTStandardType.ERC1155) && (request.OnChainProvider.Value == ProviderType.ArbitrumOASIS || request.OnChainProvider.Value == ProviderType.EthereumOASIS || request.OnChainProvider.Value == ProviderType.PolygonOASIS))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} When selecting NFTStandardType ERC721 or ERC1155 then the OnChainProvider needs to be set to a supported EVM chain such as ArbitrumOASIS, EthereumOASIS or PolygonOASIS.");
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

                    EnumValue<ProviderType> NFTMetaDataProviderType;

                    if (request.StoreNFTMetaDataOnChain)
                        NFTMetaDataProviderType = request.OnChainProvider;
                    else
                        NFTMetaDataProviderType = request.OffChainProvider;

                    result = MintNFTInternal(request, request.NFTStandardType, NFTMetaDataProviderType, nftProviderResult, result, errorMessage, responseFormatType);

                    //switch (request.NFTStandardType)
                    //{
                    //    case NFTStandardType.ERC721:
                    //        result = MintNFTInternal(request, NFTStandardType.ERC721, NFTMetaDataProviderType, nftProviderResult, result, errorMessage);
                    //        break;

                    //    case NFTStandardType.ERC1155:
                    //        result = MintNFTInternal(request, NFTStandardType.ERC1155, NFTMetaDataProviderType, nftProviderResult, result, errorMessage);
                    //        break;

                    //    case NFTStandardType.SPL:
                    //        result = MintNFTInternal(request, NFTStandardType.SPL, NFTMetaDataProviderType, nftProviderResult, result, errorMessage);
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
                //result = DecodeNFTMetaData(await Data.LoadHolonByCustomKeyAsync(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);

                //TODO: It may be more efficient and faster to add a custom/metadata field to IHolonBase that can used to Load holons by? Just means having to add additional LoadHolon methods...
                //OASISResult<ISearchResults> searchResult = await SearchManager.Instance.SearchAsync(new SearchParams()
                //{
                //    SearchGroups = new List<ISearchGroupBase>() 
                //    { 
                //        new SearchTextGroup() 
                //        {
                //            SearchQuery = hash, 
                //            SearchHolons = true,
                //            HolonSearchParams = new SearchHolonParams()
                //            {
                //                 MetaData = true
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

                //if (searchResult != null && !searchResult.IsError && searchResult.Result != null && searchResult.Result.SearchResultHolons.Count > 0)
                //    result.Result = (IOASISNFT)JsonSerializer.Deserialize(searchResult.Result.SearchResultHolons[0].MetaData["OASISNFT"].ToString(), typeof(IOASISNFT));
                //else
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading/searching for the holon metadata. Reason: {searchResult.Message}");
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

        public async Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in PlaceGeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFT> loadNftResult = await LoadNftAsync(request.OriginalOASISNFTId, request.OriginalOASISNFTOffChainProviderType);

                if (loadNftResult != null && !loadNftResult.IsError && loadNftResult.Result != null)
                {
                    result.Result = CreateGeoSpatialNFT(request, loadNftResult.Result);
                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateGeoSpatialNFTMetaDataHolon(result.Result), request.PlacedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider);

                    if ((saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null)) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), request.GeoNFTMetaDataProvider)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = $"Successfully created and placed the new OASIS GeoNFT. The meta data is stored on the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), request.GeoNFTMetaDataProvider)} with id {result.Result.Id} and was placed by the avatar with id {result.Result.PlacedByAvatarId}. The NFT was originally minted on the {result.Result.OnChainProvider.Name} onchain provider with hash {result.Result.Hash} and title '{result.Result.Title}' by the avatar with id {result.Result.MintedByAvatarId} for the price of {result.Result.Price}. The OASIS metadata for the original NFT is stored on the {result.Result.OffChainProvider.Name} offchain provider with the id {result.Result.OriginalOASISNFTId}.";
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

        public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in PlaceGeoNFT in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFT> loadNftResult = LoadNft(request.OriginalOASISNFTId, request.OriginalOASISNFTOffChainProviderType);

                if (loadNftResult != null && !loadNftResult.IsError && loadNftResult.Result != null)
                {
                    result.Result = CreateGeoSpatialNFT(request, loadNftResult.Result);
                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateGeoSpatialNFTMetaDataHolon(result.Result), request.PlacedByAvatarId, true, true, 0, true, false, request.GeoNFTMetaDataProvider);

                    if ((saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null)) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), request.GeoNFTMetaDataProvider)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = $"Successfully created and placed the new OASIS GeoNFT meta data on the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), request.GeoNFTMetaDataProvider)} with id {result.Result.Id}, which was placed by avatar id {result.Result.PlacedByAvatarId} and was originally minted on the {Enum.GetName(typeof(ProviderType), result.Result.OnChainProvider)} onchain provider with hash {result.Result.Hash} and title '{result.Result.Title}' by AvatarId {result.Result.MintedByAvatarId} for price {result.Result.Price}. The OASIS metadata for the original NFT is stored on the {Enum.GetName(typeof(ProviderType), result.Result.OffChainProvider)} offchain provider with the id {result.Result.OriginalOASISNFTId}.";
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

        public async Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
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
                        OriginalOASISNFTOffChainProviderType = request.OffChainProvider.Value,
                        ProviderType = request.OffChainProvider.Value,
                        PlacedByAvatarId = request.MintedByAvatarId,
                        Lat = request.Lat,
                        Long = request.Long,
                        AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect,
                        PermSpawn = request.PermSpawn,
                        GlobalSpawnQuantity = request.GlobalSpawnQuantity,
                        PlayerSpawnQuantity = request.PlayerSpawnQuantity
                    };

                    result.Result = CreateGeoSpatialNFT(placeGeoSpatialNFTRequest, mintNftResult.Result.OASISNFT);
                    OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateGeoSpatialNFTMetaDataHolon(result.Result), request.MintedByAvatarId, true, true, 0, true, false, request.OffChainProvider.Value);

                    if (saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the OffChainProvider {Enum.GetName(typeof(ProviderType), request.OffChainProvider.Value)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        FormatSuccessMessage(request, result);
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

        public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request)
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
                        OriginalOASISNFTOffChainProviderType = request.OffChainProvider.Value,
                        ProviderType = request.OffChainProvider.Value,
                        PlacedByAvatarId = request.MintedByAvatarId,
                        Lat = request.Lat,
                        Long = request.Long,
                        AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect,
                        PermSpawn = request.PermSpawn,
                        GlobalSpawnQuantity = request.GlobalSpawnQuantity,
                        PlayerSpawnQuantity = request.PlayerSpawnQuantity
                    };

                    result.Result = CreateGeoSpatialNFT(placeGeoSpatialNFTRequest, mintNftResult.Result.OASISNFT);
                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateGeoSpatialNFTMetaDataHolon(result.Result), request.MintedByAvatarId, true, true, 0, true, false, request.OffChainProvider.Value);

                    if (saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the OffChainProvider {Enum.GetName(typeof(ProviderType), request.OffChainProvider.Value)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = $"Successfully minted and placed the new OASIS GeoNFT which was placed and minted by avatar with id {result.Result.PlacedByAvatarId}. The NFT was minted on the {result.Result.OnChainProvider.Name} onchain provider with hash {result.Result.Hash} and title '{result.Result.Title}' for the price of {result.Result.Price} using OASIS Minting Account {result.Result.OASISMintWalletAddress}. NFT Address: {result.Result.NFTTokenAddress}. SendToAvatarAfterMintingId is {result.Result.SendToAvatarAfterMintingId}, SendToAvatarAfterMintingUsername is {result.Result.SendToAvatarAfterMintingUsername} & SendToAvatarAddressAfterMinting is {result.Result.SendToAddressAfterMinting}. The OASIS metadata is stored on the {result.Result.OffChainProvider.Name} provider with the id {result.Result.Id} and JSON URL {result.Result.JSONMetaDataURL}. OASIS JSON MetaData URL Holon Id is {result.Result.JSONMetaDataURIHolonId}. ImageURL: {result.Result.ImageUrl}, Mint Date: {result.Result.MintedOn}";
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

        //public ProviderType GetProviderTypeFromNFTProviderType(NFTProviderType nftProviderType)
        //{
        //    ProviderType providerType = ProviderType.None;

        //    switch (nftProviderType)
        //    {
        //        case NFTProviderType.Solana:
        //            providerType = ProviderType.SolanaOASIS;
        //            break;

        //        case NFTProviderType.EOS:
        //            providerType = ProviderType.EOSIOOASIS;
        //            break;

        //        case NFTProviderType.Ethereum:
        //            providerType = ProviderType.EthereumOASIS;
        //            break;
        //    }

        //    return providerType;
        //}

        //public NFTProviderType GetNFTProviderTypeFromProviderType(ProviderType providerType)
        //{
        //    NFTProviderType nftProviderType = NFTProviderType.None;

        //    switch (providerType)
        //    {
        //        case ProviderType.SolanaOASIS:
        //            nftProviderType = NFTProviderType.Solana;
        //            break;

        //        case ProviderType.EOSIOOASIS:
        //            nftProviderType = NFTProviderType.EOS;
        //            break;

        //        case ProviderType.EthereumOASIS:
        //            nftProviderType = NFTProviderType.Ethereum;
        //            break;
        //    }

        //    return nftProviderType;
        //}

        //public IOASISNFTProvider GetNFTProvider<T>(NFTProviderType NFTProviderType, ref OASISResult<T> result, string errorMessage)
        //{
        //    return GetNFTProvider(GetProviderTypeFromNFTProviderType(NFTProviderType), ref result, errorMessage);
        //}

        //public OASISResult<IOASISNFTProvider> GetNFTProvider(NFTProviderType NFTProviderType, string errorMessage = "")
        //{
        //    return GetNFTProvider(GetProviderTypeFromNFTProviderType(NFTProviderType), errorMessage);
        //}

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

        private async Task<OASISResult<INFTTransactionRespone>> MintNFTInternalAsync(IMintNFTTransactionRequest request, NFTStandardType NFTStandardType, EnumValue<ProviderType> metaDataProviderType, OASISResult<IOASISNFTProvider> nftProviderResult, OASISResult<INFTTransactionRespone> result, string errorMessage, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            Guid JSONMetaDataURIHolonId = Guid.Empty;

            //Need to save the image to the off-chain provider first to get the URL to pass into the onchain provider.
            if (request.Image != null)
            {
                if (request.NFTOffChainMetaType == NFTOffChainMetaType.Pinata)
                {
                    //TODO: Save to Pinata here...
                    result.Message = "Pinata Support Coming Soon...";
                    result.IsError = true;
                    return result;

                    string pinataURL = "";
                    request.ImageUrl = pinataURL;
                }
                else if (metaDataProviderType.Value != ProviderType.None)
                {
                    //OASISResult<Guid> imageSaveResult = await Data.SaveFileAsync(request.Image, request.MintedByAvatarId, NFTMetaDataProviderType);
                    OASISResult<IHolon> imageSaveResult = await Data.SaveHolonAsync(new Holon()
                    {
                        MetaData = new Dictionary<string, object>()
                                {
                                    { "data",  request.Image }
                                }
                    }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                    if (imageSaveResult != null && imageSaveResult.Result != null && !imageSaveResult.IsError)
                    {
                        switch (request.NFTOffChainMetaType)
                        {
                            case NFTOffChainMetaType.IPFS:
                                request.ImageUrl = string.Concat(OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString, imageSaveResult.Result.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
                                break;

                            case NFTOffChainMetaType.OASIS:
                                request.ImageUrl = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/getdata/", imageSaveResult.Result.Id);
                                break;
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to the offchain provider {request.OffChainProvider.Name}. Reason: {imageSaveResult.Message}");
                }
            }

            if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                OASISResult<IHolon> jsonSaveResult = null;

                if ((request.NFTOffChainMetaType == NFTOffChainMetaType.IPFS || request.NFTOffChainMetaType == NFTOffChainMetaType.OASIS) && metaDataProviderType.Value != ProviderType.None)
                {
                    jsonSaveResult = await SaveMetaDataToOASISAsync(request, NFTStandardType, metaDataProviderType);

                    if (jsonSaveResult != null && jsonSaveResult.Result != null && !jsonSaveResult.IsError)
                        JSONMetaDataURIHolonId = jsonSaveResult.Result.Id;
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to the offchain provider {request.OffChainProvider.Name}. Reason: {jsonSaveResult.Message}");
                        return result;
                    }
                }

                //IMintNFTTransactionRequestForProvider providerRequest = null;

                //switch (request.NFTOffChainMetaType)
                //{
                //    case NFTOffChainMetaType.Pinata:
                //        {
                //            //TODO: Save to Pinata here...
                //            result.Message = "Pinata Support Coming Soon...";
                //            result.IsError = true;
                //            return result;
                //        }
                //        break;

                //    case NFTOffChainMetaType.IPFS:
                //        providerRequest = CreateNFTTransactionRequestForProvider(request, string.Concat(OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString, jsonSaveResult.Result.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]));
                //        break;

                //    case NFTOffChainMetaType.OASIS:
                //        providerRequest = CreateNFTTransactionRequestForProvider(request, string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/load-file/", jsonSaveResult.Result.Id));
                //        break;

                //    case NFTOffChainMetaType.ExternalJsonURL:
                //        {
                //            if (!string.IsNullOrEmpty(request.JSONMetaDataURL))
                //                providerRequest = CreateNFTTransactionRequestForProvider(request, request.JSONMetaDataURL);
                //            else
                //            {
                //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} When setting NFTOffChainMetaType to ExternalJsonURL, the JSONUrl cannot be empty!");
                //                return result;
                //            }
                //            break;
                //        }
                //}

                switch (request.NFTOffChainMetaType)
                {
                    case NFTOffChainMetaType.Pinata:
                        {
                            //TODO: Save to Pinata here...
                            result.Message = "Pinata Support Coming Soon...";
                            result.IsError = true;
                            return result;
                        }
                        break;

                    case NFTOffChainMetaType.IPFS:
                        request.JSONMetaDataURL = string.Concat(OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString, jsonSaveResult.Result.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
                        break;

                    case NFTOffChainMetaType.OASIS:
                        request.JSONMetaDataURL = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/load-file/", jsonSaveResult.Result.Id);
                        break;

                    case NFTOffChainMetaType.ExternalJsonURL:
                        {
                            if (string.IsNullOrEmpty(request.JSONMetaDataURL))
                            { 
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} When setting NFTOffChainMetaType to ExternalJsonURL, the JSONUrl cannot be empty!");
                                return result;
                            }
                            break;
                        }
                }

                if (string.IsNullOrEmpty(request.Symbol))
                    request.Symbol = "OASISNFT";

                //result = await nftProviderResult.Result.MintNFTAsync(providerRequest);
                result = await nftProviderResult.Result.MintNFTAsync(request);

                if (result != null && !result.IsError && result.Result != null)
                {
                    result.Result.OASISNFT = CreateOASISNFT(request, result.Result);

                    //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                    if (metaDataProviderType.Value == ProviderType.None)
                        metaDataProviderType.Value = ProviderType.MongoDBOASIS;

                    result.Result.OASISNFT.JSONMetaDataURIHolonId = JSONMetaDataURIHolonId;

                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateNFTMetaDataHolon(result.Result.OASISNFT, request), request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                    if ((saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null)) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {metaDataProviderType.Name} {Enum.GetName(typeof(ProviderType), metaDataProviderType.Value)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
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
            //Need to save the image to the off-chain provider first to get the URL to pass into the onchain provider.
            if (request.Image != null)
            {
                if (request.NFTOffChainMetaType == NFTOffChainMetaType.Pinata)
                {
                    //TODO: Save to Pinata here...
                    result.Message = "Pinata Support Coming Soon...";
                    result.IsError = true;
                    return result;

                    string pinataURL = "";
                    request.ImageUrl = pinataURL;
                }
                else if (metaDataProviderType.Value != ProviderType.None)
                {
                    //OASISResult<Guid> imageSaveResult = await Data.SaveFileAsync(request.Image, request.MintedByAvatarId, NFTMetaDataProviderType);
                    OASISResult<IHolon> imageSaveResult = Data.SaveHolon(new Holon()
                    {
                        MetaData = new Dictionary<string, object>()
                                {
                                    { "data",  request.Image }
                                }
                    }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                    if (imageSaveResult != null && imageSaveResult.Result != null && !imageSaveResult.IsError)
                    {
                        switch (request.NFTOffChainMetaType)
                        {
                            case NFTOffChainMetaType.IPFS:
                                request.ImageUrl = string.Concat(OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString, imageSaveResult.Result.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
                                break;

                            case NFTOffChainMetaType.OASIS:
                                request.ImageUrl = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/getdata/", imageSaveResult.Result.Id);
                                break;
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the image to the offchain provider {request.OffChainProvider.Name}. Reason: {imageSaveResult.Message}");
                }
            }

            if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                OASISResult<IHolon> jsonSaveResult = null;

                if ((request.NFTOffChainMetaType == NFTOffChainMetaType.IPFS || request.NFTOffChainMetaType == NFTOffChainMetaType.OASIS) && metaDataProviderType.Value != ProviderType.None)
                {
                    jsonSaveResult = SaveMetaDataToOASIS(request, NFTStandardType, metaDataProviderType);

                    if (!(jsonSaveResult != null && jsonSaveResult.Result != null && !jsonSaveResult.IsError))
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to the offchain provider {request.OffChainProvider.Name}. Reason: {jsonSaveResult.Message}");
                        return result;
                    }
                }

                //IMintNFTTransactionRequestForProvider providerRequest = null;

                //switch (request.NFTOffChainMetaType)
                //{
                //    case NFTOffChainMetaType.Pinata:
                //        {
                //            //TODO: Save to Pinata here...
                //            result.Message = "Pinata Support Coming Soon...";
                //            result.IsError = true;
                //            return result;
                //        }
                //        break;

                //    case NFTOffChainMetaType.IPFS:
                //        providerRequest = CreateNFTTransactionRequestForProvider(request, string.Concat(OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString, jsonSaveResult.Result.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]));
                //        break;

                //    case NFTOffChainMetaType.OASIS:
                //        providerRequest = CreateNFTTransactionRequestForProvider(request, string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/load-file/", jsonSaveResult.Result.Id));
                //        break;

                //    case NFTOffChainMetaType.ExternalJsonURL:
                //        {
                //            if (!string.IsNullOrEmpty(request.JSONMetaDataURL))
                //                providerRequest = CreateNFTTransactionRequestForProvider(request, request.JSONMetaDataURL);
                //            else
                //            {
                //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} When setting NFTOffChainMetaType to ExternalJsonURL, the JSONUrl cannot be empty!");
                //                return result;
                //            }
                //            break;
                //        }
                //}

                switch (request.NFTOffChainMetaType)
                {
                    case NFTOffChainMetaType.Pinata:
                        {
                            //TODO: Save to Pinata here...
                            result.Message = "Pinata Support Coming Soon...";
                            result.IsError = true;
                            return result;
                        }
                        break;

                    case NFTOffChainMetaType.IPFS:
                        request.JSONMetaDataURL = string.Concat(OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString, jsonSaveResult.Result.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
                        break;

                    case NFTOffChainMetaType.OASIS:
                        request.JSONMetaDataURL = string.Concat(OASISDNA.OASIS.OASISAPIURL, "/data/load-file/", jsonSaveResult.Result.Id);
                        break;

                    case NFTOffChainMetaType.ExternalJsonURL:
                        {
                            if (string.IsNullOrEmpty(request.JSONMetaDataURL))
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} When setting NFTOffChainMetaType to ExternalJsonURL, the JSONUrl cannot be empty!");
                                return result;
                            }
                            break;
                        }
                }

                if (string.IsNullOrEmpty(request.Symbol))
                    request.Symbol = "OASISNFT";

                //result = await nftProviderResult.Result.MintNFTAsync(providerRequest);
                result = nftProviderResult.Result.MintNFT(request);

                if (result != null && !result.IsError && result.Result != null)
                {
                    result.Result.OASISNFT = CreateOASISNFT(request, result.Result);

                    //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                    if (metaDataProviderType.Value == ProviderType.None)
                        metaDataProviderType.Value = ProviderType.MongoDBOASIS;

                    OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateNFTMetaDataHolon(result.Result.OASISNFT, request), request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                    if ((saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result == null)) || saveHolonResult == null)
                    {
                        result.Result = null;
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {metaDataProviderType.Name} {Enum.GetName(typeof(ProviderType), metaDataProviderType.Value)}. Reason: {saveHolonResult.Message}");
                    }
                    else
                        result.Message = FormatSuccessMessage(request, result, metaDataProviderType, responseFormatType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured minting the OASISNFT: Reason: {result.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The ImageUrl is null!");

            return result;
        }

        private string FormatSuccessMessage(IMintNFTTransactionRequest request, OASISResult<INFTTransactionRespone> response, EnumValue<ProviderType> metaDataProviderType, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, bool geoNFT = false, int colWidth = 20)
        {
            string htmlBreak = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";
                string sendNFTMessage = GenerateSendMessage(request, response.Result.SendNFTTransactionResult, colWidth);

                if (response.Result.OASISNFT.JSONMetaDataURIHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.OASISNFT.JSONMetaDataURIHolonId, " ");

                return $"Successfully minted the NFT on the {request.OnChainProvider.Name} provider with hash {response.Result.TransactionResult} and title '{request.Title}' by AvatarId {request.MintedByAvatarId} using OASIS Minting Account {response.Result.OASISNFT.OASISMintWalletAddress} for price {request.Price}. NFT Address: {response.Result.OASISNFT.NFTTokenAddress}. The OASIS metadata is stored on the {Enum.GetName(typeof(ProviderType), metaDataProviderType.Value)} provider with the id {response.Result.OASISNFT.Id} and JSON URL {response.Result.OASISNFT.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.OASISNFT.ImageUrl}, Mint Date: {response.Result.OASISNFT.MintedOn}. {sendNFTMessage}";
            }

            if (responseFormatType == ResponseFormatType.HTML)
                htmlBreak = "<br>";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Successfully minted the OASIS NFT!{htmlBreak}");
            sb.AppendLine($"{htmlBreak}");
            sb.AppendLine(string.Concat("Onchain Provider:".PadRight(colWidth), request.OnChainProvider.Name, htmlBreak));
            sb.AppendLine(string.Concat("Offchain Provider:".PadRight(colWidth), metaDataProviderType.Name, htmlBreak));
            sb.AppendLine(string.Concat("Hash:".PadRight(colWidth), response.Result.TransactionResult, htmlBreak));
            sb.AppendLine(string.Concat("Title:".PadRight(colWidth), request.Title, htmlBreak));
            sb.AppendLine(string.Concat("Description:".PadRight(colWidth), request.Description, htmlBreak));
            sb.AppendLine(string.Concat("Price:".PadRight(colWidth), request.Price, htmlBreak));
            sb.AppendLine(string.Concat("Symbol:".PadRight(colWidth), request.Symbol, htmlBreak));
            sb.AppendLine(string.Concat("NFT Standard Type:".PadRight(colWidth), Enum.GetName(typeof(NFTStandardType), request.NFTStandardType)));
            sb.AppendLine(string.Concat("Number To Mint:".PadRight(colWidth), request.NumberToMint, htmlBreak));
            sb.AppendLine(string.Concat("Minted By Avatar Id:".PadRight(colWidth), request.MintedByAvatarId, htmlBreak));
            sb.AppendLine(string.Concat("Minted Date:".PadRight(colWidth), response.Result.OASISNFT.MintedOn, htmlBreak));
            sb.AppendLine(string.Concat("OASIS Minting Account:".PadRight(colWidth), response.Result.OASISNFT.OASISMintWalletAddress, htmlBreak));
            sb.AppendLine(string.Concat("NFT Address:".PadRight(colWidth), response.Result.OASISNFT.NFTTokenAddress, htmlBreak));
            sb.AppendLine(string.Concat("OASIS NFT Id:".PadRight(colWidth), response.Result.OASISNFT.Id, htmlBreak));
            sb.AppendLine(string.Concat("JSON MetaData URL:".PadRight(colWidth), response.Result.OASISNFT.JSONMetaDataURL, htmlBreak));
            
            if (response.Result.OASISNFT.JSONMetaDataURIHolonId != Guid.Empty)
                sb.AppendLine(string.Concat("JSON MetaData URI Holon Id:".PadRight(colWidth), response.Result.OASISNFT.JSONMetaDataURIHolonId, htmlBreak));

            sb.AppendLine(string.Concat("Image URL:".PadRight(colWidth), response.Result.OASISNFT.ImageUrl, htmlBreak));
            sb.AppendLine(string.Concat("Thumbnail URL:".PadRight(colWidth), response.Result.OASISNFT.ThumbnailUrl, htmlBreak));

            GenerateSendMessage(sb, request, response.Result.TransactionResult, htmlBreak, colWidth);

            if (response.IsWarning)
                sb.AppendLine(string.Concat("Warning:".PadRight(colWidth), response.Message, htmlBreak));

            return sb.ToString();
        }

        private string FormatSuccessMessage(IMintNFTTransactionRequest request, OASISResult<IOASISGeoSpatialNFT> response, string sendNFTHash = "", ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = 20)
        {
            string htmlBreak = "";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";
                string sendNFTMessage = GenerateSendMessage(request, sendNFTHash, colWidth);

                if (response.Result.JSONMetaDataURIHolonId != Guid.Empty)
                    JSONMetaDataURIHolonId = string.Concat("JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURIHolonId, " ");

                return $"Successfully created and placed the new OASIS GeoNFT. The meta data is stored on the GeoNFTMetaDataProvider {Enum.GetName(typeof(ProviderType), response.Result.GeoNFTMetaDataProvider)} with id {response.Result.Id} and was placed by the avatar with id {response.Result.PlacedByAvatarId}. The NFT was originally minted on the {response.Result.OnChainProvider.Name} onchain provider with hash {response.Result.Hash} and title '{response.Result.Title}' by the avatar with id {response.Result.MintedByAvatarId} for the price of {response.Result.Price} using OASIS Minting Account {response.Result.OASISMintWalletAddress}. NFT Address: {response.Result.NFTTokenAddress}. The OASIS metadata for the original NFT is stored on the {response.Result.OffChainProvider.Name} offchain provider with the id {response.Result.OriginalOASISNFTId} and JSON URL {response.Result.JSONMetaDataURL}. {JSONMetaDataURIHolonId}Image URL: {response.Result.ImageUrl}, Mint Date: {response.Result.MintedOn}. {sendNFTMessage}";
            }

            if (responseFormatType == ResponseFormatType.HTML)
                htmlBreak = "<br>";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Successfully minted the OASIS Geo-NFT!{htmlBreak}");
            sb.AppendLine($"{htmlBreak}");
            sb.AppendLine(string.Concat("Onchain Provider:".PadRight(colWidth), response.Result.OnChainProvider.Name, htmlBreak));
            sb.AppendLine(string.Concat("Offchain Provider:".PadRight(colWidth), response.Result.OffChainProvider.Name, htmlBreak));
            sb.AppendLine(string.Concat("Hash:".PadRight(colWidth), response.Result.Hash, htmlBreak));
            sb.AppendLine(string.Concat("Title:".PadRight(colWidth), response.Result.Title, htmlBreak));
            sb.AppendLine(string.Concat("Description:".PadRight(colWidth), response.Result.Description, htmlBreak));
            sb.AppendLine(string.Concat("Price:".PadRight(colWidth), response.Result.Price, htmlBreak));
            sb.AppendLine(string.Concat("Symbol:".PadRight(colWidth), response.Result.Symbol, htmlBreak));
            sb.AppendLine(string.Concat("NFT Standard Type:".PadRight(colWidth), Enum.GetName(typeof(NFTStandardType), response.Result.NFTStandardType)));
            sb.AppendLine(string.Concat("Number To Mint:".PadRight(colWidth), request.NumberToMint, htmlBreak));
            sb.AppendLine(string.Concat("Minted By Avatar Id:".PadRight(colWidth), request.MintedByAvatarId, htmlBreak));
            sb.AppendLine(string.Concat("Minted Date:".PadRight(colWidth), response.Result.MintedOn, htmlBreak));
            sb.AppendLine(string.Concat("OASIS Minting Account:".PadRight(colWidth), response.Result.OASISMintWalletAddress, htmlBreak));
            sb.AppendLine(string.Concat("NFT Address:".PadRight(colWidth), response.Result.NFTTokenAddress, htmlBreak));
            sb.AppendLine(string.Concat("OASIS NFT Id:".PadRight(colWidth), response.Result.Id, htmlBreak));
            sb.AppendLine(string.Concat("JSON MetaData URL:".PadRight(colWidth), response.Result.JSONMetaDataURL, htmlBreak));

            if (response.Result.JSONMetaDataURIHolonId != Guid.Empty)
                sb.AppendLine(string.Concat("JSON MetaData URI Holon Id:".PadRight(colWidth), response.Result.JSONMetaDataURIHolonId, htmlBreak));

            sb.AppendLine(string.Concat("Image URL:".PadRight(colWidth), response.Result.ImageUrl, htmlBreak));
            sb.AppendLine(string.Concat("Thumbnail URL:".PadRight(colWidth), response.Result.ThumbnailUrl, htmlBreak));

            GenerateSendMessage(sb, request, sendNFTHash, htmlBreak, colWidth);

            if (response.IsWarning)
                sb.AppendLine(string.Concat("Warning:".PadRight(colWidth), response.Message, htmlBreak));

            return sb.ToString();
        }

        private string GenerateSendMessage(IMintNFTTransactionRequest request, string sendNFTHash, int colWidth = 20)
        {
            string sendNFTMessage = "";

            if (!string.IsNullOrEmpty(request.SendToAddressAfterMinting))
                sendNFTMessage = string.Concat("Send To Address After Minting: ", request.SendToAddressAfterMinting, ". ");

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingId.ToString()) && request.SendToAvatarAfterMintingId.ToString() != Guid.Empty.ToString())
                sendNFTMessage = string.Concat(sendNFTMessage, "Send To Avatar After Minting Id: ", request.SendToAvatarAfterMintingId, ". ");

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingUsername))
                sendNFTMessage = string.Concat(sendNFTMessage, "Send To Avatar After Minting Username: ".PadRight(colWidth), request.SendToAvatarAfterMintingUsername, ". ");

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingEmail))
                sendNFTMessage = string.Concat(sendNFTMessage, "Send To Avatar After Minting Email: ", request.SendToAvatarAfterMintingEmail, ". ");

            if (!string.IsNullOrEmpty(sendNFTHash))
                sendNFTMessage = string.Concat(sendNFTMessage, "Send NFT Hash: ", sendNFTHash, ". ");

            return sendNFTMessage;
        }

        private StringBuilder GenerateSendMessage(StringBuilder sb, IMintNFTTransactionRequest request, string sendNFTHash, string htmlBreak = "", int colWidth = 20)
        {
            if (!string.IsNullOrEmpty(request.SendToAddressAfterMinting))
                sb.AppendLine(string.Concat("Send To Address After Minting:".PadRight(colWidth), request.SendToAddressAfterMinting, htmlBreak));

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingId.ToString()) && request.SendToAvatarAfterMintingId.ToString() != Guid.Empty.ToString())
                sb.AppendLine(string.Concat("Send To Avatar After Minting Id:".PadRight(colWidth), request.SendToAvatarAfterMintingId, htmlBreak));

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingUsername))
                sb.AppendLine(string.Concat("Send To Avatar After Minting Username:".PadRight(colWidth), request.SendToAvatarAfterMintingUsername, htmlBreak));

            if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingEmail))
                sb.AppendLine(string.Concat("Send To Avatar After Minting Email:".PadRight(colWidth), request.SendToAvatarAfterMintingEmail, htmlBreak));

            if (!string.IsNullOrEmpty(sendNFTHash))
                sb.AppendLine(string.Concat("Send NFT Hash:".PadRight(colWidth), sendNFTHash, htmlBreak));

            return sb;
        }

        //public string CreateERCJson(IMintNFTTransactionRequest request, NFTStandardType NFTStandardType)
        //    => NFTStandardType switch
        //    {
        //        NFTStandardType.ERC721 => CreateERC721Json(request),
        //        NFTStandardType.ERC1155 => CreateERC1155Json(request),
        //        NFTStandardType.Metaplex => CreateMetaplexJson(request),
        //        _ => "",
        //    };


        private OASISResult<IHolon> SaveMetaDataToOASIS(IMintNFTTransactionRequest request, NFTStandardType NFTStandardType, EnumValue<ProviderType> metaDataProviderType)
        {
            OASISResult<IHolon> jsonSaveResult = Data.SaveHolon(new Holon()
            {
                MetaData = new Dictionary<string, object>()
                            {
                                { "data",  CreateERCJson(request, NFTStandardType) }
                            }
            }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

            //if (jsonSaveResult != null && jsonSaveResult.Result != null && !jsonSaveResult.IsError)
            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to the offchain provider {request.OffChainProvider.Name}. Reason: {jsonSaveResult.Message}");

            return jsonSaveResult;
        }

        private async Task<OASISResult<IHolon>> SaveMetaDataToOASISAsync(IMintNFTTransactionRequest request, NFTStandardType NFTStandardType, EnumValue<ProviderType> metaDataProviderType)
        {
            OASISResult<IHolon> jsonSaveResult = await Data.SaveHolonAsync(new Holon()
            {
                MetaData = new Dictionary<string, object>()
                            {
                                { "data",  CreateERCJson(request, NFTStandardType) }
                            }
            }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

            //if (jsonSaveResult != null && jsonSaveResult.Result != null && !jsonSaveResult.IsError)
            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the JSON metadata to the offchain provider {request.OffChainProvider.Name}. Reason: {jsonSaveResult.Message}");

            return jsonSaveResult;
        }

        public string CreateERCJson(IMintNFTTransactionRequest request, NFTStandardType NFTStandardType)
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

            return JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
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

            return JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
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

            return JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
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
                Hash = mintNFTResponse.TransactionResult,
                SellerFeeBasisPoints = mintNFTResponse.OASISNFT != null ? mintNFTResponse.OASISNFT.SellerFeeBasisPoints : 0,
                MetaData = mintNFTResponse.OASISNFT != null ? mintNFTResponse.OASISNFT.MetaData : null,
                OASISMintWalletAddress = mintNFTResponse.OASISNFT != null ? mintNFTResponse.OASISNFT.OASISMintWalletAddress : null,
                UpdateAuthority = mintNFTResponse.OASISNFT != null ? mintNFTResponse.OASISNFT.UpdateAuthority : null,
                NFTTokenAddress = mintNFTResponse.OASISNFT != null ? mintNFTResponse.OASISNFT.NFTTokenAddress : null, //TODO: Need to pull this from the provider mint functions...
                MintedByAvatarId = request.MintedByAvatarId,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
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

        private OASISGeoSpatialNFT CreateGeoSpatialNFT(IPlaceGeoSpatialNFTRequest request, IOASISNFT originalNftMetaData)
        {
            return new OASISGeoSpatialNFT()
            {
                //Id = request.OASISNFTId != Guid.Empty ? request.OASISNFTId : Guid.NewGuid(),
                //Id = request.OASISNFTId,
                Id = Guid.NewGuid(),  //The NFT could be placed many times so we need a new ID for each time
                OriginalOASISNFTId = request.OriginalOASISNFTId, //We need to link back to the orignal NFT (but we copy across the NFT properties making it quicker and easier to get at the data). TODO: Do we want to copy the data across? Pros and Cons? Need to think about this... for now it's fine... ;-)
                GeoNFTMetaDataProvider = new EnumValue<ProviderType>(request.GeoNFTMetaDataProvider),
                //OriginalOASISNFTProviderType = request.OriginalOASISNFTOffChainProviderType,
                Hash = originalNftMetaData.Hash,
                JSONMetaDataURL = originalNftMetaData.JSONMetaDataURL,
                OASISMintWalletAddress = originalNftMetaData.OASISMintWalletAddress,
                MintedByAvatarId = originalNftMetaData.MintedByAvatarId,
                SendToAddressAfterMinting = originalNftMetaData.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = originalNftMetaData.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = originalNftMetaData.SendToAvatarAfterMintingUsername,
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

        private IHolon CreateNFTMetaDataHolon(IOASISNFT nftMetaData, IMintNFTTransactionRequest request)
        {
            IHolon holonNFT = new Holon(HolonType.NFT);
            //holonNFT.Id = result.Result.OASISNFT.OffChainProviderHolonId;
            holonNFT.Id = nftMetaData.Id;
            //holonNFT.CustomKey = nftMetaData.Hash;
            //holonNFT.MetaData["OnChainNFTHash"] = nftMetaData.Hash;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} NFT Minted On The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.OASISNFT"] = JsonSerializer.Serialize(nftMetaData); //TODO: May remove this because its duplicated data. BUT we may need this for other purposes later such as exporting it to a file etc (but then we could just serialaize it there and then).
            holonNFT.MetaData["NFT.Hash"] = nftMetaData.Hash;
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.MintedByAvatarId"] = nftMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["NFT.OASISMintWalletAddress"] = nftMetaData.OASISMintWalletAddress;
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingId"] = nftMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingUsername"] = nftMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["NFT.SendToAddressAfterMinting"] = nftMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["NFT.NFTTokenAddress"] = nftMetaData.NFTTokenAddress;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = nftMetaData.Price.ToString();
            holonNFT.MetaData["NFT.Discount"] = nftMetaData.Discount.ToString();
            holonNFT.MetaData["NFT.NumberToMint"] = request.NumberToMint.ToString();
            holonNFT.MetaData["NFT.OnChainProvider"] = nftMetaData.OnChainProvider.Name;
            holonNFT.MetaData["NFT.OffChainProvider"] = nftMetaData.OffChainProvider.Name;
            holonNFT.MetaData["NFT.StoreNFTMetaDataOnChain"] = nftMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["NFT.NFTOffChainMetaType"] = Enum.GetName(typeof(NFTOffChainMetaType), nftMetaData.NFTOffChainMetaType);
            holonNFT.MetaData["NFT.NFTStandardType"] = Enum.GetName(typeof(NFTStandardType), nftMetaData.NFTStandardType);
            holonNFT.MetaData["NFT.Symbol"] = nftMetaData.Symbol;
            holonNFT.MetaData["NFT.Image"] = nftMetaData.Image;
            holonNFT.MetaData["NFT.ImageUrl"] = nftMetaData.ImageUrl;
            holonNFT.MetaData["NFT.Thumbnail"] = nftMetaData.Thumbnail;
            holonNFT.MetaData["NFT.ThumbnailUrl"] = nftMetaData.ThumbnailUrl;
            holonNFT.MetaData["NFT.JSONMetaDataURL"] = nftMetaData.JSONMetaDataURL;
            holonNFT.MetaData["NFT.JSONMetaDataURIHolonId"] = nftMetaData.JSONMetaDataURIHolonId;
            holonNFT.MetaData["NFT.MintedOn"] = nftMetaData.MintedOn.ToShortDateString();
            holonNFT.MetaData["NFT.SellerFeeBasisPoints"] = nftMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["NFT.UpdateAuthority"] = nftMetaData.UpdateAuthority;
            holonNFT.MetaData["NFT.MetaData"] = JsonSerializer.Serialize(nftMetaData.MetaData);
            holonNFT.ParentHolonId = nftMetaData.MintedByAvatarId;

            return holonNFT;
        }

        private IHolon CreateGeoSpatialNFTMetaDataHolon(IOASISGeoSpatialNFT geoNFTMetaData)
        {
            IHolon holonNFT = new Holon(HolonType.GeoNFT);
            //holonNFT.Id = result.Result.OASISNFT.OffChainProviderHolonId;
            holonNFT.Id = geoNFTMetaData.Id;
            holonNFT.Name = "OASIS GEO NFT"; // $"{Enum.GetName(typeof(ProviderType), request.OnChainProvider)} NFT Minted On The OASIS with title {request.Title}";
            holonNFT.Description = "OASIS GEO NFT";
            holonNFT.MetaData["GEONFT.OASISGEONFT"] = JsonSerializer.Serialize(geoNFTMetaData); //TODO: May remove this because its duplicated data.
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
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Hash"] = geoNFTMetaData.Hash;
            //holonNFT.MetaData["GEONFT.OriginalOASISNFT.MemoText"] = geoNFTMetaData.MetaData["MemoText"];
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MemoText"] = geoNFTMetaData.MemoText;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Title"] = geoNFTMetaData.Title;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Description"] = geoNFTMetaData.Description;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MintedByAvatarId"] = geoNFTMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.OASISMintWalletAddress"] = geoNFTMetaData.OASISMintWalletAddress;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingId"] = geoNFTMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingUsername"] = geoNFTMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAddressAfterMinting"] = geoNFTMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.NFTTokenAddress"] = geoNFTMetaData.NFTTokenAddress;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Price"] = geoNFTMetaData.Price.ToString();
            //holonNFT.MetaData["GEONFT.NumberToMint"] = geoNFTMetaData.NumberToMint.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.OnChainProvider"] = geoNFTMetaData.OnChainProvider.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.OffChainProvider"] = geoNFTMetaData.OffChainProvider.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.StoreNFTMetaDataOnChain"] = geoNFTMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.NFTOffChainMetaType"] = Enum.GetName(typeof(NFTOffChainMetaType), geoNFTMetaData.NFTOffChainMetaType);
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.NFTStandardType"] = Enum.GetName(typeof(NFTStandardType), geoNFTMetaData.NFTStandardType);
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Symbol"] = geoNFTMetaData.Symbol;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Image"] = geoNFTMetaData.Image;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.ImageUrl"] = geoNFTMetaData.ImageUrl;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Thumbnail"] = geoNFTMetaData.Thumbnail;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.ThumbnailUrl"] = geoNFTMetaData.ThumbnailUrl;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.JSONMetaDataURL"] = geoNFTMetaData.JSONMetaDataURL;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.JSONMetaDataURIHolonId"] = geoNFTMetaData.JSONMetaDataURIHolonId;
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
                result.Result = (IOASISNFT)JsonSerializer.Deserialize(holonResult.Result.MetaData["NFT.OASISNFT"].ToString(), typeof(OASISNFT));
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonResult.Message}");

            return result;
        }

        private OASISResult<IOASISGeoSpatialNFT> DecodeGeoNFTMetaData(OASISResult<IHolon> holonResult, OASISResult<IOASISGeoSpatialNFT> result, string errorMessage)
        {
            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                result.Result = (OASISGeoSpatialNFT)JsonSerializer.Deserialize(holonResult.Result.MetaData["GEONFT.OASISGEONFT"].ToString(), typeof(OASISGeoSpatialNFT));
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
                        nfts.Add((IOASISNFT)JsonSerializer.Deserialize(holon.MetaData["NFT.OASISNFT"].ToString(), typeof(OASISNFT)));

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
                        nfts.Add((IOASISGeoSpatialNFT)JsonSerializer.Deserialize(holon.MetaData["GEONFT.OASISGEONFT"].ToString(), typeof(OASISGeoSpatialNFT)));

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