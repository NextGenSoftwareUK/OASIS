using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class NFTCollections : STARNETUIBase<STARNFTCollection, DownloadedNFTCollection, InstalledNFTCollection, STARNETDNA>
    {
        public NFTCommon NFTCommon { get; set; } = new NFTCommon();
        //public NFTs NFTs { get; set; } = new NFTs(STAR.BeamedInAvatar.Id, STAR.STARDNA);

        public NFTCollections(Guid avatarId, STARDNA STARDNA) : base(new STARNFTCollectionManager(avatarId, STARDNA),
            "Welcome to the WEB5 STAR NFT Collection Wizard", new List<string> 
            {
                "This wizard will allow you create a WEB5 STAR NFT Collection which wraps around a WEB4 OASIS NFT Collection.",
                "You can create a WEB5 OASIS NFT Collection using the 'nft collection create web4' sub-command.",
                "You then convert or wrap around the WEB4 OASIS NFT Collection using the sub-command 'nft collection create' which will create a WEB5 STAR NFT Collection compatible with STARNET.",
                "A WEB5 NFT Collection can then be published to STARNET in much the same way as everything else within STAR using the same sub-commands such as publish, download, install etc.",
                "The wizard will create an empty folder with a NFTCollectionDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the NFT Collection into this folder.",
                "Finally you run the sub-command 'nft collection publish' to convert the folder containing the NFT (can contain any number of files and sub-folders) into a OASIS NFT Collection file (.onftcollection) as well as optionally upload to STARNET.",
                "You can then share the .onftcollection file with others across any platform or OS, who can then install the NFT Collection from the file using the sub-command 'nft collection install'.",
                "You can also optionally choose to upload the .onftcollection file to the STARNET store so others can search, download and install the NFT Collection."
            },
            STAR.STARDNA.DefaultNFTCollectionsSourcePath, "DefaultNFTCollectionsSourcePath",
            STAR.STARDNA.DefaultNFTCollectionsPublishedPath, "DefaultNFTCollectionsPublishedPath",
            STAR.STARDNA.DefaultNFTCollectionsDownloadedPath, "DefaultNFTCollectionsDownloadedPath",
            STAR.STARDNA.DefaultNFTCollectionsInstalledPath, "DefaultNFTCollectionsInstalledPath")
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

        public override async Task<OASISResult<STARNFTCollection>> CreateAsync(ISTARNETCreateOptions<STARNFTCollection, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARNFTCollection> result = new OASISResult<STARNFTCollection>();
            OASISResult<IWeb4OASISNFTCollection> collectionResult = null;
            bool mint = false;

            ShowHeader();

            if (CLIEngine.GetConfirmation("Do you have an existing WEB4 OASIS NFT Collection you wish to create a WEB5 NFT Collection from?"))
            {
                Console.WriteLine("");
                collectionResult = await FindWeb4NFTCollectionAsync("wrap");

                //Guid id = CLIEngine.GetValidInputForGuid("Please enter the ID of the WEB4 NFT Collection you wish to upload to STARNET: ");

                //if (id != Guid.Empty)
                //    collectionResult = await STAR.OASISAPI.NFTs.LoadOASISNFTCollectionAsync(id, providerType: providerType);
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
                collectionResult = await CreateWeb4NFTCollectionAsync(providerType);
                mint = true;
            }

            if (collectionResult != null && collectionResult.Result != null && !collectionResult.IsError)
            {
                IWeb4OASISNFTCollection collection = collectionResult.Result;
                collection.Web4OASISNFTs.Clear();

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS NFT Collection to WEB5 STARNET which will create a WEB5 STAR NFT Collection that wraps around the WEB4 OASISNFT Collection allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended).")))
                {
                    Console.WriteLine("");

                    result = await base.CreateAsync(new STARNETCreateOptions<STARNFTCollection, STARNETDNA>()
                    {
                        STARNETDNA = new STARNETDNA()
                        {
                            MetaData = new Dictionary<string, object>() { { "NFTCollection", collection } }
                        }
                        //STARNETHolon = new STARNFTCollection()
                        //{
                        //    OASISNFTId = NFTResult.Result.Id,
                        //    NFTCollectionType = collection.NFTCollectionType
                        //}
                    }, holonSubType, showHeaderAndInro, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        result.Result.NFTCollectionType = (NFTCollectionType)Enum.Parse(typeof(NFTCollectionType), result.Result.STARNETDNA.STARNETCategory.ToString());
                        OASISResult<STARNFTCollection> saveResult = await result.Result.SaveAsync<STARNFTCollection>();

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured saving WEB5 STAR NFT Collection after creation in CreateAsync method. Reason: {saveResult.Message}");
                    }
                }
            }
            else
            {
                if (mint)
                    OASISErrorHandling.HandleError(ref result, $"Error occured creating WEB4 NFT Collection in CreateWeb4NFTCollectionAsync method. Reason: {collectionResult.Message}");
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading WEB4 NFT Collection in LoadOASISNFTCollectionAsync method. Reason: {collectionResult.Message}");
            }

            return result;
        }

        public override Task UpdateAsync(string idOrName = "", object editParams = null, bool editLaunchTarget = true, ProviderType providerType = ProviderType.Default)
        {
            return base.UpdateAsync(idOrName, editParams, false, providerType);
        }

        public override void Show<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 35, object customData = null)
        {
            displayFieldLength = DEFAULT_FIELD_LENGTH;
            base.Show(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("NFTCollection") && starHolon.STARNETDNA.MetaData["NFTCollection"] != null)
            {
                IWeb4OASISNFTCollection collection = starHolon.STARNETDNA.MetaData["NFTCollection"] as IWeb4OASISNFTCollection;

                if (collection == null)
                    collection = JsonConvert.DeserializeObject<Web4OASISNFTCollection>(starHolon.STARNETDNA.MetaData["NFTCollection"].ToString());

                if (collection != null)
                {
                    Console.WriteLine("");
                    DisplayProperty("WEB4 NFT COLLECTION DETAILS", "", displayFieldLength, false);
                    ShowNFTCollectionAsync(collection, showHeader: false, showFooter: false);
                }
            }

            CLIEngine.ShowDivider();
        }

        public async Task<OASISResult<IWeb4OASISNFTCollection>> CreateWeb4NFTCollectionAsync(object createOptions = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new OASISResult<IWeb4OASISNFTCollection>();
            CreateWeb4NFTCollectionRequest request = new CreateWeb4NFTCollectionRequest();

            request.Title = CLIEngine.GetValidInput("Please enter a title for the WEB4 NFT Collection: ");
            request.Description = CLIEngine.GetValidInput("Please enter a description for the WEB4 NFT Collection: ");
            request.CreatedBy = STAR.BeamedInAvatar.Id;

            OASISResult<ImageAndThumbnail> imageAndThumbnailResult = NFTCommon.ProcessImageAndThumbnail("WEB4 NFT Collection");

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

            request.MetaData = request.MetaData = MetaDataHelper.AddMetaData("NFT Collection");

            Console.WriteLine("");
            if (CLIEngine.GetConfirmation("Do you wish to add any WEB4 NFTs to this collection now? (You can always add more later)."))
            {
                request.Web4OASISNFTs = new List<IWeb4OASISNFT>();
                OASISResult<IWeb4OASISNFT> nftResult = null;

                do
                {
                    Console.WriteLine("");

                    if (CLIEngine.GetConfirmation("Does the WEB4 NFT already exist? (If you select 'N' you will be taken through the minting process to create a new NFT to add to the collection)."))
                    {
                        Console.WriteLine("");
                        nftResult = await STARCLI.NFTs.FindWeb4NFTAsync("use", providerType: providerType);
                    }
                    else
                    {
                        Console.WriteLine("");
                        nftResult = await STARCLI.NFTs.MintNFTAsync();
                    }

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                        request.Web4OASISNFTs.Add(nftResult.Result);
                    else
                    {
                        string msg = nftResult != null ? nftResult.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 NFT to add to Collection: {msg}");
                        return result;
                    }

                    CLIEngine.ShowSuccessMessage("WEB4 NFT Successfully Added To The Collection.");
                    ShowNFTCollectionNFTs(request.Web4OASISNFTs);

                } while (CLIEngine.GetConfirmation("Do you wish to add another WEB4 NFT to this collection?"));
            }

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Creating WEB4 NFT Collection...");
            result = await NFTCommon.NFTManager.CreateOASISNFTCollectionAsync(request, providerType);

            if (result != null && result.Result != null && !result.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 OASIS NFT Collection Successfully Created.");
                await ShowNFTCollectionAsync(result.Result);
            }
            else
            {
                string msg = result != null ? result.Message : "";
                CLIEngine.ShowErrorMessage($"Error Occured Creating WEB4 NFT Collection: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IWeb4OASISNFTCollection>> UpdateWeb4NFTCollectionAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new OASISResult<IWeb4OASISNFTCollection>();
            UpdateWeb4NFTCollectionRequest request = new UpdateWeb4NFTCollectionRequest();

            OASISResult<IWeb4OASISNFTCollection> collectionResult = await FindWeb4NFTCollectionAsync("update", idOrName, providerType: providerType);

            if (collectionResult != null && collectionResult.Result != null && !collectionResult.IsError)
            {
                request.Id = collectionResult.Result.Id;

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


                request.MetaData = request.MetaData = MetaDataHelper.AddMetaData("WEB4 NFT Collection");

                //if (CLIEngine.GetConfirmation("Do you wish to add more NFT's to this collection now? (You can always add more later)."))
                //{
                //    request.Web4OASISNFTs = new List<IWeb4OASISNFT>();
                //    OASISResult<IWeb4OASISNFT> nftResult = null;

                //    do
                //    {
                //        if (CLIEngine.GetConfirmation("Does the NFT already exist? (If you select 'N' you will be taken through the minting process to create a new NFT to add to the collection)."))
                //            nftResult = await FindWeb4NFTAsync("use", providerType: providerType);
                //        else
                //            nftResult = await MintNFTAsync();

                //        if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                //            request.Web4OASISNFTs.Add(nftResult.Result);
                //        else
                //        {
                //            string msg = nftResult != null ? nftResult.Message : "";
                //            OASISErrorHandling.HandleError(ref result, $"Error Occured Finding NFT to add to Collection: {msg}");
                //            return result;
                //        }

                //        ShowNFTCollectionNFTs(collectionResult.Result.Web4OASISNFTs);

                //    } while (CLIEngine.GetConfirmation("Do you wish to add another NFT to this collection?"));
                //}

                CLIEngine.ShowWorkingMessage("Updating WEB4 NFT Collection...");
                result = await NFTCommon.NFTManager.UpdateOASISNFTCollectionAsync(request, providerType);

                if (result != null && result.Result != null && !result.IsError)
                    CLIEngine.ShowSuccessMessage("WEB4 OASIS NFT Collection Successfully Updated.");
                else
                {
                    string msg = result != null ? result.Message : "";
                    CLIEngine.ShowErrorMessage($"Error Occured Updating WEB4 NFT Collection: {msg}");
                }
            }
            else
            {
                string msg = collectionResult != null ? collectionResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 NFT Collection to update: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4OASISNFTCollection>>> ListAllWeb4NFTCollections(ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading WEB4 NFT Collections...");
            OASISResult<IEnumerable<IWeb4OASISNFTCollection>> result = new OASISResult<IEnumerable<IWeb4OASISNFTCollection>>();
            result = ListWeb4NFTCollections(await NFTCommon.NFTManager.LoadAllNFTCollectionsAsync(providerType: providerType));
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4OASISNFTCollection>>> ListWeb4NFTCollectionsForAvatar(ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading WEB4 NFT Collections...");
            OASISResult<IEnumerable<IWeb4OASISNFTCollection>> result = new OASISResult<IEnumerable<IWeb4OASISNFTCollection>>();
            result = ListWeb4NFTCollections(await NFTCommon.NFTManager.LoadNFTCollectionsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType: providerType));
            return result;
        }

        public async Task<OASISResult<IWeb4OASISNFTCollection>> AddWeb4NFTToCollectionAsync(string collectionIdOrName, string nftIdOrName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new OASISResult<IWeb4OASISNFTCollection>();
            OASISResult<IWeb4OASISNFTCollection> collection = await FindWeb4NFTCollectionAsync("add to", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding WEB4 NFT Collection to add to. Reason: {collection.Message}");
                return result;
            }

            OASISResult<IWeb4OASISNFT> geoNft = await STARCLI.NFTs.FindWeb4NFTAsync("add", nftIdOrName, true);

            if (geoNft == null || geoNft.Result == null || geoNft.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding WEB4 NFT. Reason: {geoNft.Message}");
                return result;
            }

            CLIEngine.ShowWorkingMessage("Adding WEB4 NFT To Collection...");
            result = await NFTCommon.NFTManager.AddOASISNFTToCollectionAsync(collection.Result.Id, geoNft.Result.Id, providerType: providerType);

            if (result != null && result.Result != null && !result.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 OASIS NFT Successfully Added to Collection.");
                await ShowNFTCollectionAsync(result.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured adding NFT to collection. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IWeb4OASISNFTCollection>> RemoveWeb4NFTFromCollectionAsync(string collectionIdOrName, string nftIdOrName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new OASISResult<IWeb4OASISNFTCollection>();
            OASISResult<IWeb4OASISNFTCollection> collection = await FindWeb4NFTCollectionAsync("remove from", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding NFT Collection to remove from. Reason: {collection.Message}");
                return result;
            }

            OASISResult<IWeb4OASISNFT> geoNft = await STARCLI.NFTs.FindWeb4NFTAsync("add", nftIdOrName, true);

            if (geoNft == null || geoNft.Result == null || geoNft.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding NFT. Reason: {geoNft.Message}");
                return result;
            }

            CLIEngine.ShowWorkingMessage("Removing WEB4 NFT From Collection...");
            result = await NFTCommon.NFTManager.RemoveOASISNFTFromCollectionAsync(collection.Result.Id, geoNft.Result.Id, providerType);

            if (result != null && result.Result != null && !result.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 OASIS NFT Successfully Removed From Collection.");
                await ShowNFTCollectionAsync(result.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured removing WEB4 NFT from collection. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IWeb4OASISNFTCollection>> DeleteWeb4NFTCollectionAsync(string collectionIdOrName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> collection = await FindWeb4NFTCollectionAsync("delete", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref collection, $"Error occured finding NFT Collection to delete. Reason: {collection.Message}");
                return collection;
            }

            CLIEngine.ShowWorkingMessage("Deleting WEB4 NFT Collection...");
            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteOASISNFTCollectionAsync(STAR.BeamedInAvatar.Id, collection.Result.Id, softDelete, providerType: providerType);

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

        public virtual async Task<OASISResult<IWeb4OASISNFTCollection>> ShowWeb4NFTCollectionAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new OASISResult<IWeb4OASISNFTCollection>();

            //Console.WriteLine("");
            //CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT Collection's...");

            result = await FindWeb4NFTCollectionAsync("view", idOrName, true, providerType: providerType);

            if (result != null && result.Result != null && !result.IsError)
                await ShowNFTCollectionAsync(result.Result);
            else
                OASISErrorHandling.HandleError(ref result, "No WEB4 NFT Collection Found For That Id or Name!");

            return result;
        }

        public virtual async Task SearchWeb4NFTCollectionAsync(string searchTerm = "", bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
                searchTerm = CLIEngine.GetValidInput($"What is the name of the WEB4 NFT Collection you wish to search for?");

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching WEB4 NFT Collection's...");
            ListWeb4NFTCollections(await NFTCommon.NFTManager.SearchNFTCollectionsAsync(searchTerm, STAR.BeamedInAvatar.Id, !showForAllAvatars, providerType: providerType));
        }

        private async Task<OASISResult<IWeb4OASISNFTCollection>> FindWeb4NFTCollectionAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "WEB4 NFT Collection", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4OASISNFTCollection> result = new OASISResult<IWeb4OASISNFTCollection>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IWeb4OASISNFTCollection>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadNFTCollectionsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType: providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllNFTCollectionsAsync(providerType: providerType);

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
                    OASISResult<IEnumerable<IWeb4OASISNFTCollection>> searchResults = await NFTCommon.NFTManager.SearchNFTCollectionsAsync(idOrName, STAR.BeamedInAvatar.Id, showOnlyForCurrentAvatar, providerType: providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListWeb4NFTCollections(searchResults, true);

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
                    await ShowNFTCollectionAsync(result.Result);

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

        private OASISResult<IEnumerable<IWeb4OASISNFTCollection>> ListWeb4NFTCollections(OASISResult<IEnumerable<IWeb4OASISNFTCollection>> collections, bool showNumbers = false, bool showDetailedInfo = false)
        {
            if (collections != null)
            {
                if (!collections.IsError)
                {
                    if (collections.Result != null && collections.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (collections.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{collections.Result.Count()} WEB4 NFT Collection Found:");
                        else
                            CLIEngine.ShowMessage($"{collections.Result.Count()} WEB4 Collection's Found:");

                        for (int i = 0; i < collections.Result.Count(); i++)
                            ShowNFTCollectionAsync(collections.Result.ElementAt(i), i == 0, true, showNumbers, i + 1, showDetailedInfo);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No WEB4 NFT Collection's Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading WEB4 NFT Collection's. Reason: {collections.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading WEB4 NFT Collection's.");

            return collections;
        }

        private async Task ShowNFTCollectionAsync(IWeb4OASISNFTCollection collection, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            //DisplayProperty("NFT COLLECTION DETAILS", "", displayFieldLength, false);
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
            collection.MetaData.Remove("Web4OASISNFTs");
            collection.MetaData.Remove("Web4OASISNFTIds");
            collection.MetaData.Remove("Tags");

            MetaDataHelper.ShowMetaData(collection.MetaData);
            collection.MetaData = metaData;

            if (collection.Web4OASISNFTs.Count() == 0 && collection.Web4OASISNFTIds.Count() > 0)
            {
                OASISResult<IList<IWeb4OASISNFT>> nfts = await NFTCommon.NFTManager.LoadChildNFTsForNFTCollectionAsync(collection.Web4OASISNFTIds);

                if (nfts != null && nfts.Result != null && !nfts.IsError)
                    collection.Web4OASISNFTs = nfts.Result.ToList();
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading child nfts. Reason: {nfts.Message}");
            }

            ShowNFTCollectionNFTs(collection.Web4OASISNFTs, showDetailedInfo, 20);

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        private void ShowNFTCollectionNFTs(IEnumerable<IWeb4OASISNFT> nfts, bool showDetailed = false, int defaultFieldLength = 37)
        {
            if (nfts != null)
            {
                CLIEngine.ShowMessage($"{nfts.Count()} NFT(s) in this collection:");
                defaultFieldLength = 37;

                if (showDetailed)
                {
                    foreach (IWeb4OASISNFT nft in nfts)
                    {
                        if (nft != null)
                        {
                            Console.WriteLine("");
                            DisplayProperty("Id", nft.Id.ToString(), DEFAULT_FIELD_LENGTH);
                            DisplayProperty("Title", nft.Title, DEFAULT_FIELD_LENGTH);
                            DisplayProperty("Description", nft.Description, DEFAULT_FIELD_LENGTH);
                            //DisplayProperty("Lat/Long", $"{geoNFT.Lat}/{geoNFT.Long}", DEFAULT_FIELD_LENGTH);
                        }
                    }
                }
                else if (nfts.Count() > 0)
                {
                    CLIEngine.ShowMessage(string.Concat("ID".PadRight(defaultFieldLength), " | TITLE".PadRight(defaultFieldLength)));

                    foreach (IWeb4OASISNFT nft in nfts)
                    {
                        if (nft != null)
                            CLIEngine.ShowMessage(string.Concat(nft.Id.ToString().PadRight(defaultFieldLength), " | ", nft.Title.PadRight(defaultFieldLength)), false);
                    }
                }
            }
        }
    }
}