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
                Guid geoNFTId = CLIEngine.GetValidInputForGuid("Please enter the ID of the WEB4 NFT you wish to upload to STARNET: ");

                if (geoNFTId != Guid.Empty)
                    NFTResult = await STAR.OASISAPI.NFTs.LoadNftAsync(geoNFTId);
                else
                {
                    result.IsWarning = true;
                    result.Message = "User Exited";
                    return result;
                }
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

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS NFT to WEB5 STARNET which will create a WEB5 STAR NFT that wraps around the WEB4 OASISNFT allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended). Selecting 'Y' will also create a WEB3 JSONMetaData and a WEB4 OASISNFT json file in the WEB5 STAR NFT folder. Currently if you select 'N' then it will not show up for the 'nft list' or 'nft show' sub-command's since these only support WEB5 NFTs. Future support may be added to list/show WEB4 GeoNFT's and NFT's.")))
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
                        result.Result.NFTType = (NFTType)result.Result.STARNETDNA.STARNETCategory;
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
                    OASISErrorHandling.HandleError(ref result, $"Error occured minting GeoNFT in MintGeoNFTAsync method. Reason: {NFTResult.Message}");
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading GeoNFT in LoadGeoNftAsync method. Reason: {NFTResult.Message}");
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
                    ShowNFT(nft, displayFieldLength);
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

        //public async Task<OASISResult<IOASISNFT>> ExportNFTAsync(object mintParams = null)
        //{
        //    OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
        //    return result;
        //}

        public async Task<OASISResult<IOASISNFT>> CloneNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            return result;
        }

        public async Task<OASISResult<IOASISNFT>> ConvertNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IOASISNFT>>> ListAllWeb4NFTsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(await NFTCommon.NFTManager.LoadAllNFTsAsync(providerType));
        }

        public virtual OASISResult<IEnumerable<IOASISNFT>> ListAllWeb4NFTs(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(NFTCommon.NFTManager.LoadAllNFTs(providerType));
        }

        public virtual async Task<OASISResult<IEnumerable<IOASISNFT>>> ListAllWeb4NFTForAvatarsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(await NFTCommon.NFTManager.LoadAllNFTsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType));
        }

        public virtual OASISResult<IEnumerable<IOASISNFT>> ListAllWeb4NFTsForAvatar(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
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
                ShowNFT(result.Result, DEFAULT_FIELD_LENGTH);
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

        public async Task<OASISResult<IOASISNFTCollection>> CreateNFTCollectionAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFTCollection> result = new OASISResult<IOASISNFTCollection>();
            CreateOASISNFTCollectionRequest request = new CreateOASISNFTCollectionRequest();

            request.Title = CLIEngine.GetValidInput("Please enter a title for the NFT Collection: ");
            request.Description = CLIEngine.GetValidInput("Please enter a description for the NFT Collection: ");
            request.CreatedBy = STAR.BeamedInAvatar.Id;

            OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("NFT Collection");

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
                OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail for NFT Collection: {msg}");
                return result;
            }

            request.MetaData = request.MetaData = NFTCommon.AddMetaData("NFT Collection");

            if (CLIEngine.GetConfirmation("Do you wish to add any NFTs to this collection now? (You can always add more later)."))
            {
                request.OASISNFTs = new List<IOASISNFT>();
                OASISResult<IOASISNFT> nftResult = null;

                do
                {
                    if (CLIEngine.GetConfirmation("Does the NFT already exist? (If you select 'N' you will be taken through the minting process to create a new NFT to add to the collection)."))
                        nftResult = await FindWeb4NFTAsync("use", providerType: providerType);
                    else
                        nftResult = await MintNFTAsync();

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                        request.OASISNFTs.Add(nftResult.Result);
                    else
                    {
                        string msg = nftResult != null ? nftResult.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Finding NFT to add to Collection: {msg}");
                        return result;
                    }

                    ShowNFTCollectionNFTs(request.OASISNFTs);

                } while (CLIEngine.GetConfirmation("Do you wish to add another NFT to this collection?"));
            }

            result = await NFTCommon.NFTManager.CreateOASISNFTCollectionAsync(request);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("OASIS NFT Collection Successfully Created.");
            else
            {
                string msg = result != null ? result.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured Creating NFT Collection: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IOASISNFTCollection>> UpdateNFTCollectionAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFTCollection> result = new OASISResult<IOASISNFTCollection>();
            UpdateOASISNFTCollectionRequest request = new UpdateOASISNFTCollectionRequest();

            OASISResult<IOASISNFTCollection> collectionResult = await FindWeb4NFTCollectionAsync("update", providerType: providerType);

            if (collectionResult != null && collectionResult.Result != null && !collectionResult.IsError)
            {
                if (CLIEngine.GetConfirmation("Do you wish to edit the Title?"))
                    request.Title = CLIEngine.GetValidInput("Please enter the new title for the NFT Collection: ");

                if (CLIEngine.GetConfirmation("Do you wish to edit the Description?"))
                    request.Description = CLIEngine.GetValidInput("Please enter the new description for the NFT Collection: ");

                request.ModifiedBy = STAR.BeamedInAvatar.Id;

                if (CLIEngine.GetConfirmation("Do you wish to update the Image and Thumbnail?"))
                {
                    OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("NFT Collection");

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
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail for NFT Collection: {msg}");
                        return result;
                    }
                }


                request.MetaData = request.MetaData = NFTCommon.AddMetaData("NFT Collection");

                //if (CLIEngine.GetConfirmation("Do you wish to add more NFT's to this collection now? (You can always add more later)."))
                //{
                //    request.OASISNFTs = new List<IOASISNFT>();
                //    OASISResult<IOASISNFT> nftResult = null;

                //    do
                //    {
                //        if (CLIEngine.GetConfirmation("Does the NFT already exist? (If you select 'N' you will be taken through the minting process to create a new NFT to add to the collection)."))
                //            nftResult = await FindWeb4NFTAsync("use", providerType: providerType);
                //        else
                //            nftResult = await MintNFTAsync();

                //        if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                //            request.OASISNFTs.Add(nftResult.Result);
                //        else
                //        {
                //            string msg = nftResult != null ? nftResult.Message : "";
                //            OASISErrorHandling.HandleError(ref result, $"Error Occured Finding NFT to add to Collection: {msg}");
                //            return result;
                //        }

                //        ShowNFTCollectionNFTs(collectionResult.Result.OASISNFTs);

                //    } while (CLIEngine.GetConfirmation("Do you wish to add another NFT to this collection?"));
                //}

                result = await NFTCommon.NFTManager.UpdateOASISNFTCollectionAsync(request);

                if (result != null && result.Result != null && !result.IsError)
                    CLIEngine.ShowSuccessMessage("OASIS NFT Collection Successfully Updated.");
                else
                {
                    string msg = result != null ? result.Message : "";
                    CLIEngine.ShowErrorMessage($"Error Occured Updating NFT Collection: {msg}");
                }
            }
            else
            {
                string msg = collectionResult != null ? collectionResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding NFT Collection to update: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISNFTCollection>>> ShowAllNFTCollections(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFTCollection>> result = new OASISResult<IEnumerable<IOASISNFTCollection>>();
            result = ListWeb4NFTCollections(await NFTCommon.NFTManager.LoadAllNFTCollectionsAsync(providerType));
            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISNFTCollection>>> ShowNFTCollectionsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISNFTCollection>> result = new OASISResult<IEnumerable<IOASISNFTCollection>>();
            result = ListWeb4NFTCollections(await NFTCommon.NFTManager.LoadNFTCollectionsForAvatarAsync(avatarId, providerType));
            return result;
        }

        public async Task<OASISResult<IOASISNFTCollection>> AddNFTToCollectionAsync(string collectionIdOrName, string nftIdOrName)
        {
            OASISResult<IOASISNFTCollection> result = new OASISResult<IOASISNFTCollection>();
            OASISResult<IOASISNFTCollection> collection = await FindWeb4NFTCollectionAsync("add to", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding NFT Collection to add to. Reason: {collection.Message}");
                return result;
            }

            OASISResult<IOASISNFT> geoNft = await FindWeb4NFTAsync("add", nftIdOrName, true);

            if (geoNft == null || geoNft.Result == null || geoNft.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding NFT. Reason: {geoNft.Message}");
                return result;
            }

            result = await NFTCommon.NFTManager.AddOASISNFTToCollectionAsync(collection.Result.Id, geoNft.Result.Id);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("OASIS NFT Successfully Added to Collection.");
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured adding NFT to collection. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IOASISNFTCollection>> RemoveNFTFromCollectionAsync(string collectionIdOrName, string nftIdOrName)
        {
            OASISResult<IOASISNFTCollection> result = new OASISResult<IOASISNFTCollection>();
            OASISResult<IOASISNFTCollection> collection = await FindWeb4NFTCollectionAsync("remove from", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding NFT Collection to remove from. Reason: {collection.Message}");
                return result;
            }

            OASISResult<IOASISNFT> geoNft = await FindWeb4NFTAsync("add", nftIdOrName, true);

            if (geoNft == null || geoNft.Result == null || geoNft.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding NFT. Reason: {geoNft.Message}");
                return result;
            }

            result = await NFTCommon.NFTManager.RemoveOASISNFTFromCollectionAsync(collection.Result.Id, geoNft.Result.Id);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("OASIS NFT Successfully Removed From Collection.");
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured removing NFT from collection. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IOASISNFTCollection>> DeleteNFTCollectionAsync(string collectionIdOrName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFTCollection> collection = await FindWeb4NFTCollectionAsync("delete", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref collection, $"Error occured finding NFT Collection to delete. Reason: {collection.Message}");
                return collection;
            }

            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteOASISNFTCollectionAsync(STAR.BeamedInAvatar.Id, collection.Result.Id, providerType: providerType);

            if (deleteResult != null && deleteResult.Result && !deleteResult.IsError)
            {
                CLIEngine.ShowSuccessMessage("OASIS NFT Collection Successfully Deleted.");
                return collection;
            }
            else
            {
                string msg = deleteResult != null ? deleteResult.Message : "";
                OASISErrorHandling.HandleError(ref collection, $"Error occured deleting NFT Collection. Reason: {msg}");
                return collection;
            }

            return collection;
        }

        private async Task<OASISResult<IOASISNFT>> FindWeb4NFTAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "NFT", ProviderType providerType = ProviderType.Default)
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
                            ListWeb4NFTs(searchResults);

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
                    ShowNFT(result.Result, DEFAULT_FIELD_LENGTH);

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

        private async Task<OASISResult<IOASISNFTCollection>> FindWeb4NFTCollectionAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "WEB4 NFT Collection", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFTCollection> result = new OASISResult<IOASISNFTCollection>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IOASISNFTCollection>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadNFTCollectionsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllNFTCollectionsAsync(providerType);

                        ListWeb4NFTCollections(starHolonsResult);

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
                    result = await NFTCommon.NFTManager.LoadOASISNFTCollectionAsync(id, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IOASISNFTCollection>> searchResults = await NFTCommon.NFTManager.SearchNFTCollectionsAsync(idOrName, STAR.BeamedInAvatar.Id, showOnlyForCurrentAvatar, providerType: providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListWeb4NFTCollections(searchResults);

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
                        CLIEngine.ShowErrorMessage($"An error occured calling FindWeb4GeoNFTCollectionAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null)
                    ShowNFTCollection(result.Result);

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

        private OASISResult<IEnumerable<IOASISNFT>> ListWeb4NFTs(OASISResult<IEnumerable<IOASISNFT>> nfts)
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

                        foreach (IOASISNFT nft in nfts.Result)
                            ShowNFT(nft, DEFAULT_FIELD_LENGTH);
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

        private OASISResult<IEnumerable<IOASISNFTCollection>> ListWeb4NFTCollections(OASISResult<IEnumerable<IOASISNFTCollection>> nfts)
        {
            if (nfts != null)
            {
                if (!nfts.IsError)
                {
                    if (nfts.Result != null && nfts.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (nfts.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{nfts.Result.Count()} WEB4 NFT Collection Found:");
                        else
                            CLIEngine.ShowMessage($"{nfts.Result.Count()} WEB4 Collection's Found:");

                        foreach (IOASISNFT nft in nfts.Result)
                            ShowNFT(nft, DEFAULT_FIELD_LENGTH);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No WEB4 Collection's Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading WEB4 Collection's. Reason: {nfts.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading WEB4 NFT Collection's.");

            return nfts;
        }

        private void ShowNFT(IOASISNFT nft, int displayFieldLength)
        {
            Console.WriteLine("");
            DisplayProperty("NFT DETAILS", "", displayFieldLength, false);
            Console.WriteLine("");
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
        }

        private void ShowNFTCollection(IOASISNFTCollection collection, bool showDetailed = true, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            Console.WriteLine("");
            DisplayProperty("NFT COLLECTION DETAILS", "", displayFieldLength, false);
            Console.WriteLine("");
            DisplayProperty("Id", collection.Id.ToString(), displayFieldLength);
            DisplayProperty("Title", collection.Name, displayFieldLength);
            DisplayProperty("Description", collection.Description, displayFieldLength);
            //DisplayProperty("Price", collection.Price.ToString(), displayFieldLength);
            //DisplayProperty("Discount", collection.Discount.ToString(), displayFieldLength);
            DisplayProperty("Created By Avatar Id", collection.CreatedByAvatarId.ToString(), displayFieldLength);
            DisplayProperty("Created On", collection.CreatedDate.ToString(), displayFieldLength);
            DisplayProperty("Modified By Avatar Id", collection.CreatedByAvatarId.ToString(), displayFieldLength);
            DisplayProperty("Modified On", collection.CreatedDate.ToString(), displayFieldLength);
            DisplayProperty("Image", collection.Image != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("Image Url", collection.ImageUrl, displayFieldLength);
            DisplayProperty("Thumbnail", collection.Thumbnail != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("Thumbnail Url", !string.IsNullOrEmpty(collection.ThumbnailUrl) ? collection.ThumbnailUrl : "None", displayFieldLength);
            ShowMetaData(collection.MetaData);
            ShowNFTCollectionNFTs(collection.OASISNFTs, showDetailed, 20);
        }

        private void ShowNFTCollectionNFTs(IEnumerable<IOASISNFT> nfts, bool showDetailed = false, int defaultFieldLength = 20)
        {
            CLIEngine.ShowMessage($"{nfts.Count()} NFT's in this collection:");

            if (showDetailed)
            {
                foreach (IOASISGeoSpatialNFT geoNFT in nfts)
                {
                    if (geoNFT != null)
                    {
                        Console.WriteLine("");
                        DisplayProperty("Geo-NFT Id", geoNFT.Id.ToString(), DEFAULT_FIELD_LENGTH);
                        DisplayProperty("Title", geoNFT.Title, DEFAULT_FIELD_LENGTH);
                        DisplayProperty("Description", geoNFT.Description, DEFAULT_FIELD_LENGTH);
                        DisplayProperty("Lat/Long", $"{geoNFT.Lat}/{geoNFT.Long}", DEFAULT_FIELD_LENGTH);
                    }
                }
            }
            else
            {
                CLIEngine.ShowMessage(string.Concat("Id".PadRight(defaultFieldLength), " | Title".PadRight(defaultFieldLength)));

                foreach (IOASISNFT geoNFT in nfts)
                {
                    if (geoNFT != null)
                        Console.WriteLine(string.Concat(geoNFT.Id.ToString().PadRight(defaultFieldLength), " | ", geoNFT.Title.PadRight(defaultFieldLength)));
                }
            }
        }
    }
}