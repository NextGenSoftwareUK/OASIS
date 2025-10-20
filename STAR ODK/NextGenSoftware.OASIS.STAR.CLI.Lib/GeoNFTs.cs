using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

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

        public override async Task<OASISResult<STARGeoNFT>> CreateAsync(ISTARNETCreateOptions<STARGeoNFT, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARGeoNFT> result = new OASISResult<STARGeoNFT>();
            OASISResult<IOASISGeoSpatialNFT> geoNFTResult = null;
            bool mint = false;

            ShowHeader();

            if (CLIEngine.GetConfirmation("Do you have an existing WEB4 OASIS Geo-NFT you wish to create a WEB5 Geo-NFT from?"))
            {
                Console.WriteLine("");
                Guid geoNFTId = CLIEngine.GetValidInputForGuid("Please enter the ID of the WEB4 GeoNFT you wish to upload to STARNET: ");

                if (geoNFTId != Guid.Empty)
                    geoNFTResult = await STAR.OASISAPI.NFTs.LoadGeoNftAsync(geoNFTId);
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
                geoNFTResult = await MintGeoNFTAsync(); //Mint WEB4 GeoNFT (mints and wraps around a WEB4 OASIS NFT).
                mint = true;
            }

            if (geoNFTResult != null && geoNFTResult.Result != null && !geoNFTResult.IsError)
            {
                IOASISGeoSpatialNFT geoNFT = geoNFTResult.Result;

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS Geo-NFT to WEB5 STARNET which will create a WEB5 STAR GeoNFT that wraps around the WEB4 GeoNFT allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended). Selecting 'Y' will also create a WEB3 JSONMetaData and a WEB4 OASIS GeoNFT json file in the WEB5 STAR GeoNFT folder. Currently if you select 'N' then it will not show up for the 'geonft list' or 'geonft show' sub-command's since these only support WEB5 GeoNFTs. Future support may be added to list/show WEB4 GeoNFT's and NFT's.")))
                {
                    Console.WriteLine("");

                    result = await base.CreateAsync(new STARNETCreateOptions<STARGeoNFT, STARNETDNA>()
                    {
                        STARNETDNA = new STARNETDNA()
                        {
                            MetaData = new Dictionary<string, object>() { { "GeoNFT", geoNFT } }
                        },
                        STARNETHolon = new STARGeoNFT() 
                        { 
                            GeoNFTId = geoNFTResult.Result.Id 
                        }
                    }, holonSubType, showHeaderAndInro, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"OASISGeoNFT_{geoNFTResult.Result.Id}.json"), JsonConvert.SerializeObject(geoNFT));

                        if (!string.IsNullOrEmpty(geoNFTResult.Result.JSONMetaData))
                            File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"JSONMetaData_{geoNFTResult.Result.Id}.json"), geoNFTResult.Result.JSONMetaData);

                        result.Result.NFTType = (NFTType)Enum.Parse(typeof(NFTType), result.Result.STARNETDNA.STARNETCategory.ToString());
                        OASISResult<STARGeoNFT> saveResult = await result.Result.SaveAsync<STARGeoNFT>();

                        if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                            OASISErrorHandling.HandleError(ref result, $"Error occured saving STARGeoNFT after creation in CreateAsync method. Reason: {saveResult.Message}");
                    }
                }
            }
            else
            {
                if (mint)
                    OASISErrorHandling.HandleError(ref result, $"Error occured minting GeoNFT in MintGeoNFTAsync method. Reason: {geoNFTResult.Message}");
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading GeoNFT in LoadGeoNftAsync method. Reason: {geoNFTResult.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> MintGeoNFTAsync(object mintParams = null)
        {
            IMintNFTTransactionRequest request = await NFTCommon.GenerateNFTRequestAsync();
            IPlaceGeoSpatialNFTRequest geoRequest = await GenerateGeoNFTRequestAsync(false);

            CLIEngine.ShowWorkingMessage("Minting OASIS Geo-NFT...");
            OASISResult<IOASISGeoSpatialNFT> nftResult = await STAR.OASISAPI.NFTs.MintAndPlaceGeoNFTAsync(new MintAndPlaceGeoSpatialNFTRequest()
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

        public async Task PlaceGeoNFTAsync()
        {
            IPlaceGeoSpatialNFTRequest geoRequest = await GenerateGeoNFTRequestAsync(true);
            CLIEngine.ShowWorkingMessage("Creating OASIS Geo-NFT...");
            OASISResult<IOASISGeoSpatialNFT> nftResult = await STAR.OASISAPI.NFTs.PlaceGeoNFTAsync(geoRequest);

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

            CLIEngine.ShowWorkingMessage("Sending GeoNFT...");

            OASISResult<INFTTransactionRespone> response = await STAR.OASISAPI.NFTs.SendNFTAsync(new NFTWalletTransactionRequest()
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

        public async Task<OASISResult<IOASISGeoSpatialNFT>> BurnGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            return result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> ImportGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            return result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> ExportGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            return result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> CloneGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            return result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> ConvertGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> ListAllWeb4GeoNFTsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(await NFTCommon.NFTManager.LoadAllGeoNFTsAsync(providerType));
        }

        public virtual OASISResult<IEnumerable<IOASISGeoSpatialNFT>> ListAllWeb4GeoNFTs(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(NFTCommon.NFTManager.LoadAllGeoNFTs(providerType));
        }

        public virtual async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> ListAllWeb4GeoNFTForAvatarsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(await NFTCommon.NFTManager.LoadAllGeoNFTsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType));
        }

        public virtual OASISResult<IEnumerable<IOASISGeoSpatialNFT>> ListAllWeb4GeoNFTsForAvatar(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(NFTCommon.NFTManager.LoadAllGeoNFTsForAvatar(STAR.BeamedInAvatar.Id, providerType));
        }

        public virtual async Task<OASISResult<IOASISGeoSpatialNFT>> ShowWeb4GeoNFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");

            result = await FindWeb4GeoNFTAsync("view", idOrName, true, providerType: providerType);

            if (result != null && result.Result != null && !result.IsError)
                ShowGeoNFT(result.Result, DEFAULT_FIELD_LENGTH);
            else
                OASISErrorHandling.HandleError(ref result, "No WEB4 Geo-NFT Found For That Id or Name!");

            return result;
        }


        public override void Show<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH, object customData = null)
        {
            displayFieldLength = DEFAULT_FIELD_LENGTH;
            base.Show(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("GeoNFT") && starHolon.STARNETDNA.MetaData["GeoNFT"] != null)
            {
                IOASISGeoSpatialNFT geoNFT = starHolon.STARNETDNA.MetaData["GeoNFT"] as IOASISGeoSpatialNFT;

                if (geoNFT == null)
                    geoNFT = JsonConvert.DeserializeObject<OASISGeoSpatialNFT>(starHolon.STARNETDNA.MetaData["GeoNFT"].ToString());

                if (geoNFT != null)
                    ShowGeoNFT(geoNFT, displayFieldLength);
            }

            CLIEngine.ShowDivider();
        }

        public virtual async Task SearchWeb4GeoNFTAsync(string searchTerm = "", bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
                searchTerm = CLIEngine.GetValidInput($"What is the name of the WEB4 Geo-NFT you wish to search for?");

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching WEB4 Geo-NFT's...");
            ListWeb4GeoNFTs(await NFTCommon.NFTManager.SearchGeoNFTsAsync(searchTerm, STAR.BeamedInAvatar.Id, !showForAllAvatars, providerType: providerType));
        }

        public async Task<OASISResult<IOASISGeoNFTCollection>> CreateGeoNFTCollectionAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new OASISResult<IOASISGeoNFTCollection>();
            CreateOASISGeoNFTCollectionRequest request = new CreateOASISGeoNFTCollectionRequest();

            request.Title = CLIEngine.GetValidInput("Please enter a title for the GeoNFT Collection: ");
            request.Description = CLIEngine.GetValidInput("Please enter a description for the GeoNFT Collection: ");
            request.CreatedBy = STAR.BeamedInAvatar.Id;

            OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("GeoNFT Collection");

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
                OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail for GeoNFT Collection: {msg}");
                return result;
            }

            request.MetaData = request.MetaData = NFTCommon.AddMetaData("GeoNFT Collection");

            if (CLIEngine.GetConfirmation("Do you wish to add any GeoNFT's to this collection now? (You can always add more later)."))
            {
                request.OASISGeoNFTs = new List<IOASISGeoSpatialNFT>();
                OASISResult<IOASISGeoSpatialNFT> nftResult = null;

                do
                {
                    if (CLIEngine.GetConfirmation("Does the GeoNFT already exist? (If you select 'N' you will be taken through the minting process to create a new GeoNFT to add to the collection)."))
                        nftResult = await FindWeb4GeoNFTAsync("use", providerType: providerType);
                    else
                        nftResult = await MintGeoNFTAsync();

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                        request.OASISGeoNFTs.Add(nftResult.Result);
                    else
                    {
                        string msg = nftResult != null ? nftResult.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Finding GeoNFT to add to Collection: {msg}");
                        return result;
                    }

                } while (CLIEngine.GetConfirmation("Do you wish to add another GeoNFT to this collection?"));
            }

            result = await NFTCommon.NFTManager.CreateOASISGeoNFTCollectionAsyc(request);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("OASIS GeoNFT Collection Successfully Created.");
            else
            {
                string msg = result != null ? result.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured Creating GeoNFT Collection: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IOASISGeoNFTCollection>> UpdateGeoNFTCollectionAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new OASISResult<IOASISGeoNFTCollection>();
            UpdateOASISGeoNFTCollectionRequest request = new UpdateOASISGeoNFTCollectionRequest();

            OASISResult<IOASISGeoNFTCollection> collectionResult = await FindWeb4GeoNFTCollectionAsync("update", providerType: providerType);

            if (collectionResult != null && collectionResult.Result != null && !collectionResult.IsError)
            {
                if (CLIEngine.GetConfirmation("Do you wish to edit the Title?"))
                    request.Title = CLIEngine.GetValidInput("Please enter the new title for the GeoNFT Collection: ");

                if (CLIEngine.GetConfirmation("Do you wish to edit the Description?"))
                    request.Description = CLIEngine.GetValidInput("Please enter the new description for the GeoNFT Collection: ");

                request.ModifiedBy = STAR.BeamedInAvatar.Id;

                if (CLIEngine.GetConfirmation("Do you wish to update the Image and Thumbnail?"))
                {
                    OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("GeoNFT Collection");

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
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail for GeoNFT Collection: {msg}");
                        return result;
                    }
                }


                request.MetaData = request.MetaData = NFTCommon.AddMetaData("GeoNFT Collection");

                if (CLIEngine.GetConfirmation("Do you wish to add more GeoNFT's to this collection now? (You can always add more later)."))
                {
                    request.OASISGeoNFTs = new List<IOASISGeoSpatialNFT>();
                    OASISResult<IOASISGeoSpatialNFT> nftResult = null;

                    do
                    {
                        if (CLIEngine.GetConfirmation("Does the GeoNFT already exist? (If you select 'N' you will be taken through the minting process to create a new GeoNFT to add to the collection)."))
                            nftResult = await FindWeb4GeoNFTAsync("use", providerType: providerType);
                        else
                            nftResult = await MintGeoNFTAsync();

                        if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                            request.OASISGeoNFTs.Add(nftResult.Result);
                        else
                        {
                            string msg = nftResult != null ? nftResult.Message : "";
                            OASISErrorHandling.HandleError(ref result, $"Error Occured Finding GeoNFT to add to Collection: {msg}");
                            return result;
                        }

                    } while (CLIEngine.GetConfirmation("Do you wish to add another GeoNFT to this collection?"));
                }

                result = await NFTCommon.NFTManager.UpdateOASISGeoNFTCollectionAsync(request);

                if (result != null && result.Result != null && !result.IsError)
                    CLIEngine.ShowSuccessMessage("OASIS GeoNFT Collection Successfully Updated.");
                else
                {
                    string msg = result != null ? result.Message : "";
                    CLIEngine.ShowErrorMessage($"Error Occured Updating GeoNFT Collection: {msg}");
                }
            }
            else
            {
                string msg = collectionResult != null ? collectionResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding GeoNFT Collection to update: {msg}");
            }

            return result;
        }

        private async Task<OASISResult<IOASISGeoSpatialNFT>> FindWeb4GeoNFTAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "WEB4 GeoNFT", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IOASISGeoSpatialNFT>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllGeoNFTsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllGeoNFTsAsync(providerType);

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
                    result = await NFTCommon.NFTManager.LoadGeoNftAsync(id, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.MintedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was minted by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IOASISGeoSpatialNFT>> searchResults = await NFTCommon.NFTManager.SearchGeoNFTsAsync(idOrName, STAR.BeamedInAvatar.Id, showOnlyForCurrentAvatar, providerType: providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListWeb4GeoNFTs(searchResults);

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
                    ShowGeoNFT(result.Result, DEFAULT_FIELD_LENGTH);

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

        private async Task<OASISResult<IOASISGeoNFTCollection>> FindWeb4GeoNFTCollectionAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "WEB4 GeoNFT Collection", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new OASISResult<IOASISGeoNFTCollection>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IOASISGeoNFTCollection>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadGeoNFTCollectionsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllGeoNFTCollectionsAsync(providerType);

                        ListWeb4GeoNFTCollections(starHolonsResult);

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
                    result = await NFTCommon.NFTManager.LoadGeoNftAsync(id, providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.MintedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was minted by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IOASISGeoSpatialNFT>> searchResults = await NFTCommon.NFTManager.SearchGeoNFTsAsync(idOrName, STAR.BeamedInAvatar.Id, showOnlyForCurrentAvatar, providerType: providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListWeb4GeoNFTs(searchResults);

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
                    ShowGeoNFT(result.Result, DEFAULT_FIELD_LENGTH);

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

        private OASISResult<IEnumerable<IOASISGeoSpatialNFT>> ListWeb4GeoNFTs(OASISResult<IEnumerable<IOASISGeoSpatialNFT>> geoNFTs)
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

                        foreach (IOASISGeoSpatialNFT geoNFT in geoNFTs.Result)
                            ShowGeoNFT(geoNFT, DEFAULT_FIELD_LENGTH);
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

        private void ShowGeoNFT(IOASISGeoSpatialNFT geoNFT, int displayFieldLength)
        {
            Console.WriteLine("");
            DisplayProperty("GEO-NFT DETAILS", "", displayFieldLength, false);
            Console.WriteLine("");
            DisplayProperty("Geo-NFT Id", geoNFT.Id.ToString(), displayFieldLength);
            DisplayProperty("NFT Id", geoNFT.OriginalOASISNFTId.ToString(), displayFieldLength);
            DisplayProperty("Title", geoNFT.Title, displayFieldLength);
            DisplayProperty("Description", geoNFT.Description, displayFieldLength);
            DisplayProperty("Price", geoNFT.Price.ToString(), displayFieldLength);
            DisplayProperty("Discount", geoNFT.Discount.ToString(), displayFieldLength);
            DisplayProperty("OASIS MintWallet Address", geoNFT.OASISMintWalletAddress, displayFieldLength);
            DisplayProperty("Mint Transaction Hash", geoNFT.MintTransactionHash, displayFieldLength);
            DisplayProperty("NFT Token Address", geoNFT.NFTTokenAddress, displayFieldLength);
            DisplayProperty("Minted By Avatar Id", geoNFT.MintedByAvatarId.ToString(), displayFieldLength);
            DisplayProperty("Minted On", geoNFT.MintedOn.ToString(), displayFieldLength);
            DisplayProperty("OnChain Provider", geoNFT.OnChainProvider.Name, displayFieldLength);
            DisplayProperty("OffChain Provider", geoNFT.OffChainProvider.Name, displayFieldLength);
            DisplayProperty("Store NFT Meta Data OnChain", geoNFT.StoreNFTMetaDataOnChain.ToString(), displayFieldLength);
            DisplayProperty("NFT OffChain Meta Type", geoNFT.NFTOffChainMetaType.Name, displayFieldLength);
            DisplayProperty("NFT Standard Type", geoNFT.NFTStandardType.Name, displayFieldLength);
            DisplayProperty("Symbol", geoNFT.Symbol, displayFieldLength);
            DisplayProperty("Image", geoNFT.Image != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("Image Url", geoNFT.ImageUrl, displayFieldLength);
            DisplayProperty("Thumbnail", geoNFT.Thumbnail != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("Thumbnail Url", !string.IsNullOrEmpty(geoNFT.ThumbnailUrl) ? geoNFT.ThumbnailUrl : "None", displayFieldLength);
            DisplayProperty("JSON MetaData URL", geoNFT.JSONMetaDataURL, displayFieldLength);
            DisplayProperty("JSON MetaData URL Holon Id", geoNFT.JSONMetaDataURLHolonId != Guid.Empty ? geoNFT.JSONMetaDataURLHolonId.ToString() : "None", displayFieldLength);
            DisplayProperty("Seller Fee Basis Points", geoNFT.SellerFeeBasisPoints.ToString(), displayFieldLength);
            DisplayProperty("Update Authority", geoNFT.UpdateAuthority, displayFieldLength);
            DisplayProperty("Send To Address After Minting", geoNFT.SendToAddressAfterMinting, displayFieldLength);
            DisplayProperty("Send To Avatar After Minting Id", geoNFT.SendToAvatarAfterMintingId != Guid.Empty ? geoNFT.SendToAvatarAfterMintingId.ToString() : "None", displayFieldLength);
            DisplayProperty("Send To Avatar After Minting Username", !string.IsNullOrEmpty(geoNFT.SendToAvatarAfterMintingUsername) ? geoNFT.SendToAvatarAfterMintingUsername : "None", displayFieldLength);
            DisplayProperty("Send NFT Transaction Hash", geoNFT.SendNFTTransactionHash, displayFieldLength);
            DisplayProperty("Lat/Long", $"{geoNFT.Lat}/{geoNFT.Long}", displayFieldLength);
            DisplayProperty("Perm Spawn", geoNFT.PermSpawn.ToString(), displayFieldLength);

            if (!geoNFT.PermSpawn)
            {
                DisplayProperty("Allow Other Players To Also Collect", geoNFT.AllowOtherPlayersToAlsoCollect.ToString(), displayFieldLength);

                if (geoNFT.AllowOtherPlayersToAlsoCollect)
                {
                    DisplayProperty("Global Spawn Quantity", geoNFT.GlobalSpawnQuantity.ToString(), displayFieldLength);
                    DisplayProperty("Player Spawn Quantity", geoNFT.PlayerSpawnQuantity.ToString(), displayFieldLength);
                    DisplayProperty("Respawn Duration In Seconds", geoNFT.RespawnDurationInSeconds.ToString(), displayFieldLength);
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

            DisplayProperty("2D Sprite", geoNFT.Nft2DSprite != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("2D Sprite URL", !string.IsNullOrEmpty(geoNFT.Nft2DSpriteURI) ? geoNFT.Nft2DSpriteURI : "None", displayFieldLength);
            DisplayProperty("3D Object", geoNFT.Nft2DSprite != null ? "Yes" : "None", displayFieldLength);
            DisplayProperty("3D Object URL", !string.IsNullOrEmpty(geoNFT.Nft3DObjectURI) ? geoNFT.Nft3DObjectURI : "None", displayFieldLength);

            if (geoNFT.MetaData != null)
            {
                CLIEngine.ShowMessage($"MetaData:");

                foreach (string key in geoNFT.MetaData.Keys)
                    CLIEngine.ShowMessage($"          {key} = {geoNFT.MetaData[key]}", false);
            }
            else
                CLIEngine.ShowMessage($"MetaData: None");
        }

        private async Task<IPlaceGeoSpatialNFTRequest> GenerateGeoNFTRequestAsync(bool isExistingNFT)
        {
            PlaceGeoSpatialNFTRequest request = new PlaceGeoSpatialNFTRequest();
            request.PlacedByAvatarId = STAR.BeamedInAvatar.Id;

            if (isExistingNFT)
            {
                request.OriginalOASISNFTId = CLIEngine.GetValidInputForGuid("What is the original WEB4 OASIS NFT ID?");
                request.OriginalOASISNFTOffChainProvider = new Utilities.EnumValue<ProviderType>((ProviderType)CLIEngine.GetValidInputForEnum("What provider did you choose to store the off-chain metadata for the original OASIS NFT? (if you cannot remember, then enter 'All' and the OASIS HyperDrive will attempt to find it through auto-replication).", typeof(ProviderType)));
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
    }
}