using ADRaffy.ENSNormalize;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
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
            OASISResult<IWeb4OASISGeoSpatialNFT> geoNFTResult = null;
            bool mint = false;

            ShowHeader();

            if (CLIEngine.GetConfirmation("Do you have an existing WEB4 OASIS Geo-NFT you wish to create a WEB5 Geo-NFT from?"))
            {
                Console.WriteLine("");
                geoNFTResult = await FindWeb4GeoNFTAsync("wrap");

                //Guid geoNFTId = CLIEngine.GetValidInputForGuid("Please enter the ID of the WEB4 GeoNFT you wish to upload to STARNET: ");

                //if (geoNFTId != Guid.Empty)
                //    geoNFTResult = await STAR.OASISAPI.NFTs.LoadGeoNftAsync(geoNFTId);
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
                geoNFTResult = await MintGeoNFTAsync(); //Mint WEB4 GeoNFT (mints and wraps around a WEB4 OASIS NFT).
                mint = true;
            }

            if (geoNFTResult != null && geoNFTResult.Result != null && !geoNFTResult.IsError)
            {
                IWeb4OASISGeoSpatialNFT geoNFT = geoNFTResult.Result;

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS Geo-NFT to WEB5 STARNET which will create a WEB5 STAR GeoNFT that wraps around the WEB4 GeoNFT allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended). Selecting 'Y' will also create a WEB3 JSONMetaData and a WEB4 OASIS GeoNFT json file in the WEB5 STAR GeoNFT folder.")))
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
                            OASISErrorHandling.HandleError(ref result, $"Error occured saving WEB STAR Geo-NFT after creation in CreateAsync method. Reason: {saveResult.Message}");
                    }
                }
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

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> MintGeoNFTAsync(object mintParams = null)
        {
            IMintWeb4NFTTRequest request = await NFTCommon.GenerateNFTRequestAsync();
            IPlaceWeb4GeoSpatialNFTRequest geoRequest = await GenerateGeoNFTRequestAsync(false);

            CLIEngine.ShowWorkingMessage("Minting WEB4 OASIS Geo-NFT...");
            OASISResult<IWeb4OASISGeoSpatialNFT> nftResult = await STAR.OASISAPI.NFTs.MintAndPlaceGeoNFTAsync(new MintAndPlaceWeb4GeoSpatialNFTRequest()
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
            IPlaceWeb4GeoSpatialNFTRequest geoRequest = await GenerateGeoNFTRequestAsync(true);
            CLIEngine.ShowWorkingMessage("Creating WEB4 OASIS Geo-NFT...");
            OASISResult<IWeb4OASISGeoSpatialNFT> nftResult = await STAR.OASISAPI.NFTs.PlaceGeoNFTAsync(geoRequest);

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

            OASISResult<IWeb4NFTTransactionRespone> response = await STAR.OASISAPI.NFTs.SendNFTAsync(new Web4NFTWalletTransactionRequest()
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

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> BurnGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            return result;
        }

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ImportGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            return result;
        }

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ExportGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            return result;
        }

        //public async Task<OASISResult<IOASISGeoSpatialNFT>> CloneGeoNFTAsync(object mintParams = null)
        //{
        //    OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
        //    return result;
        //}

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ConvertGeoNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            return result;
        }

        //public virtual async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> ListAllWeb4GeoNFTsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> ListAllWeb4GeoNFTsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(await NFTCommon.NFTManager.LoadAllGeoNFTsAsync(providerType));
        }

        //public virtual OASISResult<IEnumerable<IOASISGeoSpatialNFT>> ListAllWeb4GeoNFTs(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> ListAllWeb4GeoNFTs(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(NFTCommon.NFTManager.LoadAllGeoNFTs(providerType));
        }

        //public virtual async Task<OASISResult<IEnumerable<IOASISGeoSpatialNFT>>> ListAllWeb4GeoNFTForAvatarsAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> ListAllWeb4GeoNFTForAvatarsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(await NFTCommon.NFTManager.LoadAllGeoNFTsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType));
        }

        //public virtual OASISResult<IEnumerable<IOASISGeoSpatialNFT>> ListAllWeb4GeoNFTsForAvatar(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        public virtual OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> ListAllWeb4GeoNFTsForAvatar(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");
            return ListWeb4GeoNFTs(NFTCommon.NFTManager.LoadAllGeoNFTsForAvatar(STAR.BeamedInAvatar.Id, providerType));
        }

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> UpdateWeb4GeoNFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            UpdateWeb4GeoNFTRequest request = new UpdateWeb4GeoNFTRequest();

            OASISResult<IWeb4OASISGeoSpatialNFT> collectionResult = await FindWeb4GeoNFTAsync("update", idOrName, providerType: providerType);

            if (collectionResult != null && collectionResult.Result != null && !collectionResult.IsError)
            {
                // Prefill request with existing values so unchanged fields are preserved
                var existing = collectionResult.Result;

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
                request.MetaData = existing.MetaData != null ? new Dictionary<string, object>(existing.MetaData) : new Dictionary<string, object>();

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

                if (CLIEngine.GetConfirmation("Do you wish to edit the Title?"))
                    request.Title = CLIEngine.GetValidInput("Please enter the new title for the WEB4 Geo-NFT: ");

                if (CLIEngine.GetConfirmation("Do you wish to edit the Description?"))
                    request.Description = CLIEngine.GetValidInput("Please enter the new description for the WEB4 Geo-NFT: ");

                request.ModifiedByAvatarId = STAR.BeamedInAvatar.Id;

                if (CLIEngine.GetConfirmation("Do you wish to update the Image and Thumbnail?"))
                {
                    OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("WEB4 Geo-NFT");

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
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail for WEB4 Geo-NFT: {msg}");
                        return result;
                    }
                }

                if (CLIEngine.GetConfirmation("Do you wish to edit the Price?"))
                    request.Price = CLIEngine.GetValidInputForDecimal("Please enter the new Price for the WEB4 Geo-NFT: ");

                if (CLIEngine.GetConfirmation("Do you wish to edit the Discount?"))
                    request.Discount = CLIEngine.GetValidInputForDecimal("Please enter the new Discount for the WEB4 Geo-NFT: ");

                if (CLIEngine.GetConfirmation("Do you wish to edit the Royalty Percentage?"))
                    request.RoyaltyPercentage = CLIEngine.GetValidInputForInt("Please enter the Royalty Percentage (integer): ", false);

                if (CLIEngine.GetConfirmation("Do you wish to change the sale status (Is For Sale)?"))
                    request.IsForSale = CLIEngine.GetConfirmation("Is the NFT for sale? Press 'Y' for Yes or 'N' for No.");

                if (CLIEngine.GetConfirmation("Do you wish to edit Sale Start Date?"))
                {
                    string input = CLIEngine.GetValidInput("Please enter the Sale Start Date (YYYY-MM-DD) or 'none' to clear:");
                    if (!string.IsNullOrEmpty(input) && input.ToLower() != "none" && DateTime.TryParse(input, out DateTime startDate))
                        request.SaleStartDate = startDate;
                    else
                        request.SaleStartDate = null;
                }

                if (CLIEngine.GetConfirmation("Do you wish to edit Sale End Date?"))
                {
                    string input = CLIEngine.GetValidInput("Please enter the Sale End Date (YYYY-MM-DD) or 'none' to clear:");
                    if (!string.IsNullOrEmpty(input) && input.ToLower() != "none" && DateTime.TryParse(input, out DateTime endDate))
                        request.SaleEndDate = endDate;
                    else
                        request.SaleEndDate = null;
                }

                // Geo specific edits
                if (CLIEngine.GetConfirmation("Do you wish to edit the Lat/Long location?"))
                {
                    request.Lat = CLIEngine.GetValidInputForLong("Please enter the new Lat location:");
                    request.Long = CLIEngine.GetValidInputForLong("Please enter the new Long location:");
                }

                if (CLIEngine.GetConfirmation("Do you wish to edit spawn settings (PermSpawn / AllowOtherPlayersToAlsoCollect)?"))
                {
                    request.PermSpawn = CLIEngine.GetConfirmation("Will the NFT be permanently spawned? Press 'Y' for Yes or 'N' for No.");

                    if (!request.PermSpawn.Value)
                    {
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

                if (CLIEngine.GetConfirmation("Do you wish to update the 2D sprite or 3D object assets?"))
                {
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

                // Manage metadata (add/edit/delete)
                request.MetaData = MetaDataHelper.ManageMetaData(collectionResult.Result.MetaData, "WEB4 Geo-NFT");

                CLIEngine.ShowWorkingMessage("Saving WEB4 Geo-NFT...");
                result = await NFTCommon.NFTManager.UpdateOASISGeoNFTAsync(request, providerType);

                if (result != null && result.Result != null && !result.IsError)
                    CLIEngine.ShowSuccessMessage("WEB4 OASIS Geo-NFT Successfully Updated.");
                else
                {
                    string msg = result != null ? result.Message : "";
                    CLIEngine.ShowErrorMessage($"Error Occured Updating WEB4 Geo-NFT: {msg}");
                }
            }
            else
            {
                string msg = collectionResult != null ? collectionResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 Geo-NFT to update: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> DeleteWeb4GeoNFTAsync(string collectionIdOrName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> geoNFT = await FindWeb4GeoNFTAsync("delete", collectionIdOrName, true);

            if (geoNFT == null || geoNFT.Result == null || geoNFT.IsError)
            {
                OASISErrorHandling.HandleError(ref geoNFT, $"Error occured finding WEB4 Geo-NFT to delete. Reason: {geoNFT.Message}");
                return geoNFT;
            }

            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteOASISGeoNFTAsync(STAR.BeamedInAvatar.Id, geoNFT.Result.Id, softDelete, providerType: providerType);

            if (deleteResult != null && deleteResult.Result && !deleteResult.IsError)
                CLIEngine.ShowSuccessMessage("WEB4 OASIS Geo-NFT Successfully Deleted.");
            else
            {
                string msg = deleteResult != null ? deleteResult.Message : "";
                OASISErrorHandling.HandleError(ref geoNFT, $"Error occured deleting WEB4 Geo-NFT. Reason: {msg}");
            }

            return geoNFT;
        }

        public virtual async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ShowWeb4GeoNFTAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 Geo-NFT's...");

            result = await FindWeb4GeoNFTAsync("view", idOrName, true, providerType: providerType);

            if (result != null && result.Result != null && !result.IsError)
                ShowGeoNFT(result.Result);
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
                IWeb4OASISGeoSpatialNFT geoNFT = starHolon.STARNETDNA.MetaData["GeoNFT"] as IWeb4OASISGeoSpatialNFT;

                if (geoNFT == null)
                    geoNFT = JsonConvert.DeserializeObject<Web4OASISGeoSpatialNFT>(starHolon.STARNETDNA.MetaData["GeoNFT"].ToString());

                if (geoNFT != null)
                {
                    Console.WriteLine("");
                    DisplayProperty("WEB4 GEO-NFT DETAILS", "", displayFieldLength, false);
                    ShowGeoNFT(geoNFT, showHeader: false, showFooter: false);
                }
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

        public async Task<OASISResult<IWeb4OASISGeoSpatialNFT>> FindWeb4GeoNFTAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "WEB4 GeoNFT", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoSpatialNFT> result = new OASISResult<IWeb4OASISGeoSpatialNFT>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> starHolonsResult = null;

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
                    OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> searchResults = await NFTCommon.NFTManager.SearchGeoNFTsAsync(idOrName, STAR.BeamedInAvatar.Id, showOnlyForCurrentAvatar, providerType: providerType);

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


        private OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> ListWeb4GeoNFTs(OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> geoNFTs, bool showNumbers = false, bool showDetailedInfo = false)
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

        private void ShowGeoNFT(IWeb4OASISGeoSpatialNFT geoNFT, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 39)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            //DisplayProperty("GEO-NFT DETAILS", "", displayFieldLength, false);
            //Console.WriteLine("");
            DisplayProperty("Geo-NFT Id", geoNFT.Id.ToString(), displayFieldLength);
            DisplayProperty("NFT Id", geoNFT.OriginalWeb4OASISNFTId.ToString(), displayFieldLength);
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

            MetaDataHelper.ShowMetaData(geoNFT.MetaData);

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
    }
}