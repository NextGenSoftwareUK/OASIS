using ADRaffy.ENSNormalize;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
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

        public override async Task<OASISResult<STARNFTCollection>> CreateAsync(ISTARNETCreateOptions<STARNFTCollection, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<STARNFTCollection> result = new OASISResult<STARNFTCollection>();
            OASISResult<IWeb4NFTCollection> collectionResult = null;
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
                IWeb4NFTCollection collection = collectionResult.Result;
                collection.Web4NFTs.Clear();

                if (!mint || (mint && CLIEngine.GetConfirmation("Would you like to submit the WEB4 OASIS NFT Collection to WEB5 STARNET which will create a WEB5 STAR NFT Collection that wraps around the WEB4 OASISNFT Collection allowing you to version control, publish, share, use in Our World, Quests, etc? (recommended).")))
                {
                    Console.WriteLine("");

                    result = await base.CreateAsync(new STARNETCreateOptions<STARNFTCollection, STARNETDNA>()
                    {
                        STARNETDNA = new STARNETDNA()
                        {
                            MetaData = new Dictionary<string, object>() { { "NFTCollection", collection } }
                            //MetaData = new Dictionary<string, object>() { { "NFTCollectionId", collection.Id } }
                        },
                        STARNETHolon = new STARNFTCollection()
                        {
                            NFTCollectionId = collection.Id
                        }
                    }, holonSubType, showHeaderAndInro, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        result.Result.NFTCollectionType = (NFTCollectionType)Enum.Parse(typeof(NFTCollectionType), result.Result.STARNETDNA.STARNETCategory.ToString());
                        OASISResult<STARNFTCollection> saveResult = await result.Result.SaveAsync<STARNFTCollection>();

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            collection.MetaData["Web5STARGeoNFTId"] = saveResult.Result.Id.ToString();
                            OASISResult<IWeb4NFTCollection> web4NFTCollection = await NFTCommon.NFTManager.UpdateWeb4NFTCollectionAsync(new UpdateWeb4NFTCollectionRequest() { Id = collection.Id, ModifiedBy = STAR.BeamedInAvatar.Id, MetaData = collection.MetaData }, providerType: providerType);

                            if (!(web4NFTCollection != null && web4NFTCollection.Result != null && !web4NFTCollection.IsError))
                                OASISErrorHandling.HandleError(ref result, $"Error occured updating WEB4 NFT Collection after creation of WEB5 STAR NFT Collection in CreateAsync method. Reason: {web4NFTCollection.Message}");
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

        public override async Task ShowAsync<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = 35, object customData = null)
        {
            displayFieldLength = DEFAULT_FIELD_LENGTH;
            await base.ShowAsync(starHolon, showHeader, false, showNumbers, number, showDetailedInfo, displayFieldLength, customData);

            //if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("NFTCollectionId") && starHolon.STARNETDNA.MetaData["NFTCollectionId"] != null)
            //{
            //    Guid id = Guid.Empty;

            //    if (Guid.TryParse(starHolon.STARNETDNA.MetaData["NFTCollectionId"].ToString(), out id))
            //    {
            //        OASISResult<IWeb4OASISNFTCollection> web4NFTCollection = await NFTCommon.NFTManager.LoadNFTCollectionAsync(id);

            //        if (web4NFTCollection != null && web4NFTCollection.Result != null && !web4NFTCollection.IsError)
            //        {
            //            Console.WriteLine("");
            //            DisplayProperty("WEB4 NFT COLLECTION DETAILS", "", displayFieldLength, false);
            //            await ShowNFTCollectionAsync(web4NFTCollection.Result, showHeader: false, showFooter: false);
            //        }
            //    }
            //}

            if (starHolon.STARNETDNA != null && starHolon.STARNETDNA.MetaData != null && starHolon.STARNETDNA.MetaData.ContainsKey("NFTCollection") && starHolon.STARNETDNA.MetaData["NFTCollection"] != null)
            {
                IWeb4NFTCollection collection = starHolon.STARNETDNA.MetaData["NFTCollection"] as IWeb4NFTCollection;

                if (collection == null)
                    collection = JsonConvert.DeserializeObject<Web4NFTCollection>(starHolon.STARNETDNA.MetaData["NFTCollection"].ToString());

                if (collection != null)
                {
                    Console.WriteLine("");
                    DisplayProperty("WEB4 NFT COLLECTION DETAILS", "", displayFieldLength, false);
                    ShowNFTCollectionAsync(collection, showHeader: false, showFooter: false);
                }
            }

            CLIEngine.ShowDivider();
        }

        public async Task<OASISResult<IWeb4NFTCollection>> CreateWeb4NFTCollectionAsync(object createOptions = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new OASISResult<IWeb4NFTCollection>();
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

            request.MetaData = MetaDataHelper.ManageMetaData(request.MetaData, "WEB4 NFT Collection");

            Console.WriteLine("");
            if (CLIEngine.GetConfirmation("Do you wish to add any WEB4 NFTs to this collection now? (You can always add more later)."))
            {
                request.Web4NFTs = new List<IWeb4NFT>();
                OASISResult<IWeb4NFT> nftResult = null;

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
                        request.Web4NFTs.Add(nftResult.Result);
                    else
                    {
                        string msg = nftResult != null ? nftResult.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 NFT to add to Collection: {msg}");
                        return result;
                    }

                    CLIEngine.ShowSuccessMessage("WEB4 NFT Successfully Added To The Collection.");
                    ShowNFTCollectionNFTs(request.Web4NFTs);

                } while (CLIEngine.GetConfirmation("Do you wish to add another WEB4 NFT to this collection?"));
            }

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Creating WEB4 NFT Collection...");
            result = await NFTCommon.NFTManager.CreateWeb4NFTCollectionAsync(request, providerType);

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

        public async Task<OASISResult<IWeb4NFTCollection>> UpdateWeb4NFTCollectionAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new OASISResult<IWeb4NFTCollection>();
            UpdateWeb4NFTCollectionRequest request = new UpdateWeb4NFTCollectionRequest();

            OASISResult<IWeb4NFTCollection> collectionResult = await FindWeb4NFTCollectionAsync("update", idOrName, providerType: providerType);

            if (collectionResult != null && collectionResult.Result != null && !collectionResult.IsError)
            {
                OASISResult<IUpdateWeb4NFTCollectionRequestBase> updateResult = NFTCommon.UpdateWeb4NFTCollection(request, collectionResult.Result, "WEB4 NFT Collection");

                if (updateResult != null && updateResult.Result != null && !updateResult.IsError)
                {
                    request = (UpdateWeb4NFTCollectionRequest)updateResult.Result;

                    CLIEngine.ShowWorkingMessage("Updating WEB4 NFT Collection...");
                    result = await NFTCommon.NFTManager.UpdateWeb4NFTCollectionAsync(request, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        CLIEngine.ShowSuccessMessage("WEB4 OASIS NFT Collection Successfully Updated.");
                        result = await NFTCommon.UpdateSTARNETHolonAsync("Web5STARNFTCollectionId", "NFTCollection", STARNETManager, result.Result.MetaData, result, providerType);
                    }
                    else
                    {
                        string msg = result != null ? result.Message : "";
                        OASISErrorHandling.HandleError(ref result, $"Error Occured Updating WEB4 NFT Collection in UpdateWeb4NFTCollectionAsync method. Reason: {msg}");
                    }
                }
            }
            else
            {
                string msg = collectionResult != null ? collectionResult.Message : "";
                OASISErrorHandling.HandleError(ref result, $"Error Occured Finding WEB4 NFT Collection to update in UpdateWeb4NFTCollectionAsync method. Reason: {msg}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFTCollection>>> ListAllWeb4NFTCollections(ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading WEB4 NFT Collections...");
            OASISResult<IEnumerable<IWeb4NFTCollection>> result = new OASISResult<IEnumerable<IWeb4NFTCollection>>();
            result = ListWeb4NFTCollections(await NFTCommon.NFTManager.LoadAllWeb4NFTCollectionsAsync(providerType: providerType));
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFTCollection>>> ListWeb4NFTCollectionsForAvatar(ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading WEB4 NFT Collections...");
            OASISResult<IEnumerable<IWeb4NFTCollection>> result = new OASISResult<IEnumerable<IWeb4NFTCollection>>();
            result = ListWeb4NFTCollections(await NFTCommon.NFTManager.LoadWeb4NFTCollectionsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType: providerType));
            return result;
        }

        public async Task<OASISResult<IWeb4NFTCollection>> AddWeb4NFTToCollectionAsync(string collectionIdOrName, string nftIdOrName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new OASISResult<IWeb4NFTCollection>();
            OASISResult<IWeb4NFTCollection> collection = await FindWeb4NFTCollectionAsync("add to", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding WEB4 NFT Collection to add to. Reason: {collection.Message}");
                return result;
            }

            OASISResult<IWeb4NFT> geoNft = await STARCLI.NFTs.FindWeb4NFTAsync("add", nftIdOrName, true);

            if (geoNft == null || geoNft.Result == null || geoNft.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding WEB4 NFT. Reason: {geoNft.Message}");
                return result;
            }

            CLIEngine.ShowWorkingMessage("Adding WEB4 NFT To Collection...");
            result = await NFTCommon.NFTManager.AddWeb4NFTToCollectionAsync(collection.Result.Id, geoNft.Result.Id, providerType: providerType);

            if (result != null && result.Result != null && !result.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 OASIS NFT Successfully Added to Collection.");
                await ShowNFTCollectionAsync(result.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured adding NFT to collection. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IWeb4NFTCollection>> RemoveWeb4NFTFromCollectionAsync(string collectionIdOrName, string nftIdOrName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new OASISResult<IWeb4NFTCollection>();
            OASISResult<IWeb4NFTCollection> collection = await FindWeb4NFTCollectionAsync("remove from", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding NFT Collection to remove from. Reason: {collection.Message}");
                return result;
            }

            OASISResult<IWeb4NFT> geoNft = await STARCLI.NFTs.FindWeb4NFTAsync("add", nftIdOrName, true);

            if (geoNft == null || geoNft.Result == null || geoNft.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured finding NFT. Reason: {geoNft.Message}");
                return result;
            }

            CLIEngine.ShowWorkingMessage("Removing WEB4 NFT From Collection...");
            result = await NFTCommon.NFTManager.RemoveWeb4NFTFromCollectionAsync(collection.Result.Id, geoNft.Result.Id, providerType);

            if (result != null && result.Result != null && !result.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 OASIS NFT Successfully Removed From Collection.");
                await ShowNFTCollectionAsync(result.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured removing WEB4 NFT from collection. Reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IWeb4NFTCollection>> DeleteWeb4NFTCollectionAsync(string collectionIdOrName, bool? softDelete = true, bool? deleteChildWeb4NFTs = false, bool? deleteChildWeb3NFTs = false, bool? burnChildWebNFTs = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> collection = await FindWeb4NFTCollectionAsync("delete", collectionIdOrName, true);

            if (collection == null || collection.Result == null || collection.IsError)
            {
                OASISErrorHandling.HandleError(ref collection, $"Error occured finding NFT Collection to delete. Reason: {collection.Message}");
                return collection;
            }

            if (!softDelete.HasValue)
                softDelete = CLIEngine.GetConfirmation("Do you wish to permanently delete the Web4 NFT Collection? (defaults to false)");

            if (!deleteChildWeb4NFTs.HasValue)
                deleteChildWeb4NFTs = CLIEngine.GetConfirmation("Do you wish to also delete the child Web4 NFTs? (defaults to false)");

            if (deleteChildWeb4NFTs.Value)
            {
                if (!deleteChildWeb3NFTs.HasValue)
                    deleteChildWeb3NFTs = CLIEngine.GetConfirmation("Do you wish to also delete the child Web3 NFTs? (the OASIS holon/metadata)(recommeneded/default)");
                
                if (!burnChildWebNFTs.HasValue)
                    burnChildWebNFTs = CLIEngine.GetConfirmation("Do you wish to also burn the child Web3 NFTs? (permanently destroy the Web3 NFTs on-chain) (recommeneded/default)");
            }
            else
            {
                deleteChildWeb3NFTs = false;
                burnChildWebNFTs = false;
            }

            CLIEngine.ShowWorkingMessage("Deleting WEB4 NFT Collection...");
            OASISResult<bool> deleteResult = await NFTCommon.NFTManager.DeleteWeb4NFTCollectionAsync(STAR.BeamedInAvatar.Id, collection.Result.Id, softDelete.Value, deleteChildWeb4NFTs.Value, deleteChildWeb3NFTs.Value, burnChildWebNFTs.Value, providerType: providerType);

            if (deleteResult != null && deleteResult.Result && !deleteResult.IsError)
            {
                CLIEngine.ShowSuccessMessage("WEB4 NFT Collection Successfully Deleted.");
                collection = await NFTCommon.DeleteAllSTARNETVersionsAsync("Web5STARNFTCollectionId", STARNETManager, collection.Result.MetaData, collection, providerType);
            }
            else
            {
                string msg = deleteResult != null ? deleteResult.Message : "";
                OASISErrorHandling.HandleError(ref collection, $"Error occured deleting WEB4 NFT Collection. Reason: {msg}");
            }

            return collection;
        }

        public virtual async Task<OASISResult<IWeb4NFTCollection>> ShowWeb4NFTCollectionAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new OASISResult<IWeb4NFTCollection>();

            //Console.WriteLine("");
            //CLIEngine.ShowWorkingMessage($"Loading WEB4 NFT Collection's...");

            result = await FindWeb4NFTCollectionAsync("view", idOrName, true, providerType: providerType);

            //if (result != null && result.Result != null && !result.IsError)
            //    await ShowNFTCollectionAsync(result.Result);
            //else
            //    OASISErrorHandling.HandleError(ref result, "No WEB4 NFT Collection Found For That Id or Name!");

            return result;
        }

        public virtual async Task SearchWeb4NFTCollectionAsync(string searchTerm = "", bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
                searchTerm = CLIEngine.GetValidInput($"What is the name of the WEB4 NFT Collection you wish to search for?");

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching WEB4 NFT Collection's...");
            ListWeb4NFTCollections(await NFTCommon.NFTManager.SearchWeb4NFTCollectionsAsync(searchTerm, STAR.BeamedInAvatar.Id, null, MetaKeyValuePairMatchMode.All, !showForAllAvatars, providerType));
        }

        private async Task<OASISResult<IWeb4NFTCollection>> FindWeb4NFTCollectionAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = false, bool addSpace = true, string UIName = "WEB4 NFT Collection", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new OASISResult<IWeb4NFTCollection>();
            Guid id = Guid.Empty;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IWeb4NFTCollection>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {UIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {UIName}'s...");

                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await NFTCommon.NFTManager.LoadWeb4NFTCollectionsForAvatarAsync(STAR.BeamedInAvatar.AvatarId, providerType: providerType);
                        else
                            starHolonsResult = await NFTCommon.NFTManager.LoadAllWeb4NFTCollectionsAsync(providerType: providerType);

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
                    result = await NFTCommon.NFTManager.LoadWeb4NFTCollectionAsync(id, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {UIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {UIName}s...");
                    OASISResult<IEnumerable<IWeb4NFTCollection>> searchResults = await NFTCommon.NFTManager.SearchWeb4NFTCollectionsAsync(idOrName, STAR.BeamedInAvatar.Id, null, MetaKeyValuePairMatchMode.All, showOnlyForCurrentAvatar, providerType);

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

        private OASISResult<IEnumerable<IWeb4NFTCollection>> ListWeb4NFTCollections(OASISResult<IEnumerable<IWeb4NFTCollection>> collections, bool showNumbers = false, bool showDetailedInfo = false)
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

        private async Task ShowNFTCollectionAsync(IWeb4NFTCollection collection, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH)
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
            TagHelper.ShowTags(collection.Tags, displayFieldLength);

            Dictionary<string, string> metaData = collection.MetaData;

            //Temp remove internal metaData.
            collection.MetaData.Remove("Image");
            collection.MetaData.Remove("ImageUrl");
            collection.MetaData.Remove("Thumbnail");
            collection.MetaData.Remove("ThumbnailUrl");
            collection.MetaData.Remove("Web4NFTs");
            collection.MetaData.Remove("Web4OASISNFTIds");
            collection.MetaData.Remove("Tags");

            MetaDataHelper.ShowMetaData(collection.MetaData, displayFieldLength);
            collection.MetaData = metaData;

            if (collection.Web4NFTs.Count() == 0 && collection.Web4NFTIds.Count() > 0)
            {
                OASISResult<IList<IWeb4NFT>> nfts = await NFTCommon.NFTManager.LoadChildWeb4NFTsForNFTCollectionAsync(collection.Web4NFTIds);

                if (nfts != null && nfts.Result != null && !nfts.IsError)
                    collection.Web4NFTs = nfts.Result.ToList();
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading child nfts. Reason: {nfts.Message}");
            }

            ShowNFTCollectionNFTs(collection.Web4NFTs, showDetailedInfo, 20);

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        private void ShowNFTCollectionNFTs(IEnumerable<IWeb4NFT> nfts, bool showDetailed = false, int defaultFieldLength = 37)
        {
            if (nfts != null)
            {
                CLIEngine.ShowMessage($"{nfts.Count()} NFT(s) in this collection:");
                defaultFieldLength = 37;

                if (showDetailed)
                {
                    foreach (IWeb4NFT nft in nfts)
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

                    foreach (IWeb4NFT nft in nfts)
                    {
                        if (nft != null)
                            CLIEngine.ShowMessage(string.Concat(nft.Id.ToString().PadRight(defaultFieldLength), " | ", nft.Title.PadRight(defaultFieldLength)), false);
                    }
                }
            }
        }
    }
}