using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
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
            OASISResult<IOASISGeoNFTCollection> geoNFTCollectionResult = null;
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
                IOASISGeoNFTCollection geoNFTCollection = geoNFTCollectionResult.Result;
                geoNFTCollection.OASISGeoNFTs.Clear();

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS Geo-NFT Collection to WEB5 STARNET which will create a WEB5 STAR GeoNFT Collection that wraps around the WEB4 GeoNFT Collection allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended).")))
                {
                    Console.WriteLine("");

                    result = await base.CreateAsync(new STARNETCreateOptions<STARGeoNFTCollection, STARNETDNA>()
                    {
                        STARNETDNA = new STARNETDNA()
                        {
                            MetaData = new Dictionary<string, object>() { { "GeoNFTCollection", geoNFTCollection } }
                        }
                        //STARNETHolon = new STARGeoNFT() 
                        //{ 
                        //    GeoNFTId = geoNFTResult.Result.Id 
                        //}
                    }, holonSubType, showHeaderAndInro, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        //File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"OASISGeoNFT_{geoNFTCollectionResult.Result.Id}.json"), JsonConvert.SerializeObject(geoNFT));

                        //if (!string.IsNullOrEmpty(geoNFTResult.Result.JSONMetaData))
                        //    File.WriteAllText(Path.Combine(result.Result.STARNETDNA.SourcePath, $"JSONMetaData_{geoNFTResult.Result.Id}.json"), geoNFTResult.Result.JSONMetaData);

                        result.Result.GeoNFTCollectionType = (NFTCollectionType)Enum.Parse(typeof(NFTCollectionType), result.Result.STARNETDNA.STARNETCategory.ToString());
                        OASISResult<STARGeoNFTCollection> saveResult = await result.Result.SaveAsync<STARGeoNFTCollection>();

                        if (!(saveResult != null && saveResult.Result != null && !saveResult.IsError))
                            OASISErrorHandling.HandleError(ref result, $"Error occured saving STARGeoNFTCollection after creation in CreateAsync method. Reason: {saveResult.Message}");
                    }
                }
            }
            else
            {
                if (mint)
                    OASISErrorHandling.HandleError(ref result, $"Error occured creating GeoNFT Collection in CreateWeb4GeoNFTCollectionAsync method. Reason: {geoNFTCollectionResult.Message}");
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading GeoNFT Collection in LoadOASISGeoNFTCollectionAsync method. Reason: {geoNFTCollectionResult.Message}");
            }

            return result;
        }

        public override void Show<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 35, object customData = null)
        {
            displayFieldLength = DEFAULT_FIELD_LENGTH;
            base.Show(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("GeoNFTCollection") && starHolon.STARNETDNA.MetaData["GeoNFTCollection"] != null)
            {
                IOASISGeoNFTCollection collection = starHolon.STARNETDNA.MetaData["GeoNFTCollection"] as IOASISGeoNFTCollection;

                if (collection == null)
                    collection = JsonConvert.DeserializeObject<OASISGeoNFTCollection>(starHolon.STARNETDNA.MetaData["GeoNFTCollection"].ToString());

                if (collection != null)
                {
                    Console.WriteLine("");
                    DisplayProperty("GEO-NFT COLLECTION DETAILS", "", displayFieldLength, false);
                    ShowGeoNFTCollectionAsync(collection, showHeader: false, showFooter: false);
                }
            }

            CLIEngine.ShowDivider();
        }

        public async Task<OASISResult<IOASISGeoNFTCollection>> CreateWeb4GeoNFTCollectionAsync(object createOptions = null, ProviderType providerType = ProviderType.Default)
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

            Console.WriteLine("");
            if (CLIEngine.GetConfirmation("Do you wish to add any GeoNFT's to this collection now? (You can always add more later)."))
            {
                request.OASISGeoNFTs = new List<IOASISGeoSpatialNFT>();
                OASISResult<IOASISGeoSpatialNFT> nftResult = null;

                do
                {
                    Console.WriteLine("");

                    if (CLIEngine.GetConfirmation("Does the GeoNFT already exist? (If you select 'N' you will be taken through the minting process to create a new GeoNFT to add to the collection)."))
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
                        request.OASISGeoNFTs.Add(nftResult.Result);
                    else
                    {
                        string msg = nftResult != null ? nftResult.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Finding GeoNFT to add to Collection: {msg}");
                        return result;
                    }

                    CLIEngine.ShowSuccessMessage("Geo-NFT Successfully Added To The Collection.");
                    ShowGeoNFTCollectionNFTs(request.OASISGeoNFTs);

                } while (CLIEngine.GetConfirmation("Do you wish to add another GeoNFT to this collection?"));
            }

            Console.WriteLine("");
            result = await NFTCommon.NFTManager.CreateOASISGeoNFTCollectionAsyc(request, providerType);

            if (result != null && result.Result != null && !result.IsError)
            {
                CLIEngine.ShowSuccessMessage("OASIS GeoNFT Collection Successfully Created.");
                await ShowGeoNFTCollectionAsync(result.Result);
            }
            else
            {
                string msg = result != null ? result.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured Creating GeoNFT Collection: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IOASISGeoNFTCollection>> UpdateWeb4GeoNFTCollectionAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new OASISResult<IOASISGeoNFTCollection>();
            UpdateOASISGeoNFTCollectionRequest request = new UpdateOASISGeoNFTCollectionRequest();

            OASISResult<IOASISGeoNFTCollection> collectionResult = await FindWeb4GeoNFTCollectionAsync("update", idOrName, providerType: providerType);

            if (collectionResult != null && collectionResult.Result != null && !collectionResult.IsError)
            {
               //request.OASISGeoNFTs.AddRange(collectionResult.Result.OASISGeoNFTs);

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

                //if (CLIEngine.GetConfirmation("Do you wish to add more GeoNFT's to this collection now? (You can always add more later)."))
                //{
                //    request.OASISGeoNFTs = new List<IOASISGeoSpatialNFT>();
                //    OASISResult<IOASISGeoSpatialNFT> nftResult = null;

                //    do
                //    {
                //        if (CLIEngine.GetConfirmation("Does the GeoNFT already exist? (If you select 'N' you will be taken through the minting process to create a new GeoNFT to add to the collection)."))
                //            nftResult = await FindWeb4GeoNFTAsync("use", providerType: providerType);
                //        else
                //            nftResult = await MintGeoNFTAsync();

                //        if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                //            request.OASISGeoNFTs.Add(nftResult.Result);
                //        else
                //        {
                //            string msg = nftResult != null ? nftResult.Message : "";
                //            OASISErrorHandling.HandleError(ref result, $"Error Occured Finding GeoNFT to add to Collection: {msg}");
                //            return result;
                //        }

                //        ShowGeoNFTCollectionNFTs(collectionResult.Result);

                //    } while (CLIEngine.GetConfirmation("Do you wish to add another GeoNFT to this collection?"));
                //}

                result = await NFTCommon.NFTManager.UpdateOASISGeoNFTCollectionAsync(request, providerType);

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

        public async Task<OASISResult<IEnumerable<IOASISGeoNFTCollection>>> ListAllWeb4GeoNFTCollections(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoNFTCollection>> result = new OASISResult<IEnumerable<IOASISGeoNFTCollection>>();
            result = ListWeb4GeoNFTCollections(await NFTCommon.NFTManager.LoadAllGeoNFTCollectionsAsync(providerType: providerType));
            return result;
        }

        public async Task<OASISResult<IEnumerable<IOASISGeoNFTCollection>>> ListWeb4GeoNFTCollectionsForAvatar(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOASISGeoNFTCollection>> result = new OASISResult<IEnumerable<IOASISGeoNFTCollection>>();
            result = ListWeb4GeoNFTCollections(await NFTCommon.NFTManager.LoadGeoNFTCollectionsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType: providerType));
            return result;
        }

        public async Task<OASISResult<IOASISGeoNFTCollection>> AddWeb4GeoNFTToCollectionAsync(string collectionIdOrName, string nftIdOrName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new OASISResult<IOASISGeoNFTCollection>();
            OASISResult<IOASISGeoNFTCollection> collection = await FindWeb4GeoNFTCollectionAsync("add to", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding GeoNFT Collection to add to. Reason: {collection.Message}");
                return result;
            }

            OASISResult<IOASISGeoSpatialNFT> geoNft = await STARCLI.GeoNFTs.FindWeb4GeoNFTAsync("add", nftIdOrName, true);

            if (geoNft == null || geoNft.Result == null || geoNft.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding GeoNFT. Reason: {geoNft.Message}");
                return result;
            }

            result = await NFTCommon.NFTManager.AddOASISGeoNFTToCollectionAsync(collection.Result.Id, geoNft.Result.Id, providerType);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("OASIS GeoNFT Successfully Added to Collection.");
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured adding GeoNFT to collection. Reason: {result.Message}");
            
            return result;
        }

        public async Task<OASISResult<IOASISGeoNFTCollection>> RemoveWeb4GeoNFTFromCollectionAsync(string collectionIdOrName, string nftIdOrName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new OASISResult<IOASISGeoNFTCollection>();
            OASISResult<IOASISGeoNFTCollection> collection = await FindWeb4GeoNFTCollectionAsync("remove from", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding GeoNFT Collection to remove from. Reason: {collection.Message}");
                return result;
            }

            OASISResult<IOASISGeoSpatialNFT> geoNft = await STARCLI.GeoNFTs.FindWeb4GeoNFTAsync("add", nftIdOrName, true);

            if (geoNft == null || geoNft.Result == null || geoNft.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding GeoNFT. Reason: {geoNft.Message}");
                return result;
            }

            result = await NFTCommon.NFTManager.RemoveOASISGeoNFTFromCollectionAsync(collection.Result.Id, geoNft.Result.Id, providerType);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("OASIS GeoNFT Successfully Removed From Collection.");
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured removing GeoNFT from collection. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IOASISGeoNFTCollection>> DeleteWeb4GeoNFTCollectionAsync(string collectionIdOrName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> collection = await FindWeb4GeoNFTCollectionAsync("delete", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref collection, $"Error occured finding GeoNFT Collection to delete. Reason: {collection.Message}");
                return collection;
            }

            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteOASISGeoNFTCollectionAsync(STAR.BeamedInAvatar.Id, collection.Result.Id, softDelete, providerType: providerType);

            if (deleteResult != null && deleteResult.Result && !deleteResult.IsError)
            {
                CLIEngine.ShowSuccessMessage("OASIS GeoNFT Collection Successfully Deleted.");
                return collection;
            }
            else
            {
                string msg = deleteResult != null ? deleteResult.Message : "";
                OASISErrorHandling.HandleError(ref collection, $"Error occured deleting GeoNFT Collection. Reason: {msg}");
                return collection;
            }

            return collection;
        }

        public virtual async Task<OASISResult<IOASISGeoNFTCollection>> ShowWeb4GeoNFTCollectionAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new OASISResult<IOASISGeoNFTCollection>();

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading WEB4 GeoNFT Collection's...");

            result = await FindWeb4GeoNFTCollectionAsync("view", idOrName, true, providerType: providerType);

            if (result != null && result.Result != null && !result.IsError)
                await ShowGeoNFTCollectionAsync(result.Result);
            else
                OASISErrorHandling.HandleError(ref result, "No WEB4 GeoNFT Collection Found For That Id or Name!");

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
                    result = await NFTCommon.NFTManager.LoadOASISGeoNFTCollectionAsync(id, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IOASISGeoNFTCollection>> searchResults = await NFTCommon.NFTManager.SearchGeoNFTCollectionsAsync(idOrName, STAR.BeamedInAvatar.Id, showOnlyForCurrentAvatar, providerType: providerType);

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

        private OASISResult<IEnumerable<IOASISGeoNFTCollection>> ListWeb4GeoNFTCollections(OASISResult<IEnumerable<IOASISGeoNFTCollection>> collections, bool showNumbers = false, bool showDetailedInfo = false)
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

        private async Task ShowGeoNFTCollectionAsync(IOASISGeoNFTCollection collection, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH)
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
            
            Dictionary<string, object> metaData = collection.MetaData;

            //Temp remove internal metaData.
            collection.MetaData.Remove("Image");
            collection.MetaData.Remove("ImageUrl");
            collection.MetaData.Remove("Thumbnail");
            collection.MetaData.Remove("ThumbnailUrl");
            collection.MetaData.Remove("OASISGeoNFTs");
            collection.MetaData.Remove("OASISGeoNFTIds");
            collection.MetaData.Remove("Tags");

            ShowMetaData(collection.MetaData);
            collection.MetaData = metaData;

            if (collection.OASISGeoNFTs.Count() == 0 && collection.OASISGeoNFTIds.Count() > 0)
            {
                OASISResult<IList<IOASISGeoSpatialNFT>> nfts = await NFTCommon.NFTManager.LoadChildGeoNFTsForNFTCollectionAsync(collection.OASISGeoNFTIds);

                if (nfts != null && nfts.Result != null && !nfts.IsError)
                    collection.OASISGeoNFTs = nfts.Result.ToList();
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading child nfts. Reason: {nfts.Message}");
            }

            ShowGeoNFTCollectionNFTs(collection.OASISGeoNFTs, showDetailedInfo, 20);

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        private void ShowGeoNFTCollectionNFTs(IEnumerable<IOASISGeoSpatialNFT> geoNFTs, bool showDetailed = false, int defaultFieldLength = 37)
        {
            if (geoNFTs != null)
            {
                CLIEngine.ShowMessage($"{geoNFTs.Count()} Geo-NFT(s) in this collection:");
                defaultFieldLength = 37;

                if (showDetailed)
                {
                    foreach (IOASISGeoSpatialNFT geoNFT in geoNFTs)
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

                    foreach (IOASISGeoSpatialNFT geoNFT in geoNFTs)
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