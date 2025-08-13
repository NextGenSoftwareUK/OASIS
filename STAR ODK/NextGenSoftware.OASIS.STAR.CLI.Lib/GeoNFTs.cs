using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class GeoNFTs : STARNETUIBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, GeoNFTDNA>
    public class GeoNFTs : STARNETUIBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, STARNETDNA>
    {
        public NFTCommon NFTCommon { get; set; } = new NFTCommon();

        public GeoNFTs(Guid avatarId) : base(new STARGeoNFTManager(avatarId),
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

        public override async Task<OASISResult<STARGeoNFT>> CreateAsync(object createParams, STARGeoNFT newHolon = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARGeoNFT> result = new OASISResult<STARGeoNFT>();

            //Guid geoNFTId = CLIEngine.GetValidInputForGuid("Please enter the ID of the GeoNFT you wish to upload to STARNET: ");
            //OASISResult<IOASISGeoSpatialNFT> geoNFTResult = await NFTManager.LoadGeoNftAsync(geoNFTId);

            OASISResult<IOASISGeoSpatialNFT> mintResult = await MintGeoNFTAsync(); //Mint WEB4 GeoNFT (mints and wraps around a WEB4 OASIS NFT).

            if (mintResult != null && mintResult.Result != null && !mintResult.IsError)
                result = await base.CreateAsync(createParams, new STARGeoNFT() { GeoNFTId = mintResult.Result.Id }, showHeaderAndInro, checkIfSourcePathExists,  providerType);
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
                MintWalletAddress = request.MintWalletAddress,
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
                Nft3DSpriteURI = geoRequest.Nft3DSpriteURI,
                Nft3DObject = geoRequest.Nft3DObject,
                Nft3DObjectURI = geoRequest.Nft3DObjectURI
            });

            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                CLIEngine.ShowSuccessMessage($"OASIS Geo-NFT Successfully Minted. {nftResult.Message} Id: {nftResult.Result.Id}, Hash: {nftResult.Result.Hash} Minted On: {nftResult.Result.MintedOn}, Minted By Avatar Id: {nftResult.Result.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.MintedByAddress}.");
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
                CLIEngine.ShowSuccessMessage($"OASIS Geo-NFT Successfully Created. {nftResult.Message} OriginalOASISNFTId: {nftResult.Result.OriginalOASISNFTId}, Id: {nftResult.Result.Id}, Hash: {nftResult.Result.Hash} Minted On: {nftResult.Result.MintedOn}, Minted By Avatar Id: {nftResult.Result.MintedByAvatarId}, Minted Wallet Address: {nftResult.Result.MintedByAddress}.");
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
            string memoText = CLIEngine.GetValidInput("What is the memo text?");
            //decimal amount = CLIEngine.GetValidInputForDecimal("What is the amount?");

            CLIEngine.ShowWorkingMessage("Sending GeoNFT...");

            OASISResult<INFTTransactionRespone> response = await STAR.OASISAPI.NFTs.SendNFTAsync(new NFTWalletTransactionRequest()
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                //MintWalletAddress = mintWalletAddress,
                MemoText = memoText,
                //Amount = amount,
            });

            if (response != null && response.Result != null && !response.IsError)
                CLIEngine.ShowSuccessMessage($"GeoNFT Successfully Sent. {response.Message} Hash: {response.Result.TransactionResult}");
            else
            {
                string msg = response != null ? response.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
            }
        }

        public async Task ShowGeoNFTAsync(Guid id = new Guid(), ProviderType providerType = ProviderType.Default)
        {
            if (id == Guid.Empty)
                id = CLIEngine.GetValidInputForGuid("What is the GUID/ID to the NFT you wish to view?");

            CLIEngine.ShowWorkingMessage("Loading Geo-NFT...");
            OASISResult<IOASISGeoSpatialNFT> nft = await STAR.OASISAPI.NFTs.LoadGeoNftAsync(id);

            if (nft != null && !nft.IsError && nft.Result != null)
            {
                CLIEngine.ShowDivider();
                ShowGeoNFT(nft.Result);
            }
            else
                CLIEngine.ShowErrorMessage("No Geo-NFT Found.");
        }

        public void ShowGeoNFT(IOASISGeoSpatialNFT nft)
        {
            string image = nft.Image != null ? "Yes" : "No";
            string thumbnail = nft.Thumbnail != null ? "Yes" : "No";

            CLIEngine.ShowMessage(string.Concat($"Title: ", !string.IsNullOrEmpty(nft.Title) ? nft.Title : "None"));
            CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(nft.Description) ? nft.Description : "None"));
            CLIEngine.ShowMessage($"Price: {nft.Price}");
            CLIEngine.ShowMessage($"Discount: {nft.Discount}");
            CLIEngine.ShowMessage(string.Concat($"MemoText: ", !string.IsNullOrEmpty(nft.MemoText) ? nft.MemoText : "None"));
            CLIEngine.ShowMessage($"Id: {nft.Id}");
            CLIEngine.ShowMessage(string.Concat($"Hash: ", !string.IsNullOrEmpty(nft.Hash) ? nft.Hash : "None"));
            CLIEngine.ShowMessage($"MintedByAvatarId: {nft.MintedByAvatarId}");
            CLIEngine.ShowMessage(string.Concat($"MintedByAddress: ", !string.IsNullOrEmpty(nft.MintedByAddress) ? nft.MintedByAddress : "None"));
            CLIEngine.ShowMessage($"MintedOn: {nft.MintedOn}");
            CLIEngine.ShowMessage($"OnChainProvider: {nft.OnChainProvider.Name}");
            CLIEngine.ShowMessage($"OffChainProvider: {nft.OffChainProvider.Name}");
            CLIEngine.ShowMessage(string.Concat($"URL: ", !string.IsNullOrEmpty(nft.URL) ? nft.URL : "None"));
            CLIEngine.ShowMessage(string.Concat($"ImageUrl: ", !string.IsNullOrEmpty(nft.ImageUrl) ? nft.ImageUrl : "None"));
            CLIEngine.ShowMessage(string.Concat("Image: ", nft.Image != null ? "Yes" : "No"));
            CLIEngine.ShowMessage(string.Concat($"ThumbnailUrl: ", !string.IsNullOrEmpty(nft.ThumbnailUrl) ? nft.ThumbnailUrl : "None"));
            CLIEngine.ShowMessage(string.Concat("Thumbnail: ", nft.Thumbnail != null ? "Yes" : "No"));
            CLIEngine.ShowMessage($"Lat: {nft.Lat}");
            CLIEngine.ShowMessage($"Long: {nft.Long}");
            CLIEngine.ShowMessage($"PlacedByAvatarId: {nft.PlacedByAvatarId}");
            CLIEngine.ShowMessage($"PlacedOn: {nft.PlacedOn}");
            CLIEngine.ShowMessage($"GeoNFTMetaDataOffChainProvider: {nft.GeoNFTMetaDataOffChainProvider.Name}");
            CLIEngine.ShowMessage($"PermSpawn: {nft.PermSpawn}");
            CLIEngine.ShowMessage($"AllowOtherPlayersToAlsoCollect: {nft.AllowOtherPlayersToAlsoCollect}");
            CLIEngine.ShowMessage($"GlobalSpawnQuantity: {nft.GlobalSpawnQuantity}");
            CLIEngine.ShowMessage($"PlayerSpawnQuantity: {nft.PlayerSpawnQuantity}");
            CLIEngine.ShowMessage($"RepawnDurationInSeconds: {nft.RepawnDurationInSeconds}");

            if (nft.MetaData.Count > 0)
            {
                CLIEngine.ShowMessage($"MetaData:");

                foreach (string key in nft.MetaData.Keys)
                    CLIEngine.ShowMessage($"          {key} = {nft.MetaData[key]}");
            }
            else
                CLIEngine.ShowMessage($"MetaData: None");

            CLIEngine.ShowDivider();
        }

        private async Task<IPlaceGeoSpatialNFTRequest> GenerateGeoNFTRequestAsync(bool isExistingNFT)
        {
            Guid originalOASISNFTId = Guid.Empty;
            ProviderType providerType = ProviderType.None;
            ProviderType originalOffChainProviderType = ProviderType.All;
            string nft3dObjectPath = "";
            string nft2dSpritePath = "";
            byte[] nft3dObject = null;
            byte[] nft2dSprite = null;
            Uri nft3dObjectURI = null;
            Uri nft2dSpriteURI = null;
            int globalSpawnQuanity = 0;
            int respawnDurationInSeconds = 0;
            int playerSpawnQuanity = 0;
            bool allowOtherPlayersToAlsoCollect = false;

            if (isExistingNFT)
            {
                originalOASISNFTId = CLIEngine.GetValidInputForGuid("What is the original WEB4 OASIS NFT ID?");
                providerType = (ProviderType)CLIEngine.GetValidInputForEnum("What provider would you like to store the Geo-NFT metadata on? (NOTE: It will automatically auto-replicate to other providers across the OASIS through the auto-replication feature in the OASIS HyperDrive)", typeof(ProviderType));
                originalOffChainProviderType = (ProviderType)CLIEngine.GetValidInputForEnum("What provider did you choose to store the off-chain metadata for the original OASIS NFT? (if you cannot remember, then enter 'All' and the OASIS HyperDrive will attempt to find it through auto-replication).", typeof(ProviderType));
            }

            long nftLat = CLIEngine.GetValidInputForLong("What is the lat geo-location you wish for your NFT to appear in Our World/AR World?");
            long nftLong = CLIEngine.GetValidInputForLong("What is the long geo-location you wish for your NFT to appear in Our World/AR World?");

            if (CLIEngine.GetConfirmation("Would you rather use a 3D object or a 2D sprite/image to represent your NFT within Our World/AR World? Press Y for 3D or N for 2D."))
            {
                Console.WriteLine("");

                if (CLIEngine.GetConfirmation("Would you like to upload a local 3D object from your device or input a URI to an online object? (Press Y for local or N for online)"))
                {
                    Console.WriteLine("");
                    nft3dObjectPath = CLIEngine.GetValidFile("What is the full path to the local 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");
                    nft3dObject = File.ReadAllBytes(nft3dObjectPath);
                }
                else
                {
                    Console.WriteLine("");
                    nft3dObjectURI = await CLIEngine.GetValidURIAsync("What is the URI to the 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");
                }
            }
            else
            {
                Console.WriteLine("");

                if (CLIEngine.GetConfirmation("Would you like to upload a local 2D sprite/image from your device or input a URI to an online sprite/image? (Press Y for local or N for online)"))
                {
                    Console.WriteLine("");
                    nft2dSpritePath = CLIEngine.GetValidFile("What is the full path to the local 2d sprite/image? (Press Enter if you wish to skip and use the NFT Image instead. You can always change this later.)");
                    nft2dSprite = File.ReadAllBytes(nft2dSpritePath);
                }
                else
                {
                    Console.WriteLine("");
                    nft2dSpriteURI = await CLIEngine.GetValidURIAsync("What is the URI to the 2D sprite/image? (Press Enter if you wish to skip and use the NFT Image instead. You can always change this later.)");
                }
            }

            bool permSpawn = CLIEngine.GetConfirmation("Will the NFT be permantly spawned allowing infinite number of players to collect as many times as they wish? If you select Y to this then the NFT will always be available with zero re-spawn time.");
            Console.WriteLine("");

            if (!permSpawn)
            {
                allowOtherPlayersToAlsoCollect = CLIEngine.GetConfirmation("Once the NFT has been collected by a given player/avatar, do you want it to also still be collectable by other players/avatars?");

                if (allowOtherPlayersToAlsoCollect)
                {
                    Console.WriteLine("");
                    globalSpawnQuanity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected?");
                    respawnDurationInSeconds = CLIEngine.GetValidInputForInt("How long will it take (in seconds) for the NFT to re-spawn once it has been collected?");
                    playerSpawnQuanity = CLIEngine.GetValidInputForInt("How many times can the NFT re-spawn once it has been collected for a given player/avatar? (If you want to enforce that players/avatars can only collect each NFT once then set this to 0.)");
                }
            }

            return new PlaceGeoSpatialNFTRequest()
            {
                AllowOtherPlayersToAlsoCollect = allowOtherPlayersToAlsoCollect,
                PermSpawn = permSpawn,
                GlobalSpawnQuantity = globalSpawnQuanity,
                PlayerSpawnQuantity = playerSpawnQuanity,
                RespawnDurationInSeconds = respawnDurationInSeconds,
                Lat = nftLat,
                Long = nftLong,
                Nft2DSprite = nft2dSprite,
                Nft3DSpriteURI = nft2dSpriteURI != null ? nft2dSpriteURI.AbsoluteUri : "",
                Nft3DObject = nft3dObject,
                Nft3DObjectURI = nft3dObjectURI != null ? nft3dObjectURI.AbsoluteUri : "",
                OriginalOASISNFTId = originalOASISNFTId,
                ProviderType = providerType,
                OriginalOASISNFTOffChainProviderType = originalOffChainProviderType,
                PlacedByAvatarId = STAR.BeamedInAvatar.Id
            };
        }

        //private void ListGeoNFTs(OASISResult<IEnumerable<IOASISGeoSpatialNFT>> geonftsResult)
        //{
        //    if (geonftsResult != null)
        //    {
        //        if (!geonftsResult.IsError)
        //        {
        //            if (geonftsResult.Result != null && geonftsResult.Result.Count() > 0)
        //            {
        //                Console.WriteLine();

        //                if (geonftsResult.Result.Count() == 1)
        //                    CLIEngine.ShowMessage($"{geonftsResult.Result.Count()} Geo-NFT Found:");
        //                else
        //                    CLIEngine.ShowMessage($"{geonftsResult.Result.Count()} Geo-NFT's' Found:");

        //                CLIEngine.ShowDivider();

        //                foreach (IOASISGeoSpatialNFT geoNFT in geonftsResult.Result)
        //                    ShowGeoNFT(geoNFT);
        //            }
        //            else
        //                CLIEngine.ShowWarningMessage("No Geo-NFT's Found.");
        //        }
        //        else
        //            CLIEngine.ShowErrorMessage($"Error occured loading Geo-NFT's. Reason: {geonftsResult.Message}");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Unknown error occured loading Geo-NFT's.");
        //}

    }
}