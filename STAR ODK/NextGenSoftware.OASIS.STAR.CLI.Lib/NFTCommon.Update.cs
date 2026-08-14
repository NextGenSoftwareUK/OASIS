using ADRaffy.ENSNormalize;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.Utilities;
using Newtonsoft.Json;
using Solnet.Rpc.Models;
using System.IO;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class NFTCommon
    {
        public async Task<IRemintWeb4NFTRequest> GenerateWeb4NFTRemintRequestAsync(IWeb4NFT web4NFT)
        {
            int numberToMint = CLIEngine.GetValidInputForInt("How many additional WEB3 NFT's do you wish to mint for this WEB4 OASIS NFT? ");

            MintWeb4NFTRequest request = new MintWeb4NFTRequest()
            {
                MintedByAvatarId = web4NFT.MintedByAvatarId,
                Title = web4NFT.Title,
                Description = web4NFT.Description,
                MemoText = web4NFT.MemoText,
                Price = web4NFT.Price,
                Discount = web4NFT.Discount,
                RoyaltyPercentage = web4NFT.RoyaltyPercentage,
                IsForSale = web4NFT.IsForSale,
                SaleStartDate = web4NFT.SaleStartDate,
                SaleEndDate = web4NFT.SaleEndDate,
                OnChainProvider = web4NFT.OnChainProvider,
                OffChainProvider = web4NFT.OffChainProvider,
                StoreNFTMetaDataOnChain = web4NFT.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = web4NFT.NFTOffChainMetaType,
                NFTStandardType = web4NFT.NFTStandardType,
                Image = web4NFT.Image,
                ImageUrl = web4NFT.ImageUrl,
                Thumbnail = web4NFT.Thumbnail,
                ThumbnailUrl = web4NFT.ThumbnailUrl,
                MetaData = web4NFT.MetaData,
                Tags = web4NFT.Tags,
                JSONMetaData = web4NFT.JSONMetaData,
                JSONMetaDataURL = web4NFT.JSONMetaDataURL,
                Symbol = web4NFT.Symbol,
                NumberToMint = numberToMint
            };

            request = GetSendAndAdvancedOptions(request) as MintWeb4NFTRequest;
            request.Web3NFTs = await GenerateWeb3NFTRequestsAsync(request, true);

            return new RemintWeb4NFTRequest()
            {
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                WaitTillNFTSent = request.WaitTillNFTSent,
                Web3NFTs = request.Web3NFTs,
                Web4NFT = web4NFT
            };
        }

        public async Task<IRemintWeb4GeoNFTRequest> GenerateWeb4GeoNFTRemintRequestAsync(IWeb4GeoSpatialNFT web4GeoNFT)
        {
            int numberToMint = CLIEngine.GetValidInputForInt("How many additional WEB3 NFT's do you wish to mint for this WEB4 OASIS Geo-NFT? ");

            MintWeb4NFTRequest request = new MintWeb4NFTRequest()
            {
                MintedByAvatarId = web4GeoNFT.MintedByAvatarId,
                Title = web4GeoNFT.Title,
                Description = web4GeoNFT.Description,
                MemoText = web4GeoNFT.MemoText,
                Price = web4GeoNFT.Price,
                Discount = web4GeoNFT.Discount,
                RoyaltyPercentage = web4GeoNFT.RoyaltyPercentage,
                IsForSale = web4GeoNFT.IsForSale,
                SaleStartDate = web4GeoNFT.SaleStartDate,
                SaleEndDate = web4GeoNFT.SaleEndDate,
                OnChainProvider = web4GeoNFT.OnChainProvider,
                OffChainProvider = web4GeoNFT.OffChainProvider,
                StoreNFTMetaDataOnChain = web4GeoNFT.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = web4GeoNFT.NFTOffChainMetaType,
                NFTStandardType = web4GeoNFT.NFTStandardType,
                Image = web4GeoNFT.Image,
                ImageUrl = web4GeoNFT.ImageUrl,
                Thumbnail = web4GeoNFT.Thumbnail,
                ThumbnailUrl = web4GeoNFT.ThumbnailUrl,
                MetaData = web4GeoNFT.MetaData,
                Tags = web4GeoNFT.Tags,
                JSONMetaData = web4GeoNFT.JSONMetaData,
                JSONMetaDataURL = web4GeoNFT.JSONMetaDataURL,
                Symbol = web4GeoNFT.Symbol,
                NumberToMint = numberToMint
            };

            request = GetSendAndAdvancedOptions(request) as MintWeb4NFTRequest;
            request.Web3NFTs = await GenerateWeb3NFTRequestsAsync(request);

            return new RemintWeb4GeoNFTRequest()
            {
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                WaitTillNFTSent = request.WaitTillNFTSent,
                Web3NFTs = request.Web3NFTs,
                Web4GeoNFT = web4GeoNFT
            };
        }

        //public async Task<List<IMintWeb3NFTRequest>> GenerateWeb3NFTRequestsAsync(IMintWeb4NFTRequest request, int numberToMint = 1)
        //{
        //    return await GenerateWeb3NFTRequestsAsync(request);
        //}

        public async Task<IImportWeb3NFTRequest> GenerateImportNFTRequestAsync()
        {
            ImportWeb3NFTRequest request = new ImportWeb3NFTRequest();

            request.NFTTokenAddress = CLIEngine.GetValidInput("Please enter the token address of the NFT you wish to import: ");
            request.ImportedByAvatarId = STAR.BeamedInAvatar.Id;
            request.Title = CLIEngine.GetValidInput("What is the NFT's title?");
            request.Description = CLIEngine.GetValidInput("What is the NFT's description?");
            request.MemoText = CLIEngine.GetValidInput("What is the NFT's memotext? (optional)");

            if (CLIEngine.GetConfirmation("Do you want to upload a local image on your device to represent the NFT or input a URI to an online image? (Press Y for local or N for online)"))
            {
                Console.WriteLine("");
                string localImagePath = CLIEngine.GetValidFile("What is the full path to the local image you want to represent the NFT?");
                request.Image = File.ReadAllBytes(localImagePath);
            }
            else
            {
                Console.WriteLine("");
                request.ImageUrl = CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT?").Result.AbsoluteUri;
            }


            if (CLIEngine.GetConfirmation("Do you want to upload a local image on your device to represent the NFT Thumbnail or input a URI to an online image? (Press Y for local or N for online)"))
            {
                Console.WriteLine("");
                string localImagePath = CLIEngine.GetValidFile("What is the full path to the local image you want to represent the NFT Thumbnail?");
                request.Thumbnail = File.ReadAllBytes(localImagePath);
            }
            else
            {
                Console.WriteLine("");
                request.ThumbnailUrl = CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT Thumbnail?").Result.AbsoluteUri;
            }

            request.Price = CLIEngine.GetValidInputForLong("What is the price for the NFT?");

            if (CLIEngine.GetConfirmation("Is there any discount for the NFT? (This can always be changed later)"))
            {
                Console.WriteLine("");
                request.Discount = CLIEngine.GetValidInputForLong("What is the discount?");
            }
            else
                Console.WriteLine("");

            object onChainProviderObj = CLIEngine.GetValidInputForEnum("What on-chain provider did you use to mint on?", typeof(ProviderType));
            request.OnChainProvider = new EnumValue<ProviderType>((ProviderType)onChainProviderObj);

            request.StoreNFTMetaDataOnChain = CLIEngine.GetConfirmation("Was the NFT metadata stored on-chain or off-chain? (Press Y for on-chain or N for off-chain)");
            Console.WriteLine("");

            //if (!request.StoreNFTMetaDataOnChain)
            //{
                object offChainMetaDataTypeObj = CLIEngine.GetValidInputForEnum("How do you wish to store the offchain WEB4 OASIS NFT meta data/image? OASIS, IPFS, Pinata or External JSON URI (for the last option you will need to generate the meta data yourself and host somewhere like Pinata and then enter the URI, for the first three options the metadata will be generated automatically)? If you choose OASIS, it will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive. If you choose OASIS and then IPFSOASIS for the next question for the OASIS Provider it will store it on IPFS via The OASIS and then benefit from the OASIS HyperDrive feature to provide more reliable service and up-time etc. If you choose IPFS or Pinata for this question then it will store it directly on IPFS/Pinata without any additional benefits of The OASIS.", typeof(NFTOffChainMetaType));
                request.NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>((NFTOffChainMetaType)offChainMetaDataTypeObj);

                if (request.NFTOffChainMetaType.Value == NFTOffChainMetaType.OASIS)
                {
                    object offChainProviderObj = CLIEngine.GetValidInputForEnum("What OASIS off-chain provider do you wish to store the metadata on? (NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive)", typeof(ProviderType));
                    request.OffChainProvider = new EnumValue<ProviderType>((ProviderType)offChainProviderObj);
                }
                else if (request.NFTOffChainMetaType.Value == NFTOffChainMetaType.ExternalJSONURL)
                {
                    Uri uriResult = await CLIEngine.GetValidURIAsync("What is the URI to the JSON meta data you have created for this NFT?");
                    request.JSONMetaDataURL = uriResult.AbsoluteUri;
                }
            //}

            bool validStandard = false;
            do
            {
                object nftStandardObj = CLIEngine.GetValidInputForEnum("What NFT standard did you use? ERC721, ERC1155 or SPL? (ERC standards are only supported by EVM chains such as EthereumOASIS, PolygonsOASIS & ArbitrumOASIS. SPL is only supported by SolanaOASIS)", typeof(NFTStandardType));
                request.NFTStandardType = new EnumValue<NFTStandardType>((NFTStandardType)nftStandardObj);

                OASISResult<bool> nftStandardValid = NFTManager.IsNFTStandardTypeValid(request.NFTStandardType.Value, request.OnChainProvider.Value);

                if (!nftStandardValid.IsError)
                    validStandard = true;

            } while (!validStandard);


            request.MetaData = MetaDataHelper.AddMetaData("NFT");
            return request;
        }

        public OASISResult<ImageAndThumbnail> ProcessImageAndThumbnail(string itemName)
        {
            OASISResult<ImageAndThumbnail> result = new OASISResult<ImageAndThumbnail>(new ImageAndThumbnail());

            if (CLIEngine.GetConfirmation($"Do you want to upload a local image on your device to represent the {itemName} or input a URI to an online image? (Press Y for local or N for online)"))
            {
                Console.WriteLine("");
                string localImagePath = CLIEngine.GetValidFile($"What is the full path to the local image you want to represent the {itemName}?");
                result.Result.Image = File.ReadAllBytes(localImagePath);
            }
            else
            {
                Console.WriteLine("");
                result.Result.ImageUrl = CLIEngine.GetValidURIAsync("What is the URI to the image you want to represent the NFT?").Result.AbsoluteUri;
            }


            if (CLIEngine.GetConfirmation($"Do you want to upload a local image on your device to represent the {itemName} Thumbnail or input a URI to an online image? (Press Y for local or N for online)"))
            {
                Console.WriteLine("");
                string localImagePath = CLIEngine.GetValidFile($"What is the full path to the local image you want to represent the {itemName} Thumbnail?");
                result.Result.Thumbnail = File.ReadAllBytes(localImagePath);
            }
            else
            {
                Console.WriteLine("");
                result.Result.ThumbnailUrl = CLIEngine.GetValidURIAsync($"What is the URI to the image you want to represent the {itemName} Thumbnail?").Result.AbsoluteUri;
            }

            return result;
        }

        public async Task<OASISResult<IUpdateWeb4NFTRequest>> UpdateWeb4NFTAsync(IUpdateWeb4NFTRequest request, IWeb4NFT nft, string displayName, bool updateTags = true, bool updateMetaData = true)
        {
            OASISResult<IUpdateWeb4NFTRequest> result = new OASISResult<IUpdateWeb4NFTRequest>();

            request.Id = nft.Id;
            request.ModifiedByAvatarId = STAR.BeamedInAvatar.Id;

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Title? (currently is: {nft.Title})"))
                request.Title = CLIEngine.GetValidInput("Please enter the new title: ", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Description? (currently is: {nft.Description})"))
                request.Description = CLIEngine.GetValidInput("Please enter the new description: ", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Do you wish to update the Image and Thumbnail?"))
            {
                Console.WriteLine("");
                OASISResult<ImageAndThumbnail> imageAndThumbnailResult = ProcessImageAndThumbnail(displayName);

                if (imageAndThumbnailResult != null && imageAndThumbnailResult.Result != null && !imageAndThumbnailResult.IsError)
                {
                    request.Image = imageAndThumbnailResult.Result.Image;
                    request.ImageUrl = imageAndThumbnailResult.Result.ImageUrl;
                    request.Thumbnail = imageAndThumbnailResult.Result.Thumbnail;
                    request.ThumbnailUrl = imageAndThumbnailResult.Result.ThumbnailUrl;
                }
                else
                {
                    string msg = imageAndThumbnailResult != null ? imageAndThumbnailResult.Message : "";
                    CLIEngine.ShowErrorMessage($"Error Occured Processing Image and Thumbnail: {msg}");
                    return result;
                    //OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail: {msg}");
                    //return result;
                }
            }
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Price? (currently is: {nft.Price})"))
            {
                Console.WriteLine("");
                request.Price = CLIEngine.GetValidInputForDecimal("Please enter the new Price: ", addLineBefore: false);
            }
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Discount? (currently is: {nft.Discount})"))
            {
                Console.WriteLine("");
                request.Discount = CLIEngine.GetValidInputForDecimal("Please enter the new Discount: ", addLineBefore: false);
            }
            else
                Console.WriteLine("");

            // Allow editing additional NFT-specific fields
            if (CLIEngine.GetConfirmation($"Do you wish to edit the Royalty Percentage? (currently is: {nft.RoyaltyPercentage})"))
                request.RoyaltyPercentage = CLIEngine.GetValidInputForInt("Please enter the Royalty Percentage (integer): ", false, addLineBefore: true);
            else
                Console.WriteLine("");
            //if (CLIEngine.GetConfirmation("Do you wish to edit the Previous Owner Avatar Id?"))
            //    request.PreviousOwnerAvatarId = CLIEngine.GetValidInputForGuid("Please enter the Previous Owner Avatar Id (GUID): ");

            //if (CLIEngine.GetConfirmation("Do you wish to edit the Current Owner Avatar Id?"))
            //    request.CurrentOwnerAvatarId = CLIEngine.GetValidInputForGuid("Please enter the Current Owner Avatar Id (GUID): ");

            if (CLIEngine.GetConfirmation($"Do you wish to change the sale status (Is For Sale)? (currently is: {nft.IsForSale})"))
            {
                Console.WriteLine("");
                SalesInfo salesInfo = UpdateSalesInfo(new SalesInfo() { IsForSale = nft.IsForSale, SaleStartDate = nft.SaleStartDate, SaleEndDate = nft.SaleEndDate });
                
                request.IsForSale = salesInfo.IsForSale;
                request.SaleStartDate = salesInfo.SaleStartDate;
                request.SaleEndDate = salesInfo.SaleEndDate;
            }
            else
                Console.WriteLine("");


            if (updateTags)
                request.Tags = TagHelper.ManageTags(nft.Tags);

            if (updateMetaData)
                request.MetaData = MetaDataHelper.ManageMetaData(nft.MetaData, displayName);

            request.UpdateAllChildWeb3NFTs = CLIEngine.GetConfirmation("Do you wish to apply these edits to all child WEB3 NFT's contained inside this WEB4 OASIS NFT? NOTE: This will override any varients you created!");

            if (!request.UpdateAllChildWeb3NFTs)
            {
                if (request.UpdateChildWebNFTIds == null)
                    request.UpdateChildWebNFTIds = new List<string>();

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to apply these edits to a selection of child WEB3 NFT's contained inside this WEB4 OASIS NFT? NOTE: This will override any varients you created!"))
                {
                    do
                    {
                        Console.WriteLine("");
                        OASISResult<IWeb3NFT> web3NFTResult = await FindWeb3NFTAsync("edit", request.Id);

                        if (web3NFTResult != null && web3NFTResult.Result != null && !web3NFTResult.IsError)
                            request.UpdateChildWebNFTIds.Add(web3NFTResult.Result.Id.ToString());

                    } while (CLIEngine.GetConfirmation("Do you wish to apply this edit to any other WEB3 NFT's?"));
                }
            }

            result.Result = request;
            return result;
        }

        public async Task<OASISResult<IWeb3NFT>> FindWeb3NFTAsync(string operationName, Guid parentWeb4NFTId = default, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "WEB3 NFT", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IWeb3NFT>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTManager.LoadAllWeb3NFTsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, parentWeb4NFTId, providerType);
                        else
                            starHolonsResult = await NFTManager.LoadAllWeb3NFTsAsync(parentWeb4NFTId, providerType);

                        ListWeb3NFTs(starHolonsResult);

                        if (!(starHolonsResult != null && starHolonsResult.Result != null && !starHolonsResult.IsError && starHolonsResult.Result.Count() > 0))
                            cont = false;
                    }
                    else
                        Console.WriteLine("");

                    if (cont)
                        idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {UIName} you wish to {operationName}?");
                    else
                    {
                        idOrName = "nonefound";
                        break;
                    }

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {UIName}...");
                    result = await NFTManager.LoadWeb3NftAsync(id, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.MintedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was minted by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    Dictionary<string, string> metaData = null;

                    if (parentWeb4NFTId != Guid.Empty)
                        metaData = new Dictionary<string, string>() { { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() } };
                
                    OASISResult<IEnumerable<IWeb3NFT>> searchResults = await NFTManager.SearchWeb3NFTsAsync(idOrName, STAR.BeamedInAvatar.Id, default, metaData, MetaKeyValuePairMatchMode.All, showOnlyForCurrentAvatar, providerType: providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListWeb3NFTs(searchResults, true);

                            if (CLIEngine.GetConfirmation("Are any of these correct?"))
                            {
                                Console.WriteLine("");

                                do
                                {
                                    int number = CLIEngine.GetValidInputForInt($"What is the number of the {UIName} you wish to {operationName}?");

                                    if (number > 0 && number <= searchResults.Result.Count())
                                        result.Result = searchResults.Result.ElementAt(number - 1);
                                    else
                                        CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");

                                } while (result.Result == null || result.IsError);
                            }
                            else
                            {
                                Console.WriteLine("");
                                idOrName = "";
                            }
                        }
                        else if (searchResults.Result.Count() == 1)
                            result.Result = searchResults.Result.FirstOrDefault();
                        else
                        {
                            idOrName = "";
                            CLIEngine.ShowWarningMessage($"No {UIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARNETManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null)
                    ShowWeb3NFT(result.Result);

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {UIName}?"))
                    {

                    }
                    else
                    {
                        Console.WriteLine("");
                        result.Result = default;
                        idOrName = "";

                        if (!CLIEngine.GetConfirmation($"Do you wish to search for another {UIName}?"))
                        {
                            idOrName = "exit";
                            break;
                        }
                    }

                    Console.WriteLine("");
                }

                idOrName = "";
            }
            while (result.Result == null || result.IsError);

            if (idOrName == "exit")
            {
                result.IsError = true;
                result.Message = "User Exited";
            }
            else if (idOrName == "nonefound")
            {
                result.IsError = true;
                result.Message = "None Found";
            }

            return result;
        }

    }
}
