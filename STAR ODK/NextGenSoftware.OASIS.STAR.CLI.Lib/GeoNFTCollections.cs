using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class GeoNFTCollections : STARNETUIBase<STARGeoNFTCollection, DownloadedGeoNFTCollection, InstalledGeoNFTCollection, STARNETDNA>
    {
        protected new const int DEFAULT_FIELD_LENGTH = 40;

        public NFTCommon NFTCommon { get; set; } = new NFTCommon();

        public GeoNFTCollections(Guid avatarId, STARDNA STARDNA) : base(new STARGeoNFTCollectionManager(avatarId, STARDNA),
            "Welcome to the WEB5 STAR GeoNFT Collection Wizard", new List<string> 
            {
                "This wizard will allow you create a WEB5 STAR GeoNFT Collection which wraps around a WEB4 OASIS GeoNFT Collection",
                "You can mint a WEB4 GeoNFT using the 'geonft mint' sub-command. This will automatically create the WEB4 OASIS NFT to wrap around or it can wrap around an existing WEB4 OASIS NFT.",
                "You can then convert or wrap around the WEB4 OASIS GeoNFT using the sub-command 'geonft create'.",
                "A WEB5 GeoNFT can then be published to STARNET in much the same way as everything else within STAR using the same sub-commands such as publish, download, install etc.",
                "Both WEB4 and WEB5 STAR GeoNFT's can be placed in any location within Our World as part of Quest's. The main difference is WEB5 STAR GeoNFT's can be published to STARNET, version controlled, shared, used in Our World (support for Web4 GeoNFT's may be added later), Quests etc whereas WEB4 GeoNFT's cannot.",
                "The wizard will create an empty folder with a GeoNFTDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the GeoNFT into this folder.",
                "Finally you run the sub-command 'geonft publish' to convert the folder containing the GeoNFT (can contain any number of files and sub-folders) into a OASIS GeoNFT file (.ogeonft) as well as optionally upload to STARNET.",
                "You can then share the .ogeonft file with others across any platform or OS, who can then install the GeoNFT from the file using the sub-command 'geonft install'.",
                "You can also optionally choose to upload the .ogeonft file to the STARNET store so others can search, download and install the GeoNFT."
            },
            STAR.STARDNA.DefaultGeoNFTCollectionsSourcePath, "DefaultGeoNFTCollectionsSourcePath",
            STAR.STARDNA.DefaultGeoNFTCollectionsPublishedPath, "DefaultGeoNFTCollectionsPublishedPath",
            STAR.STARDNA.DefaultGeoNFTCollectionsDownloadedPath, "DefaultGeoNFTCollectionsDownloadedPath",
            STAR.STARDNA.DefaultGeoNFTCollectionsInstalledPath, "DefaultGeoNFTCollectionsInstalledPath", DEFAULT_FIELD_LENGTH)
        { }

        public override async Task<OASISResult<STARGeoNFTCollection>> CreateAsync(ISTARNETCreateOptions<STARGeoNFTCollection, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARGeoNFTCollection> result = new OASISResult<STARGeoNFTCollection>();
            OASISResult<IWeb4OASISGeoNFTCollection> geoNFTCollectionResult = null;
            bool mint = false;

            ShowHeader();

            if (CLIEngine.GetConfirmation("Do you have an existing WEB4 OASIS Geo-NFT Collection you wish to create a WEB5 Geo-NFT Collection from?"))
            {
                Console.WriteLine("");
                geoNFTCollectionResult = await FindWeb4GeoNFTCollectionAsync("wrap");

                //Guid id = CLIEngine.GetValidInputForGuid("Please enter the ID of the WEB4 GeoNFT Collection you wish to upload to STARNET: ");

                //if (id != Guid.Empty)
                //    geoNFTCollectionResult = await STAR.OASISAPI.NFTs.LoadOASISGeoNFTCollectionAsync(id);
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
                geoNFTCollectionResult = await CreateWeb4GeoNFTCollectionAsync(providerType);
                mint = true;
            }

            if (geoNFTCollectionResult != null && geoNFTCollectionResult.Result != null && !geoNFTCollectionResult.IsError)
            {
                IWeb4OASISGeoNFTCollection geoNFTCollection = geoNFTCollectionResult.Result;
                geoNFTCollection.Web4OASISGeoNFTs.Clear();

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS Geo-NFT Collection to WEB5 STARNET which will create a WEB5 STAR GeoNFT Collection that wraps around the WEB4 GeoNFT Collection allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended).")))
                {
                    Console.WriteLine("");

                    result = await base.CreateAsync(new STARNETCreateOptions<STARGeoNFTCollection, STARNETDNA>()
                    {
                        STARNETDNA = new STARNETDNA()
                        {
                            MetaData = new Dictionary<string, object>() { { "GeoNFTCollection", geoNFTCollection } }
                            //MetaData = new Dictionary<string, object>() { { "GeoNFTCollectionId", geoNFTCollection.Id } }
                        },
                        STARNETHolon = new STARGeoNFTCollection()
                        {
                            GeoNFTCollectionId = geoNFTCollection.Id
                        }
                    }, holonSubType, showHeaderAndInro, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        result.Result.GeoNFTCollectionType = (NFTCollectionType)Enum.Parse(typeof(NFTCollectionType), result.Result.STARNETDNA.STARNETCategory.ToString());
                        OASISResult<STARGeoNFTCollection> saveResult = await result.Result.SaveAsync<STARGeoNFTCollection>();

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            geoNFTCollection.MetaData["Web5STARGeoNFTCollectionId"] = saveResult.Result.Id;
                            OASISResult<IWeb4OASISGeoNFTCollection> web4GeoNFTCollection = await NFTCommon.NFTManager.UpdateGeoNFTCollectionAsync(new UpdateWeb4GeoNFTCollectionRequest() { Id = geoNFTCollection.Id, ModifiedBy = STAR.BeamedInAvatar.Id, MetaData = geoNFTCollection.MetaData }, providerType: providerType);

                            if (!(web4GeoNFTCollection != null && web4GeoNFTCollection.Result != null && !web4GeoNFTCollection.IsError))
                                OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 Geo-NFT Collection after creation of WEB5 STAR Geo-NFT Collection in CreateAsync method. Reason: {web4GeoNFTCollection.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured saving WEB5 STAR Geo-NFT Collection after creation in CreateAsync method. Reason: {saveResult.Message}");
                    }
                }
            }
            else
            {
                if (mint)
                    OASISErrorHandling.HandleError(ref result, $"Error occured creating WEB4 GeoNFT Collection in CreateWeb4GeoNFTCollectionAsync method. Reason: {geoNFTCollectionResult.Message}");
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading WEB4 GeoNFT Collection in LoadOASISGeoNFTCollectionAsync method. Reason: {geoNFTCollectionResult.Message}");
            }

            return result;
        }

        public override async Task ShowAsync<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 35, object customData = null)
        {
            displayFieldLength = DEFAULT_FIELD_LENGTH;
            await base.ShowAsync(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            //if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("GeoNFTCollectionId") && starHolon.STARNETDNA.MetaData["GeoNFTCollectionId"] != null)
            //{
            //    Guid id = Guid.Empty;

            //    if (Guid.TryParse(starHolon.STARNETDNA.MetaData["GeoNFTCollectionId"].ToString(), out id))
            //    {
            //        OASISResult<IWeb4OASISGeoNFTCollection> web4GeoNFTCollection = await NFTCommon.NFTManager.LoadGeoNFTCollectionAsync(id);

            //        if (web4GeoNFTCollection != null && web4GeoNFTCollection.Result != null && !web4GeoNFTCollection.IsError)
            //        {
            //            Console.WriteLine("");
            //            DisplayProperty("WEB4 GEO-NFT COLLECTION DETAILS", "", displayFieldLength, false);
            //            await ShowGeoNFTCollectionAsync(web4GeoNFTCollection.Result, showHeader: false, showFooter: false);
            //        }
            //    }
            //}

            if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("GeoNFTCollection") && starHolon.STARNETDNA.MetaData["GeoNFTCollection"] != null)
            {
                IWeb4OASISGeoNFTCollection collection = starHolon.STARNETDNA.MetaData["GeoNFTCollection"] as IWeb4OASISGeoNFTCollection;

                if (collection == null)
                    collection = JsonConvert.DeserializeObject<Web4OASISGeoNFTCollection>(starHolon.STARNETDNA.MetaData["GeoNFTCollection"].ToString());

                if (collection != null)
                {
                    Console.WriteLine("");
                    DisplayProperty("WEB4 GEO-NFT COLLECTION DETAILS", "", displayFieldLength, false);
                    ShowGeoNFTCollectionAsync(collection, showHeader: false, showFooter: false);
                }
            }

            CLIEngine.ShowDivider();
        }

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> CreateWeb4GeoNFTCollectionAsync(object createOptions = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new OASISResult<IWeb4OASISGeoNFTCollection>();
            CreateWeb4GeoNFTCollectionRequest request = new CreateWeb4GeoNFTCollectionRequest();

            request.Title = CLIEngine.GetValidInput("Please enter a title for the WEB4 GeoNFT Collection: ");
            request.Description = CLIEngine.GetValidInput("Please enter a description for the WEB4 GeoNFT Collection: ");
            request.CreatedBy = STAR.BeamedInAvatar.Id;

            OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("WEB4 GeoNFT Collection");

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

            //TODO: Allow tags to be added/removed/edited rather than replaced.
            if (CLIEngine.GetConfirmation("Do you wish to edit the Tags?"))
            {
                List<string> tags = new List<string>();
                string tag = "";
                Console.WriteLine("");
                Console.WriteLine("Enter each tag followed by enter. When you are finished enter 'done' and press enter.");
                while (tag.ToLower() != "done")
                {
                    tag = CLIEngine.GetValidInput("Enter Tag: ");
                    if (tag.ToLower() != "done")
                        tags.Add(tag);
                }
                request.Tags = tags;
            }
            else
                Console.WriteLine("");

            request.MetaData = MetaDataHelper.ManageMetaData(request.MetaData, "WEB4 GeoNFT Collection");

            Console.WriteLine("");
            if (CLIEngine.GetConfirmation("Do you wish to add any WEB4 GeoNFT's to this collection now? (You can always add more later)."))
            {
                request.Web4OASISGeoNFTs = new List<IWeb4OASISGeoSpatialNFT>();
                OASISResult<IWeb4OASISGeoSpatialNFT> nftResult = null;

                do
                {
                    Console.WriteLine("");

                    if (CLIEngine.GetConfirmation("Does the WEB4 GeoNFT already exist? (If you select 'N' you will be taken through the minting process to create a new WEB4 GeoNFT to add to the collection)."))
                    {
                        Console.WriteLine("");
                        nftResult = await STARCLI.GeoNFTs.FindWeb4GeoNFTAsync("use", providerType: providerType);
                    }
                    else
                    {
                        Console.WriteLine("");
                        nftResult = await STARCLI.GeoNFTs.MintGeoNFTAsync();
                    }

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                        request.Web4OASISGeoNFTs.Add(nftResult.Result);
                    else
                    {
                        string msg = nftResult != null ? nftResult.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 Geo-NFT to add to Collection: {msg}");
                        return result;
                    }

                    CLIEngine.ShowSuccessMessage("WEB4 Geo-NFT Successfully Added To The Collection.");
                    ShowGeoNFTCollectionNFTs(request.Web4OASISGeoNFTs);

                } while (CLIEngine.GetConfirmation("Do you wish to add another WEB4 GeoNFT to this collection?"));
            }

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Creating WEB4 GeoNFT Collection...");
            result = await NFTCommon.NFTManager.CreateGeoNFTCollectionAsyc(request, providerType);

            if (result != null && result.Result != null && !result.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 OASIS GeoNFT Collection Successfully Created.");
                await ShowGeoNFTCollectionAsync(result.Result);
            }
            else
            {
                string msg = result != null ? result.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured Creating WEB4 GeoNFT Collection: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> UpdateWeb4GeoNFTCollectionAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new OASISResult<IWeb4OASISGeoNFTCollection>();
            UpdateWeb4GeoNFTCollectionRequest request = new UpdateWeb4GeoNFTCollectionRequest();

            OASISResult<IWeb4OASISGeoNFTCollection> collectionResult = await FindWeb4GeoNFTCollectionAsync("update", idOrName, providerType: providerType);

            if (collectionResult != null && collectionResult.Result != null && !collectionResult.IsError)
            {
                OASISResult<IUpdateWeb4NFTCollectionRequestBase> updateResult = NFTCommon.UpdateWeb4NFTCollection(request, collectionResult.Result, "WEB4 GeoNFT Collection");

                if (updateResult != null && updateResult.Result != null && !updateResult.IsError)
                {
                    request = (UpdateWeb4GeoNFTCollectionRequest)updateResult.Result;

                    //request.Id = collectionResult.Result.Id;
                    //request.ModifiedBy = STAR.BeamedInAvatar.Id;

                    //if (CLIEngine.GetConfirmation($"Do you wish to edit the Title? (currently is: {collectionResult.Result.Name})"))
                    //    request.Title = CLIEngine.GetValidInput("Please enter the new title: ");
                    //else
                    //    Console.WriteLine("");

                    //if (CLIEngine.GetConfirmation("Do you wish to edit the Description?"))
                    //    request.Description = CLIEngine.GetValidInput("Please enter the new description: ");
                    //else
                    //    Console.WriteLine("");

                    //if (CLIEngine.GetConfirmation("Do you wish to update the Image and Thumbnail?"))
                    //{
                    //    OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("WEB4 GeoNFT Collection");

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
                    //        OASISErrorHandling.HandleError(ref result, $"Error Occured Processing Image and Thumbnail for GeoNFT Collection: {msg}");
                    //        return result;
                    //    }
                    //}
                    //else
                    //    Console.WriteLine("");


                    //request.Tags = TagHelper.ManageTags(collectionResult.Result.Tags);
                    //request.MetaData = MetaDataHelper.ManageMetaData(collectionResult.Result.MetaData, "WEB4 GeoNFT Collection");

                    CLIEngine.ShowWorkingMessage("Updating WEB4 GeoNFT Collection...");
                    result = await NFTCommon.NFTManager.UpdateGeoNFTCollectionAsync(request, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        CLIEngine.ShowSuccessMessage("WEB4 OASIS GeoNFT Collection Successfully Updated.");
                        result = await NFTCommon.UpdateSTARNETHolonAsync("Web5STARGeoNFTCollectionId", "GeoNFTCollection", STARNETManager, result.Result.MetaData, result, providerType);
                    }
                    else
                    {
                        string msg = result != null ? result.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Updating WEB4 GeoNFT Collection in UpdateWeb4GeoNFTCollectionAsync method. Reason: {msg}");
                    }
                }
            }
            else
            {
                string msg = collectionResult != null ? collectionResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 GeoNFT Collection to update: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>> ListAllWeb4GeoNFTCollections(ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading WEB4 Geo-NFT Collection's...");
            OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> result = new OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>();
            result = ListWeb4GeoNFTCollections(await NFTCommon.NFTManager.LoadAllGeoNFTCollectionsAsync(providerType: providerType));
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>> ListWeb4GeoNFTCollectionsForAvatar(ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading WEB4 Geo-NFT Collection's...");
            OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> result = new OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>();
            result = ListWeb4GeoNFTCollections(await NFTCommon.NFTManager.LoadGeoNFTCollectionsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType: providerType));
            return result;
        }

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> AddWeb4GeoNFTToCollectionAsync(string collectionIdOrName, string nftIdOrName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new OASISResult<IWeb4OASISGeoNFTCollection>();
            OASISResult<IWeb4OASISGeoNFTCollection> collection = await FindWeb4GeoNFTCollectionAsync("add to", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding WEB4 GeoNFT Collection to add to. Reason: {collection.Message}");
                return result;
            }

            OASISResult<IWeb4OASISGeoSpatialNFT> geoNft = await STARCLI.GeoNFTs.FindWeb4GeoNFTAsync("add", nftIdOrName, true);

            if (geoNft == null || geoNft.Result == null || geoNft.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding WEB4 GeoNFT. Reason: {geoNft.Message}");
                return result;
            }

            CLIEngine.ShowWorkingMessage("Adding WEB4 GeoNFT To Collection...");
            result = await NFTCommon.NFTManager.AddGeoNFTToCollectionAsync(collection.Result.Id, geoNft.Result.Id, providerType);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("WEB4 OASIS GeoNFT Successfully Added to Collection.");
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured adding WEB4 GeoNFT to collection. Reason: {result.Message}");
            
            return result;
        }

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> RemoveWeb4GeoNFTFromCollectionAsync(string collectionIdOrName, string nftIdOrName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new OASISResult<IWeb4OASISGeoNFTCollection>();
            OASISResult<IWeb4OASISGeoNFTCollection> collection = await FindWeb4GeoNFTCollectionAsync("remove from", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding WEB4 GeoNFT Collection to remove from. Reason: {collection.Message}");
                return result;
            }

            OASISResult<IWeb4OASISGeoSpatialNFT> geoNft = await STARCLI.GeoNFTs.FindWeb4GeoNFTAsync("add", nftIdOrName, true);

            if (geoNft == null || geoNft.Result == null || geoNft.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding WEB4 GeoNFT. Reason: {geoNft.Message}");
                return result;
            }

            CLIEngine.ShowWorkingMessage("Removing WEB4 GeoNFT From Collection...");
            result = await NFTCommon.NFTManager.RemoveGeoNFTFromCollectionAsync(collection.Result.Id, geoNft.Result.Id, providerType);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("WEB4 OASIS GeoNFT Successfully Removed From Collection.");
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured removing WEB4 GeoNFT from collection. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IWeb4OASISGeoNFTCollection>> DeleteWeb4GeoNFTCollectionAsync(string collectionIdOrName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> collection = await FindWeb4GeoNFTCollectionAsync("delete", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref collection, $"Error occured finding WEB4 GeoNFT Collection to delete. Reason: {collection.Message}");
                return collection;
            }

            CLIEngine.ShowWorkingMessage("Deleting WEB4 GeoNFT Collection...");
            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteGeoNFTCollectionAsync(STAR.BeamedInAvatar.Id, collection.Result.Id, softDelete, providerType: providerType);

            if (deleteResult != null && deleteResult.Result && !deleteResult.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 GeoNFT Collection Successfully Deleted.");
                collection = await NFTCommon.DeleteAllSTARNETVersionsAsync("Web5STARGeoNFTCollectionId", STARNETManager, collection.Result.MetaData, collection, providerType);
            }
            else
            {
                string msg = deleteResult != null ? deleteResult.Message : "";
                OASISErrorHandling.HandleError(ref collection, $"Error occured deleting WEB4 GeoNFT Collection. Reason: {msg}");
            }

            return collection;
        }

        public virtual async Task<OASISResult<IWeb4OASISGeoNFTCollection>> ShowWeb4GeoNFTCollectionAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new OASISResult<IWeb4OASISGeoNFTCollection>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 GeoNFT Collection's...");

            result = await FindWeb4GeoNFTCollectionAsync("view", idOrName, true, providerType: providerType);

            //if (result != null && result.Result != null && !result.IsError)
            //    await ShowGeoNFTCollectionAsync(result.Result);
            //else
            //    OASISErrorHandling.HandleError(ref result, "No WEB4 GeoNFT Collection Found For That Id or Name!");

            return result;
        }

        public virtual async Task SearchWeb4GeoNFTCollectionAsync(string searchTerm = "", bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
                searchTerm = CLIEngine.GetValidInput($"What is the name of the WEB4 GeoNFT Collection you wish to search for?");

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching WEB4 GeoNFT Collection's...");
            ListWeb4GeoNFTCollections(await NFTCommon.NFTManager.SearchGeoNFTCollectionsAsync(searchTerm, STAR.BeamedInAvatar.Id, !showForAllAvatars, providerType: providerType));
        }

        private async Task<OASISResult<IWeb4OASISGeoNFTCollection>> FindWeb4GeoNFTCollectionAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "WEB4 GeoNFT Collection", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISGeoNFTCollection> result = new OASISResult<IWeb4OASISGeoNFTCollection>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadGeoNFTCollectionsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType: providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllGeoNFTCollectionsAsync(providerType: providerType);

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
                    result = await NFTCommon.NFTManager.LoadGeoNFTCollectionAsync(id, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> searchResults = await NFTCommon.NFTManager.SearchGeoNFTCollectionsAsync(idOrName, STAR.BeamedInAvatar.Id, showOnlyForCurrentAvatar, providerType: providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListWeb4GeoNFTCollections(searchResults, true);

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
                    await ShowGeoNFTCollectionAsync(result.Result);

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

        private OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> ListWeb4GeoNFTCollections(OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> collections, bool showNumbers = false, bool showDetailedInfo = false)
        {
            if (collections != null)
            {
                if (!collections.IsError)
                {
                    if (collections.Result != null && collections.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (collections.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{collections.Result.Count()} WEB4 GeoNFT Collection Found:");
                        else
                            CLIEngine.ShowMessage($"{collections.Result.Count()} WEB4 GeoNFT Collection's Found:");

                        for (int i = 0; i < collections.Result.Count(); i++)
                            ShowGeoNFTCollectionAsync(collections.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showDetailedInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No WEB4 GeoNFT Collection's Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading WEB4 GeoNFT Collection's. Reason: {collections.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading WEB4 GeoNFT Collection's.");

            return collections;
        }

        private async Task ShowGeoNFTCollectionAsync(IWeb4OASISGeoNFTCollection collection, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            //DisplayProperty("GEO-NFT COLLECTION DETAILS", "", displayFieldLength, false);
            //Console.WriteLine("");
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
            TagHelper.ShowTags(collection.Tags, displayFieldLength);

            Dictionary<string, object> metaData = collection.MetaData;

            //Temp remove internal metaData.
            collection.MetaData.Remove("Image");
            collection.MetaData.Remove("ImageUrl");
            collection.MetaData.Remove("Thumbnail");
            collection.MetaData.Remove("ThumbnailUrl");
            collection.MetaData.Remove("Web4OASISGeoNFTs");
            collection.MetaData.Remove("Web4OASISGeoNFTIds");
            collection.MetaData.Remove("Tags");

            MetaDataHelper.ShowMetaData(collection.MetaData, displayFieldLength);
            collection.MetaData = metaData;

            if (collection.Web4OASISGeoNFTs.Count() == 0 && collection.Web4OASISGeoNFTIds.Count() > 0)
            {
                OASISResult<IList<IWeb4OASISGeoSpatialNFT>> nfts = await NFTCommon.NFTManager.LoadChildGeoNFTsForNFTCollectionAsync(collection.Web4OASISGeoNFTIds);

                if (nfts != null && nfts.Result != null && !nfts.IsError)
                    collection.Web4OASISGeoNFTs = nfts.Result.ToList();
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading child nfts. Reason: {nfts.Message}");
            }

            ShowGeoNFTCollectionNFTs(collection.Web4OASISGeoNFTs, showDetailedInfo, 20);

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        private void ShowGeoNFTCollectionNFTs(IEnumerable<IWeb4OASISGeoSpatialNFT> geoNFTs, bool showDetailed = false, int defaultFieldLength = 37)
        {
            if (geoNFTs != null)
            {
                CLIEngine.ShowMessage($"{geoNFTs.Count()} Geo-NFT(s) in this collection:");
                defaultFieldLength = 37;

                if (showDetailed)
                {
                    foreach (IWeb4OASISGeoSpatialNFT geoNFT in geoNFTs)
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
                    CLIEngine.ShowMessage(string.Concat("ID".PadRight(defaultFieldLength), " | TITLE".PadRight(defaultFieldLength), " | LAT/LONG"));

                    foreach (IWeb4OASISGeoSpatialNFT geoNFT in geoNFTs)
                    {
                        if (geoNFT != null)
                            CLIEngine.ShowMessage(string.Concat(geoNFT.Id.ToString().PadRight(defaultFieldLength), " | ", geoNFT.Title.PadRight(34), " | ", $"{geoNFT.Lat}/{geoNFT.Long}"), false);
                        //CLIEngine.ShowMessage(string.Concat(geoNFT.Id.ToString().PadRight(defaultFieldLength), " | ", geoNFT.Title.PadRight(defaultFieldLength), " | ", $"{geoNFT.Lat}/{geoNFT.Long}".PadRight(defaultFieldLength)), false);
                    }
                }
            }
        }
    }
}