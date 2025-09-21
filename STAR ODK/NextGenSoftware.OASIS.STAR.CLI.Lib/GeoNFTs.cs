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

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class GeoNFTs : STARNETUIBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, GeoNFTDNA>
    public class GeoNFTs : STARNETUIBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, STARNETDNA>
    {
        public NFTCommon NFTCommon { get; set; } = new NFTCommon();

        public GeoNFTs(Guid avatarId, STARDNA STARDNA) : base(new STARGeoNFTManager(avatarId, STARDNA),
            "Welcome to the WEB5 STAR GeoNFT Wizard", new List<string> 
            {
                "This wizard will allow you create a WEB5 STAR GeoNFT which wraps around a WEB4 OASIS GeoNFT, which in turn wraps around a WEB4 OASIS NFT.",
                "You can mint a WEB4 OASIS NFT using the 'nft mint' sub-command.",
                "You can mint a WEB4 GeoNFT using the 'geonft mint' sub-command. This will automatically create the WEB4 OASIS NFT to wrap around or it can wrap around an existing WEB4 OASIS NFT.",
                "You then convert or wrap around the WEB4 OASIS GeoNFT using the sub-command 'geonft create'.",
                "A WEB5 GeoNFT can then be published to STARNET in much the same way as everything else within STAR using the same sub-commands such as publish, download, install etc.",
                "Both WEB4 and WEB5 STAR GeoNFT's can be placed in any location within Our World as part of Quest's. The main difference is WEB5 STAR GeoNFT's can be published to STARNET, version controlled, shared, etc whereas WEB4 GeoNFT's cannot.",
                "The wizard will create an empty folder with a GeoNFTDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the GeoNFT into this folder.",
                "Finally you run the sub-command 'geonft publish' to convert the folder containing the GeoNFT (can contain any number of files and sub-folders) into a OASIS GeoNFT file (.ogeonft) as well as optionally upload to STARNET.",
                "You can then share the .ogeonft file with others across any platform or OS, who can then install the GeoNFT from the file using the sub-command 'geonft install'.",
                "You can also optionally choose to upload the .ogeonft file to the STARNET store so others can search, download and install the GeoNFT."
            },
            STAR.STARDNA.DefaultGeoNFTsSourcePath, "DefaultGeoNFTsSourcePath",
            STAR.STARDNA.DefaultGeoNFTsPublishedPath, "DefaultGeoNFTsPublishedPath",
            STAR.STARDNA.DefaultGeoNFTsDownloadedPath, "DefaultGeoNFTsDownloadedPath",
            STAR.STARDNA.DefaultGeoNFTsInstalledPath, "DefaultGeoNFTsInstalledPath")
        { }

        //public override async Task CreateAsync(object createParams, STARGeoNFT newHolon = null, ProviderType providerType = ProviderType.Default)
        //{
        //    Guid geoNFTId = CLIEngine.GetValidInputForGuid("Please enter the ID of the GeoNFT you wish to upload to STARNET: ");
        //    OASISResult<IOASISGeoSpatialNFT> geoNFTResult = await NFTManager.LoadGeoNftAsync(geoNFTId);

        //    if (geoNFTResult != null && !geoNFTResult.IsError && geoNFTResult.Result != null)
        //        await base.CreateAsync(createParams, new STARGeoNFT() { GeoNFTId = geoNFTId }, providerType);
        //    else
        //        CLIEngine.ShowErrorMessage("No GeoNFT Found For That Id!");
        //}

        public override async Task<OASISResult<STARGeoNFT>> CreateAsync(object createParams, STARGeoNFT newHolon = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, Dictionary<string, object> metaData = null, STARNETDNA STARNETDNA = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARGeoNFT> result = new OASISResult<STARGeoNFT>();

            //Guid geoNFTId = CLIEngine.GetValidInputForGuid("Please enter the ID of the GeoNFT you wish to upload to STARNET: ");
            //OASISResult<IOASISGeoSpatialNFT> geoNFTResult = await NFTManager.LoadGeoNftAsync(geoNFTId);

            OASISResult<IOASISGeoSpatialNFT> mintResult = await MintGeoNFTAsync(); //Mint WEB4 GeoNFT (mints and wraps around a WEB4 OASIS NFT).

            if (mintResult != null && mintResult.Result != null && !mintResult.IsError)
            {
                result = await base.CreateAsync(createParams, new STARGeoNFT()
                {
                    GeoNFTId = mintResult.Result.Id
                }, showHeaderAndInro, checkIfSourcePathExists, metaData: mintResult.Result.MetaData, providerType: providerType);

                //result = await base.CreateAsync(createParams, new STARGeoNFT()
                //{
                //    GeoNFTId = mintResult.Result.Id
                //}, showHeaderAndInro, checkIfSourcePathExists, metaData: new Dictionary<string, object>() 
                //{
                //    { "OASISGeoNFTId", mintResult.Result.Id },
                //    { "OriginalOASISNFTId", mintResult.Result.OriginalOASISNFTId },
                //    { "OnChainProviderType", mintResult.Result.OnChainProvider.Name },
                //    { "OffChainProviderType", mintResult.Result.OffChainProvider.Name },
                //    { "MintAddress", mintResult.Result.MintAddress },
                //    { "MintAddress", mintResult.Result.MintedByAddress },
                //    { "MintedByAvatarId", mintResult.Result.MintedByAvatarId },
                //    { "MintedOn", mintResult.Result.MintedOn },
                //    { "Hash", mintResult.Result.Hash },
                //    { "Title", mintResult.Result.Title },
                //    { "Description", mintResult.Result.Description },
                //    { "MemoText", mintResult.Result.MemoText },
                //    { "Price", mintResult.Result.Price },
                //    { "Discount", mintResult.Result.Discount },
                //    { "ImageURL", mintResult.Result.ImageUrl },
                //    { "ThumbnailUrl", mintResult.Result.ThumbnailUrl },
                //    { "JSONMetaDataURL", mintResult.Result.JSONMetaDataURL },

                //    { "MintAddress", mintResult.Result.MintAddress },

                //}, providerType: providerType);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured minting GeoNFT in MintGeoNFTAsync method. Reason: {mintResult.Message}");

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

        public override void Show<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 35, object customData = null)
        {
            base.Show(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            Console.WriteLine("");
            DisplayProperty("GEO-NFT DETAILS", "", displayFieldLength, false);
            DisplayProperty("Geo-NFT Id", ParseMetaData(starHolon.MetaData, "GEONFT.Id"), displayFieldLength);
            DisplayProperty("NFT Id", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.Id"), displayFieldLength);
            DisplayProperty("Title", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.Title"), displayFieldLength);
            DisplayProperty("Description", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.Description"), displayFieldLength);
            DisplayProperty("Price", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.Price"), displayFieldLength);
            DisplayProperty("Discount", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.Discount"), displayFieldLength);
            DisplayProperty("OASIS MintWallet Address", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.OASISMintWalletAddress"), displayFieldLength);
            DisplayProperty("Mint Transaction Hash", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.MintTransactionHash"), displayFieldLength);
            DisplayProperty("NFT Token Address", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.NFTTokenAddress"), displayFieldLength);
            DisplayProperty("Minted By Avatar Id", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.MintedByAvatarId"), displayFieldLength);
            DisplayProperty("Minted On", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.MintedOn"), displayFieldLength);
            DisplayProperty("OnChain Provider", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.OnChainProvider"), displayFieldLength);
            DisplayProperty("OffChain Provider", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.OffChainProvider"), displayFieldLength);
            DisplayProperty("Store NFT Meta Data OnChain", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.StoreNFTMetaDataOnChain"), displayFieldLength);
            DisplayProperty("NFT OffChain Meta Type", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.NFTOffChainMetaType"), displayFieldLength);
            DisplayProperty("NFT Standard Type", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.NFTStandardType"), displayFieldLength);
            DisplayProperty("Symbol", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.Symbol"), displayFieldLength);
            DisplayProperty("Image", ParseMetaDataForByteArray(starHolon.MetaData, "GEONFT.OriginalOASISNFT.Image"), displayFieldLength);
            DisplayProperty("Image Url", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.ImageUrl"), displayFieldLength);
            DisplayProperty("Thumbnail", ParseMetaDataForByteArray(starHolon.MetaData, "GEONFT.OriginalOASISNFT.Thumbnail"), displayFieldLength);
            DisplayProperty("Thumbnail Url", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.ThumbnailUrl"), displayFieldLength);
            DisplayProperty("JSON MetaData URL", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.JSONMetaDataURL"), displayFieldLength);
            DisplayProperty("JSON MetaData URL Holon Id", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.JSONMetaDataURLHolonId"), displayFieldLength);            
            DisplayProperty("Seller Fee Basis Points", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.SellerFeeBasisPoints"), displayFieldLength);
            DisplayProperty("Update Authority", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.UpdateAuthority"), displayFieldLength);
            DisplayProperty("Send To Address After Minting", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.SendToAddressAfterMinting"), displayFieldLength);
            DisplayProperty("Send To Avatar After Minting Id", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingId"), displayFieldLength);
            DisplayProperty("Send To Avatar After Minting Username", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingUsername"), displayFieldLength);
            DisplayProperty("Send NFT Transaction Hash", ParseMetaData(starHolon.MetaData, "GEONFT.OriginalOASISNFT.SendNFTTransactionHash"), displayFieldLength);
            DisplayProperty("Lat/Long", ParseMetaData(starHolon.MetaData, "GEONFT.LatLong"), displayFieldLength);
            DisplayProperty("Perm Spawn", ParseMetaData(starHolon.MetaData, "GEONFT.PermSpawn"), displayFieldLength);            
            DisplayProperty("Allow Other Players To Also Collect", ParseMetaData(starHolon.MetaData, "GEONFT.AllowOtherPlayersToAlsoCollect"), displayFieldLength);
            DisplayProperty("Player Spawn Quantity", ParseMetaData(starHolon.MetaData, "GEONFT.PlayerSpawnQuantity"), displayFieldLength);
            DisplayProperty("Global Spawn Quantity", ParseMetaData(starHolon.MetaData, "GEONFT.GlobalSpawnQuantity"), displayFieldLength);
            DisplayProperty("Global Spawn Quantity", ParseMetaData(starHolon.MetaData, "GEONFT.GlobalSpawnQuantity"), displayFieldLength);
            DisplayProperty("Respawn Duration In Seconds", ParseMetaData(starHolon.MetaData, "GEONFT.RespawnDurationInSeconds"), displayFieldLength);
            DisplayProperty("2D Sprite", ParseMetaDataForByteArray(starHolon.MetaData, "GEONFT.Nft2DSprite"), displayFieldLength);
            DisplayProperty("2D Sprite URI", ParseMetaData(starHolon.MetaData, "GEONFT.Nft2DSpriteURI"), displayFieldLength);
            DisplayProperty("3D Object", ParseMetaDataForByteArray(starHolon.MetaData, "GEONFT.Nft3DObject"), displayFieldLength);
            DisplayProperty("3D Object URI", ParseMetaData(starHolon.MetaData, "GEONFT.Nft3DObjectURI"), displayFieldLength);

            if (starHolon.MetaData.Count > 0)
            {
                CLIEngine.ShowMessage($"MetaData:");

                foreach (string key in starHolon.MetaData.Keys)
                    CLIEngine.ShowMessage($"          {key} = {starHolon.MetaData[key]}");
            }
            else
                CLIEngine.ShowMessage($"MetaData: None");

            CLIEngine.ShowDivider();
        }

        private async Task<IPlaceGeoSpatialNFTRequest> GenerateGeoNFTRequestAsync(bool isExistingNFT)
        {
            PlaceGeoSpatialNFTRequest request = new PlaceGeoSpatialNFTRequest();
            request.PlacedByAvatarId = STAR.BeamedInAvatar.Id;

            if (isExistingNFT)
            {
                request.OriginalOASISNFTId = CLIEngine.GetValidInputForGuid("What is the original WEB4 OASIS NFT ID?");
                request.GeoNFTMetaDataProvider = new Utilities.EnumValue<ProviderType>((ProviderType)CLIEngine.GetValidInputForEnum("What provider would you like to store the Geo-NFT metadata on? (NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive)", typeof(ProviderType)));
                request.OriginalOASISNFTOffChainProvider = (ProviderType)CLIEngine.GetValidInputForEnum("What provider did you choose to store the off-chain metadata for the original OASIS NFT? (if you cannot remember, then enter 'All' and the OASIS HyperDrive will attempt to find it through auto-replication).", typeof(ProviderType));
            }

            long nftLat = CLIEngine.GetValidInputForLong("What is the lat geo-location you wish for your NFT to appear in Our World/AR World?");
            long nftLong = CLIEngine.GetValidInputForLong("What is the long geo-location you wish for your NFT to appear in Our World/AR World?");

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
            }

            return request;
        }
    }
}