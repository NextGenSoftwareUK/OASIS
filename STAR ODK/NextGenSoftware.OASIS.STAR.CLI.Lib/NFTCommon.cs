using ADRaffy.ENSNormalize;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class NFTCommon
    {
        public NFTManager NFTManager { get; set; } = new NFTManager(STAR.BeamedInAvatar.Id);

        public async Task<IMintWeb4NFTRequest> GenerateNFTRequestAsync(string web3JSONMetaDataFile = "")
        {
            MintWeb4NFTRequest request = new MintWeb4NFTRequest();

            request.MintedByAvatarId = STAR.BeamedInAvatar.Id;
            request.Title = CLIEngine.GetValidInput("What is the NFT's title?");
            request.Description = CLIEngine.GetValidInput("What is the NFT's description?");
            request.MemoText = CLIEngine.GetValidInput("What is the NFT's memotext? (optional)");
            request.Price = CLIEngine.GetValidInputForLong("What is the price for the NFT?");

            if (CLIEngine.GetConfirmation("Is there any discount for the NFT? (This can always be changed later)"))
            {
                Console.WriteLine("");
                request.Discount = CLIEngine.GetValidInputForLong("What is the discount?");
            }
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Is there any Royalty Percentage?"))
                request.RoyaltyPercentage = CLIEngine.GetValidInputForInt("Please enter the Royalty Percentage (integer): ", false);
            //else
            //    Console.WriteLine("");

            SalesInfo salesInfo = UpdateSalesInfo(new SalesInfo());

            request.IsForSale = salesInfo.IsForSale;
            request.SaleStartDate = salesInfo.SaleStartDate;
            request.SaleEndDate = salesInfo.SaleEndDate;

            //request.IsForSale = CLIEngine.GetConfirmation("Is the NFT for sale? Press 'Y' for Yes or 'N' for No.");

            //if (request.IsForSale.Value)
            //{
            //    request.SaleStartDate = CLIEngine.GetValidInputForDate("Please enter the Sale Start Date (YYYY-MM-DD)", addLineBefore: true);

            //    if (request.SaleStartDate.HasValue)
            //    {
            //        do
            //        {
            //            request.SaleEndDate = CLIEngine.GetValidInputForDate("Please enter the Sale End Date (YYYY-MM-DD) or 'none' to have no end date:", addLineBefore: true);

            //            if (request.SaleEndDate.HasValue && request.SaleEndDate.Value <= request.SaleEndDate.Value)
            //                CLIEngine.ShowWarningMessage("The end date must be after the start date!");
            //        }
            //        while (request.SaleEndDate.HasValue && request.SaleEndDate.Value <= request.SaleStartDate.Value);
            //    }
            //    else
            //        request.SaleEndDate = null;
            //}

            object onChainProviderObj = CLIEngine.GetValidInputForEnum("What on-chain provider do you wish to mint on?", typeof(ProviderType));
            request.OnChainProvider = new EnumValue<ProviderType>((ProviderType)onChainProviderObj);

            request.StoreNFTMetaDataOnChain = CLIEngine.GetConfirmation("Do you wish to store the NFT metadata on-chain or off-chain? (Press Y for on-chain or N for off-chain)");
            Console.WriteLine("");

            if (!request.StoreNFTMetaDataOnChain)
            {
                object offChainMetaDataTypeObj = CLIEngine.GetValidInputForEnum("How do you wish to store the offchain meta data/image? OASIS, IPFS, Pinata or External JSON URI (for the last option you will need to generate the meta data yourself and host somewhere like Pinata and then enter the URI, for the first three options the metadata will be generated automatically)? If you choose OASIS, it will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive. If you choose OASIS and then IPFSOASIS for the next question for the OASIS Provider it will store it on IPFS via The OASIS and then benefit from the OASIS HyperDrive feature to provide more reliable service and up-time etc. If you choose IPFS or Pinata for this question then it will store it directly on IPFS/Pinata without any additional benefits of The OASIS.", typeof(NFTOffChainMetaType));
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
                //else if (request.NFTOffChainMetaType.Value == NFTOffChainMetaType.ExternalJSON)
                //{
                //    if (string.IsNullOrEmpty(web3JSONMetaDataFile))
                //        web3JSONMetaDataFile = CLIEngine.GetValidFile("What is the full path to the JSON meta data file you have created for this NFT?");

                //    request.JSONMetaData = web3JSONMetaDataFile;
                //}
            }

            //if (string.IsNullOrEmpty(web3JSONMetaDataFile))
            if (string.IsNullOrEmpty(web3JSONMetaDataFile) && request.NFTOffChainMetaType.Value != NFTOffChainMetaType.ExternalJSONURL)
            {
                if (CLIEngine.GetConfirmation("Do you wish to import the JSON meta data now? (Press Y to import or N to generate new meta data)"))
                    web3JSONMetaDataFile = CLIEngine.GetValidFile("Please enter the full path to the JSON MetaData file you wish to import: ");
            }

            if (File.Exists(web3JSONMetaDataFile))
                request.JSONMetaData = File.ReadAllText(web3JSONMetaDataFile);
            else
                CLIEngine.ShowMessage("The JSON meta data file path you entered does not exist. A new JSON meta data file will be generated instead.", addLineBefore: true);

            bool validStandard = false;
            do
            {
                object nftStandardObj = CLIEngine.GetValidInputForEnum("What NFT standard do you wish to use? ERC721, ERC1155 or SPL? (ERC standards are only supported by EVM chains such as EthereumOASIS, PolygonsOASIS & ArbitrumOASIS. SPL is only supported by SolanaOASIS)", typeof(NFTStandardType));
                request.NFTStandardType = new EnumValue<NFTStandardType>((NFTStandardType)nftStandardObj);

                OASISResult<bool> nftStandardValid = NFTManager.IsNFTStandardTypeValid(request.NFTStandardType.Value, request.OnChainProvider.Value);

                if (!nftStandardValid.IsError)
                    validStandard = true;

            } while (!validStandard);

            if (request.NFTOffChainMetaType.Value != NFTOffChainMetaType.ExternalJSONURL)
            {
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
            }



            //if (CLIEngine.GetConfirmation("Do you wish to add any metadata to this NFT?"))
            //{
            //    request.MetaData = new Dictionary<string, object>();
            //    request.MetaData = AddMe(request.MetaData);
            //    bool metaDataDone = false;

            //    do
            //    {
            //        if (CLIEngine.GetConfirmation("Do you wish to add more metadata?"))
            //            request.MetaData = AddMetaDataToNFT(request.MetaData);
            //        else
            //            metaDataDone = true;
            //    }
            //    while (!metaDataDone);
            //}


            request.Tags = TagHelper.ManageTags(request.Tags);
            MetaDataHelper.ManageMetaData(request.MetaData, "NFT");
            Console.WriteLine("");
            request.NumberToMint = CLIEngine.GetValidInputForInt("How many NFT's do you wish to mint?");

            if (CLIEngine.GetConfirmation("Do you wish to send the NFT to yourself after it is minted?"))
                request.SendToAvatarAfterMintingId = STAR.BeamedInAvatar.Id;
            else
            {
                Console.WriteLine("");
                int selection = CLIEngine.GetValidInputForInt("Do you wish to send the NFT using the users (1) Wallet Address, (2) Avatar Id, (3) Username or (4) Email? (Please enter 1, 2, 3 or 4)", true, 1, 4);

                switch (selection)
                {
                    case 1:
                        //Console.WriteLine("");
                        request.SendToAddressAfterMinting = CLIEngine.GetValidInput("What is the wallet address you want to send the NFT after it is minted?");
                        break;

                    case 2:
                        //Console.WriteLine("");
                        request.SendToAvatarAfterMintingId = CLIEngine.GetValidInputForGuid("What is the Id of the Avatar you want to send the NFT after it is minted?");
                        break;

                    case 3:
                        //Console.WriteLine("");
                        request.SendToAvatarAfterMintingUsername = CLIEngine.GetValidInput("What is the Username of the Avatar you want to send the NFT after it is minted?");
                        break;

                    case 4:
                        //Console.WriteLine("");
                        request.SendToAvatarAfterMintingEmail = CLIEngine.GetValidInputForEmail("What is the Email of the Avatar you want to send the NFT after it is minted?");
                        break;
                }
            }

            if (CLIEngine.GetConfirmation("Do you wish to view the Advanced Options? (allows you to configure minting and sending retry timeouts, polling etc)."))
            {
                Console.WriteLine("");
                request.WaitTillNFTMinted = CLIEngine.GetConfirmation("Do you wish to wait till the NFT has been minted before continuing? If you select yes it will continue to attempt minting for X seconds (defined in next question). Default is Yes.");

                if (request.WaitTillNFTMinted)
                {
                    Console.WriteLine("");
                    request.WaitForNFTToMintInSeconds = CLIEngine.GetValidInputForInt("How many seconds do you wish to wait for the NFT to mint before timing out? (default is 60 seconds)");
                    request.AttemptToMintEveryXSeconds = CLIEngine.GetValidInputForInt("How often (in seconds) do you wish to attempt to mint? (default is every 1 second)");
                }

                request.WaitTillNFTSent = CLIEngine.GetConfirmation("Do you wish to wait till the NFT has been sent before continuing? If you select yes it will continue to attempt sending for X seconds (defined in next question). Default is Yes.");

                if (request.WaitTillNFTSent)
                {
                    Console.WriteLine("");
                    request.WaitForNFTToSendInSeconds = CLIEngine.GetValidInputForInt("How many seconds do you wish to wait for the NFT to send before timing out? (default is 60 seconds)");
                    request.AttemptToSendEveryXSeconds = CLIEngine.GetValidInputForInt("How often (in seconds) do you wish to attempt to send? (default is every 1 second)");
                }
            }
            else
                Console.WriteLine("");

            return request;
        }


        public async Task<IImportWeb3NFTRequest> GenerateImportNFTRequestAsync()
        {
            ImportWeb3NFTRequest request = new ImportWeb3NFTRequest();

            request.NFTTokenAddress = CLIEngine.GetValidInput("Please enter the token address of the NFT you wish to import: ");
            request.ImportedByByAvatarId = STAR.BeamedInAvatar.Id;
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

        public OASISResult<IUpdateWeb4NFTRequest> UpdateWeb4NFT(IUpdateWeb4NFTRequest request, IWeb4OASISNFT nft, string displayName, bool updateTags = true, bool updateMetaData = true)
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
                SalesInfo salesInfo = UpdateSalesInfo(new SalesInfo() { IsForSale = nft.IsForSale, SaleStartDate = nft.SaleStartDate, SaleEndDate = nft.SaleEndDate });
                
                request.IsForSale = salesInfo.IsForSale;
                request.SaleStartDate = salesInfo.SaleStartDate;
                request.SaleEndDate = salesInfo.SaleEndDate;
            }
            else
                Console.WriteLine("");

            //if (CLIEngine.GetConfirmation("Do you wish to edit Total Number Of Sales?"))
            //    request.TotalNumberOfSales = CLIEngine.GetValidInputForInt("Please enter the total number of sales:", false);

            //if (CLIEngine.GetConfirmation("Do you wish to edit Last Sale Transaction Hash?"))
            //    request.LastSaleTransactionHash = CLIEngine.GetValidInput("Please enter the last sale transaction hash:");

            //if (CLIEngine.GetConfirmation("Do you wish to edit Last Sold By Avatar Id?"))
            //    request.LastSoldByAvatarId = CLIEngine.GetValidInputForGuid("Please enter the Last Sold By Avatar Id (GUID): ");

            //if (CLIEngine.GetConfirmation("Do you wish to edit Last Purchased By Avatar Id?"))
            //    request.LastPurchasedByAvatarId = CLIEngine.GetValidInputForGuid("Please enter the Last Purchased By Avatar Id (GUID): ");

            //if (CLIEngine.GetConfirmation("Do you wish to edit Last Sale Quantity?"))
            //    request.LastSaleQuantity = CLIEngine.GetValidInputForInt("Please enter the last sale quantity:", false);

            //if (CLIEngine.GetConfirmation("Do you wish to edit Last Sale Discount?"))
            //    request.LastSaleDiscount = CLIEngine.GetValidInputForDecimal("Please enter the last sale discount:");

            //if (CLIEngine.GetConfirmation("Do you wish to edit Last Sale Tax?"))
            //    request.LastSaleTax = CLIEngine.GetValidInputForDecimal("Please enter the last sale tax:");

            //if (CLIEngine.GetConfirmation("Do you wish to edit Sales History?"))
            //    request.SalesHistory = CLIEngine.GetValidInput("Please enter the sales history string:");

            //if (CLIEngine.GetConfirmation("Do you wish to edit Last Sale Price?"))
            //    request.LastSalePrice = CLIEngine.GetValidInputForDecimal("Please enter the last sale price:");

            //if (CLIEngine.GetConfirmation("Do you wish to edit Last Sale Amount?"))
            //    request.LastSaleAmount = CLIEngine.GetValidInputForDecimal("Please enter the last sale amount:");

            //if (CLIEngine.GetConfirmation("Do you wish to edit Last Sale Date?"))
            //{
            //    string input = CLIEngine.GetValidInput("Please enter the Last Sale Date (YYYY-MM-DD) or 'none' to clear:");
            //    if (!string.IsNullOrEmpty(input) && input.ToLower() != "none" && DateTime.TryParse(input, out DateTime lastSaleDate))
            //        request.LastSaleDate = lastSaleDate;
            //    // Note: UpdateNFTRequest.LastSaleDate is non-nullable; if user doesn't set it will remain default(DateTime)
            //}

            if (updateTags)
                request.Tags = TagHelper.ManageTags(nft.Tags);

            if (updateMetaData)
                request.MetaData = MetaDataHelper.ManageMetaData(nft.MetaData, displayName);

            result.Result = request;

            return result;
        }

        public SalesInfo UpdateSalesInfo(SalesInfo salesInfo, bool edit = true)
        {
            salesInfo.IsForSale = CLIEngine.GetConfirmation("Is the NFT for sale? Press 'Y' for Yes or 'N' for No.", addLineBefore: true);

            if (salesInfo.IsForSale.HasValue && salesInfo.IsForSale.Value)
            {
                string existingSaleStartDate = salesInfo.SaleStartDate.HasValue ? salesInfo.SaleStartDate.Value == DateTime.MinValue ? "None" : salesInfo.SaleStartDate.Value.ToShortDateString() : "None";

                if (!edit || (edit && CLIEngine.GetConfirmation($"Do you wish to edit the Sale Start Date? (currently is: {existingSaleStartDate})", addLineBefore: true)))
                    salesInfo.SaleStartDate = CLIEngine.GetValidInputForDate("Please enter the Sale Start Date or 'none' to clear:", addLineBefore: true);
                else
                    Console.WriteLine("");

                if (salesInfo.SaleStartDate.HasValue)
                {
                    string existingSaleEndDate = salesInfo.SaleEndDate.HasValue ? salesInfo.SaleEndDate.Value == DateTime.MinValue ? "None" : salesInfo.SaleEndDate.Value.ToShortDateString() : "None";

                    if (!edit || (edit && CLIEngine.GetConfirmation($"Do you wish to edit Sale End Date? (currently is: {existingSaleEndDate})")))
                    {
                        do
                        {
                            salesInfo.SaleEndDate = CLIEngine.GetValidInputForDate("Please enter the Sale End Date or 'none' to clear:", addLineBefore: true);

                            if (salesInfo.SaleEndDate.HasValue && salesInfo.SaleEndDate.Value <= salesInfo.SaleStartDate.Value)
                                CLIEngine.ShowWarningMessage("The end date must be after the start date!");
                        }
                        while (salesInfo.SaleEndDate.HasValue && salesInfo.SaleEndDate.Value <= salesInfo.SaleStartDate.Value);
                    }
                    else
                        Console.WriteLine("");
                }
                else
                    salesInfo.SaleEndDate = null;
            }
            else
                Console.WriteLine("");

            return salesInfo;
        }

        public OASISResult<IUpdateWeb4NFTCollectionRequestBase> UpdateWeb4NFTCollection(IUpdateWeb4NFTCollectionRequestBase request, IWeb4OASISNFTCollectionBase collection, string displayName, bool updateTags = true, bool updateMetaData = true)
        {
            OASISResult<IUpdateWeb4NFTCollectionRequestBase> result = new OASISResult<IUpdateWeb4NFTCollectionRequestBase>();

            request.Id = collection.Id;
            request.ModifiedBy = STAR.BeamedInAvatar.Id;

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Title? (currently is: {collection.Name})"))
                request.Title = CLIEngine.GetValidInput("Please enter the new title: ", addLineBefore: true);
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to edit the Description? (currently is: {collection.Description})"))
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
                }
            }
            else
                Console.WriteLine("");

            if (updateTags)
                request.Tags = TagHelper.ManageTags(collection.Tags);

            if (updateMetaData)
                request.MetaData = MetaDataHelper.ManageMetaData(collection.MetaData, displayName);

            result.Result = request;
            return result;
        }

        public async Task<OASISResult<T5>> UpdateSTARNETHolonAsync<T1, T2, T3, T4, T5>(string web5IdMetaDataKey, string starnetDNAKeyForWeb4Object, ISTARNETManagerBase<T1, T2, T3, T4> STARNETManager, Dictionary<string, object> metaData, OASISResult<T5> result, ProviderType providerType = ProviderType.Default) 
            where T1 : ISTARNETHolon, new()
            where T2 : IDownloadedSTARNETHolon, new()
            where T3 : IInstalledSTARNETHolon, new()
            where T4 : ISTARNETDNA, new()
        {
            Guid web5Id = Guid.Empty;

            if (metaData != null && metaData.ContainsKey(web5IdMetaDataKey) && metaData[web5IdMetaDataKey] != null && Guid.TryParse(metaData[web5IdMetaDataKey].ToString(), out web5Id))
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Updating WEB5 STAR {STARNETManager.STARNETHolonUIName} with updated WEB4 OASIS {STARNETManager.STARNETHolonUIName} data...");
                OASISResult<T1> starNFTCollection = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, web5Id, providerType: providerType);

                if (starNFTCollection != null && starNFTCollection.Result != null && !starNFTCollection.IsError)
                {
                    starNFTCollection.Result.STARNETDNA.MetaData[starnetDNAKeyForWeb4Object] = result.Result;
                    starNFTCollection = await STARNETManager.UpdateAsync(STAR.BeamedInAvatar.Id, starNFTCollection.Result, updateDNAJSONFile: true, providerType: providerType);

                    if (starNFTCollection != null && starNFTCollection.Result != null && !starNFTCollection.IsError)
                        CLIEngine.ShowSuccessMessage($"WEB5 STAR {STARNETManager.STARNETHolonUIName} Successfully Updated.");
                    else
                    {
                        string msg = starNFTCollection != null ? starNFTCollection.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB5 STAR {STARNETManager.STARNETHolonUIName} after updating WEB4 OASIS {STARNETManager.STARNETHolonUIName}. Reason: {msg}");
                    }
                }
                else
                {
                    string msg = starNFTCollection != null ? starNFTCollection.Message : "";
                    OASISErrorHandling.HandleError(ref result, $"Error Occured Loading WEB5 STAR {STARNETManager.STARNETHolonUIName}. Reason: {msg}");
                }
            }

            return result;
        }

        public async Task<OASISResult<T5>> DeleteAllSTARNETVersionsAsync<T1, T2, T3, T4, T5>(string web5IdMetaDataKey, ISTARNETManagerBase<T1, T2, T3, T4> STARNETManager, Dictionary<string, object> metaData, OASISResult<T5> result, ProviderType providerType = ProviderType.Default)
            where T1 : ISTARNETHolon, new()
            where T2 : IDownloadedSTARNETHolon, new()
            where T3 : IInstalledSTARNETHolon, new()
            where T4 : ISTARNETDNA, new()
        {
            Guid web5Id = Guid.Empty;

            if (metaData != null && metaData.ContainsKey(web5IdMetaDataKey) && metaData[web5IdMetaDataKey] != null && Guid.TryParse(metaData[web5IdMetaDataKey].ToString(), out web5Id))
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Deleting All WEB5 STAR {STARNETManager.STARNETHolonUIName} Versions...");

                OASISResult<IEnumerable<T1>> versionsResult = await STARNETManager.LoadVersionsAsync(web5Id, providerType);

                if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
                {
                    foreach (T1 version in versionsResult.Result)
                    {
                        OASISResult<T1> deleteResult = await STARNETManager.DeleteAsync(STAR.BeamedInAvatar.Id, web5Id, version.STARNETDNA.VersionSequence, providerType: providerType);

                        if (deleteResult != null && deleteResult.Result != null && !deleteResult.IsError)
                            CLIEngine.ShowSuccessMessage($"Successfully Deleted Version {version.STARNETDNA.VersionSequence}.");
                        else
                        {
                            string msg = versionsResult != null ? versionsResult.Message : "";
                            OASISErrorHandling.HandleError(ref result, $"Error Occured Deleting WEB5 STAR {STARNETManager.STARNETHolonUIName} Version {version.STARNETDNA.VersionSequence}. Reason: {msg}");
                        }
                    }
                }
                else
                {
                    string msg = versionsResult != null ? versionsResult.Message : "";
                    OASISErrorHandling.HandleError(ref result, $"Error Occured Loading WEB5 STAR {STARNETManager.STARNETHolonUIName} versions. Reason: {msg}");
                }   
            }

            return result;
        }
    }
}