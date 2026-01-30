using ADRaffy.ENSNormalize;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class GeoNFTs : STARNETUIBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, GeoNFTDNA>
    public class GeoNFTs : STARNETUIBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, STARNETDNA>
    {
        protected new const int DEFAULT_FIELD_LENGTH = 40;

        public NFTCommon NFTCommon { get; set; } = new NFTCommon();

        public GeoNFTs(Guid avatarId, STARDNA STARDNA) : base(new STARGeoNFTManager(avatarId, STARDNA),
            "Welcome to the WEB5 STAR GeoNFT Wizard", new List<string> 
            {
                "This wizard will allow you create a WEB5 STAR GeoNFT which wraps around a WEB4 OASIS GeoNFT, which in turn wraps around a WEB4 OASIS NFT.",
                "You can mint a WEB4 OASIS NFT using the 'nft mint' sub-command.",
                "You can mint a WEB4 GeoNFT using the 'geonft mint' sub-command. This will automatically create the WEB4 OASIS NFT to wrap around or it can wrap around an existing WEB4 OASIS NFT.",
                "You can then convert or wrap around the WEB4 OASIS GeoNFT using the sub-command 'geonft create'.",
                "A WEB5 GeoNFT can then be published to STARNET in much the same way as everything else within STAR using the same sub-commands such as publish, download, install etc.",
                "Both WEB4 and WEB5 STAR GeoNFT's can be placed in any location within Our World as part of Quest's. The main difference is WEB5 STAR GeoNFT's can be published to STARNET, version controlled, shared, used in Our World (support for Web4 GeoNFT's may be added later), Quests etc whereas WEB4 GeoNFT's cannot.",
                "The wizard will create an empty folder with a GeoNFTDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the GeoNFT into this folder.",
                "Finally you run the sub-command 'geonft publish' to convert the folder containing the GeoNFT (can contain any number of files and sub-folders) into a OASIS GeoNFT file (.ogeonft) as well as optionally upload to STARNET.",
                "You can then share the .ogeonft file with others across any platform or OS, who can then install the GeoNFT from the file using the sub-command 'geonft install'.",
                "You can also optionally choose to upload the .ogeonft file to the STARNET store so others can search, download and install the GeoNFT."
            },
            STAR.STARDNA.DefaultGeoNFTsSourcePath, "DefaultGeoNFTsSourcePath",
            STAR.STARDNA.DefaultGeoNFTsPublishedPath, "DefaultGeoNFTsPublishedPath",
            STAR.STARDNA.DefaultGeoNFTsDownloadedPath, "DefaultGeoNFTsDownloadedPath",
            STAR.STARDNA.DefaultGeoNFTsInstalledPath, "DefaultGeoNFTsInstalledPath", DEFAULT_FIELD_LENGTH)
        { }

        public override async Task<OASISResult<STARGeoNFT>> CreateAsync(ISTARNETCreateOptions<STARGeoNFT, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARGeoNFT> result = new OASISResult<STARGeoNFT>();
            OASISResult<IWeb4GeoSpatialNFT> geoNFTResult = null;
            bool mint = false;

            ShowHeader();

            if (CLIEngine.GetConfirmation("Do you have an existing WEB4 OASIS Geo-NFT you wish to create a WEB5 Geo-NFT from?"))
            {
                Console.WriteLine("");
                geoNFTResult = await FindWeb4GeoNFTAsync("wrap");
            }
            else
            {
                Console.WriteLine("");
                geoNFTResult = await MintGeoNFTAsync(); //Mint WEB4 GeoNFT (mints and wraps around a WEB4 OASIS NFT).
                mint = true;
            }

            if (geoNFTResult != null && geoNFTResult.Result != null && !geoNFTResult.IsError)
            {
                IWeb4GeoSpatialNFT geoNFT = geoNFTResult.Result;

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS Geo-NFT to WEB5 STARNET which will create a WEB5 STAR GeoNFT that wraps around the WEB4 GeoNFT allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended). Selecting 'Y' will also create a WEB3 JSONMetaData and a WEB4 OASIS GeoNFT json file in the WEB5 STAR GeoNFT folder.")))
                {
                    Console.WriteLine("");

                    result = await base.CreateAsync(new STARNETCreateOptions<STARGeoNFT, STARNETDNA>()
                    {
                        STARNETDNA = new STARNETDNA()
                        {
                            MetaData = new Dictionary<string, object>() { { "WEB4 GeoNFT", geoNFT } }
                        },
                        STARNETHolon = new STARGeoNFT() 
                        { 
                            GeoNFTId = geoNFTResult.Result.Id 
                        }
                    }, holonSubType, showHeaderAndInro, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        UpdateWeb4AndWeb3GeoNFTJSONFiles(geoNFT, result.Result.STARNETDNA.SourcePath);

                        //File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"WEB4_GEONFT_{geoNFTResult.Result.Id}.json"), JsonConvert.SerializeObject(geoNFT));

                        //if (!string.IsNullOrEmpty(geoNFTResult.Result.JSONMetaData))
                        //    File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"WEB4_JSONMetaData_{geoNFTResult.Result.Id}.json"), geoNFTResult.Result.JSONMetaData);

                        //foreach (IWeb3NFT web3Nft in geoNFTResult.Result.Web3NFTs)
                        //{
                        //    File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"WEB3_NFT_{web3Nft.Id}.json"), JsonConvert.SerializeObject(web3Nft));

                        //    if (!string.IsNullOrEmpty(web3Nft.JSONMetaData))
                        //        File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"WEB3_JSONMetaData_{web3Nft.Id}.json"), web3Nft.JSONMetaData);
                        //}

                        result.Result.NFTType = (NFTType)Enum.Parse(typeof(NFTType), result.Result.STARNETDNA.STARNETCategory.ToString());
                        OASISResult<STARGeoNFT> saveResult = await result.Result.SaveAsync<STARGeoNFT>();

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            geoNFT.MetaData["Web5STARGeoNFTId"] = saveResult.Result.Id.ToString();
                            OASISResult<IWeb4GeoSpatialNFT> web4GeoNFT = await NFTCommon.NFTManager.UpdateWeb4GeoNFTAsync(new UpdateWeb4GeoNFTRequest() { Id = geoNFT.Id, ModifiedByAvatarId = STAR.BeamedInAvatar.Id, MetaData = geoNFT.MetaData }, providerType: providerType);

                            if (!(web4GeoNFT != null && web4GeoNFT.Result != null && !web4GeoNFT.IsError))
                                OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 Geo-NFT after creation of WEB5 STAR Geo-NFT in CreateAsync method. Reason: {web4GeoNFT.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured saving WEB5 STAR Geo-NFT after creation in CreateAsync method. Reason: {saveResult.Message}");
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
            {
                if (mint)
                    OASISErrorHandling.HandleError(ref result, $"Error occured minting WEB4 GeoNFT in MintGeoNFTAsync method. Reason: {geoNFTResult.Message}");
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading WEB4 GeoNFT in LoadGeoNftAsync method. Reason: {geoNFTResult.Message}");
            }

            return result;
        }

        public override async Task ShowAsync<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH, object customData = null)
        {
            displayFieldLength = DEFAULT_FIELD_LENGTH;
            await base.ShowAsync(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            //if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("GeoNFTId") && starHolon.STARNETDNA.MetaData["GeoNFTId"] != null)
            //{
            //    Guid id = Guid.Empty;

            //    if (Guid.TryParse(starHolon.STARNETDNA.MetaData["GeoNFTId"].ToString(), out id))
            //    {
            //        OASISResult<IWeb4GeoSpatialNFT> web4GeoNFT = await NFTCommon.NFTManager.LoadGeoNftAsync(id);

            //        if (web4GeoNFT != null && web4GeoNFT.Result != null && !web4GeoNFT.IsError)
            //        {
            //            Console.WriteLine("");
            //            DisplayProperty("WEB4 GEO-NFT DETAILS", "", displayFieldLength, false);
            //            ShowGeoNFT(web4GeoNFT.Result, showHeader: false, showFooter: false);
            //        }
            //    }
            //}

            if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("WEB4 GeoNFT") && starHolon.STARNETDNA.MetaData["WEB4 GeoNFT"] != null)
            {
                IWeb4GeoSpatialNFT geoNFT = starHolon.STARNETDNA.MetaData["WEB4 GeoNFT"] as IWeb4GeoSpatialNFT;

                if (geoNFT == null)
                    geoNFT = JsonConvert.DeserializeObject<Web4OASISGeoSpatialNFT>(starHolon.STARNETDNA.MetaData["WEB4 GeoNFT"].ToString());

                if (geoNFT != null)
                {
                    Console.WriteLine("");
                    DisplayProperty("WEB4 GEO-NFT DETAILS", "", displayFieldLength, false);
                    ShowGeoNFT(geoNFT, showHeader: false, showFooter: false);
                }
            }

            CLIEngine.ShowDivider();
        }

        public override async Task<OASISResult<STARGeoNFT>> DeleteAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARGeoNFT> result = await base.DeleteAsync(idOrName, softDelete, providerType);

            if (result != null && result.Result != null && !result.IsError && result.IsDeleted)
            {
                if (result.Result.STARNETDNA != null && result.Result.STARNETDNA.MetaData != null && result.Result.STARNETDNA.MetaData.ContainsKey("WEB4 GEONFT") && result.Result.STARNETDNA.MetaData["WEB4 GEONFT"] != null)
                {
                    IWeb4GeoSpatialNFT nft = result.Result.STARNETDNA.MetaData["WEB4 GEONFT"] as IWeb4GeoSpatialNFT;

                    if (nft == null)
                        nft = JsonConvert.DeserializeObject<IWeb4GeoSpatialNFT>(result.Result.STARNETDNA.MetaData["WEB4 GEONFT"].ToString());

                    if (nft != null)
                    {
                        //Need to delete the link from Web4 to Web5 in the web4 metadata here.
                        nft.MetaData.Remove("Web5STARNFTId");
                        OASISResult<IWeb4GeoSpatialNFT> web4NFT = await NFTCommon.NFTManager.UpdateWeb4GeoNFTAsync(new UpdateWeb4GeoNFTRequest() { Id = nft.Id, ModifiedByAvatarId = STAR.BeamedInAvatar.Id, MetaData = nft.MetaData }, providerType: providerType);

                        if (!(web4NFT != null && web4NFT.Result != null && !web4NFT.IsError))
                            OASISErrorHandling.HandleError(ref result, $"Error occured removing WEB5 GeoNFT ID link from the metadata on it's child/wrapped WEB4 GeoNFT {nft.Id} and title {nft.Title}. Reason: {web4NFT.Message}");
                        else
                            CLIEngine.ShowSuccessMessage("WEB4 Link To WEB5 Removed.");

                        if (CLIEngine.GetConfirmation($"Do you wish to also delete the child WEB4 GeoNFT ({nft.Title}) and optionally it's child WEB3 NFT's?"))
                            await DeleteWeb4GeoNFTAsync(nft.Id.ToString(), providerType);
                        else
                            Console.WriteLine("");
                    }
                }
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> MintGeoNFTAsync(object mintParams = null)
        {
            IMintWeb4NFTRequest request = await NFTCommon.GenerateNFTRequestAsync();
            IPlaceWeb4GeoSpatialNFTRequest geoRequest = await GenerateGeoNFTRequestAsync(false);

            CLIEngine.ShowWorkingMessage("Minting WEB4 OASIS Geo-NFT...");
            OASISResult<IWeb4GeoSpatialNFT> nftResult = await STAR.OASISAPI.NFTs.MintAndPlaceWeb4GeoNFTAsync(new MintAndPlaceWeb4GeoSpatialNFTRequest()
            {
                Title = request.Title,
                Description = request.Description,
                MemoText = request.MemoText,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                MintedByAvatarId = request.MintedByAvatarId,
                //MintWalletAddress = request.MintWalletAddress,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                Price = request.Price,
                Discount = request.Discount,
                OnChainProvider = request.OnChainProvider,
                OffChainProvider = request.OffChainProvider,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NumberToMint = request.NumberToMint,
                MetaData = request.MetaData,
                AllowOtherPlayersToAlsoCollect = geoRequest.AllowOtherPlayersToAlsoCollect,
                PermSpawn = geoRequest.PermSpawn,
                GlobalSpawnQuantity = geoRequest.GlobalSpawnQuantity,
                PlayerSpawnQuantity = geoRequest.PlayerSpawnQuantity,
                RespawnDurationInSeconds = geoRequest.RespawnDurationInSeconds,
                Lat = geoRequest.Lat,
                Long = geoRequest.Long,
                Nft2DSprite = geoRequest.Nft2DSprite,
                Nft2DSpriteURI = geoRequest.Nft2DSpriteURI,
                Nft3DObject = geoRequest.Nft3DObject,
                Nft3DObjectURI = geoRequest.Nft3DObjectURI,
                PlacedByAvatarId = geoRequest.PlacedByAvatarId,
                GeoNFTMetaDataProvider = geoRequest.GeoNFTMetaDataProvider,
                JSONMetaDataURL = request.JSONMetaDataURL,
                NFTOffChainMetaType = request.NFTOffChainMetaType,
                NFTStandardType = request.NFTStandardType,
                Symbol = request.Symbol,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                WaitTillNFTSent = request.WaitTillNFTSent,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds
            });

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                //CLIEngine.ShowSuccessMessage($"OASIS Geo-NFT Successfully Minted. {nftResult.Message} Id: {nftResult.Result.Id}, Hash: {nftResult.Result.Hash} Minted On: {nftResult.Result.MintedOn}, Minted By Avatar Id: {nftResult.Result.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.MintedByAddress}.");
                CLIEngine.ShowSuccessMessage(nftResult.Message);
            else
            {
                string msg = nftResult != null ? nftResult.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }

            return nftResult;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> RemintGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured reminting WEB4 OASIS GeoNFT in RemintGeoNFTAsync method. Reason: ";
            result = await FindWeb4GeoNFTAsync("remint");

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                {
                    IRemintWeb4GeoNFTRequest remintRequest = await NFTCommon.GenerateWeb4GeoNFTRemintRequestAsync(result.Result);

                    CLIEngine.ShowWorkingMessage("Reminting WEB4 OASIS GeoNFT & WEB3 NFT's...");
                    result = await STAR.OASISAPI.NFTs.RemintGeoNftAsync(remintRequest);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        if (result.Result.MetaData != null && result.Result.MetaData.ContainsKey("Web5STARNFTId"))
                        {
                            OASISResult<STARGeoNFT> web5NFT = await STAR.STARAPI.GeoNFTs.LoadAsync(STAR.BeamedInAvatar.Id, new Guid(result.Result.MetaData["Web5STARNFTId"].ToString()));

                            if (web5NFT != null && web5NFT.Result != null && !web5NFT.IsError)
                            {
                                web5NFT.Result.STARNETDNA.MetaData["WEB4 NFT"] = result.Result;
                                OASISResult<STARGeoNFT> saveWeb5NFT = await STAR.STARAPI.GeoNFTs.UpdateAsync(STAR.BeamedInAvatar.Id, web5NFT.Result, true);

                                if (saveWeb5NFT != null && saveWeb5NFT.Result != null && !saveWeb5NFT.IsError)
                                {
                                    UpdateWeb4AndWeb3GeoNFTJSONFiles(result.Result, web5NFT.Result.STARNETDNA.SourcePath);
                                    CLIEngine.ShowSuccessMessage(result.Message);
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed updating WEB5 STAR GeoNFT after reminting WEB4 GeoNFT. Reason: {saveWeb5NFT.Message}");
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed loading WEB5 STAR GeoNFT to update after reminting WEB4 GeoNFT. Reason: {web5NFT.Message}");
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

        public async Task PlaceGeoNFTAsync()
        {
            IPlaceWeb4GeoSpatialNFTRequest geoRequest = await GenerateGeoNFTRequestAsync(true);
            CLIEngine.ShowWorkingMessage("Creating WEB4 OASIS Geo-NFT...");
            OASISResult<IWeb4GeoSpatialNFT> nftResult = await STAR.OASISAPI.NFTs.PlaceWeb4GeoNFTAsync(geoRequest);

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                //CLIEngine.ShowSuccessMessage($"OASIS Geo-NFT Successfully Created. {nftResult.Message} OriginalOASISNFTId: {nftResult.Result.OriginalOASISNFTId}, Id: {nftResult.Result.Id}, Hash: {nftResult.Result.Hash} Minted On: {nftResult.Result.MintedOn}, Minted By Avatar Id: {nftResult.Result.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.MintedByAddress}.");
                CLIEngine.ShowSuccessMessage(nftResult.Message);
            else
            {
                string msg = nftResult != null ? nftResult.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }
        }

        public async Task SendGeoNFTAsync()
        {
            //string mintWalletAddress = CLIEngine.GetValidInput("What is the original mint address?");
            string fromWalletAddress = CLIEngine.GetValidInput("What address are you sending the GeoNFT from?");
            string toWalletAddress = CLIEngine.GetValidInput("What address are you sending the GeoNFT to?");
            string tokenAddress = CLIEngine.GetValidInput("What is the token address of the NFT?");
            string memoText = CLIEngine.GetValidInput("What is the memo text?");
            //decimal amount = CLIEngine.GetValidInputForDecimal("What is the amount?");

            CLIEngine.ShowWorkingMessage("Sending WEB4 GeoNFT...");

            OASISResult<ISendWeb4NFTResponse> response = await STAR.OASISAPI.NFTs.SendNFTAsync(STAR.BeamedInAvatar.Id, new SendWeb4NFTRequest()
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                TokenAddress = tokenAddress,
                MemoText = memoText,
            });

            if (response != null && response.Result != null && !response.IsError)
                //CLIEngine.ShowSuccessMessage($"GeoNFT Successfully Sent. {response.Message} Hash: {response.Result.TransactionResult}");
                CLIEngine.ShowSuccessMessage(response.Message);
            else
            {
                string msg = response != null ? response.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> BurnGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();

            OASISResult<IWeb3NFTTransactionResponse> burnResult = await NFTCommon.NFTManager.BurnWeb3NFTAsync(new BurnWeb3NFTRequest()
            {
                OwnerPublicKey = CLIEngine.GetValidInput("Please enter the Public Key of the wallet that owns the NFT: "),
                OwnerPrivateKey = CLIEngine.GetValidInput("Please enter the Private Key of the wallet that owns the NFT: "),
                OwnerSeedPhrase = CLIEngine.GetValidInput("Please enter the Seed Phrase of the wallet that owns the NFT: "),
                NFTTokenAddress = CLIEngine.GetValidInput("Please enter the Token Address of the NFT you wish to burn: "),
                BurntByAvatarId = STAR.BeamedInAvatar.Id
            });

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("NFT Successfully Burnt.");
            else
                CLIEngine.ShowSuccessMessage($"Error Burning NFT, Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ImportGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();

            try
            {
                string filePath = CLIEngine.GetValidFile("Please enter the full path to the WEB4 OASIS GeoNFT file you wish to import: ");

                OASISResult<IWeb4GeoSpatialNFT> importResult = await NFTCommon.NFTManager.ImportWeb4GeoNFTAsync(STAR.BeamedInAvatar.Id, filePath);

                if (importResult != null && importResult.Result != null && !importResult.IsError)
                {
                    CLIEngine.ShowSuccessMessage(importResult.Message);
                    result.Result = importResult.Result;
                    result.Message = importResult.Message;
                }
                else
                {
                    string msg = importResult != null ? importResult.Message : "";
                    CLIEngine.ShowErrorMessage($"Failed to import WEB4 OASIS GeoNFT: {msg}");
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error importing WEB4 OASIS GeoNFT: {ex.Message}";
                CLIEngine.ShowErrorMessage($"Error importing WEB4 OASIS GeoNFT: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ExportGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();

            try
            {
                OASISResult<IWeb4GeoSpatialNFT> geoNFTResult = await FindWeb4GeoNFTAsync("export");
                
                if (geoNFTResult == null || geoNFTResult.Result == null || geoNFTResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading WEB4 Geo-NFT in ExportGeoNFTAsync method. Reason: {geoNFTResult.Message}");
                    return result;
                }

                string filePath = CLIEngine.GetValidFile("Please enter the full path to where you would like to export the WEB4 OASIS GeoNFT file: ");
                OASISResult<IWeb4GeoSpatialNFT> exportResult = await NFTCommon.NFTManager.ExportWeb4GeoNFTAsync(geoNFTResult.Result.Id, filePath);

                if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
                {
                    CLIEngine.ShowSuccessMessage(exportResult.Message);
                    result.Result = exportResult.Result;
                    result.Message = exportResult.Message;
                }
                else
                {
                    string msg = exportResult != null ? exportResult.Message : "";
                    CLIEngine.ShowErrorMessage($"Failed to export WEB4 OASIS GeoNFT: {msg}");
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error exporting WEB4 OASIS GeoNFT: {ex.Message}";
                CLIEngine.ShowErrorMessage($"Error importing WEB4 OASIS GeoNFT: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ConvertGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> ListAllWeb4GeoNFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(await NFTCommon.NFTManager.LoadAllWeb4GeoNFTsAsync(providerType));
        }

        public virtual OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> ListAllWeb4GeoNFTs(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(NFTCommon.NFTManager.LoadAllWeb4GeoNFTs(providerType));
        }

        //public virtual async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> ListAllWeb4GeoNFTForAvatarsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> ListAllWeb4GeoNFTForAvatarsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(await NFTCommon.NFTManager.LoadAllWeb4GeoNFTsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType));
        }

        //public virtual OASISResult<IEnumerable<IOASISGeoSpatialNFT>> ListAllWeb4GeoNFTsForAvatar(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> ListAllWeb4GeoNFTsForAvatar(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(NFTCommon.NFTManager.LoadAllWeb4GeoNFTsForAvatar(STAR.BeamedInAvatar.Id, providerType));
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> UpdateWeb4GeoNFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            UpdateWeb4GeoNFTRequest request = new UpdateWeb4GeoNFTRequest();

            OASISResult<IWeb4GeoSpatialNFT> nftResult = await FindWeb4GeoNFTAsync("update", idOrName, providerType: providerType);

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
            {
                // Prefill request with existing values so unchanged fields are preserved
                var existing = nftResult.Result;

                request.Id = existing.Id;
                request.Title = existing.Title;
                request.Description = existing.Description;
                request.Image = existing.Image;
                request.ImageUrl = existing.ImageUrl;
                request.Thumbnail = existing.Thumbnail;
                request.ThumbnailUrl = existing.ThumbnailUrl;
                request.Price = existing.Price;
                request.Discount = existing.Discount;
                request.Tags = existing.Tags != null ? new List<string>(existing.Tags) : null;
                request.MetaData = existing.MetaData != null ? new Dictionary<string, string>(existing.MetaData) : new Dictionary<string, string>();

                // Geo specific
                request.Lat = existing.Lat;
                request.Long = existing.Long;
                request.AllowOtherPlayersToAlsoCollect = existing.AllowOtherPlayersToAlsoCollect;
                request.PermSpawn = existing.PermSpawn;
                request.GlobalSpawnQuantity = existing.GlobalSpawnQuantity;
                request.PlayerSpawnQuantity = existing.PlayerSpawnQuantity;
                request.RespawnDurationInSeconds = existing.RespawnDurationInSeconds;
                request.Nft3DObject = existing.Nft3DObject;
                request.Nft3DObjectURI = existing.Nft3DObjectURI;
                request.Nft2DSprite = existing.Nft2DSprite;
                request.Nft2DSpriteURI = existing.Nft2DSpriteURI;

                OASISResult<IUpdateWeb4NFTRequest> updateResult = await NFTCommon.UpdateWeb4NFTAsync(request, nftResult.Result, "WEB4 GeoNFT", false, false);

                if (updateResult != null && updateResult.Result != null && !updateResult.IsError)
                {
                    request = (UpdateWeb4GeoNFTRequest)updateResult.Result;

                    // Geo specific edits
                    if (CLIEngine.GetConfirmation("Do you wish to edit the Lat/Long location?"))
                    {
                        request.Lat = CLIEngine.GetValidInputForLong("Please enter the new Lat location:", addLineBefore: true);
                        request.Long = CLIEngine.GetValidInputForLong("Please enter the new Long location:", addLineBefore: true);
                    }
                    else
                        Console.WriteLine("");

                    if (CLIEngine.GetConfirmation("Do you wish to edit spawn settings (PermSpawn / AllowOtherPlayersToAlsoCollect)?"))
                    {
                        Console.WriteLine("");
                        request.PermSpawn = CLIEngine.GetConfirmation("Will the NFT be permanently spawned? Press 'Y' for Yes or 'N' for No.");

                        if (!request.PermSpawn.Value)
                        {
                            Console.WriteLine("");
                            request.AllowOtherPlayersToAlsoCollect = CLIEngine.GetConfirmation("Once the NFT has been collected by a given player/avatar, do you want it to also still be collectable by other players/avatars? (Press Y for Yes or N for No)");

                            if (request.AllowOtherPlayersToAlsoCollect.Value)
                            {
                                Console.WriteLine("");
                                request.GlobalSpawnQuantity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected?");
                                request.RespawnDurationInSeconds = CLIEngine.GetValidInputForInt("How long will it take (in seconds) for the NFT to re-spawn once it has been collected?");
                                request.PlayerSpawnQuantity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected for a given player/avatar? (If you want to enforce that players/avatars can only collect each NFT once then set this to 0.)");
                            }
                        }
                    }
                    else
                        Console.WriteLine("");

                    if (CLIEngine.GetConfirmation("Do you wish to update the 2D sprite or 3D object assets?", addLineBefore: true))
                    {
                        Console.WriteLine("");
                        OASISResult<ImageObjectResult> imageObjectResult = await ProcessImageOrObjectAsync("WEB4 Geo-NFT");

                        if (imageObjectResult != null && imageObjectResult.Result != null && !imageObjectResult.IsError)
                        {
                            request.Nft3DObject = imageObjectResult.Result.Object3D;
                            request.Nft3DObjectURI = imageObjectResult.Result.Object3DURI != null ? imageObjectResult.Result.Object3DURI.AbsoluteUri : request.Nft3DObjectURI;
                            request.Nft2DSprite = imageObjectResult.Result.Image2D;
                            request.Nft2DSpriteURI = imageObjectResult.Result.Image2DURI != null ? imageObjectResult.Result.Image2DURI.AbsoluteUri : request.Nft2DSpriteURI;
                        }
                        else
                        {
                            string msg = imageObjectResult != null ? imageObjectResult.Message : "";
                            OASISErrorHandling.HandleError(ref result, $"Error Occured Processing 2D/3D assets for WEB4 Geo-NFT: {msg}");
                            return result;
                        }
                    }
                    else
                        Console.WriteLine("");

                    request.Tags = TagHelper.ManageTags(nftResult.Result.Tags);
                    request.MetaData = MetaDataHelper.ManageMetaData(nftResult.Result.MetaData, "WEB4 Geo-NFT");

                    CLIEngine.ShowWorkingMessage("Saving WEB4 Geo-NFT...");
                    result = await NFTCommon.NFTManager.UpdateWeb4GeoNFTAsync(request, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        CLIEngine.ShowSuccessMessage("WEB4 OASIS GeoNFT Successfully Updated.");
                        result = await NFTCommon.UpdateSTARNETHolonAsync("Web5STARGeoNFTId", "GeoNFT", STARNETManager, result.Result.MetaData, result, providerType);

                        if (result != null && result.Result != null && !result.IsError && result.Result.MetaData != null && result.Result.MetaData.ContainsKey("Web5STARNFTId") && Guid.TryParse(result.Result.MetaData["Web5STARNFTId"].ToString(), out Guid web5Id))
                        {
                            var starNFTResult = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, web5Id, providerType: providerType);

                            if (starNFTResult != null && starNFTResult.Result != null && !starNFTResult.IsError)
                                UpdateWeb4AndWeb3GeoNFTJSONFiles(result.Result, starNFTResult.Result.STARNETDNA.SourcePath);
                        }
                    }
                    else
                    {
                        string msg = result != null ? result.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Updating WEB4 GeoNFT in UpdateWeb4GeoNFTAsync method. Reason: {msg}");
                    }
                }
                
                //if (CLIEngine.GetConfirmation("Do you wish to edit the Title?"))
                //    request.Title = CLIEngine.GetValidInput("Please enter the new title: ", addLineBefore: true);
                //else
                //    Console.WriteLine("");

                //if (CLIEngine.GetConfirmation("Do you wish to edit the Description?"))
                //    request.Description = CLIEngine.GetValidInput("Please enter the new description: ", addLineBefore: true);
                //else
                //    Console.WriteLine("");

                //request.ModifiedByAvatarId = STAR.BeamedInAvatar.Id;

                //if (CLIEngine.GetConfirmation("Do you wish to update the Image and Thumbnail?"))
                //{
                //    Console.WriteLine("");
                //    OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("WEB4 Geo-NFT");

                //    if (imageAndThumbnailResult != null && imageAndThumbnailResult.Result != null && !imageAndThumbnailResult.IsError)
                //    {
                //        request.Image = imageAndThumbnailResult.Result.Image;
                //        request.ImageUrl = imageAndThumbnailResult.Result.ImageUrl;
                //        request.Thumbnail = imageAndThumbnailResult.Result.Thumbnail;
                //        request.ThumbnailUrl = imageAndThumbnailResult.Result.ThumbnailUrl;
                //    }
                //    else
                //    {
                //        string msg = imageAndThumbnailResult != null ? imageAndThumbnailResult.Message : "";
                //        OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail for WEB4 Geo-NFT: {msg}");
                //        return result;
                //    }
                //}
                //else
                //    Console.WriteLine("");

                //if (CLIEngine.GetConfirmation("Do you wish to edit the Price?"))
                //{
                //    Console.WriteLine("");
                //    request.Price = CLIEngine.GetValidInputForDecimal("Please enter the new Price: ");
                //}
                //else
                //    Console.WriteLine("");

                //if (CLIEngine.GetConfirmation("Do you wish to edit the Discount?"))
                //{
                //    Console.WriteLine("");
                //    request.Discount = CLIEngine.GetValidInputForDecimal("Please enter the new Discount: ");
                //}
                //else
                //    Console.WriteLine("");

                //if (CLIEngine.GetConfirmation("Do you wish to edit the Royalty Percentage?"))
                //    request.RoyaltyPercentage = CLIEngine.GetValidInputForInt("Please enter the Royalty Percentage (integer): ", false);
                //else
                //    Console.WriteLine("");

                //if (CLIEngine.GetConfirmation("Do you wish to change the sale status (Is For Sale)?"))
                //    request.IsForSale = CLIEngine.GetConfirmation("Is the NFT for sale? Press 'Y' for Yes or 'N' for No.");
                //else
                //    Console.WriteLine("");

                //if (request.IsForSale.HasValue && request.IsForSale.Value)
                //{
                //    string existingSaleStartDate = collectionResult.Result.SaleStartDate.HasValue ? collectionResult.Result.SaleStartDate.Value == DateTime.MinValue ? "None" : collectionResult.Result.SaleStartDate.Value.ToShortDateString() : "None";

                //    if (CLIEngine.GetConfirmation($"Do you wish to edit the Sale Start Date? (currently is: {existingSaleStartDate})", addLineBefore: true))
                //        request.SaleStartDate = CLIEngine.GetValidInputForDate("Please enter the Sale Start Date (YYYY-MM-DD) or 'none' to clear:", addLineBefore: true);
                //    else
                //        Console.WriteLine("");

                //    if (request.SaleStartDate.HasValue)
                //    {
                //        string existingSaleEndDate = collectionResult.Result.SaleEndDate.HasValue ? collectionResult.Result.SaleEndDate.Value == DateTime.MinValue ? "None" : collectionResult.Result.SaleEndDate.Value.ToShortDateString() : "None";

                //        if (CLIEngine.GetConfirmation($"Do you wish to edit Sale End Date? (currently is: {existingSaleEndDate})"))
                //        {
                //            do
                //            {
                //                request.SaleEndDate = CLIEngine.GetValidInputForDate("Please enter the Sale End Date (YYYY-MM-DD) or 'none' to clear:", addLineBefore: true);

                //                if (request.SaleEndDate.HasValue && request.SaleEndDate.Value <= request.SaleEndDate.Value)
                //                    CLIEngine.ShowWarningMessage("The end date must be after the start date!");
                //            }
                //            while (request.SaleEndDate.HasValue && request.SaleEndDate.Value <= request.SaleStartDate.Value);
                //        }
                //        else
                //            Console.WriteLine("");
                //    }
                //    else
                //        request.SaleEndDate = null;
                //}
            }
            else
            {
                string msg = nftResult != null ? nftResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 Geo-NFT to update: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> DeleteWeb4GeoNFTAsync(string idOrName, ProviderType providerType = ProviderType.Default)
        {
            return await DeleteWeb4GeoNFTAsync(idOrName, null, null, null, providerType);
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> DeleteWeb4GeoNFTAsync(string idOrName, bool? softDelete = true, bool? deleteChildWeb3NFTs = false, bool? burnChildWebNFTs = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> geoNFT = await FindWeb4GeoNFTAsync("delete", idOrName, true);

            if (geoNFT == null || geoNFT.Result == null || geoNFT.IsError)
            {
                OASISErrorHandling.HandleError(ref geoNFT, $"Error occured finding WEB4 Geo-NFT to delete. Reason: {geoNFT.Message}");
                return geoNFT;
            }

            if (!softDelete.HasValue)
                softDelete = CLIEngine.GetConfirmation("Do you wish to permanently delete the Web4 Geo-NFT? (defaults to false)");

            if (!deleteChildWeb3NFTs.HasValue)
                deleteChildWeb3NFTs = CLIEngine.GetConfirmation("Do you wish to also delete the child Web3 NFTs? (the OASIS holon/metadata)(recommeneded/default)");

            if (!burnChildWebNFTs.HasValue)
                burnChildWebNFTs = CLIEngine.GetConfirmation("Do you wish to also burn the child Web3 NFTs? (permanently destroy the Web3 NFTs on-chain) (recommeneded/default)");

            CLIEngine.ShowWorkingMessage("Deleting WEB4 OASIS Geo-NFT...");
            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteWeb4GeoNFTAsync(STAR.BeamedInAvatar.Id, geoNFT.Result.Id, softDelete.Value, deleteChildWeb3NFTs.Value, burnChildWebNFTs.Value, providerType: providerType);

            if (deleteResult != null && deleteResult.Result && !deleteResult.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 GeoNFT Successfully Deleted.");
                geoNFT = await NFTCommon.DeleteAllSTARNETVersionsAsync("Web5STARGeoNFTId", STARNETManager, geoNFT.Result.MetaData, geoNFT, providerType);
            }
            else
            {
                string msg = deleteResult != null ? deleteResult.Message : "";
                OASISErrorHandling.HandleError(ref geoNFT, $"Error occured deleting WEB4 GeoNFT. Reason: {msg}");
            }

            return geoNFT;
        }

        public virtual async Task<OASISResult<IWeb4GeoSpatialNFT>> ShowWeb4GeoNFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");

            result = await FindWeb4GeoNFTAsync("view", idOrName, true, providerType: providerType);

            //if (result != null && result.Result != null && !result.IsError)
            //    ShowGeoNFT(result.Result);
            //else
            //    OASISErrorHandling.HandleError(ref result, "No WEB4 Geo-NFT Found For That Id or Name!");

            return result;
        }

        public virtual async Task SearchWeb4GeoNFTAsync(string searchTerm = "", bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
                searchTerm = CLIEngine.GetValidInput($"What is the name of the WEB4 Geo-NFT you wish to search for?");

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching WEB4 Geo-NFT's...");
            ListWeb4GeoNFTs(await NFTCommon.NFTManager.SearchWeb4GeoNFTsAsync(searchTerm, STAR.BeamedInAvatar.Id, null, MetaKeyValuePairMatchMode.All, !showForAllAvatars, providerType));
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> FindWeb4GeoNFTAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, string UIName = "WEB4 GeoNFT", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllWeb4GeoNFTsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllWeb4GeoNFTsAsync(providerType);

                        ListWeb4GeoNFTs(starHolonsResult);

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
                    result = await NFTCommon.NFTManager.LoadWeb4GeoNftAsync(id, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.MintedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was minted by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> searchResults = await NFTCommon.NFTManager.SearchWeb4GeoNFTsAsync(idOrName, STAR.BeamedInAvatar.Id, null, MetaKeyValuePairMatchMode.All, showOnlyForCurrentAvatar, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListWeb4GeoNFTs(searchResults, true);

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
                    ShowGeoNFT(result.Result);

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


        private OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> ListWeb4GeoNFTs(OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> geoNFTs, bool showNumbers = false, bool showDetailedInfo = false)
        {
            if (geoNFTs != null)
            {
                if (!geoNFTs.IsError)
                {
                    if (geoNFTs.Result != null && geoNFTs.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (geoNFTs.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{geoNFTs.Result.Count()} WEB4 GeoNFT Found:");
                        else
                            CLIEngine.ShowMessage($"{geoNFTs.Result.Count()} WEB4 GeoNFT's Found:");

                        for (int i = 0; i < geoNFTs.Result.Count(); i++)
                            ShowGeoNFT(geoNFTs.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showDetailedInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No WEB4 GeoNFT's Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading WEB4 GeoNFT's. Reason: {geoNFTs.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading WEB4 GeoNFT's.");

            return geoNFTs;
        }

        private void ShowGeoNFT(IWeb4GeoSpatialNFT web4GeoNFT, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 39)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            NFTCommon.ShowNFTDetails(web4GeoNFT, null, displayFieldLength, false, false);

            //DisplayProperty("Geo-NFT Id", geoNFT.Id.ToString(), displayFieldLength);
            //DisplayProperty("NFT Id", geoNFT.OriginalWeb4OASISNFTId.ToString(), displayFieldLength);
            //DisplayProperty("Title", geoNFT.Title, displayFieldLength);
            //DisplayProperty("Description", geoNFT.Description, displayFieldLength);
            //DisplayProperty("Price", geoNFT.Price.ToString(), displayFieldLength);
            //DisplayProperty("Discount", geoNFT.Discount.ToString(), displayFieldLength);
            //DisplayProperty("Royalty Percentage", geoNFT.RoyaltyPercentage.ToString(), displayFieldLength);
            //DisplayProperty("For Sale", geoNFT.IsForSale ? string.Concat("Yes (StartDate: ", geoNFT.SaleStartDate.HasValue ? geoNFT.SaleStartDate.Value.ToShortDateString() : "Not Set", geoNFT.SaleEndDate.HasValue ? geoNFT.SaleEndDate.Value.ToShortDateString() : "Not Set") : "No", displayFieldLength);
            //DisplayProperty("Minted By Avatar Id", geoNFT.MintedByAvatarId.ToString(), displayFieldLength);
            //DisplayProperty("Minted On", geoNFT.MintedOn.ToString(), displayFieldLength);
            //DisplayProperty("OnChain Provider", geoNFT.OnChainProvider.Name, displayFieldLength);
            //DisplayProperty("OffChain Provider", geoNFT.OffChainProvider.Name, displayFieldLength);
            //DisplayProperty("Store NFT Meta Data OnChain", geoNFT.StoreNFTMetaDataOnChain.ToString(), displayFieldLength);
            //DisplayProperty("NFT OffChain Meta Type", geoNFT.NFTOffChainMetaType.Name, displayFieldLength);
            //DisplayProperty("NFT Standard Type", geoNFT.NFTStandardType.Name, displayFieldLength);
            //DisplayProperty("Symbol", geoNFT.Symbol, displayFieldLength);
            //DisplayProperty("Image", geoNFT.Image != null ? "Yes" : "None", displayFieldLength);
            //DisplayProperty("Image Url", geoNFT.ImageUrl, displayFieldLength);
            //DisplayProperty("Thumbnail", geoNFT.Thumbnail != null ? "Yes" : "None", displayFieldLength);
            //DisplayProperty("Thumbnail Url", !string.IsNullOrEmpty(geoNFT.ThumbnailUrl) ? geoNFT.ThumbnailUrl : "None", displayFieldLength);
            //DisplayProperty("JSON MetaData URL", geoNFT.JSONMetaDataURL, displayFieldLength);
            //DisplayProperty("JSON MetaData URL Holon Id", geoNFT.JSONMetaDataURLHolonId != Guid.Empty ? geoNFT.JSONMetaDataURLHolonId.ToString() : "None", displayFieldLength);
            //DisplayProperty("Seller Fee Basis Points", geoNFT.SellerFeeBasisPoints.ToString(), displayFieldLength);
            //DisplayProperty("Send To Address After Minting", geoNFT.SendToAddressAfterMinting, displayFieldLength);
            //DisplayProperty("Send To Avatar After Minting Id", geoNFT.SendToAvatarAfterMintingId != Guid.Empty ? geoNFT.SendToAvatarAfterMintingId.ToString() : "None", displayFieldLength);
            //DisplayProperty("Send To Avatar After Minting Username", !string.IsNullOrEmpty(geoNFT.SendToAvatarAfterMintingUsername) ? geoNFT.SendToAvatarAfterMintingUsername : "None", displayFieldLength);
            DisplayProperty("Lat/Long", $"{web4GeoNFT.Lat}/{web4GeoNFT.Long}", displayFieldLength);
            DisplayProperty("Perm Spawn", web4GeoNFT.PermSpawn.ToString(), displayFieldLength);

            if (!web4GeoNFT.PermSpawn)
            {
                DisplayProperty("Allow Other Players To Also Collect", web4GeoNFT.AllowOtherPlayersToAlsoCollect.ToString(), displayFieldLength);

                if (web4GeoNFT.AllowOtherPlayersToAlsoCollect)
                {
                    DisplayProperty("Global Spawn Quantity", web4GeoNFT.GlobalSpawnQuantity.ToString(), displayFieldLength);
                    DisplayProperty("Player Spawn Quantity", web4GeoNFT.PlayerSpawnQuantity.ToString(), displayFieldLength);
                    DisplayProperty("Respawn Duration In Seconds", web4GeoNFT.RespawnDurationInSeconds.ToString(), displayFieldLength);
                }
                else
                {
                    DisplayProperty("Global Spawn Quantity", "N/A", displayFieldLength);
                    DisplayProperty("Player Spawn Quantity", "N/A", displayFieldLength);
                    DisplayProperty("Respawn Duration In Seconds", "N/A", displayFieldLength);
                }
            }
            else
            {
                DisplayProperty("Allow Other Players To Also Collect", "N/A", displayFieldLength);
                DisplayProperty("Global Spawn Quantity", "N/A", displayFieldLength);
                DisplayProperty("Player Spawn Quantity", "N/A", displayFieldLength);
                DisplayProperty("Respawn Duration In Seconds", "N/A", displayFieldLength);
            }

            DisplayProperty("2D Sprite", web4GeoNFT.Nft2DSprite != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("2D Sprite URL", !string.IsNullOrEmpty(web4GeoNFT.Nft2DSpriteURI) ? web4GeoNFT.Nft2DSpriteURI : "None", displayFieldLength);
            DisplayProperty("3D Object", web4GeoNFT.Nft2DSprite != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("3D Object URL", !string.IsNullOrEmpty(web4GeoNFT.Nft3DObjectURI) ? web4GeoNFT.Nft3DObjectURI : "None", displayFieldLength);

            TagHelper.ShowTags(web4GeoNFT.Tags, displayFieldLength);
            MetaDataHelper.ShowMetaData(web4GeoNFT.MetaData, displayFieldLength);

            Console.WriteLine("");
            DisplayProperty($"WEB3 NFT's ({web4GeoNFT.Web3NFTs.Count})", "", displayFieldLength);

            foreach (Web3NFT web3NFT in web4GeoNFT.Web3NFTs)
            {
                NFTCommon.ShowNFTDetails(web3NFT, web4GeoNFT, displayFieldLength);
                DisplayProperty("Send NFT Transaction Hash", web3NFT.SendNFTTransactionHash, displayFieldLength);
                DisplayProperty("OASIS MintWallet Address", web3NFT.OASISMintWalletAddress, displayFieldLength);
                DisplayProperty("Mint Transaction Hash", web3NFT.MintTransactionHash, displayFieldLength);
                DisplayProperty("NFT Token Address", web3NFT.NFTTokenAddress, displayFieldLength);
                DisplayProperty("Update Authority", web3NFT.UpdateAuthority, displayFieldLength);
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        private async Task<IPlaceWeb4GeoSpatialNFTRequest> GenerateGeoNFTRequestAsync(bool isExistingNFT)
        {
            PlaceWeb4GeoSpatialNFTRequest request = new PlaceWeb4GeoSpatialNFTRequest();
            request.PlacedByAvatarId = STAR.BeamedInAvatar.Id;

            if (isExistingNFT)
            {
                request.OriginalWeb4OASISNFTId = CLIEngine.GetValidInputForGuid("What is the original WEB4 OASIS NFT ID?");
                request.OriginalWeb4OASISNFTOffChainProvider = new Utilities.EnumValue<ProviderType>((ProviderType)CLIEngine.GetValidInputForEnum("What provider did you choose to store the off-chain metadata for the original OASIS NFT? (if you cannot remember, then enter 'All' and the OASIS HyperDrive will attempt to find it through auto-replication).", typeof(ProviderType)));
            }

            request.GeoNFTMetaDataProvider = new Utilities.EnumValue<ProviderType>((ProviderType)CLIEngine.GetValidInputForEnum("What provider would you like to store the Geo-NFT metadata on? (NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive)", typeof(ProviderType)));
            request.Lat = CLIEngine.GetValidInputForLong("What is the lat geo-location you wish for your NFT to appear in Our World/AR World?");
            request.Long = CLIEngine.GetValidInputForLong("What is the long geo-location you wish for your NFT to appear in Our World/AR World?");

            OASISResult<ImageObjectResult> imageObjectResult = await ProcessImageOrObjectAsync("Geo-NFT");

            if (imageObjectResult != null && imageObjectResult.Result != null && !imageObjectResult.IsError)
            {
                request.Nft3DObject = imageObjectResult.Result.Object3D;
                request.Nft3DObjectURI = imageObjectResult.Result.Object3DURI != null ? imageObjectResult.Result.Object3DURI.AbsoluteUri : "";
                request.Nft2DSprite = imageObjectResult.Result.Image2D;
                request.Nft2DSpriteURI = imageObjectResult.Result.Image2DURI != null ? imageObjectResult.Result.Image2DURI.AbsoluteUri : "";
            }

            request.PermSpawn = CLIEngine.GetConfirmation("Will the NFT be permantly spawned allowing infinite number of players to collect as many times as they wish? If you select Y to this then the NFT will always be available with zero re-spawn time.");
            Console.WriteLine("");

            if (!request.PermSpawn)
            {
                request.AllowOtherPlayersToAlsoCollect = CLIEngine.GetConfirmation("Once the NFT has been collected by a given player/avatar, do you want it to also still be collectable by other players/avatars?");

                if (request.AllowOtherPlayersToAlsoCollect)
                {
                    Console.WriteLine("");
                    request.GlobalSpawnQuantity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected?");
                    request.RespawnDurationInSeconds = CLIEngine.GetValidInputForInt("How long will it take (in seconds) for the NFT to re-spawn once it has been collected?");
                    request.PlayerSpawnQuantity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected for a given player/avatar? (If you want to enforce that players/avatars can only collect each NFT once then set this to 0.)");
                }
                else
                    Console.WriteLine("");
            }

            return request;
        }

        private OASISResult<bool> UpdateWeb4AndWeb3GeoNFTJSONFiles(IWeb4GeoSpatialNFT NFT, string path)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                File.WriteAllText(Path.Combine(path, $"WEB4_GEONFT_{NFT.Id}.json"), JsonConvert.SerializeObject(NFT));

                if (!string.IsNullOrEmpty(NFT.JSONMetaData))
                    File.WriteAllText(Path.Combine(path, $"WEB4_JSONMetaData_{NFT.Id}.json"), NFT.JSONMetaData);

                foreach (IWeb3NFT web3Nft in NFT.Web3NFTs)
                {
                    File.WriteAllText(Path.Combine(path, $"WEB3_NFT_{web3Nft.Id}.json"), JsonConvert.SerializeObject(web3Nft));

                    if (!string.IsNullOrEmpty(web3Nft.JSONMetaData))
                        File.WriteAllText(Path.Combine(path, $"WEB3_JSONMetaData_{web3Nft.Id}.json"), web3Nft.JSONMetaData);
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 and WEB3 NFT JSON files. Reason: {e.Message}");
            }

            result.Result = true;
            return result;
        }
    }
}