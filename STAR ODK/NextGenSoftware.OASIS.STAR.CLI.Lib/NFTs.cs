using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

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
        //    OASISResult<IWeb4OASISNFT> NFTResult = await NFTManager.LoadNftAsync(geoNFTId);

        //    if (NFTResult != null && !NFTResult.IsError && NFTResult.Result != null)
        //        await base.CreateAsync(createParams, new STARNFT() { OASISNFTId = geoNFTId }, providerType);
        //    else
        //        CLIEngine.ShowErrorMessage("No NFT Found For That Id!");
        //}

        public override async Task<OASISResult<STARNFT>> CreateAsync(ISTARNETCreateOptions<STARNFT, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARNFT> result = new OASISResult<STARNFT>();
            OASISResult<IWeb4NFT> NFTResult = null;
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
                IWeb4NFT NFT = NFTResult.Result;

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS NFT to WEB5 STARNET which will create a WEB5 STAR NFT that wraps around the WEB4 OASISNFT allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended). Selecting 'Y' will also create a WEB3 JSONMetaData and a WEB4 OASISNFT json file in the WEB5 STAR NFT folder.")))
                {
                    Console.WriteLine("");

                    result = await base.CreateAsync(new STARNETCreateOptions<STARNFT, STARNETDNA>()
                    {
                        STARNETDNA = new STARNETDNA()
                        {
                            MetaData = new Dictionary<string, object>() { { "WEB4 NFT", NFT } }
                        },
                        STARNETHolon = new STARNFT()
                        {
                            OASISNFTId = NFTResult.Result.Id
                        }
                    }, holonSubType, showHeaderAndInro, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"OASISNFT_{NFTResult.Result.Id}.json"), JsonConvert.SerializeObject(NFT));

                        if (!string.IsNullOrEmpty(NFTResult.Result.JSONMetaData))
                            File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"JSONMetaData_{NFTResult.Result.Id}.json"), NFTResult.Result.JSONMetaData);

                        result.Result.NFTType = (NFTType)Enum.Parse(typeof(NFTType), result.Result.STARNETDNA.STARNETCategory.ToString());
                        OASISResult<STARNFT> saveResult = await result.Result.SaveAsync<STARNFT>();

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            if (NFT.MetaData == null)
                                NFT.MetaData = new Dictionary<string, object>();

                            NFT.MetaData["Web5STARNFTId"] = saveResult.Result.Id;
                            OASISResult<IWeb4NFT> web4NFT = await NFTCommon.NFTManager.UpdateWeb4NFTAsync(new UpdateWeb4NFTRequest() { Id = NFT.Id, ModifiedByAvatarId = STAR.BeamedInAvatar.Id, MetaData = NFT.MetaData }, providerType: providerType);

                            if (!(web4NFT != null && web4NFT.Result != null && !web4NFT.IsError))
                                OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 NFT after creation of WEB5 STAR NFT in CreateAsync method. Reason: {web4NFT.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured saving WEB5 STAR NFT after creation in CreateAsync method. Reason: {saveResult.Message}");
                    }
                }
            }
            else
            {
                if (!mint)
                //    OASISErrorHandling.HandleError(ref result, $"Error occured minting NFT in MintNFTAsync method. Reason: {NFTResult.Message}");
                //else
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading NFT in LoadNftAsync method. Reason: {NFTResult.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> MintNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            IMintWeb4NFTRequest request = await NFTCommon.GenerateNFTRequestAsync();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Minting WEB4 OASIS NFT & WEB3 NFT's...");
            result = await STAR.OASISAPI.NFTs.MintNftAsync(request);

            if (result != null && result.Result != null && !result.IsError)
            {
                //CLIEngine.ShowSuccessMessage($"OASIS NFT Successfully Minted. {nftResult.Message} Transaction Result: {nftResult.Result.TransactionResult}, Id: {nftResult.Result.OASISNFT.Id}, Hash: {nftResult.Result.OASISNFT.Hash} Minted On: {nftResult.Result.OASISNFT.MintedOn}, Minted By Avatar Id: {nftResult.Result.OASISNFT.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.OASISNFT.MintedByAddress}.");
                CLIEngine.ShowSuccessMessage(result.Message);
                //result.Result = nftResult.Result;
            }
            else
            {
                string msg = result != null ? result.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }
           
            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> RemintNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured reminting WEB4 OASIS NFT in RemintNFTAsync method. Reason: ";
            result = await FindWeb4NFTAsync("remint");

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                {
                    IRemintWeb4NFTRequest remintRequest = await NFTCommon.GenerateWeb4NFTRemintRequestAsync(result.Result);

                    CLIEngine.ShowWorkingMessage("Reminting WEB4 OASIS NFT & WEB3 NFT's...");
                    result = await STAR.OASISAPI.NFTs.RemintNftAsync(remintRequest);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        if (result.Result.MetaData != null && result.Result.MetaData.ContainsKey("Web5STARNFTId"))
                        {
                            OASISResult<STARNFT> web5NFT = await STAR.STARAPI.NFTs.LoadAsync(STAR.BeamedInAvatar.Id, new Guid(result.Result.MetaData["Web5STARNFTId"].ToString()));

                            if (web5NFT != null && web5NFT.Result != null && !web5NFT.IsError)
                            {
                                web5NFT.Result.STARNETDNA.MetaData["WEB4 NFT"] = result.Result;
                                OASISResult<STARNFT> saveWeb5NFT = await STAR.STARAPI.NFTs.UpdateAsync(STAR.BeamedInAvatar.Id, web5NFT.Result, true);

                                if (saveWeb5NFT != null && saveWeb5NFT.Result != null && !saveWeb5NFT.IsError)
                                {
                                    File.WriteAllText(Path.Combine(web5NFT.Result.STARNETDNA.SourcePath, $"OASISNFT_{result.Result.Id}.json"), JsonConvert.SerializeObject(result.Result));

                                    if (!string.IsNullOrEmpty(result.Result.JSONMetaData))
                                        File.WriteAllText(Path.Combine(web5NFT.Result.STARNETDNA.SourcePath, $"JSONMetaData_{result.Result.Id}.json"), result.Result.JSONMetaData);

                                    CLIEngine.ShowSuccessMessage(result.Message);
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed updating WEB5 STAR NFT after reminting WEB4 NFT. Reason: {saveWeb5NFT.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed loading WEB5 STAR NFT to update after reminting WEB4 NFT. Reason: {web5NFT.Message}");
                        }
                        else
                            CLIEngine.ShowSuccessMessage(result.Message);
                    }
                    else
                    {
                        string msg = result != null ? result.Message : "";
                        CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}", e);
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

            OASISResult<ISendWeb4NFTResponse> response = await STAR.OASISAPI.NFTs.SendNFTAsync(STAR.BeamedInAvatar.Id, new SendWeb4NFTRequest()
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

        public override async Task ShowAsync<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 35, object customData = null)
        {
            displayFieldLength = DEFAULT_FIELD_LENGTH;
            await base.ShowAsync(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            //if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("NFTId") && starHolon.STARNETDNA.MetaData["NFTId"] != null)
            //{
            //    Guid id = Guid.Empty;

            //    if (Guid.TryParse(starHolon.STARNETDNA.MetaData["NFTId"].ToString(), out id))
            //    {
            //        OASISResult<IWeb4OASISNFT> web4NFT = await NFTCommon.NFTManager.LoadNftAsync(id);

            //        if (web4NFT != null && web4NFT.Result != null && !web4NFT.IsError)
            //        {
            //            Console.WriteLine("");
            //            DisplayProperty("WEB4 NFT DETAILS", "", displayFieldLength, false);
            //            ShowNFT(web4NFT.Result, showHeader: false, showFooter: false);
            //        }
            //    }
            //}

            if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("WEB4 NFT") && starHolon.STARNETDNA.MetaData["WEB4 NFT"] != null)
            {
                IWeb4NFT nft = starHolon.STARNETDNA.MetaData["WEB4 NFT"] as IWeb4NFT;

                if (nft == null)
                    nft = JsonConvert.DeserializeObject<Web4NFT>(starHolon.STARNETDNA.MetaData["WEB4 NFT"].ToString());

                if (nft != null)
                {
                    Console.WriteLine("");
                    DisplayProperty("WEB4 NFT DETAILS", "", displayFieldLength, false);
                    ShowWeb4NFT(nft, showHeader: false, showFooter: false);
                }
            }

            CLIEngine.ShowDivider();
        }

        public async Task<OASISResult<IWeb4NFT>> BurnNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ImportNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            bool isWeb3 = false;

            if (mintParams != null)
                bool.TryParse(mintParams.ToString(), out isWeb3);

            if (isWeb3)
            {
                if (CLIEngine.GetConfirmation("Do you wish to import a WEB3 JSON MetaData file & then mint and wrap in a WEB4 OASIS NFT or import an existing minted NFT's token address and wrap in a WEB4 OASIS NFT? Press 'Y' for JSON File or 'N' for Token Address."))
                {
                    //WEB3 NFT Import from JSON MetaData file
                    string jsonPath = CLIEngine.GetValidFile("Please enter the full path to the JSON MetaData file you wish to import: ");

                    IMintWeb4NFTRequest request = await NFTCommon.GenerateNFTRequestAsync(jsonPath);

                    CLIEngine.ShowWorkingMessage("Minting WEB4 OASIS NFT...");
                    OASISResult<IWeb4NFT> nftResult = await STAR.OASISAPI.NFTs.MintNftAsync(request);
         
                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        CLIEngine.ShowSuccessMessage(nftResult.Message);
                        result.Result = nftResult.Result;
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
                        CLIEngine.ShowWorkingMessage("Importing WEB3 NFT...");

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
                            CLIEngine.ShowErrorMessage($"Failed to import WEB3 NFT: {msg}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.IsError = true;
                        result.Message = $"Error importing WEB3 NFT: {ex.Message}";
                        CLIEngine.ShowErrorMessage($"Error importing WEB3 NFT: {ex.Message}");
                    }
                }
            }
            else
            {
                // WEB4 OASIS NFT Import
                try
                {
                    string filePath = CLIEngine.GetValidFile("Please enter the full path to the WEB4 OASIS NFT file you wish to import: ");

                    OASISResult<IWeb4NFT> importResult = await NFTCommon.NFTManager.ImportWeb4NFTAsync(STAR.BeamedInAvatar.Id, filePath);

                    if (importResult != null && importResult.Result != null && !importResult.IsError)
                    {
                        CLIEngine.ShowSuccessMessage(importResult.Message);
                        result.Result = importResult.Result;
                        result.Message = importResult.Message;
                    }
                    else
                    {
                        string msg = importResult != null ? importResult.Message : "";
                        CLIEngine.ShowErrorMessage($"Failed to import WEB4 OASIS NFT: {msg}");
                    }
                }
                catch (Exception ex)
                {
                    result.IsError = true;
                    result.Message = $"Error importing WEB4 OASIS NFT: {ex.Message}";
                    CLIEngine.ShowErrorMessage($"Error importing WEB4 OASIS NFT: {ex.Message}");
                }
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ExportNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            return result;
        }

        //public async Task<OASISResult<IWeb4OASISNFT>> CloneNFTAsync(object mintParams = null)
        //{
        //    OASISResult<IWeb4OASISNFT> result = new OASISResult<IWeb4OASISNFT>();
        //    return result;
        //}

        public async Task<OASISResult<IWeb4NFT>> ConvertNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> UpdateWeb4NFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            UpdateWeb4NFTRequest request = new UpdateWeb4NFTRequest();

            OASISResult<IWeb4NFT> nftResult = await FindWeb4NFTAsync("update", idOrName, providerType: providerType);

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
            {
                OASISResult<IUpdateWeb4NFTRequest> updateResult = await NFTCommon.UpdateWeb4NFTAsync(request, nftResult.Result, "WEB4 NFT");

                if (updateResult != null && updateResult.Result != null && !updateResult.IsError)
                {
                    request = (UpdateWeb4NFTRequest)updateResult.Result;

                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage("Saving WEB4 NFT...");
                    result = await NFTCommon.NFTManager.UpdateWeb4NFTAsync(request, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        CLIEngine.ShowSuccessMessage("WEB4 OASIS NFT Successfully Updated.");
                        result = await NFTCommon.UpdateSTARNETHolonAsync("Web5STARNFTId", "WEB4 NFT", STARNETManager, result.Result.MetaData, result, providerType);
                    }
                    else
                    {
                        string msg = result != null ? result.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Updating WEB4 NFT Collection in UpdateWeb4NFTCollectionAsync method. Reason: {msg}");
                    }
                }
                else
                {
                    string msg = updateResult != null ? updateResult.Message : "";
                    OASISErrorHandling.HandleError(ref result, $"Error Occured Updating WEB4 NFT to update: {msg}");
                }
            }
            else
            {
                string msg = nftResult != null ? nftResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 NFT to update: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> DeleteWeb4NFTAsync(string idOrName, bool? softDelete = true, bool? deleteChildWeb3NFTs = false, bool? burnChildWebNFTs = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> nft = await FindWeb4NFTAsync("delete", idOrName, true);

            if (nft == null || nft.Result == null || nft.IsError)
            {
                OASISErrorHandling.HandleError(ref nft, $"Error occured finding WEB4 NFT to delete. Reason: {nft.Message}");
                return nft;
            }

            if (!softDelete.HasValue)
                softDelete = CLIEngine.GetConfirmation("Do you wish to permanently delete the Web4 NFT? (defaults to false)");
            
            if (!deleteChildWeb3NFTs.HasValue)
                deleteChildWeb3NFTs = CLIEngine.GetConfirmation("Do you wish to also delete the child Web3 NFTs? (the OASIS holon/metadata)(recommeneded/default)");
            
            if (!burnChildWebNFTs.HasValue)
                burnChildWebNFTs = CLIEngine.GetConfirmation("Do you wish to also burn the child Web3 NFTs? (permanently destroy the Web3 NFTs on-chain) (recommeneded/default)");

            CLIEngine.ShowWorkingMessage("Deleting WEB4 NFT...");
            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteWeb4NFTAsync(STAR.BeamedInAvatar.Id, nft.Result.Id, softDelete.Value, deleteChildWeb3NFTs.Value, burnChildWebNFTs.Value, providerType: providerType);

            if (deleteResult != null && deleteResult.Result && !deleteResult.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 NFT Successfully Deleted.");
                nft = await NFTCommon.DeleteAllSTARNETVersionsAsync("Web5STARNFTId", STARNETManager, nft.Result.MetaData, nft, providerType);
            }
            else
            {
                string msg = deleteResult != null ? deleteResult.Message : "";
                OASISErrorHandling.HandleError(ref nft, $"Error occured deleting WEB4 NFT. Reason: {msg}");
            }

            return nft;
        }

        //public virtual async Task<OASISResult<IEnumerable<IWeb4OASISNFT>>> ListAllWeb4NFTsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<IEnumerable<IWeb4NFT>>> ListAllWeb4NFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(await NFTCommon.NFTManager.LoadAllWeb4NFTsAsync(providerType));
        }

        //public virtual OASISResult<IEnumerable<IWeb4OASISNFT>> ListAllWeb4NFTs(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual OASISResult<IEnumerable<IWeb4NFT>> ListAllWeb4NFTs(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(NFTCommon.NFTManager.LoadAllWeb4NFTs(providerType));
        }

        //public virtual async Task<OASISResult<IEnumerable<IWeb4OASISNFT>>> ListAllWeb4NFTForAvatarsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<IEnumerable<IWeb4NFT>>> ListAllWeb4NFTForAvatarsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(await NFTCommon.NFTManager.LoadAllWeb4NFTsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType));
        }

        //public virtual OASISResult<IEnumerable<IWeb4OASISNFT>> ListAllWeb4NFTsForAvatar(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual OASISResult<IEnumerable<IWeb4NFT>> ListAllWeb4NFTsForAvatar(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");
            return ListWeb4NFTs(NFTCommon.NFTManager.LoadAllWeb4NFTsForAvatar(STAR.BeamedInAvatar.Id, providerType));
        }

        public virtual async Task<OASISResult<IWeb4NFT>> ShowWeb4NFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT's...");

            result = await FindWeb4NFTAsync("view", idOrName, true, providerType: providerType);

            //if (result != null && result.Result != null && !result.IsError)
            //    ShowNFT(result.Result);
            //else
            //    OASISErrorHandling.HandleError(ref result, "No WEB4 NFT Found For That Id or Name!");

            return result;
        }

        public virtual async Task SearchWeb4NFTAsync(string searchTerm = "", bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
                searchTerm = CLIEngine.GetValidInput($"What is the name of the WEB4 NFT you wish to search for?");

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching WEB4 NFT's...");
            ListWeb4NFTs(await NFTCommon.NFTManager.SearchWeb4NFTsAsync(searchTerm, STAR.BeamedInAvatar.Id, searchOnlyForCurrentAvatar: !showForAllAvatars, providerType: providerType));
        }

        public async Task<OASISResult<IWeb4NFT>> FindWeb4NFTAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "WEB4 NFT", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IWeb4NFT>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllWeb4NFTsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllWeb4NFTsAsync(providerType);

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
                    result = await NFTCommon.NFTManager.LoadWeb4NftAsync(id, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.MintedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was minted by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IWeb4NFT>> searchResults = await NFTCommon.NFTManager.SearchWeb4NFTsAsync(idOrName, STAR.BeamedInAvatar.Id, searchOnlyForCurrentAvatar: showOnlyForCurrentAvatar, providerType: providerType);

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
                    ShowWeb4NFT(result.Result);

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

        private OASISResult<IEnumerable<IWeb4NFT>> ListWeb4NFTs(OASISResult<IEnumerable<IWeb4NFT>> nfts, bool showNumbers = false, bool showDetailedInfo = false)
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

                        for (int i = 0; i < nfts.Result.Count(); i++)
                            ShowWeb4NFT(nfts.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showDetailedInfo);
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

        private void ShowWeb4NFT(IWeb4NFT web4NFT, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 39)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            NFTCommon.ShowNFTDetails(web4NFT, null, displayFieldLength);

            if (web4NFT.Web3NFTs.Count > 0)
            {
                //Console.WriteLine("");
                DisplayProperty("WEB3 NFT's", "", displayFieldLength);
                Console.WriteLine("");

                foreach (Web3NFT web3NFT in web4NFT.Web3NFTs)
                {
                    NFTCommon.ShowNFTDetails(web3NFT, web4NFT, displayFieldLength);
                    DisplayProperty("Send NFT Transaction Hash", web3NFT.SendNFTTransactionHash, displayFieldLength);
                    DisplayProperty("OASIS MintWallet Address", web3NFT.OASISMintWalletAddress, displayFieldLength);
                    DisplayProperty("Mint Transaction Hash", web3NFT.MintTransactionHash, displayFieldLength);
                    DisplayProperty("NFT Token Address", web3NFT.NFTTokenAddress, displayFieldLength);
                    DisplayProperty("Update Authority", web3NFT.UpdateAuthority, displayFieldLength);
                    CLIEngine.ShowDivider();
                }

                CLIEngine.ShowMessage("NOTE: Only the deltas are shown between the WEB3 NFT and it's parent WEB4 NFT so if a field/property is not shown above for the WEB3 NFT then that means it defaults to it's parent WEB4 NFT.");
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }
    }
}