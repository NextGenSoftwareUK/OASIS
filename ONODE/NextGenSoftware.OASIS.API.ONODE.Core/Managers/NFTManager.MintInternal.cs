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
        private async Task<OASISResult<IWeb4NFT>> MintWeb3NFTsAsync(OASISResult<IWeb4NFT> result, IMintWeb4NFTRequest request, IMintWeb3NFTRequest web3Request = null, IWeb4NFT existingWeb4NFT = null, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, bool isLastWeb3NFT = false)
        {
            IMintWeb4NFTRequest originalWeb4Request = CloneWeb4NFTRequest(request);

            //Web3 Request overrides web4 (optional).
            if (web3Request != null)
            {
                if (!string.IsNullOrEmpty(web3Request.CollectionPublicKey))
                    request.CollectionPublicKey = web3Request.CollectionPublicKey;

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
                        request.MetaData = new Dictionary<string, string>();

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
                    web3Request.CollectionPublicKey = request.CollectionPublicKey;
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
                    // DISABLED: see SolanaService.cs — RevokeTokenAuthorities is a no-op on Metaplex NFTs.
                    // web3Request.RevokeTokenAuthorities = request.RevokeTokenAuthorities;
                    web3Request.FreezeMetadata = request.FreezeMetadata;

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

                mergedRequest.JSONMetaData = json;

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
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} When setting NFTOffChainMetaType to ExternalJsonURL, the JSONMetaDataURL cannot be empty!");
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
                if (mergedRequest.NumberToMint <= 0)
                    mergedRequest.NumberToMint = 1;

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

                                //if (!string.IsNullOrEmpty(currentWeb3NFT.MintTransactionHash))
                                    currentWeb3NFT.MintTransactionHash = mintResult.Result.TransactionResult;

                                currentWeb3NFT.VerifyCollectionTransactionHash = mintResult.Result.VerifyCollectionTransactionHash;

                                if (jsonSaveResult != null)
                                {
                                    currentWeb3NFT.JSONMetaDataURLHolonId = jsonSaveResult.Result.Id;
                                    currentWeb3NFT.JSONMetaData = jsonSaveResult.Result.MetaData["data"].ToString();
                                }
                                else
                                    currentWeb3NFT.JSONMetaData = mergedRequest.JSONMetaData;

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

                    if (!string.IsNullOrEmpty(currentWeb3NFT.MintTransactionHash) && !currentWeb3NFT.MintTransactionHash.ToLower().Contains("error") && !string.IsNullOrEmpty(mergedRequest.SendToAddressAfterMinting))
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
                                OASISErrorHandling.HandleWarning(ref result, mintErrorMessage, onlyLogToInnerMessages: true);

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
                                OASISErrorHandling.HandleWarning(ref result, mintErrorMessage, onlyLogToInnerMessages: true);
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
                                if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("{{{newnft}}}"))
                                    web3NFT.MetaData.Remove("{{{newnft}}}");
                            }
                        }
                    }

                    currentWeb3NFT.ParentWeb4NFTId = result.Result.Id;
                    result.Result.Web3NFTs.Add((Web3NFT)UpdateWeb3NFT(currentWeb3NFT, web3Request));

                    IHolon web3NFTHolon = CreateWeb3NFTMetaDataHolon(currentWeb3NFT, result.Result.Id, web3Request);
                    OASISResult<IHolon> saveHolonResult = null;

                    //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                    if (metaDataProviderType.Value == ProviderType.None)
                        metaDataProviderType.Value = ProviderType.MongoDBOASIS;

                    //TODO: Do we want to still save the holon even if it did not mint?!
                    //TODO: After the FormatSuccessMessage call below we need to remove the web3nft from the parent web4 nft (otherwise there wont be a matching holon fo it and could cause issues later?)
                    if (!currentWeb3NFT.MintTransactionHash.ToLower().Contains("error"))
                    {
                        saveHolonResult = await Data.SaveHolonAsync(web3NFTHolon, web3Request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                        if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                            LoggingManager.Log($"[NFTManager] Web3 NFT holon saved successfully. Id: {saveHolonResult.Result.Id}, Provider: {metaDataProviderType.Name}", LogType.Info);
                        else
                            // Non-fatal in a batch — log as warning so IsError is not set and the batch continues; error is captured in InnerMessages and included in the final response.
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured saving the WEB3 NFT metadata holon to the {metaDataProviderType.Name} {Enum.GetName(typeof(ProviderType), metaDataProviderType.Value)}. Reason: {saveHolonResult?.Message}", onlyLogToInnerMessages: true);
                    }

                    //Important to set this AFTER we save the holon so its not persited! ;-)
                    currentWeb3NFT.MetaData["{{{newnft}}}"] = "true";

                    //Check if this is the last Web3 NFT to mint. If so then we can save the Holon otherwise we wait till the final one to save.
                    if (isLastWeb3NFT)
                    {
                        IHolon webNFTHolon = null;
                        List<IWeb3NFT> newlyMintedNFTs = new List<IWeb3NFT>();

                        // Temp remove the metadata so it's not persisted on the Web4 NFT Holon.
                        var web3NftsToScan = existingWeb4NFT?.Web3NFTs ?? result.Result?.Web3NFTs;
                        if (web3NftsToScan != null)
                        {
                            foreach (IWeb3NFT web3NFT in web3NftsToScan)
                            {
                                if (web3NFT.MetaData != null && web3NFT.MetaData.ContainsKey("{{{newnft}}}"))
                                {
                                    newlyMintedNFTs.Add(web3NFT);
                                    web3NFT.MetaData.Remove("{{{newnft}}}");
                                }
                            }
                        }

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

                        //TODO: Do we want to still save the holon even if none of it's child web3 NFT's minted?!
                        if (result.SavedCount > 0)
                        {
                            saveHolonResult = await Data.SaveHolonAsync(webNFTHolon, originalWeb4Request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);

                            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                            {
                                LoggingManager.Log($"[NFTManager] Web4 NFT holon saved successfully. Id: {saveHolonResult.Result.Id}, Provider: {metaDataProviderType.Name}", LogType.Info);
                                // Preserve any prior IsError/IsWarning from Web3 holon saves — don't reset to false.
                                result.IsError = result.IsError || result.SavedCount == 0;
                                result.Message = FormatSuccessMessage(mergedRequest, result, metaDataProviderType, newlyMintedNFTs, responseFormatType);
                            }
                            else
                            {
                                // Non-fatal: Web3 NFTs minted OK but Web4 holon failed — log as warning to preserve batch results.
                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured saving the WEB4 NFT metadata holon to the {metaDataProviderType.Name} {Enum.GetName(typeof(ProviderType), metaDataProviderType.Value)}. Reason: {saveHolonResult?.Message}", onlyLogToInnerMessages: true);
                                result.IsError = result.IsError || result.SavedCount == 0;
                                result.Message = FormatSuccessMessage(mergedRequest, result, metaDataProviderType, newlyMintedNFTs, responseFormatType);
                            }
                        }
                        else
                        {
                            // Nothing was minted — preserve any accumulated errors and mark as error.
                            result.IsError = result.IsError || result.SavedCount == 0;
                            result.Message = FormatSuccessMessage(mergedRequest, result, metaDataProviderType, newlyMintedNFTs, responseFormatType);
                        }

                        if (result.Result != null)
                            result.Result.NewlyMintedWeb3NFTs = newlyMintedNFTs; // Used for returning newly minted Web3 NFTs only (not persisted).
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} MintTransactionHash is null!");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The ImageUrl is null!");

            return result;
        }

    }
}