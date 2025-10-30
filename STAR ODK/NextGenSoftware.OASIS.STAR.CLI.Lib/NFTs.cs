using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.OASIS.STAR.DNA;
using ThirdParty.Json.LitJson;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class NFTs : STARNETUIBase<STARNFT, DownloadedNFT, InstalledNFT, NFTDNA>
    public class NFTs : STARNETUIBase<STARNFT, DownloadedNFT, InstalledNFT, STARNETDNA>
    {
        public NFTCommon NFTCommon { get; set; } = new NFTCommon();

        public NFTs(Guid avatarId, STARDNA STARDNA) : base(new STARNFTManager(avatarId, STARDNA),
            "Welcome to the WEB5 STAR NFT Wizard", new List<string> 
            {
                "This wizard will allow you create a WEB5 STAR NFT which wraps around a WEB4 OASIS NFT.",
                "You can mint a WEB4 OASIS NFT using the 'nft mint' sub-command.",
                "You then convert or wrap around the WEB4 OASIS NFT using the sub-command 'nft create' which will create a WEB5 STAR NFT compatible with STARNET.",
                "A WEB5 NFT can then be published to STARNET in much the same way as everything else within STAR using the same sub-commands such as publish, download, install etc.",
                "A WEB5 GeoNFT can be created from a WEB4 GeoNFT (which in turn is created from a WEB4 NFT) and can be placed in any location within Our World as part of Quest's. The main difference is WEB5 STAR NFT's can be published to STARNET, version controlled, shared, etc whereas WEB4 NFT's cannot.",
                "The wizard will create an empty folder with a NFTDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the NFT into this folder.",
                "Finally you run the sub-command 'nft publish' to convert the folder containing the NFT (can contain any number of files and sub-folders) into a OASIS NFT file (.onft) as well as optionally upload to STARNET.",
                "You can then share the .onft file with others across any platform or OS, who can then install the NFT from the file using the sub-command 'nft install'.",
                "You can also optionally choose to upload the .onft file to the STARNET store so others can search, download and install the NFT."
            },
            STAR.STARDNA.DefaultNFTsSourcePath, "DefaultNFTsSourcePath",
            STAR.STARDNA.DefaultNFTsPublishedPath, "DefaultNFTsPublishedPath",
            STAR.STARDNA.DefaultNFTsDownloadedPath, "DefaultNFTsDownloadedPath",
            STAR.STARDNA.DefaultNFTsInstalledPath, "DefaultNFTsInstalledPath")
        { }

        //public override async Task CreateAsync(object createParams, STARNFT newHolon = null, ProviderType providerType = ProviderType.Default)
        //{
        //    Guid geoNFTId = CLIEngine.GetValidInputForGuid("Please enter the ID of the NFT you wish to upload to STARNET: ");
        //    OASISResult<IOASISNFT> NFTResult = await NFTManager.LoadNftAsync(geoNFTId);

        //    if (NFTResult != null && !NFTResult.IsError && NFTResult.Result != null)
        //        await base.CreateAsync(createParams, new STARNFT() { OASISNFTId = geoNFTId }, providerType);
        //    else
        //        CLIEngine.ShowErrorMessage("No NFT Found For That Id!");
        //}

        public override async Task<OASISResult<STARNFT>> CreateAsync(ISTARNETCreateOptions<STARNFT, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARNFT> result = new OASISResult<STARNFT>();
            OASISResult<IOASISNFT> NFTResult = null;
            bool mint = false;

            ShowHeader();

            if (CLIEngine.GetConfirmation("Do you have an existing WEB4 OASIS NFT you wish to create a WEB5 NFT from?"))
            {
                Console.WriteLine("");
                NFTResult = await FindWeb4NFTAsync("wrap");
                
                //Guid id = CLIEngine.GetValidInputForGuid("Please enter the ID of the WEB4 NFT you wish to upload to STARNET: ");

                //if (id != Guid.Empty)
                //    NFTResult = await STAR.OASISAPI.NFTs.LoadNftAsync(id);
                //else
                //{
                //    result.IsWarning = true;
                //    result.Message = "User Exited";
                //    return result;
                //}
            }
            else
            {
                Console.WriteLine("");
                NFTResult = await MintNFTAsync(); //Mint WEB4 GeoNFT (mints and wraps around a WEB4 OASIS NFT).
                mint = true;
            }

            if (NFTResult != null && NFTResult.Result != null && !NFTResult.IsError)
            {
                IOASISNFT NFT = NFTResult.Result;

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS NFT to WEB5 STARNET which will create a WEB5 STAR NFT that wraps around the WEB4 OASISNFT allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended). Selecting 'Y' will also create a WEB3 JSONMetaData and a WEB4 OASISNFT json file in the WEB5 STAR NFT folder.")))
                {
                    Console.WriteLine("");

                    result = await base.CreateAsync(new STARNETCreateOptions<STARNFT, STARNETDNA>()
                    {
                        STARNETDNA = new STARNETDNA()
                        {
                            MetaData = new Dictionary<string, object>() { { "NFT", NFT } }
                        },
                        STARNETHolon = new STARNFT()
                        {
                            OASISNFTId = NFTResult.Result.Id
                        }
                    }, holonSubType, showHeaderAndInro, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        //result.Result.NFTType = (NFTType)result.Result.STARNETDNA.STARNETCategory;
                        result.Result.NFTType = (NFTType)Enum.Parse(typeof(NFTType), result.Result.STARNETDNA.STARNETCategory.ToString());
                        OASISResult<STARNFT> saveResult = await result.Result.SaveAsync<STARNFT>();

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"OASISNFT_{NFTResult.Result.Id}.json"), JsonConvert.SerializeObject(NFT));

                            if (!string.IsNullOrEmpty(NFTResult.Result.JSONMetaData))
                                File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"JSONMetaData_{NFTResult.Result.Id}.json"), NFTResult.Result.JSONMetaData);
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured saving STARNFT after creation in CreateAsync method. Reason: {saveResult.Message}");
                    }
                }
            }
            else
            {
                if (mint)
                    OASISErrorHandling.HandleError(ref result, $"Error occured minting NFT in MintGeoNFTAsync method. Reason: {NFTResult.Message}");
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading NFT in LoadGeoNftAsync method. Reason: {NFTResult.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IOASISNFT>> MintNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            IMintNFTTransactionRequest request = await NFTCommon.GenerateNFTRequestAsync();

            CLIEngine.ShowWorkingMessage("Minting OASIS NFT...");
            OASISResult<INFTTransactionRespone> nftResult = await STAR.OASISAPI.NFTs.MintNftAsync(request);

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
            {
                //CLIEngine.ShowSuccessMessage($"OASIS NFT Successfully Minted. {nftResult.Message} Transaction Result: {nftResult.Result.TransactionResult}, Id: {nftResult.Result.OASISNFT.Id}, Hash: {nftResult.Result.OASISNFT.Hash} Minted On: {nftResult.Result.OASISNFT.MintedOn}, Minted By Avatar Id: {nftResult.Result.OASISNFT.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.OASISNFT.MintedByAddress}.");
                CLIEngine.ShowSuccessMessage(nftResult.Message);
                result.Result = nftResult.Result.OASISNFT;
            }
            else
            {
                string msg = nftResult != null ? nftResult.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }
           
            return result;
        }

        public async Task SendNFTAsync()
        {
            //string mintWalletAddress = CLIEngine.GetValidInput("What is the original mint address?");
            string fromWalletAddress = CLIEngine.GetValidInput("What address are you sending the NFT from?");
            string toWalletAddress = CLIEngine.GetValidInput("What address are you sending the NFT to?");
            string tokenAddress = CLIEngine.GetValidInput("What is the token address of the NFT?");
            string memoText = CLIEngine.GetValidInput("What is the memo text?");
            //decimal amount = CLIEngine.GetValidInputForDecimal("What is the amount?");

            CLIEngine.ShowWorkingMessage("Sending NFT...");

            OASISResult<INFTTransactionRespone> response = await STAR.OASISAPI.NFTs.SendNFTAsync(new NFTWalletTransactionRequest()
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                TokenAddress = tokenAddress,
                //MintWalletAddress = mintWalletAddress,
                MemoText = memoText,
                Amount = 1
            });

            if (response != null && response.Result != null && !response.IsError)
                //CLIEngine.ShowSuccessMessage($"NFT Successfully Sent. {response.Message} Hash: {response.Result.TransactionResult}");
                CLIEngine.ShowSuccessMessage(response.Message);
            else
            {
                string msg = response != null ? response.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }
        }

        public override void Show<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 35, object customData = null)
        {
            displayFieldLength = DEFAULT_FIELD_LENGTH;
            base.Show(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("NFT") && starHolon.STARNETDNA.MetaData["NFT"] != null)
            {
                IOASISNFT nft = starHolon.STARNETDNA.MetaData["NFT"] as IOASISNFT;

                if (nft == null)
                    nft = JsonConvert.DeserializeObject<OASISNFT>(starHolon.STARNETDNA.MetaData["NFT"].ToString());

                if (nft != null)
                {
                    Console.WriteLine("");
                    DisplayProperty("NFT DETAILS", "", displayFieldLength, false);
                    ShowNFT(nft, showHeader: false, showFooter: false);
                }
            }

            CLIEngine.ShowDivider();
        }

        public async Task<OASISResult<IOASISNFT>> BurnNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            return result;
        }

        public async Task<OASISResult<IOASISNFT>> ImportNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            bool isWeb3 = false;

            if (mintParams != null)
                bool.TryParse(mintParams.ToString(), out isWeb3);

            if (isWeb3)
            {
                if (CLIEngine.GetConfirmation("Do you wish to import a WEB3 JSON MetaData file & then mint and wrap in a WEB4 OASIS NFT or import an existing minted NFT's token address and wrap in a WEB4 OASIS NFT? Press 'Y' for JSON File or 'N' for Token Address."))
                {
                    //WEB3 NFT Import from JSON MetaData file
                    string jsonPath = CLIEngine.GetValidFile("Please enter the full path to the JSON MetaData file you wish to import: ");

                    IMintNFTTransactionRequest request = await NFTCommon.GenerateNFTRequestAsync(jsonPath);

                    CLIEngine.ShowWorkingMessage("Minting OASIS NFT...");
                    OASISResult<INFTTransactionRespone> nftResult = await STAR.OASISAPI.NFTs.MintNftAsync(request);
         
                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        CLIEngine.ShowSuccessMessage(nftResult.Message);
                        result.Result = nftResult.Result.OASISNFT;
                    }
                    else
                    {
                        string msg = nftResult != null ? nftResult.Message : "";
                        CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
                    }
                }
                else
                {
                    // Import Web3 NFT functionality
                    try
                    {
                        IImportWeb3NFTRequest request = await NFTCommon.GenerateImportNFTRequestAsync();
                        CLIEngine.ShowWorkingMessage("Importing Web3 NFT...");

                        var importResult = await NFTCommon.NFTManager.ImportWeb3NFTAsync(request);

                        if (importResult != null && importResult.Result != null && !importResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage(importResult.Message);
                            result.Result = importResult.Result;
                            result.Message = importResult.Message;
                        }
                        else
                        {
                            string msg = importResult != null ? importResult.Message : "";
                            CLIEngine.ShowErrorMessage($"Failed to import Web3 NFT: {msg}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.IsError = true;
                        result.Message = $"Error importing Web3 NFT: {ex.Message}";
                        CLIEngine.ShowErrorMessage($"Error importing Web3 NFT: {ex.Message}");
                    }
                }
            }
            else
            {
                // WEB4 OASIS NFT Import
                try
                {
                    string filePath = CLIEngine.GetValidFile("Please enter the full path to the OASIS NFT file you wish to import: ");

                    OASISResult<IOASISNFT> importResult = await NFTCommon.NFTManager.ImportOASISNFTAsync(STAR.BeamedInAvatar.Id, filePath);

                    if (importResult != null && importResult.Result != null && !importResult.IsError)
                    {
                        CLIEngine.ShowSuccessMessage(importResult.Message);
                        result.Result = importResult.Result;
                        result.Message = importResult.Message;
                    }
                    else
                    {
                        string msg = importResult != null ? importResult.Message : "";
                        CLIEngine.ShowErrorMessage($"Failed to import OASIS NFT: {msg}");
                    }
                }
                catch (Exception ex)
                {
                    result.IsError = true;
                    result.Message = $"Error importing OASIS NFT: {ex.Message}";
                    CLIEngine.ShowErrorMessage($"Error importing OASIS NFT: {ex.Message}");
                }
            }

            return result;
        }

        public async Task<OASISResult<IOASISNFT>> ExportNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            return result;
        }

        //public async Task<OASISResult<IOASISNFT>> CloneNFTAsync(object mintParams = null)
        //{
        //    OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
        //    return result;
        //}

        public async Task<OASISResult<IOASISNFT>> ConvertNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            return result;
        }

        public async Task<OASISResult<IOASISNFT>> UpdateWeb4NFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            UpdateNFTRequest request = new UpdateNFTRequest();

            OASISResult<IOASISNFT> collectionResult = await FindWeb4NFTAsync("update", idOrName, providerType: providerType);

            if (collectionResult != null && collectionResult.Result != null && !collectionResult.IsError)
            {
                // Ensure we set the Id of the NFT we're updating
                request.Id = collectionResult.Result.Id;

                if (CLIEngine.GetConfirmation($"Do you wish to edit the Title? (currently is: {collectionResult.Result.Title})"))
                    request.Title = CLIEngine.GetValidInput("Please enter the new title for the NFT: ");

                if (CLIEngine.GetConfirmation($"Do you wish to edit the Description? (currently is: {collectionResult.Result.Description})"))
                { 
                    request.Description = CLIEngine.GetValidInput("Please enter the new description for the NFT: ");

                request.ModifiedByAvatarId = STAR.BeamedInAvatar.Id;

                if (CLIEngine.GetConfirmation("Do you wish to update the Image and Thumbnail?"))
                {
                    OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("NFT");

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
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail for NFT: {msg}");
                        return result;
                    }
                }

                if (CLIEngine.GetConfirmation($"Do you wish to edit the Price? (currently is: {collectionResult.Result.Price}.)"))
                    request.Price = CLIEngine.GetValidInputForDecimal("Please enter the new Price for the NFT: ");

                if (CLIEngine.GetConfirmation($"Do you wish to edit the Discount? (currently is: {collectionResult.Result.Discount}.)"))
                    request.Discount = CLIEngine.GetValidInputForDecimal("Please enter the new Discount for the NFT: ");

                // Allow editing additional NFT-specific fields
                if (CLIEngine.GetConfirmation($"Do you wish to edit the Royalty Percentage? (currently is: {collectionResult.Result.RoyaltyPercentage}.)"))
                    request.RoyaltyPercentage = CLIEngine.GetValidInputForInt("Please enter the Royalty Percentage (integer): ", false);

                //if (CLIEngine.GetConfirmation("Do you wish to edit the Previous Owner Avatar Id?"))
                //    request.PreviousOwnerAvatarId = CLIEngine.GetValidInputForGuid("Please enter the Previous Owner Avatar Id (GUID): ");

                //if (CLIEngine.GetConfirmation("Do you wish to edit the Current Owner Avatar Id?"))
                //    request.CurrentOwnerAvatarId = CLIEngine.GetValidInputForGuid("Please enter the Current Owner Avatar Id (GUID): ");

                if (CLIEngine.GetConfirmation($"Do you wish to change the sale status (Is For Sale)? (currently is: {collectionResult.Result.IsForSale}.)"))
                    request.IsForSale = CLIEngine.GetConfirmation("Is the NFT for sale? Press 'Y' for Yes or 'N' for No.");

                string existingSaleStartDate = collectionResult.Result.SaleStartDate.HasValue ? collectionResult.Result.SaleStartDate.Value.ToShortDateString() : "None";
                if (CLIEngine.GetConfirmation($"Do you wish to edit Sale Start Date? (currently is: {existingSaleStartDate}.)"))
                {
                    string input = CLIEngine.GetValidInput("Please enter the Sale Start Date (YYYY-MM-DD) or 'none' to clear:");
                    if (!string.IsNullOrEmpty(input) && input.ToLower() != "none" && DateTime.TryParse(input, out DateTime startDate))
                        request.SaleStartDate = startDate;
                    else
                        request.SaleStartDate = null;
                }

                string existingSaleEndDate = collectionResult.Result.SaleEndDate.HasValue ? collectionResult.Result.SaleEndDate.Value.ToShortDateString() : "None";
                if (CLIEngine.GetConfirmation($"Do you wish to edit Sale End Date? (currently is: {existingSaleEndDate}.)"))
                {
                    string input = CLIEngine.GetValidInput("Please enter the Sale End Date (YYYY-MM-DD) or 'none' to clear:");
                    if (!string.IsNullOrEmpty(input) && input.ToLower() != "none" && DateTime.TryParse(input, out DateTime endDate))
                        request.SaleEndDate = endDate;
                    else
                        request.SaleEndDate = null;
                }

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

                if (CLIEngine.GetConfirmation("Do you wish to edit the Tags?"))
                {
                    List<string> tags = new List<string>();
                    string tag = "";
                    Console.WriteLine("Enter each tag followed by enter. When you are finished enter 'done' and press enter.");
                    while (tag.ToLower() != "done")
                    {
                        tag = CLIEngine.GetValidInput("Enter Tag: ");
                        if (tag.ToLower() != "done")
                            tags.Add(tag);
                    }
                    request.Tags = tags;
                }

                request.MetaData = NFTCommon.ManageMetaData(collectionResult.Result.MetaData, "NFT");
                result = await NFTCommon.NFTManager.UpdateOASISNFTAsync(request, providerType);

                if (result != null && result.Result != null && !result.IsError)
                    CLIEngine.ShowSuccessMessage("OASIS NFT Successfully Updated.");
                else
                {
                    string msg = result != null ? result.Message : "";
                    CLIEngine.ShowErrorMessage($"Error Occured Updating NFT: {msg}");
                }
            }
            else
            {
                string msg = collectionResult != null ? collectionResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding NFT to update: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IOASISNFT>> DeleteWeb4NFTAsync(string collectionIdOrName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFT> nft = await FindWeb4NFTAsync("delete", collectionIdOrName, true);

            if (nft == null || nft.Result == null || nft.IsError)
            {
                OASISErrorHandling.HandleError(ref nft, $"Error occured finding NFT to delete. Reason: {nft.Message}");
                return nft;
            }

            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteOASISNFTAsync(STAR.BeamedInAvatar.Id, nft.Result.Id, softDelete, providerType: providerType);

            if (deleteResult != null && deleteResult.Result && !deleteResult.IsError)
                CLIEngine.ShowSuccessMessage("OASIS NFT Successfully Deleted.");
            else
            {
                string msg = deleteResult != null ? deleteResult.Message : "";
                OASISErrorHandling.HandleError(ref nft, $"Error occured deleting NFT. Reason: {msg}");
            }

            return nft;
        }

        //public virtual async Task<OASISResult<IEnumerable<IOASISNFT>>> ListAllWeb4NFTsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<IEnumerable<IOASISNFT>>> ListAllWeb4NFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(await NFTCommon.NFTManager.LoadAllNFTsAsync(providerType));
        }

        //public virtual OASISResult<IEnumerable<IOASISNFT>> ListAllWeb4NFTs(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual OASISResult<IEnumerable<IOASISNFT>> ListAllWeb4NFTs(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(NFTCommon.NFTManager.LoadAllNFTs(providerType));
        }

        //public virtual async Task<OASISResult<IEnumerable<IOASISNFT>>> ListAllWeb4NFTForAvatarsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<IEnumerable<IOASISNFT>>> ListAllWeb4NFTForAvatarsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(await NFTCommon.NFTManager.LoadAllNFTsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType));
        }

        //public virtual OASISResult<IEnumerable<IOASISNFT>> ListAllWeb4NFTsForAvatar(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual OASISResult<IEnumerable<IOASISNFT>> ListAllWeb4NFTsForAvatar(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(NFTCommon.NFTManager.LoadAllNFTsForAvatar(STAR.BeamedInAvatar.Id, providerType));
        }

        public virtual async Task<OASISResult<IOASISNFT>> ShowWeb4NFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");

            result = await FindWeb4NFTAsync("view", idOrName, true, providerType: providerType);

            if (result != null && result.Result != null && !result.IsError)
                ShowNFT(result.Result);
            else
                OASISErrorHandling.HandleError(ref result, "No WEB4 NFT Found For That Id or Name!");

            return result;
        }

        public virtual async Task SearchWeb4NFTAsync(string searchTerm = "", bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
                searchTerm = CLIEngine.GetValidInput($"What is the name of the WEB4 NFT you wish to search for?");

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching WEB4 NFT's...");
            ListWeb4NFTs(await NFTCommon.NFTManager.SearchNFTsAsync(searchTerm, STAR.BeamedInAvatar.Id, !showForAllAvatars, providerType: providerType));
        }

        public async Task<OASISResult<IOASISNFT>> FindWeb4NFTAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "NFT", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IOASISNFT>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllNFTsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllNFTsAsync(providerType);

                        ListWeb4NFTs(starHolonsResult);

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
                    result = await NFTCommon.NFTManager.LoadNftAsync(id, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.MintedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was minted by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IOASISNFT>> searchResults = await NFTCommon.NFTManager.SearchNFTsAsync(idOrName, STAR.BeamedInAvatar.Id, showOnlyForCurrentAvatar, providerType: providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListWeb4NFTs(searchResults, true);

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
                    ShowNFT(result.Result);

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

        private OASISResult<IEnumerable<IOASISNFT>> ListWeb4NFTs(OASISResult<IEnumerable<IOASISNFT>> nfts, bool showNumbers = false, bool showDetailedInfo = false)
        {
            if (nfts != null)
            {
                if (!nfts.IsError)
                {
                    if (nfts.Result != null && nfts.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (nfts.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{nfts.Result.Count()} WEB4 NFT Found:");
                        else
                            CLIEngine.ShowMessage($"{nfts.Result.Count()} WEB4 NFT's Found:");

                        //foreach (IOASISNFT nft in nfts.Result)
                        //    ShowNFT(nft, showNumbers, showDetailedInfo, DEFAULT_FIELD_LENGTH);

                        for (int i = 0; i < nfts.Result.Count(); i++)
                            ShowNFT(nfts.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showDetailedInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No WEB4 NFT's Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading WEB4 NFT's. Reason: {nfts.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading WEB4 NFT's.");

            return nfts;
        }

        private void ShowNFT(IOASISNFT nft, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 39)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            //DisplayProperty("NFT DETAILS", "", displayFieldLength, false);
            //Console.WriteLine("");
            DisplayProperty("NFT Id", nft.Id.ToString(), displayFieldLength);
            DisplayProperty("Title", nft.Title, displayFieldLength);
            DisplayProperty("Description", nft.Description, displayFieldLength);
            DisplayProperty("Price", nft.Price.ToString(), displayFieldLength);
            DisplayProperty("Discount", nft.Discount.ToString(), displayFieldLength);
            DisplayProperty("OASIS MintWallet Address", nft.OASISMintWalletAddress, displayFieldLength);
            DisplayProperty("Mint Transaction Hash", nft.MintTransactionHash, displayFieldLength);
            DisplayProperty("NFT Token Address", nft.NFTTokenAddress, displayFieldLength);
            DisplayProperty("Minted By Avatar Id", nft.MintedByAvatarId.ToString(), displayFieldLength);
            DisplayProperty("Minted On", nft.MintedOn.ToString(), displayFieldLength);
            DisplayProperty("OnChain Provider", nft.OnChainProvider.Name, displayFieldLength);
            DisplayProperty("OffChain Provider", nft.OffChainProvider.Name, displayFieldLength);
            DisplayProperty("Store NFT Meta Data OnChain", nft.StoreNFTMetaDataOnChain.ToString(), displayFieldLength);
            DisplayProperty("NFT OffChain Meta Type", nft.NFTOffChainMetaType.Name, displayFieldLength);
            DisplayProperty("NFT Standard Type", nft.NFTStandardType.Name, displayFieldLength);
            DisplayProperty("Symbol", nft.Symbol, displayFieldLength);
            DisplayProperty("Image", nft.Image != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("Image Url", nft.ImageUrl, displayFieldLength);
            DisplayProperty("Thumbnail", nft.Thumbnail != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("Thumbnail Url", !string.IsNullOrEmpty(nft.ThumbnailUrl) ? nft.ThumbnailUrl : "None", displayFieldLength);
            DisplayProperty("JSON MetaData URL", nft.JSONMetaDataURL, displayFieldLength);
            DisplayProperty("JSON MetaData URL Holon Id", nft.JSONMetaDataURLHolonId != Guid.Empty ? nft.JSONMetaDataURLHolonId.ToString() : "None", displayFieldLength);
            DisplayProperty("Seller Fee Basis Points", nft.SellerFeeBasisPoints.ToString(), displayFieldLength);
            DisplayProperty("Update Authority", nft.UpdateAuthority, displayFieldLength);
            DisplayProperty("Send To Address After Minting", nft.SendToAddressAfterMinting, displayFieldLength);
            DisplayProperty("Send To Avatar After Minting Id", nft.SendToAvatarAfterMintingId != Guid.Empty ? nft.SendToAvatarAfterMintingId.ToString() : "None", displayFieldLength);
            DisplayProperty("Send To Avatar After Minting Username", !string.IsNullOrEmpty(nft.SendToAvatarAfterMintingUsername) ? nft.SendToAvatarAfterMintingUsername : "None", displayFieldLength);
            DisplayProperty("Send NFT Transaction Hash", nft.SendNFTTransactionHash, displayFieldLength);

            if (nft.MetaData != null)
            {
                CLIEngine.ShowMessage($"MetaData:");

                foreach (string key in nft.MetaData.Keys)
                    CLIEngine.ShowMessage($"          {key} = {nft.MetaData[key]}", false);
            }
            else
                CLIEngine.ShowMessage($"MetaData: None");

            if (showFooter)
                CLIEngine.ShowDivider();
        }
    }
}